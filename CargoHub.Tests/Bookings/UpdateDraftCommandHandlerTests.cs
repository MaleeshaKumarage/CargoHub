using CargoHub.Application.Bookings;
using CargoHub.Application.Bookings.Commands;
using CargoHub.Application.Bookings.Dtos;
using CargoHub.Domain.Bookings;
using Moq;
using Xunit;

namespace CargoHub.Tests.Bookings;

public class UpdateDraftCommandHandlerTests
{
    private static Booking CreateDraft(Guid id, string customerId = "cust-1")
    {
        return new Booking
        {
            Id = id,
            CustomerId = customerId,
            CustomerName = "Customer",
            IsDraft = true,
            Header = new BookingHeader { SenderId = customerId, ReferenceNumber = "old-ref" },
            Receiver = new BookingParty { Name = "R", Address1 = "A1", PostalCode = "00100", City = "Helsinki", Country = "FI" },
            Shipper = new BookingParty(),
            PickUpAddress = new BookingParty(),
            DeliveryPoint = new BookingParty(),
            Shipment = new BookingShipment(),
            ShippingInfo = new ShippingInfo(),
            Packages = new List<BookingPackage>(),
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    private static UpdateDraftRequest MinimalRequest() => new()
    {
        ReceiverName = "Updated",
        ReceiverAddress1 = "New St 1",
        ReceiverPostalCode = "00200",
        ReceiverCity = "Espoo",
        ReceiverCountry = "FI"
    };

    [Fact]
    public async Task Handle_WhenDraftExists_UpdatesAndReturnsDetail()
    {
        var id = Guid.NewGuid();
        var draft = CreateDraft(id);
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.GetByIdWithTrackingAsync(id, "cust-1", It.IsAny<CancellationToken>())).ReturnsAsync(draft);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var handler = new UpdateDraftCommandHandler(repo.Object);
        var request = MinimalRequest();
        request.ReferenceNumber = "new-ref";
        var result = await handler.Handle(new UpdateDraftCommand(id, "cust-1", request), default);

        Assert.NotNull(result);
        Assert.Equal("new-ref", draft.Header.ReferenceNumber);
        Assert.Equal("Updated", draft.Receiver.Name);
        Assert.Equal("Espoo", draft.Receiver.City);
    }

    [Fact]
    public async Task Handle_WhenDraftNotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.GetByIdWithTrackingAsync(id, "cust-1", It.IsAny<CancellationToken>())).ReturnsAsync((Booking?)null);

        var handler = new UpdateDraftCommandHandler(repo.Object);
        var result = await handler.Handle(new UpdateDraftCommand(id, "cust-1", MinimalRequest()), default);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WhenBookingIsNotDraft_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var booking = CreateDraft(id);
        booking.IsDraft = false;
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.GetByIdWithTrackingAsync(id, "cust-1", It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var handler = new UpdateDraftCommandHandler(repo.Object);
        var result = await handler.Handle(new UpdateDraftCommand(id, "cust-1", MinimalRequest()), default);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WithPayerAndDeliveryPoint_UpdatesParties()
    {
        var id = Guid.NewGuid();
        var draft = CreateDraft(id);
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.GetByIdWithTrackingAsync(id, "cust-1", It.IsAny<CancellationToken>())).ReturnsAsync(draft);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var request = MinimalRequest();
        request.Payer = new CreateBookingPartyDto { Name = "Payer Inc", Address1 = "P1", City = "Turku", Country = "FI" };
        request.DeliveryPoint = new CreateBookingPartyDto { Name = "Delivery Co", City = "Tampere", Country = "FI" };
        var handler = new UpdateDraftCommandHandler(repo.Object);
        var result = await handler.Handle(new UpdateDraftCommand(id, "cust-1", request), default);

        Assert.NotNull(result);
        Assert.Equal("Payer Inc", draft.Payer?.Name);
        Assert.Equal("Delivery Co", draft.DeliveryPoint.Name);
    }

    [Fact]
    public async Task Handle_WithPickUpAddress_UpdatesPickUpAddress()
    {
        var id = Guid.NewGuid();
        var draft = CreateDraft(id);
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.GetByIdWithTrackingAsync(id, "cust-1", It.IsAny<CancellationToken>())).ReturnsAsync(draft);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var request = MinimalRequest();
        request.PickUpAddress = new CreateBookingPartyDto { Name = "PickUp Co", Address1 = "PU1", City = "Vantaa", Country = "FI" };
        var handler = new UpdateDraftCommandHandler(repo.Object);
        var result = await handler.Handle(new UpdateDraftCommand(id, "cust-1", request), default);

        Assert.NotNull(result);
        Assert.Equal("PickUp Co", draft.PickUpAddress.Name);
    }

    [Fact]
    public async Task Handle_WithShipment_UpdatesShipment()
    {
        var id = Guid.NewGuid();
        var draft = CreateDraft(id);
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.GetByIdWithTrackingAsync(id, "cust-1", It.IsAny<CancellationToken>())).ReturnsAsync(draft);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var request = MinimalRequest();
        request.Shipment = new CreateBookingShipmentDto { Service = "express", SenderReference = "SR1", FreightPayer = "SENDER" };
        var handler = new UpdateDraftCommandHandler(repo.Object);
        var result = await handler.Handle(new UpdateDraftCommand(id, "cust-1", request), default);

        Assert.NotNull(result);
        Assert.Equal("express", draft.Shipment.Service);
        Assert.Equal("SR1", draft.Shipment.SenderReference);
    }

    [Fact]
    public async Task Handle_WithCompanyId_UpdatesCompanyId()
    {
        var companyId = Guid.NewGuid().ToString();
        var id = Guid.NewGuid();
        var draft = CreateDraft(id);
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.GetByIdWithTrackingAsync(id, "cust-1", It.IsAny<CancellationToken>())).ReturnsAsync(draft);
        repo.Setup(r => r.UpdateAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var request = MinimalRequest();
        request.CompanyId = companyId;
        var handler = new UpdateDraftCommandHandler(repo.Object);
        var result = await handler.Handle(new UpdateDraftCommand(id, "cust-1", request), default);

        Assert.NotNull(result);
        Assert.Equal(companyId, draft.Header.CompanyId);
    }
}
