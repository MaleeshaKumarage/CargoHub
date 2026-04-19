using CargoHub.Application.Bookings.Dtos;
using CargoHub.Application.Bookings.Queries;
using CargoHub.Domain.Bookings;
using MediatR;

namespace CargoHub.Application.Bookings.Commands;

public sealed class CreateDraftCommandHandler : IRequestHandler<CreateDraftCommand, BookingDetailDto?>
{
    private readonly IBookingRepository _repository;

    public CreateDraftCommandHandler(IBookingRepository repository)
    {
        _repository = repository;
    }

    public async Task<BookingDetailDto?> Handle(CreateDraftCommand request, CancellationToken cancellationToken)
    {
        var r = request.Request;
        var now = DateTime.UtcNow;
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            CompanyId = request.CompanyId,
            CustomerName = request.CustomerName ?? request.CustomerId,
            IsDraft = true,
            Enabled = false,
            IsTestBooking = false,
            IsFavourite = false,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Header = new BookingHeader
            {
                SenderId = request.CustomerId,
                ReferenceNumber = r.ReferenceNumber,
                PostalService = r.PostalService,
                CompanyId = r.CompanyId
            },
            Receiver = CreateBookingCommandHandler.MapReceiver(r),
            Shipper = CreateBookingCommandHandler.MapParty(r.Shipper) ?? new BookingParty(),
            Payer = CreateBookingCommandHandler.MapParty(r.Payer),
            PickUpAddress = CreateBookingCommandHandler.MapParty(r.PickUpAddress) ?? new BookingParty(),
            DeliveryPoint = CreateBookingCommandHandler.MapParty(r.DeliveryPoint) ?? new BookingParty(),
            Shipment = CreateBookingCommandHandler.MapShipment(r.Shipment),
            ShippingInfo = CreateBookingCommandHandler.MapShippingInfo(r.ShippingInfo)
        };
        CreateBookingCommandHandler.MapPackages(booking, r.ShippingInfo?.Packages);
        if (r.FreelanceRiderId.HasValue)
            booking.FreelanceRiderId = r.FreelanceRiderId;
        await _repository.AddAsync(booking, cancellationToken);
        await _repository.AddStatusEventAsync(booking.Id, BookingStatus.Draft, "draft_created", cancellationToken);
        return GetBookingByIdQueryHandler.MapToDetail(booking);
    }
}
