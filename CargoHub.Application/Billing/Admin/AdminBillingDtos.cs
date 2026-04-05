namespace CargoHub.Application.Billing.Admin;

/// <summary>Super Admin dropdown row for subscription templates.</summary>
public sealed class AdminSubscriptionPlanSummaryDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = "";

    public string Kind { get; init; } = "";

    public string Currency { get; init; } = "EUR";

    public bool IsActive { get; init; }
}

/// <summary>One company's UTC month bucket with payable total (excludes invoice-excluded lines).</summary>
public sealed class CompanyBillingPeriodSummaryDto
{
    public Guid Id { get; init; }

    public Guid CompanyId { get; init; }

    public int YearUtc { get; init; }

    public int MonthUtc { get; init; }

    public string Currency { get; init; } = "EUR";

    public string Status { get; init; } = "";

    public int LineItemCount { get; init; }

    public decimal PayableTotal { get; init; }
}

public sealed class BillingPeriodLineItemDto
{
    public Guid Id { get; init; }

    public Guid? BookingId { get; init; }

    public string LineType { get; init; } = "";

    public string? Component { get; init; }

    public decimal Amount { get; init; }

    public string Currency { get; init; } = "EUR";

    public bool ExcludedFromInvoice { get; init; }

    public DateTime CreatedAtUtc { get; init; }
}

/// <summary>Period header plus all posted lines (Super Admin invoice view).</summary>
public sealed class BillingPeriodDetailDto
{
    public Guid Id { get; init; }

    public Guid CompanyId { get; init; }

    public int YearUtc { get; init; }

    public int MonthUtc { get; init; }

    public string Currency { get; init; } = "EUR";

    public string Status { get; init; } = "";

    public decimal PayableTotal { get; init; }

    public IReadOnlyList<BillingPeriodLineItemDto> LineItems { get; init; } = Array.Empty<BillingPeriodLineItemDto>();
}

/// <summary>Data for Super Admin billing period PDF / email attachment.</summary>
public sealed class BillingInvoicePdfModel
{
    public Guid PeriodId { get; init; }
    public Guid CompanyId { get; init; }
    public string CompanyName { get; init; } = "";
    public string? BusinessId { get; init; }
    public int YearUtc { get; init; }
    public int MonthUtc { get; init; }
    public string Currency { get; init; } = "EUR";
    public string Status { get; init; } = "";
    public decimal PayableTotal { get; init; }
    public decimal LedgerTotal { get; init; }
    public IReadOnlyList<BillingInvoicePdfLineModel> Lines { get; init; } = Array.Empty<BillingInvoicePdfLineModel>();
}

public sealed class BillingInvoicePdfLineModel
{
    public string LineType { get; init; } = "";
    public string? Component { get; init; }
    public decimal Amount { get; init; }
    public bool ExcludedFromInvoice { get; init; }
    public Guid? BookingId { get; init; }
}
