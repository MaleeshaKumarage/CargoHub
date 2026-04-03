using CargoHub.Domain.Companies;
using CargoHub.Infrastructure.Persistence;
using Xunit;
using CompanyEntity = CargoHub.Domain.Companies.Company;

namespace CargoHub.Tests.Integration;

public class DailyDigestSendLogRepositoryTests : IDisposable
{
    private readonly TestDbFixture _fixture;

    public DailyDigestSendLogRepositoryTests()
    {
        _fixture = new TestDbFixture();
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task TryClaimAsync_SecondCallForSameSlot_ReturnsFalse()
    {
        using var context = _fixture.CreateContext();
        var company = new CompanyEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = "digest-comp",
            Name = "Digest Co",
            BusinessId = "1111111-1",
            CustomerId = "c1"
        };
        context.Companies.Add(company);
        await context.SaveChangesAsync();

        var repo = new DailyDigestSendLogRepository(context);
        var date = new DateOnly(2025, 6, 1);
        var first = await repo.TryClaimAsync(company.Id, date, "UTC", default);
        var second = await repo.TryClaimAsync(company.Id, date, "UTC", default);

        Assert.True(first);
        Assert.False(second);
    }

    [Fact]
    public async Task TryClaimAsync_NullTimeZoneId_UsesEmptyString()
    {
        using var context = _fixture.CreateContext();
        var company = new CompanyEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = "digest-comp-2",
            Name = "Digest Co 2",
            BusinessId = "2222222-2",
            CustomerId = "c2"
        };
        context.Companies.Add(company);
        await context.SaveChangesAsync();

        var repo = new DailyDigestSendLogRepository(context);
        var date = new DateOnly(2025, 6, 2);
        var ok = await repo.TryClaimAsync(company.Id, date, null!, default);
        Assert.True(ok);

        var row = context.DailyDigestSendLogs.Single(x => x.CompanyId == company.Id);
        Assert.Equal(string.Empty, row.TimeZoneId);
    }

    [Fact]
    public async Task TryClaimAsync_TimeZoneLongerThan128_IsTruncated()
    {
        using var context = _fixture.CreateContext();
        var company = new CompanyEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = "digest-comp-3",
            Name = "Digest Co 3",
            BusinessId = "3333333-3",
            CustomerId = "c3"
        };
        context.Companies.Add(company);
        await context.SaveChangesAsync();

        var longTz = new string('x', 140);
        var repo = new DailyDigestSendLogRepository(context);
        var date = new DateOnly(2025, 6, 3);
        await repo.TryClaimAsync(company.Id, date, longTz, default);

        var row = context.DailyDigestSendLogs.Single(x => x.CompanyId == company.Id);
        Assert.Equal(128, row.TimeZoneId.Length);
        Assert.Equal(longTz[..128], row.TimeZoneId);
    }
}
