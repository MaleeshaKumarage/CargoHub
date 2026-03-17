using CargoHub.Application.Bookings;
using CargoHub.Application.Bookings.Dtos;
using CargoHub.Application.Bookings.Queries;
using CargoHub.Domain.Bookings;
using Moq;
using Xunit;

namespace CargoHub.Tests.Bookings;

public class ListDraftsQueryHandlerTests
{
    private static Booking CreateDraft(Guid id, string customerId = "cust-1")
    {
        return new Booking
        {
            Id = id,
            CustomerId = customerId,
            CustomerName = "Customer",
            IsDraft = true,
            Header = new BookingHeader(),
            Receiver = new BookingParty(),
            Shipper = new BookingParty(),
            PickUpAddress = new BookingParty(),
            DeliveryPoint = new BookingParty(),
            Shipment = new BookingShipment(),
            ShippingInfo = new ShippingInfo(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task Handle_WithCustomerId_CallsListDraftsByCustomerId()
    {
        var d1 = CreateDraft(Guid.NewGuid());
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.ListDraftsByCustomerIdAsync("cust-1", 0, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking> { d1 });
        repo.Setup(r => r.GetStatusHistoryForBookingIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, List<BookingStatusEventDto>> { { d1.Id, new List<BookingStatusEventDto>() } });

        var handler = new ListDraftsQueryHandler(repo.Object);
        var result = await handler.Handle(new ListDraftsQuery("cust-1", 0, 100), default);

        Assert.Single(result);
        Assert.Equal(d1.Id, result[0].Id);
        Assert.True(result[0].IsDraft);
    }

    [Fact]
    public async Task Handle_WithoutCustomerId_CallsListAllDrafts()
    {
        var d1 = CreateDraft(Guid.NewGuid());
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.ListAllDraftsAsync(0, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking> { d1 });
        repo.Setup(r => r.GetStatusHistoryForBookingIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, List<BookingStatusEventDto>> { { d1.Id, new List<BookingStatusEventDto>() } });

        var handler = new ListDraftsQueryHandler(repo.Object);
        var result = await handler.Handle(new ListDraftsQuery(null, 0, 100), default);

        Assert.Single(result);
    }

    [Fact]
    public async Task Handle_WhenStatusHistoryMissing_ReturnsEmptyStatusHistory()
    {
        var d1 = CreateDraft(Guid.NewGuid());
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.ListDraftsByCustomerIdAsync("cust-1", 0, 100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking> { d1 });
        repo.Setup(r => r.GetStatusHistoryForBookingIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, List<BookingStatusEventDto>>());

        var handler = new ListDraftsQueryHandler(repo.Object);
        var result = await handler.Handle(new ListDraftsQuery("cust-1"), default);

        Assert.Single(result);
        Assert.Empty(result[0].StatusHistory);
    }
}
