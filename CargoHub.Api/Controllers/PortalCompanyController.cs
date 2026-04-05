using System.Security.Claims;
using System.Text.Json;
using CargoHub.Application.Auth;
using CargoHub.Application.Company;
using CargoHub.Application.Couriers;
using CargoHub.Domain.Companies;
using CargoHub.Infrastructure.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CargoHub.Api.Controllers;

/// <summary>
/// Portal company/address book endpoints. Requires JWT.
/// SuperAdmin: can see all companies' address books and filter by company. User/Admin: only their own company's address book.
/// </summary>
[ApiController]
[Route("api/v1/portal/company")]
[Authorize]
public class PortalCompanyController : ControllerBase
{
    private readonly ICompanyRepository _companyRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICourierBookingClientFactory _courierFactory;

    public PortalCompanyController(
        ICompanyRepository companyRepository,
        UserManager<ApplicationUser> userManager,
        ICourierBookingClientFactory courierFactory)
    {
        _companyRepository = companyRepository;
        _userManager = userManager;
        _courierFactory = courierFactory;
    }

    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <summary>
    /// Get address book(s). User/Admin: returns their company's address book only (companyId ignored).
    /// SuperAdmin: returns all companies' address books; optional companyId filters to that company.
    /// </summary>
    [HttpGet("address-book")]
    public async Task<ActionResult<AddressBookListResponse>> GetAddressBook([FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        var isSuperAdmin = User.IsInRole(RoleNames.SuperAdmin);

        if (isSuperAdmin)
        {
            if (companyId.HasValue)
            {
                var company = await _companyRepository.GetByIdWithAddressBooksAsync(companyId.Value, cancellationToken);
                if (company == null) return NotFound(new { message = "Company not found." });
                return Ok(new AddressBookListResponse
                {
                    AddressBooks = new List<AddressBookResponse>
                    {
                        ToAddressBookResponse(company)
                    }
                });
            }
            var allCompanies = await _companyRepository.GetAllWithAddressBooksAsync(cancellationToken);
            return Ok(new AddressBookListResponse
            {
                AddressBooks = allCompanies.Select(ToAddressBookResponse).ToList()
            });
        }

        if (user.BusinessId == null) return NotFound(new { message = "User has no company (BusinessId)." });
        var ownCompany = await _companyRepository.GetByBusinessIdWithAddressBooksAsync(user.BusinessId, cancellationToken);
        if (ownCompany == null)
        {
            await EnsureCompanyExistsForBusinessIdAsync(user.BusinessId, cancellationToken);
            ownCompany = await _companyRepository.GetByBusinessIdWithAddressBooksAsync(user.BusinessId, cancellationToken);
        }
        if (ownCompany == null) return NotFound(new { message = "Company not found for your BusinessId." });
        return Ok(new AddressBookListResponse
        {
            AddressBooks = new List<AddressBookResponse> { ToAddressBookResponse(ownCompany) }
        });
    }

    /// <summary>Add a sender to the current user's company address book. SuperAdmin may pass companyId to add to that company.</summary>
    [HttpPost("senders")]
    public async Task<ActionResult<AddressEntryDto>> AddSender([FromBody] AddressEntryDto dto, [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var targetCompanyId = await ResolveCompanyIdForAddAsync(companyId, cancellationToken);
        if (!targetCompanyId.HasValue) return NotFound(new { message = "Company not found. Link your account to a company (BusinessId) or, as SuperAdmin, pass a valid companyId." });
        var address = Map(dto);
        await _companyRepository.AddSenderAsync(targetCompanyId.Value, address, cancellationToken);
        return Ok(Map(address));
    }

    /// <summary>Add a receiver to the current user's company address book. SuperAdmin may pass companyId to add to that company.</summary>
    [HttpPost("receivers")]
    public async Task<ActionResult<AddressEntryDto>> AddReceiver([FromBody] AddressEntryDto dto, [FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var targetCompanyId = await ResolveCompanyIdForAddAsync(companyId, cancellationToken);
        if (!targetCompanyId.HasValue) return NotFound(new { message = "Company not found. Link your account to a company (BusinessId) or, as SuperAdmin, pass a valid companyId." });
        var address = Map(dto);
        await _companyRepository.AddReceiverAsync(targetCompanyId.Value, address, cancellationToken);
        return Ok(Map(address));
    }

    /// <summary>Courier contracts for the signed-in user's company.</summary>
    [HttpGet("courier-contracts")]
    public async Task<ActionResult<CourierContractsResponse>> GetCourierContracts(CancellationToken cancellationToken)
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(user.BusinessId))
            return Ok(new CourierContractsResponse());

        await EnsureCompanyExistsForBusinessIdAsync(user.BusinessId, cancellationToken);
        var company = await _companyRepository.GetByBusinessIdWithAgreementsAsync(user.BusinessId, cancellationToken);
        if (company == null) return Ok(new CourierContractsResponse());

        var contracts = company.AgreementNumbers
            .OrderBy(a => a.PostalService, StringComparer.OrdinalIgnoreCase)
            .Select(ToCourierContractDto)
            .ToList();
        return Ok(new CourierContractsResponse { Contracts = contracts });
    }

    /// <summary>Replace courier contracts. Company Admin only.</summary>
    [HttpPut("courier-contracts")]
    [Authorize(Roles = RoleNames.Admin)]
    public async Task<ActionResult<CourierContractsResponse>> PutCourierContracts(
        [FromBody] PutCourierContractsRequest? body,
        CancellationToken cancellationToken)
    {
        if (body?.Contracts == null)
            return BadRequest(new { message = "contracts is required." });

        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();
        if (string.IsNullOrWhiteSpace(user.BusinessId))
            return BadRequest(new { message = "User has no company (BusinessId)." });

        await EnsureCompanyExistsForBusinessIdAsync(user.BusinessId, cancellationToken);
        var company = await _companyRepository.GetByBusinessIdAsync(user.BusinessId, cancellationToken);
        if (company == null)
            return BadRequest(new { message = "Company not found." });

        var registered = new HashSet<string>(_courierFactory.RegisteredCourierIds, StringComparer.OrdinalIgnoreCase);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var item in body.Contracts)
        {
            var courierId = (item.CourierId ?? "").Trim();
            var contractId = (item.ContractId ?? "").Trim();
            if (string.IsNullOrEmpty(courierId))
                return BadRequest(new { message = "Each item must have courierId." });
            if (string.IsNullOrEmpty(contractId))
                return BadRequest(new { message = "Each item must have contractId." });
            if (!registered.Contains(courierId))
                return BadRequest(new { message = $"Unknown courier: {courierId}" });
            if (!seen.Add(courierId))
                return BadRequest(new { message = $"Duplicate courier: {courierId}" });
        }

        var agreements = body.Contracts
            .Select(c => new AgreementNumber
            {
                PostalService = c.CourierId!.Trim(),
                Number = c.ContractId!.Trim(),
                Service = string.IsNullOrWhiteSpace(c.Service) ? string.Empty : c.Service!.Trim()
            })
            .ToList();

        await _companyRepository.ReplaceAgreementNumbersAsync(company.Id, agreements, cancellationToken);

        var updated = await _companyRepository.GetByBusinessIdWithAgreementsAsync(user.BusinessId, cancellationToken);
        var list = (updated?.AgreementNumbers ?? new List<AgreementNumber>())
            .OrderBy(a => a.PostalService, StringComparer.OrdinalIgnoreCase)
            .Select(ToCourierContractDto)
            .ToList();
        return Ok(new CourierContractsResponse { Contracts = list });
    }

    /// <summary>Booking form field/section rules for the portal (JSON stored per company).</summary>
    [HttpGet("booking-field-rules")]
    public async Task<ActionResult<BookingFieldRulesResponse>> GetBookingFieldRules([FromQuery] Guid? companyId, CancellationToken cancellationToken)
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        var isSuperAdmin = User.IsInRole(RoleNames.SuperAdmin);
        Company? company;
        if (isSuperAdmin)
        {
            if (!companyId.HasValue)
                return BadRequest(new { message = "companyId is required for SuperAdmin." });
            company = await _companyRepository.GetByIdAsync(companyId.Value, cancellationToken);
            if (company == null) return NotFound(new { message = "Company not found." });
        }
        else
        {
            if (user.BusinessId == null) return NotFound(new { message = "User has no company (BusinessId)." });
            await EnsureCompanyExistsForBusinessIdAsync(user.BusinessId, cancellationToken);
            company = await _companyRepository.GetByBusinessIdAsync(user.BusinessId, cancellationToken);
            if (company == null) return NotFound(new { message = "Company not found for your BusinessId." });
        }

        return Ok(ParseRulesFromCompany(company));
    }

    /// <summary>Replace booking field rules. Company Admin or SuperAdmin (with companyId).</summary>
    [HttpPut("booking-field-rules")]
    [Authorize(Roles = $"{RoleNames.Admin},{RoleNames.SuperAdmin}")]
    public async Task<ActionResult<BookingFieldRulesResponse>> PutBookingFieldRules(
        [FromBody] BookingFieldRulesResponse? body,
        [FromQuery] Guid? companyId,
        CancellationToken cancellationToken)
    {
        if (body == null)
            return BadRequest(new { message = "Body is required." });

        var validationError = ValidateBookingFieldRulesBody(body);
        if (validationError != null)
            return BadRequest(new { message = validationError });

        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return Unauthorized();
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return Unauthorized();

        var isSuperAdmin = User.IsInRole(RoleNames.SuperAdmin);
        Guid targetCompanyGuid;
        if (isSuperAdmin)
        {
            if (!companyId.HasValue)
                return BadRequest(new { message = "companyId is required for SuperAdmin." });
            targetCompanyGuid = companyId.Value;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(user.BusinessId))
                return BadRequest(new { message = "User has no company (BusinessId)." });
            await EnsureCompanyExistsForBusinessIdAsync(user.BusinessId, cancellationToken);
            var c = await _companyRepository.GetByBusinessIdAsync(user.BusinessId, cancellationToken);
            if (c == null) return BadRequest(new { message = "Company not found." });
            targetCompanyGuid = c.Id;
        }

        var tracked = await _companyRepository.GetByIdForUpdateAsync(targetCompanyGuid, cancellationToken);
        if (tracked == null) return NotFound(new { message = "Company not found." });

        tracked.Configurations ??= new CompanyConfiguration();
        tracked.Configurations.BookingFieldRulesJson = JsonSerializer.Serialize(NormalizeRulesForStorage(body));
        await _companyRepository.UpdateAsync(tracked, cancellationToken);

        return Ok(ParseRulesFromCompany(tracked));
    }

    private static BookingFieldRulesResponse ParseRulesFromCompany(Company company)
    {
        var json = company.Configurations?.BookingFieldRulesJson;
        if (string.IsNullOrWhiteSpace(json))
            return new BookingFieldRulesResponse { Version = 1, Sections = new Dictionary<string, string>(), Fields = new Dictionary<string, string>() };
        try
        {
            var parsed = JsonSerializer.Deserialize<BookingFieldRulesResponse>(json);
            if (parsed == null)
                return new BookingFieldRulesResponse { Version = 1, Sections = new Dictionary<string, string>(), Fields = new Dictionary<string, string>() };
            return NormalizeRulesForStorage(parsed);
        }
        catch
        {
            return new BookingFieldRulesResponse { Version = 1, Sections = new Dictionary<string, string>(), Fields = new Dictionary<string, string>() };
        }
    }

    private static BookingFieldRulesResponse NormalizeRulesForStorage(BookingFieldRulesResponse body)
    {
        static Dictionary<string, string> NormDict(Dictionary<string, string>? d)
        {
            var o = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (d == null) return o;
            foreach (var kv in d)
            {
                var k = (kv.Key ?? "").Trim();
                if (k.Length == 0) continue;
                var v = (kv.Value ?? "").Trim().ToLowerInvariant();
                if (v != "mandatory" && v != "optional") v = "optional";
                o[k] = v;
            }
            return o;
        }

        return new BookingFieldRulesResponse
        {
            Version = body.Version <= 0 ? 1 : body.Version,
            Sections = NormDict(body.Sections),
            Fields = NormDict(body.Fields)
        };
    }

    private static string? ValidateBookingFieldRulesBody(BookingFieldRulesResponse body)
    {
        if (body.Version > 1)
            return "version must be 1.";
        foreach (var kv in body.Sections ?? new Dictionary<string, string>())
        {
            var v = (kv.Value ?? "").Trim().ToLowerInvariant();
            if (v != "mandatory" && v != "optional" && v.Length > 0)
                return $"Invalid section value for '{kv.Key}'. Use mandatory or optional.";
        }
        foreach (var kv in body.Fields ?? new Dictionary<string, string>())
        {
            var v = (kv.Value ?? "").Trim().ToLowerInvariant();
            if (v != "mandatory" && v != "optional" && v.Length > 0)
                return $"Invalid field value for '{kv.Key}'. Use mandatory or optional.";
        }
        return null;
    }

    private static CourierContractDto ToCourierContractDto(AgreementNumber a) =>
        new()
        {
            Id = a.Id,
            CourierId = a.PostalService,
            ContractId = a.Number,
            Service = string.IsNullOrEmpty(a.Service) ? null : a.Service
        };

    /// <summary>
    /// Creates a company for the given BusinessId if one does not exist. Idempotent.
    /// </summary>
    private async Task EnsureCompanyExistsForBusinessIdAsync(string businessId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(businessId)) return;
        var existing = await _companyRepository.GetByBusinessIdAsync(businessId, cancellationToken);
        if (existing != null) return;
        var trimmed = businessId.Trim();
        var newCompany = new Company
        {
            Id = Guid.NewGuid(),
            BusinessId = trimmed,
            CompanyId = trimmed,
            Name = trimmed
        };
        await _companyRepository.CreateAsync(newCompany, cancellationToken);
    }

    /// <summary>Resolve which company ID to add to. SuperAdmin + companyId = that company; otherwise current user's company.</summary>
    private async Task<Guid?> ResolveCompanyIdForAddAsync(Guid? companyId, CancellationToken cancellationToken)
    {
        if (User.IsInRole(RoleNames.SuperAdmin) && companyId.HasValue)
        {
            var exists = await _companyRepository.GetByIdAsync(companyId.Value, cancellationToken);
            return exists?.Id;
        }
        var company = await GetCurrentUserCompanyAsync(cancellationToken);
        return company?.Id;
    }

    private async Task<Company?> GetCurrentUserCompanyAsync(CancellationToken cancellationToken)
    {
        var userId = UserId;
        if (string.IsNullOrEmpty(userId)) return null;
        var user = await _userManager.FindByIdAsync(userId);
        if (string.IsNullOrWhiteSpace(user?.BusinessId)) return null;
        var company = await _companyRepository.GetByBusinessIdWithAddressBooksAsync(user.BusinessId, cancellationToken);
        if (company == null)
        {
            await EnsureCompanyExistsForBusinessIdAsync(user.BusinessId, cancellationToken);
            company = await _companyRepository.GetByBusinessIdWithAddressBooksAsync(user.BusinessId, cancellationToken);
        }
        return company;
    }

    private static AddressBookResponse ToAddressBookResponse(Company company)
    {
        return new AddressBookResponse
        {
            CompanyId = company.Id,
            CompanyName = company.Name ?? company.CompanyId,
            Senders = company.SenderAddressBook.Select(Map).ToList(),
            Receivers = company.AddressBook.Select(Map).ToList()
        };
    }

    private static CompanyAddress Map(AddressEntryDto dto)
    {
        return new CompanyAddress
        {
            Id = dto.Id ?? Guid.Empty,
            Name = dto.Name ?? "",
            Address1 = dto.Address1 ?? "",
            Address2 = dto.Address2,
            PostalCode = dto.PostalCode ?? "",
            City = dto.City ?? "",
            Country = dto.Country ?? "FI",
            PhoneNumber = dto.PhoneNumber,
            PhoneNumberMobile = false,
            ContactPersonName = dto.ContactPersonName,
            Email = dto.Email,
            County = dto.County,
            VatNo = dto.VatNo,
            CustomerNumber = dto.CustomerNumber
        };
    }

    private static AddressEntryDto Map(CompanyAddress a)
    {
        return new AddressEntryDto
        {
            Id = a.Id,
            Name = a.Name,
            Address1 = a.Address1,
            Address2 = a.Address2,
            PostalCode = a.PostalCode,
            City = a.City,
            Country = a.Country,
            PhoneNumber = a.PhoneNumber,
            ContactPersonName = a.ContactPersonName,
            Email = a.Email,
            County = a.County,
            VatNo = a.VatNo,
            CustomerNumber = a.CustomerNumber
        };
    }
}

/// <summary>Single company's address book (senders + receivers).</summary>
public class AddressBookResponse
{
    public Guid CompanyId { get; set; }
    public string? CompanyName { get; set; }
    public List<AddressEntryDto> Senders { get; set; } = new();
    public List<AddressEntryDto> Receivers { get; set; } = new();
}

/// <summary>List of address books. For User/Admin always one item; for SuperAdmin all companies or filtered.</summary>
public class AddressBookListResponse
{
    public List<AddressBookResponse> AddressBooks { get; set; } = new();
}

public sealed class CourierContractsResponse
{
    public List<CourierContractDto> Contracts { get; set; } = new();
}

public sealed class CourierContractDto
{
    public Guid Id { get; set; }
    public string CourierId { get; set; } = "";
    public string ContractId { get; set; } = "";
    public string? Service { get; set; }
}

public sealed class PutCourierContractsRequest
{
    public List<CourierContractMutationItem>? Contracts { get; set; }
}

public sealed class CourierContractMutationItem
{
    public string? CourierId { get; set; }
    public string? ContractId { get; set; }
    public string? Service { get; set; }
}

/// <summary>Portal booking form mandatory/optional rules (mirrors portal JSON contract).</summary>
public sealed class BookingFieldRulesResponse
{
    public int Version { get; set; } = 1;
    public Dictionary<string, string> Sections { get; set; } = new();
    public Dictionary<string, string> Fields { get; set; } = new();
}

public class AddressEntryDto
{
    public Guid? Id { get; set; }
    public string? Name { get; set; }
    public string? Address1 { get; set; }
    public string? Address2 { get; set; }
    public string? PostalCode { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ContactPersonName { get; set; }
    public string? Email { get; set; }
    public string? County { get; set; }
    public string? VatNo { get; set; }
    public string? CustomerNumber { get; set; }
}
