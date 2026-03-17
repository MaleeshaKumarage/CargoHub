using System.Security.Claims;
using CargoHub.Application.Auth;
using CargoHub.Application.Company;
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

    public PortalCompanyController(ICompanyRepository companyRepository, UserManager<ApplicationUser> userManager)
    {
        _companyRepository = companyRepository;
        _userManager = userManager;
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
