namespace CargoHub.Domain.Billing;

/// <summary>Audit log when Super Admin emails an invoice summary to a company Admin.</summary>
public class SubscriptionInvoiceSend
{
    public Guid Id { get; set; }

    public Guid CompanyBillingPeriodId { get; set; }

    public CompanyBillingPeriod? CompanyBillingPeriod { get; set; }

    public DateTime SentAtUtc { get; set; }

    public string SentBySuperAdminUserId { get; set; } = string.Empty;

    public string RecipientAdminUserId { get; set; } = string.Empty;

    public string RecipientEmailSnapshot { get; set; } = string.Empty;

    public decimal LedgerTotalSnapshot { get; set; }

    public decimal InvoiceTotalSnapshot { get; set; }
}
