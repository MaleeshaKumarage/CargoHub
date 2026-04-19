using CargoHub.Application.Billing;
using CargoHub.Domain.Billing;
using CargoHub.Domain.Bookings;
using CargoHub.Domain.Companies;
using CargoHub.Infrastructure.Billing;
using CargoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;
using CompanyEntity = CargoHub.Domain.Companies.Company;

namespace CargoHub.Tests.Billing;

public sealed class SubscriptionBillingOrchestratorTests : IDisposable
{
    private readonly TestDbFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    private static Booking MinimalBooking(
        Guid id,
        string customerId,
        Guid? companyId,
        bool isDraft,
        bool isTest = false,
        DateTime? firstBillableAtUtc = null)
    {
        var now = DateTime.UtcNow;
        return new Booking
        {
            Id = id,
            CustomerId = customerId,
            CompanyId = companyId,
            IsDraft = isDraft,
            IsTestBooking = isTest,
            Enabled = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            FirstBillableAtUtc = firstBillableAtUtc,
            Header = new BookingHeader { SenderId = customerId },
            Receiver = new BookingParty(),
            Shipper = new BookingParty(),
            PickUpAddress = new BookingParty(),
            DeliveryPoint = new BookingParty(),
            Shipment = new BookingShipment(),
            ShippingInfo = new ShippingInfo()
        };
    }

    [Fact]
    public async Task AssertBillableBookingAllowedAsync_Throws_WhenNoCompany()
    {
        using var ctx = _fixture.CreateContext();
        var orch = new SubscriptionBillingOrchestrator(ctx, CargoHub.Tests.TestSupport.StubRiderBookingAssignmentCoordinator.Instance);
        var ex = await Assert.ThrowsAsync<SubscriptionBillingException>(() =>
            orch.AssertBillableBookingAllowedAsync(null, false, default));
        Assert.Equal(SubscriptionBillingConstants.CompanyRequiredForBookingErrorCode, ex.ErrorCode);
    }

    [Fact]
    public async Task AssertBillableBookingAllowedAsync_Skips_WhenTestBooking()
    {
        using var ctx = _fixture.CreateContext();
        var orch = new SubscriptionBillingOrchestrator(ctx, CargoHub.Tests.TestSupport.StubRiderBookingAssignmentCoordinator.Instance);
        await orch.AssertBillableBookingAllowedAsync(Guid.NewGuid(), true, default);
    }

    [Fact]
    public async Task AssertBillableBookingAllowedAsync_Allows_WhenCompanyUsesNonTrialPlan()
    {
        using var ctx = _fixture.CreateContext();
        var planId = Guid.NewGuid();
        ctx.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "Paygo",
            Kind = SubscriptionPlanKind.PayPerBooking,
            Currency = "EUR",
            IsActive = true
        });
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "Co",
            BusinessId = "biz-nt",
            CompanyId = companyId.ToString("N"),
            SubscriptionPlanId = planId
        });
        await ctx.SaveChangesAsync();
        var orch = new SubscriptionBillingOrchestrator(ctx, CargoHub.Tests.TestSupport.StubRiderBookingAssignmentCoordinator.Instance);
        await orch.AssertBillableBookingAllowedAsync(companyId, false, default);
    }

    [Fact]
    public async Task AssertBillableBookingAllowedAsync_TrialExhausted_Throws()
    {
        using var ctx = _fixture.CreateContext();
        await SubscriptionPlanSeed.EnsureDefaultTrialPlanAsync(ctx);
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "Co",
            BusinessId = "biz-trial",
            CompanyId = companyId.ToString("N"),
            SubscriptionPlanId = SubscriptionBillingConstants.DefaultTrialPlanId
        });
        for (var i = 0; i < 5; i++)
        {
            var bid = Guid.NewGuid();
            ctx.Bookings.Add(MinimalBooking(bid, "cust-trial", companyId, isDraft: false, firstBillableAtUtc: DateTime.UtcNow.AddMinutes(-i)));
        }

        await ctx.SaveChangesAsync();
        var orch = new SubscriptionBillingOrchestrator(ctx, CargoHub.Tests.TestSupport.StubRiderBookingAssignmentCoordinator.Instance);
        var ex = await Assert.ThrowsAsync<SubscriptionBillingException>(() =>
            orch.AssertBillableBookingAllowedAsync(companyId, false, default));
        Assert.Equal("TrialBookingLimitExceeded", ex.ErrorCode);
    }

    [Fact]
    public async Task AssertBillableBookingAllowedAsync_TrialExhausted_CountsBookingsWithoutFirstBillableAtUtc()
    {
        using var ctx = _fixture.CreateContext();
        await SubscriptionPlanSeed.EnsureDefaultTrialPlanAsync(ctx);
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "Co",
            BusinessId = "biz-nofb",
            CompanyId = companyId.ToString("N"),
            SubscriptionPlanId = SubscriptionBillingConstants.DefaultTrialPlanId
        });
        for (var i = 0; i < 5; i++)
            ctx.Bookings.Add(MinimalBooking(Guid.NewGuid(), "cust-nofb", companyId, isDraft: false, firstBillableAtUtc: null));

        await ctx.SaveChangesAsync();
        var orch = new SubscriptionBillingOrchestrator(ctx, CargoHub.Tests.TestSupport.StubRiderBookingAssignmentCoordinator.Instance);
        var ex = await Assert.ThrowsAsync<SubscriptionBillingException>(() =>
            orch.AssertBillableBookingAllowedAsync(companyId, false, default));
        Assert.Equal("TrialBookingLimitExceeded", ex.ErrorCode);
    }

    [Fact]
    public async Task AssertBillableBookingAllowedAsync_TrialResolvedFromAssignment_WhenCompanyPlanIdNull()
    {
        using var ctx = _fixture.CreateContext();
        await SubscriptionPlanSeed.EnsureDefaultTrialPlanAsync(ctx);
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "Co",
            BusinessId = "biz-asg",
            CompanyId = companyId.ToString("N"),
            SubscriptionPlanId = null
        });
        ctx.CompanySubscriptionAssignments.Add(new CompanySubscriptionAssignment
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            SubscriptionPlanId = SubscriptionBillingConstants.DefaultTrialPlanId,
            EffectiveFromUtc = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
        for (var i = 0; i < 5; i++)
            ctx.Bookings.Add(MinimalBooking(Guid.NewGuid(), "cust-asg", companyId, isDraft: false, firstBillableAtUtc: DateTime.UtcNow.AddMinutes(-i)));

        await ctx.SaveChangesAsync();
        var orch = new SubscriptionBillingOrchestrator(ctx, CargoHub.Tests.TestSupport.StubRiderBookingAssignmentCoordinator.Instance);
        var ex = await Assert.ThrowsAsync<SubscriptionBillingException>(() =>
            orch.AssertBillableBookingAllowedAsync(companyId, false, default));
        Assert.Equal("TrialBookingLimitExceeded", ex.ErrorCode);
    }

    [Fact]
    public async Task ConfirmDraftWithBillingAsync_Throws_WhenNotTestAndNoCompany()
    {
        using var ctx = _fixture.CreateContext();
        await SubscriptionPlanSeed.EnsureDefaultTrialPlanAsync(ctx);
        var id = Guid.NewGuid();
        ctx.Bookings.Add(MinimalBooking(id, "c-nc", null, isDraft: true));
        await ctx.SaveChangesAsync();
        var orch = new SubscriptionBillingOrchestrator(ctx, CargoHub.Tests.TestSupport.StubRiderBookingAssignmentCoordinator.Instance);
        var ex = await Assert.ThrowsAsync<SubscriptionBillingException>(() =>
            orch.ConfirmDraftWithBillingAsync(id, "c-nc", default));
        Assert.Equal(SubscriptionBillingConstants.CompanyRequiredForBookingErrorCode, ex.ErrorCode);
    }

    [Fact]
    public async Task ConfirmDraftWithBillingAsync_ReturnsFalse_WhenBookingMissing()
    {
        using var ctx = _fixture.CreateContext();
        var orch = new SubscriptionBillingOrchestrator(ctx, CargoHub.Tests.TestSupport.StubRiderBookingAssignmentCoordinator.Instance);
        var ok = await orch.ConfirmDraftWithBillingAsync(Guid.NewGuid(), "any-customer", default);
        Assert.False(ok);
    }

    [Fact]
    public async Task ConfirmDraftWithBillingAsync_ReturnsFalse_WhenCustomerIdMismatch()
    {
        using var ctx = _fixture.CreateContext();
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "Co",
            BusinessId = "biz-mis",
            CompanyId = companyId.ToString("N"),
        });
        var id = Guid.NewGuid();
        ctx.Bookings.Add(MinimalBooking(id, "expected-cust", companyId, isDraft: true));
        await ctx.SaveChangesAsync();
        var orch = new SubscriptionBillingOrchestrator(ctx, CargoHub.Tests.TestSupport.StubRiderBookingAssignmentCoordinator.Instance);
        var ok = await orch.ConfirmDraftWithBillingAsync(id, "other-cust", default);
        Assert.False(ok);
    }

    [Fact]
    public async Task ConfirmDraftWithBillingAsync_ReturnsFalse_WhenNotDraft()
    {
        using var ctx = _fixture.CreateContext();
        var id = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "Co",
            BusinessId = "biz-nd",
            CompanyId = companyId.ToString("N")
        });
        ctx.Bookings.Add(MinimalBooking(id, "c1", companyId, isDraft: false));
        await ctx.SaveChangesAsync();
        var orch = new SubscriptionBillingOrchestrator(ctx, CargoHub.Tests.TestSupport.StubRiderBookingAssignmentCoordinator.Instance);
        var ok = await orch.ConfirmDraftWithBillingAsync(id, "c1", default);
        Assert.False(ok);
    }

    [Fact]
    public async Task ConfirmDraftWithBillingAsync_ConfirmsDraft_SetsFirstBillable()
    {
        using var ctx = _fixture.CreateContext();
        await SubscriptionPlanSeed.EnsureDefaultTrialPlanAsync(ctx);
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "Co",
            BusinessId = "biz-cf",
            CompanyId = companyId.ToString("N"),
            SubscriptionPlanId = SubscriptionBillingConstants.DefaultTrialPlanId
        });
        var id = Guid.NewGuid();
        ctx.Bookings.Add(MinimalBooking(id, "c2", companyId, isDraft: true));
        await ctx.SaveChangesAsync();
        var orch = new SubscriptionBillingOrchestrator(ctx, CargoHub.Tests.TestSupport.StubRiderBookingAssignmentCoordinator.Instance);
        var ok = await orch.ConfirmDraftWithBillingAsync(id, "c2", default);
        Assert.True(ok);
        var b = await ctx.Bookings.AsNoTracking().FirstAsync(x => x.Id == id);
        Assert.False(b.IsDraft);
        Assert.NotNull(b.FirstBillableAtUtc);
    }

    [Fact]
    public async Task PostBillingForNewCompletedBookingAsync_NoOp_WhenBookingIdUnknown()
    {
        using var ctx = _fixture.CreateContext();
        await new SubscriptionBillingOrchestrator(ctx, CargoHub.Tests.TestSupport.StubRiderBookingAssignmentCoordinator.Instance).PostBillingForNewCompletedBookingAsync(Guid.NewGuid(), default);
    }

    [Fact]
    public async Task PostBillingForNewCompletedBookingAsync_Skips_WhenNoCompany()
    {
        using var ctx = _fixture.CreateContext();
        var id = Guid.NewGuid();
        ctx.Bookings.Add(MinimalBooking(id, "c3", null, isDraft: false));
        await ctx.SaveChangesAsync();
        var orch = new SubscriptionBillingOrchestrator(ctx, CargoHub.Tests.TestSupport.StubRiderBookingAssignmentCoordinator.Instance);
        await orch.PostBillingForNewCompletedBookingAsync(id, default);
        var b = await ctx.Bookings.AsNoTracking().FirstAsync(x => x.Id == id);
        Assert.Null(b.FirstBillableAtUtc);
    }

    [Fact]
    public async Task PostBillingForNewCompletedBookingAsync_Skips_WhenTestBooking()
    {
        using var ctx = _fixture.CreateContext();
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "Co",
            BusinessId = "biz-tb",
            CompanyId = companyId.ToString("N")
        });
        var id = Guid.NewGuid();
        ctx.Bookings.Add(MinimalBooking(id, "c4", companyId, isDraft: false, isTest: true));
        await ctx.SaveChangesAsync();
        var orch = new SubscriptionBillingOrchestrator(ctx, CargoHub.Tests.TestSupport.StubRiderBookingAssignmentCoordinator.Instance);
        await orch.PostBillingForNewCompletedBookingAsync(id, default);
        var b = await ctx.Bookings.AsNoTracking().FirstAsync(x => x.Id == id);
        Assert.Null(b.FirstBillableAtUtc);
    }

    [Fact]
    public async Task PostBillingForNewCompletedBookingAsync_PayPerBooking_AddsLine()
    {
        using var ctx = _fixture.CreateContext();
        var planId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        ctx.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "Paygo",
            Kind = SubscriptionPlanKind.PayPerBooking,
            Currency = "EUR",
            IsActive = true,
            ChargeTimeAnchor = ChargeTimeAnchor.FirstBillableAtUtc
        });
        ctx.SubscriptionPlanPricingPeriods.Add(new SubscriptionPlanPricingPeriod
        {
            Id = periodId,
            SubscriptionPlanId = planId,
            EffectiveFromUtc = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ChargePerBooking = 9.99m
        });
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "Co",
            BusinessId = "biz-pay",
            CompanyId = companyId.ToString("N"),
            SubscriptionPlanId = planId
        });
        var id = Guid.NewGuid();
        ctx.Bookings.Add(MinimalBooking(id, "c5", companyId, isDraft: false));
        await ctx.SaveChangesAsync();
        var orch = new SubscriptionBillingOrchestrator(ctx, CargoHub.Tests.TestSupport.StubRiderBookingAssignmentCoordinator.Instance);
        await orch.PostBillingForNewCompletedBookingAsync(id, default);
        var lines = await ctx.BillingLineItems.Where(l => l.BookingId == id).ToListAsync();
        Assert.Single(lines);
        Assert.Equal(BillingLineType.PerBooking, lines[0].LineType);
        Assert.Equal(9.99m, lines[0].Amount);
    }

    [Fact]
    public async Task PostBillingForNewCompletedBookingAsync_PayPerBooking_SecondCall_IsIdempotent()
    {
        using var ctx = _fixture.CreateContext();
        var planId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        ctx.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "Paygo",
            Kind = SubscriptionPlanKind.PayPerBooking,
            Currency = "EUR",
            IsActive = true,
            ChargeTimeAnchor = ChargeTimeAnchor.FirstBillableAtUtc
        });
        ctx.SubscriptionPlanPricingPeriods.Add(new SubscriptionPlanPricingPeriod
        {
            Id = periodId,
            SubscriptionPlanId = planId,
            EffectiveFromUtc = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ChargePerBooking = 1m
        });
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "Co",
            BusinessId = "biz-idem",
            CompanyId = companyId.ToString("N"),
            SubscriptionPlanId = planId
        });
        var id = Guid.NewGuid();
        ctx.Bookings.Add(MinimalBooking(id, "c-idem", companyId, isDraft: false));
        await ctx.SaveChangesAsync();
        var orch = new SubscriptionBillingOrchestrator(ctx, CargoHub.Tests.TestSupport.StubRiderBookingAssignmentCoordinator.Instance);
        await orch.PostBillingForNewCompletedBookingAsync(id, default);
        await orch.PostBillingForNewCompletedBookingAsync(id, default);
        Assert.Equal(1, await ctx.BillingLineItems.CountAsync(l => l.BookingId == id));
    }

    [Fact]
    public async Task PostBillingForNewCompletedBookingAsync_UsesCreatedAtUtc_WhenAnchorIsCreatedAt()
    {
        using var ctx = _fixture.CreateContext();
        var planId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        ctx.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "PaygoC",
            Kind = SubscriptionPlanKind.PayPerBooking,
            Currency = "EUR",
            IsActive = true,
            ChargeTimeAnchor = ChargeTimeAnchor.CreatedAtUtc
        });
        ctx.SubscriptionPlanPricingPeriods.Add(new SubscriptionPlanPricingPeriod
        {
            Id = periodId,
            SubscriptionPlanId = planId,
            EffectiveFromUtc = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ChargePerBooking = 2m
        });
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "Co",
            BusinessId = "biz-ca",
            CompanyId = companyId.ToString("N"),
            SubscriptionPlanId = planId
        });
        var id = Guid.NewGuid();
        var b = MinimalBooking(id, "c-anc", companyId, isDraft: false);
        b.CreatedAtUtc = new DateTime(2040, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        ctx.Bookings.Add(b);
        await ctx.SaveChangesAsync();
        var orch = new SubscriptionBillingOrchestrator(ctx, CargoHub.Tests.TestSupport.StubRiderBookingAssignmentCoordinator.Instance);
        await orch.PostBillingForNewCompletedBookingAsync(id, default);
        Assert.True(await ctx.BillingLineItems.AnyAsync(l => l.BookingId == id && l.Amount == 2m));
    }

    [Fact]
    public async Task PostBillingForNewCompletedBookingAsync_PayPerBooking_NoLine_WhenChargePerBookingMissing()
    {
        using var ctx = _fixture.CreateContext();
        var planId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        ctx.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "PaygoEmpty",
            Kind = SubscriptionPlanKind.PayPerBooking,
            Currency = "EUR",
            IsActive = true,
            ChargeTimeAnchor = ChargeTimeAnchor.FirstBillableAtUtc
        });
        ctx.SubscriptionPlanPricingPeriods.Add(new SubscriptionPlanPricingPeriod
        {
            Id = periodId,
            SubscriptionPlanId = planId,
            EffectiveFromUtc = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ChargePerBooking = null
        });
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "Co",
            BusinessId = "biz-nc",
            CompanyId = companyId.ToString("N"),
            SubscriptionPlanId = planId
        });
        var id = Guid.NewGuid();
        ctx.Bookings.Add(MinimalBooking(id, "c-nc", companyId, isDraft: false));
        await ctx.SaveChangesAsync();
        await new SubscriptionBillingOrchestrator(ctx, CargoHub.Tests.TestSupport.StubRiderBookingAssignmentCoordinator.Instance).PostBillingForNewCompletedBookingAsync(id, default);
        Assert.Empty(await ctx.BillingLineItems.Where(l => l.BookingId == id).ToListAsync());
    }

    [Fact]
    public async Task PostBillingForNewCompletedBookingAsync_InactivePlan_NoLines()
    {
        using var ctx = _fixture.CreateContext();
        var planId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        ctx.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "Inactive",
            Kind = SubscriptionPlanKind.PayPerBooking,
            Currency = "EUR",
            IsActive = false,
            ChargeTimeAnchor = ChargeTimeAnchor.FirstBillableAtUtc
        });
        ctx.SubscriptionPlanPricingPeriods.Add(new SubscriptionPlanPricingPeriod
        {
            Id = periodId,
            SubscriptionPlanId = planId,
            EffectiveFromUtc = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ChargePerBooking = 99m
        });
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "Co",
            BusinessId = "biz-ia",
            CompanyId = companyId.ToString("N"),
            SubscriptionPlanId = planId
        });
        var id = Guid.NewGuid();
        ctx.Bookings.Add(MinimalBooking(id, "c-ia", companyId, isDraft: false));
        await ctx.SaveChangesAsync();
        await new SubscriptionBillingOrchestrator(ctx, CargoHub.Tests.TestSupport.StubRiderBookingAssignmentCoordinator.Instance).PostBillingForNewCompletedBookingAsync(id, default);
        Assert.Empty(await ctx.BillingLineItems.ToListAsync());
    }

    [Fact]
    public async Task PostBillingForNewCompletedBookingAsync_MonthlyBundle_AddsBaseAndOverage()
    {
        using var ctx = _fixture.CreateContext();
        var planId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        ctx.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "Bundle",
            Kind = SubscriptionPlanKind.MonthlyBundle,
            Currency = "EUR",
            IsActive = true,
            ChargeTimeAnchor = ChargeTimeAnchor.FirstBillableAtUtc
        });
        ctx.SubscriptionPlanPricingPeriods.Add(new SubscriptionPlanPricingPeriod
        {
            Id = periodId,
            SubscriptionPlanId = planId,
            EffectiveFromUtc = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            MonthlyFee = 50m,
            IncludedBookingsPerMonth = 1,
            OverageChargePerBooking = 7m
        });
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "Co",
            BusinessId = "biz-mb",
            CompanyId = companyId.ToString("N"),
            SubscriptionPlanId = planId
        });
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        ctx.Bookings.Add(MinimalBooking(first, "c6", companyId, isDraft: false, firstBillableAtUtc: null));
        ctx.Bookings.Add(MinimalBooking(second, "c6", companyId, isDraft: false, firstBillableAtUtc: null));
        await ctx.SaveChangesAsync();
        var orch = new SubscriptionBillingOrchestrator(ctx, CargoHub.Tests.TestSupport.StubRiderBookingAssignmentCoordinator.Instance);
        await orch.PostBillingForNewCompletedBookingAsync(first, default);
        await orch.PostBillingForNewCompletedBookingAsync(second, default);
        var baseLines = await ctx.BillingLineItems.Where(l => l.LineType == BillingLineType.MonthlyBase).ToListAsync();
        Assert.Single(baseLines);
        var over = await ctx.BillingLineItems.CountAsync(l => l.LineType == BillingLineType.Overage && l.BookingId == second);
        Assert.Equal(1, over);
    }

    [Fact]
    public async Task PostBillingForNewCompletedBookingAsync_TieredPayPerBooking_AddsMarginalAndReconcile()
    {
        using var ctx = _fixture.CreateContext();
        var planId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        ctx.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "TierPaygo",
            Kind = SubscriptionPlanKind.TieredPayPerBooking,
            Currency = "EUR",
            IsActive = true,
            ChargeTimeAnchor = ChargeTimeAnchor.FirstBillableAtUtc
        });
        var period = new SubscriptionPlanPricingPeriod
        {
            Id = periodId,
            SubscriptionPlanId = planId,
            EffectiveFromUtc = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        ctx.SubscriptionPlanPricingPeriods.Add(period);
        ctx.SubscriptionPlanPricingTiers.AddRange(
            new SubscriptionPlanPricingTier
            {
                Id = Guid.NewGuid(),
                SubscriptionPlanPricingPeriodId = periodId,
                Ordinal = 1,
                InclusiveMaxBookingsInPeriod = 1,
                ChargePerBooking = 5m
            },
            new SubscriptionPlanPricingTier
            {
                Id = Guid.NewGuid(),
                SubscriptionPlanPricingPeriodId = periodId,
                Ordinal = 2,
                InclusiveMaxBookingsInPeriod = null,
                ChargePerBooking = 3m
            });
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "Co",
            BusinessId = "biz-tp",
            CompanyId = companyId.ToString("N"),
            SubscriptionPlanId = planId
        });
        var b1 = Guid.NewGuid();
        var b2 = Guid.NewGuid();
        ctx.Bookings.Add(MinimalBooking(b1, "c7", companyId, isDraft: false, firstBillableAtUtc: null));
        ctx.Bookings.Add(MinimalBooking(b2, "c7", companyId, isDraft: false, firstBillableAtUtc: null));
        await ctx.SaveChangesAsync();
        var orch = new SubscriptionBillingOrchestrator(ctx, CargoHub.Tests.TestSupport.StubRiderBookingAssignmentCoordinator.Instance);
        await orch.PostBillingForNewCompletedBookingAsync(b1, default);
        await orch.PostBillingForNewCompletedBookingAsync(b2, default);
        var m1 = await ctx.BillingLineItems.Where(l => l.BookingId == b1 && l.LineType == BillingLineType.TieredMarginal).SumAsync(l => l.Amount);
        var m2 = await ctx.BillingLineItems.Where(l => l.BookingId == b2 && l.LineType == BillingLineType.TieredMarginal).SumAsync(l => l.Amount);
        Assert.True(m1 > 0);
        Assert.True(m2 > 0);
    }

    [Fact]
    public async Task PostBillingForNewCompletedBookingAsync_TieredMonthlyByUsage_AddsPeriodAdjustment()
    {
        using var ctx = _fixture.CreateContext();
        var planId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        ctx.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "TierMonth",
            Kind = SubscriptionPlanKind.TieredMonthlyByUsage,
            Currency = "EUR",
            IsActive = true,
            ChargeTimeAnchor = ChargeTimeAnchor.FirstBillableAtUtc
        });
        var period = new SubscriptionPlanPricingPeriod
        {
            Id = periodId,
            SubscriptionPlanId = planId,
            EffectiveFromUtc = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        ctx.SubscriptionPlanPricingPeriods.Add(period);
        ctx.SubscriptionPlanPricingTiers.AddRange(
            new SubscriptionPlanPricingTier
            {
                Id = Guid.NewGuid(),
                SubscriptionPlanPricingPeriodId = periodId,
                Ordinal = 1,
                InclusiveMaxBookingsInPeriod = 5,
                MonthlyFee = 100m
            },
            new SubscriptionPlanPricingTier
            {
                Id = Guid.NewGuid(),
                SubscriptionPlanPricingPeriodId = periodId,
                Ordinal = 2,
                InclusiveMaxBookingsInPeriod = null,
                MonthlyFee = 200m
            });
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "Co",
            BusinessId = "biz-tm",
            CompanyId = companyId.ToString("N"),
            SubscriptionPlanId = planId
        });
        var id = Guid.NewGuid();
        ctx.Bookings.Add(MinimalBooking(id, "c8", companyId, isDraft: false, firstBillableAtUtc: null));
        await ctx.SaveChangesAsync();
        var orch = new SubscriptionBillingOrchestrator(ctx, CargoHub.Tests.TestSupport.StubRiderBookingAssignmentCoordinator.Instance);
        await orch.PostBillingForNewCompletedBookingAsync(id, default);
        var adj = await ctx.BillingLineItems.AnyAsync(l => l.LineType == BillingLineType.PeriodAdjustment);
        Assert.True(adj);
    }
}
