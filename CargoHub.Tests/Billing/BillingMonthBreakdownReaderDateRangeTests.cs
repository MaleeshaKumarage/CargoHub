using CargoHub.Application.Billing.Admin;
using CargoHub.Domain.Companies;
using CargoHub.Infrastructure.Billing;
using CargoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CompanyEntity = CargoHub.Domain.Companies.Company;

namespace CargoHub.Tests.Billing;

public sealed class BillingMonthBreakdownReaderDateRangeTests : IDisposable
{
    private readonly TestDbFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task GetBreakdownForDateRangeAsync_ReturnsNull_WhenStartNotUtc()
    {
        using var ctx = _fixture.CreateContext();
        var regen = new NoOpBillingPeriodRegenerationService();
        var reader = new BillingMonthBreakdownReader(ctx, regen);
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "C",
            BusinessId = "b1",
            CompanyId = companyId.ToString("N")
        });
        await ctx.SaveChangesAsync();

        var start = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var end = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc);
        var r = await reader.GetBreakdownForDateRangeAsync(companyId, start, end, default);
        Assert.Null(r);
    }

    [Fact]
    public async Task GetBreakdownForDateRangeAsync_EmptyBookings_ReturnsZeros_AndNullPeriod()
    {
        using var ctx = _fixture.CreateContext();
        var regen = new NoOpBillingPeriodRegenerationService();
        var reader = new BillingMonthBreakdownReader(ctx, regen);
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "C",
            BusinessId = "b2",
            CompanyId = companyId.ToString("N")
        });
        await ctx.SaveChangesAsync();

        var start = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc);
        var r = await reader.GetBreakdownForDateRangeAsync(companyId, start, end, default);
        Assert.NotNull(r);
        Assert.Null(r!.BillingPeriodId);
        Assert.Equal(0, r.BillableBookingCount);
        Assert.Equal(0m, r.PayableTotal);
        Assert.Equal(0m, r.LedgerTotal);
        Assert.Equal(start, r.RangeStartUtc);
        Assert.Equal(end, r.RangeEndExclusiveUtc);
    }

    private sealed class NoOpBillingPeriodRegenerationService : IBillingPeriodRegenerationService
    {
        public Task RegenerateAsync(Guid companyBillingPeriodId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
