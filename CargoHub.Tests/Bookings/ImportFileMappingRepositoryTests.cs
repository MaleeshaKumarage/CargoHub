using CargoHub.Application.Bookings;
using CargoHub.Domain.Companies;
using CargoHub.Infrastructure.Persistence;
using CompanyEntity = CargoHub.Domain.Companies.Company;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CargoHub.Tests.Bookings;

public class ImportFileMappingRepositoryTests
{
    [Fact]
    public async Task UpsertThenGet_ReturnsColumnMap()
    {
        await using var ctx = CreateContext();
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            CompanyId = "test-co",
            Counter = 1,
        });
        await ctx.SaveChangesAsync();

        var repo = new ImportFileMappingRepository(ctx);
        var map = new Dictionary<string, string?>(StringComparer.Ordinal) { ["ReferenceNumber"] = "Ref" };
        await repo.UpsertAsync(companyId, "report.csv", "A\u001FB", map);

        var got = await repo.GetColumnMapAsync(companyId, "report.csv", "A\u001FB");
        Assert.NotNull(got);
        Assert.Equal("Ref", got["ReferenceNumber"]);
    }

    [Fact]
    public async Task UpsertTwice_OverwritesJson()
    {
        await using var ctx = CreateContext();
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity { Id = companyId, CompanyId = "c2", Counter = 1 });
        await ctx.SaveChangesAsync();
        var repo = new ImportFileMappingRepository(ctx);
        await repo.UpsertAsync(companyId, "f.csv", "x", new Dictionary<string, string?> { ["A"] = "1" });
        await repo.UpsertAsync(companyId, "f.csv", "x", new Dictionary<string, string?> { ["A"] = "2" });
        var got = await repo.GetColumnMapAsync(companyId, "f.csv", "x");
        Assert.Equal("2", got!["A"]);
    }

    [Fact]
    public async Task GetColumnMapAsync_ReturnsNull_WhenNoRow()
    {
        await using var ctx = CreateContext();
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity { Id = companyId, CompanyId = "c3", Counter = 1 });
        await ctx.SaveChangesAsync();
        var repo = new ImportFileMappingRepository(ctx);
        Assert.Null(await repo.GetColumnMapAsync(companyId, "missing.csv", "sig"));
    }

    [Fact]
    public async Task GetColumnMapAsync_ReturnsNull_WhenColumnMapJsonBlank()
    {
        await using var ctx = CreateContext();
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity { Id = companyId, CompanyId = "c4", Counter = 1 });
        ctx.BookingImportFileMappings.Add(new BookingImportFileMapping
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            FileNameKey = "a.csv",
            HeaderSignature = "h",
            ColumnMapJson = "   ",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });
        await ctx.SaveChangesAsync();
        var repo = new ImportFileMappingRepository(ctx);
        Assert.Null(await repo.GetColumnMapAsync(companyId, "a.csv", "h"));
    }

    private static ApplicationDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        var ctx = new ApplicationDbContext(options);
        ctx.Database.EnsureCreated();
        return ctx;
    }
}
