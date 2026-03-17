using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CargoHub.Application.Auth.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace CargoHub.Infrastructure.Auth;

/// <summary>
/// Default implementation that issues JWT tokens compatible with the configuration
/// in Program.cs. This can be tuned to fully match the existing Node.js token-utils.
/// </summary>
public sealed class JwtTokenFactory : IJwtTokenFactory
{
    private readonly IConfiguration _configuration;

    public JwtTokenFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string CreateToken(string userId, string email, IEnumerable<Claim> additionalClaims)
    {
        // Must match Program.cs TokenValidationParameters (ValidIssuer/ValidAudience) or tokens will be rejected with 401.
        var issuer = _configuration["Jwt:Issuer"] ?? "PortalIssuer";
        var audience = _configuration["Jwt:Audience"] ?? "PortalAudience";
        var key = _configuration["Jwt:SigningKey"] ?? "PLEASE_CHANGE_ME_TO_A_LONG_SECURE_KEY";

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId),
            new(JwtRegisteredClaimNames.Email, email)
        };
        claims.AddRange(additionalClaims);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

