namespace CargoHub.Application.Bookings;

/// <summary>Builds stable keys for matching import files to saved column maps.</summary>
public static class ImportMappingSignature
{
    /// <summary>Normalized file basename for matching (trimmed, lowercase).</summary>
    public static string NormalizeFileNameKey(string? fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
            return string.Empty;
        var name = Path.GetFileName(fileName.Trim());
        return name.ToLowerInvariant();
    }

    /// <summary>Signature of file header row (trimmed labels, file order, unit-separated).</summary>
    public static string BuildHeaderSignature(IReadOnlyList<string> fileHeaders)
    {
        if (fileHeaders == null || fileHeaders.Count == 0)
            return string.Empty;
        return string.Join('\u001F', fileHeaders.Select(h => (h ?? string.Empty).Trim()));
    }
}
