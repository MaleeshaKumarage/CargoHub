using CargoHub.Application.Billing.Admin;
using CargoHub.Domain.Billing;
using CargoHub.Domain.Bookings;
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
    public async Task GetBreakdownForDateRangeAsync_ReturnsNull_WhenCompanyMissing()
    {
        using var ctx = _fixture.CreateContext();
        var reader = new BillingMonthBreakdownReader(ctx, new NoOpBillingPeriodRegenerationService());
        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var r = await reader.GetBreakdownForDateRangeAsync(Guid.NewGuid(), start, end, default);
        Assert.Null(r);
    }

    [Fact]
    public async Task GetBreakdownForDateRangeAsync_ReturnsNull_WhenStartAfterEnd()
    {
        using var ctx = _fixture.CreateContext();
        var reader = new BillingMonthBreakdownReader(ctx, new NoOpBillingPeriodRegenerationService());
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "C",
            BusinessId = "b0",
            CompanyId = companyId.ToString("N"),
        });
        await ctx.SaveChangesAsync();

        var start = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc);
        var r = await reader.GetBreakdownForDateRangeAsync(companyId, start, end, default);
        Assert.Null(r);
    }

    [Fact]
    public async Task GetBreakdownForDateRangeAsync_ReturnsNull_WhenRangeExceeds731Days()
    {
        using var ctx = _fixture.CreateContext();
        var reader = new BillingMonthBreakdownReader(ctx, new NoOpBillingPeriodRegenerationService());
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "C",
            BusinessId = "b732",
            CompanyId = companyId.ToString("N"),
        });
        await ctx.SaveChangesAsync();

        var start = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddDays(732);
        var r = await reader.GetBreakdownForDateRangeAsync(companyId, start, end, default);
        Assert.Null(r);
    }

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

    [Fact]
    public async Task GetBreakdownAsync_ReturnsNull_WhenCompanyMissing()
    {
        using var ctx = _fixture.CreateContext();
        var reader = new BillingMonthBreakdownReader(ctx, new NoOpBillingPeriodRegenerationService());
        var r = await reader.GetBreakdownAsync(Guid.NewGuid(), 2025, 6, default);
        Assert.Null(r);
    }

    [Fact]
    public async Task GetBreakdownAsync_NoBookings_CreatesPeriod_AndReturnsZeros()
    {
        using var ctx = _fixture.CreateContext();
        var regen = new NoOpBillingPeriodRegenerationService();
        var reader = new BillingMonthBreakdownReader(ctx, regen);
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "Co",
            BusinessId = "biz-br",
            CompanyId = companyId.ToString("N"),
        });
        await ctx.SaveChangesAsync();

        var r = await reader.GetBreakdownAsync(companyId, 2026, 3, default);
        Assert.NotNull(r);
        Assert.Equal(companyId, r!.CompanyId);
        Assert.Equal(2026, r.YearUtc);
        Assert.Equal(3, r.MonthUtc);
        Assert.Equal(0, r.BillableBookingCount);
        Assert.Equal(0m, r.PayableTotal);
        Assert.Equal(0m, r.LedgerTotal);
        Assert.NotEqual(Guid.Empty, r.BillingPeriodId);
    }

    [Fact]
    public async Task GetBreakdownAsync_WithBillableBooking_InMonth_CountsBooking()
    {
        using var ctx = _fixture.CreateContext();
        var reader = new BillingMonthBreakdownReader(ctx, new NoOpBillingPeriodRegenerationService());
        var planId = Guid.NewGuid();
        var pricingId = Guid.NewGuid();
        ctx.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "Paygo",
            Kind = SubscriptionPlanKind.PayPerBooking,
            Currency = "EUR",
            IsActive = true,
        });
        ctx.SubscriptionPlanPricingPeriods.Add(new SubscriptionPlanPricingPeriod
        {
            Id = pricingId,
            SubscriptionPlanId = planId,
            EffectiveFromUtc = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ChargePerBooking = 2m,
        });
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "CoBk",
            BusinessId = "biz-bk",
            CompanyId = companyId.ToString("N"),
            SubscriptionPlanId = planId,
        });
        await ctx.SaveChangesAsync();

        var billableAt = new DateTime(2026, 8, 12, 10, 0, 0, DateTimeKind.Utc);
        ctx.Bookings.Add(MinimalBillableBooking(Guid.NewGuid(), companyId, billableAt));
        await ctx.SaveChangesAsync();

        var r = await reader.GetBreakdownAsync(companyId, 2026, 8, default);
        Assert.NotNull(r);
        Assert.Equal(1, r!.BillableBookingCount);
        Assert.Equal(2026, r.YearUtc);
        Assert.Equal(8, r.MonthUtc);
    }

    [Fact]
    public async Task GetBillableMonthsAsync_GroupsByFirstBillableMonth()
    {
        using var ctx = _fixture.CreateContext();
        var reader = new BillingMonthBreakdownReader(ctx, new NoOpBillingPeriodRegenerationService());
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "Co",
            BusinessId = "biz-bm",
            CompanyId = companyId.ToString("N"),
        });
        await ctx.SaveChangesAsync();

        var t = new DateTime(2025, 7, 15, 12, 0, 0, DateTimeKind.Utc);
        ctx.Bookings.Add(MinimalBillableBooking(Guid.NewGuid(), companyId, t));
        ctx.Bookings.Add(MinimalBillableBooking(Guid.NewGuid(), companyId, t.AddHours(1)));
        await ctx.SaveChangesAsync();

        var months = await reader.GetBillableMonthsAsync(companyId, default);
        Assert.Single(months);
        Assert.Equal(2025, months[0].YearUtc);
        Assert.Equal(7, months[0].MonthUtc);
        Assert.Equal(2, months[0].BillableBookingCount);
        Assert.Null(months[0].BillingPeriodId);
    }

    [Fact]
    public async Task GetBillableMonthsAsync_SetsBillingPeriodId_WhenOpenPeriodExistsForMonth()
    {
        using var ctx = _fixture.CreateContext();
        var reader = new BillingMonthBreakdownReader(ctx, new NoOpBillingPeriodRegenerationService());
        var companyId = Guid.NewGuid();
        var periodId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "CoP",
            BusinessId = "biz-per",
            CompanyId = companyId.ToString("N"),
        });
        ctx.CompanyBillingPeriods.Add(new CompanyBillingPeriod
        {
            Id = periodId,
            CompanyId = companyId,
            YearUtc = 2025,
            MonthUtc = 11,
            Currency = "EUR",
            Status = CompanyBillingPeriodStatus.Open,
        });
        await ctx.SaveChangesAsync();

        var t = new DateTime(2025, 11, 3, 8, 0, 0, DateTimeKind.Utc);
        ctx.Bookings.Add(MinimalBillableBooking(Guid.NewGuid(), companyId, t));
        await ctx.SaveChangesAsync();

        var months = await reader.GetBillableMonthsAsync(companyId, default);
        var row = Assert.Single(months);
        Assert.Equal(2025, row.YearUtc);
        Assert.Equal(11, row.MonthUtc);
        Assert.Equal(periodId, row.BillingPeriodId);
    }

    [Fact]
    public async Task GetBillableMonthsAsync_ReturnsEmpty_WhenNoBillableBookings()
    {
        using var ctx = _fixture.CreateContext();
        var reader = new BillingMonthBreakdownReader(ctx, new NoOpBillingPeriodRegenerationService());
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "CoE",
            BusinessId = "biz-empty",
            CompanyId = companyId.ToString("N"),
        });
        await ctx.SaveChangesAsync();

        var months = await reader.GetBillableMonthsAsync(companyId, default);
        Assert.Empty(months);
    }

    [Fact]
    public async Task GetBreakdownForDateRangeAsync_SingleMonthWithBooking_SetsBillingPeriodId()
    {
        using var ctx = _fixture.CreateContext();
        var reader = new BillingMonthBreakdownReader(ctx, new NoOpBillingPeriodRegenerationService());
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "CoRng",
            BusinessId = "biz-rng",
            CompanyId = companyId.ToString("N"),
        });
        await ctx.SaveChangesAsync();

        var t = new DateTime(2026, 2, 14, 0, 0, 0, DateTimeKind.Utc);
        ctx.Bookings.Add(MinimalBillableBooking(Guid.NewGuid(), companyId, t));
        await ctx.SaveChangesAsync();

        var start = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var r = await reader.GetBreakdownForDateRangeAsync(companyId, start, end, default);
        Assert.NotNull(r);
        Assert.Equal(1, r!.BillableBookingCount);
        Assert.NotNull(r.BillingPeriodId);
    }

    [Fact]
    public async Task GetBreakdownForDateRangeAsync_TwoMonthsWithBookings_TouchesBothPeriods()
    {
        using var ctx = _fixture.CreateContext();
        var reader = new BillingMonthBreakdownReader(ctx, new NoOpBillingPeriodRegenerationService());
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "Co2m",
            BusinessId = "biz-2m",
            CompanyId = companyId.ToString("N"),
        });
        await ctx.SaveChangesAsync();

        ctx.Bookings.Add(MinimalBillableBooking(Guid.NewGuid(), companyId, new DateTime(2026, 1, 5, 0, 0, 0, DateTimeKind.Utc)));
        ctx.Bookings.Add(MinimalBillableBooking(Guid.NewGuid(), companyId, new DateTime(2026, 2, 5, 0, 0, 0, DateTimeKind.Utc)));
        await ctx.SaveChangesAsync();

        var start = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var r = await reader.GetBreakdownForDateRangeAsync(companyId, start, end, default);
        Assert.NotNull(r);
        Assert.Equal(2, r!.BillableBookingCount);
        Assert.Null(r.BillingPeriodId);
    }

    [Fact]
    public async Task GetBreakdownAsync_AfterLineItemAdded_ReflectsPayableTotal()
    {
        using var ctx = _fixture.CreateContext();
        var reader = new BillingMonthBreakdownReader(ctx, new NoOpBillingPeriodRegenerationService());
        var planId = Guid.NewGuid();
        var pricingId = Guid.NewGuid();
        ctx.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "PaygoL",
            Kind = SubscriptionPlanKind.PayPerBooking,
            Currency = "EUR",
            IsActive = true,
        });
        ctx.SubscriptionPlanPricingPeriods.Add(new SubscriptionPlanPricingPeriod
        {
            Id = pricingId,
            SubscriptionPlanId = planId,
            EffectiveFromUtc = new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ChargePerBooking = 3m,
        });
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "CoLi",
            BusinessId = "biz-li",
            CompanyId = companyId.ToString("N"),
            SubscriptionPlanId = planId,
        });
        await ctx.SaveChangesAsync();

        var bookingId = Guid.NewGuid();
        var billAt = new DateTime(2026, 9, 2, 0, 0, 0, DateTimeKind.Utc);
        ctx.Bookings.Add(MinimalBillableBooking(bookingId, companyId, billAt));
        await ctx.SaveChangesAsync();

        var r1 = await reader.GetBreakdownAsync(companyId, 2026, 9, default);
        Assert.NotNull(r1);
        var periodId = r1!.BillingPeriodId!.Value;

        ctx.BillingLineItems.Add(new BillingLineItem
        {
            Id = Guid.NewGuid(),
            CompanyBillingPeriodId = periodId,
            LineType = BillingLineType.PerBooking,
            Amount = 15m,
            Currency = "EUR",
            SubscriptionPlanId = planId,
            SubscriptionPlanPricingPeriodId = pricingId,
            CreatedAtUtc = DateTime.UtcNow,
            ExcludedFromInvoice = false,
            BookingId = bookingId,
        });
        await ctx.SaveChangesAsync();

        var r2 = await reader.GetBreakdownAsync(companyId, 2026, 9, default);
        Assert.NotNull(r2);
        Assert.Equal(15m, r2!.PayableTotal);
        Assert.Equal(1, r2.BillableBookingCount);
    }

    private static Booking MinimalBillableBooking(Guid id, Guid companyId, DateTime firstBillableAtUtc)
    {
        var now = DateTime.UtcNow;
        return new Booking
        {
            Id = id,
            CustomerId = "cust-bm",
            CompanyId = companyId,
            IsDraft = false,
            IsTestBooking = false,
            Enabled = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            FirstBillableAtUtc = firstBillableAtUtc,
            Header = new BookingHeader { SenderId = "cust-bm" },
            Receiver = new BookingParty(),
            Shipper = new BookingParty(),
            PickUpAddress = new BookingParty(),
            DeliveryPoint = new BookingParty(),
            Shipment = new BookingShipment(),
            ShippingInfo = new ShippingInfo(),
        };
    }

    private sealed class NoOpBillingPeriodRegenerationService : IBillingPeriodRegenerationService
    {
        public Task RegenerateAsync(Guid companyBillingPeriodId, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;
    }
}
