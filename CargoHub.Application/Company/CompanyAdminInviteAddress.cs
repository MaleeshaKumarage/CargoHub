using System.Globalization;
using System.Text;

namespace CargoHub.Application.Company;

/// <summary>
/// Builds fallback admin invite email <c>{local}@domain</c> from a government business id.
/// </summary>
public static class CompanyAdminInviteAddress
{
    public const int MaxLocalPartLength = 64;

    /// <summary>
    /// Produces a safe SMTP local-part (mostly alphanumeric and hyphens).
    /// </summary>
    public static string SanitizeLocalPart(string businessId)
    {
        if (string.IsNullOrWhiteSpace(businessId))
            return "company";

        var sb = new StringBuilder(businessId.Trim().Length);
        foreach (var c in businessId.Trim().ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(c))
                sb.Append(c);
            else if (c is '.' or '_' or '+' or '%' or '-')
                sb.Append(c);
            else
                sb.Append('-');
        }

        var s = sb.ToString().Trim('-');
        while (s.Contains("--", StringComparison.Ordinal))
            s = s.Replace("--", "-", StringComparison.Ordinal);

        if (string.IsNullOrEmpty(s))
            s = "company";

        if (s.Length > MaxLocalPartLength)
            s = s[..MaxLocalPartLength].TrimEnd('-');

        return s;
    }

    public static string BuildFallbackEmail(string businessId, string domain)
    {
        var d = string.IsNullOrWhiteSpace(domain) ? "example.com" : domain.Trim();
        return $"{SanitizeLocalPart(businessId)}@{d}";
    }
}
