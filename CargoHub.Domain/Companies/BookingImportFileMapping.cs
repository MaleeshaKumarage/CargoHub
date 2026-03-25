namespace CargoHub.Domain.Companies;

/// <summary>
/// Saved CSV/Excel column mapping for a company, keyed by normalized file name and header layout signature.
/// </summary>
public sealed class BookingImportFileMapping
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    /// <summary>Lowercase trimmed file basename (e.g. report.csv).</summary>
    public string FileNameKey { get; set; } = string.Empty;

    /// <summary>Ordered trimmed file headers joined with U+001F (unit separator).</summary>
    public string HeaderSignature { get; set; } = string.Empty;

    /// <summary>JSON object: canonical booking field → disambiguated file column (or null).</summary>
    public string ColumnMapJson { get; set; } = "{}";

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }
}
