using System.Text.RegularExpressions;
using MySqlConnector;

namespace DatabaseSchemaChecker;

internal static class SchemaPolicy
{
    private static readonly HashSet<string> KnownTestSchemas =
        new(StringComparer.Ordinal)
        {
            "internship_restore_check",
            "ttcs_sprint4_test",
            "internship_sprint5_dev",
            "internship_sprint5_v2_verify_20261017"
        };

    public static bool IsAllowedTestSchema(string schema) =>
        !schema.StartsWith("internship_management", StringComparison.OrdinalIgnoreCase) &&
        KnownTestSchemas.Contains(schema);

    public static void ValidateTarget(string schema, string confirmation)
    {
        if (string.IsNullOrWhiteSpace(schema) ||
            !Regex.IsMatch(schema, "^[A-Za-z0-9_]{1,64}$", RegexOptions.CultureInvariant))
        {
            throw new InvalidOperationException("Schema identifier is invalid.");
        }

        if (schema.StartsWith("internship_management", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Production schema is never allowed by this checker.");

        if (!IsAllowedTestSchema(schema))
            throw new InvalidOperationException("Schema must exactly match an explicitly allowed Test schema.");

        if (!string.Equals(schema, confirmation, StringComparison.Ordinal))
            throw new InvalidOperationException("Exact Test schema confirmation is required.");
    }

    public static string ValidateConnection(string connectionString, string schema)
    {
        ValidateTarget(schema, schema);
        MySqlConnectionStringBuilder builder;
        try
        {
            builder = new MySqlConnectionStringBuilder(connectionString);
        }
        catch
        {
            throw new InvalidOperationException("Test connection settings could not be parsed; details suppressed.");
        }

        if (!builder.ContainsKey("Server") || !IsLoopbackHost(builder.Server))
            throw new InvalidOperationException("MySQL host must be explicitly loopback.");

        if (!string.Equals(builder.Database, schema, StringComparison.Ordinal))
            throw new InvalidOperationException("Connection schema does not match the confirmed Test schema.");

        if (schema.StartsWith("internship_management", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Production schema is never allowed by this checker.");

        if (string.IsNullOrWhiteSpace(builder.UserID) ||
            builder.UserID.Equals("root", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("A named non-root MySQL account is required.");
        }

        builder.PersistSecurityInfo = false;
        return builder.ConnectionString;
    }

    public static bool IsNonRootEffectiveUser(string? currentUser)
    {
        if (string.IsNullOrWhiteSpace(currentUser))
            return false;

        var userName = currentUser.Split('@', 2)[0].Trim((char)96, (char)39);
        return userName.Length > 0 && !userName.Equals("root", StringComparison.OrdinalIgnoreCase);
    }

    public static bool IsLoopbackHost(string? host) =>
        !string.IsNullOrWhiteSpace(host) &&
        (host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
         host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
         host.Equals("::1", StringComparison.OrdinalIgnoreCase) ||
         host.Equals("[::1]", StringComparison.OrdinalIgnoreCase));
}
