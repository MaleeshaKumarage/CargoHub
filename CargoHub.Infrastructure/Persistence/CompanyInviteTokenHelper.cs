using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace CargoHub.Infrastructure.Persistence;

public static class CompanyInviteTokenHelper
{
    public static string GenerateRawToken()
    {
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        return WebEncoders.Base64UrlEncode(bytes);
    }

    public static string HashRawToken(string rawToken)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
    }
}
