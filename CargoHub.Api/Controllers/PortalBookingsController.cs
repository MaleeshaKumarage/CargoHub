using System.Globalization;
using System.Security.Claims;
using CargoHub.Api.Services;
using CargoHub.Application.Auth;
using CargoHub.Application.Bookings;
using CargoHub.Application.Company;
using CargoHub.Application.Bookings.Commands;
using CargoHub.Application.Bookings.Dtos;
using CargoHub.Application.Bookings.Queries;
using CargoHub.Domain.Bookings;
using CargoHub.Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CargoHub.Api.Controllers;

/// <summary>
/// Portal booking endpoints. Requires JWT; user id from claims is used as CustomerId.
/// Bookings can be saved as draft; retrieve and fill the rest, then confirm to complete.
/// </summary>
[ApiController]
[Route("api/v1/portal/bookings")]
[Authorize]
public class PortalBookingsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IBookingRepository _bookingRepository;
    private readonly ICompanyRepository _companyRepository;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<PortalBookingsController> _logger;
    private readonly WaybillPdfGenerator _waybillPdfGenerator;
    private readonly BookingImportService _bookingImportService;
    private readonly BookingExportService _bookingExportService;

    public PortalBookingsController(
        IMediator mediator,
        IBookingRepository bookingRepository,
        ICompanyRepository companyRepository,
        UserManager<ApplicationUser> userManager,
        ILogger<PortalBookingsController> logger,
        WaybillPdfGenerator waybillPdfGenerator,
        BookingImportService bookingImportService,
        BookingExportService bookingExportService)
    {
        _mediator = mediator;
        _bookingRepository = bookingRepository;
        _companyRepository = companyRepository;
        _userManager = userManager;
        _logger = logger;
        _waybillPdfGenerator = waybillPdfGenerator;
        _bookingImportService = bookingImportService;
        _bookingExportService = bookingExportService;
    }

    private string? CustomerId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool IsSuperAdmin => User.IsInRole(RoleNames.SuperAdmin);

    /// <summary>List completed bookings. SuperAdmin sees all companies; others see only their own.</summary>
    [HttpGet]
    public async Task<ActionResult<List<BookingListDto>>> List([FromQuery] int skip = 0, [FromQuery] int take = 100)
    {
        var customerId = IsSuperAdmin ? null : CustomerId;
        if (!IsSuperAdmin && string.IsNullOrEmpty(customerId))
            return Unauthorized();
        var list = await _mediator.Send(new ListBookingsQuery(customerId, skip, take), HttpContext.RequestAborted);
        return Ok(list);
    }

    /// <summary>Generate and download a sample waybill PDF for a completed booking. Route must be before GetById so /waybill is matched.</summary>
    [HttpGet("{id:guid}/waybill")]
    public async Task<ActionResult> GetWaybillPdf(Guid id)
    {
        var customerId = IsSuperAdmin ? null : CustomerId;
        if (!IsSuperAdmin && string.IsNullOrEmpty(customerId))
            return Unauthorized();
        var booking = await _mediator.Send(new GetBookingByIdQuery(id, customerId), HttpContext.RequestAborted);
        if (booking == null)
            return NotFound();
        if (booking.IsDraft)
            return BadRequest(new { message = "Waybill is available only for completed bookings." });
        try
        {
            await _bookingRepository.TryAddStatusEventAsync(id, BookingStatus.Waybill, "waybill_printed", HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            // Do not fail waybill download; log so missing table or DB errors can be fixed (e.g. run migrations).
            _logger.LogWarning(ex, "Could not record Waybill status for booking {BookingId}. Ensure migrations are applied.", id);
        }
        var pdfBytes = _waybillPdfGenerator.Generate(booking);
        var fileName = $"waybill-{id:N}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    /// <summary>Get a single completed booking by id. SuperAdmin can view any; others only own. Returns 404 if not found or not owned.</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<BookingDetailDto>> GetById(Guid id)
    {
        var customerId = IsSuperAdmin ? null : CustomerId;
        if (!IsSuperAdmin && string.IsNullOrEmpty(customerId))
            return Unauthorized();
        var booking = await _mediator.Send(new GetBookingByIdQuery(id, customerId), HttpContext.RequestAborted);
        if (booking == null)
            return NotFound();
        return Ok(booking);
    }

    /// <summary>Create a new completed booking for the current user.</summary>
    [HttpPost]
    public async Task<ActionResult<BookingDetailDto>> Create([FromBody] CreateBookingRequest request)
    {
        var customerId = CustomerId;
        if (string.IsNullOrEmpty(customerId))
            return Unauthorized();
        var displayName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email);
        var (companyId, _) = await GetCompanyIdAndAllowedSlotsAsync(HttpContext.RequestAborted);
        var created = await _mediator.Send(new CreateBookingCommand(customerId, displayName, request, companyId), HttpContext.RequestAborted);
        if (created == null)
            return BadRequest();
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // ---- Drafts: save as draft, retrieve, fill rest, confirm to complete ----

    /// <summary>Create a draft booking. Retrieve later to fill the rest and confirm.</summary>
    [HttpPost("draft")]
    public async Task<ActionResult<BookingDetailDto>> CreateDraft([FromBody] CreateBookingRequest request)
    {
        var customerId = CustomerId;
        if (string.IsNullOrEmpty(customerId))
            return Unauthorized();
        var displayName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email);
        var (companyId, _) = await GetCompanyIdAndAllowedSlotsAsync(HttpContext.RequestAborted);
        var created = await _mediator.Send(new CreateDraftCommand(customerId, displayName, request, companyId), HttpContext.RequestAborted);
        if (created == null)
            return BadRequest();
        return CreatedAtAction(nameof(GetDraftById), new { id = created.Id }, created);
    }

    /// <summary>List draft bookings. SuperAdmin sees all companies; others see only their own.</summary>
    [HttpGet("draft")]
    public async Task<ActionResult<List<BookingListDto>>> ListDrafts([FromQuery] int skip = 0, [FromQuery] int take = 100)
    {
        var customerId = IsSuperAdmin ? null : CustomerId;
        if (!IsSuperAdmin && string.IsNullOrEmpty(customerId))
            return Unauthorized();
        var list = await _mediator.Send(new ListDraftsQuery(customerId, skip, take), HttpContext.RequestAborted);
        return Ok(list);
    }

    /// <summary>Get a single draft by id. SuperAdmin can view any; others only own. Returns 404 if not found or not a draft.</summary>
    [HttpGet("draft/{id:guid}")]
    public async Task<ActionResult<BookingDetailDto>> GetDraftById(Guid id)
    {
        var customerId = IsSuperAdmin ? null : CustomerId;
        if (!IsSuperAdmin && string.IsNullOrEmpty(customerId))
            return Unauthorized();
        var draft = await _mediator.Send(new GetDraftByIdQuery(id, customerId), HttpContext.RequestAborted);
        if (draft == null)
            return NotFound();
        return Ok(draft);
    }

    /// <summary>Update a draft. Fill the rest of the fields and save.</summary>
    [HttpPatch("draft/{id:guid}")]
    public async Task<ActionResult<BookingDetailDto>> UpdateDraft(Guid id, [FromBody] UpdateDraftRequest request)
    {
        var customerId = CustomerId;
        if (string.IsNullOrEmpty(customerId))
            return Unauthorized();
        var updated = await _mediator.Send(new UpdateDraftCommand(id, customerId, request), HttpContext.RequestAborted);
        if (updated == null)
            return NotFound();
        return Ok(updated);
    }

    /// <summary>Confirm a draft: marks it as completed. The booking then appears in the main list.</summary>
    [HttpPost("draft/{id:guid}/confirm")]
    public async Task<ActionResult<BookingDetailDto>> ConfirmDraft(Guid id)
    {
        var customerId = CustomerId;
        if (string.IsNullOrEmpty(customerId))
            return Unauthorized();
        var result = await _mediator.Send(new ConfirmDraftCommand(id, customerId), HttpContext.RequestAborted);
        if (result == null)
            return NotFound();
        return Ok(result);
    }

    /// <summary>Download completed bookings as CSV or Excel (columns match export/import template).</summary>
    [HttpGet("export")]
    public async Task<ActionResult> ExportBookings([FromQuery] string format = "csv")
    {
        var customerId = IsSuperAdmin ? null : CustomerId;
        if (!IsSuperAdmin && string.IsNullOrEmpty(customerId))
            return Unauthorized();
        var fmt = string.IsNullOrWhiteSpace(format) ? "csv" : format.Trim().ToLowerInvariant();
        if (fmt is not ("csv" or "xlsx" or "xls"))
            return BadRequest(new { message = "format must be csv or xlsx" });
        var details = await _mediator.Send(new ExportBookingsQuery(customerId, 0, 10_000, null), HttpContext.RequestAborted);
        var stamp = DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
        if (fmt == "csv")
        {
            var bytes = _bookingExportService.ExportBulkToCsv(details);
            return File(bytes, "text/csv; charset=utf-8", $"bookings-export-{stamp}.csv");
        }
        var excel = _bookingExportService.ExportBulkToExcel(details);
        return File(
            excel,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"bookings-export-{stamp}.xlsx");
    }

    /// <summary>Import bookings from CSV or Excel. Headers must match export format.</summary>
    [HttpPost("import")]
    [RequestSizeLimit(25_000_000)]
    public async Task<ActionResult<ImportBookingsResult>> ImportBookings(IFormFile? file)
    {
        var customerId = CustomerId;
        if (string.IsNullOrEmpty(customerId))
            return Unauthorized();
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });
        await using var stream = file.OpenReadStream();
        var parseError = _bookingImportService.Parse(stream, file.FileName, out var rows);
        if (parseError != null)
            return BadRequest(new { message = parseError });
        var importRows = rows.Select(r => new ImportRowDto(r.Request, r.IsComplete)).ToList();
        var displayName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? "";
        var (companyId, _) = await GetCompanyIdAndAllowedSlotsAsync(HttpContext.RequestAborted);
        var result = await _mediator.Send(
            new ImportBookingsCommand(customerId, displayName, companyId, importRows),
            HttpContext.RequestAborted);
        return Ok(result);
    }

    /// <summary>Returns (CompanyId, _) for the current user's company. CompanyId is null if user has no company.</summary>
    private async Task<(Guid? CompanyId, IReadOnlySet<string>?)> GetCompanyIdAndAllowedSlotsAsync(CancellationToken cancellationToken)
    {
        var userId = CustomerId;
        if (string.IsNullOrEmpty(userId)) return (null, null);
        var user = await _userManager.FindByIdAsync(userId);
        if (string.IsNullOrWhiteSpace(user?.BusinessId)) return (null, null);
        var company = await _companyRepository.GetByBusinessIdAsync(user.BusinessId, cancellationToken);
        if (company == null) return (null, null);
        return (company.Id, null);
    }
}
