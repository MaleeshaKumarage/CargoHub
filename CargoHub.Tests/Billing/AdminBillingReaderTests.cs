using CargoHub.Application.Billing;
using CargoHub.Application.Billing.Admin;
using CargoHub.Domain.Billing;
using CargoHub.Domain.Companies;
using CargoHub.Infrastructure.Billing;
using CargoHub.Infrastructure.Persistence;
using Moq;
using Xunit;
using CompanyEntity = CargoHub.Domain.Companies.Company;

namespace CargoHub.Tests.Billing;

public class AdminBillingReaderTests : IDisposable
{
    private readonly TestDbFixture _fixture;

    public AdminBillingReaderTests() => _fixture = new TestDbFixture();

    public void Dispose() => _fixture.Dispose();

    private static AdminBillingReader CreateReader(ApplicationDbContext ctx)
    {
        var breakMock = new Mock<IBillingMonthBreakdownReader>();
        breakMock
            .Setup(x => x.GetBreakdownAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingMonthBreakdownDto?)null);
        return new AdminBillingReader(ctx, breakMock.Object);
    }

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

        var reader = CreateReader(ctx);
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

        var reader = CreateReader(ctx);
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
        var reader = CreateReader(ctx);
        Assert.Null(await reader.GetBillingPeriodDetailAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetInvoicePdfModelAsync_ReturnsNull_WhenPeriodMissing()
    {
        using var ctx = _fixture.CreateContext();
        var reader = CreateReader(ctx);
        Assert.Null(await reader.GetInvoicePdfModelAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ListBillingPeriodsForCompany_ReturnsEmpty_WhenNone()
    {
        using var ctx = _fixture.CreateContext();
        var reader = CreateReader(ctx);
        var list = await reader.ListBillingPeriodsForCompanyAsync(Guid.NewGuid());
        Assert.Empty(list);
    }

    [Fact]
    public async Task GetInvoicePdfModelAsync_MapsBreakdownSegmentsAndBookings_WhenBreakdownReturned()
    {
        using var ctx = _fixture.CreateContext();
        var planId = Guid.NewGuid();
        var pricingId = Guid.NewGuid();
        ctx.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "P",
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
            CompanyId = "co-pdf",
            Name = "Invoice Co",
            BusinessId = "BIZ-PDF",
            CustomerId = "x",
            SubscriptionPlanId = planId,
        });
        var periodId = Guid.NewGuid();
        ctx.CompanyBillingPeriods.Add(new CompanyBillingPeriod
        {
            Id = periodId,
            CompanyId = companyId,
            YearUtc = 2026,
            MonthUtc = 4,
            Currency = "EUR",
            Status = CompanyBillingPeriodStatus.Open,
        });
        ctx.BillingLineItems.Add(new BillingLineItem
        {
            Id = Guid.NewGuid(),
            CompanyBillingPeriodId = periodId,
            LineType = BillingLineType.PerBooking,
            Amount = 11m,
            Currency = "EUR",
            SubscriptionPlanId = planId,
            SubscriptionPlanPricingPeriodId = pricingId,
            CreatedAtUtc = DateTime.UtcNow,
            ExcludedFromInvoice = false,
        });
        await ctx.SaveChangesAsync();

        var bookingId = Guid.NewGuid();
        var breakMock = new Mock<IBillingMonthBreakdownReader>();
        breakMock
            .Setup(x => x.GetBreakdownAsync(companyId, 2026, 4, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingMonthBreakdownDto
            {
                CompanyId = companyId,
                YearUtc = 2026,
                MonthUtc = 4,
                BillingPeriodId = periodId,
                RangeStartUtc = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                RangeEndExclusiveUtc = new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc),
                Currency = "EUR",
                BillableBookingCount = 1,
                PayableTotal = 11m,
                LedgerTotal = 11m,
                Segments =
                [
                    new BillingMonthSegmentDto
                    {
                        Label = "Unit",
                        BookingCount = 1,
                        UnitRate = 11m,
                        Subtotal = 11m,
                    },
                ],
                Bookings =
                [
                    new BillingMonthBookingRowDto
                    {
                        BookingId = bookingId,
                        ShipmentNumber = "SN-1",
                        Amount = 11m,
                        ExcludedFromInvoice = false,
                    },
                ],
            });

        var reader = new AdminBillingReader(ctx, breakMock.Object);
        var model = await reader.GetInvoicePdfModelAsync(periodId);
        Assert.NotNull(model);
        Assert.Single(model!.Segments);
        Assert.Equal("Unit", model.Segments[0].Label);
        Assert.Single(model.BookingRows);
        Assert.Equal("SN-1", model.BookingRows[0].Reference);
        breakMock.Verify(x => x.GetBreakdownAsync(companyId, 2026, 4, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc), model!.InvoiceRangeStartUtc);
        Assert.Equal(new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc), model.InvoiceRangeEndExclusiveUtc);
    }

    [Fact]
    public async Task GetInvoicePdfModelAsync_UsesBookingRowReferenceFallback_WhenNoShipmentOrReference()
    {
        using var ctx = _fixture.CreateContext();
        var planId = Guid.NewGuid();
        var pricingId = Guid.NewGuid();
        ctx.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "P2",
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
            CompanyId = "co-pdf2",
            Name = "Co",
            BusinessId = "B2",
            CustomerId = "x",
            SubscriptionPlanId = planId,
        });
        var periodId = Guid.NewGuid();
        ctx.CompanyBillingPeriods.Add(new CompanyBillingPeriod
        {
            Id = periodId,
            CompanyId = companyId,
            YearUtc = 2025,
            MonthUtc = 11,
            Currency = "EUR",
            Status = CompanyBillingPeriodStatus.Closed,
        });
        await ctx.SaveChangesAsync();

        var bookingId = Guid.Parse("a1b2c3d4-e5f6-4789-a012-3456789abcde");
        var breakMock = new Mock<IBillingMonthBreakdownReader>();
        breakMock
            .Setup(x => x.GetBreakdownAsync(companyId, 2025, 11, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingMonthBreakdownDto
            {
                CompanyId = companyId,
                YearUtc = 2025,
                MonthUtc = 11,
                BillingPeriodId = periodId,
                RangeStartUtc = new DateTime(2025, 11, 1, 0, 0, 0, DateTimeKind.Utc),
                RangeEndExclusiveUtc = new DateTime(2025, 12, 1, 0, 0, 0, DateTimeKind.Utc),
                Currency = "EUR",
                Segments = [],
                Bookings =
                [
                    new BillingMonthBookingRowDto
                    {
                        BookingId = bookingId,
                        ShipmentNumber = null,
                        ReferenceNumber = null,
                        Amount = 0m,
                        ExcludedFromInvoice = false,
                    },
                ],
            });

        var reader = new AdminBillingReader(ctx, breakMock.Object);
        var model = await reader.GetInvoicePdfModelAsync(periodId);
        Assert.NotNull(model);
        Assert.Single(model!.BookingRows);
        Assert.Equal(8, model.BookingRows[0].Reference?.Length);
    }

    [Fact]
    public async Task GetInvoicePdfModelAsync_WhenBreakdownNull_ReturnsLinesWithEmptySegmentsAndRows()
    {
        using var ctx = _fixture.CreateContext();
        var planId = Guid.NewGuid();
        var pricingId = Guid.NewGuid();
        ctx.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "P",
            Kind = SubscriptionPlanKind.Trial,
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
            CompanyId = "co-nb",
            Name = "No Breakdown Co",
            BusinessId = "B-NB",
            CustomerId = "x",
            SubscriptionPlanId = planId,
        });
        var periodId = Guid.NewGuid();
        ctx.CompanyBillingPeriods.Add(new CompanyBillingPeriod
        {
            Id = periodId,
            CompanyId = companyId,
            YearUtc = 2026,
            MonthUtc = 6,
            Currency = "EUR",
            Status = CompanyBillingPeriodStatus.Open,
        });
        ctx.BillingLineItems.Add(new BillingLineItem
        {
            Id = Guid.NewGuid(),
            CompanyBillingPeriodId = periodId,
            LineType = BillingLineType.PerBooking,
            Amount = 3m,
            Currency = "EUR",
            SubscriptionPlanId = planId,
            SubscriptionPlanPricingPeriodId = pricingId,
            CreatedAtUtc = DateTime.UtcNow,
            ExcludedFromInvoice = false,
        });
        await ctx.SaveChangesAsync();

        var breakMock = new Mock<IBillingMonthBreakdownReader>();
        breakMock
            .Setup(x => x.GetBreakdownAsync(companyId, 2026, 6, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingMonthBreakdownDto?)null);

        var reader = new AdminBillingReader(ctx, breakMock.Object);
        var model = await reader.GetInvoicePdfModelAsync(periodId);
        Assert.NotNull(model);
        Assert.Single(model!.Lines);
        Assert.Empty(model.Segments);
        Assert.Empty(model.BookingRows);
        Assert.Equal(3m, model.PayableTotal);
    }

    [Fact]
    public async Task GetInvoicePdfModelAsync_CustomRange_ReturnsNull_WhenOnlyOneBoundProvided()
    {
        using var ctx = _fixture.CreateContext();
        var (periodId, _) = await SeedMinimalOpenPeriodAsync(ctx);
        var breakMock = new Mock<IBillingMonthBreakdownReader>();
        var reader = new AdminBillingReader(ctx, breakMock.Object);

        var start = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        Assert.Null(await reader.GetInvoicePdfModelAsync(periodId, default, start, null));
        Assert.Null(await reader.GetInvoicePdfModelAsync(periodId, default, null, start.AddDays(1)));
        breakMock.Verify(
            x => x.GetBreakdownForDateRangeAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task GetInvoicePdfModelAsync_CustomRange_ReturnsNull_WhenKindNotUtc()
    {
        using var ctx = _fixture.CreateContext();
        var (periodId, _) = await SeedMinimalOpenPeriodAsync(ctx);
        var breakMock = new Mock<IBillingMonthBreakdownReader>();
        var reader = new AdminBillingReader(ctx, breakMock.Object);

        var start = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 4, 10, 0, 0, 0, DateTimeKind.Utc);
        var badStart = DateTime.SpecifyKind(start, DateTimeKind.Unspecified);
        Assert.Null(await reader.GetInvoicePdfModelAsync(periodId, default, badStart, end));
    }

    [Fact]
    public async Task GetInvoicePdfModelAsync_CustomRange_ReturnsNull_WhenOutsideBillingMonth()
    {
        using var ctx = _fixture.CreateContext();
        var (periodId, _) = await SeedMinimalOpenPeriodAsync(ctx);
        var breakMock = new Mock<IBillingMonthBreakdownReader>();
        var reader = new AdminBillingReader(ctx, breakMock.Object);

        var marchStart = new DateTime(2026, 3, 28, 0, 0, 0, DateTimeKind.Utc);
        var april5 = new DateTime(2026, 4, 5, 0, 0, 0, DateTimeKind.Utc);
        Assert.Null(await reader.GetInvoicePdfModelAsync(periodId, default, marchStart, april5));

        var april1 = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var may2 = new DateTime(2026, 5, 2, 0, 0, 0, DateTimeKind.Utc);
        Assert.Null(await reader.GetInvoicePdfModelAsync(periodId, default, april1, may2));

        var april20 = new DateTime(2026, 4, 20, 0, 0, 0, DateTimeKind.Utc);
        Assert.Null(await reader.GetInvoicePdfModelAsync(periodId, default, april20, april1));
    }

    [Fact]
    public async Task GetInvoicePdfModelAsync_CustomRange_ReturnsNull_WhenBreakdownReaderReturnsNull()
    {
        using var ctx = _fixture.CreateContext();
        var (periodId, companyId) = await SeedMinimalOpenPeriodAsync(ctx);
        var breakMock = new Mock<IBillingMonthBreakdownReader>();
        breakMock
            .Setup(x => x.GetBreakdownForDateRangeAsync(companyId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingMonthBreakdownDto?)null);
        var reader = new AdminBillingReader(ctx, breakMock.Object);

        var start = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc);
        Assert.Null(await reader.GetInvoicePdfModelAsync(periodId, default, start, end));
    }

    [Fact]
    public async Task GetInvoicePdfModelAsync_CustomRange_FiltersLines_ToBookingsInBreakdown()
    {
        using var ctx = _fixture.CreateContext();
        var planId = Guid.NewGuid();
        var pricingId = Guid.NewGuid();
        ctx.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "P",
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
            CompanyId = "co-cr",
            Name = "Custom Range Co",
            BusinessId = "B-CR",
            CustomerId = "x",
            SubscriptionPlanId = planId,
        });
        var periodId = Guid.NewGuid();
        ctx.CompanyBillingPeriods.Add(new CompanyBillingPeriod
        {
            Id = periodId,
            CompanyId = companyId,
            YearUtc = 2026,
            MonthUtc = 4,
            Currency = "EUR",
            Status = CompanyBillingPeriodStatus.Open,
        });
        var bIn = Guid.NewGuid();
        var bOut = Guid.NewGuid();
        ctx.BillingLineItems.Add(new BillingLineItem
        {
            Id = Guid.NewGuid(),
            CompanyBillingPeriodId = periodId,
            BookingId = bIn,
            LineType = BillingLineType.PerBooking,
            Amount = 10m,
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
            BookingId = bOut,
            LineType = BillingLineType.PerBooking,
            Amount = 99m,
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
            BookingId = null,
            LineType = BillingLineType.Adjustment,
            Amount = 2m,
            Currency = "EUR",
            SubscriptionPlanId = planId,
            SubscriptionPlanPricingPeriodId = pricingId,
            CreatedAtUtc = DateTime.UtcNow,
            ExcludedFromInvoice = false,
        });
        await ctx.SaveChangesAsync();

        var breakMock = new Mock<IBillingMonthBreakdownReader>();
        breakMock
            .Setup(x => x.GetBreakdownForDateRangeAsync(companyId, It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingMonthBreakdownDto
            {
                CompanyId = companyId,
                YearUtc = 2026,
                MonthUtc = 4,
                BillingPeriodId = periodId,
                RangeStartUtc = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc),
                RangeEndExclusiveUtc = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc),
                Currency = "EUR",
                Segments = [],
                Bookings =
                [
                    new BillingMonthBookingRowDto
                    {
                        BookingId = bIn,
                        Amount = 10m,
                        ExcludedFromInvoice = false,
                    },
                ],
            });

        var reader = new AdminBillingReader(ctx, breakMock.Object);
        var start = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 4, 15, 0, 0, 0, DateTimeKind.Utc);
        var model = await reader.GetInvoicePdfModelAsync(periodId, default, start, end);
        Assert.NotNull(model);
        Assert.Equal(2, model!.Lines.Count);
        Assert.Contains(model.Lines, l => l.Amount == 10m);
        Assert.Contains(model.Lines, l => l.Amount == 2m);
        Assert.DoesNotContain(model.Lines, l => l.Amount == 99m);
        Assert.Equal(12m, model.LedgerTotal);
        Assert.Equal(12m, model.PayableTotal);
    }

    private static async Task<(Guid PeriodId, Guid CompanyId)> SeedMinimalOpenPeriodAsync(ApplicationDbContext ctx)
    {
        var planId = Guid.NewGuid();
        var pricingId = Guid.NewGuid();
        ctx.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "Seed",
            Kind = SubscriptionPlanKind.Trial,
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
            CompanyId = "co-seed",
            Name = "Seed Co",
            BusinessId = "B-SEED",
            CustomerId = "x",
            SubscriptionPlanId = planId,
        });
        var periodId = Guid.NewGuid();
        ctx.CompanyBillingPeriods.Add(new CompanyBillingPeriod
        {
            Id = periodId,
            CompanyId = companyId,
            YearUtc = 2026,
            MonthUtc = 4,
            Currency = "EUR",
            Status = CompanyBillingPeriodStatus.Open,
        });
        await ctx.SaveChangesAsync();
        return (periodId, companyId);
    }
}
