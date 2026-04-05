namespace CargoHub.Application.Billing.AdminInvoicing;

/// <summary>Consistent invoice PDF/email wording for a UTC date window (end is exclusive in storage).</summary>
public static class BillingInvoicePeriodLabel
{
    public static string FormatUtcInclusiveRange(DateTime rangeStartUtc, DateTime rangeEndExclusiveUtc)
    {
        var endInclusive = rangeEndExclusiveUtc.AddDays(-1);
        return $"{rangeStartUtc:yyyy-MM-dd} – {endInclusive:yyyy-MM-dd} (UTC)";
    }

    public static string FileNameStem(DateTime rangeStartUtc, DateTime rangeEndExclusiveUtc)
    {
        var endInclusive = rangeEndExclusiveUtc.AddDays(-1);
        return $"invoice-{rangeStartUtc:yyyy-MM-dd}-{endInclusive:yyyy-MM-dd}";
    }
}
