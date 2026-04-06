using System.Security.Claims;
using CargoHub.Application.Auth;
using CargoHub.Application.Auth.Abstractions;
using CargoHub.Application.Auth.Dtos;
using CargoHub.Application.Company;
using CargoHub.Infrastructure.Identity;
using CargoHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;

namespace CargoHub.Infrastructure.Auth;

public sealed class AcceptCompanyAdminInviteRunner : IAcceptCompanyAdminInviteRunner
{
    private readonly ICompanyAdminInviteRepository _invites;
    private readonly ICompanyRepository _companies;
    private readonly IUserRegistrationService _registration;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenFactory _jwtTokenFactory;
    private readonly ICompanyUserMetrics _metrics;

    public AcceptCompanyAdminInviteRunner(
        ICompanyAdminInviteRepository invites,
        ICompanyRepository companies,
        IUserRegistrationService registration,
        UserManager<ApplicationUser> userManager,
        IJwtTokenFactory jwtTokenFactory,
        ICompanyUserMetrics metrics)
    {
        _invites = invites;
        _companies = companies;
        _registration = registration;
        _userManager = userManager;
        _jwtTokenFactory = jwtTokenFactory;
        _metrics = metrics;
    }

    public async Task<RegisterResult> RunAsync(string rawToken, string password, string userName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return Fail("InviteTokenRequired", "Invitation token is required.");
        if (string.IsNullOrWhiteSpace(password))
            return Fail("PasswordRequired", "Password is required.");

        var hash = CompanyInviteTokenHelper.HashRawToken(rawToken.Trim());
        var invite = await _invites.GetActiveByTokenHashAsync(hash, cancellationToken);
        if (invite?.Company == null)
            return Fail("InviteInvalid", "This invitation link is invalid or has expired.");

        var company = invite.Company;
        if (string.IsNullOrWhiteSpace(company.BusinessId))
            return Fail("CompanyMisconfigured", "Company has no Business ID; contact support.");

        if (!company.IsActive)
            return Fail("CompanyInactive", AuthMessages.CompanyInactive);

        var businessId = company.BusinessId.Trim();

        var existing = await _userManager.FindByEmailAsync(invite.Email);
        if (existing != null)
        {
            if (await _userManager.IsInRoleAsync(existing, RoleNames.SuperAdmin))
                return Fail("InviteWrongAccount", "Sign in with a company account, not a Super Admin account.");

            var userBid = existing.BusinessId?.Trim().ToLowerInvariant();
            if (!string.Equals(userBid, businessId.ToLowerInvariant(), StringComparison.Ordinal))
                return Fail("InviteCompanyMismatch", "This account belongs to a different company.");

            if (!await _userManager.CheckPasswordAsync(existing, password))
                return Fail("InvalidPassword", "Password is incorrect.");

            if (company.MaxAdminAccounts is { } cap)
            {
                var admins = await _metrics.CountAdminsForBusinessIdAsync(businessId, cancellationToken);
                var isAlreadyAdmin = await _userManager.IsInRoleAsync(existing, RoleNames.Admin);
                if (!isAlreadyAdmin && admins >= cap)
                    return Fail("AdminLimitReached", "This company has reached its administrator limit.");
            }

            var roles = await _userManager.GetRolesAsync(existing);
            await _userManager.RemoveFromRolesAsync(existing, roles);
            await _userManager.AddToRoleAsync(existing, RoleNames.Admin);

            await _invites.MarkConsumedAsync(invite.Id, cancellationToken);
            await TryRecordFirstAdminInviteAcceptedAsync(company.Id, cancellationToken);

            var updatedRoles = await _userManager.GetRolesAsync(existing);
            var claims = updatedRoles.Select(r => new Claim(ClaimTypes.Role, r)).ToList();
            var jwt = _jwtTokenFactory.CreateToken(existing.Id, existing.Email ?? invite.Email, claims);

            return new RegisterResult
            {
                Success = true,
                Data = new LoginResponse
                {
                    UserId = existing.Id,
                    Email = existing.Email ?? invite.Email,
                    DisplayName = existing.DisplayName,
                    BusinessId = existing.BusinessId,
                    CustomerMappingId = existing.CustomerMappingId,
                    JwtToken = jwt,
                    Roles = updatedRoles.ToList()
                }
            };
        }

        if (company.MaxUserAccounts is { } userCap)
        {
            var n = await _metrics.CountActiveUsersForBusinessIdAsync(businessId, cancellationToken);
            if (n >= userCap)
                return Fail("CompanyUserLimitReached", "This company has reached its user account limit. Contact an administrator.");
        }

        if (company.MaxAdminAccounts is { } adminCap)
        {
            var admins = await _metrics.CountAdminsForBusinessIdAsync(businessId, cancellationToken);
            if (admins >= adminCap)
                return Fail("AdminLimitReached", "This company has reached its administrator limit.");
        }

        try
        {
            var (userId, email, displayName, bid, customerMappingId) = await _registration.CreateUserAsync(
                invite.Email,
                password,
                userName,
                businessId,
                null,
                RoleNames.Admin,
                cancellationToken);

            await _invites.MarkConsumedAsync(invite.Id, cancellationToken);
            await TryRecordFirstAdminInviteAcceptedAsync(company.Id, cancellationToken);

            var roleClaims = new[] { new Claim(ClaimTypes.Role, RoleNames.Admin) };
            var token = _jwtTokenFactory.CreateToken(userId, email, roleClaims);

            return new RegisterResult
            {
                Success = true,
                Data = new LoginResponse
                {
                    UserId = userId,
                    Email = email,
                    DisplayName = displayName,
                    BusinessId = bid,
                    CustomerMappingId = customerMappingId,
                    JwtToken = token,
                    Roles = new[] { RoleNames.Admin }
                }
            };
        }
        catch (InvalidOperationException ex)
        {
            var message = ex.Message;
            const string prefix = "Failed to register user: ";
            if (message.StartsWith(prefix, StringComparison.Ordinal))
                message = message[prefix.Length..];
            return Fail("RegistrationFailed", string.IsNullOrWhiteSpace(message) ? "Registration failed." : message);
        }
    }

    private async Task TryRecordFirstAdminInviteAcceptedAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var tracked = await _companies.GetByIdForUpdateAsync(companyId, cancellationToken);
        if (tracked == null || tracked.AdminInviteFirstAcceptedAtUtc != null)
            return;
        tracked.AdminInviteFirstAcceptedAtUtc = DateTimeOffset.UtcNow;
        await _companies.UpdateAsync(tracked, cancellationToken);
    }

    private static RegisterResult Fail(string code, string message) =>
        new() { Success = false, ErrorCode = code, Message = message };
}
