using CargoHub.Application.Auth.Abstractions;
using CargoHub.Application.Auth.Commands;
using CargoHub.Application.Auth.Dtos;
using CargoHub.Application.Company;
using MediatR;

namespace CargoHub.Application.Auth.Handlers;

/// <summary>
/// Handles new user registration. Company (government ID) must already exist and be created by admin.
/// </summary>
public sealed class RegisterUserCommandHandler : IRequestHandler<RegisterUserCommand, RegisterResult>
{
    private readonly ICompanyRepository _companyRepository;
    private readonly IUserRegistrationService _userRegistrationService;
    private readonly IJwtTokenFactory _jwtTokenFactory;

    public RegisterUserCommandHandler(
        ICompanyRepository companyRepository,
        IUserRegistrationService userRegistrationService,
        IJwtTokenFactory jwtTokenFactory)
    {
        _companyRepository = companyRepository;
        _userRegistrationService = userRegistrationService;
        _jwtTokenFactory = jwtTokenFactory;
    }

    public async Task<RegisterResult> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var body = request.Request;
        var companyId = string.IsNullOrWhiteSpace(body.BusinessId) ? null : body.BusinessId.Trim();

        if (string.IsNullOrEmpty(companyId))
            return new RegisterResult { Success = false, ErrorCode = "CompanyIdRequired", Message = "Company ID (government business ID) is required." };

        var company = await _companyRepository.GetByBusinessIdAsync(companyId, cancellationToken);
        if (company == null)
            return new RegisterResult { Success = false, ErrorCode = "CompanyNotFound", Message = "No company found with this Company ID. The company must be created by an administrator first." };

        try
        {
            var (userId, email, displayName, businessId, customerMappingId) =
                await _userRegistrationService.CreateUserAsync(
                    body.Email,
                    body.Password,
                    body.UserName,
                    company.BusinessId,
                    body.GsOne,
                    cancellationToken);

            var roleClaims = new[] { new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role, RoleNames.User) };
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
                    Roles = new[] { RoleNames.User }
                }
            };
        }
        catch (InvalidOperationException ex)
        {
            // Identity failures (duplicate email/username, password rules, etc.) must be JSON BadRequest,
            // not an unhandled 500/HTML response — the portal would mislabel that as a network/CORS error.
            var message = ex.Message;
            const string prefix = "Failed to register user: ";
            if (message.StartsWith(prefix, StringComparison.Ordinal))
                message = message[prefix.Length..];

            return new RegisterResult
            {
                Success = false,
                ErrorCode = "RegistrationFailed",
                Message = string.IsNullOrWhiteSpace(message) ? "Registration failed." : message
            };
        }
    }
}
