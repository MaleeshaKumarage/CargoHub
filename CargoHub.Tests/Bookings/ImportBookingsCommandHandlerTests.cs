using CargoHub.Application.Bookings.Commands;
using CargoHub.Application.Bookings.Dtos;
using MediatR;
using NSubstitute;
using Xunit;

namespace CargoHub.Tests.Bookings;

public class ImportBookingsCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRowIsComplete_SendsCreateBookingCommand()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<IRequest<BookingDetailDto?>>(), default)
            .Returns(new BookingDetailDto { Id = Guid.NewGuid() });

        var request = FullRequest();
        var handler = new ImportBookingsCommandHandler(mediator);
        var result = await handler.Handle(new ImportBookingsCommand(
            "cust-1", "User", null,
            new List<ImportRowDto> { new(request, IsComplete: true) }), default);

        Assert.Equal(1, result.CreatedCount);
        Assert.Equal(0, result.DraftCount);
        Assert.Empty(result.Errors);
        await mediator.Received(1).Send(Arg.Is<CreateBookingCommand>(c =>
            c.CustomerId == "cust-1" && c.Request.ReferenceNumber == request.ReferenceNumber), default);
    }

    [Fact]
    public async Task Handle_WhenRowIsDraft_SendsCreateDraftCommand()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<IRequest<BookingDetailDto?>>(), default)
            .Returns(new BookingDetailDto { Id = Guid.NewGuid() });

        var request = FullRequest();
        var handler = new ImportBookingsCommandHandler(mediator);
        var result = await handler.Handle(new ImportBookingsCommand(
            "cust-1", "User", null,
            new List<ImportRowDto> { new(request, IsComplete: false) }), default);

        Assert.Equal(0, result.CreatedCount);
        Assert.Equal(1, result.DraftCount);
        Assert.Empty(result.Errors);
        await mediator.Received(1).Send(Arg.Is<CreateDraftCommand>(c =>
            c.CustomerId == "cust-1" && c.Request.ReferenceNumber == request.ReferenceNumber), default);
    }

    [Fact]
    public async Task Handle_WhenMultipleRows_CreatesCorrectMix()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<IRequest<BookingDetailDto?>>(), default)
            .Returns(new BookingDetailDto { Id = Guid.NewGuid() });

        var req1 = FullRequest();
        req1.ReferenceNumber = "REF1";
        var req2 = FullRequest();
        req2.ReferenceNumber = "REF2";
        var req3 = FullRequest();
        req3.ReferenceNumber = "REF3";

        var handler = new ImportBookingsCommandHandler(mediator);
        var result = await handler.Handle(new ImportBookingsCommand(
            "cust-1", "User", null,
            new List<ImportRowDto>
            {
                new(req1, true),
                new(req2, false),
                new(req3, true),
            }), default);

        Assert.Equal(2, result.CreatedCount);
        Assert.Equal(1, result.DraftCount);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task Handle_WhenCreateFails_ReturnsError()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<IRequest<BookingDetailDto?>>(), default)
            .Returns((BookingDetailDto?)null);

        var request = FullRequest();
        var handler = new ImportBookingsCommandHandler(mediator);
        var result = await handler.Handle(new ImportBookingsCommand(
            "cust-1", "User", null,
            new List<ImportRowDto> { new(request, true) }), default);

        Assert.Equal(0, result.CreatedCount);
        Assert.Single(result.Errors);
        Assert.Contains("REF-FULL", result.Errors[0]);
    }

    private static CreateBookingRequest FullRequest()
    {
        return new CreateBookingRequest
        {
            ReferenceNumber = "REF-FULL",
            PostalService = "PS1",
            ReceiverName = "R",
            ReceiverAddress1 = "A1",
            ReceiverCity = "Helsinki",
            ReceiverPostalCode = "00100",
            ReceiverCountry = "FI",
            Shipper = new CreateBookingPartyDto
            {
                Name = "S", Address1 = "A", City = "H", PostalCode = "00100", Country = "FI" },
            Shipment = new CreateBookingShipmentDto { Service = "S", FreightPayer = "FP" },
            ShippingInfo = new CreateBookingShippingInfoDto
            {
                GrossWeight = "5",
                Packages = new List<CreateBookingPackageDto>
                {
                    new() { Weight = "2", PackageType = "Box" },
                },
            },
        };
    }
}
