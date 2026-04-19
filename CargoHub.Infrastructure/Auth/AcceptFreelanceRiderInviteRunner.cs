using System.Security.Claims;
using CargoHub.Application.Auth;
using CargoHub.Application.Auth.Abstractions;
using CargoHub.Application.Auth.Dtos;
using CargoHub.Application.FreelanceRiders;
using CargoHub.Domain.FreelanceRiders;
using CargoHub.Infrastructure.Identity;
using CargoHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Infrastructure.Auth;

public sealed class AcceptFreelanceRiderInviteRunner : IAcceptFreelanceRiderInviteRunner
{
    private readonly IFreelanceRiderInviteRepository _invites;
    private readonly IUserRegistrationService _registration;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IJwtTokenFactory _jwtTokenFactory;
    private readonly ApplicationDbContext _db;

    public AcceptFreelanceRiderInviteRunner(
        IFreelanceRiderInviteRepository invites,
        IUserRegistrationService registration,
        UserManager<ApplicationUser> userManager,
        IJwtTokenFactory jwtTokenFactory,
        ApplicationDbContext db)
    {
        _invites = invites;
        _registration = registration;
        _userManager = userManager;
        _jwtTokenFactory = jwtTokenFactory;
        _db = db;
    }

    public async Task<RegisterResult> RunAsync(string rawToken, string password, string userName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return Fail("InviteTokenRequired", "Invitation token is required.");
        if (string.IsNullOrWhiteSpace(password))
            return Fail("PasswordRequired", "Password is required.");

        var hash = CompanyInviteTokenHelper.HashRawToken(rawToken.Trim());
        var invite = await _invites.GetActiveByTokenHashAsync(hash, cancellationToken);
        if (invite?.FreelanceRider == null)
            return Fail("InviteInvalid", "This invitation link is invalid or has expired.");

        var rider = await _db.FreelanceRiders.FirstOrDefaultAsync(r => r.Id == invite.FreelanceRiderId, cancellationToken);
        if (rider == null)
            return Fail("InviteInvalid", "Rider record not found.");

        if (rider.Status != FreelanceRiderStatus.PendingInvite && rider.Status != FreelanceRiderStatus.Active)
            return Fail("RiderInactive", "This rider account cannot accept invites.");

        var businessId = rider.BusinessId?.Trim();
        if (string.IsNullOrEmpty(businessId))
            return Fail("RiderMisconfigured", "Rider has no business id; contact support.");

        var existing = await _userManager.FindByEmailAsync(invite.Email);
        if (existing != null)
        {
            if (await _userManager.IsInRoleAsync(existing, RoleNames.SuperAdmin))
                return Fail("InviteWrongAccount", "Sign in with a rider account, not a Super Admin account.");

            if (!await _userManager.CheckPasswordAsync(existing, password))
                return Fail("InvalidPassword", "Password is incorrect.");

            var roles = await _userManager.GetRolesAsync(existing);
            await _userManager.RemoveFromRolesAsync(existing, roles);
            await _userManager.AddToRoleAsync(existing, RoleNames.Rider);

            rider.ApplicationUserId = existing.Id;
            rider.Status = FreelanceRiderStatus.Active;
            rider.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await _invites.MarkConsumedAsync(invite.Id, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

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
                    BusinessId = businessId,
                    CustomerMappingId = existing.CustomerMappingId,
                    JwtToken = jwt,
                    Roles = updatedRoles.ToList()
                }
            };
        }

        try
        {
            var (userId, email, displayName, _, customerMappingId) = await _registration.CreateUserAsync(
                invite.Email,
                password,
                userName,
                businessId,
                null,
                RoleNames.Rider,
                cancellationToken);

            rider.ApplicationUserId = userId;
            rider.Status = FreelanceRiderStatus.Active;
            rider.UpdatedAtUtc = DateTimeOffset.UtcNow;
            await _invites.MarkConsumedAsync(invite.Id, cancellationToken);
            await _db.SaveChangesAsync(cancellationToken);

            var roleClaims = new[] { new Claim(ClaimTypes.Role, RoleNames.Rider) };
            var token = _jwtTokenFactory.CreateToken(userId, email, roleClaims);

            return new RegisterResult
            {
                Success = true,
                Data = new LoginResponse
                {
                    UserId = userId,
                    Email = email,
                    DisplayName = displayName,
                    BusinessId = businessId,
                    CustomerMappingId = customerMappingId,
                    JwtToken = token,
                    Roles = new List<string> { RoleNames.Rider }
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

    private static RegisterResult Fail(string code, string message) =>
        new() { Success = false, ErrorCode = code, Message = message };
}
