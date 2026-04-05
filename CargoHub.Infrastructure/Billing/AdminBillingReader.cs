using CargoHub.Application.Billing.Admin;
using CargoHub.Domain.Billing;
using CargoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Infrastructure.Billing;

public sealed class AdminBillingReader : IAdminBillingReader
{
    private readonly ApplicationDbContext _db;
    private readonly IBillingMonthBreakdownReader _breakdownReader;

    public AdminBillingReader(ApplicationDbContext db, IBillingMonthBreakdownReader breakdownReader)
    {
        _db = db;
        _breakdownReader = breakdownReader;
    }

    public async Task<IReadOnlyList<AdminSubscriptionPlanSummaryDto>> ListSubscriptionPlansAsync(
        CancellationToken cancellationToken = default)
    {
        return await _db.SubscriptionPlans.AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new AdminSubscriptionPlanSummaryDto
            {
                Id = p.Id,
                Name = p.Name,
                Kind = p.Kind.ToString(),
                Currency = p.Currency,
                IsActive = p.IsActive
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CompanyBillingPeriodSummaryDto>> ListBillingPeriodsForCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        return await _db.CompanyBillingPeriods.AsNoTracking()
            .Where(p => p.CompanyId == companyId)
            .OrderByDescending(p => p.YearUtc)
            .ThenByDescending(p => p.MonthUtc)
            .Select(p => new CompanyBillingPeriodSummaryDto
            {
                Id = p.Id,
                CompanyId = p.CompanyId,
                YearUtc = p.YearUtc,
                MonthUtc = p.MonthUtc,
                Currency = p.Currency,
                Status = p.Status == CompanyBillingPeriodStatus.Open ? "Open" : "Closed",
                LineItemCount = p.LineItems.Count,
                PayableTotal = p.LineItems.Where(l => !l.ExcludedFromInvoice).Sum(l => (decimal?)l.Amount) ?? 0m
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<BillingPeriodDetailDto?> GetBillingPeriodDetailAsync(Guid periodId, CancellationToken cancellationToken = default)
    {
        var header = await _db.CompanyBillingPeriods.AsNoTracking()
            .Where(p => p.Id == periodId)
            .Select(p => new
            {
                p.Id,
                p.CompanyId,
                p.YearUtc,
                p.MonthUtc,
                p.Currency,
                p.Status
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (header == null)
            return null;

        var lines = await _db.BillingLineItems.AsNoTracking()
            .Where(l => l.CompanyBillingPeriodId == periodId)
            .OrderBy(l => l.CreatedAtUtc)
            .Select(l => new BillingPeriodLineItemDto
            {
                Id = l.Id,
                BookingId = l.BookingId,
                LineType = l.LineType.ToString(),
                Component = l.Component,
                Amount = l.Amount,
                Currency = l.Currency,
                ExcludedFromInvoice = l.ExcludedFromInvoice,
                CreatedAtUtc = l.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);

        var payable = lines.Where(l => !l.ExcludedFromInvoice).Sum(l => l.Amount);

        return new BillingPeriodDetailDto
        {
            Id = header.Id,
            CompanyId = header.CompanyId,
            YearUtc = header.YearUtc,
            MonthUtc = header.MonthUtc,
            Currency = header.Currency,
            Status = header.Status == CompanyBillingPeriodStatus.Open ? "Open" : "Closed",
            PayableTotal = payable,
            LineItems = lines
        };
    }

    public async Task<BillingInvoicePdfModel?> GetInvoicePdfModelAsync(
        Guid periodId,
        CancellationToken cancellationToken = default,
        DateTime? invoiceRangeStartUtc = null,
        DateTime? invoiceRangeEndExclusiveUtc = null)
    {
        var header = await _db.CompanyBillingPeriods.AsNoTracking()
            .Where(p => p.Id == periodId)
            .Select(p => new
            {
                p.Id,
                p.CompanyId,
                p.YearUtc,
                p.MonthUtc,
                p.Currency,
                p.Status
            })
            .FirstOrDefaultAsync(cancellationToken);
        if (header == null)
            return null;

        var monthStart = new DateTime(header.YearUtc, header.MonthUtc, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEndExclusive = monthStart.AddMonths(1);

        DateTime windowStart;
        DateTime windowEndExclusive;
        var useCustomWindow = invoiceRangeStartUtc.HasValue || invoiceRangeEndExclusiveUtc.HasValue;
        if (useCustomWindow)
        {
            if (invoiceRangeStartUtc is not { } wStart || invoiceRangeEndExclusiveUtc is not { } wEndEx)
                return null;
            if (wStart.Kind != DateTimeKind.Utc || wEndEx.Kind != DateTimeKind.Utc)
                return null;
            if (wStart < monthStart || wEndEx > monthEndExclusive || wStart >= wEndEx)
                return null;
            windowStart = wStart;
            windowEndExclusive = wEndEx;
        }
        else
        {
            windowStart = monthStart;
            windowEndExclusive = monthEndExclusive;
        }

        var company = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == header.CompanyId)
            .Select(c => new { c.Name, c.BusinessId })
            .FirstOrDefaultAsync(cancellationToken);

        BillingMonthBreakdownDto? breakdown;
        if (useCustomWindow)
        {
            breakdown = await _breakdownReader.GetBreakdownForDateRangeAsync(
                header.CompanyId,
                windowStart,
                windowEndExclusive,
                cancellationToken);
            if (breakdown == null)
                return null;
        }
        else
        {
            breakdown = await _breakdownReader.GetBreakdownAsync(
                header.CompanyId,
                header.YearUtc,
                header.MonthUtc,
                cancellationToken);
        }

        var allLines = await _db.BillingLineItems.AsNoTracking()
            .Where(l => l.CompanyBillingPeriodId == periodId)
            .OrderBy(l => l.CreatedAtUtc)
            .Select(l => new BillingInvoicePdfLineModel
            {
                LineType = l.LineType.ToString(),
                Component = l.Component,
                Amount = l.Amount,
                ExcludedFromInvoice = l.ExcludedFromInvoice,
                BookingId = l.BookingId
            })
            .ToListAsync(cancellationToken);

        List<BillingInvoicePdfLineModel> lines;
        if (breakdown != null && useCustomWindow)
        {
            var bookingIds = breakdown.Bookings.Select(b => b.BookingId).ToHashSet();
            lines = allLines
                .Where(l => l.BookingId == null || bookingIds.Contains(l.BookingId.Value))
                .ToList();
        }
        else
        {
            lines = allLines;
        }

        var ledger = lines.Sum(l => l.Amount);
        var payable = lines.Where(l => !l.ExcludedFromInvoice).Sum(l => l.Amount);

        IReadOnlyList<BillingInvoicePdfSegmentModel> segments;
        IReadOnlyList<BillingInvoicePdfBookingRowModel> bookingRows;
        if (breakdown != null)
        {
            segments = breakdown.Segments
                .Select(s => new BillingInvoicePdfSegmentModel
                {
                    Label = s.Label,
                    BookingCount = s.BookingCount,
                    UnitRate = s.UnitRate,
                    Subtotal = s.Subtotal
                })
                .ToList();
            bookingRows = breakdown.Bookings
                .Select(b => new BillingInvoicePdfBookingRowModel
                {
                    BookingId = b.BookingId,
                    Reference = b.ShipmentNumber ?? b.ReferenceNumber ?? b.BookingId.ToString("N")[..8],
                    Amount = b.Amount,
                    ExcludedFromInvoice = b.ExcludedFromInvoice
                })
                .ToList();
        }
        else
        {
            segments = Array.Empty<BillingInvoicePdfSegmentModel>();
            bookingRows = Array.Empty<BillingInvoicePdfBookingRowModel>();
        }

        return new BillingInvoicePdfModel
        {
            PeriodId = header.Id,
            CompanyId = header.CompanyId,
            CompanyName = company?.Name ?? "",
            BusinessId = company?.BusinessId,
            YearUtc = header.YearUtc,
            MonthUtc = header.MonthUtc,
            InvoiceRangeStartUtc = windowStart,
            InvoiceRangeEndExclusiveUtc = windowEndExclusive,
            Currency = header.Currency,
            Status = header.Status == CompanyBillingPeriodStatus.Open ? "Open" : "Closed",
            PayableTotal = payable,
            LedgerTotal = ledger,
            Lines = lines,
            Segments = segments,
            BookingRows = bookingRows
        };
    }
}
