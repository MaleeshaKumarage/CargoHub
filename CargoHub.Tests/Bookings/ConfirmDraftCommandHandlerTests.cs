using CargoHub.Application.Bookings;
using CargoHub.Application.Bookings.Commands;
using CargoHub.Domain.Bookings;
using Moq;
using Xunit;

namespace CargoHub.Tests.Bookings;

public class ConfirmDraftCommandHandlerTests
{
    private static Booking CreateCompletedBooking(Guid id, string customerId = "cust-1")
    {
        return new Booking
        {
            Id = id,
            CustomerId = customerId,
            CustomerName = "Customer",
            IsDraft = false,
            Enabled = true,
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
    public async Task Handle_WhenConfirmSucceeds_ReturnsDetail()
    {
        var id = Guid.NewGuid();
        var confirmed = CreateCompletedBooking(id);
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.ConfirmDraftAsync(id, "cust-1", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(confirmed);

        var handler = new ConfirmDraftCommandHandler(repo.Object);
        var result = await handler.Handle(new ConfirmDraftCommand(id, "cust-1"), default);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.False(result.IsDraft);
    }

    [Fact]
    public async Task Handle_WhenConfirmReturnsFalse_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.ConfirmDraftAsync(id, "cust-1", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var handler = new ConfirmDraftCommandHandler(repo.Object);
        var result = await handler.Handle(new ConfirmDraftCommand(id, "cust-1"), default);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WhenGetByIdReturnsNullAfterConfirm_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.ConfirmDraftAsync(id, "cust-1", It.IsAny<CancellationToken>())).ReturnsAsync(true);
        repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Booking?)null);

        var handler = new ConfirmDraftCommandHandler(repo.Object);
        var result = await handler.Handle(new ConfirmDraftCommand(id, "cust-1"), default);

        Assert.Null(result);
    }
}
