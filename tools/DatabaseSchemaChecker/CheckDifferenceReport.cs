using System.Text;
using System.Text.Json;

namespace DatabaseSchemaChecker;

internal sealed record CheckDifferenceReport(
    string ReportType,
    string Schema,
    DateTimeOffset GeneratedAtUtc,
    string ActualExpressionSource,
    string ExpressionEncodingNote,
    IReadOnlyList<CheckDifferenceReportItem> Differences);

internal sealed record CheckDifferenceReportItem(
    string Table,
    string ConstraintName,
    bool ExpectedEnforced,
    bool ActualEnforced,
    string ActualEnforcedValue,
    string ExpectedExpression,
    string ExpectedExpressionJsonStringLiteral,
    string ExpectedExpressionUtf8Base64,
    int ExpectedExpressionUtf8ByteCount,
    string ActualExpression,
    string ActualExpressionJsonStringLiteral,
    string ActualExpressionUtf8Base64,
    int ActualExpressionUtf8ByteCount);

internal static class CheckDifferenceReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string Build(
        string schema,
        SchemaInventory expected,
        SchemaInventory actual,
        IReadOnlyList<SchemaDifference> differences,
        DateTimeOffset generatedAtUtc)
    {
        var items = differences
            .Where(difference => difference.Kind == "Check" && difference.Status == "DIFFERENT")
            .Select(difference => BuildItem(expected, actual, difference))
            .ToArray();

        var report = new CheckDifferenceReport(
            "DatabaseSchemaChecker CHECK DIFFERENCES",
            schema,
            generatedAtUtc.ToUniversalTime(),
            "INFORMATION_SCHEMA.CHECK_CONSTRAINTS.CHECK_CLAUSE read by MySqlDataReader.GetString; before checker normalization.",
            "Expression fields are decoded JSON string values. The JsonStringLiteral fields show JSON escaping; the Base64 fields encode the exact UTF-8 bytes of the corresponding expression string, allowing lossless recovery and disambiguation of backslashes/quotes.",
            items);

        return JsonSerializer.Serialize(report, JsonOptions) + Environment.NewLine;
    }

    public static string WriteToLocalTemp(
        string schema,
        SchemaInventory expected,
        SchemaInventory actual,
        IReadOnlyList<SchemaDifference> differences,
        DateTimeOffset generatedAtUtc)
    {
        var contents = Build(schema, expected, actual, differences, generatedAtUtc);
        var reportDirectory = Path.Combine(Path.GetTempPath(), "DatabaseSchemaChecker");
        Directory.CreateDirectory(reportDirectory);

        var safeSchema = new string(schema.Where(character =>
            char.IsAsciiLetterOrDigit(character) || character is '_' or '-').ToArray());
        var fileName = $"check-differences-{safeSchema}-{generatedAtUtc.UtcDateTime:yyyyMMddTHHmmssfffZ}-{Guid.NewGuid():N}.txt";
        var path = Path.Combine(reportDirectory, fileName);
        using var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        using var writer = new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        writer.Write(contents);
        return path;
    }

    private static CheckDifferenceReportItem BuildItem(
        SchemaInventory expected,
        SchemaInventory actual,
        SchemaDifference difference)
    {
        var expectedCheck = expected.Checks.SingleOrDefault(check =>
            check.Table.Equals(difference.Table, StringComparison.OrdinalIgnoreCase) &&
            check.Name.Equals(difference.Name, StringComparison.OrdinalIgnoreCase));
        if (expectedCheck is null || difference.Actual is null)
            throw new InvalidOperationException(
                $"Could not map CHECK difference {difference.Table}.{difference.Name} to source and database expressions.");

        var normalizedExpected = SchemaComparer.NormalizeCheckExpression(expectedCheck.Expression);
        var candidate = actual.Checks.FirstOrDefault(check =>
            check.Table.Equals(expectedCheck.Table, StringComparison.OrdinalIgnoreCase) &&
            (check.Name.Equals(expectedCheck.Name, StringComparison.OrdinalIgnoreCase) ||
             SchemaComparer.NormalizeCheckExpression(check.Expression) == normalizedExpected));
        if (candidate is null || !candidate.Expression.Equals(difference.Actual, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"Could not verify the original INFORMATION_SCHEMA expression for CHECK {difference.Table}.{difference.Name}.");

        return new CheckDifferenceReportItem(
            expectedCheck.Table,
            expectedCheck.Name,
            expectedCheck.Enforced,
            candidate.Enforced,
            candidate.EnforcedValue ?? (candidate.Enforced ? "YES" : "NO"),
            expectedCheck.Expression,
            JsonSerializer.Serialize(expectedCheck.Expression),
            Convert.ToBase64String(Encoding.UTF8.GetBytes(expectedCheck.Expression)),
            Encoding.UTF8.GetByteCount(expectedCheck.Expression),
            candidate.Expression,
            JsonSerializer.Serialize(candidate.Expression),
            Convert.ToBase64String(Encoding.UTF8.GetBytes(candidate.Expression)),
            Encoding.UTF8.GetByteCount(candidate.Expression));
    }
}
