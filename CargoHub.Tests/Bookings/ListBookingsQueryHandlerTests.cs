using CargoHub.Application.Bookings;
using CargoHub.Application.Bookings.Dtos;
using CargoHub.Application.Bookings.Queries;
using CargoHub.Domain.Bookings;
using Moq;
using Xunit;

namespace CargoHub.Tests.Bookings;

public class ListBookingsQueryHandlerTests
{
    private static Booking CreateBooking(Guid id, string customerId = "cust-1")
    {
        return new Booking
        {
            Id = id,
            CustomerId = customerId,
            CustomerName = "Customer",
            IsDraft = false,
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
    public async Task Handle_WithCustomerId_CallsListByCustomerId()
    {
        var b1 = CreateBooking(Guid.NewGuid());
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.ListByCustomerIdAsync("cust-1", 0, 100, It.IsAny<BookingListFilter?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking> { b1 });
        repo.Setup(r => r.GetStatusHistoryForBookingIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, List<BookingStatusEventDto>> { { b1.Id, new List<BookingStatusEventDto>() } });

        var handler = new ListBookingsQueryHandler(repo.Object);
        var result = await handler.Handle(new ListBookingsQuery("cust-1", 0, 100), default);

        Assert.Single(result);
        Assert.Equal(b1.Id, result[0].Id);
        Assert.False(result[0].IsDraft);
    }

    [Fact]
    public async Task Handle_WithoutCustomerId_CallsListAll()
    {
        var b1 = CreateBooking(Guid.NewGuid());
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.ListAllAsync(0, 100, It.IsAny<BookingListFilter?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking> { b1 });
        repo.Setup(r => r.GetStatusHistoryForBookingIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, List<BookingStatusEventDto>> { { b1.Id, new List<BookingStatusEventDto>() } });

        var handler = new ListBookingsQueryHandler(repo.Object);
        var result = await handler.Handle(new ListBookingsQuery(null, 0, 100), default);

        Assert.Single(result);
    }

    [Fact]
    public async Task Handle_WhenListEmpty_ReturnsEmptyWithEmptyStatusHistory()
    {
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.ListByCustomerIdAsync("cust-empty", 0, 100, It.IsAny<BookingListFilter?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking>());
        repo.Setup(r => r.GetStatusHistoryForBookingIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, List<BookingStatusEventDto>>());

        var handler = new ListBookingsQueryHandler(repo.Object);
        var result = await handler.Handle(new ListBookingsQuery("cust-empty"), default);

        Assert.Empty(result);
    }

    [Fact]
    public async Task Handle_WithStatusHistory_MergesStatusHistory()
    {
        var b1 = CreateBooking(Guid.NewGuid());
        var statusEvent = new BookingStatusEventDto { Status = "Completed", OccurredAtUtc = DateTime.UtcNow };
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.ListByCustomerIdAsync("cust-1", 0, 100, It.IsAny<BookingListFilter?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking> { b1 });
        repo.Setup(r => r.GetStatusHistoryForBookingIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, List<BookingStatusEventDto>> { { b1.Id, new List<BookingStatusEventDto> { statusEvent } } });

        var handler = new ListBookingsQueryHandler(repo.Object);
        var result = await handler.Handle(new ListBookingsQuery("cust-1"), default);

        Assert.Single(result[0].StatusHistory);
        Assert.Equal("Completed", result[0].StatusHistory[0].Status);
    }

    [Fact]
    public async Task Handle_WhenStatusHistoryMissingForBooking_ReturnsEmptyList()
    {
        var b1 = CreateBooking(Guid.NewGuid());
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.ListByCustomerIdAsync("cust-1", 0, 100, It.IsAny<BookingListFilter?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking> { b1 });
        repo.Setup(r => r.GetStatusHistoryForBookingIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, List<BookingStatusEventDto>>());

        var handler = new ListBookingsQueryHandler(repo.Object);
        var result = await handler.Handle(new ListBookingsQuery("cust-1"), default);

        Assert.Single(result);
        Assert.Empty(result[0].StatusHistory);
    }
}
