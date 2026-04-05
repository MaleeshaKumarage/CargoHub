using CargoHub.Application.AdminEmail;
using CargoHub.Application.Auth;
using CargoHub.Application.Company;
using CargoHub.Application.Couriers;
using CargoHub.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Infrastructure.AdminEmail;

public sealed class AdminReleaseNotesBroadcaster : IAdminReleaseNotesBroadcaster
{
    private static readonly HashSet<string> ValidRoles = new(StringComparer.Ordinal)
    {
        RoleNames.SuperAdmin,
        RoleNames.Admin,
        RoleNames.User,
    };

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ICompanyRepository _companies;
    private readonly IEmailSender _emailSender;

    public AdminReleaseNotesBroadcaster(
        UserManager<ApplicationUser> userManager,
        ICompanyRepository companies,
        IEmailSender emailSender)
    {
        _userManager = userManager;
        _companies = companies;
        _emailSender = emailSender;
    }

    public async Task<(ReleaseNotesBroadcastResult? Result, string? ErrorMessage)> TryBroadcastAsync(
        ReleaseNotesBroadcastRequest request,
        CancellationToken cancellationToken = default)
    {
        var subject = request.Subject?.Trim() ?? "";
        var body = request.BodyPlain ?? "";
        if (string.IsNullOrEmpty(subject))
            return (null, "Subject is required.");
        if (string.IsNullOrWhiteSpace(body))
            return (null, "Body is required.");
        if (body.Length > ReleaseNotesEmailBodyFormatter.MaxBodyLength)
            return (null, $"Body exceeds maximum length ({ReleaseNotesEmailBodyFormatter.MaxBodyLength} characters).");

        HashSet<string>? businessIds = null;
        if (!request.AllCompanies)
        {
            var ids = request.CompanyIds?.Where(x => x != Guid.Empty).Distinct().ToList() ?? new List<Guid>();
            if (ids.Count == 0)
                return (null, "Select at least one company, or choose all companies.");

            businessIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var id in ids)
            {
                var c = await _companies.GetByIdAsync(id, cancellationToken);
                if (c == null)
                    return (null, $"Company not found: {id}.");
                var bid = c.BusinessId?.Trim();
                if (!string.IsNullOrEmpty(bid))
                    businessIds.Add(bid);
            }

            if (businessIds.Count == 0)
                return (null, "Selected companies have no business ID; there are no recipients to match.");
        }

        HashSet<string>? roleFilter = null;
        if (!request.AllRoles)
        {
            var roles = request.Roles?.Select(r => r?.Trim() ?? "").Where(r => r.Length > 0).Distinct(StringComparer.Ordinal).ToList()
                ?? new List<string>();
            if (roles.Count == 0)
                return (null, "Select at least one role, or choose all roles.");
            foreach (var r in roles)
            {
                if (!ValidRoles.Contains(r))
                    return (null, $"Invalid role: {r}. Use SuperAdmin, Admin, or User.");
            }

            roleFilter = new HashSet<string>(roles, StringComparer.Ordinal);
        }

        var candidates = await _userManager.Users
            .Where(u => u.IsActive && u.Email != null && u.Email != "")
            .ToListAsync(cancellationToken);

        var matched = new List<ApplicationUser>();
        foreach (var u in candidates)
        {
            if (businessIds != null)
            {
                var ub = u.BusinessId?.Trim();
                if (string.IsNullOrEmpty(ub) || !businessIds.Contains(ub))
                    continue;
            }

            if (roleFilter != null)
            {
                var userRoles = await _userManager.GetRolesAsync(u);
                if (!userRoles.Any(r => roleFilter.Contains(r)))
                    continue;
            }

            matched.Add(u);
        }

        // One send per distinct email (avoid duplicate inboxes if data is inconsistent).
        var byEmail = matched
            .GroupBy(u => u.Email!.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .ToList();

        if (byEmail.Count == 0)
            return (null, "No recipients match the selected filters.");

        var html = ReleaseNotesEmailBodyFormatter.ToHtml(body);
        var failures = new List<ReleaseNotesSendFailure>();
        var sent = 0;
        foreach (var u in byEmail)
        {
            var to = u.Email!.Trim();
            try
            {
                await _emailSender.SendAsync(to, subject, html, cancellationToken);
                sent++;
            }
            catch (Exception ex)
            {
                failures.Add(new ReleaseNotesSendFailure { Email = to, Message = ex.Message });
            }
        }

        return (new ReleaseNotesBroadcastResult
        {
            RecipientCount = byEmail.Count,
            SentCount = sent,
            Failures = failures,
        }, null);
    }
}
