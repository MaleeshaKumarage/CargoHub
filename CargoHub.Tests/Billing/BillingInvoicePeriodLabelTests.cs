using CargoHub.Application.Billing.AdminInvoicing;
using Xunit;

namespace CargoHub.Tests.Billing;

public sealed class BillingInvoicePeriodLabelTests
{
    [Fact]
    public void FormatUtcInclusiveRange_SubtractsOneDayFromExclusiveEnd()
    {
        var start = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
        var endEx = new DateTime(2026, 4, 1, 0, 0, 0, DateTimeKind.Utc);
        var s = BillingInvoicePeriodLabel.FormatUtcInclusiveRange(start, endEx);
        Assert.Equal("2026-03-01 – 2026-03-31 (UTC)", s);
    }

    [Fact]
    public void FileNameStem_UsesInclusiveEndDate()
    {
        var start = new DateTime(2025, 12, 15, 0, 0, 0, DateTimeKind.Utc);
        var endEx = new DateTime(2025, 12, 16, 0, 0, 0, DateTimeKind.Utc);
        var stem = BillingInvoicePeriodLabel.FileNameStem(start, endEx);
        Assert.Equal("invoice-2025-12-15-2025-12-15", stem);
    }
}
