using HiavaNet.Application.Bookings.Dtos;
using HiavaNet.Domain.Bookings;
using MediatR;

namespace HiavaNet.Application.Bookings.Queries;

public sealed class GetBookingByIdQueryHandler : IRequestHandler<GetBookingByIdQuery, BookingDetailDto?>
{
    private readonly IBookingRepository _repository;

    public GetBookingByIdQueryHandler(IBookingRepository repository)
    {
        _repository = repository;
    }

    public async Task<BookingDetailDto?> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
    {
        var b = await _repository.GetByIdAsync(request.Id, cancellationToken);
        if (b == null || b.IsDraft)
            return null;
        if (!string.IsNullOrEmpty(request.CustomerId) && b.CustomerId != request.CustomerId)
            return null;
        var dto = Map(b);
        try
        {
            dto.StatusHistory = await _repository.GetStatusHistoryAsync(request.Id, cancellationToken);
        }
        catch
        {
            dto.StatusHistory = new List<BookingStatusEventDto>();
        }
        return dto;
    }

    /// <summary>Maps a booking entity to detail DTO. Used by draft create/update handlers.</summary>
    public static BookingDetailDto MapToDetail(HiavaNet.Domain.Bookings.Booking b)
    {
        return Map(b);
    }

    private static BookingDetailDto Map(HiavaNet.Domain.Bookings.Booking b)
    {
        return new BookingDetailDto
        {
            Id = b.Id,
            CustomerId = b.CustomerId,
            ShipmentNumber = b.ShipmentNumber,
            WaybillNumber = b.WaybillNumber,
            CustomerName = b.CustomerName,
            Enabled = b.Enabled,
            IsTestBooking = b.IsTestBooking,
            IsFavourite = b.IsFavourite,
            IsDraft = b.IsDraft,
            CreatedAtUtc = b.CreatedAtUtc,
            UpdatedAtUtc = b.UpdatedAtUtc,
            Header = new BookingHeaderDto
            {
                SenderId = b.Header.SenderId,
                CompanyId = b.Header.CompanyId,
                ReferenceNumber = b.Header.ReferenceNumber,
                PostalService = b.Header.PostalService
            },
            Shipper = MapParty(b.Shipper),
            Receiver = MapParty(b.Receiver),
            Payer = MapParty(b.Payer),
            PickUpAddress = MapParty(b.PickUpAddress),
            DeliveryPoint = MapParty(b.DeliveryPoint),
            Shipment = MapShipment(b.Shipment),
            ShippingInfo = MapShippingInfo(b.ShippingInfo),
            Packages = b.Packages?.Select(MapPackage).ToList() ?? new List<BookingPackageDto>()
        };
    }

    private static BookingShipmentDto? MapShipment(HiavaNet.Domain.Bookings.BookingShipment? s)
    {
        if (s == null) return null;
        return new BookingShipmentDto
        {
            Service = s.Service,
            SenderReference = s.SenderReference,
            ReceiverReference = s.ReceiverReference,
            FreightPayer = s.FreightPayer,
            HandlingInstructions = s.HandlingInstructions
        };
    }

    private static BookingShippingInfoDto? MapShippingInfo(HiavaNet.Domain.Bookings.ShippingInfo? s)
    {
        if (s == null) return null;
        return new BookingShippingInfoDto
        {
            GrossWeight = s.GrossWeight,
            GrossVolume = s.GrossVolume,
            PackageQuantity = s.PackageQuantity,
            PickupHandlingInstructions = s.PickupHandlingInstructions,
            DeliveryHandlingInstructions = s.DeliveryHandlingInstructions,
            GeneralInstructions = s.GeneralInstructions,
            DeliveryWithoutSignature = s.DeliveryWithoutSignature
        };
    }

    private static BookingPackageDto MapPackage(HiavaNet.Domain.Bookings.BookingPackage p)
    {
        return new BookingPackageDto
        {
            Id = p.Id,
            Weight = p.Weight,
            Volume = p.Volume,
            PackageType = p.PackageType,
            Description = p.Description,
            Length = p.Length,
            Width = p.Width,
            Height = p.Height
        };
    }

    private static BookingPartyDto? MapParty(HiavaNet.Domain.Bookings.BookingParty? p)
    {
        if (p == null) return null;
        return new BookingPartyDto
        {
            Name = p.Name,
            Address1 = p.Address1,
            Address2 = p.Address2,
            PostalCode = p.PostalCode,
            City = p.City,
            Country = p.Country,
            Email = p.Email,
            PhoneNumber = p.PhoneNumber,
            PhoneNumberMobile = p.PhoneNumberMobile,
            ContactPersonName = p.ContactPersonName,
            VatNo = p.VatNo,
            CustomerNumber = p.CustomerNumber
        };
    }
}
