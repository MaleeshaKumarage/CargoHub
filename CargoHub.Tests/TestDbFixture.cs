using Microsoft.EntityFrameworkCore;
using CargoHub.Infrastructure.Persistence;

namespace CargoHub.Tests;

/// <summary>
/// Provides an in-memory test database using the same ApplicationDbContext as the real app.
/// Uses a unique database name per creation so tests do not share data. Same server as dev: use one logical "test db" per run.
/// See README for using a real PostgreSQL test database on the same server (optional).
/// </summary>
public sealed class TestDbFixture : IDisposable
{
    private readonly string _databaseName;

    public TestDbFixture(string? databaseName = null)
    {
        _databaseName = databaseName ?? Guid.NewGuid().ToString("N");
    }

    public DbContextOptions<ApplicationDbContext> CreateOptions()
    {
        var builder = new DbContextOptionsBuilder<ApplicationDbContext>();
        builder.UseInMemoryDatabase(_databaseName);
        return builder.Options;
    }

    /// <summary>Create a new context and ensure the in-memory database is created.</summary>
    public ApplicationDbContext CreateContext()
    {
        var options = CreateOptions();
        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public void Dispose() => GC.SuppressFinalize(this);
}
