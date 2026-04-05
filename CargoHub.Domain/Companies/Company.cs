namespace CargoHub.Domain.Companies;

/// <summary>
/// Company configuration aggregate.
/// Represents shipper level settings, address books and agreement numbers
/// that are currently stored in MongoDB as ICompanyRecord.
/// </summary>
public class Company
{
    /// <summary>
    /// Primary key for the company row.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Logical company identifier used across the system. Auto-generated as GUID if not provided (aligns with booking-backend companyId).
    /// </summary>
    public string CompanyId { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the company (e.g. "Acme Oy"). Aligns with booking-backend usage where company is identified by companyId and businessId.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Owner/customer id (user's CustomerMappingId). Links company to the user who created it.
    /// </summary>
    public string? CustomerId { get; set; }

    /// <summary>
    /// Optional business identifier (e.g. VAT or national business id). Used for user registration and matching booking-backend businessId.
    /// </summary>
    public string? BusinessId { get; set; }

    /// <summary>
    /// Optional sender number used by carriers.
    /// </summary>
    public string? SenderNumber { get; set; }

    /// <summary>
    /// Optional division code used by some customers.
    /// </summary>
    public string? DivisionCode { get; set; }

    /// <summary>
    /// Running counter for generating sequential numbers per company.
    /// </summary>
    public int Counter { get; set; } = 1;

    /// <summary>
    /// Maximum active portal users linked to this company (same <see cref="BusinessId"/>). Null = no limit.
    /// </summary>
    public int? MaxUserAccounts { get; set; }

    /// <summary>
    /// Maximum users with Admin role for this company. Null = no limit.
    /// </summary>
    public int? MaxAdminAccounts { get; set; }

    /// <summary>
    /// When set, the initial admin invite targeted this explicit email. Null if only the fallback address was used.
    /// </summary>
    public string? InitialAdminInviteEmail { get; set; }

    /// <summary>JSON array of explicit admin invite emails (e.g. <c>["a@x.com","b@x.com"]</c>). Null when only fallback or legacy single field.</summary>
    public string? InitialAdminInviteEmailsJson { get; set; }

    /// <summary>
    /// Default shipper address used when booking does not override it.
    /// </summary>
    public CompanyAddress? DefaultShipperAddress { get; set; }

    /// <summary>
    /// Sender/shipper address book. One company can have multiple senders.
    /// </summary>
    public ICollection<CompanyAddress> SenderAddressBook { get; set; } = new List<CompanyAddress>();

    /// <summary>
    /// Receiver address book. One company can have multiple receivers. Not shared with other companies.
    /// </summary>
    public ICollection<CompanyAddress> AddressBook { get; set; } = new List<CompanyAddress>();

    /// <summary>
    /// Additional pick-up addresses for this company.
    /// </summary>
    public ICollection<CompanyAddress> PickUpAddressBook { get; set; } = new List<CompanyAddress>();

    /// <summary>
    /// Agreement numbers that tie this company to postal services and products.
    /// </summary>
    public ICollection<AgreementNumber> AgreementNumbers { get; set; } = new List<AgreementNumber>();

    /// <summary>
    /// Additional configuration values influencing booking defaults.
    /// </summary>
    public CompanyConfiguration? Configurations { get; set; }
}

/// <summary>
/// Shared address structure reused by address books and default shipper address.
/// </summary>
public class CompanyAddress
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address1 { get; set; } = string.Empty;
    public string? Address2 { get; set; }
    public string PostalCode { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool PhoneNumberMobile { get; set; }
    public string? ContactPersonName { get; set; }
    public string? Email { get; set; }
    public string? County { get; set; }
    public string? VatNo { get; set; }
    public string? CustomerNumber { get; set; }
}

/// <summary>
/// Agreement number for a particular postal service and possibly service/product.
/// </summary>
public class AgreementNumber
{
    public Guid Id { get; set; }

    /// <summary>
    /// Name of the postal service (e.g. Posti, Postnord, DHL).
    /// Stored as string to keep it flexible across carriers.
    /// </summary>
    public string PostalService { get; set; } = string.Empty;

    /// <summary>
    /// Optional service code or product identifier used by that postal service.
    /// </summary>
    public string Service { get; set; } = string.Empty;

    /// <summary>
    /// Contract or agreement number used when booking shipments.
    /// </summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>
    /// Optional counter used for generating contiguous shipment ranges.
    /// </summary>
    public int? Counter { get; set; }
}

/// <summary>
/// Optional configuration section for company specific defaults.
/// Mirrors IConfigurations from the Node.js types.
/// </summary>
public class CompanyConfiguration
{
    public string? DefaultPostalService { get; set; }
    public string? ShipperVatNo { get; set; }
    public string? PickUpAddressVatNo { get; set; }
    public string? FreightPayer { get; set; }
    public string? Service { get; set; }
    public string? PhoneNumber { get; set; }

    /// <summary>JSON document for portal booking form mandatory/optional rules per section and field.</summary>
    public string? BookingFieldRulesJson { get; set; }
}

