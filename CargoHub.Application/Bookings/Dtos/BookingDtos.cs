namespace CargoHub.Application.Bookings.Dtos;

/// <summary>One status event from BookingStatusHistory (when a milestone was reached).</summary>
public sealed class BookingStatusEventDto
{
    public string Status { get; set; } = "";
    public DateTime OccurredAtUtc { get; set; }
    public string? Source { get; set; }
}

/// <summary>
/// Summary of a booking for list views.
/// </summary>
public sealed class BookingListDto
{
    public Guid Id { get; set; }
    public string? ShipmentNumber { get; set; }
    public string? WaybillNumber { get; set; }
    public string? CustomerName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public bool Enabled { get; set; }
    public bool IsFavourite { get; set; }
    public bool IsDraft { get; set; }
    /// <summary>Status history for milestone bar. Populated when listing bookings/drafts.</summary>
    public List<BookingStatusEventDto> StatusHistory { get; set; } = new();
}

/// <summary>
/// Shipment info for detail view.
/// </summary>
public sealed class BookingShipmentDto
{
    public string? Service { get; set; }
    public string? SenderReference { get; set; }
    public string? ReceiverReference { get; set; }
    public string? FreightPayer { get; set; }
    public string? HandlingInstructions { get; set; }
}

/// <summary>
/// Shipping info for detail view.
/// </summary>
public sealed class BookingShippingInfoDto
{
    public string? GrossWeight { get; set; }
    public string? GrossVolume { get; set; }
    public string? PackageQuantity { get; set; }
    public string? PickupHandlingInstructions { get; set; }
    public string? DeliveryHandlingInstructions { get; set; }
    public string? GeneralInstructions { get; set; }
    public bool DeliveryWithoutSignature { get; set; }
}

/// <summary>
/// Package for detail view.
/// </summary>
public sealed class BookingPackageDto
{
    public int Id { get; set; }
    public string? Weight { get; set; }
    public string? Volume { get; set; }
    public string? PackageType { get; set; }
    public string? Description { get; set; }
    public string? Length { get; set; }
    public string? Width { get; set; }
    public string? Height { get; set; }
}

/// <summary>
/// Full booking for detail view. Flattened for API consumers.
/// </summary>
public sealed class BookingDetailDto
{
    public Guid Id { get; set; }
    public string CustomerId { get; set; } = "";
    public string? ShipmentNumber { get; set; }
    public string? WaybillNumber { get; set; }
    public string? CustomerName { get; set; }
    public bool Enabled { get; set; }
    public bool IsTestBooking { get; set; }
    public bool IsFavourite { get; set; }
    public bool IsDraft { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public BookingHeaderDto Header { get; set; } = new();
    public BookingPartyDto? Shipper { get; set; }
    public BookingPartyDto? Receiver { get; set; }
    public BookingPartyDto? Payer { get; set; }
    public BookingPartyDto? PickUpAddress { get; set; }
    public BookingPartyDto? DeliveryPoint { get; set; }
    public BookingShipmentDto? Shipment { get; set; }
    public BookingShippingInfoDto? ShippingInfo { get; set; }
    public List<BookingPackageDto> Packages { get; set; } = new();
    /// <summary>When each status (Draft, Waybill, etc.) was reached. Empty if not loaded.</summary>
    public List<BookingStatusEventDto> StatusHistory { get; set; } = new();

    public Guid? FreelanceRiderId { get; set; }
    public DateTime? FreelanceRiderAssignmentDeadlineUtc { get; set; }
    public DateTime? FreelanceRiderAcceptedAtUtc { get; set; }
    public bool FreelanceRiderAssignmentLapsed { get; set; }
}

public sealed class BookingHeaderDto
{
    public string SenderId { get; set; } = "";
    public string? CompanyId { get; set; }
    public string? ReferenceNumber { get; set; }
    public string? PostalService { get; set; }
}

public sealed class BookingPartyDto
{
    public string Name { get; set; } = "";
    public string Address1 { get; set; } = "";
    public string? Address2 { get; set; }
    public string PostalCode { get; set; } = "";
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? PhoneNumberMobile { get; set; }
    public string? ContactPersonName { get; set; }
    public string? VatNo { get; set; }
    public string? CustomerNumber { get; set; }
}

/// <summary>
/// Party data for create/update (shipper, receiver, payer, pickup, delivery).
/// </summary>
public sealed class CreateBookingPartyDto
{
    public string? Name { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? PhoneNumberMobile { get; set; }
    public string? ContactPersonName { get; set; }
    public string? VatNo { get; set; }
    public string? CustomerNumber { get; set; }
}

/// <summary>
/// Shipment data for create/update.
/// </summary>
public sealed class CreateBookingShipmentDto
{
    public string? Service { get; set; }
    public string? SenderReference { get; set; }
    public string? ReceiverReference { get; set; }
    public string? FreightPayer { get; set; }
    public string? HandlingInstructions { get; set; }
}

/// <summary>
/// A single package in a booking. A booking can have multiple packages.
/// </summary>
public sealed class CreateBookingPackageDto
{
    public string? Weight { get; set; }
    public string? Volume { get; set; }
    public string? PackageType { get; set; }
    public string? Description { get; set; }
    public string? Length { get; set; }
    public string? Width { get; set; }
    public string? Height { get; set; }
}

/// <summary>
/// Shipping info for create/update.
/// </summary>
public sealed class CreateBookingShippingInfoDto
{
    public string? GrossWeight { get; set; }
    public string? GrossVolume { get; set; }
    public string? PackageQuantity { get; set; }
    public string? PickupHandlingInstructions { get; set; }
    public string? DeliveryHandlingInstructions { get; set; }
    public string? GeneralInstructions { get; set; }
    public bool? DeliveryWithoutSignature { get; set; }
    public List<CreateBookingPackageDto>? Packages { get; set; }
}

/// <summary>
/// Request to create a new booking. Supports flat (legacy) or nested shape.
/// </summary>
public sealed class CreateBookingRequest
{
    public string? ReferenceNumber { get; set; }
    public string? PostalService { get; set; }
    public string? CompanyId { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiverAddress1 { get; set; }
    public string? ReceiverAddress2 { get; set; }
    public string? ReceiverPostalCode { get; set; }
    public string? ReceiverCity { get; set; }
    public string? ReceiverCountry { get; set; }
    public string? ReceiverEmail { get; set; }
    public string? ReceiverPhone { get; set; }
    public string? ReceiverPhoneMobile { get; set; }
    public string? ReceiverContactPersonName { get; set; }
    public string? ReceiverVatNo { get; set; }
    public string? ReceiverCustomerNumber { get; set; }
    public CreateBookingPartyDto? Shipper { get; set; }
    public CreateBookingPartyDto? Payer { get; set; }
    public CreateBookingPartyDto? PickUpAddress { get; set; }
    public CreateBookingPartyDto? DeliveryPoint { get; set; }
    public CreateBookingShipmentDto? Shipment { get; set; }
    public CreateBookingShippingInfoDto? ShippingInfo { get; set; }

    /// <summary>Optional freelance rider (validated on save). Omit to use carrier only.</summary>
    public Guid? FreelanceRiderId { get; set; }
}

/// <summary>
/// Request to update a draft (same shape as create).
/// </summary>
public sealed class UpdateDraftRequest
{
    public string? ReferenceNumber { get; set; }
    public string? PostalService { get; set; }
    public string? CompanyId { get; set; }
    public string? ReceiverName { get; set; }
    public string? ReceiverAddress1 { get; set; }
    public string? ReceiverAddress2 { get; set; }
    public string? ReceiverPostalCode { get; set; }
    public string? ReceiverCity { get; set; }
    public string? ReceiverCountry { get; set; }
    public string? ReceiverEmail { get; set; }
    public string? ReceiverPhone { get; set; }
    public string? ReceiverPhoneMobile { get; set; }
    public string? ReceiverContactPersonName { get; set; }
    public string? ReceiverVatNo { get; set; }
    public string? ReceiverCustomerNumber { get; set; }
    public CreateBookingPartyDto? Shipper { get; set; }
    public CreateBookingPartyDto? Payer { get; set; }
    public CreateBookingPartyDto? PickUpAddress { get; set; }
    public CreateBookingPartyDto? DeliveryPoint { get; set; }
    public CreateBookingShipmentDto? Shipment { get; set; }
    public CreateBookingShippingInfoDto? ShippingInfo { get; set; }

    public Guid? FreelanceRiderId { get; set; }

    /// <summary>When true, clears freelance rider on the draft (omit <see cref="FreelanceRiderId"/>).</summary>
    public bool ClearFreelanceRider { get; set; }
}
