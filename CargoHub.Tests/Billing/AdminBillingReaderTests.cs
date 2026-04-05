using CargoHub.Application.Billing;
using CargoHub.Domain.Billing;
using CargoHub.Domain.Companies;
using CargoHub.Infrastructure.Billing;
using CargoHub.Infrastructure.Persistence;
using Xunit;
using CompanyEntity = CargoHub.Domain.Companies.Company;

namespace CargoHub.Tests.Billing;

public class AdminBillingReaderTests : IDisposable
{
    private readonly TestDbFixture _fixture;

    public AdminBillingReaderTests() => _fixture = new TestDbFixture();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task ListSubscriptionPlans_ReturnsOrderedByName()
    {
        using var ctx = _fixture.CreateContext();
        var planB = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Beta",
            Kind = SubscriptionPlanKind.Trial,
            Currency = "EUR",
            IsActive = true
        };
        var planA = new SubscriptionPlan
        {
            Id = Guid.NewGuid(),
            Name = "Alpha",
            Kind = SubscriptionPlanKind.PayPerBooking,
            Currency = "SEK",
            IsActive = false
        };
        ctx.SubscriptionPlans.AddRange(planB, planA);
        await ctx.SaveChangesAsync();

        var reader = new AdminBillingReader(ctx);
        var list = await reader.ListSubscriptionPlansAsync();
        Assert.Equal(2, list.Count);
        Assert.Equal("Alpha", list[0].Name);
        Assert.Equal("Beta", list[1].Name);
        Assert.Equal("PayPerBooking", list[0].Kind);
        Assert.False(list[0].IsActive);
    }

    [Fact]
    public async Task ListBillingPeriodsAndDetail_AggregatesPayableTotal()
    {
        using var ctx = _fixture.CreateContext();
        var planId = SubscriptionBillingConstants.DefaultTrialPlanId;
        var periodId = Guid.NewGuid();
        ctx.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "Trial",
            Kind = SubscriptionPlanKind.Trial,
            Currency = "EUR",
            IsActive = true
        });
        var pricingPeriodId = Guid.NewGuid();
        ctx.SubscriptionPlanPricingPeriods.Add(new SubscriptionPlanPricingPeriod
        {
            Id = pricingPeriodId,
            SubscriptionPlanId = planId,
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-1)
        });
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            CompanyId = "c1",
            Name = "Co",
            BusinessId = "1234567-8",
            CustomerId = "x",
            SubscriptionPlanId = planId
        });
        ctx.CompanyBillingPeriods.Add(new CompanyBillingPeriod
        {
            Id = periodId,
            CompanyId = companyId,
            YearUtc = 2026,
            MonthUtc = 3,
            Currency = "EUR",
            Status = CompanyBillingPeriodStatus.Open
        });
        ctx.BillingLineItems.Add(new BillingLineItem
        {
            Id = Guid.NewGuid(),
            CompanyBillingPeriodId = periodId,
            LineType = BillingLineType.PerBooking,
            Amount = 10m,
            Currency = "EUR",
            SubscriptionPlanId = planId,
            SubscriptionPlanPricingPeriodId = pricingPeriodId,
            CreatedAtUtc = DateTime.UtcNow,
            ExcludedFromInvoice = false
        });
        ctx.BillingLineItems.Add(new BillingLineItem
        {
            Id = Guid.NewGuid(),
            CompanyBillingPeriodId = periodId,
            LineType = BillingLineType.Adjustment,
            Amount = 5m,
            Currency = "EUR",
            SubscriptionPlanId = planId,
            SubscriptionPlanPricingPeriodId = pricingPeriodId,
            CreatedAtUtc = DateTime.UtcNow,
            ExcludedFromInvoice = true
        });
        await ctx.SaveChangesAsync();

        var reader = new AdminBillingReader(ctx);
        var periods = await reader.ListBillingPeriodsForCompanyAsync(companyId);
        Assert.Single(periods);
        Assert.Equal(2026, periods[0].YearUtc);
        Assert.Equal(3, periods[0].MonthUtc);
        Assert.Equal(2, periods[0].LineItemCount);
        Assert.Equal(10m, periods[0].PayableTotal);

        var detail = await reader.GetBillingPeriodDetailAsync(periodId);
        Assert.NotNull(detail);
        Assert.Equal(10m, detail!.PayableTotal);
        Assert.Equal(2, detail.LineItems.Count);
        Assert.Contains(detail.LineItems, l => l.LineType == "PerBooking" && l.Amount == 10m);
    }

    [Fact]
    public async Task GetBillingPeriodDetail_UnknownId_ReturnsNull()
    {
        using var ctx = _fixture.CreateContext();
        var reader = new AdminBillingReader(ctx);
        Assert.Null(await reader.GetBillingPeriodDetailAsync(Guid.NewGuid()));
    }
}
