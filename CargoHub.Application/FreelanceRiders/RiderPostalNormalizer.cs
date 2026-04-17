namespace CargoHub.Application.FreelanceRiders;

/// <summary>Normalizes postal codes for matching (FI-focused).</summary>
public static class RiderPostalNormalizer
{
    public static string Normalize(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return string.Empty;
        var s = raw.Trim().ToUpperInvariant().Replace(" ", "");
        // FI-00100 -> 00100
        if (s.StartsWith("FI-", StringComparison.Ordinal))
            s = s[3..];
        if (s.Length > 3 && s.StartsWith("FI", StringComparison.Ordinal) && char.IsDigit(s[2]))
            s = s[2..];
        return s;
    }
}
