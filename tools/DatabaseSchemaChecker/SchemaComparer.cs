using System.Text.RegularExpressions;

namespace DatabaseSchemaChecker;

internal static class SchemaComparer
{
    public static IReadOnlyList<SchemaDifference> Compare(SchemaInventory expected, SchemaInventory actual)
    {
        var result = new List<SchemaDifference>();
        var expectedTables = expected.Tables.ToDictionary(table => Key(table.Name), StringComparer.Ordinal);
        var actualTables = actual.Tables.ToDictionary(table => Key(table.Name), StringComparer.Ordinal);

        foreach (var table in expected.Tables)
        {
            if (!actualTables.TryGetValue(Key(table.Name), out var actualTable))
            {
                result.Add(new("Table", table.Name, table.Name, "MISSING", Describe(table), null));
                foreach (var column in table.Columns)
                    result.Add(new("Column", table.Name, column.Name, "MISSING", Describe(column), null));
                if (table.PrimaryKeyColumns.Count > 0)
                    result.Add(new("PrimaryKey", table.Name, "PRIMARY", "MISSING", Join(table.PrimaryKeyColumns), null));
                continue;
            }

            var tableMatches = TableMatches(table, actualTable);
            result.Add(new("Table", table.Name, table.Name, tableMatches ? "MATCHED" : "DIFFERENT",
                Describe(table), Describe(actualTable)));
            CompareColumns(table, actualTable, result);

            var primaryKeyMatches = SequenceEqual(table.PrimaryKeyColumns, actualTable.PrimaryKeyColumns);
            result.Add(new(
                "PrimaryKey",
                table.Name,
                "PRIMARY",
                primaryKeyMatches ? "MATCHED" : actualTable.PrimaryKeyColumns.Count == 0 ? "MISSING" : "DIFFERENT",
                Join(table.PrimaryKeyColumns),
                Join(actualTable.PrimaryKeyColumns)));
        }

        foreach (var table in actual.Tables.Where(table =>
                     !expectedTables.ContainsKey(Key(table.Name)) &&
                     !table.Name.Equals("__EFMigrationsHistory", StringComparison.OrdinalIgnoreCase)))
        {
            result.Add(new("Table", table.Name, table.Name, "EXTRA", null, Describe(table),
                "Database contains a table not represented in the current EF model; preserved."));
        }

        CompareIndexes(expected, actual, result);
        CompareForeignKeys(expected, actual, result);
        CompareChecks(expected, actual, result);
        return result;
    }

    public static InventoryCounts Counts(SchemaInventory inventory) =>
        new(inventory.Tables.Count, inventory.Tables.Sum(table => table.Columns.Count),
            inventory.ForeignKeys.Count,
            inventory.Indexes.Count(index => !index.Primary && !index.UniqueConstraint),
            inventory.Indexes.Count(index => index.UniqueConstraint),
            inventory.Checks.Count);

    public static InventoryCounts CountStatus(IReadOnlyList<SchemaDifference> differences, string status)
    {
        var items = differences.Where(item => item.Status.Equals(status, StringComparison.Ordinal)).ToArray();
        return new(
            items.Count(item => item.Kind == "Table"),
            items.Count(item => item.Kind == "Column"),
            items.Count(item => item.Kind == "ForeignKey"),
            items.Count(item => item.Kind == "Index"),
            items.Count(item => item.Kind == "UniqueConstraint"),
            items.Count(item => item.Kind == "Check"));
    }

    internal static string NormalizeCheckExpression(string value)
        => CheckExpressionNormalizer.Normalize(value);

    private static bool TableMatches(TableSpec expected, TableSpec actual) =>
        (expected.Charset is null || EqualsIgnoreCase(expected.Charset, actual.Charset)) &&
        (expected.Collation is null || EqualsIgnoreCase(expected.Collation, actual.Collation));

    private static void CompareColumns(TableSpec expected, TableSpec actual, List<SchemaDifference> result)
    {
        var expectedColumns = expected.Columns.ToDictionary(column => Key(column.Name), StringComparer.Ordinal);
        var actualColumns = actual.Columns.ToDictionary(column => Key(column.Name), StringComparer.Ordinal);

        foreach (var column in expected.Columns)
        {
            if (!actualColumns.TryGetValue(Key(column.Name), out var actualColumn))
            {
                result.Add(new("Column", expected.Name, column.Name, "MISSING", Describe(column), null));
                continue;
            }

            var matches =
                NormalizeType(column.StoreType) == NormalizeType(actualColumn.StoreType) &&
                column.Nullable == actualColumn.Nullable &&
                (column.HasDefault
                    ? NormalizeDefault(column.DefaultValue, column.StoreType) ==
                      NormalizeDefault(actualColumn.DefaultValue, actualColumn.StoreType)
                    : actualColumn.DefaultValue is null) &&
                (column.Charset is null || EqualsIgnoreCase(column.Charset, actualColumn.Charset)) &&
                (column.Collation is null || EqualsIgnoreCase(column.Collation, actualColumn.Collation));

            result.Add(new("Column", expected.Name, column.Name, matches ? "MATCHED" : "DIFFERENT",
                Describe(column), Describe(actualColumn)));
        }

        foreach (var column in actual.Columns.Where(column => !expectedColumns.ContainsKey(Key(column.Name))))
        {
            result.Add(new("Column", actual.Name, column.Name, "EXTRA", null, Describe(column),
                "Database contains a column not represented in the current EF model; preserved."));
        }
    }

    private static void CompareIndexes(SchemaInventory expected, SchemaInventory actual, List<SchemaDifference> result)
    {
        var consumed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var index in expected.Indexes)
        {
            var candidates = actual.Indexes.Where(candidate =>
                candidate.Table.Equals(index.Table, StringComparison.OrdinalIgnoreCase) &&
                !candidate.Primary).ToArray();

            // Match by enforced key shape first: a MySQL unique index and UNIQUE constraint
            // have the same uniqueness semantics even when metadata classifies them differently.
            var match = candidates.FirstOrDefault(candidate => SameIndexDefinition(index, candidate));
            if (match is not null)
            {
                consumed.Add(IndexKey(match));
                var noteParts = new List<string>();
                if (!index.Name.Equals(match.Name, StringComparison.OrdinalIgnoreCase))
                    noteParts.Add($"Equivalent key uses database name '{match.Name}'.");
                if (index.UniqueConstraint != match.UniqueConstraint)
                    noteParts.Add("MySQL exposes the equivalent unique key using a different constraint/index representation.");
                result.Add(new(IndexKind(index), index.Table, index.Name, "MATCHED",
                    Describe(index), Describe(match), noteParts.Count == 0 ? null : string.Join(" ", noteParts)));
                continue;
            }

            var sameName = candidates.FirstOrDefault(candidate =>
                candidate.Name.Equals(index.Name, StringComparison.OrdinalIgnoreCase));
            if (sameName is not null)
            {
                consumed.Add(IndexKey(sameName));
                result.Add(new(IndexKind(index), index.Table, index.Name, "DIFFERENT",
                    Describe(index), Describe(sameName)));
            }
            else
            {
                result.Add(new(IndexKind(index), index.Table, index.Name, "MISSING", Describe(index), null));
            }
        }

        foreach (var actualIndex in actual.Indexes.Where(index =>
                     !index.Primary && !consumed.Contains(IndexKey(index))))
        {
            result.Add(new(IndexKind(actualIndex), actualIndex.Table, actualIndex.Name, "EXTRA",
                null, Describe(actualIndex),
                "Database contains an additional index or unique key not required by the current EF model; preserved."));
        }
    }

    private static string IndexKind(IndexSpec index) => index.UniqueConstraint ? "UniqueConstraint" : "Index";

    private static void CompareForeignKeys(SchemaInventory expected, SchemaInventory actual, List<SchemaDifference> result)
    {
        foreach (var foreignKey in expected.ForeignKeys)
        {
            var candidates = actual.ForeignKeys.Where(candidate => SameForeignKeyShape(candidate, foreignKey)).ToArray();
            var match = candidates.FirstOrDefault(candidate =>
                SameRule(candidate.DeleteRule, foreignKey.DeleteRule) &&
                (foreignKey.UpdateRule is null || SameRule(candidate.UpdateRule, foreignKey.UpdateRule)));

            if (match is not null)
            {
                var note = !foreignKey.Name.Equals(match.Name, StringComparison.OrdinalIgnoreCase)
                    ? $"Equivalent FK uses database name '{match.Name}'."
                    : null;
                var updateNote = foreignKey.UpdateRule is null
                    ? $"Observed ON UPDATE {match.UpdateRule}; EF Core does not encode this rule for this relationship."
                    : null;
                note = string.Join(" ", new[] { note, updateNote }.Where(value => !string.IsNullOrWhiteSpace(value)));
                result.Add(new("ForeignKey", foreignKey.Table, foreignKey.Name, "MATCHED",
                    Describe(foreignKey), Describe(match), string.IsNullOrWhiteSpace(note) ? null : note));
            }
            else if (candidates.Length > 0)
            {
                result.Add(new("ForeignKey", foreignKey.Table, foreignKey.Name, "DIFFERENT",
                    Describe(foreignKey), Describe(candidates[0])));
            }
            else
            {
                result.Add(new("ForeignKey", foreignKey.Table, foreignKey.Name, "MISSING",
                    Describe(foreignKey), null));
            }
        }

        foreach (var foreignKey in actual.ForeignKeys.Where(actualKey =>
                     !expected.ForeignKeys.Any(expectedKey => SameForeignKeyShape(expectedKey, actualKey))))
        {
            result.Add(new("ForeignKey", foreignKey.Table, foreignKey.Name, "EXTRA", null, Describe(foreignKey),
                "Database contains an FK not represented in the current EF model; preserved for review."));
        }
    }

    private static void CompareChecks(SchemaInventory expected, SchemaInventory actual, List<SchemaDifference> result)
    {
        foreach (var check in expected.Checks)
        {
            var normalizedExpected = NormalizeCheckExpression(check.Expression);
            var candidates = actual.Checks.Where(candidate =>
                candidate.Table.Equals(check.Table, StringComparison.OrdinalIgnoreCase) &&
                (candidate.Name.Equals(check.Name, StringComparison.OrdinalIgnoreCase) ||
                 NormalizeCheckExpression(candidate.Expression) == normalizedExpected)).ToArray();
            var match = candidates.FirstOrDefault(candidate =>
                candidate.Enforced && NormalizeCheckExpression(candidate.Expression) == normalizedExpected);

            if (match is not null)
            {
                var note = !check.Name.Equals(match.Name, StringComparison.OrdinalIgnoreCase)
                    ? $"Equivalent CHECK uses database name '{match.Name}'."
                    : null;
                result.Add(new("Check", check.Table, check.Name, "MATCHED",
                    check.Expression, match.Expression, note));
            }
            else if (candidates.Length > 0)
            {
                result.Add(new("Check", check.Table, check.Name, "DIFFERENT",
                    check.Expression, candidates[0].Expression));
            }
            else
            {
                result.Add(new("Check", check.Table, check.Name, "MISSING", check.Expression, null));
            }
        }

        foreach (var check in actual.Checks.Where(actualCheck =>
                     !expected.Checks.Any(expectedCheck =>
                         expectedCheck.Table.Equals(actualCheck.Table, StringComparison.OrdinalIgnoreCase) &&
                         NormalizeCheckExpression(expectedCheck.Expression) == NormalizeCheckExpression(actualCheck.Expression)) &&
                     !expected.Checks.Any(expectedCheck =>
                         expectedCheck.Table.Equals(actualCheck.Table, StringComparison.OrdinalIgnoreCase) &&
                         expectedCheck.Name.Equals(actualCheck.Name, StringComparison.OrdinalIgnoreCase))))
        {
            result.Add(new("Check", check.Table, check.Name, "EXTRA", null, check.Expression,
                "Database contains a CHECK not represented in the current EF model; preserved."));
        }
    }

    private static bool SameForeignKeyShape(ForeignKeySpec left, ForeignKeySpec right) =>
        left.Table.Equals(right.Table, StringComparison.OrdinalIgnoreCase) &&
        SequenceEqual(left.Columns, right.Columns) &&
        left.PrincipalTable.Equals(right.PrincipalTable, StringComparison.OrdinalIgnoreCase) &&
        SequenceEqual(left.PrincipalColumns, right.PrincipalColumns);

    private static bool SameIndexDefinition(IndexSpec left, IndexSpec right) =>
        left.Unique == right.Unique && SequenceEqual(left.Columns, right.Columns);

    private static bool SameRule(string? left, string? right) =>
        left is not null && right is not null &&
        NormalizeRule(left).Equals(NormalizeRule(right), StringComparison.OrdinalIgnoreCase);

    private static string NormalizeRule(string rule) =>
        rule.Equals("NO ACTION", StringComparison.OrdinalIgnoreCase) ? "RESTRICT" : rule.Trim().ToUpperInvariant();

    private static string IndexKey(IndexSpec index) => $"{Key(index.Table)}|{Key(index.Name)}";
    private static bool EqualsIgnoreCase(string? left, string? right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    private static bool SequenceEqual(IReadOnlyList<string> left, IReadOnlyList<string> right) =>
        left.SequenceEqual(right, StringComparer.OrdinalIgnoreCase);
    private static string Key(string value) => value.ToLowerInvariant();
    private static string Join(IEnumerable<string> values) => string.Join(",", values);
    private static string NormalizeType(string value) =>
        string.Concat(value.Where(character => !char.IsWhiteSpace(character))).ToLowerInvariant();

    private static string NormalizeDefault(string? value, string storeType)
    {
        if (value is null)
            return "<null>";

        var normalized = string.Concat(value.Where(character => !char.IsWhiteSpace(character)));
        if ((storeType.StartsWith("decimal", StringComparison.OrdinalIgnoreCase) ||
             storeType.StartsWith("numeric", StringComparison.OrdinalIgnoreCase)) &&
            decimal.TryParse(normalized, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var decimalValue))
        {
            return decimalValue.ToString("G29", System.Globalization.CultureInfo.InvariantCulture);
        }

        if (Regex.IsMatch(normalized, "^(CURRENT_TIMESTAMP(?:\\([0-9]{1,6}\\))?|NOW\\(\\))$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant))
            return normalized.Equals("NOW()", StringComparison.OrdinalIgnoreCase)
                ? "CURRENT_TIMESTAMP"
                : normalized.ToUpperInvariant();

        // String defaults are deliberately compared case-sensitively.
        return normalized;
    }

    private static string Describe(TableSpec table) =>
        $"{table.Name}; charset={table.Charset ?? "implicit"}; collation={table.Collation ?? "implicit"}";

    private static string Describe(ColumnSpec column) =>
        $"{column.StoreType}; nullable={column.Nullable}; default={(column.HasDefault ? column.DefaultValue ?? "NULL" : "none")}; charset={column.Charset ?? "implicit"}; collation={column.Collation ?? "implicit"}";

    private static string Describe(IndexSpec index) =>
        $"{Join(index.Columns)}; unique={index.Unique}; primary={index.Primary}; uniqueConstraint={index.UniqueConstraint}";

    private static string Describe(ForeignKeySpec foreignKey) =>
        $"{Join(foreignKey.Columns)} -> {foreignKey.PrincipalTable}({Join(foreignKey.PrincipalColumns)}); delete={foreignKey.DeleteRule}; update={foreignKey.UpdateRule ?? "not specified by EF"}";
}
