namespace CargoHub.Domain.Billing;

/// <summary>One accrual bucket per company per UTC calendar month.</summary>
public class CompanyBillingPeriod
{
    public Guid Id { get; set; }

    public Guid CompanyId { get; set; }

    public int YearUtc { get; set; }

    /// <summary>1–12.</summary>
    public int MonthUtc { get; set; }

    public string Currency { get; set; } = "EUR";

    public CompanyBillingPeriodStatus Status { get; set; } = CompanyBillingPeriodStatus.Open;

    public ICollection<BillingLineItem> LineItems { get; set; } = new List<BillingLineItem>();

    public ICollection<SubscriptionInvoiceSend> InvoiceSends { get; set; } = new List<SubscriptionInvoiceSend>();

    public ICollection<BillingPeriodExcludedBooking> ExcludedBookings { get; set; } = new List<BillingPeriodExcludedBooking>();
}
