using CargoHub.Application.Billing.Admin;
using CargoHub.Domain.Billing;
using CargoHub.Domain.Bookings;
using CargoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Infrastructure.Billing;

public sealed class BillingPeriodRegenerationService : IBillingPeriodRegenerationService
{
    private const string MonthlyBaseComponent = "base";

    private readonly ApplicationDbContext _db;

    public BillingPeriodRegenerationService(ApplicationDbContext db) => _db = db;

    public async Task RegenerateAsync(Guid companyBillingPeriodId, CancellationToken cancellationToken = default)
    {
        var header = await _db.CompanyBillingPeriods
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == companyBillingPeriodId, cancellationToken);
        if (header == null)
            return;

        var companyId = header.CompanyId;
        var yearUtc = header.YearUtc;
        var monthUtc = header.MonthUtc;

        await SyncExclusionsFromLinesAsync(companyBillingPeriodId, cancellationToken);

        await _db.BillingLineItems
            .Where(l => l.CompanyBillingPeriodId == companyBillingPeriodId)
            .ExecuteDeleteAsync(cancellationToken);

        var excludedList = await _db.BillingPeriodExcludedBookings.AsNoTracking()
            .Where(x => x.CompanyBillingPeriodId == companyBillingPeriodId)
            .Select(x => x.BookingId)
            .ToListAsync(cancellationToken);
        var excluded = excludedList.ToHashSet();

        var monthStart = new DateTime(yearUtc, monthUtc, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var bookingsInMonth = await _db.Bookings
            .Where(b =>
                b.CompanyId == companyId &&
                !b.IsDraft &&
                !b.IsTestBooking &&
                b.FirstBillableAtUtc != null &&
                b.FirstBillableAtUtc >= monthStart &&
                b.FirstBillableAtUtc < monthEnd)
            .ToListAsync(cancellationToken);

        var included = bookingsInMonth.Where(b => !excluded.Contains(b.Id)).ToList();

        var byPlan = new Dictionary<Guid, List<Booking>>();
        foreach (var b in included)
        {
            var instant = b.FirstBillableAtUtc ?? b.CreatedAtUtc;
            var planId = await ResolvePlanIdAtAsync(companyId, instant, cancellationToken);
            if (planId is not { } pid)
                continue;
            if (!byPlan.TryGetValue(pid, out var list))
            {
                list = new List<Booking>();
                byPlan[pid] = list;
            }

            list.Add(b);
        }

        foreach (var (planId, groupBookings) in byPlan)
        {
            var plan = await _db.SubscriptionPlans
                .Include(p => p.PricingPeriods)
                .ThenInclude(pp => pp.Tiers)
                .FirstOrDefaultAsync(p => p.Id == planId && p.IsActive, cancellationToken);
            if (plan == null || plan.Kind == SubscriptionPlanKind.Trial)
                continue;

            groupBookings.Sort((a, b) => CompareBookings(a, b, plan));

            switch (plan.Kind)
            {
                case SubscriptionPlanKind.PayPerBooking:
                    foreach (var b in groupBookings)
                        await PostPayPerBookingAsync(b, plan, header, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);
                    break;
                case SubscriptionPlanKind.MonthlyBundle:
                    for (var i = 0; i < groupBookings.Count; i++)
                    {
                        await PostMonthlyBundleForBookingAsync(groupBookings[i], plan, companyId, header, i + 1, cancellationToken);
                        await _db.SaveChangesAsync(cancellationToken);
                    }

                    break;
                case SubscriptionPlanKind.TieredPayPerBooking:
                    foreach (var b in groupBookings)
                        await PostTieredPayPerBookingAsync(b, plan, companyId, header, groupBookings, cancellationToken);
                    break;
                case SubscriptionPlanKind.TieredMonthlyByUsage:
                    await PostTieredMonthlyForGroupAsync(plan, header, groupBookings.Count, cancellationToken);
                    await _db.SaveChangesAsync(cancellationToken);
                    break;
            }
        }
    }

    private async Task SyncExclusionsFromLinesAsync(Guid periodId, CancellationToken cancellationToken)
    {
        var fromLines = await _db.BillingLineItems.AsNoTracking()
            .Where(l => l.CompanyBillingPeriodId == periodId && l.BookingId != null && l.ExcludedFromInvoice)
            .Select(l => l.BookingId!.Value)
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var bid in fromLines)
        {
            var exists = await _db.BillingPeriodExcludedBookings.AnyAsync(
                x => x.CompanyBillingPeriodId == periodId && x.BookingId == bid,
                cancellationToken);
            if (!exists)
            {
                _db.BillingPeriodExcludedBookings.Add(new BillingPeriodExcludedBooking
                {
                    CompanyBillingPeriodId = periodId,
                    BookingId = bid
                });
            }
        }

        if (fromLines.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);
    }

    private static int CompareBookings(Booking a, Booking b, SubscriptionPlan plan)
    {
        var ta = SortAnchor(a, plan);
        var tb = SortAnchor(b, plan);
        var c = ta.CompareTo(tb);
        return c != 0 ? c : string.CompareOrdinal(a.Id.ToString("N"), b.Id.ToString("N"));
    }

    private static DateTime SortAnchor(Booking b, SubscriptionPlan plan) =>
        plan.ChargeTimeAnchor == ChargeTimeAnchor.CreatedAtUtc
            ? b.CreatedAtUtc
            : b.FirstBillableAtUtc ?? b.CreatedAtUtc;

    private Task<Guid?> ResolvePlanIdAtAsync(Guid companyId, DateTime instantUtc, CancellationToken cancellationToken) =>
        CompanySubscriptionPlanResolver.ResolvePlanIdAtAsync(_db, companyId, instantUtc, cancellationToken);

    private static DateTime GetAnchorUtc(Booking booking, SubscriptionPlan plan) =>
        plan.ChargeTimeAnchor == ChargeTimeAnchor.CreatedAtUtc
            ? booking.CreatedAtUtc
            : booking.FirstBillableAtUtc ?? DateTime.UtcNow;

    private static SubscriptionPlanPricingPeriod? ResolvePricingPeriod(SubscriptionPlan plan, DateTime instantUtc) =>
        plan.PricingPeriods
            .Where(pp => pp.EffectiveFromUtc <= instantUtc)
            .OrderByDescending(pp => pp.EffectiveFromUtc)
            .FirstOrDefault();

    private async Task PostPayPerBookingAsync(
        Booking booking,
        SubscriptionPlan plan,
        CompanyBillingPeriod billingPeriod,
        CancellationToken cancellationToken)
    {
        var anchor = GetAnchorUtc(booking, plan);
        var period = ResolvePricingPeriod(plan, anchor);
        if (period?.ChargePerBooking is not { } amount || amount == 0)
            return;

        _db.BillingLineItems.Add(new BillingLineItem
        {
            Id = Guid.NewGuid(),
            CompanyBillingPeriodId = billingPeriod.Id,
            BookingId = booking.Id,
            LineType = BillingLineType.PerBooking,
            Component = null,
            Amount = amount,
            Currency = plan.Currency,
            SubscriptionPlanId = plan.Id,
            SubscriptionPlanPricingPeriodId = period.Id,
            CreatedAtUtc = DateTime.UtcNow
        });
        await Task.CompletedTask;
    }

    private async Task PostMonthlyBundleForBookingAsync(
        Booking booking,
        SubscriptionPlan plan,
        Guid companyId,
        CompanyBillingPeriod billingPeriod,
        int rankInGroup,
        CancellationToken cancellationToken)
    {
        var monthStart = new DateTime(billingPeriod.YearUtc, billingPeriod.MonthUtc, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodAtMonth = ResolvePricingPeriod(plan, monthStart);
        if (periodAtMonth == null)
            return;

        if (periodAtMonth.MonthlyFee is { } baseFee and not 0)
        {
            var hasBase = await _db.BillingLineItems.AnyAsync(li =>
                li.CompanyBillingPeriodId == billingPeriod.Id &&
                li.BookingId == null &&
                li.LineType == BillingLineType.MonthlyBase &&
                li.Component == MonthlyBaseComponent, cancellationToken);
            if (!hasBase)
            {
                _db.BillingLineItems.Add(new BillingLineItem
                {
                    Id = Guid.NewGuid(),
                    CompanyBillingPeriodId = billingPeriod.Id,
                    BookingId = null,
                    LineType = BillingLineType.MonthlyBase,
                    Component = MonthlyBaseComponent,
                    Amount = baseFee,
                    Currency = plan.Currency,
                    SubscriptionPlanId = plan.Id,
                    SubscriptionPlanPricingPeriodId = periodAtMonth.Id,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        var included = periodAtMonth.IncludedBookingsPerMonth ?? 0;
        if (rankInGroup <= included)
            return;

        if (periodAtMonth.OverageChargePerBooking is not decimal over || over == 0m)
            return;

        var comp = booking.Id.ToString("N");
        _db.BillingLineItems.Add(new BillingLineItem
        {
            Id = Guid.NewGuid(),
            CompanyBillingPeriodId = billingPeriod.Id,
            BookingId = booking.Id,
            LineType = BillingLineType.Overage,
            Component = comp,
            Amount = over,
            Currency = plan.Currency,
            SubscriptionPlanId = plan.Id,
            SubscriptionPlanPricingPeriodId = periodAtMonth.Id,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    private async Task PostTieredPayPerBookingAsync(
        Booking booking,
        SubscriptionPlan plan,
        Guid companyId,
        CompanyBillingPeriod billingPeriod,
        IReadOnlyList<Booking> orderedGroup,
        CancellationToken cancellationToken)
    {
        var anchor = GetAnchorUtc(booking, plan);
        var period = ResolvePricingPeriod(plan, anchor);
        if (period == null)
            return;

        var rank = 0;
        for (var i = 0; i < orderedGroup.Count; i++)
        {
            if (orderedGroup[i].Id == booking.Id)
            {
                rank = i + 1;
                break;
            }
        }

        if (rank == 0)
            return;
        var rate = TieredPaygoRate(period.Tiers, rank);
        if (rate is not { } amount || amount == 0)
            return;

        var comp = booking.Id.ToString("N");
        _db.BillingLineItems.Add(new BillingLineItem
        {
            Id = Guid.NewGuid(),
            CompanyBillingPeriodId = billingPeriod.Id,
            BookingId = booking.Id,
            LineType = BillingLineType.TieredMarginal,
            Component = comp,
            Amount = amount,
            Currency = plan.Currency,
            SubscriptionPlanId = plan.Id,
            SubscriptionPlanPricingPeriodId = period.Id,
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        for (var i = 0; i < orderedGroup.Count; i++)
        {
            var b = orderedGroup[i];
            var r = i + 1;
            var a = GetAnchorUtc(b, plan);
            var pp = ResolvePricingPeriod(plan, a);
            if (pp == null)
                continue;
            var expected = TieredPaygoRate(pp.Tiers, r) ?? 0;
            var posted = await _db.BillingLineItems
                .Where(li => li.CompanyBillingPeriodId == billingPeriod.Id &&
                             li.BookingId == b.Id &&
                             (li.LineType == BillingLineType.TieredMarginal || li.LineType == BillingLineType.Adjustment))
                .SumAsync(li => li.Amount, cancellationToken);
            var delta = expected - posted;
            if (Math.Abs(delta) < 0.0001m)
                continue;

            _db.BillingLineItems.Add(new BillingLineItem
            {
                Id = Guid.NewGuid(),
                CompanyBillingPeriodId = billingPeriod.Id,
                BookingId = b.Id,
                LineType = BillingLineType.Adjustment,
                Component = $"tiered-paygo-{b.Id:N}",
                Amount = delta,
                Currency = plan.Currency,
                SubscriptionPlanId = plan.Id,
                SubscriptionPlanPricingPeriodId = pp.Id,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task PostTieredMonthlyForGroupAsync(
        SubscriptionPlan plan,
        CompanyBillingPeriod billingPeriod,
        int bookingCountInGroup,
        CancellationToken cancellationToken)
    {
        var monthStart = new DateTime(billingPeriod.YearUtc, billingPeriod.MonthUtc, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodAtMonth = ResolvePricingPeriod(plan, monthStart);
        if (periodAtMonth == null || periodAtMonth.Tiers.Count == 0)
            return;

        var target = TieredMonthlyFee(periodAtMonth.Tiers, bookingCountInGroup) ?? 0;
        var posted = await _db.BillingLineItems
            .Where(li => li.CompanyBillingPeriodId == billingPeriod.Id &&
                         li.BookingId == null &&
                         (li.LineType == BillingLineType.TieredMonthlyFee || li.LineType == BillingLineType.PeriodAdjustment))
            .SumAsync(li => li.Amount, cancellationToken);

        var delta = target - posted;
        if (Math.Abs(delta) < 0.0001m)
            return;

        _db.BillingLineItems.Add(new BillingLineItem
        {
            Id = Guid.NewGuid(),
            CompanyBillingPeriodId = billingPeriod.Id,
            BookingId = null,
            LineType = BillingLineType.PeriodAdjustment,
            Component = "tiered-monthly",
            Amount = delta,
            Currency = plan.Currency,
            SubscriptionPlanId = plan.Id,
            SubscriptionPlanPricingPeriodId = periodAtMonth.Id,
            CreatedAtUtc = DateTime.UtcNow
        });
    }

    private static decimal? TieredPaygoRate(IEnumerable<SubscriptionPlanPricingTier> tiers, int rank1Based)
    {
        foreach (var t in tiers.OrderBy(x => x.Ordinal))
        {
            if (t.InclusiveMaxBookingsInPeriod == null || rank1Based <= t.InclusiveMaxBookingsInPeriod.Value)
                return t.ChargePerBooking;
        }

        return tiers.OrderByDescending(x => x.Ordinal).FirstOrDefault()?.ChargePerBooking;
    }

    private static decimal? TieredMonthlyFee(IEnumerable<SubscriptionPlanPricingTier> tiers, int bookingCountInMonth)
    {
        foreach (var t in tiers.OrderBy(x => x.Ordinal))
        {
            if (t.InclusiveMaxBookingsInPeriod == null || bookingCountInMonth <= t.InclusiveMaxBookingsInPeriod.Value)
                return t.MonthlyFee;
        }

        return tiers.OrderByDescending(x => x.Ordinal).FirstOrDefault()?.MonthlyFee;
    }
}
