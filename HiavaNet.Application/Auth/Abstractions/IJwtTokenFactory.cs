using System.Security.Claims;

namespace HiavaNet.Application.Auth.Abstractions;

/// <summary>
/// Abstraction for creating JWT tokens used by the portal and API.
/// Implementation lives in the Infrastructure project.
/// </summary>
public interface IJwtTokenFactory
{
    /// <summary>
    /// Creates a signed JWT token for the specified user and claims.
    /// </summary>
    string CreateToken(string userId, string email, IEnumerable<Claim> additionalClaims);
}

