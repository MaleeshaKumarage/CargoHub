using HiavaNet.Application.Bookings.Dtos;
using HiavaNet.Application.Bookings.Queries;
using HiavaNet.Domain.Bookings;
using MediatR;

namespace HiavaNet.Application.Bookings.Commands;

public sealed class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, BookingDetailDto?>
{
    private readonly IBookingRepository _repository;

    public CreateBookingCommandHandler(IBookingRepository repository)
    {
        _repository = repository;
    }

    public async Task<BookingDetailDto?> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
    {
        var r = request.Request;
        var now = DateTime.UtcNow;
        var booking = new Booking
        {
            Id = Guid.NewGuid(),
            CustomerId = request.CustomerId,
            CompanyId = request.CompanyId,
            CustomerName = request.CustomerName ?? request.CustomerId,
            IsDraft = false,
            Enabled = true,
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
            Receiver = MapReceiver(r),
            Shipper = MapParty(r.Shipper) ?? new BookingParty(),
            Payer = MapParty(r.Payer),
            PickUpAddress = MapParty(r.PickUpAddress) ?? new BookingParty(),
            DeliveryPoint = MapParty(r.DeliveryPoint) ?? new BookingParty(),
            Shipment = MapShipment(r.Shipment),
            ShippingInfo = MapShippingInfo(r.ShippingInfo)
        };
        MapPackages(booking, r.ShippingInfo?.Packages);
        await _repository.AddAsync(booking, cancellationToken);
        await _repository.AddStatusEventAsync(booking.Id, BookingStatus.CompletedBooking, "booking_created", cancellationToken);
        return GetBookingByIdQueryHandler.MapToDetail(booking);
    }

    public static BookingParty MapReceiver(CreateBookingRequest r)
    {
        return new BookingParty
        {
            Name = r.ReceiverName ?? "",
            Address1 = r.ReceiverAddress1 ?? "",
            Address2 = r.ReceiverAddress2,
            PostalCode = r.ReceiverPostalCode ?? "",
            City = r.ReceiverCity ?? "",
            Country = r.ReceiverCountry ?? "FI",
            Email = r.ReceiverEmail,
            PhoneNumber = r.ReceiverPhone,
            PhoneNumberMobile = r.ReceiverPhoneMobile,
            ContactPersonName = r.ReceiverContactPersonName,
            VatNo = r.ReceiverVatNo,
            CustomerNumber = r.ReceiverCustomerNumber
        };
    }

    public static BookingParty MapReceiver(UpdateDraftRequest r)
    {
        return new BookingParty
        {
            Name = r.ReceiverName ?? "",
            Address1 = r.ReceiverAddress1 ?? "",
            Address2 = r.ReceiverAddress2,
            PostalCode = r.ReceiverPostalCode ?? "",
            City = r.ReceiverCity ?? "",
            Country = r.ReceiverCountry ?? "FI",
            Email = r.ReceiverEmail,
            PhoneNumber = r.ReceiverPhone,
            PhoneNumberMobile = r.ReceiverPhoneMobile,
            ContactPersonName = r.ReceiverContactPersonName,
            VatNo = r.ReceiverVatNo,
            CustomerNumber = r.ReceiverCustomerNumber
        };
    }

    public static BookingParty? MapParty(CreateBookingPartyDto? dto)
    {
        if (dto == null) return null;
        return new BookingParty
        {
            Name = dto.Name ?? "",
            Address1 = dto.Address1 ?? "",
            Address2 = dto.Address2,
            PostalCode = dto.PostalCode ?? "",
            City = dto.City ?? "",
            Country = dto.Country ?? "FI",
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            PhoneNumberMobile = dto.PhoneNumberMobile,
            ContactPersonName = dto.ContactPersonName,
            VatNo = dto.VatNo,
            CustomerNumber = dto.CustomerNumber
        };
    }

    public static BookingShipment MapShipment(CreateBookingShipmentDto? dto)
    {
        if (dto == null) return new BookingShipment();
        return new BookingShipment
        {
            Service = dto.Service,
            SenderReference = dto.SenderReference,
            ReceiverReference = dto.ReceiverReference,
            FreightPayer = dto.FreightPayer,
            HandlingInstructions = dto.HandlingInstructions
        };
    }

    public static ShippingInfo MapShippingInfo(CreateBookingShippingInfoDto? dto)
    {
        if (dto == null) return new ShippingInfo();
        return new ShippingInfo
        {
            GrossWeight = dto.GrossWeight ?? "0",
            GrossVolume = dto.GrossVolume ?? "0",
            PackageQuantity = dto.PackageQuantity ?? "0",
            PickupHandlingInstructions = dto.PickupHandlingInstructions,
            DeliveryHandlingInstructions = dto.DeliveryHandlingInstructions,
            GeneralInstructions = dto.GeneralInstructions,
            DeliveryWithoutSignature = dto.DeliveryWithoutSignature ?? false
        };
    }

    public static void MapPackages(Booking booking, List<CreateBookingPackageDto>? packages)
    {
        if (packages == null || packages.Count == 0) return;
        for (var i = 0; i < packages.Count; i++)
        {
            var p = packages[i];
            booking.Packages.Add(new BookingPackage
            {
                Id = i + 1,
                Weight = p.Weight,
                Volume = p.Volume,
                PackageType = p.PackageType,
                Description = p.Description,
                Length = p.Length,
                Width = p.Width,
                Height = p.Height
            });
        }
    }
}
