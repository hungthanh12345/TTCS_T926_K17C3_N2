using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using MySqlConnector;

namespace InternshipManagementApi.Data;

public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddUserSecrets(typeof(AppDbContext).Assembly, optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connectionString = ResolveConnectionString(configuration);
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;

        return new AppDbContext(options);
    }

    private static string ResolveConnectionString(IConfiguration configuration)
    {
        var raw = Environment.GetEnvironmentVariable("DATABASE_URL")
            ?? Environment.GetEnvironmentVariable("MYSQL_URL")
            ?? configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException("Set DATABASE_URL, MYSQL_URL, or ConnectionStrings:DefaultConnection.");

        if (raw.StartsWith("mysql://", StringComparison.OrdinalIgnoreCase) ||
            raw.StartsWith("mysqls://", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(raw);
            var userInfo = uri.UserInfo.Split(':', 2);
            var builder = new MySqlConnectionStringBuilder
            {
                Server = uri.Host,
                Port = (uint)(uri.Port > 0 ? uri.Port : 3306),
                Database = uri.AbsolutePath.TrimStart('/'),
                UserID = userInfo.Length > 0 ? Uri.UnescapeDataString(userInfo[0]) : string.Empty,
                Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty,
                CharacterSet = "utf8mb4",
                SslMode = uri.Scheme.Equals("mysqls", StringComparison.OrdinalIgnoreCase)
                    ? MySqlSslMode.Required
                    : MySqlSslMode.Preferred,
                AllowPublicKeyRetrieval = true
            };
            return builder.ConnectionString;
        }

        var connectionBuilder = new MySqlConnectionStringBuilder(raw)
        {
            CharacterSet = "utf8mb4",
            AllowPublicKeyRetrieval = true
        };
        return connectionBuilder.ConnectionString;
    }
}
