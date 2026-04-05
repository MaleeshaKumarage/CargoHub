using CargoHub.Domain.Billing;
using CargoHub.Domain.Companies;
using CargoHub.Infrastructure.Persistence;
using Xunit;
using CompanyEntity = CargoHub.Domain.Companies.Company;

namespace CargoHub.Tests.Subscriptions;

public class PortalCompanySubscriptionReaderTests : IDisposable
{
    private readonly TestDbFixture _fixture;

    public PortalCompanySubscriptionReaderTests()
    {
        _fixture = new TestDbFixture();
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task GetForBusinessIdAsync_NullOrWhiteSpace_ReturnsNull()
    {
        using var ctx = _fixture.CreateContext();
        var reader = new PortalCompanySubscriptionReader(ctx);
        Assert.Null(await reader.GetForBusinessIdAsync(null!));
        Assert.Null(await reader.GetForBusinessIdAsync(""));
        Assert.Null(await reader.GetForBusinessIdAsync("   "));
    }

    [Fact]
    public async Task GetForBusinessIdAsync_CompanyMissing_ReturnsNull()
    {
        using var ctx = _fixture.CreateContext();
        var reader = new PortalCompanySubscriptionReader(ctx);
        Assert.Null(await reader.GetForBusinessIdAsync("no-such-biz"));
    }

    [Fact]
    public async Task GetForBusinessIdAsync_NoPlanAssigned_ReturnsNone()
    {
        using var ctx = _fixture.CreateContext();
        var company = new CompanyEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = "c-none",
            Name = "No Plan Oy",
            BusinessId = "1111111-1",
            CustomerId = "cust"
        };
        ctx.Companies.Add(company);
        await ctx.SaveChangesAsync();

        var reader = new PortalCompanySubscriptionReader(ctx);
        var dto = await reader.GetForBusinessIdAsync("1111111-1");
        Assert.NotNull(dto);
        Assert.Equal("None", dto.PlanKind);
        Assert.Equal("", dto.PlanName);
        Assert.Null(dto.TrialBookingAllowance);
    }

    [Fact]
    public async Task GetForBusinessIdAsync_PlanRowMissing_ReturnsUnknown()
    {
        using var ctx = _fixture.CreateContext();
        var orphanPlanId = Guid.NewGuid();
        var company = new CompanyEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = "c-orphan",
            Name = "Orphan Oy",
            BusinessId = "2222222-2",
            CustomerId = "cust",
            SubscriptionPlanId = orphanPlanId
        };
        ctx.Companies.Add(company);
        await ctx.SaveChangesAsync();

        var reader = new PortalCompanySubscriptionReader(ctx);
        var dto = await reader.GetForBusinessIdAsync("2222222-2");
        Assert.NotNull(dto);
        Assert.Equal("Unknown", dto.PlanKind);
    }

    [Fact]
    public async Task GetForBusinessIdAsync_MatchesBusinessIdCaseInsensitively()
    {
        using var ctx = _fixture.CreateContext();
        var company = new CompanyEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = "c-case",
            Name = "Case Oy",
            BusinessId = "Ab-12",
            CustomerId = "cust"
        };
        ctx.Companies.Add(company);
        await ctx.SaveChangesAsync();

        var reader = new PortalCompanySubscriptionReader(ctx);
        var dto = await reader.GetForBusinessIdAsync("  ab-12 ");
        Assert.NotNull(dto);
        Assert.Equal("None", dto.PlanKind);
    }

    [Fact]
    public async Task GetForBusinessIdAsync_TrialPlan_IncludesAllowanceAndPeriodFields()
    {
        using var ctx = _fixture.CreateContext();
        var planId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var plan = new SubscriptionPlan
        {
            Id = planId,
            Name = "Trial X",
            Kind = SubscriptionPlanKind.Trial,
            TrialBookingAllowance = 7,
            Currency = "SEK",
            PricingPeriods =
            {
                new SubscriptionPlanPricingPeriod
                {
                    Id = periodId,
                    SubscriptionPlanId = planId,
                    EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
                    ChargePerBooking = 1.5m,
                    MonthlyFee = 9.99m,
                    IncludedBookingsPerMonth = 10,
                    OverageChargePerBooking = 2m
                }
            }
        };
        ctx.SubscriptionPlans.Add(plan);
        var company = new CompanyEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = "c-trial",
            Name = "Trial Oy",
            BusinessId = "3333333-3",
            CustomerId = "cust",
            SubscriptionPlanId = planId
        };
        ctx.Companies.Add(company);
        await ctx.SaveChangesAsync();

        var reader = new PortalCompanySubscriptionReader(ctx);
        var dto = await reader.GetForBusinessIdAsync("3333333-3");
        Assert.NotNull(dto);
        Assert.Equal("Trial", dto.PlanKind);
        Assert.Equal(7, dto.TrialBookingAllowance);
        Assert.Equal("Trial X", dto.PlanName);
        Assert.Equal("SEK", dto.Currency);
        Assert.Equal(1.5m, dto.ChargePerBooking);
        Assert.Equal(9.99m, dto.MonthlyFee);
        Assert.Equal(10, dto.IncludedBookingsPerMonth);
        Assert.Equal(2m, dto.OverageChargePerBooking);
        Assert.Null(dto.Tiers);
    }

    [Fact]
    public async Task GetForBusinessIdAsync_NonTrial_OmitsTrialAllowance()
    {
        using var ctx = _fixture.CreateContext();
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan
        {
            Id = planId,
            Name = "PayGo",
            Kind = SubscriptionPlanKind.PayPerBooking,
            TrialBookingAllowance = 99,
            Currency = "EUR",
            PricingPeriods =
            {
                new SubscriptionPlanPricingPeriod
                {
                    Id = Guid.NewGuid(),
                    SubscriptionPlanId = planId,
                    EffectiveFromUtc = DateTime.UtcNow.AddHours(-1),
                    ChargePerBooking = 3m
                }
            }
        };
        ctx.SubscriptionPlans.Add(plan);
        var company = new CompanyEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = "c-pay",
            Name = "Pay Oy",
            BusinessId = "4444444-4",
            CustomerId = "cust",
            SubscriptionPlanId = planId
        };
        ctx.Companies.Add(company);
        await ctx.SaveChangesAsync();

        var reader = new PortalCompanySubscriptionReader(ctx);
        var dto = await reader.GetForBusinessIdAsync("4444444-4");
        Assert.NotNull(dto);
        Assert.Equal("PayPerBooking", dto.PlanKind);
        Assert.Null(dto.TrialBookingAllowance);
        Assert.Equal(3m, dto.ChargePerBooking);
    }

    [Fact]
    public async Task GetForBusinessIdAsync_NoEffectivePeriod_YieldsNullPeriodFields()
    {
        using var ctx = _fixture.CreateContext();
        var planId = Guid.NewGuid();
        var plan = new SubscriptionPlan
        {
            Id = planId,
            Name = "Future",
            Kind = SubscriptionPlanKind.MonthlyBundle,
            Currency = "EUR",
            PricingPeriods =
            {
                new SubscriptionPlanPricingPeriod
                {
                    Id = Guid.NewGuid(),
                    SubscriptionPlanId = planId,
                    EffectiveFromUtc = DateTime.UtcNow.AddDays(30),
                    MonthlyFee = 100m
                }
            }
        };
        ctx.SubscriptionPlans.Add(plan);
        var company = new CompanyEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = "c-fut",
            Name = "Future Oy",
            BusinessId = "5555555-5",
            CustomerId = "cust",
            SubscriptionPlanId = planId
        };
        ctx.Companies.Add(company);
        await ctx.SaveChangesAsync();

        var reader = new PortalCompanySubscriptionReader(ctx);
        var dto = await reader.GetForBusinessIdAsync("5555555-5");
        Assert.NotNull(dto);
        Assert.Null(dto.ChargePerBooking);
        Assert.Null(dto.MonthlyFee);
        Assert.Null(dto.Tiers);
    }

    [Fact]
    public async Task GetForBusinessIdAsync_TiersOrderedByOrdinal()
    {
        using var ctx = _fixture.CreateContext();
        var planId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var plan = new SubscriptionPlan
        {
            Id = planId,
            Name = "Tiered",
            Kind = SubscriptionPlanKind.TieredPayPerBooking,
            Currency = "EUR",
            PricingPeriods =
            {
                new SubscriptionPlanPricingPeriod
                {
                    Id = periodId,
                    SubscriptionPlanId = planId,
                    EffectiveFromUtc = DateTime.UtcNow.AddDays(-2),
                    Tiers =
                    {
                        new SubscriptionPlanPricingTier
                        {
                            Id = Guid.NewGuid(),
                            SubscriptionPlanPricingPeriodId = periodId,
                            Ordinal = 2,
                            InclusiveMaxBookingsInPeriod = 20,
                            ChargePerBooking = 2m
                        },
                        new SubscriptionPlanPricingTier
                        {
                            Id = Guid.NewGuid(),
                            SubscriptionPlanPricingPeriodId = periodId,
                            Ordinal = 1,
                            InclusiveMaxBookingsInPeriod = 10,
                            ChargePerBooking = 1m
                        }
                    }
                }
            }
        };
        ctx.SubscriptionPlans.Add(plan);
        var company = new CompanyEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = "c-tier",
            Name = "Tier Oy",
            BusinessId = "6666666-6",
            CustomerId = "cust",
            SubscriptionPlanId = planId
        };
        ctx.Companies.Add(company);
        await ctx.SaveChangesAsync();

        var reader = new PortalCompanySubscriptionReader(ctx);
        var dto = await reader.GetForBusinessIdAsync("6666666-6");
        Assert.NotNull(dto);
        Assert.NotNull(dto.Tiers);
        Assert.Equal(2, dto.Tiers!.Count);
        Assert.Equal(1, dto.Tiers[0].Ordinal);
        Assert.Equal(10, dto.Tiers[0].InclusiveMaxBookingsInPeriod);
        Assert.Equal(2, dto.Tiers[1].Ordinal);
    }

    [Fact]
    public async Task GetForBusinessIdAsync_PeriodWithEmptyTiers_YieldsNullTiers()
    {
        using var ctx = _fixture.CreateContext();
        var planId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        var plan = new SubscriptionPlan
        {
            Id = planId,
            Name = "Flat",
            Kind = SubscriptionPlanKind.PayPerBooking,
            Currency = "EUR",
            PricingPeriods =
            {
                new SubscriptionPlanPricingPeriod
                {
                    Id = periodId,
                    SubscriptionPlanId = planId,
                    EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
                    ChargePerBooking = 5m,
                    Tiers = new List<SubscriptionPlanPricingTier>()
                }
            }
        };
        ctx.SubscriptionPlans.Add(plan);
        var company = new CompanyEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = "c-flat",
            Name = "Flat Oy",
            BusinessId = "7777777-7",
            CustomerId = "cust",
            SubscriptionPlanId = planId
        };
        ctx.Companies.Add(company);
        await ctx.SaveChangesAsync();

        var reader = new PortalCompanySubscriptionReader(ctx);
        var dto = await reader.GetForBusinessIdAsync("7777777-7");
        Assert.NotNull(dto);
        Assert.Null(dto.Tiers);
        Assert.Equal(5m, dto.ChargePerBooking);
    }
}
