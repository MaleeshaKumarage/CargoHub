using CargoHub.Application.Billing.Admin;
using CargoHub.Domain.Billing;
using CargoHub.Domain.Bookings;
using CargoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Infrastructure.Billing;

public sealed class BillingMonthBreakdownReader : IBillingMonthBreakdownReader
{
    private readonly ApplicationDbContext _db;
    private readonly IBillingPeriodRegenerationService _regeneration;

    public BillingMonthBreakdownReader(ApplicationDbContext db, IBillingPeriodRegenerationService regeneration)
    {
        _db = db;
        _regeneration = regeneration;
    }

    public async Task<IReadOnlyList<BillableMonthSummaryDto>> GetBillableMonthsAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _db.Bookings.AsNoTracking()
            .Where(b =>
                b.CompanyId == companyId &&
                !b.IsDraft &&
                !b.IsTestBooking &&
                b.FirstBillableAtUtc != null)
            .Select(b => new { t = b.FirstBillableAtUtc!.Value })
            .ToListAsync(cancellationToken);

        var periodRows = await _db.CompanyBillingPeriods.AsNoTracking()
            .Where(p => p.CompanyId == companyId)
            .Select(p => new { p.Id, p.YearUtc, p.MonthUtc })
            .ToListAsync(cancellationToken);
        var periods = periodRows.ToDictionary(p => (p.YearUtc, p.MonthUtc), p => p.Id);

        return rows
            .GroupBy(x => new { x.t.Year, x.t.Month })
            .Select(g => new BillableMonthSummaryDto
            {
                YearUtc = g.Key.Year,
                MonthUtc = g.Key.Month,
                BillableBookingCount = g.Count(),
                BillingPeriodId = periods.TryGetValue((g.Key.Year, g.Key.Month), out var pid) ? pid : null
            })
            .OrderByDescending(x => x.YearUtc)
            .ThenByDescending(x => x.MonthUtc)
            .ToList();
    }

    public async Task<BillingMonthBreakdownDto?> GetBreakdownAsync(
        Guid companyId,
        int yearUtc,
        int monthUtc,
        CancellationToken cancellationToken = default)
    {
        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company == null)
            return null;

        var monthStart = new DateTime(yearUtc, monthUtc, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var currency = await ResolveCurrencyAsync(companyId, cancellationToken);
        var period = await EnsureBillingPeriodAsync(companyId, yearUtc, monthUtc, currency, cancellationToken);

        var allBookings = await _db.Bookings.AsNoTracking()
            .Include(b => b.Header)
            .Where(b =>
                b.CompanyId == companyId &&
                !b.IsDraft &&
                !b.IsTestBooking &&
                b.FirstBillableAtUtc != null &&
                b.FirstBillableAtUtc >= monthStart &&
                b.FirstBillableAtUtc < monthEnd)
            .OrderBy(b => b.FirstBillableAtUtc)
            .ThenBy(b => b.Id)
            .ToListAsync(cancellationToken);

        var excludedIds = await LoadExcludedBookingIdsForPeriodAsync(period.Id, cancellationToken);

        var lineCount = await _db.BillingLineItems.CountAsync(l => l.CompanyBillingPeriodId == period.Id, cancellationToken);
        if (lineCount == 0 && await HasPayableBookingsAsync(allBookings, companyId, excludedIds, cancellationToken))
            await _regeneration.RegenerateAsync(period.Id, cancellationToken);

        var lines = await _db.BillingLineItems.AsNoTracking()
            .Where(l => l.CompanyBillingPeriodId == period.Id)
            .ToListAsync(cancellationToken);

        var ledger = lines.Sum(l => l.Amount);
        var payable = lines.Where(l => !l.ExcludedFromInvoice).Sum(l => l.Amount);

        var segments = await BuildSegmentsFromBookingsAndLinesAsync(
            allBookings, excludedIds, companyId, lines, cancellationToken);

        var bookingRows = await BuildBookingRowsAsync(allBookings, excludedIds, lines, companyId, cancellationToken);

        return new BillingMonthBreakdownDto
        {
            CompanyId = companyId,
            YearUtc = yearUtc,
            MonthUtc = monthUtc,
            BillingPeriodId = period.Id,
            RangeStartUtc = monthStart,
            RangeEndExclusiveUtc = monthEnd,
            Currency = period.Currency,
            BillableBookingCount = allBookings.Count,
            PayableTotal = payable,
            LedgerTotal = ledger,
            Segments = segments,
            Bookings = bookingRows
        };
    }

    public async Task<BillingMonthBreakdownDto?> GetBreakdownForDateRangeAsync(
        Guid companyId,
        DateTime rangeStartUtc,
        DateTime rangeEndExclusiveUtc,
        CancellationToken cancellationToken = default)
    {
        if (rangeStartUtc.Kind != DateTimeKind.Utc || rangeEndExclusiveUtc.Kind != DateTimeKind.Utc)
            return null;
        if (rangeStartUtc >= rangeEndExclusiveUtc)
            return null;
        if ((rangeEndExclusiveUtc - rangeStartUtc).TotalDays > 731)
            return null;

        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId, cancellationToken);
        if (company == null)
            return null;

        var currency = await ResolveCurrencyAsync(companyId, cancellationToken);

        var rangeBookings = await _db.Bookings.AsNoTracking()
            .Include(b => b.Header)
            .Where(b =>
                b.CompanyId == companyId &&
                !b.IsDraft &&
                !b.IsTestBooking &&
                b.FirstBillableAtUtc != null &&
                b.FirstBillableAtUtc >= rangeStartUtc &&
                b.FirstBillableAtUtc < rangeEndExclusiveUtc)
            .OrderBy(b => b.FirstBillableAtUtc)
            .ThenBy(b => b.Id)
            .ToListAsync(cancellationToken);

        var distinctMonths = rangeBookings
            .Select(b => (Year: b.FirstBillableAtUtc!.Value.Year, Month: b.FirstBillableAtUtc.Value.Month))
            .Distinct()
            .ToList();

        foreach (var ym in distinctMonths)
        {
            var monthStart = new DateTime(ym.Year, ym.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);
            var period = await EnsureBillingPeriodAsync(companyId, ym.Year, ym.Month, currency, cancellationToken);
            var monthBookings = await _db.Bookings.AsNoTracking()
                .Where(b =>
                    b.CompanyId == companyId &&
                    !b.IsDraft &&
                    !b.IsTestBooking &&
                    b.FirstBillableAtUtc != null &&
                    b.FirstBillableAtUtc >= monthStart &&
                    b.FirstBillableAtUtc < monthEnd)
                .ToListAsync(cancellationToken);
            var excludedForMonth = await LoadExcludedBookingIdsForPeriodAsync(period.Id, cancellationToken);
            var lineCount = await _db.BillingLineItems.CountAsync(l => l.CompanyBillingPeriodId == period.Id, cancellationToken);
            if (lineCount == 0 && await HasPayableBookingsAsync(monthBookings, companyId, excludedForMonth, cancellationToken))
                await _regeneration.RegenerateAsync(period.Id, cancellationToken);
        }

        var bookingIds = rangeBookings.Select(b => b.Id).ToHashSet();
        var touchedPeriodIds = new List<Guid>();
        foreach (var ym in distinctMonths)
        {
            var p = await _db.CompanyBillingPeriods.AsNoTracking()
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.YearUtc == ym.Year && x.MonthUtc == ym.Month, cancellationToken);
            if (p != null)
                touchedPeriodIds.Add(p.Id);
        }

        IReadOnlyList<BillingLineItem> lines;
        if (bookingIds.Count == 0)
            lines = Array.Empty<BillingLineItem>();
        else
        {
            var periodIdSet = touchedPeriodIds.ToHashSet();
            lines = await _db.BillingLineItems.AsNoTracking()
                .Where(l =>
                    periodIdSet.Contains(l.CompanyBillingPeriodId) &&
                    ((l.BookingId != null && bookingIds.Contains(l.BookingId.Value)) ||
                     l.BookingId == null))
                .ToListAsync(cancellationToken);
        }

        var ledger = lines.Sum(l => l.Amount);
        var payable = lines.Where(l => !l.ExcludedFromInvoice).Sum(l => l.Amount);

        var excludedUnion = new HashSet<Guid>();
        foreach (var pid in touchedPeriodIds)
        {
            foreach (var id in await LoadExcludedBookingIdsForPeriodAsync(pid, cancellationToken))
                excludedUnion.Add(id);
        }

        var segments = await BuildSegmentsFromBookingsAndLinesAsync(
            rangeBookings, excludedUnion, companyId, lines, cancellationToken);
        var bookingRows = await BuildBookingRowsAsync(rangeBookings, excludedUnion, lines, companyId, cancellationToken);

        Guid? singlePeriodId = null;
        if (distinctMonths.Count == 1)
        {
            var only = distinctMonths[0];
            var p = await _db.CompanyBillingPeriods.AsNoTracking()
                .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.YearUtc == only.Year && x.MonthUtc == only.Month, cancellationToken);
            singlePeriodId = p?.Id;
        }

        return new BillingMonthBreakdownDto
        {
            CompanyId = companyId,
            YearUtc = rangeStartUtc.Year,
            MonthUtc = rangeStartUtc.Month,
            BillingPeriodId = singlePeriodId,
            RangeStartUtc = rangeStartUtc,
            RangeEndExclusiveUtc = rangeEndExclusiveUtc,
            Currency = currency,
            BillableBookingCount = rangeBookings.Count,
            PayableTotal = payable,
            LedgerTotal = ledger,
            Segments = segments,
            Bookings = bookingRows
        };
    }

    private async Task<CompanyBillingPeriod> EnsureBillingPeriodAsync(
        Guid companyId,
        int yearUtc,
        int monthUtc,
        string currency,
        CancellationToken cancellationToken)
    {
        var period = await _db.CompanyBillingPeriods
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.YearUtc == yearUtc && p.MonthUtc == monthUtc, cancellationToken);
        if (period != null)
            return period;
        period = new CompanyBillingPeriod
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            YearUtc = yearUtc,
            MonthUtc = monthUtc,
            Currency = currency,
            Status = CompanyBillingPeriodStatus.Open
        };
        _db.CompanyBillingPeriods.Add(period);
        await _db.SaveChangesAsync(cancellationToken);
        return period;
    }

    private async Task<HashSet<Guid>> LoadExcludedBookingIdsForPeriodAsync(Guid periodId, CancellationToken cancellationToken)
    {
        var list = await _db.BillingPeriodExcludedBookings.AsNoTracking()
            .Where(x => x.CompanyBillingPeriodId == periodId)
            .Select(x => x.BookingId)
            .ToListAsync(cancellationToken);
        return list.ToHashSet();
    }

    private async Task<bool> HasPayableBookingsAsync(
        IReadOnlyList<Booking> monthBookings,
        Guid companyId,
        HashSet<Guid> excludedIds,
        CancellationToken cancellationToken)
    {
        foreach (var b in monthBookings)
        {
            if (excludedIds.Contains(b.Id))
                continue;
            var instant = b.FirstBillableAtUtc ?? b.CreatedAtUtc;
            var planId = await ResolvePlanIdAtAsync(companyId, instant, cancellationToken);
            if (planId is not { } pid)
                continue;
            var plan = await _db.SubscriptionPlans.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == pid && p.IsActive, cancellationToken);
            if (plan != null && plan.Kind != SubscriptionPlanKind.Trial)
                return true;
        }

        return false;
    }

    private async Task<string> ResolveCurrencyAsync(Guid companyId, CancellationToken cancellationToken)
    {
        var planId = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == companyId)
            .Select(c => c.SubscriptionPlanId)
            .FirstOrDefaultAsync(cancellationToken);
        if (planId is not { } pid)
            return "EUR";
        var cur = await _db.SubscriptionPlans.AsNoTracking()
            .Where(p => p.Id == pid)
            .Select(p => p.Currency)
            .FirstOrDefaultAsync(cancellationToken);
        return string.IsNullOrWhiteSpace(cur) ? "EUR" : cur;
    }

    private Task<Guid?> ResolvePlanIdAtAsync(Guid companyId, DateTime instantUtc, CancellationToken cancellationToken) =>
        CompanySubscriptionPlanResolver.ResolvePlanIdAtAsync(_db, companyId, instantUtc, cancellationToken);

    private async Task<IReadOnlyList<BillingMonthSegmentDto>> BuildSegmentsFromBookingsAndLinesAsync(
        IReadOnlyList<Booking> monthBookings,
        HashSet<Guid> excludedIds,
        Guid companyId,
        IReadOnlyList<BillingLineItem> lines,
        CancellationToken cancellationToken)
    {
        var list = new List<BillingMonthSegmentDto>();
        var trialCount = 0;
        foreach (var b in monthBookings)
        {
            if (excludedIds.Contains(b.Id))
                continue;
            var instant = b.FirstBillableAtUtc ?? b.CreatedAtUtc;
            var planId = await ResolvePlanIdAtAsync(companyId, instant, cancellationToken);
            if (planId is not { } pid)
                continue;
            var plan = await _db.SubscriptionPlans.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == pid, cancellationToken);
            if (plan?.Kind == SubscriptionPlanKind.Trial)
                trialCount++;
        }

        if (trialCount > 0)
        {
            list.Add(new BillingMonthSegmentDto
            {
                Label = "Trial (no charge)",
                BookingCount = trialCount,
                UnitRate = 0m,
                Subtotal = 0m,
                PlanKind = nameof(SubscriptionPlanKind.Trial),
                SubscriptionPlanId = null
            });
        }

        var countedLines = lines.Where(l => !l.ExcludedFromInvoice).ToList();
        var byPlan = countedLines.GroupBy(l => l.SubscriptionPlanId);
        foreach (var g in byPlan)
        {
            var meta = await _db.SubscriptionPlans.AsNoTracking()
                .Where(p => p.Id == g.Key)
                .Select(p => new { p.Name, Kind = p.Kind.ToString() })
                .FirstOrDefaultAsync(cancellationToken);
            var name = meta?.Name ?? "Subscription";
            var kind = meta?.Kind ?? "";
            var distinctBookings = g.Where(x => x.BookingId != null).Select(x => x.BookingId!.Value).Distinct().Count();
            var hasNonBookingLine = g.Any(x => x.BookingId == null);
            list.Add(new BillingMonthSegmentDto
            {
                Label = $"{name} ({kind})",
                BookingCount = distinctBookings > 0 ? distinctBookings : (hasNonBookingLine ? 1 : 0),
                UnitRate = null,
                Subtotal = g.Sum(x => x.Amount),
                PlanKind = kind,
                SubscriptionPlanId = g.Key
            });
        }

        return list;
    }

    private async Task<IReadOnlyList<BillingMonthBookingRowDto>> BuildBookingRowsAsync(
        IReadOnlyList<Booking> bookings,
        HashSet<Guid> excludedIds,
        IReadOnlyList<BillingLineItem> lines,
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var byBooking = lines
            .Where(l => l.BookingId != null)
            .GroupBy(l => l.BookingId!.Value)
            .ToDictionary(g => g.Key, g => (Sum: g.Sum(x => x.Amount), Excluded: g.Any(x => x.ExcludedFromInvoice)));

        var rows = new List<BillingMonthBookingRowDto>();
        foreach (var b in bookings)
        {
            var instant = b.FirstBillableAtUtc ?? b.CreatedAtUtc;
            var planId = await ResolvePlanIdAtAsync(companyId, instant, cancellationToken);
            var planName = planId is { } pid
                ? await _db.SubscriptionPlans.AsNoTracking()
                    .Where(p => p.Id == pid)
                    .Select(p => p.Name)
                    .FirstOrDefaultAsync(cancellationToken) ?? ""
                : "";

            var tableExcluded = excludedIds.Contains(b.Id);
            byBooking.TryGetValue(b.Id, out var agg);
            var lineExcluded = agg.Excluded;
            var excl = tableExcluded || lineExcluded;
            rows.Add(new BillingMonthBookingRowDto
            {
                BookingId = b.Id,
                ShipmentNumber = b.ShipmentNumber,
                ReferenceNumber = b.Header.ReferenceNumber,
                PlanLabel = planName,
                Description = excl ? "Excluded from invoice" : "Charges this period",
                Amount = excl ? 0m : agg.Sum,
                ExcludedFromInvoice = excl
            });
        }

        return rows;
    }
}
