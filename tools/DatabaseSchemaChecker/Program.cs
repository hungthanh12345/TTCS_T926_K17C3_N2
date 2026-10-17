using InternshipManagementApi.Data;
using Microsoft.EntityFrameworkCore;

namespace DatabaseSchemaChecker;

internal static class Program
{
    private const string ConnectionEnvironmentVariable = "TTCS_SCHEMA_CHECK_CONNECTION_STRING";
    private const string KnownSourceGapsFile = "known-source-gaps.json";

    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 1 && args[0].Equals("--self-test", StringComparison.OrdinalIgnoreCase))
            return OfflineTests.Run();

        if (args.Length == 1 && args[0].Equals("--inventory", StringComparison.OrdinalIgnoreCase))
            return PrintInventory();

        try
        {
            var options = Arguments.Parse(args);
            SchemaPolicy.ValidateTarget(options.Schema, options.Confirmation);

            var rawConnection = Environment.GetEnvironmentVariable(ConnectionEnvironmentVariable);
            if (string.IsNullOrWhiteSpace(rawConnection))
            {
                Console.Error.WriteLine($"BLOCKED: set {ConnectionEnvironmentVariable} from a protected local secret source.");
                return 3;
            }

            var connectionString = SchemaPolicy.ValidateConnection(rawConnection, options.Schema);
            var knownGaps = LoadKnownSourceGaps();
            using var context = SchemaInventoryReader.CreateContext();
            var expected = SchemaInventoryReader.Read(context);
            var sourceMigrations = context.Database.GetMigrations().ToArray();

            var (identity, actual, history) = await MySqlSchemaReader.ReadAsync(
                connectionString,
                options.Schema,
                CancellationToken.None);

            var differences = SchemaComparer.Compare(expected, actual);
            var migrations = MigrationClassifier.Classify(
                sourceMigrations,
                history,
                knownGaps.Select(gap => gap.MigrationId))
                .Select(status =>
                {
                    var knownGap = knownGaps.FirstOrDefault(gap => gap.MigrationId == status.MigrationId);
                    return knownGap is null ? status : status with
                    {
                        Note = status.PresentInHistory
                            ? knownGap.Reason + " The database history contains this ID and blocks upgrade."
                            : knownGap.Reason + " This ID is absent from this database history; no historical row is inferred."
                    };
                })
                .ToArray();

            PrintReport(identity, expected, actual, differences, migrations, options.ShowMatched);
            if (options.ExportCheckDetails)
            {
                var reportPath = CheckDifferenceReportWriter.WriteToLocalTemp(
                    options.Schema,
                    expected,
                    actual,
                    differences,
                    DateTimeOffset.UtcNow);
                Console.WriteLine($"CHECK difference details (local, CHECK-only): {reportPath}");
            }

            var schemaNeedsReview = differences.Any(item => item.Status != "MATCHED");
            var migrationNeedsReview = migrations.Any(item =>
                item.Status == "PENDING" || item.BlocksUpgrade);
            var historyTableInvalid = !identity.HistoryTableValid;
            return schemaNeedsReview || migrationNeedsReview || historyTableInvalid ? 2 : 0;
        }
        catch (InvalidOperationException exception)
        {
            Console.Error.WriteLine($"BLOCKED: {exception.Message}");
            return 3;
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine($"CHECK FAILED: {exception.GetType().Name}; diagnostic details suppressed.");
            return 4;
        }
    }

    private static int PrintInventory()
    {
        using var context = SchemaInventoryReader.CreateContext();
        var inventory = SchemaInventoryReader.Read(context);
        Console.WriteLine("EF model inventory only; no MySQL connection or database evidence was read.");
        Console.WriteLine("Expected model: " + Format(SchemaComparer.Counts(inventory)));
        Console.WriteLine("EF migration source IDs:");
        foreach (var id in context.Database.GetMigrations())
            Console.WriteLine($"  {id}");
        Console.WriteLine("Known source gaps:");
        foreach (var gap in LoadKnownSourceGaps())
            Console.WriteLine($"  SOURCE_MISSING {gap.MigrationId}: {gap.Reason}");
        return 0;
    }

    private static KnownSourceGap[] LoadKnownSourceGaps()
    {
        var path = Path.Combine(AppContext.BaseDirectory, KnownSourceGapsFile);
        if (!File.Exists(path))
            throw new InvalidOperationException("Known migration source-gap manifest is missing; check cannot proceed.");

        return System.Text.Json.JsonSerializer.Deserialize<KnownSourceGap[]>(
                   File.ReadAllText(path),
                   new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ??
               throw new InvalidOperationException("Known migration source-gap manifest is invalid.");
    }

    private static void PrintReport(
        MySqlIdentity identity,
        SchemaInventory expected,
        SchemaInventory actual,
        IReadOnlyList<SchemaDifference> differences,
        IReadOnlyList<MigrationStatus> migrations,
        bool showMatched)
    {
        var matched = SchemaComparer.CountStatus(differences, "MATCHED");
        var missing = SchemaComparer.CountStatus(differences, "MISSING");
        var different = SchemaComparer.CountStatus(differences, "DIFFERENT");
        var extra = SchemaComparer.CountStatus(differences, "EXTRA");
        var historyMatches = identity.HistoryTableValid && migrations.All(item =>
            item.Status == "APPLIED" || (item.Status == "SOURCE_MISSING" && !item.BlocksUpgrade));
        var schemaMatches = differences.All(item => item.Status == "MATCHED");

        Console.WriteLine("DATABASE SCHEMA CHECK — SELECT ONLY");
        Console.WriteLine($"Target: {identity.Database}");
        Console.WriteLine($"MySQL: {identity.ServerVersion}");
        Console.WriteLine($"Schema defaults: {identity.DefaultCharset} / {identity.DefaultCollation}");
        Console.WriteLine($"EF model: {Format(SchemaComparer.Counts(expected))}");
        Console.WriteLine($"Database (EF business objects): {Format(SchemaComparer.Counts(actual))}");
        Console.WriteLine($"__EFMigrationsHistory: {(identity.HistoryTableValid ? "MATCHED" : identity.HistoryTablePresent ? "DIFFERENT" : "MISSING")}");
        Console.WriteLine($"MATCHED: {Format(matched)}");
        Console.WriteLine($"MISSING: {Format(missing)}");
        Console.WriteLine($"DIFFERENT: {Format(different)}");
        Console.WriteLine($"EXTRA: {Format(extra)}");
        Console.WriteLine($"STRUCTURE: {(schemaMatches ? "MATCHED from live MySQL metadata" : "REVIEW REQUIRED")}");
        Console.WriteLine($"MIGRATION HISTORY: {(historyMatches ? "SOURCE-BACKED ENTRIES MATCH; see gaps below" : "REVIEW REQUIRED")}");
        Console.WriteLine("Migration history:");

        foreach (var migration in migrations)
        {
            var historyState = migration.PresentInHistory ? "history=present" : "history=absent";
            var sourceState = migration.SourceAvailable ? "source=available" : "source=missing";
            var blocker = migration.BlocksUpgrade ? "upgrade-blocker=yes" : "upgrade-blocker=no";
            Console.WriteLine($"  {migration.Status} {migration.MigrationId} ({sourceState}, {historyState}, {blocker})");
            if (migration.Note is not null)
                Console.WriteLine($"    {migration.Note}");
        }

        Console.WriteLine("Schema differences:");
        var entries = showMatched
            ? differences
            : differences.Where(item => item.Status != "MATCHED").ToArray();

        if (entries.Count == 0)
        {
            Console.WriteLine("  No schema differences.");
        }
        else
        {
            foreach (var item in entries)
            {
                var expectedText = item.Expected is null ? "" : $" expected=[{item.Expected}]";
                var actualText = item.Actual is null ? "" : $" actual=[{item.Actual}]";
                var note = item.Note is null ? "" : $" note=[{item.Note}]";
                Console.WriteLine($"  {item.Status} {item.Kind} {item.Table}.{item.Name}{expectedText}{actualText}{note}");
            }
        }

        if (migrations.Any(item => item.Status == "SOURCE_MISSING"))
            Console.WriteLine("SOURCE_MISSING remains visible; no migration history or schema was changed.");
    }

    private static string Format(InventoryCounts counts) =>
        string.Format(System.Globalization.CultureInfo.InvariantCulture,
            "{0} tables, {1} columns, {2} FKs, {3} indexes, {4} unique constraints, {5} CHECKs",
            counts.Tables, counts.Columns, counts.ForeignKeys, counts.Indexes, counts.UniqueConstraints, counts.Checks);

    private sealed record KnownSourceGap(string MigrationId, string Reason);
    private sealed record CheckOptions(string Schema, string Confirmation, bool ShowMatched, bool ExportCheckDetails);

    private static class Arguments
    {
        public static CheckOptions Parse(string[] args)
        {
            string? schema = null;
            string? confirmation = null;
            var showMatched = false;
            var exportCheckDetails = false;
            for (var index = 0; index < args.Length; index++)
            {
                switch (args[index])
                {
                    case "--schema" when index + 1 < args.Length:
                        schema = args[++index];
                        break;
                    case "--confirm-test-schema" when index + 1 < args.Length:
                        confirmation = args[++index];
                        break;
                    case "--show-matched":
                        showMatched = true;
                        break;
                    case "--export-check-details":
                        exportCheckDetails = true;
                        break;
                    default:
                        throw new InvalidOperationException(
                            "Usage: --schema <TestSchema> --confirm-test-schema <same-schema> [--show-matched] [--export-check-details].");
                }
            }

            if (schema is null || confirmation is null)
                throw new InvalidOperationException(
                    "Usage: --schema <TestSchema> --confirm-test-schema <same-schema> [--show-matched] [--export-check-details].");
            return new CheckOptions(schema, confirmation, showMatched, exportCheckDetails);
        }
    }
}
