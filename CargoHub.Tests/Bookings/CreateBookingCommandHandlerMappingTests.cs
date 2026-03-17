using CargoHub.Application.Bookings;
using CargoHub.Application.Bookings.Commands;
using CargoHub.Application.Bookings.Dtos;
using CargoHub.Domain.Bookings;
using Moq;
using Xunit;

namespace CargoHub.Tests.Bookings;

/// <summary>
/// Tests for CreateBookingCommandHandler mapping logic and branches.
/// </summary>
public class CreateBookingCommandHandlerMappingTests
{
    private static CreateBookingRequest MinimalRequest() => new()
    {
        ReceiverName = "Test",
        ReceiverAddress1 = "Street 1",
        ReceiverPostalCode = "00100",
        ReceiverCity = "Helsinki",
        ReceiverCountry = "FI"
    };

    [Fact]
    public async Task Handle_WithShipper_MapsShipper()
    {
        var repo = new Mock<IBookingRepository>();
        Booking? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Callback<Booking, CancellationToken>((b, _) => captured = b)
            .Returns<Booking, CancellationToken>((b, _) => Task.FromResult(b));
        repo.Setup(r => r.AddStatusEventAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = MinimalRequest();
        request.Shipper = new CreateBookingPartyDto { Name = "Shipper Co", Address1 = "S1", City = "Helsinki", Country = "FI" };
        var handler = new CreateBookingCommandHandler(repo.Object);
        await handler.Handle(new CreateBookingCommand("c1", "C", request, null), default);

        Assert.NotNull(captured);
        Assert.Equal("Shipper Co", captured.Shipper.Name);
    }

    [Fact]
    public async Task Handle_WithShipment_MapsShipment()
    {
        var repo = new Mock<IBookingRepository>();
        Booking? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Callback<Booking, CancellationToken>((b, _) => captured = b)
            .Returns<Booking, CancellationToken>((b, _) => Task.FromResult(b));
        repo.Setup(r => r.AddStatusEventAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = MinimalRequest();
        request.Shipment = new CreateBookingShipmentDto { Service = "express", SenderReference = "REF1" };
        var handler = new CreateBookingCommandHandler(repo.Object);
        await handler.Handle(new CreateBookingCommand("c1", "C", request, null), default);

        Assert.NotNull(captured);
        Assert.Equal("express", captured.Shipment.Service);
    }

    [Fact]
    public async Task Handle_WithShippingInfoAndPackages_MapsPackages()
    {
        var repo = new Mock<IBookingRepository>();
        Booking? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Callback<Booking, CancellationToken>((b, _) => captured = b)
            .Returns<Booking, CancellationToken>((b, _) => Task.FromResult(b));
        repo.Setup(r => r.AddStatusEventAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = MinimalRequest();
        request.ShippingInfo = new CreateBookingShippingInfoDto
        {
            GrossWeight = "5",
            Packages = new List<CreateBookingPackageDto> { new() { Weight = "2", Description = "P1" }, new() { Weight = "3" } }
        };
        var handler = new CreateBookingCommandHandler(repo.Object);
        await handler.Handle(new CreateBookingCommand("c1", "C", request, null), default);

        Assert.NotNull(captured);
        Assert.Equal(2, captured.Packages.Count);
        Assert.Equal("2", captured.Packages.First().Weight);
    }

    [Fact]
    public async Task Handle_WithPayer_MapsPayer()
    {
        var repo = new Mock<IBookingRepository>();
        Booking? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Callback<Booking, CancellationToken>((b, _) => captured = b)
            .Returns<Booking, CancellationToken>((b, _) => Task.FromResult(b));
        repo.Setup(r => r.AddStatusEventAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = MinimalRequest();
        request.Payer = new CreateBookingPartyDto { Name = "Payer Ltd", City = "Turku", Country = "FI" };
        var handler = new CreateBookingCommandHandler(repo.Object);
        await handler.Handle(new CreateBookingCommand("c1", "C", request, null), default);

        Assert.NotNull(captured);
        Assert.NotNull(captured.Payer);
        Assert.Equal("Payer Ltd", captured.Payer.Name);
    }

    [Fact]
    public async Task Handle_WithNullShippingInfo_UsesDefaults()
    {
        var repo = new Mock<IBookingRepository>();
        Booking? captured = null;
        repo.Setup(r => r.AddAsync(It.IsAny<Booking>(), It.IsAny<CancellationToken>()))
            .Callback<Booking, CancellationToken>((b, _) => captured = b)
            .Returns<Booking, CancellationToken>((b, _) => Task.FromResult(b));
        repo.Setup(r => r.AddStatusEventAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var request = MinimalRequest();
        request.ShippingInfo = null;
        var handler = new CreateBookingCommandHandler(repo.Object);
        await handler.Handle(new CreateBookingCommand("c1", "C", request, null), default);

        Assert.NotNull(captured);
        Assert.Equal("0", captured.ShippingInfo.GrossWeight);
    }
}
