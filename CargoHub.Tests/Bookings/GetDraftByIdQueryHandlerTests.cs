using CargoHub.Application.Bookings;
using CargoHub.Application.Bookings.Dtos;
using CargoHub.Application.Bookings.Queries;
using CargoHub.Domain.Bookings;
using Moq;
using Xunit;

namespace CargoHub.Tests.Bookings;

public class GetDraftByIdQueryHandlerTests
{
    private static Booking CreateDraft(Guid id, string customerId = "cust-1")
    {
        return new Booking
        {
            Id = id,
            CustomerId = customerId,
            CustomerName = "Customer",
            IsDraft = true,
            Header = new BookingHeader { SenderId = customerId },
            Receiver = new BookingParty { Name = "R", Address1 = "A1", PostalCode = "00100", City = "Helsinki", Country = "FI" },
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
    public async Task Handle_WhenDraftExists_ReturnsDetail()
    {
        var id = Guid.NewGuid();
        var draft = CreateDraft(id);
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);
        repo.Setup(r => r.GetStatusHistoryAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<BookingStatusEventDto>());

        var handler = new GetDraftByIdQueryHandler(repo.Object);
        var result = await handler.Handle(new GetDraftByIdQuery(id, null), default);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.True(result.IsDraft);
    }

    [Fact]
    public async Task Handle_WhenBookingIsNull_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Booking?)null);

        var handler = new GetDraftByIdQueryHandler(repo.Object);
        var result = await handler.Handle(new GetDraftByIdQuery(id, null), default);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WhenBookingIsNotDraft_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var booking = CreateDraft(id);
        booking.IsDraft = false;
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var handler = new GetDraftByIdQueryHandler(repo.Object);
        var result = await handler.Handle(new GetDraftByIdQuery(id, null), default);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WhenCustomerIdMismatch_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var draft = CreateDraft(id, "cust-1");
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);

        var handler = new GetDraftByIdQueryHandler(repo.Object);
        var result = await handler.Handle(new GetDraftByIdQuery(id, "other-cust"), default);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WhenCustomerIdMatch_ReturnsDetail()
    {
        var id = Guid.NewGuid();
        var draft = CreateDraft(id, "cust-1");
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);
        repo.Setup(r => r.GetStatusHistoryAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<BookingStatusEventDto>());

        var handler = new GetDraftByIdQueryHandler(repo.Object);
        var result = await handler.Handle(new GetDraftByIdQuery(id, "cust-1"), default);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
    }
}
