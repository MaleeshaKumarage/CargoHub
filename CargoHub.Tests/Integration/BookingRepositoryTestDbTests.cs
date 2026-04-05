using CargoHub.Application.Bookings;
using CargoHub.Application.Bookings.Commands;
using CargoHub.Application.Bookings.Dtos;
using CargoHub.Domain.Bookings;
using CargoHub.Infrastructure.Billing;
using CargoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CargoHub.Tests.Integration;

/// <summary>
/// Integration tests using the in-memory test database. Verify that bookings are stored with CompanyId and can be read back.
/// </summary>
public class BookingRepositoryTestDbTests : IDisposable
{
    private readonly TestDbFixture _fixture;

    public BookingRepositoryTestDbTests()
    {
        _fixture = new TestDbFixture();
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task AddBooking_WithCompanyId_PersistsAndCanBeReadBack()
    {
        // When we add a booking with a company id, it is saved and we can load it with the same company id.
        var companyId = Guid.NewGuid();
        var customerId = "cust-1";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "Receiver",
            ReceiverAddress1 = "Addr 1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI"
        };
        var handler = new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context));
        var result = await handler.Handle(new CreateBookingCommand(customerId, "Customer", request, companyId), default);

        Assert.NotNull(result);
        var loaded = await repo.GetByIdAsync(result.Id, default);
        Assert.NotNull(loaded);
        Assert.Equal(companyId, loaded.CompanyId);
        Assert.False(loaded.IsDraft);
    }

    [Fact]
    public async Task AddDraft_WithCompanyId_PersistsAndAppearsInDraftList()
    {
        // When we add a draft with a company id, it is saved and appears when we list drafts for that customer.
        var companyId = Guid.NewGuid();
        var customerId = "cust-draft";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI"
        };
        var handler = new CreateDraftCommandHandler(repo);
        var result = await handler.Handle(new CreateDraftCommand(customerId, "C", request, companyId), default);

        Assert.NotNull(result);
        var drafts = await repo.ListDraftsByCustomerIdAsync(customerId, 0, 10, default);
        Assert.Single(drafts);
        Assert.Equal(companyId, drafts[0].CompanyId);
        Assert.True(drafts[0].IsDraft);
    }

    [Fact]
    public async Task ListBookings_ReturnsOnlyCompleted_NotDrafts()
    {
        // When we list bookings by customer, only completed bookings are returned; drafts are separate.
        var customerId = "cust-list";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI"
        };
        await new CreateDraftCommandHandler(repo).Handle(new CreateDraftCommand(customerId, "C", request, Guid.NewGuid()), default);
        var completed = await new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context)).Handle(new CreateBookingCommand(customerId, "C", request, Guid.NewGuid()), default);
        Assert.NotNull(completed);

        var list = await repo.ListByCustomerIdAsync(customerId, 0, 10, null, default);
        Assert.Single(list);
        Assert.Equal(completed.Id, list[0].Id);
        Assert.False(list[0].IsDraft);
    }

    [Fact]
    public async Task GetById_WhenBookingDoesNotExist_ReturnsNull()
    {
        // When we ask for a booking that does not exist, the repository returns null.
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var loaded = await repo.GetByIdAsync(Guid.NewGuid(), default);
        Assert.Null(loaded);
    }

    [Fact]
    public async Task ConfirmDraft_WhenDraftExists_ConvertsToCompleted()
    {
        var customerId = "cust-confirm";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI"
        };
        var draftResult = await new CreateDraftCommandHandler(repo).Handle(new CreateDraftCommand(customerId, "C", request, Guid.NewGuid()), default);
        Assert.NotNull(draftResult);

        var confirmed = await repo.ConfirmDraftAsync(draftResult.Id, customerId, default);
        Assert.True(confirmed);

        var loaded = await repo.GetByIdAsync(draftResult.Id, default);
        Assert.NotNull(loaded);
        Assert.False(loaded.IsDraft);
        Assert.True(loaded.Enabled);
    }

    [Fact]
    public async Task TryAddStatusEvent_WhenStatusExists_ReturnsFalse()
    {
        var customerId = "cust-tryadd";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI"
        };
        var result = await new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context)).Handle(new CreateBookingCommand(customerId, "C", request, Guid.NewGuid()), default);
        Assert.NotNull(result);

        var first = await repo.TryAddStatusEventAsync(result.Id, "CustomStatus", "test", default);
        Assert.True(first);
        var second = await repo.TryAddStatusEventAsync(result.Id, "CustomStatus", "test", default);
        Assert.False(second);
    }

    [Fact]
    public async Task GetStatusHistory_ReturnsEventsForBooking()
    {
        var customerId = "cust-status";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI"
        };
        var result = await new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context)).Handle(new CreateBookingCommand(customerId, "C", request, Guid.NewGuid()), default);
        Assert.NotNull(result);

        var history = await repo.GetStatusHistoryAsync(result.Id, default);
        Assert.NotEmpty(history);
        Assert.Contains(history, h => h.Status == CargoHub.Domain.Bookings.BookingStatus.CompletedBooking);
    }

    [Fact]
    public async Task GetDashboardStats_ReturnsCounts()
    {
        var customerId = "cust-stats";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI"
        };
        await new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context)).Handle(new CreateBookingCommand(customerId, "C", request, Guid.NewGuid()), default);

        var stats = await repo.GetDashboardStatsAsync(customerId, default);
        Assert.True(stats.CountToday >= 1);
        Assert.True(stats.CountMonth >= 1);
        Assert.True(stats.CountYear >= 1);
    }

    [Fact]
    public async Task GetDashboardStats_WithNullCustomerId_ReturnsAllBookingsStats()
    {
        var customerId = "cust-all";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI"
        };
        await new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context)).Handle(new CreateBookingCommand(customerId, "C", request, Guid.NewGuid()), default);

        var stats = await repo.GetDashboardStatsAsync(null, default);
        Assert.True(stats.CountToday >= 1);
    }

    [Fact]
    public async Task GetDashboardStats_NormalizesScopeWhitespace_AndUnknownScopeUsesCompletedOnly()
    {
        var customerId = "cust-dash-scope";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI"
        };
        await new CreateDraftCommandHandler(repo).Handle(new CreateDraftCommand(customerId, "C", request, Guid.NewGuid()), default);
        await new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context)).Handle(new CreateBookingCommand(customerId, "C", request, Guid.NewGuid()), default);

        var draftStats = await repo.GetDashboardStatsAsync(customerId, "  DrAfTs  ", null, null, default);
        Assert.Equal("drafts", draftStats.Scope);
        Assert.True(draftStats.CountMonth >= 1);

        var odd = await repo.GetDashboardStatsAsync(customerId, " CustomScope ", null, null, default);
        Assert.Equal("customscope", odd.Scope);
        Assert.True(odd.CountMonth >= 1);
    }

    [Fact]
    public async Task GetDashboardStats_WithDraftsScope_CountsOnlyDraftBookings()
    {
        var customerId = "cust-dash-drafts";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI"
        };
        await new CreateDraftCommandHandler(repo).Handle(new CreateDraftCommand(customerId, "C", request, Guid.NewGuid()), default);
        await new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context)).Handle(new CreateBookingCommand(customerId, "C", request, Guid.NewGuid()), default);

        var draftStats = await repo.GetDashboardStatsAsync(customerId, "drafts", null, null, default);
        Assert.True(draftStats.CountMonth >= 1);
        Assert.Equal("drafts", draftStats.Scope);

        var allStats = await repo.GetDashboardStatsAsync(customerId, "all", null, null, default);
        Assert.True(allStats.CountMonth >= 1);
        Assert.Equal("all", allStats.Scope);
    }

    [Fact]
    public async Task GetDashboardStats_WithExplicitHeatmapMonth_HasExpectedDaySlots()
    {
        var customerId = "cust-heatmap-feb";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI"
        };
        await new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context)).Handle(new CreateBookingCommand(customerId, "C", request, Guid.NewGuid()), default);

        var stats = await repo.GetDashboardStatsAsync(customerId, null, 2024, 2, default);
        Assert.Equal(29, stats.BookingsPerDayCurrentMonth.Count);
        Assert.Equal(29, stats.CompletedBookingsPerDayCurrentMonth.Count);
        Assert.Equal(29, stats.DraftsPerDayCurrentMonth.Count);
    }

    [Fact]
    public async Task GetDashboardStats_InvalidHeatmapMonth_FallsBackToCurrentMonth()
    {
        var customerId = "cust-heat-invalid";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI"
        };
        await new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context)).Handle(new CreateBookingCommand(customerId, "C", request, Guid.NewGuid()), default);

        var now = DateTime.UtcNow;
        var stats = await repo.GetDashboardStatsAsync(customerId, null, now.Year, 13, default);
        Assert.Equal(DateTime.DaysInMonth(now.Year, now.Month), stats.BookingsPerDayCurrentMonth.Count);
    }

    [Fact]
    public async Task GetDashboardStats_FutureHeatmapMonth_ClampsToCurrentMonth()
    {
        var customerId = "cust-heat-future";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI"
        };
        await new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context)).Handle(new CreateBookingCommand(customerId, "C", request, Guid.NewGuid()), default);

        var now = DateTime.UtcNow;
        var stats = await repo.GetDashboardStatsAsync(customerId, null, now.Year + 1, 6, default);
        Assert.Equal(DateTime.DaysInMonth(now.Year, now.Month), stats.BookingsPerDayCurrentMonth.Count);
    }

    [Fact]
    public async Task GetDashboardStats_WithDeliveredHistory_PopulatesDeliveryTimeDistribution()
    {
        var customerId = "cust-dash-delivery";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI"
        };
        var created = await new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context)).Handle(new CreateBookingCommand(customerId, "C", request, Guid.NewGuid()), default);
        Assert.NotNull(created);
        var booking = await context.Bookings.AsNoTracking().FirstAsync(b => b.Id == created.Id);
        context.BookingStatusHistory.Add(new BookingStatusHistory
        {
            Id = Guid.NewGuid(),
            BookingId = booking.Id,
            Status = BookingStatus.Delivered,
            OccurredAtUtc = booking.CreatedAtUtc.AddHours(12)
        });
        await context.SaveChangesAsync();

        var stats = await repo.GetDashboardStatsAsync(customerId, "all", null, null, default);
        Assert.True(stats.DeliveryTime.SampleSize >= 1);
        Assert.Equal(12.0, stats.DeliveryTime.MedianHours, precision: 5);
    }

    [Fact]
    public async Task GetDashboardStats_WithTwoDeliverySamples_UsesInterpolatedPercentiles()
    {
        var customerId = "cust-dash-delivery-2";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI"
        };
        var a = await new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context)).Handle(new CreateBookingCommand(customerId, "C", request, Guid.NewGuid()), default);
        var b = await new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context)).Handle(new CreateBookingCommand(customerId, "C", request, Guid.NewGuid()), default);
        Assert.NotNull(a);
        Assert.NotNull(b);
        var ba = await context.Bookings.AsNoTracking().FirstAsync(x => x.Id == a.Id);
        var bb = await context.Bookings.AsNoTracking().FirstAsync(x => x.Id == b.Id);
        context.BookingStatusHistory.AddRange(
            new BookingStatusHistory
            {
                Id = Guid.NewGuid(),
                BookingId = ba.Id,
                Status = BookingStatus.Delivered,
                OccurredAtUtc = ba.CreatedAtUtc.AddHours(10)
            },
            new BookingStatusHistory
            {
                Id = Guid.NewGuid(),
                BookingId = bb.Id,
                Status = BookingStatus.Delivered,
                OccurredAtUtc = bb.CreatedAtUtc.AddHours(20)
            });
        await context.SaveChangesAsync();

        var stats = await repo.GetDashboardStatsAsync(customerId, "all", null, null, default);
        Assert.Equal(2, stats.DeliveryTime.SampleSize);
        Assert.Equal(10.0, stats.DeliveryTime.MinHours, precision: 5);
        Assert.Equal(20.0, stats.DeliveryTime.MaxHours, precision: 5);
        Assert.InRange(stats.DeliveryTime.MedianHours, 10.0, 20.0);
    }

    [Fact]
    public async Task GetDashboardStats_PossiblyStuckCount_IncludesOldCompletedWithoutDelivered()
    {
        var customerId = "cust-dash-stuck";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI"
        };
        var created = await new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context)).Handle(new CreateBookingCommand(customerId, "C", request, Guid.NewGuid()), default);
        Assert.NotNull(created);
        var b = await context.Bookings.FirstAsync(x => x.Id == created.Id);
        b.CreatedAtUtc = DateTime.UtcNow.AddDays(-10);
        await context.SaveChangesAsync();

        var stats = await repo.GetDashboardStatsAsync(customerId, "all", null, null, default);
        Assert.True(stats.Kpi.PossiblyStuckCount >= 1);
    }

    [Fact]
    public async Task GetDashboardStats_ExceptionHeatmap_CountsSignalStatusesOnUpdates()
    {
        var customerId = "cust-dash-exc";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI"
        };
        var created = await new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context)).Handle(new CreateBookingCommand(customerId, "C", request, Guid.NewGuid()), default);
        Assert.NotNull(created);
        var b = await repo.GetByIdWithTrackingAsync(created.Id, customerId, default);
        Assert.NotNull(b);
        b!.Updates.Add(new BookingUpdate
        {
            Status = "Carrier reported delay",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1)
        });
        await repo.UpdateAsync(b, default);

        var stats = await repo.GetDashboardStatsAsync(customerId, "all", null, null, default);
        Assert.True(stats.ExceptionSignalsHeatmap.MaxCount >= 1);
    }

    [Fact]
    public async Task ListByCustomerId_WithFilter_AppliesSearchAndDateRange()
    {
        var customerId = "cust-filter";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI"
        };
        var r1 = await new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context)).Handle(new CreateBookingCommand(customerId, "CustomerA", request, Guid.NewGuid()), default);
        var r2 = await new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context)).Handle(new CreateBookingCommand(customerId, "CustomerB", request, Guid.NewGuid()), default);
        Assert.NotNull(r1);
        Assert.NotNull(r2);

        var b1 = await repo.GetByIdWithTrackingAsync(r1.Id, customerId, default);
        var b2 = await repo.GetByIdWithTrackingAsync(r2.Id, customerId, default);
        Assert.NotNull(b1);
        Assert.NotNull(b2);
        b1.ShipmentNumber = "SHIP-ALPHA";
        b1.CustomerName = "CustomerA";
        b2.ShipmentNumber = "SHIP-BETA";
        b2.CustomerName = "CustomerB";
        await repo.UpdateAsync(b1, default);
        await repo.UpdateAsync(b2, default);

        var from = DateTime.UtcNow.AddHours(-1);
        var to = DateTime.UtcNow.AddHours(1);
        var filter = new BookingListFilter(Search: "ALPHA", CreatedFrom: from, CreatedTo: to, Enabled: null);
        var list = await repo.ListByCustomerIdAsync(customerId, 0, 10, filter, default);
        Assert.Single(list);
        Assert.Equal("SHIP-ALPHA", list[0].ShipmentNumber);
    }

    [Fact]
    public async Task ListByCustomerId_WithEnabledFilter_ReturnsOnlyMatching()
    {
        var customerId = "cust-enabled";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI"
        };
        var r1 = await new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context)).Handle(new CreateBookingCommand(customerId, "C", request, Guid.NewGuid()), default);
        Assert.NotNull(r1);
        var b1 = await repo.GetByIdWithTrackingAsync(r1.Id, customerId, default);
        Assert.NotNull(b1);
        b1.Enabled = false;
        await repo.UpdateAsync(b1, default);

        var enabledOnly = await repo.ListByCustomerIdAsync(customerId, 0, 10, new BookingListFilter(Enabled: true), default);
        var disabledOnly = await repo.ListByCustomerIdAsync(customerId, 0, 10, new BookingListFilter(Enabled: false), default);
        Assert.Empty(enabledOnly);
        Assert.Single(disabledOnly);
    }

    [Fact]
    public async Task ListByCompanyCreatedUtcRange_IncludesDrafts_ExcludesTestsAndRespectsHalfOpenInterval()
    {
        var companyId = Guid.NewGuid();
        var customerId = "cust-digest-range";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI"
        };
        await new CreateDraftCommandHandler(repo).Handle(new CreateDraftCommand(customerId, "C", request, companyId), default);
        var completed = await new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context)).Handle(new CreateBookingCommand(customerId, "C", request, companyId), default);
        Assert.NotNull(completed);

        var testBooking = await new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context)).Handle(new CreateBookingCommand(customerId, "C", request, companyId), default);
        Assert.NotNull(testBooking);
        var tb = await repo.GetByIdWithTrackingAsync(testBooking.Id, customerId, default);
        Assert.NotNull(tb);
        tb.IsTestBooking = true;
        await repo.UpdateAsync(tb, default);

        var from = DateTime.UtcNow.AddHours(-2);
        var to = DateTime.UtcNow.AddHours(2);
        var list = await repo.ListByCompanyCreatedUtcRangeAsync(companyId, from, to, default);
        Assert.Equal(2, list.Count);
        Assert.Contains(list, b => b.IsDraft);
        Assert.Contains(list, b => !b.IsDraft && !b.IsTestBooking);
        Assert.DoesNotContain(list, b => b.IsTestBooking);
    }

    [Fact]
    public async Task UpdateDraft_WithShippingInfoAndPackages_UpdatesCorrectly()
    {
        var customerId = "cust-update";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI"
        };
        var draftResult = await new CreateDraftCommandHandler(repo).Handle(new CreateDraftCommand(customerId, "C", request, Guid.NewGuid()), default);
        Assert.NotNull(draftResult);

        var updateRequest = new UpdateDraftRequest
        {
            ReferenceNumber = "REF-001",
            PostalService = "DHL",
            ReceiverName = "Updated R",
            ReceiverAddress1 = "New St",
            ReceiverPostalCode = "00200",
            ReceiverCity = "Espoo",
            ReceiverCountry = "FI",
            ShippingInfo = new CreateBookingShippingInfoDto
            {
                GrossWeight = "10",
                Packages = new List<CreateBookingPackageDto> { new() { Weight = "5", Description = "Box" } }
            }
        };
        var updateHandler = new UpdateDraftCommandHandler(repo);
        var updated = await updateHandler.Handle(new UpdateDraftCommand(draftResult.Id, customerId, updateRequest), default);

        Assert.NotNull(updated);
        Assert.Equal("REF-001", updated.Header.ReferenceNumber);
        Assert.Equal("Updated R", updated.Receiver?.Name);
        Assert.Single(updated.Packages);
    }

    [Fact]
    public async Task ConfirmDraftAsync_ReturnsFalse_WhenBookingMissing()
    {
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        Assert.False(await repo.ConfirmDraftAsync(Guid.NewGuid(), "cust", default));
    }

    [Fact]
    public async Task ConfirmDraftAsync_ReturnsFalse_WhenAlreadyCompleted()
    {
        var customerId = "cust-conf";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI",
        };
        var done = await new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context))
            .Handle(new CreateBookingCommand(customerId, "C", request, Guid.NewGuid()), default);
        Assert.NotNull(done);
        Assert.False(await repo.ConfirmDraftAsync(done.Id, customerId, default));
    }

    [Fact]
    public async Task GetByIdWithTracking_ReturnsNull_WhenCustomerIdMismatch()
    {
        var customerId = "cust-id";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI",
        };
        var done = await new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context))
            .Handle(new CreateBookingCommand(customerId, "C", request, Guid.NewGuid()), default);
        Assert.NotNull(done);
        Assert.Null(await repo.GetByIdWithTrackingAsync(done.Id, "other", default));
    }

    [Fact]
    public async Task TryAddStatusEvent_ReturnsFalse_OnDuplicate()
    {
        var customerId = "cust-dup-st";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI",
        };
        var done = await new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context))
            .Handle(new CreateBookingCommand(customerId, "C", request, Guid.NewGuid()), default);
        Assert.NotNull(done);
        var first = await repo.TryAddStatusEventAsync(done.Id, "Custom", "t", default);
        Assert.True(first);
        var second = await repo.TryAddStatusEventAsync(done.Id, "Custom", "t", default);
        Assert.False(second);
    }

    [Fact]
    public async Task GetStatusHistoryForBookingIds_ReturnsEmptyDict_WhenNoIds()
    {
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var map = await repo.GetStatusHistoryForBookingIdsAsync(Array.Empty<Guid>(), default);
        Assert.Empty(map);
    }

    [Fact]
    public async Task GetStatusHistoryForBookingIds_GroupsByBooking()
    {
        var customerId = "cust-bulk-hist";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI",
        };
        var a = await new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context))
            .Handle(new CreateBookingCommand(customerId, "C", request, Guid.NewGuid()), default);
        var b = await new CreateBookingCommandHandler(repo, new SubscriptionBillingOrchestrator(context))
            .Handle(new CreateBookingCommand(customerId, "C", request, Guid.NewGuid()), default);
        Assert.NotNull(a);
        Assert.NotNull(b);
        await repo.TryAddStatusEventAsync(a.Id, "CustomA", "s", default);
        await repo.TryAddStatusEventAsync(b.Id, "CustomB", "s", default);

        var map = await repo.GetStatusHistoryForBookingIdsAsync(new[] { a.Id, b.Id }, default);
        Assert.True(map[a.Id].Any(x => x.Status == "CustomA"));
        Assert.True(map[b.Id].Any(x => x.Status == "CustomB"));
    }

    [Fact]
    public async Task ListAllDrafts_ReturnsDraftRows()
    {
        var customerId = "cust-all-drafts";
        using var context = _fixture.CreateContext();
        var repo = new BookingRepository(context);
        var request = new CreateBookingRequest
        {
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverPostalCode = "00100",
            ReceiverCity = "Helsinki",
            ReceiverCountry = "FI",
        };
        await new CreateDraftCommandHandler(repo).Handle(new CreateDraftCommand(customerId, "C", request, Guid.NewGuid()), default);
        var drafts = await repo.ListAllDraftsAsync(0, 50, default);
        Assert.Contains(drafts, d => d.CustomerId == customerId);
    }
}
