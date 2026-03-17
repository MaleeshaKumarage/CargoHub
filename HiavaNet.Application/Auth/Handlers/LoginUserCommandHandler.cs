using System.Security.Claims;
using HiavaNet.Application.Auth.Abstractions;
using HiavaNet.Application.Auth.Commands;
using HiavaNet.Application.Auth.Dtos;
using MediatR;

namespace HiavaNet.Application.Auth.Handlers;

/// <summary>
/// Handles portal login by validating credentials via Identity and issuing a JWT with role claims.
/// </summary>
public sealed class LoginUserCommandHandler : IRequestHandler<LoginUserCommand, LoginResult>
{
    private readonly IUserAuthenticationService _authenticationService;
    private readonly IJwtTokenFactory _jwtTokenFactory;

    public LoginUserCommandHandler(
        IUserAuthenticationService authenticationService,
        IJwtTokenFactory jwtTokenFactory)
    {
        _authenticationService = authenticationService;
        _jwtTokenFactory = jwtTokenFactory;
    }

    public async Task<LoginResult> Handle(LoginUserCommand request, CancellationToken cancellationToken)
    {
        var body = request.Request;

        var authResult = await _authenticationService.ValidateCredentialsAsync(
            body.Account,
            body.Password,
            cancellationToken);

        if (!authResult.Success)
        {
            return new LoginResult
            {
                Success = false,
                ErrorCode = "InvalidCredentials",
                Message = "Invalid email/username or password."
            };
        }

        var roleClaims = authResult.Roles.Select(r => new Claim(ClaimTypes.Role, r)).ToArray();
        var token = _jwtTokenFactory.CreateToken(authResult.UserId, authResult.Email, roleClaims);

        return new LoginResult
        {
            Success = true,
            Data = new LoginResponse
            {
                UserId = authResult.UserId,
                Email = authResult.Email,
                DisplayName = authResult.DisplayName,
                BusinessId = authResult.BusinessId,
                CustomerMappingId = authResult.CustomerMappingId,
                JwtToken = token,
                Roles = authResult.Roles.ToList()
            }
        };
    }
}
