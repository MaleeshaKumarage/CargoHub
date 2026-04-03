using CargoHub.Api.Options;
using CargoHub.Application.Auth;
using CargoHub.Application.Bookings;
using CargoHub.Application.Bookings.Dtos;
using CargoHub.Application.Company;
using CargoHub.Application.Couriers;
using CargoHub.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TimeZoneConverter;

namespace CargoHub.Api.Services;

/// <summary>Loads bookings, builds PDF, emails company Admins.</summary>
public sealed class DailyBookingDigestOrchestrator : IDailyBookingDigestOrchestrator
{
    private readonly ICompanyRepository _companies;
    private readonly IBookingRepository _bookings;
    private readonly IDailyDigestSendLogRepository _digestLog;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IEmailSender _emailSender;
    private readonly DailyBookingsDigestPdfGenerator _pdf;
    private readonly IOptions<BrandingOptions> _branding;
    private readonly ILogger<DailyBookingDigestOrchestrator> _logger;

    public DailyBookingDigestOrchestrator(
        ICompanyRepository companies,
        IBookingRepository bookings,
        IDailyDigestSendLogRepository digestLog,
        UserManager<ApplicationUser> userManager,
        IEmailSender emailSender,
        DailyBookingsDigestPdfGenerator pdf,
        IOptions<BrandingOptions> branding,
        ILogger<DailyBookingDigestOrchestrator> logger)
    {
        _companies = companies;
        _bookings = bookings;
        _digestLog = digestLog;
        _userManager = userManager;
        _emailSender = emailSender;
        _pdf = pdf;
        _branding = branding;
        _logger = logger;
    }

    public async Task ProcessDigestForLocalDateAsync(DateOnly digestLocalDate, string timeZoneId, bool skipIfEmpty, CancellationToken cancellationToken = default)
    {
        var tzId = (timeZoneId ?? "UTC").Trim();
        var (fromUtc, toUtc) = DailyDigestTimeHelper.GetUtcRangeForLocalDate(digestLocalDate, tzId);
        var tz = TZConvert.GetTimeZoneInfo(tzId);

        var companyList = await _companies.GetAllAsync(cancellationToken);
        foreach (var company in companyList)
        {
            if (string.IsNullOrWhiteSpace(company.BusinessId))
                continue;

            if (!await _digestLog.TryClaimAsync(company.Id, digestLocalDate, tzId, cancellationToken))
                continue;

            try
            {
                await ProcessCompanyAsync(company.Id, company.Name ?? company.CompanyId, company.BusinessId, digestLocalDate, tzId, tz, fromUtc, toUtc, skipIfEmpty, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Daily digest failed for company {CompanyId} on {Date}", company.Id, digestLocalDate);
            }
        }
    }

    private async Task ProcessCompanyAsync(
        Guid companyId,
        string companyDisplayName,
        string businessId,
        DateOnly digestLocalDate,
        string tzId,
        TimeZoneInfo tz,
        DateTime fromUtc,
        DateTime toUtc,
        bool skipIfEmpty,
        CancellationToken cancellationToken)
    {
        var list = await _bookings.ListByCompanyCreatedUtcRangeAsync(companyId, fromUtc, toUtc, cancellationToken);
        var draftCount = list.Count(b => b.IsDraft);
        var confirmedCount = list.Count - draftCount;

        if (list.Count == 0 && skipIfEmpty)
            return;

        var adminEmails = await GetAdminEmailsAsync(businessId, cancellationToken);
        if (adminEmails.Count == 0)
        {
            _logger.LogWarning("Daily digest: no active Admin recipients for company {CompanyId} (BusinessId {BusinessId})", companyId, businessId);
            return;
        }

        var history = await _bookings.GetStatusHistoryForBookingIdsAsync(list.Select(b => b.Id), cancellationToken);
        var customerIds = list.Select(b => b.CustomerId).Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
        var displayByCustomer = await ResolveDisplayNamesAsync(customerIds, cancellationToken);

        var rows = new List<DailyDigestPdfRow>(list.Count);
        foreach (var b in list)
        {
            var events = history.GetValueOrDefault(b.Id) ?? new List<BookingStatusEventDto>();
            var lastStatus = events.Count > 0 ? events[^1].Status : "—";
            displayByCustomer.TryGetValue(b.CustomerId, out var createdBy);
            createdBy = string.IsNullOrWhiteSpace(createdBy) ? (b.CustomerName ?? b.CustomerId) : createdBy;

            var localCreated = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(b.CreatedAtUtc, DateTimeKind.Utc), tz);
            var courier = !string.IsNullOrWhiteSpace(b.Shipment?.CarrierId)
                ? b.Shipment.CarrierId!
                : (b.Header?.PostalService ?? "—");
            var city = !string.IsNullOrWhiteSpace(b.DeliveryPoint?.City)
                ? b.DeliveryPoint.City
                : b.Receiver.City;

            rows.Add(new DailyDigestPdfRow
            {
                BookingId = b.Id,
                Courier = courier,
                ReceiverName = b.Receiver.Name,
                City = string.IsNullOrWhiteSpace(city) ? "—" : city,
                CreatedAtDisplay = localCreated.ToString("yyyy-MM-dd HH:mm"),
                CreatedBy = createdBy,
                Status = lastStatus,
                Reference = b.Header?.ReferenceNumber,
                Waybill = b.WaybillNumber,
                IsDraft = b.IsDraft
            });
        }

        var pdfBytes = _pdf.Generate(companyDisplayName, digestLocalDate, tzId, rows);
        var appName = !string.IsNullOrWhiteSpace(_branding.Value.AppName) ? _branding.Value.AppName : "CargoHub";
        var subject = $"{appName} — Daily bookings summary ({digestLocalDate:yyyy-MM-dd})";
        var body =
            $"<p>Booking activity for <strong>{System.Net.WebUtility.HtmlEncode(companyDisplayName)}</strong> " +
            $"on <strong>{digestLocalDate:yyyy-MM-dd}</strong> ({System.Net.WebUtility.HtmlEncode(tzId)}): " +
            $"<strong>{list.Count}</strong> booking(s) created " +
            $"({confirmedCount} confirmed, {draftCount} draft).</p>" +
            "<p>Details are in the attached PDF.</p>";

        var attachments = new List<EmailAttachment>
        {
            new()
            {
                FileName = $"bookings-digest-{digestLocalDate:yyyy-MM-dd}.pdf",
                ContentType = "application/pdf",
                Content = pdfBytes
            }
        };

        foreach (var email in adminEmails)
        {
            try
            {
                await _emailSender.SendAsync(email, subject, body, attachments, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send daily digest to {Email} for company {CompanyId}", email, companyId);
            }
        }
    }

    private async Task<List<string>> GetAdminEmailsAsync(string businessId, CancellationToken cancellationToken)
    {
        var bid = businessId.Trim();
        var admins = await _userManager.GetUsersInRoleAsync(RoleNames.Admin);
        var list = new List<string>();
        foreach (var u in admins)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!u.IsActive || string.IsNullOrEmpty(u.Email) || !u.EmailConfirmed)
                continue;
            if (string.IsNullOrWhiteSpace(u.BusinessId))
                continue;
            if (!string.Equals(u.BusinessId.Trim(), bid, StringComparison.OrdinalIgnoreCase))
                continue;
            list.Add(u.Email);
        }

        return list.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private async Task<Dictionary<string, string>> ResolveDisplayNamesAsync(List<string> customerIds, CancellationToken cancellationToken)
    {
        if (customerIds.Count == 0)
            return new Dictionary<string, string>(StringComparer.Ordinal);

        var users = await _userManager.Users
            .AsNoTracking()
            .Where(u => u.CustomerMappingId != null && customerIds.Contains(u.CustomerMappingId))
            .Select(u => new { u.CustomerMappingId, u.DisplayName })
            .ToListAsync(cancellationToken);

        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var u in users)
        {
            if (u.CustomerMappingId != null && !string.IsNullOrWhiteSpace(u.DisplayName))
                map[u.CustomerMappingId] = u.DisplayName;
        }

        return map;
    }
}
