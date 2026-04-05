namespace CargoHub.Domain.Billing;

/// <summary>Immutable posted amount (adjust via new lines). Snapshot FKs for audit.</summary>
public class BillingLineItem
{
    public Guid Id { get; set; }

    public Guid CompanyBillingPeriodId { get; set; }

    public CompanyBillingPeriod? CompanyBillingPeriod { get; set; }

    public Guid? BookingId { get; set; }

    public BillingLineType LineType { get; set; }

    /// <summary>Distinguishes multiple lines of same <see cref="LineType"/> (e.g. unique per booking component).</summary>
    public string? Component { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "EUR";

    public Guid SubscriptionPlanId { get; set; }

    public Guid SubscriptionPlanPricingPeriodId { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    /// <summary>When true, line is omitted from Super Admin invoice payable total (ledger unchanged).</summary>
    public bool ExcludedFromInvoice { get; set; }

    public DateTime? InvoiceExclusionUpdatedAtUtc { get; set; }

    public string? InvoiceExclusionUpdatedByUserId { get; set; }
}
