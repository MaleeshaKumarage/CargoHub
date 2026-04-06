using CargoHub.Application.Billing.Admin;
using CargoHub.Domain.Billing;
using CargoHub.Domain.Companies;
using CargoHub.Infrastructure.Billing;
using CargoHub.Infrastructure.Persistence;
using Xunit;
using CompanyEntity = CargoHub.Domain.Companies.Company;

namespace CargoHub.Tests.Billing;

public class AdminPlatformEarningsReaderTests : IDisposable
{
    private readonly TestDbFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task GetMonthlyTotalsAsync_FillsZerosAndSumsPayableEurLines()
    {
        using var ctx = _fixture.CreateContext();
        var planId = Guid.NewGuid();
        var pricingId = Guid.NewGuid();
        ctx.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "P1",
            Kind = SubscriptionPlanKind.PayPerBooking,
            Currency = "EUR",
            IsActive = true,
        });
        ctx.SubscriptionPlanPricingPeriods.Add(new SubscriptionPlanPricingPeriod
        {
            Id = pricingId,
            SubscriptionPlanId = planId,
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-30),
        });

        var c1 = Guid.NewGuid();
        var c2 = Guid.NewGuid();
        ctx.Companies.AddRange(
            new CompanyEntity { Id = c1, CompanyId = "a", Name = "A Co", BusinessId = "1", CustomerId = "u1", SubscriptionPlanId = planId },
            new CompanyEntity { Id = c2, CompanyId = "b", Name = "B Co", BusinessId = "2", CustomerId = "u2", SubscriptionPlanId = planId });

        var now = DateTime.UtcNow;
        var end = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var older = end.AddMonths(-2);
        var newer = end.AddMonths(-1);

        var pOld = Guid.NewGuid();
        var pNew = Guid.NewGuid();
        ctx.CompanyBillingPeriods.AddRange(
            new CompanyBillingPeriod
            {
                Id = pOld,
                CompanyId = c1,
                YearUtc = older.Year,
                MonthUtc = older.Month,
                Currency = "EUR",
                Status = CompanyBillingPeriodStatus.Open,
            },
            new CompanyBillingPeriod
            {
                Id = pNew,
                CompanyId = c2,
                YearUtc = newer.Year,
                MonthUtc = newer.Month,
                Currency = "EUR",
                Status = CompanyBillingPeriodStatus.Open,
            });

        ctx.BillingLineItems.AddRange(
            new BillingLineItem
            {
                Id = Guid.NewGuid(),
                CompanyBillingPeriodId = pOld,
                LineType = BillingLineType.PerBooking,
                Amount = 100m,
                Currency = "EUR",
                SubscriptionPlanId = planId,
                SubscriptionPlanPricingPeriodId = pricingId,
                CreatedAtUtc = DateTime.UtcNow,
                ExcludedFromInvoice = false,
            },
            new BillingLineItem
            {
                Id = Guid.NewGuid(),
                CompanyBillingPeriodId = pOld,
                LineType = BillingLineType.Adjustment,
                Amount = 50m,
                Currency = "EUR",
                SubscriptionPlanId = planId,
                SubscriptionPlanPricingPeriodId = pricingId,
                CreatedAtUtc = DateTime.UtcNow,
                ExcludedFromInvoice = true,
            },
            new BillingLineItem
            {
                Id = Guid.NewGuid(),
                CompanyBillingPeriodId = pNew,
                LineType = BillingLineType.PerBooking,
                Amount = 40m,
                Currency = "EUR",
                SubscriptionPlanId = planId,
                SubscriptionPlanPricingPeriodId = pricingId,
                CreatedAtUtc = DateTime.UtcNow,
                ExcludedFromInvoice = false,
            });

        await ctx.SaveChangesAsync();

        var reader = new AdminPlatformEarningsReader(ctx);
        var months = await reader.GetMonthlyTotalsAsync(3);

        Assert.Contains(months, x => x.YearUtc == older.Year && x.MonthUtc == older.Month && x.TotalEur == 100m);
        Assert.Contains(months, x => x.YearUtc == newer.Year && x.MonthUtc == newer.Month && x.TotalEur == 40m);
        Assert.All(months, m => Assert.True(m.TotalEur >= 0m));
    }

    [Fact]
    public async Task GetByCompanyForMonthAsync_GroupsByCompanyDescending()
    {
        using var ctx = _fixture.CreateContext();
        var planId = Guid.NewGuid();
        var pricingId = Guid.NewGuid();
        ctx.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "P1",
            Kind = SubscriptionPlanKind.PayPerBooking,
            Currency = "EUR",
            IsActive = true,
        });
        ctx.SubscriptionPlanPricingPeriods.Add(new SubscriptionPlanPricingPeriod
        {
            Id = pricingId,
            SubscriptionPlanId = planId,
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
        });

        var c1 = Guid.NewGuid();
        var c2 = Guid.NewGuid();
        ctx.Companies.AddRange(
            new CompanyEntity { Id = c1, CompanyId = "a", Name = "Small", BusinessId = "1", CustomerId = "u1", SubscriptionPlanId = planId },
            new CompanyEntity { Id = c2, CompanyId = "b", Name = "Big", BusinessId = "2", CustomerId = "u2", SubscriptionPlanId = planId });

        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        ctx.CompanyBillingPeriods.AddRange(
            new CompanyBillingPeriod
            {
                Id = p1,
                CompanyId = c1,
                YearUtc = 2025,
                MonthUtc = 6,
                Currency = "EUR",
                Status = CompanyBillingPeriodStatus.Closed,
            },
            new CompanyBillingPeriod
            {
                Id = p2,
                CompanyId = c2,
                YearUtc = 2025,
                MonthUtc = 6,
                Currency = "EUR",
                Status = CompanyBillingPeriodStatus.Closed,
            });

        ctx.BillingLineItems.AddRange(
            new BillingLineItem
            {
                Id = Guid.NewGuid(),
                CompanyBillingPeriodId = p1,
                LineType = BillingLineType.PerBooking,
                Amount = 10m,
                Currency = "EUR",
                SubscriptionPlanId = planId,
                SubscriptionPlanPricingPeriodId = pricingId,
                CreatedAtUtc = DateTime.UtcNow,
                ExcludedFromInvoice = false,
            },
            new BillingLineItem
            {
                Id = Guid.NewGuid(),
                CompanyBillingPeriodId = p2,
                LineType = BillingLineType.PerBooking,
                Amount = 90m,
                Currency = "EUR",
                SubscriptionPlanId = planId,
                SubscriptionPlanPricingPeriodId = pricingId,
                CreatedAtUtc = DateTime.UtcNow,
                ExcludedFromInvoice = false,
            });

        await ctx.SaveChangesAsync();

        var reader = new AdminPlatformEarningsReader(ctx);
        var list = await reader.GetByCompanyForMonthAsync(2025, 6);

        Assert.Equal(2, list.Count);
        Assert.Equal("Big", list[0].CompanyName);
        Assert.Equal(90m, list[0].AmountEur);
        Assert.Equal("Small", list[1].CompanyName);
    }

    [Fact]
    public async Task GetBySubscriptionForMonthAsync_PercentagesSumTo100()
    {
        using var ctx = _fixture.CreateContext();
        var planA = Guid.NewGuid();
        var planB = Guid.NewGuid();
        var pricingA = Guid.NewGuid();
        var pricingB = Guid.NewGuid();
        ctx.SubscriptionPlans.AddRange(
            new SubscriptionPlan { Id = planA, Name = "Plan A", Kind = SubscriptionPlanKind.MonthlyBundle, Currency = "EUR", IsActive = true },
            new SubscriptionPlan { Id = planB, Name = "Plan B", Kind = SubscriptionPlanKind.PayPerBooking, Currency = "EUR", IsActive = true });
        ctx.SubscriptionPlanPricingPeriods.AddRange(
            new SubscriptionPlanPricingPeriod { Id = pricingA, SubscriptionPlanId = planA, EffectiveFromUtc = DateTime.UtcNow.AddDays(-1) },
            new SubscriptionPlanPricingPeriod { Id = pricingB, SubscriptionPlanId = planB, EffectiveFromUtc = DateTime.UtcNow.AddDays(-1) });

        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            CompanyId = "c",
            Name = "Co",
            BusinessId = "9",
            CustomerId = "u",
            SubscriptionPlanId = planA,
        });

        var periodId = Guid.NewGuid();
        ctx.CompanyBillingPeriods.Add(new CompanyBillingPeriod
        {
            Id = periodId,
            CompanyId = companyId,
            YearUtc = 2024,
            MonthUtc = 8,
            Currency = "EUR",
            Status = CompanyBillingPeriodStatus.Open,
        });

        ctx.BillingLineItems.AddRange(
            new BillingLineItem
            {
                Id = Guid.NewGuid(),
                CompanyBillingPeriodId = periodId,
                LineType = BillingLineType.MonthlyBase,
                Amount = 25m,
                Currency = "EUR",
                SubscriptionPlanId = planA,
                SubscriptionPlanPricingPeriodId = pricingA,
                CreatedAtUtc = DateTime.UtcNow,
                ExcludedFromInvoice = false,
            },
            new BillingLineItem
            {
                Id = Guid.NewGuid(),
                CompanyBillingPeriodId = periodId,
                LineType = BillingLineType.PerBooking,
                Amount = 75m,
                Currency = "EUR",
                SubscriptionPlanId = planB,
                SubscriptionPlanPricingPeriodId = pricingB,
                CreatedAtUtc = DateTime.UtcNow,
                ExcludedFromInvoice = false,
            });

        await ctx.SaveChangesAsync();

        var reader = new AdminPlatformEarningsReader(ctx);
        var list = await reader.GetBySubscriptionForMonthAsync(2024, 8);

        Assert.Equal(2, list.Count);
        Assert.Equal(75m, list[0].AmountEur);
        Assert.Equal("Plan B", list[0].PlanName);
        var sumPct = list.Sum(x => x.Percent);
        Assert.InRange(sumPct, 99.9m, 100.1m);
    }

    [Fact]
    public async Task GetMonthlyTotalsAsync_ClampsMonthsToRange()
    {
        using var ctx = _fixture.CreateContext();
        var reader = new AdminPlatformEarningsReader(ctx);
        var many = await reader.GetMonthlyTotalsAsync(500);
        Assert.Equal(120, many.Count);
        var one = await reader.GetMonthlyTotalsAsync(0);
        Assert.Single(one);
    }

    [Fact]
    public async Task GetMonthlyTotalsAsync_SkipsNonEurLines()
    {
        using var ctx = _fixture.CreateContext();
        var planId = Guid.NewGuid();
        var pricingId = Guid.NewGuid();
        ctx.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "P1",
            Kind = SubscriptionPlanKind.PayPerBooking,
            Currency = "EUR",
            IsActive = true,
        });
        ctx.SubscriptionPlanPricingPeriods.Add(new SubscriptionPlanPricingPeriod
        {
            Id = pricingId,
            SubscriptionPlanId = planId,
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
        });

        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            CompanyId = "c",
            Name = "Co",
            BusinessId = "1",
            CustomerId = "u",
            SubscriptionPlanId = planId,
        });

        var now = DateTime.UtcNow;
        var end = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodId = Guid.NewGuid();
        ctx.CompanyBillingPeriods.Add(new CompanyBillingPeriod
        {
            Id = periodId,
            CompanyId = companyId,
            YearUtc = end.Year,
            MonthUtc = end.Month,
            Currency = "EUR",
            Status = CompanyBillingPeriodStatus.Open,
        });

        ctx.BillingLineItems.Add(new BillingLineItem
        {
            Id = Guid.NewGuid(),
            CompanyBillingPeriodId = periodId,
            LineType = BillingLineType.PerBooking,
            Amount = 99m,
            Currency = "USD",
            SubscriptionPlanId = planId,
            SubscriptionPlanPricingPeriodId = pricingId,
            CreatedAtUtc = DateTime.UtcNow,
            ExcludedFromInvoice = false,
        });

        await ctx.SaveChangesAsync();

        var reader = new AdminPlatformEarningsReader(ctx);
        var months = await reader.GetMonthlyTotalsAsync(1);
        Assert.Contains(months, x => x.YearUtc == end.Year && x.MonthUtc == end.Month && x.TotalEur == 0m);
    }

    [Fact]
    public async Task GetMonthlyTotalsAsync_IncludesCaseInsensitiveEurCurrencyCode()
    {
        using var ctx = _fixture.CreateContext();
        var planId = Guid.NewGuid();
        var pricingId = Guid.NewGuid();
        ctx.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "P1",
            Kind = SubscriptionPlanKind.PayPerBooking,
            Currency = "EUR",
            IsActive = true,
        });
        ctx.SubscriptionPlanPricingPeriods.Add(new SubscriptionPlanPricingPeriod
        {
            Id = pricingId,
            SubscriptionPlanId = planId,
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
        });

        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            CompanyId = "c",
            Name = "Co",
            BusinessId = "1",
            CustomerId = "u",
            SubscriptionPlanId = planId,
        });

        var now = DateTime.UtcNow;
        var end = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodId = Guid.NewGuid();
        ctx.CompanyBillingPeriods.Add(new CompanyBillingPeriod
        {
            Id = periodId,
            CompanyId = companyId,
            YearUtc = end.Year,
            MonthUtc = end.Month,
            Currency = "EUR",
            Status = CompanyBillingPeriodStatus.Open,
        });

        ctx.BillingLineItems.Add(new BillingLineItem
        {
            Id = Guid.NewGuid(),
            CompanyBillingPeriodId = periodId,
            LineType = BillingLineType.PerBooking,
            Amount = 12.5m,
            Currency = "eur",
            SubscriptionPlanId = planId,
            SubscriptionPlanPricingPeriodId = pricingId,
            CreatedAtUtc = DateTime.UtcNow,
            ExcludedFromInvoice = false,
        });

        await ctx.SaveChangesAsync();

        var reader = new AdminPlatformEarningsReader(ctx);
        var months = await reader.GetMonthlyTotalsAsync(1);
        Assert.Contains(months, x => x.YearUtc == end.Year && x.MonthUtc == end.Month && x.TotalEur == 12.5m);
    }

    [Fact]
    public async Task GetBySubscriptionForMonthAsync_ReturnsEmpty_WhenNoLines()
    {
        using var ctx = _fixture.CreateContext();
        var reader = new AdminPlatformEarningsReader(ctx);
        var list = await reader.GetBySubscriptionForMonthAsync(2020, 1);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetBySubscriptionForMonthAsync_ReturnsEmpty_WhenOnlyNonPositiveTotal()
    {
        using var ctx = _fixture.CreateContext();
        var planId = Guid.NewGuid();
        var pricingId = Guid.NewGuid();
        ctx.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "P1",
            Kind = SubscriptionPlanKind.PayPerBooking,
            Currency = "EUR",
            IsActive = true,
        });
        ctx.SubscriptionPlanPricingPeriods.Add(new SubscriptionPlanPricingPeriod
        {
            Id = pricingId,
            SubscriptionPlanId = planId,
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
        });

        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            CompanyId = "c",
            Name = "Co",
            BusinessId = "1",
            CustomerId = "u",
            SubscriptionPlanId = planId,
        });

        var periodId = Guid.NewGuid();
        ctx.CompanyBillingPeriods.Add(new CompanyBillingPeriod
        {
            Id = periodId,
            CompanyId = companyId,
            YearUtc = 2022,
            MonthUtc = 3,
            Currency = "EUR",
            Status = CompanyBillingPeriodStatus.Open,
        });

        ctx.BillingLineItems.Add(new BillingLineItem
        {
            Id = Guid.NewGuid(),
            CompanyBillingPeriodId = periodId,
            LineType = BillingLineType.Adjustment,
            Amount = -10m,
            Currency = "EUR",
            SubscriptionPlanId = planId,
            SubscriptionPlanPricingPeriodId = pricingId,
            CreatedAtUtc = DateTime.UtcNow,
            ExcludedFromInvoice = false,
        });
        ctx.BillingLineItems.Add(new BillingLineItem
        {
            Id = Guid.NewGuid(),
            CompanyBillingPeriodId = periodId,
            LineType = BillingLineType.Adjustment,
            Amount = 5m,
            Currency = "EUR",
            SubscriptionPlanId = planId,
            SubscriptionPlanPricingPeriodId = pricingId,
            CreatedAtUtc = DateTime.UtcNow,
            ExcludedFromInvoice = false,
        });

        await ctx.SaveChangesAsync();

        var reader = new AdminPlatformEarningsReader(ctx);
        var list = await reader.GetBySubscriptionForMonthAsync(2022, 3);
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetBySubscriptionForMonthAsync_UsesUnknownPlan_WhenPlanRowMissing()
    {
        using var ctx = _fixture.CreateContext();
        var orphanPlanId = Guid.NewGuid();
        var knownPlanId = Guid.NewGuid();
        var pricingId = Guid.NewGuid();
        ctx.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = knownPlanId,
            Name = "Other",
            Kind = SubscriptionPlanKind.PayPerBooking,
            Currency = "EUR",
            IsActive = true,
        });
        ctx.SubscriptionPlanPricingPeriods.Add(new SubscriptionPlanPricingPeriod
        {
            Id = pricingId,
            SubscriptionPlanId = knownPlanId,
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
        });

        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            CompanyId = "c",
            Name = "Co",
            BusinessId = "1",
            CustomerId = "u",
            SubscriptionPlanId = knownPlanId,
        });

        var periodId = Guid.NewGuid();
        ctx.CompanyBillingPeriods.Add(new CompanyBillingPeriod
        {
            Id = periodId,
            CompanyId = companyId,
            YearUtc = 2023,
            MonthUtc = 5,
            Currency = "EUR",
            Status = CompanyBillingPeriodStatus.Open,
        });

        ctx.BillingLineItems.Add(new BillingLineItem
        {
            Id = Guid.NewGuid(),
            CompanyBillingPeriodId = periodId,
            LineType = BillingLineType.PerBooking,
            Amount = 50m,
            Currency = "EUR",
            SubscriptionPlanId = orphanPlanId,
            SubscriptionPlanPricingPeriodId = pricingId,
            CreatedAtUtc = DateTime.UtcNow,
            ExcludedFromInvoice = false,
        });

        await ctx.SaveChangesAsync();

        var reader = new AdminPlatformEarningsReader(ctx);
        var list = await reader.GetBySubscriptionForMonthAsync(2023, 5);
        Assert.Single(list);
        Assert.Equal("Unknown plan", list[0].PlanName);
        Assert.Equal(100m, list[0].Percent);
    }

    [Fact]
    public async Task GetSeriesAsync_Yesterday_BucketsByCreatedAtUtc()
    {
        using var ctx = _fixture.CreateContext();
        var planId = Guid.NewGuid();
        var pricingId = Guid.NewGuid();
        ctx.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "P1",
            Kind = SubscriptionPlanKind.PayPerBooking,
            Currency = "EUR",
            IsActive = true,
        });
        ctx.SubscriptionPlanPricingPeriods.Add(new SubscriptionPlanPricingPeriod
        {
            Id = pricingId,
            SubscriptionPlanId = planId,
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-30),
        });

        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            CompanyId = "c",
            Name = "Co",
            BusinessId = "1",
            CustomerId = "u",
            SubscriptionPlanId = planId,
        });

        var periodId = Guid.NewGuid();
        ctx.CompanyBillingPeriods.Add(new CompanyBillingPeriod
        {
            Id = periodId,
            CompanyId = companyId,
            YearUtc = DateTime.UtcNow.Year,
            MonthUtc = DateTime.UtcNow.Month,
            Currency = "EUR",
            Status = CompanyBillingPeriodStatus.Open,
        });

        var yesterdayNoon = DateTime.UtcNow.Date.AddDays(-1).AddHours(12);
        ctx.BillingLineItems.Add(new BillingLineItem
        {
            Id = Guid.NewGuid(),
            CompanyBillingPeriodId = periodId,
            LineType = BillingLineType.PerBooking,
            Amount = 33m,
            Currency = "EUR",
            SubscriptionPlanId = planId,
            SubscriptionPlanPricingPeriodId = pricingId,
            CreatedAtUtc = yesterdayNoon,
            ExcludedFromInvoice = false,
        });

        await ctx.SaveChangesAsync();

        var reader = new AdminPlatformEarningsReader(ctx);
        var series = await reader.GetSeriesAsync(PlatformEarningsSeriesRange.Yesterday);
        Assert.Single(series);
        Assert.Equal(33m, series[0].TotalEur);
        Assert.Equal(DateTime.UtcNow.Date.AddDays(-1).ToString("yyyy-MM-dd"), series[0].Period);
    }
}
