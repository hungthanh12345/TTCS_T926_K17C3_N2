using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace InternshipManagementApi.Data;

/// <summary>
/// Creates the EF model for migration tooling without running Program.cs,
/// auto-detecting a server version, or opening a database connection.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseMySql(
                "Server=127.0.0.1;Port=1;Database=design_time_only;User ID=none;Password=none;Connection Timeout=1",
                new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;

        return new AppDbContext(options);
    }
}
