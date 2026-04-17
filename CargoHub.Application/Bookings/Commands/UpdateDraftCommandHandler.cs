using CargoHub.Application.Bookings.Dtos;
using CargoHub.Application.Bookings.Queries;
using MediatR;

namespace CargoHub.Application.Bookings.Commands;

public sealed class UpdateDraftCommandHandler : IRequestHandler<UpdateDraftCommand, BookingDetailDto?>
{
    private readonly IBookingRepository _repository;

    public UpdateDraftCommandHandler(IBookingRepository repository)
    {
        _repository = repository;
    }

    public async Task<BookingDetailDto?> Handle(UpdateDraftCommand request, CancellationToken cancellationToken)
    {
        var draft = await _repository.GetByIdWithTrackingAsync(request.DraftId, request.CustomerId, cancellationToken);
        if (draft == null || !draft.IsDraft)
            return null;
        var r = request.Request;

        if (r.ReferenceNumber != null) draft.Header.ReferenceNumber = r.ReferenceNumber;
        if (r.PostalService != null) draft.Header.PostalService = r.PostalService;
        if (r.CompanyId != null) draft.Header.CompanyId = r.CompanyId;

        draft.Receiver = CreateBookingCommandHandler.MapReceiver(r);
        if (r.Shipper != null) draft.Shipper = CreateBookingCommandHandler.MapParty(r.Shipper) ?? new CargoHub.Domain.Bookings.BookingParty();
        if (r.Payer != null) draft.Payer = CreateBookingCommandHandler.MapParty(r.Payer);
        if (r.PickUpAddress != null) draft.PickUpAddress = CreateBookingCommandHandler.MapParty(r.PickUpAddress) ?? new CargoHub.Domain.Bookings.BookingParty();
        if (r.DeliveryPoint != null) draft.DeliveryPoint = CreateBookingCommandHandler.MapParty(r.DeliveryPoint) ?? new CargoHub.Domain.Bookings.BookingParty();
        if (r.Shipment != null) draft.Shipment = CreateBookingCommandHandler.MapShipment(r.Shipment);
        if (r.ShippingInfo != null)
        {
            draft.ShippingInfo = CreateBookingCommandHandler.MapShippingInfo(r.ShippingInfo);
            draft.Packages.Clear();
            CreateBookingCommandHandler.MapPackages(draft, r.ShippingInfo.Packages);
        }

        if (r.ClearFreelanceRider)
            draft.FreelanceRiderId = null;
        else if (r.FreelanceRiderId.HasValue)
            draft.FreelanceRiderId = r.FreelanceRiderId;

        await _repository.UpdateAsync(draft, cancellationToken);
        return GetBookingByIdQueryHandler.MapToDetail(draft);
    }
}
