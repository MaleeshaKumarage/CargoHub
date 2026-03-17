using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CargoHub.Infrastructure.Persistence;

/// <summary>
/// Used by EF Core tools at design time (e.g. dotnet ef migrations add).
/// Ensures migrations work without running the API.
/// </summary>
public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Load .env from repo root so migrations use same config as API
        TryLoadEnv();

        // Must match API DefaultConnection (from .env or appsettings) so migrations apply to the same DB.
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5433;Database=portal;Username=postgres;Password=postgres";

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsAssembly("CargoHub.Infrastructure"));

        return new ApplicationDbContext(optionsBuilder.Options);
    }

    private static void TryLoadEnv()
    {
        var dir = Directory.GetCurrentDirectory();
        var paths = new[] { Path.Combine(dir, ".env"), Path.Combine(dir, "..", ".env"), Path.Combine(dir, "..", "..", ".env") };
        foreach (var p in paths)
        {
            var resolved = Path.GetFullPath(p);
            if (File.Exists(resolved))
            {
                Env.Load(resolved);
                return;
            }
        }
    }
}
