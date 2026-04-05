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

    /// <summary>Inclusive UTC start of the invoice window (matches SA date-range picker).</summary>
    public DateTime InvoiceRangeStartUtc { get; init; }

    /// <summary>Exclusive UTC end of the invoice window.</summary>
    public DateTime InvoiceRangeEndExclusiveUtc { get; init; }

    public string Currency { get; init; } = "EUR";
    public string Status { get; init; } = "";
    public decimal PayableTotal { get; init; }
    public decimal LedgerTotal { get; init; }
    public IReadOnlyList<BillingInvoicePdfLineModel> Lines { get; init; } = Array.Empty<BillingInvoicePdfLineModel>();

    public IReadOnlyList<BillingInvoicePdfSegmentModel> Segments { get; init; } = Array.Empty<BillingInvoicePdfSegmentModel>();

    public IReadOnlyList<BillingInvoicePdfBookingRowModel> BookingRows { get; init; } = Array.Empty<BillingInvoicePdfBookingRowModel>();
}

public sealed class BillingInvoicePdfLineModel
{
    public string LineType { get; init; } = "";
    public string? Component { get; init; }
    public decimal Amount { get; init; }
    public bool ExcludedFromInvoice { get; init; }
    public Guid? BookingId { get; init; }
}

/// <summary>UTC month that has at least one billable booking for a company.</summary>
public sealed class BillableMonthSummaryDto
{
    public int YearUtc { get; init; }

    public int MonthUtc { get; init; }

    public int BillableBookingCount { get; init; }

    public Guid? BillingPeriodId { get; init; }
}

public sealed class BillingMonthSegmentDto
{
    public string Label { get; init; } = "";

    public int BookingCount { get; init; }

    public decimal? UnitRate { get; init; }

    public decimal Subtotal { get; init; }

    public string PlanKind { get; init; } = "";

    public Guid? SubscriptionPlanId { get; init; }
}

public sealed class BillingMonthBookingRowDto
{
    public Guid BookingId { get; init; }

    public string? ShipmentNumber { get; init; }

    public string? ReferenceNumber { get; init; }

    public string PlanLabel { get; init; } = "";

    public string Description { get; init; } = "";

    public decimal Amount { get; init; }

    public bool ExcludedFromInvoice { get; init; }
}

/// <summary>Super Admin billing month view: segments, per-booking rows, totals.</summary>
public sealed class BillingMonthBreakdownDto
{
    public Guid CompanyId { get; init; }

    /// <summary>Label month (UTC) — range start when using date-range breakdown.</summary>
    public int YearUtc { get; init; }

    /// <summary>Label month (UTC) — range start when using date-range breakdown.</summary>
    public int MonthUtc { get; init; }

    /// <summary>Set when the result maps to a single calendar month (invoice PDF/email/toggles apply).</summary>
    public Guid? BillingPeriodId { get; init; }

    /// <summary>Inclusive UTC start of the breakdown window.</summary>
    public DateTime RangeStartUtc { get; init; }

    /// <summary>Exclusive UTC end of the breakdown window.</summary>
    public DateTime RangeEndExclusiveUtc { get; init; }

    public string Currency { get; init; } = "EUR";

    public int BillableBookingCount { get; init; }

    public decimal PayableTotal { get; init; }

    public decimal LedgerTotal { get; init; }

    public IReadOnlyList<BillingMonthSegmentDto> Segments { get; init; } = Array.Empty<BillingMonthSegmentDto>();

    public IReadOnlyList<BillingMonthBookingRowDto> Bookings { get; init; } = Array.Empty<BillingMonthBookingRowDto>();
}

public sealed class BillingInvoicePdfSegmentModel
{
    public string Label { get; init; } = "";

    public int BookingCount { get; init; }

    public decimal? UnitRate { get; init; }

    public decimal Subtotal { get; init; }
}

public sealed class BillingInvoicePdfBookingRowModel
{
    public Guid BookingId { get; init; }

    public string? Reference { get; init; }

    public decimal Amount { get; init; }

    public bool ExcludedFromInvoice { get; init; }
}
