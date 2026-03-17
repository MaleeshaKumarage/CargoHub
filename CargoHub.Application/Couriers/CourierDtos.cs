namespace CargoHub.Application.Couriers;

/// <summary>
/// Normalized request for creating a booking with a courier.
/// Each client maps this to its own format (REST JSON, XML, or email body).
/// </summary>
public sealed class CourierCreateRequest
{
    /// <summary>Our internal booking/shipment reference (e.g. shipment number).</summary>
    public string ShipmentNumber { get; set; } = string.Empty;

    public string? WaybillNumber { get; set; }
    public bool IsTestBooking { get; set; }
    public string? Service { get; set; }
    public string? DocumentDateTime { get; set; }
    public string? SenderId { get; set; }
    public string? CompanyId { get; set; }

    public CourierPartyDto Shipper { get; set; } = new();
    public CourierPartyDto Receiver { get; set; } = new();
    public CourierPartyDto? Payer { get; set; }
    public CourierPartyDto? PickUpAddress { get; set; }
    public CourierPartyDto? DeliveryPoint { get; set; }

    public CourierShipmentDto Shipment { get; set; } = new();
    public CourierShippingInfoDto ShippingInfo { get; set; } = new();
    public List<CourierPackageDto> Packages { get; set; } = new();
}

public sealed class CourierPartyDto
{
    public string Name { get; set; } = string.Empty;
    public string Address1 { get; set; } = string.Empty;
    public string? Address2 { get; set; }
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ContactPersonName { get; set; }
}

public sealed class CourierShipmentDto
{
    public string? PickUpTimeEarliest { get; set; }
    public string? PickUpTimeLatest { get; set; }
    public string? DeliveryTimeEarliest { get; set; }
    public string? DeliveryTimeLatest { get; set; }
    public string? ShipmentDateTime { get; set; }
}

public sealed class CourierShippingInfoDto
{
    public string? GrossWeight { get; set; }
    public string? GrossVolume { get; set; }
    public string? PickupHandlingInstructions { get; set; }
    public string? LoadMeter { get; set; }
}

public sealed class CourierPackageDto
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
/// Result of a create-booking call to a courier (API, XML, or email).
/// </summary>
public sealed class CourierCreateResult
{
    public bool Success { get; set; }
    /// <summary>Carrier's shipment/order ID when available (not set for email-only flows).</summary>
    public string? CarrierShipmentId { get; set; }
    public string? TrackingNumber { get; set; }
    /// <summary>Label PDF as base64 when the courier returns it.</summary>
    public string? LabelPdfBase64 { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Normalized status/tracking result from a courier.
/// </summary>
public sealed class CourierStatusResult
{
    public string? CarrierShipmentId { get; set; }
    public string? StatusCode { get; set; }
    public string? StatusDescription { get; set; }
    public string? TrackingUrl { get; set; }
    public List<CourierStatusEventDto> Events { get; set; } = new();
}

public sealed class CourierStatusEventDto
{
    public string? Code { get; set; }
    public string? Description { get; set; }
    public DateTime? OccurredAtUtc { get; set; }
}
