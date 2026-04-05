using System.Globalization;
using System.Security.Claims;
using CargoHub.Api.Services;
using CargoHub.Application.Auth;
using CargoHub.Application.Billing;
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
using Microsoft.Extensions.Caching.Memory;

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
    private readonly IImportFileMappingRepository _importFileMappingRepository;
    private readonly IMemoryCache _importSessionCache;

    private const int BulkWaybillMaxIds = 50;
    private static readonly TimeSpan ImportSessionTtl = TimeSpan.FromMinutes(15);

    public PortalBookingsController(
        IMediator mediator,
        IBookingRepository bookingRepository,
        ICompanyRepository companyRepository,
        UserManager<ApplicationUser> userManager,
        ILogger<PortalBookingsController> logger,
        WaybillPdfGenerator waybillPdfGenerator,
        BookingImportService bookingImportService,
        BookingExportService bookingExportService,
        IImportFileMappingRepository importFileMappingRepository,
        IMemoryCache memoryCache)
    {
        _mediator = mediator;
        _bookingRepository = bookingRepository;
        _companyRepository = companyRepository;
        _userManager = userManager;
        _logger = logger;
        _waybillPdfGenerator = waybillPdfGenerator;
        _bookingImportService = bookingImportService;
        _bookingExportService = bookingExportService;
        _importFileMappingRepository = importFileMappingRepository;
        _importSessionCache = memoryCache;
    }

    private string? CustomerId => User.FindFirstValue(ClaimTypes.NameIdentifier);
    private bool IsSuperAdmin => User.IsInRole(RoleNames.SuperAdmin);

    private async Task<CourierValidationError?> ValidateCourierForCompanyBookingAsync(
        Guid? companyId,
        string? postalService,
        CancellationToken cancellationToken)
    {
        var allowed = await BookingCourierValidation.LoadAllowedCourierIdsAsync(_companyRepository, companyId, cancellationToken);
        return BookingCourierValidation.ValidatePostalServiceForCompany(allowed, postalService);
    }

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

    /// <summary>Multi-page waybill PDF for many completed bookings (same order as bookingIds).</summary>
    [HttpPost("waybills/bulk")]
    public async Task<ActionResult> GetBulkWaybillPdf([FromBody] BulkWaybillRequestDto? request)
    {
        var customerId = IsSuperAdmin ? null : CustomerId;
        if (!IsSuperAdmin && string.IsNullOrEmpty(customerId))
            return Unauthorized();
        if (request?.BookingIds == null || request.BookingIds.Count == 0)
            return BadRequest(new { message = "bookingIds is required." });
        if (request.BookingIds.Count > BulkWaybillMaxIds)
            return BadRequest(new { message = $"At most {BulkWaybillMaxIds} bookings per PDF." });

        var seen = new HashSet<Guid>();
        var orderedIds = new List<Guid>();
        foreach (var id in request.BookingIds)
        {
            if (seen.Add(id))
                orderedIds.Add(id);
        }

        var details = new List<BookingDetailDto>();
        foreach (var id in orderedIds)
        {
            var booking = await _mediator.Send(new GetBookingByIdQuery(id, customerId), HttpContext.RequestAborted);
            if (booking == null)
                return BadRequest(new { message = $"Booking {id:N} was not found or is not accessible." });
            if (booking.IsDraft)
                return BadRequest(new { message = $"Booking {id:N} is a draft; waybills are only for completed bookings." });
            details.Add(booking);
        }

        try
        {
            foreach (var b in details)
                await _bookingRepository.TryAddStatusEventAsync(b.Id, BookingStatus.Waybill, "waybill_printed", HttpContext.RequestAborted);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not record Waybill status for bulk waybill.");
        }

        var pdfBytes = _waybillPdfGenerator.GenerateCombined(details);
        return File(pdfBytes, "application/pdf", "waybills.pdf");
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
        var courierError = await ValidateCourierForCompanyBookingAsync(companyId, request.PostalService, HttpContext.RequestAborted);
        if (courierError != null)
            return BadRequest(new { errorCode = courierError.ErrorCode, message = courierError.Message });
        try
        {
            var created = await _mediator.Send(new CreateBookingCommand(customerId, displayName, request, companyId), HttpContext.RequestAborted);
            if (created == null)
                return BadRequest();
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }
        catch (SubscriptionBillingException ex)
        {
            return SubscriptionBillingError(ex);
        }
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
        var courierError = await ValidateCourierForCompanyBookingAsync(companyId, request.PostalService, HttpContext.RequestAborted);
        if (courierError != null)
            return BadRequest(new { errorCode = courierError.ErrorCode, message = courierError.Message });
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
        var (companyId, _) = await GetCompanyIdAndAllowedSlotsAsync(HttpContext.RequestAborted);
        var draftDetail = await _mediator.Send(new GetDraftByIdQuery(id, customerId), HttpContext.RequestAborted);
        if (draftDetail == null)
            return NotFound();
        var effectivePostal = request.PostalService != null ? request.PostalService : draftDetail.Header?.PostalService;
        var courierError = await ValidateCourierForCompanyBookingAsync(companyId, effectivePostal, HttpContext.RequestAborted);
        if (courierError != null)
            return BadRequest(new { errorCode = courierError.ErrorCode, message = courierError.Message });
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
        var (companyId, _) = await GetCompanyIdAndAllowedSlotsAsync(HttpContext.RequestAborted);
        var draftDetail = await _mediator.Send(new GetDraftByIdQuery(id, customerId), HttpContext.RequestAborted);
        if (draftDetail == null)
            return NotFound();
        var courierError = await ValidateCourierForCompanyBookingAsync(companyId, draftDetail.Header?.PostalService, HttpContext.RequestAborted);
        if (courierError != null)
            return BadRequest(new { errorCode = courierError.ErrorCode, message = courierError.Message });
        try
        {
            var result = await _mediator.Send(new ConfirmDraftCommand(id, customerId), HttpContext.RequestAborted);
            if (result == null)
                return NotFound();
            return Ok(result);
        }
        catch (SubscriptionBillingException ex)
        {
            return SubscriptionBillingError(ex);
        }
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

    /// <summary>Analyze upload: if headers match export (trim + BOM), same as preview; otherwise store raw rows for <see cref="ImportApplyMapping"/>.</summary>
    [HttpPost("import/analyze")]
    [RequestSizeLimit(25_000_000)]
    public async Task<ActionResult<ImportAnalyzeResponseDto>> ImportAnalyze([FromForm] IFormFile? file)
    {
        var customerId = CustomerId;
        if (string.IsNullOrEmpty(customerId))
            return Unauthorized();
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });
        await using var stream = file.OpenReadStream();
        var analysis = _bookingImportService.AnalyzeImport(stream, file.FileName);
        if (analysis.Error != null)
            return BadRequest(new { message = analysis.Error });
        var sessionId = Guid.NewGuid();
        if (!analysis.NeedsMapping)
        {
            var parsed = analysis.ParsedRows!;
            var importRows = parsed.Select(r => new ImportRowDto(r.Request, r.IsComplete)).ToList();
            var key = ImportSessionCacheKey(customerId, sessionId);
            _importSessionCache.Set(key, importRows, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ImportSessionTtl });
            var completed = importRows.Count(r => r.IsComplete);
            var drafts = importRows.Count - completed;
            return Ok(new ImportAnalyzeResponseDto
            {
                NeedsMapping = false,
                SessionId = sessionId,
                CompletedCount = completed,
                DraftCount = drafts,
                SkippedEmptyRows = analysis.SkippedEmptyRows,
                TotalDataRows = importRows.Count,
            });
        }

        if (analysis.RawRows!.Count == 0)
            return BadRequest(new { message = "No data rows in file." });

        var (companyId, _) = await GetCompanyIdAndAllowedSlotsAsync(HttpContext.RequestAborted);
        var fileNameKey = ImportMappingSignature.NormalizeFileNameKey(file.FileName);
        var headerSignature = ImportMappingSignature.BuildHeaderSignature(analysis.FileHeaders!);
        Dictionary<string, string?>? savedColumnMap = null;
        if (companyId is { } cid)
        {
            var saved = await _importFileMappingRepository.GetColumnMapAsync(cid, fileNameKey, headerSignature, HttpContext.RequestAborted);
            if (saved is { Count: > 0 })
                savedColumnMap = new Dictionary<string, string?>(saved, StringComparer.Ordinal);
        }

        var rawKey = ImportRawSessionCacheKey(customerId, sessionId);
        var state = new ImportRawSessionState
        {
            Rows = analysis.RawRows,
            FileHeaders = analysis.FileHeaders!,
            SkippedEmptyRows = analysis.SkippedEmptyRows,
            FileNameKey = fileNameKey,
            HeaderSignature = headerSignature,
        };
        _importSessionCache.Set(rawKey, state, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ImportSessionTtl });
        return Ok(new ImportAnalyzeResponseDto
        {
            NeedsMapping = true,
            SessionId = sessionId,
            FileHeaders = analysis.FileHeaders,
            BookingFields = BookingImportService.BookingImportFieldNames.ToList(),
            HasSavedMapping = savedColumnMap is { Count: > 0 },
            SavedColumnMap = savedColumnMap,
        });
    }

    /// <summary>Apply column mapping for a raw import session; returns the same shape as import/preview for confirm.</summary>
    [HttpPost("import/apply-mapping")]
    public async Task<ActionResult<ImportPreviewResponseDto>> ImportApplyMapping([FromBody] ImportApplyMappingRequestDto? body)
    {
        var customerId = CustomerId;
        if (string.IsNullOrEmpty(customerId))
            return Unauthorized();
        if (body == null)
            return BadRequest(new { message = "Request body is required." });
        var rawKey = ImportRawSessionCacheKey(customerId, body.SessionId);
        if (!_importSessionCache.TryGetValue(rawKey, out ImportRawSessionState? raw) || raw == null)
            return BadRequest(new { message = "Import session expired or invalid. Upload the file again." });

        var merged = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var h in BookingImportService.BookingImportFieldNames)
            merged[h] = null;
        if (body.ColumnMap != null)
        {
            foreach (var kv in body.ColumnMap)
            {
                if (merged.ContainsKey(kv.Key))
                    merged[kv.Key] = string.IsNullOrWhiteSpace(kv.Value) ? null : kv.Value;
            }
        }

        var parseError = _bookingImportService.ParseFromMappedRows(raw.Rows, merged, raw.FileHeaders, out var rows);
        if (parseError != null)
            return BadRequest(new { message = parseError });

        _importSessionCache.Remove(rawKey);
        var importRows = rows.Select(r => new ImportRowDto(r.Request, r.IsComplete)).ToList();
        var parsedKey = ImportSessionCacheKey(customerId, body.SessionId);
        _importSessionCache.Set(parsedKey, importRows, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ImportSessionTtl });
        var completed = importRows.Count(r => r.IsComplete);
        var drafts = importRows.Count - completed;

        if (body.SaveMappingForCompany)
        {
            var (companyId, _) = await GetCompanyIdAndAllowedSlotsAsync(HttpContext.RequestAborted);
            if (companyId is { } saveCid
                && !string.IsNullOrEmpty(raw.FileNameKey)
                && !string.IsNullOrEmpty(raw.HeaderSignature))
            {
                await _importFileMappingRepository.UpsertAsync(
                    saveCid,
                    raw.FileNameKey,
                    raw.HeaderSignature,
                    merged,
                    HttpContext.RequestAborted);
            }
        }

        return Ok(new ImportPreviewResponseDto(body.SessionId, completed, drafts, raw.SkippedEmptyRows, importRows.Count));
    }

    /// <summary>Parse upload and return counts; stores rows in server cache for <see cref="ImportConfirm"/>.</summary>
    [HttpPost("import/preview")]
    [RequestSizeLimit(25_000_000)]
    public async Task<ActionResult<ImportPreviewResponseDto>> ImportPreview([FromForm] IFormFile? file)
    {
        var customerId = CustomerId;
        if (string.IsNullOrEmpty(customerId))
            return Unauthorized();
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });
        await using var stream = file.OpenReadStream();
        var parseError = _bookingImportService.Parse(stream, file.FileName, out var rows, out var skippedEmpty);
        if (parseError != null)
            return BadRequest(new { message = parseError });
        var sessionId = Guid.NewGuid();
        var importRows = rows.Select(r => new ImportRowDto(r.Request, r.IsComplete)).ToList();
        var key = ImportSessionCacheKey(customerId, sessionId);
        _importSessionCache.Set(key, importRows, new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = ImportSessionTtl });
        var completed = importRows.Count(r => r.IsComplete);
        var drafts = importRows.Count - completed;
        return Ok(new ImportPreviewResponseDto(sessionId, completed, drafts, skippedEmpty, importRows.Count));
    }

    /// <summary>Creates only the categories the user opted into; consumes the preview session.</summary>
    [HttpPost("import/confirm")]
    public async Task<ActionResult<ImportBookingsResult>> ImportConfirm([FromBody] ImportConfirmRequestDto? body)
    {
        var customerId = CustomerId;
        if (string.IsNullOrEmpty(customerId))
            return Unauthorized();
        if (body == null)
            return BadRequest(new { message = "Request body is required." });
        var key = ImportSessionCacheKey(customerId, body.SessionId);
        if (!_importSessionCache.TryGetValue(key, out List<ImportRowDto>? cached) || cached == null)
            return BadRequest(new { message = "Import session expired or invalid. Upload the file again." });
        _importSessionCache.Remove(key);

        var toImport = new List<ImportRowDto>();
        foreach (var row in cached)
        {
            if (row.IsComplete && body.ImportCompleted)
                toImport.Add(row);
            else if (!row.IsComplete && body.ImportDrafts)
                toImport.Add(row);
        }
        if (toImport.Count == 0)
            return BadRequest(new { message = "Nothing selected to import." });

        var displayName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? "";
        var (companyId, _) = await GetCompanyIdAndAllowedSlotsAsync(HttpContext.RequestAborted);
        var allowed = await BookingCourierValidation.LoadAllowedCourierIdsAsync(_companyRepository, companyId, HttpContext.RequestAborted);
        foreach (var row in toImport)
        {
            var err = BookingCourierValidation.ValidatePostalServiceForCompany(allowed, row.Request.PostalService);
            if (err != null)
                return BadRequest(new { errorCode = err.ErrorCode, message = err.Message });
        }

        var result = await _mediator.Send(
            new ImportBookingsCommand(customerId, displayName, companyId, toImport),
            HttpContext.RequestAborted);
        return Ok(result);
    }

    /// <summary>Import bookings from CSV or Excel in one step (all rows). Prefer import/preview + import/confirm from the portal.</summary>
    [HttpPost("import")]
    [RequestSizeLimit(25_000_000)]
    public async Task<ActionResult<ImportBookingsResult>> ImportBookings([FromForm] IFormFile? file)
    {
        var customerId = CustomerId;
        if (string.IsNullOrEmpty(customerId))
            return Unauthorized();
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "No file uploaded." });
        await using var stream = file.OpenReadStream();
        var parseError = _bookingImportService.Parse(stream, file.FileName, out var rows, out _);
        if (parseError != null)
            return BadRequest(new { message = parseError });
        var importRows = rows.Select(r => new ImportRowDto(r.Request, r.IsComplete)).ToList();
        var displayName = User.FindFirstValue(ClaimTypes.Name) ?? User.FindFirstValue(ClaimTypes.Email) ?? "";
        var (companyId, _) = await GetCompanyIdAndAllowedSlotsAsync(HttpContext.RequestAborted);
        var allowed = await BookingCourierValidation.LoadAllowedCourierIdsAsync(_companyRepository, companyId, HttpContext.RequestAborted);
        foreach (var row in importRows)
        {
            var err = BookingCourierValidation.ValidatePostalServiceForCompany(allowed, row.Request.PostalService);
            if (err != null)
                return BadRequest(new { errorCode = err.ErrorCode, message = err.Message });
        }

        var result = await _mediator.Send(
            new ImportBookingsCommand(customerId, displayName, companyId, importRows),
            HttpContext.RequestAborted);
        return Ok(result);
    }

    private static string ImportSessionCacheKey(string customerId, Guid sessionId) =>
        $"booking-import:{customerId}:{sessionId:N}";

    private static string ImportRawSessionCacheKey(string customerId, Guid sessionId) =>
        $"booking-import-raw:{customerId}:{sessionId:N}";

    private static ActionResult SubscriptionBillingError(SubscriptionBillingException ex) =>
        ex.ErrorCode == SubscriptionBillingConstants.CompanyRequiredForBookingErrorCode
            ? new BadRequestObjectResult(new { errorCode = ex.ErrorCode, message = ex.Message })
            : new ConflictObjectResult(new { errorCode = ex.ErrorCode, message = ex.Message });

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
