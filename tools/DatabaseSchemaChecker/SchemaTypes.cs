namespace DatabaseSchemaChecker;

internal sealed record ColumnSpec(
    string Table,
    string Name,
    string StoreType,
    bool Nullable,
    bool HasDefault,
    string? DefaultValue,
    string? Charset,
    string? Collation);

internal sealed record IndexSpec(
    string Table,
    string Name,
    IReadOnlyList<string> Columns,
    bool Unique,
    bool Primary = false,
    bool UniqueConstraint = false);

internal sealed record ForeignKeySpec(
    string Table,
    string Name,
    IReadOnlyList<string> Columns,
    string PrincipalTable,
    IReadOnlyList<string> PrincipalColumns,
    string DeleteRule,
    string? UpdateRule);

internal sealed record CheckSpec(string Table, string Name, string Expression, bool Enforced, string? EnforcedValue = null);

internal sealed record TableSpec(
    string Name,
    string? Charset,
    string? Collation,
    IReadOnlyList<ColumnSpec> Columns,
    IReadOnlyList<string> PrimaryKeyColumns);

internal sealed record SchemaInventory(
    IReadOnlyList<TableSpec> Tables,
    IReadOnlyList<IndexSpec> Indexes,
    IReadOnlyList<ForeignKeySpec> ForeignKeys,
    IReadOnlyList<CheckSpec> Checks);

internal sealed record SchemaDifference(
    string Kind,
    string Table,
    string Name,
    string Status,
    string? Expected,
    string? Actual,
    string? Note = null);

internal sealed record MigrationStatus(
    string MigrationId,
    string Status,
    bool SourceAvailable,
    bool PresentInHistory,
    bool BlocksUpgrade,
    string? Note = null);

internal sealed record InventoryCounts(
    int Tables,
    int Columns,
    int ForeignKeys,
    int Indexes,
    int UniqueConstraints,
    int Checks);

internal sealed record SchemaCheckReport(
    string Schema,
    string MySqlVersion,
    string SchemaDefaultCharset,
    string SchemaDefaultCollation,
    InventoryCounts Expected,
    InventoryCounts Actual,
    InventoryCounts Matched,
    InventoryCounts Missing,
    InventoryCounts Different,
    InventoryCounts Extra,
    IReadOnlyList<SchemaDifference> Differences,
    IReadOnlyList<MigrationStatus> Migrations);
