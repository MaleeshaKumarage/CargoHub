using CargoHub.Application.Billing.AdminPlans;
using CargoHub.Domain.Billing;
using CargoHub.Infrastructure.Persistence;
using CompanyEntity = CargoHub.Domain.Companies.Company;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CargoHub.Tests.Billing;

public sealed class SubscriptionPlanAdminRepositoryTests
{
    private static SubscriptionPlanAdminRepository CreateRepo(ApplicationDbContext db) => new(db);

    [Fact]
    public async Task GetPlanDetailAsync_returns_null_when_missing()
    {
        using var fx = new TestDbFixture();
        using var db = fx.CreateContext();
        var repo = CreateRepo(db);
        Assert.Null(await repo.GetPlanDetailAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetPlanDetailAsync_orders_periods_and_tiers()
    {
        using var fx = new TestDbFixture();
        using var db = fx.CreateContext();
        var planId = Guid.NewGuid();
        var p1 = Guid.NewGuid();
        var p2 = Guid.NewGuid();
        db.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "Zed",
            Kind = SubscriptionPlanKind.PayPerBooking,
            ChargeTimeAnchor = ChargeTimeAnchor.CreatedAtUtc,
            Currency = "EUR",
            IsActive = true
        });
        db.SubscriptionPlanPricingPeriods.Add(new SubscriptionPlanPricingPeriod
        {
            Id = p1,
            SubscriptionPlanId = planId,
            EffectiveFromUtc = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
        db.SubscriptionPlanPricingPeriods.Add(new SubscriptionPlanPricingPeriod
        {
            Id = p2,
            SubscriptionPlanId = planId,
            EffectiveFromUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ChargePerBooking = 2m
        });
        db.SubscriptionPlanPricingTiers.Add(new SubscriptionPlanPricingTier
        {
            Id = Guid.NewGuid(),
            SubscriptionPlanPricingPeriodId = p2,
            Ordinal = 2,
            InclusiveMaxBookingsInPeriod = 5,
            ChargePerBooking = 1m
        });
        db.SubscriptionPlanPricingTiers.Add(new SubscriptionPlanPricingTier
        {
            Id = Guid.NewGuid(),
            SubscriptionPlanPricingPeriodId = p2,
            Ordinal = 1,
            InclusiveMaxBookingsInPeriod = 3,
            ChargePerBooking = 2m
        });
        await db.SaveChangesAsync();

        var detail = await CreateRepo(db).GetPlanDetailAsync(planId);
        Assert.NotNull(detail);
        Assert.Equal(2, detail!.PricingPeriods.Count);
        Assert.True(detail.PricingPeriods[0].EffectiveFromUtc > detail.PricingPeriods[1].EffectiveFromUtc);
        Assert.Equal(2, detail.PricingPeriods[0].Tiers.Count);
        Assert.Equal(1, detail.PricingPeriods[0].Tiers[0].Ordinal);
    }

    [Fact]
    public async Task Create_Update_Delete_roundtrip_when_unused()
    {
        using var fx = new TestDbFixture();
        using var db = fx.CreateContext();
        var repo = CreateRepo(db);
        var id = await repo.CreatePlanAsync(
            "R1",
            SubscriptionPlanKind.MonthlyBundle.ToString(),
            ChargeTimeAnchor.FirstBillableAtUtc.ToString(),
            null,
            "SEK",
            true,
            default);
        Assert.NotEqual(Guid.Empty, id);

        var upd = await repo.UpdatePlanAsync(
            id,
            "R2",
            SubscriptionPlanKind.Trial.ToString(),
            ChargeTimeAnchor.CreatedAtUtc.ToString(),
            3,
            "nok",
            false,
            default);
        Assert.True(upd.Success);

        var badKind = await repo.UpdatePlanAsync(
            id,
            "R2",
            "NotAKind",
            ChargeTimeAnchor.CreatedAtUtc.ToString(),
            3,
            "NOK",
            false,
            default);
        Assert.False(badKind.Success);

        var badAnchor = await repo.UpdatePlanAsync(
            id,
            "R2",
            SubscriptionPlanKind.Trial.ToString(),
            "BadAnchor",
            3,
            "NOK",
            false,
            default);
        Assert.False(badAnchor.Success);

        var badTrial = await repo.UpdatePlanAsync(
            id,
            "R2",
            SubscriptionPlanKind.Trial.ToString(),
            ChargeTimeAnchor.CreatedAtUtc.ToString(),
            null,
            "NOK",
            false,
            default);
        Assert.False(badTrial.Success);

        var missing = await repo.UpdatePlanAsync(
            Guid.NewGuid(),
            "X",
            SubscriptionPlanKind.PayPerBooking.ToString(),
            ChargeTimeAnchor.CreatedAtUtc.ToString(),
            null,
            "EUR",
            true,
            default);
        Assert.False(missing.Success);

        var del = await repo.DeletePlanAsync(id, default);
        Assert.True(del.Success);
    }

    [Fact]
    public async Task DeletePlanAsync_blocked_when_company_assigned()
    {
        using var fx = new TestDbFixture();
        using var db = fx.CreateContext();
        var planId = Guid.NewGuid();
        db.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "P",
            Kind = SubscriptionPlanKind.PayPerBooking,
            ChargeTimeAnchor = ChargeTimeAnchor.CreatedAtUtc,
            Currency = "EUR",
            IsActive = true
        });
        db.Companies.Add(new CompanyEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = Guid.NewGuid().ToString("N"),
            SubscriptionPlanId = planId
        });
        await db.SaveChangesAsync();

        var r = await CreateRepo(db).DeletePlanAsync(planId, default);
        Assert.False(r.Success);
    }

    [Fact]
    public async Task DeletePlanAsync_blocked_when_billing_lines_reference_plan()
    {
        using var fx = new TestDbFixture();
        using var db = fx.CreateContext();
        var planId = Guid.NewGuid();
        var periodPricingId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        db.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "P",
            Kind = SubscriptionPlanKind.PayPerBooking,
            ChargeTimeAnchor = ChargeTimeAnchor.CreatedAtUtc,
            Currency = "EUR",
            IsActive = true
        });
        db.SubscriptionPlanPricingPeriods.Add(new SubscriptionPlanPricingPeriod
        {
            Id = periodPricingId,
            SubscriptionPlanId = planId,
            EffectiveFromUtc = DateTime.UtcNow
        });
        db.Companies.Add(new CompanyEntity { Id = companyId, CompanyId = Guid.NewGuid().ToString("N") });
        var billingPeriodId = Guid.NewGuid();
        db.CompanyBillingPeriods.Add(new CompanyBillingPeriod
        {
            Id = billingPeriodId,
            CompanyId = companyId,
            YearUtc = 2026,
            MonthUtc = 1,
            Currency = "EUR",
            Status = CompanyBillingPeriodStatus.Open
        });
        db.BillingLineItems.Add(new BillingLineItem
        {
            Id = Guid.NewGuid(),
            CompanyBillingPeriodId = billingPeriodId,
            LineType = BillingLineType.PerBooking,
            Amount = 1m,
            Currency = "EUR",
            SubscriptionPlanId = planId,
            SubscriptionPlanPricingPeriodId = periodPricingId,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var r = await CreateRepo(db).DeletePlanAsync(planId, default);
        Assert.False(r.Success);
    }

    [Fact]
    public async Task Pricing_periods_CRUD_and_tiers_replace()
    {
        using var fx = new TestDbFixture();
        using var db = fx.CreateContext();
        var repo = CreateRepo(db);
        var planId = Guid.NewGuid();
        db.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "P",
            Kind = SubscriptionPlanKind.TieredPayPerBooking,
            ChargeTimeAnchor = ChargeTimeAnchor.CreatedAtUtc,
            Currency = "EUR",
            IsActive = true
        });
        await db.SaveChangesAsync();

        var badAdd = await repo.AddPricingPeriodAsync(Guid.NewGuid(), DateTime.UtcNow, 1m, null, null, null, default);
        Assert.False(badAdd.Success);

        var add = await repo.AddPricingPeriodAsync(
            planId,
            new DateTime(2026, 3, 1, 12, 0, 0, DateTimeKind.Unspecified),
            1.5m,
            9m,
            10,
            2m,
            default);
        Assert.True(add.Success);

        var periodId = (await db.SubscriptionPlanPricingPeriods.FirstAsync()).Id;

        var upd = await repo.UpdatePricingPeriodAsync(periodId, DateTime.UtcNow, 2m, 8m, 11, 3m, default);
        Assert.True(upd.Success);

        var dupTiers = await repo.ReplaceTiersAsync(
            periodId,
            new[]
            {
                new AdminPricingTierInput { Ordinal = 1, ChargePerBooking = 1m },
                new AdminPricingTierInput { Ordinal = 1, ChargePerBooking = 2m }
            },
            default);
        Assert.False(dupTiers.Success);

        var okTiers = await repo.ReplaceTiersAsync(
            periodId,
            new[]
            {
                new AdminPricingTierInput { Ordinal = 1, InclusiveMaxBookingsInPeriod = 5, ChargePerBooking = 1m },
                new AdminPricingTierInput { Ordinal = 2, MonthlyFee = 4m }
            },
            default);
        Assert.True(okTiers.Success);

        var missingTier = await repo.ReplaceTiersAsync(Guid.NewGuid(), Array.Empty<AdminPricingTierInput>(), default);
        Assert.False(missingTier.Success);

        var delPeriodMissing = await repo.DeletePricingPeriodAsync(Guid.NewGuid(), default);
        Assert.False(delPeriodMissing.Success);

        var delOk = await repo.DeletePricingPeriodAsync(periodId, default);
        Assert.True(delOk.Success);
    }

    [Fact]
    public async Task DeletePricingPeriodAsync_blocked_when_referenced_by_line_item()
    {
        using var fx = new TestDbFixture();
        using var db = fx.CreateContext();
        var planId = Guid.NewGuid();
        var periodPricingId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        db.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "P",
            Kind = SubscriptionPlanKind.PayPerBooking,
            ChargeTimeAnchor = ChargeTimeAnchor.CreatedAtUtc,
            Currency = "EUR",
            IsActive = true
        });
        db.SubscriptionPlanPricingPeriods.Add(new SubscriptionPlanPricingPeriod
        {
            Id = periodPricingId,
            SubscriptionPlanId = planId,
            EffectiveFromUtc = DateTime.UtcNow
        });
        db.Companies.Add(new CompanyEntity { Id = companyId, CompanyId = Guid.NewGuid().ToString("N") });
        var billingPeriodId = Guid.NewGuid();
        db.CompanyBillingPeriods.Add(new CompanyBillingPeriod
        {
            Id = billingPeriodId,
            CompanyId = companyId,
            YearUtc = 2026,
            MonthUtc = 2,
            Currency = "EUR",
            Status = CompanyBillingPeriodStatus.Open
        });
        db.BillingLineItems.Add(new BillingLineItem
        {
            Id = Guid.NewGuid(),
            CompanyBillingPeriodId = billingPeriodId,
            LineType = BillingLineType.PerBooking,
            Amount = 1m,
            Currency = "EUR",
            SubscriptionPlanId = planId,
            SubscriptionPlanPricingPeriodId = periodPricingId,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var r = await CreateRepo(db).DeletePricingPeriodAsync(periodPricingId, default);
        Assert.False(r.Success);
    }

    [Fact]
    public async Task UpdatePricingPeriodAsync_NotFound()
    {
        using var fx = new TestDbFixture();
        using var db = fx.CreateContext();
        var r = await CreateRepo(db).UpdatePricingPeriodAsync(Guid.NewGuid(), DateTime.UtcNow, null, null, null, null, default);
        Assert.False(r.Success);
    }
}
