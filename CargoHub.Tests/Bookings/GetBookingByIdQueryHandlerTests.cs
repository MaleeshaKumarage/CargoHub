using CargoHub.Application.Bookings;
using CargoHub.Application.Bookings.Dtos;
using CargoHub.Application.Bookings.Queries;
using CargoHub.Domain.Bookings;
using Moq;
using Xunit;

namespace CargoHub.Tests.Bookings;

public class GetBookingByIdQueryHandlerTests
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
    public async Task Handle_WhenBookingExistsAndNotDraft_ReturnsDetail()
    {
        var id = Guid.NewGuid();
        var booking = CreateCompletedBooking(id);
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);
        repo.Setup(r => r.GetStatusHistoryAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BookingStatusEventDto> { new() { Status = "Completed", OccurredAtUtc = DateTime.UtcNow } });

        var handler = new GetBookingByIdQueryHandler(repo.Object);
        var result = await handler.Handle(new GetBookingByIdQuery(id, null), default);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("cust-1", result.CustomerId);
        Assert.False(result.IsDraft);
        Assert.Single(result.StatusHistory);
    }

    [Fact]
    public async Task Handle_WhenBookingIsNull_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Booking?)null);

        var handler = new GetBookingByIdQueryHandler(repo.Object);
        var result = await handler.Handle(new GetBookingByIdQuery(id, null), default);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WhenBookingIsDraft_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var booking = CreateCompletedBooking(id);
        booking.IsDraft = true;
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var handler = new GetBookingByIdQueryHandler(repo.Object);
        var result = await handler.Handle(new GetBookingByIdQuery(id, null), default);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WhenCustomerIdProvidedAndMismatch_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var booking = CreateCompletedBooking(id, "cust-1");
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);

        var handler = new GetBookingByIdQueryHandler(repo.Object);
        var result = await handler.Handle(new GetBookingByIdQuery(id, "other-cust"), default);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_WhenCustomerIdProvidedAndMatch_ReturnsDetail()
    {
        var id = Guid.NewGuid();
        var booking = CreateCompletedBooking(id, "cust-1");
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);
        repo.Setup(r => r.GetStatusHistoryAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(new List<BookingStatusEventDto>());

        var handler = new GetBookingByIdQueryHandler(repo.Object);
        var result = await handler.Handle(new GetBookingByIdQuery(id, "cust-1"), default);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
    }

    [Fact]
    public async Task Handle_WhenGetStatusHistoryThrows_ReturnsDetailWithEmptyStatusHistory()
    {
        var id = Guid.NewGuid();
        var booking = CreateCompletedBooking(id);
        var repo = new Mock<IBookingRepository>();
        repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(booking);
        repo.Setup(r => r.GetStatusHistoryAsync(id, It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException());

        var handler = new GetBookingByIdQueryHandler(repo.Object);
        var result = await handler.Handle(new GetBookingByIdQuery(id, null), default);

        Assert.NotNull(result);
        Assert.Empty(result.StatusHistory);
    }

    [Fact]
    public void MapToDetail_WithBookingWithPackages_MapsPackages()
    {
        var booking = CreateCompletedBooking(Guid.NewGuid());
        booking.Packages.Add(new BookingPackage { Id = 1, Weight = "5", Description = "Box" });

        var result = GetBookingByIdQueryHandler.MapToDetail(booking);

        Assert.Single(result.Packages);
        Assert.Equal(1, result.Packages[0].Id);
        Assert.Equal("5", result.Packages[0].Weight);
        Assert.Equal("Box", result.Packages[0].Description);
    }

    [Fact]
    public void MapToDetail_WithNullShipment_MapsNull()
    {
        var booking = CreateCompletedBooking(Guid.NewGuid());
        booking.Shipment = null!;
        var result = GetBookingByIdQueryHandler.MapToDetail(booking);
        Assert.Null(result.Shipment);
    }

    [Fact]
    public void MapToDetail_WithNullShippingInfo_MapsNull()
    {
        var booking = CreateCompletedBooking(Guid.NewGuid());
        booking.ShippingInfo = null!;
        var result = GetBookingByIdQueryHandler.MapToDetail(booking);
        Assert.Null(result.ShippingInfo);
    }

    [Fact]
    public void MapToDetail_WithNullPayer_MapsNull()
    {
        var booking = CreateCompletedBooking(Guid.NewGuid());
        booking.Payer = null;
        var result = GetBookingByIdQueryHandler.MapToDetail(booking);
        Assert.Null(result.Payer);
    }

    [Fact]
    public void MapToDetail_WithNullPackages_MapsEmptyList()
    {
        var booking = CreateCompletedBooking(Guid.NewGuid());
        booking.Packages = null!;
        var result = GetBookingByIdQueryHandler.MapToDetail(booking);
        Assert.NotNull(result.Packages);
        Assert.Empty(result.Packages);
    }
}
