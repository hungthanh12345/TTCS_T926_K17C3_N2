using InternshipManagementApi.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DatabaseSchemaChecker;

internal static class SchemaInventoryReader
{
    public static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(
                "Server=127.0.0.1;Port=1;Database=model_only;User ID=model_only;Connection Timeout=1",
                new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;
        return new AppDbContext(options);
    }

    public static SchemaInventory Read(DbContext context)
    {
        var model = context.GetService<IDesignTimeModel>().Model;
        var tables = new List<TableSpec>();
        var indexes = new List<IndexSpec>();
        var foreignKeys = new List<ForeignKeySpec>();
        var checks = new List<CheckSpec>();

        foreach (var entity in model.GetEntityTypes().OrderBy(item => item.GetTableName(), StringComparer.Ordinal))
        {
            var tableName = entity.GetTableName();
            if (tableName is null)
            {
                continue;
            }

            var storeObject = StoreObjectIdentifier.Table(tableName, entity.GetSchema());
            var tableCharset = Annotation(entity, "MySql:CharSet") ??
                               Annotation(model, "MySql:CharSet") ??
                               "utf8mb4";
            var tableCollation = Annotation(entity, "Relational:Collation");

            var columns = entity.GetProperties()
                .Select(property =>
                {
                    var columnName = property.GetColumnName(storeObject);
                    if (columnName is null)
                    {
                        return null;
                    }

                    var storeType = property.GetColumnType(storeObject) ??
                                    property.GetRelationalTypeMapping().StoreType;
                    var defaultSql = Annotation(property, "Relational:DefaultValueSql");
                    var defaultAnnotation = property.FindAnnotation("Relational:DefaultValue");
                    var hasDefault = defaultSql is not null || defaultAnnotation is not null;
                    var defaultValue = defaultSql ?? ConvertDefaultValue(property, defaultAnnotation?.Value);
                    var isString = IsStringType(storeType);
                    var charset = isString
                        ? Annotation(property, "MySql:CharSet") ?? tableCharset
                        : null;
                    var collation = isString
                        ? Annotation(property, "Relational:Collation") ?? tableCollation
                        : null;

                    return new ColumnSpec(
                        tableName,
                        columnName,
                        storeType,
                        property.IsNullable,
                        hasDefault,
                        defaultValue,
                        charset,
                        collation);
                })
                .Where(column => column is not null)
                .Cast<ColumnSpec>()
                .OrderBy(column => column.Name, StringComparer.Ordinal)
                .ToArray();

            var primaryKey = entity.FindPrimaryKey();
            var primaryColumns = primaryKey?.Properties
                .Select(property => property.GetColumnName(storeObject)!)
                .ToArray() ?? Array.Empty<string>();

            tables.Add(new TableSpec(tableName, tableCharset, tableCollation, columns, primaryColumns));

            foreach (var index in entity.GetIndexes())
            {
                indexes.Add(new IndexSpec(
                    tableName,
                    index.GetDatabaseName(storeObject) ?? $"IX_{tableName}_{string.Join("_", index.Properties.Select(property => property.GetColumnName(storeObject)))}",
                    index.Properties.Select(property => property.GetColumnName(storeObject)!).ToArray(),
                    index.IsUnique));
            }

            foreach (var key in entity.GetKeys().Where(key => !key.IsPrimaryKey()))
            {
                var keyName = key.GetName() ?? $"AK_{tableName}_{string.Join("_", key.Properties.Select(property => property.GetColumnName(storeObject)))}";
                indexes.Add(new IndexSpec(
                    tableName,
                    keyName,
                    key.Properties.Select(property => property.GetColumnName(storeObject)!).ToArray(),
                    Unique: true,
                    UniqueConstraint: true));
            }

            foreach (var foreignKey in entity.GetForeignKeys())
            {
                var principalTable = foreignKey.PrincipalEntityType.GetTableName();
                if (principalTable is null)
                {
                    continue;
                }

                var principalStore = StoreObjectIdentifier.Table(
                    principalTable,
                    foreignKey.PrincipalEntityType.GetSchema());

                foreignKeys.Add(new ForeignKeySpec(
                    tableName,
                    foreignKey.GetConstraintName(storeObject, principalStore) ?? $"FK_{tableName}_{principalTable}",
                    foreignKey.Properties.Select(property => property.GetColumnName(storeObject)!).ToArray(),
                    principalTable,
                    foreignKey.PrincipalKey.Properties.Select(property => property.GetColumnName(principalStore)!).ToArray(),
                    DeleteRule(foreignKey.DeleteBehavior),
                    KnownUpdateRule(tableName, foreignKey.Properties.Select(property => property.GetColumnName(storeObject)!).ToArray())));
            }

            foreach (var check in entity.GetCheckConstraints())
            {
                checks.Add(new CheckSpec(tableName, check.Name ?? $"CK_{tableName}", check.Sql, true));
            }
        }

        return new SchemaInventory(tables, indexes, foreignKeys, checks);
    }

    private static string? Annotation(IReadOnlyAnnotatable annotatable, string name) =>
        annotatable.FindAnnotation(name)?.Value?.ToString();

    private static bool IsStringType(string storeType) =>
        storeType.StartsWith("varchar", StringComparison.OrdinalIgnoreCase) ||
        storeType.StartsWith("char", StringComparison.OrdinalIgnoreCase) ||
        storeType.Equals("text", StringComparison.OrdinalIgnoreCase) ||
        storeType.Equals("tinytext", StringComparison.OrdinalIgnoreCase) ||
        storeType.Equals("mediumtext", StringComparison.OrdinalIgnoreCase) ||
        storeType.Equals("longtext", StringComparison.OrdinalIgnoreCase) ||
        storeType.StartsWith("enum(", StringComparison.OrdinalIgnoreCase) ||
        storeType.StartsWith("set(", StringComparison.OrdinalIgnoreCase);

    private static string? ConvertDefaultValue(IProperty property, object? value)
    {
        if (value is null)
        {
            return null;
        }

        var converter = property.GetTypeMapping().Converter;
        var providerValue = converter?.ConvertToProvider(value) ?? value;

        return providerValue switch
        {
            bool boolean => boolean ? "1" : "0",
            Enum enumValue => enumValue.ToString(),
            DateTime dateTime => dateTime.ToString("yyyy-MM-dd HH:mm:ss.ffffff", System.Globalization.CultureInfo.InvariantCulture),
            _ => Convert.ToString(providerValue, System.Globalization.CultureInfo.InvariantCulture)
        };
    }

    // EF Core does not model ON UPDATE. Keep only project-specific rules backed by
    // reviewed deployment SQL; all other update actions are reported as observed.
    private static string? KnownUpdateRule(string table, IReadOnlyList<string> columns)
    {
        if (table.Equals("internship_tasks", StringComparison.OrdinalIgnoreCase) &&
            columns.SequenceEqual(new[] { "mentor_id" }, StringComparer.OrdinalIgnoreCase))
            return "CASCADE";

        if (table.Equals("internship_contracts", StringComparison.OrdinalIgnoreCase) &&
            columns.SequenceEqual(new[] { "student_id" }, StringComparer.OrdinalIgnoreCase))
            return "CASCADE";

        return null;
    }
    private static string DeleteRule(DeleteBehavior behavior) => behavior switch
    {
        DeleteBehavior.Cascade => "CASCADE",
        DeleteBehavior.SetNull => "SET NULL",
        DeleteBehavior.ClientCascade => "CASCADE",
        _ => "RESTRICT"
    };
}
