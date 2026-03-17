using CargoHub.Application.Bookings;
using CargoHub.Application.Bookings.Dtos;
using CargoHub.Application.Bookings.Queries;
using CargoHub.Domain.Bookings;
using Moq;
using Xunit;

namespace CargoHub.Tests.Bookings;

public class ExportBookingsQueryHandlerTests
{
    private static Booking CreateBooking(Guid id, string customerId = "cust-1", bool enabled = true)
    {
        return new Booking
        {
            Id = id,
            CustomerId = customerId,
            CustomerName = "Customer",
            IsDraft = false,
            Enabled = enabled,
            ShipmentNumber = "SHIP-001",
            WaybillNumber = "WB-001",
            Header = new BookingHeader(),
            Receiver = new BookingParty(),
            Shipper = new BookingParty(),
            Shipment = new BookingShipment(),
            ShippingInfo = new ShippingInfo(),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    [Fact]
    public async Task Handle_WithCustomerId_CallsListByCustomerId()
    {
        var b1 = CreateBooking(Guid.NewGuid());
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.ListByCustomerIdAsync("cust-1", 0, 1000, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking> { b1 });
        repo.Setup(r => r.GetStatusHistoryForBookingIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, List<BookingStatusEventDto>>());

        var handler = new ExportBookingsQueryHandler(repo.Object);
        var result = await handler.Handle(new ExportBookingsQuery("cust-1", 0, 1000, null), default);

        Assert.Single(result);
        repo.Verify(r => r.ListByCustomerIdAsync("cust-1", 0, 1000, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutCustomerId_CallsListAll()
    {
        var b1 = CreateBooking(Guid.NewGuid());
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.ListAllAsync(0, 1000, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking> { b1 });
        repo.Setup(r => r.GetStatusHistoryForBookingIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, List<BookingStatusEventDto>>());

        var handler = new ExportBookingsQueryHandler(repo.Object);
        var result = await handler.Handle(new ExportBookingsQuery(null, 0, 1000, null), default);

        Assert.Single(result);
        repo.Verify(r => r.ListAllAsync(0, 1000, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithFilter_PassesFilterToRepository()
    {
        var filter = new BookingListFilter(Search: "test", CreatedFrom: DateTime.UtcNow.AddDays(-7), CreatedTo: DateTime.UtcNow, Enabled: true);
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.ListByCustomerIdAsync("cust-1", 0, 500, filter, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking>());

        var handler = new ExportBookingsQueryHandler(repo.Object);
        await handler.Handle(new ExportBookingsQuery("cust-1", 0, 500, filter), default);

        repo.Verify(r => r.ListByCustomerIdAsync("cust-1", 0, 500, filter, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithStatusHistory_MergesStatusHistory()
    {
        var b1 = CreateBooking(Guid.NewGuid());
        var statusEvent = new BookingStatusEventDto { Status = "Completed", OccurredAtUtc = DateTime.UtcNow };
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.ListByCustomerIdAsync("cust-1", 0, 1000, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Booking> { b1 });
        repo.Setup(r => r.GetStatusHistoryForBookingIdsAsync(It.IsAny<IEnumerable<Guid>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, List<BookingStatusEventDto>> { { b1.Id, new List<BookingStatusEventDto> { statusEvent } } });

        var handler = new ExportBookingsQueryHandler(repo.Object);
        var result = await handler.Handle(new ExportBookingsQuery("cust-1", 0, 1000, null), default);

        Assert.Single(result);
        Assert.Single(result[0].StatusHistory);
        Assert.Equal("Completed", result[0].StatusHistory[0].Status);
    }
}
