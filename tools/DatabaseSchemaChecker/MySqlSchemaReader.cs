using System.Data;
using MySqlConnector;

namespace DatabaseSchemaChecker;

internal sealed record MySqlIdentity(string Database, string ServerVersion, string DefaultCharset, string DefaultCollation, bool HistoryTablePresent, bool HistoryTableValid);

internal static class MySqlSchemaReader
{
    public static async Task<(MySqlIdentity Identity, SchemaInventory Inventory, IReadOnlyList<string> History)> ReadAsync(
        string connectionString,
        string schema,
        CancellationToken cancellationToken)
    {
        await using var connection = new MySqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        var identity = await ReadIdentityAsync(connection, schema, cancellationToken);
        var schemaDefaults = await ReadSchemaDefaultsAsync(connection, schema, cancellationToken);
        var tables = await ReadTablesAsync(connection, schema, cancellationToken);
        var columns = await ReadColumnsAsync(connection, schema, cancellationToken);
        var statistics = await ReadStatisticsAsync(connection, schema, cancellationToken);
        var uniqueConstraintNames = await ReadUniqueConstraintNamesAsync(connection, schema, cancellationToken);
        statistics = statistics.Select(index => index with
        {
            UniqueConstraint = uniqueConstraintNames.Contains($"{index.Table}|{index.Name}")
        }).ToList();
        var foreignKeys = await ReadForeignKeysAsync(connection, schema, cancellationToken);
        var checks = await ReadChecksAsync(connection, schema, cancellationToken);
        var historyTablePresent = tables.Any(table => table.Name.Equals("__EFMigrationsHistory", StringComparison.OrdinalIgnoreCase));
        var historyTableValid = IsEfHistoryTableValid(tables, columns, statistics);
        var history = await ReadMigrationHistoryAsync(connection, tables, cancellationToken);

        var tableSpecs = tables
            .Where(table => !table.Name.Equals("__EFMigrationsHistory", StringComparison.OrdinalIgnoreCase))
            .Select(table =>
            {
                var tableColumns = columns.Where(column =>
                    column.Table.Equals(table.Name, StringComparison.OrdinalIgnoreCase)).ToArray();
                var tableStatistics = statistics.Where(index =>
                    index.Table.Equals(table.Name, StringComparison.OrdinalIgnoreCase)).ToArray();
                var primaryKey = tableStatistics.FirstOrDefault(index => index.Primary);
                return new TableSpec(
                    table.Name,
                    table.Charset,
                    table.Collation,
                    tableColumns,
                    primaryKey?.Columns ?? Array.Empty<string>());
            })
            .ToArray();

        var nonPrimaryIndexes = statistics.Where(index => !index.Primary).ToArray();
        var inventory = new SchemaInventory(tableSpecs, nonPrimaryIndexes, foreignKeys, checks);
        return (identity with
        {
            DefaultCharset = schemaDefaults.Charset,
            DefaultCollation = schemaDefaults.Collation,
            HistoryTablePresent = historyTablePresent,
            HistoryTableValid = historyTableValid
        }, inventory, history);
    }

    private static async Task<MySqlIdentity> ReadIdentityAsync(
        MySqlConnection connection,
        string expectedSchema,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT DATABASE(), CURRENT_USER(), VERSION()";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException("Could not verify the connected MySQL target.");

        var database = reader.GetString(0);
        var effectiveUser = reader.GetString(1);
        var version = reader.GetString(2);
        if (!SchemaPolicy.IsNonRootEffectiveUser(effectiveUser))
            throw new InvalidOperationException("Effective MySQL account is root or cannot be verified as non-root.");
        if (!database.Equals(expectedSchema, StringComparison.Ordinal))
            throw new InvalidOperationException("Connected schema differs from the confirmed Test schema.");

        return new MySqlIdentity(database, version, string.Empty, string.Empty, false, false);
    }

    private static async Task<(string Charset, string Collation)> ReadSchemaDefaultsAsync(
        MySqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT DEFAULT_CHARACTER_SET_NAME, DEFAULT_COLLATION_NAME
            FROM information_schema.SCHEMATA
            WHERE SCHEMA_NAME = @schema
            """;
        command.Parameters.AddWithValue("@schema", schema);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            throw new InvalidOperationException("Confirmed Test schema was not found.");

        return (reader.GetString(0), reader.GetString(1));
    }

    private static async Task<List<(string Name, string? Charset, string? Collation)>> ReadTablesAsync(
        MySqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        var result = new List<(string, string?, string?)>();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT table_info.TABLE_NAME, collations.CHARACTER_SET_NAME, table_info.TABLE_COLLATION
            FROM information_schema.TABLES AS table_info
            LEFT JOIN information_schema.COLLATIONS AS collations
              ON collations.COLLATION_NAME = table_info.TABLE_COLLATION
            WHERE table_info.TABLE_SCHEMA = @schema
              AND table_info.TABLE_TYPE = 'BASE TABLE'
            ORDER BY table_info.TABLE_NAME
            """;
        command.Parameters.AddWithValue("@schema", schema);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add((reader.GetString(0), NullableString(reader, 1), NullableString(reader, 2)));
        return result;
    }

    private static async Task<List<ColumnSpec>> ReadColumnsAsync(
        MySqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        var result = new List<ColumnSpec>();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TABLE_NAME, COLUMN_NAME, COLUMN_TYPE, IS_NULLABLE, COLUMN_DEFAULT,
                   CHARACTER_SET_NAME, COLLATION_NAME
            FROM information_schema.COLUMNS
            WHERE TABLE_SCHEMA = @schema
            ORDER BY TABLE_NAME, ORDINAL_POSITION
            """;
        command.Parameters.AddWithValue("@schema", schema);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new ColumnSpec(
                reader.GetString(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetString(3).Equals("YES", StringComparison.OrdinalIgnoreCase),
                HasDatabaseDefault(reader, 4),
                NullableString(reader, 4),
                NullableString(reader, 5),
                NullableString(reader, 6)));
        }
        return result;
    }

    private static async Task<List<IndexSpec>> ReadStatisticsAsync(
        MySqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        var rows = new List<(string Table, string Name, bool Unique, bool Primary, int Ordinal, string Column)>();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TABLE_NAME, INDEX_NAME, NON_UNIQUE, SEQ_IN_INDEX, COLUMN_NAME, SUB_PART
            FROM information_schema.STATISTICS
            WHERE TABLE_SCHEMA = @schema
            ORDER BY TABLE_NAME, INDEX_NAME, SEQ_IN_INDEX
            """;
        command.Parameters.AddWithValue("@schema", schema);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var column = NullableString(reader, 4) ?? "<expression>";
            var prefixLength = reader.IsDBNull(5) ? null : reader.GetInt64(5).ToString(System.Globalization.CultureInfo.InvariantCulture);
            if (prefixLength is not null)
                column += $"(prefix:{prefixLength})";
            var name = reader.GetString(1);
            rows.Add((reader.GetString(0), name, reader.GetInt32(2) == 0,
                name.Equals("PRIMARY", StringComparison.OrdinalIgnoreCase), reader.GetInt32(3), column));
        }

        return rows.GroupBy(row => new { row.Table, row.Name })
            .Select(group => new IndexSpec(
                group.Key.Table,
                group.Key.Name,
                group.OrderBy(row => row.Ordinal).Select(row => row.Column).ToArray(),
                group.First().Unique,
                group.First().Primary))
            .ToList();
    }

    private static async Task<HashSet<string>> ReadUniqueConstraintNamesAsync(
        MySqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT TABLE_NAME, CONSTRAINT_NAME
            FROM information_schema.TABLE_CONSTRAINTS
            WHERE CONSTRAINT_SCHEMA = @schema AND CONSTRAINT_TYPE = 'UNIQUE'
            """;
        command.Parameters.AddWithValue("@schema", schema);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add($"{reader.GetString(0)}|{reader.GetString(1)}");
        return result;
    }

    private static async Task<List<ForeignKeySpec>> ReadForeignKeysAsync(
        MySqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        var rows = new List<(string Table, string Name, string Column, int Ordinal, string PrincipalTable, string PrincipalColumn, string DeleteRule, string UpdateRule)>();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT key_usage.TABLE_NAME, key_usage.CONSTRAINT_NAME, key_usage.COLUMN_NAME,
                   key_usage.ORDINAL_POSITION, key_usage.REFERENCED_TABLE_NAME,
                   key_usage.REFERENCED_COLUMN_NAME, referential.DELETE_RULE, referential.UPDATE_RULE
            FROM information_schema.KEY_COLUMN_USAGE AS key_usage
            JOIN information_schema.REFERENTIAL_CONSTRAINTS AS referential
              ON referential.CONSTRAINT_SCHEMA = key_usage.CONSTRAINT_SCHEMA
             AND referential.TABLE_NAME = key_usage.TABLE_NAME
             AND referential.CONSTRAINT_NAME = key_usage.CONSTRAINT_NAME
            WHERE key_usage.CONSTRAINT_SCHEMA = @schema
              AND key_usage.REFERENCED_TABLE_NAME IS NOT NULL
            ORDER BY key_usage.TABLE_NAME, key_usage.CONSTRAINT_NAME, key_usage.ORDINAL_POSITION
            """;
        command.Parameters.AddWithValue("@schema", schema);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add((reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.GetInt32(3), reader.GetString(4), reader.GetString(5),
                NormalizeRule(reader.GetString(6)), NormalizeRule(reader.GetString(7))));
        }

        return rows.GroupBy(row => new { row.Table, row.Name })
            .Select(group => new ForeignKeySpec(
                group.Key.Table,
                group.Key.Name,
                group.OrderBy(row => row.Ordinal).Select(row => row.Column).ToArray(),
                group.First().PrincipalTable,
                group.OrderBy(row => row.Ordinal).Select(row => row.PrincipalColumn).ToArray(),
                group.First().DeleteRule,
                group.First().UpdateRule))
            .ToList();
    }

    private static async Task<List<CheckSpec>> ReadChecksAsync(
        MySqlConnection connection,
        string schema,
        CancellationToken cancellationToken)
    {
        var result = new List<CheckSpec>();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT table_constraints.TABLE_NAME, table_constraints.CONSTRAINT_NAME,
                   check_constraints.CHECK_CLAUSE, table_constraints.ENFORCED
            FROM information_schema.TABLE_CONSTRAINTS AS table_constraints
            JOIN information_schema.CHECK_CONSTRAINTS AS check_constraints
              ON check_constraints.CONSTRAINT_SCHEMA = table_constraints.CONSTRAINT_SCHEMA
             AND check_constraints.CONSTRAINT_NAME = table_constraints.CONSTRAINT_NAME
            WHERE table_constraints.CONSTRAINT_SCHEMA = @schema
              AND table_constraints.CONSTRAINT_TYPE = 'CHECK'
            ORDER BY table_constraints.TABLE_NAME, table_constraints.CONSTRAINT_NAME
            """;
        command.Parameters.AddWithValue("@schema", schema);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var enforcedValue = reader.GetString(3);
            result.Add(new CheckSpec(reader.GetString(0), reader.GetString(1), reader.GetString(2),
                enforcedValue.Equals("YES", StringComparison.OrdinalIgnoreCase), enforcedValue));
        }
        return result;
    }

    internal static bool IsEfHistoryTableValid(
        IReadOnlyList<(string Name, string? Charset, string? Collation)> tables,
        IReadOnlyList<ColumnSpec> columns,
        IReadOnlyList<IndexSpec> indexes)
    {
        const string tableName = "__EFMigrationsHistory";
        if (!tables.Any(table => table.Name.Equals(tableName, StringComparison.OrdinalIgnoreCase)))
            return false;

        var historyColumns = columns.Where(column =>
            column.Table.Equals(tableName, StringComparison.OrdinalIgnoreCase)).ToArray();
        var migrationId = historyColumns.SingleOrDefault(column =>
            column.Name.Equals("MigrationId", StringComparison.OrdinalIgnoreCase));
        var productVersion = historyColumns.SingleOrDefault(column =>
            column.Name.Equals("ProductVersion", StringComparison.OrdinalIgnoreCase));
        var primaryKey = indexes.FirstOrDefault(index =>
            index.Table.Equals(tableName, StringComparison.OrdinalIgnoreCase) && index.Primary);

        return migrationId is { Nullable: false } &&
               migrationId.StoreType.Equals("varchar(150)", StringComparison.OrdinalIgnoreCase) &&
               productVersion is { Nullable: false } &&
               productVersion.StoreType.Equals("varchar(32)", StringComparison.OrdinalIgnoreCase) &&
               primaryKey is not null &&
               primaryKey.Columns.SequenceEqual(new[] { "MigrationId" }, StringComparer.OrdinalIgnoreCase);
    }
    private static async Task<IReadOnlyList<string>> ReadMigrationHistoryAsync(
        MySqlConnection connection,
        IReadOnlyList<(string Name, string? Charset, string? Collation)> tables,
        CancellationToken cancellationToken)
    {
        if (!tables.Any(table => table.Name.Equals("__EFMigrationsHistory", StringComparison.OrdinalIgnoreCase)))
            return Array.Empty<string>();

        var result = new List<string>();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT MigrationId FROM __EFMigrationsHistory ORDER BY MigrationId";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            result.Add(reader.GetString(0));
        return result;
    }

    private static bool HasDatabaseDefault(IDataRecord reader, int ordinal) =>
        !reader.IsDBNull(ordinal);

    private static string? NullableString(IDataRecord reader, int ordinal) =>
        reader.IsDBNull(ordinal) ? null : Convert.ToString(reader.GetValue(ordinal), System.Globalization.CultureInfo.InvariantCulture);

    private static string NormalizeRule(string rule) =>
        rule.Equals("NO ACTION", StringComparison.OrdinalIgnoreCase) ? "RESTRICT" : rule.ToUpperInvariant();
}
