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
}
