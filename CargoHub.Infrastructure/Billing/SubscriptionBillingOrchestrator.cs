using CargoHub.Application.Billing;
using CargoHub.Domain.Billing;
using CargoHub.Domain.Bookings;
using CargoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Infrastructure.Billing;

public sealed class SubscriptionBillingOrchestrator : ISubscriptionBillingOrchestrator
{
    private const string TrialLimitCode = "TrialBookingLimitExceeded";
    private const string MonthlyBaseComponent = "base";

    private readonly ApplicationDbContext _db;

    public SubscriptionBillingOrchestrator(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> ConfirmDraftWithBillingAsync(Guid bookingId, string customerId, CancellationToken cancellationToken = default)
    {
        async Task<bool> Body(CancellationToken ct)
        {
            var booking = await _db.Bookings
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.CustomerId == customerId && b.IsDraft, ct);
            if (booking == null)
                return false;

            if (!booking.IsTestBooking)
                await AssertTrialNotExceededForCompanyAsync(booking.CompanyId, ct);

            await ApplyBillableTransitionAsync(booking, ct);

            booking.IsDraft = false;
            booking.Enabled = true;
            booking.UpdatedAtUtc = DateTime.UtcNow;

            await _db.BookingStatusHistory.AddAsync(new BookingStatusHistory
            {
                Id = Guid.NewGuid(),
                BookingId = bookingId,
                Status = BookingStatus.CompletedBooking,
                OccurredAtUtc = DateTime.UtcNow,
                Source = "draft_confirmed"
            }, ct);

            await _db.SaveChangesAsync(ct);
            return true;
        }

        if (_db.Database.IsRelational())
        {
            var strategy = _db.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
                var ok = await Body(cancellationToken);
                await tx.CommitAsync(cancellationToken);
                return ok;
            });
        }

        return await Body(cancellationToken);
    }

    public async Task AssertBillableBookingAllowedAsync(Guid? companyId, bool isTestBooking, CancellationToken cancellationToken = default)
    {
        if (isTestBooking || !companyId.HasValue)
            return;
        await AssertTrialNotExceededForCompanyAsync(companyId, cancellationToken);
    }

    public async Task PostBillingForNewCompletedBookingAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        async Task Body(CancellationToken ct)
        {
            var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == bookingId, ct);
            if (booking == null || booking.IsDraft || booking.IsTestBooking || !booking.CompanyId.HasValue)
                return;

            if (booking.FirstBillableAtUtc != null)
                return;

            await ApplyBillableTransitionAsync(booking, ct);
            booking.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        if (_db.Database.IsRelational())
        {
            var strategy = _db.Database.CreateExecutionStrategy();
            await strategy.ExecuteAsync(async () =>
            {
                await using var tx = await _db.Database.BeginTransactionAsync(cancellationToken);
                await Body(cancellationToken);
                await tx.CommitAsync(cancellationToken);
            });
        }
        else
            await Body(cancellationToken);
    }

    private async Task AssertTrialNotExceededForCompanyAsync(Guid? companyId, CancellationToken cancellationToken)
    {
        if (!companyId.HasValue)
            return;

        var company = await _db.Companies.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == companyId.Value, cancellationToken);
        if (company?.SubscriptionPlanId is not { } planId)
            return;

        var plan = await _db.SubscriptionPlans.AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == planId, cancellationToken);
        if (plan == null || plan.Kind != SubscriptionPlanKind.Trial || plan.TrialBookingAllowance is not int cap)
            return;

        var used = await _db.Bookings.CountAsync(b =>
            b.CompanyId == companyId &&
            !b.IsDraft &&
            !b.IsTestBooking &&
            b.FirstBillableAtUtc != null, cancellationToken);

        if (used >= cap)
            throw new SubscriptionBillingException(TrialLimitCode, "Trial booking allowance has been used.");
    }

    private async Task ApplyBillableTransitionAsync(Booking booking, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        if (booking.FirstBillableAtUtc == null)
            booking.FirstBillableAtUtc = now;

        if (!booking.CompanyId.HasValue)
            return;

        var companyId = booking.CompanyId.Value;
        var planId = await _db.Companies.AsNoTracking()
            .Where(c => c.Id == companyId)
            .Select(c => c.SubscriptionPlanId)
            .FirstOrDefaultAsync(cancellationToken);
        if (planId is not { } pid)
            return;

        var plan = await _db.SubscriptionPlans
            .Include(p => p.PricingPeriods)
            .ThenInclude(pp => pp.Tiers)
            .FirstOrDefaultAsync(p => p.Id == pid && p.IsActive, cancellationToken);
        if (plan == null)
            return;

        if (plan.Kind == SubscriptionPlanKind.Trial)
            return;

        var anchor = GetAnchorUtc(booking, plan);
        var (year, month) = (anchor.Year, anchor.Month);
        var billingPeriod = await GetOrCreateOpenPeriodAsync(companyId, year, month, plan.Currency, cancellationToken);

        switch (plan.Kind)
        {
            case SubscriptionPlanKind.PayPerBooking:
                await PostPayPerBookingAsync(booking, plan, billingPeriod, anchor, cancellationToken);
                break;
            case SubscriptionPlanKind.MonthlyBundle:
                await PostMonthlyBundleAsync(booking, plan, companyId, billingPeriod, anchor, cancellationToken);
                break;
            case SubscriptionPlanKind.TieredPayPerBooking:
                await PostTieredPayPerBookingAsync(booking, plan, companyId, billingPeriod, anchor, cancellationToken);
                break;
            case SubscriptionPlanKind.TieredMonthlyByUsage:
                await PostTieredMonthlyByUsageAsync(booking, plan, companyId, billingPeriod, anchor, cancellationToken);
                break;
        }
    }

    private static DateTime GetAnchorUtc(Booking booking, SubscriptionPlan plan)
    {
        return plan.ChargeTimeAnchor == ChargeTimeAnchor.CreatedAtUtc
            ? booking.CreatedAtUtc
            : booking.FirstBillableAtUtc ?? DateTime.UtcNow;
    }

    private static SubscriptionPlanPricingPeriod? ResolvePeriod(SubscriptionPlan plan, DateTime instantUtc)
    {
        return plan.PricingPeriods
            .Where(pp => pp.EffectiveFromUtc <= instantUtc)
            .OrderByDescending(pp => pp.EffectiveFromUtc)
            .FirstOrDefault();
    }

    private async Task<CompanyBillingPeriod> GetOrCreateOpenPeriodAsync(
        Guid companyId,
        int yearUtc,
        int monthUtc,
        string currency,
        CancellationToken cancellationToken)
    {
        var existing = await _db.CompanyBillingPeriods
            .FirstOrDefaultAsync(p => p.CompanyId == companyId && p.YearUtc == yearUtc && p.MonthUtc == monthUtc, cancellationToken);
        if (existing != null)
            return existing;

        var p = new CompanyBillingPeriod
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            YearUtc = yearUtc,
            MonthUtc = monthUtc,
            Currency = currency,
            Status = CompanyBillingPeriodStatus.Open
        };
        _db.CompanyBillingPeriods.Add(p);
        return p;
    }

    private async Task PostPayPerBookingAsync(
        Booking booking,
        SubscriptionPlan plan,
        CompanyBillingPeriod billingPeriod,
        DateTime anchorUtc,
        CancellationToken cancellationToken)
    {
        var period = ResolvePeriod(plan, anchorUtc);
        if (period?.ChargePerBooking is not { } amount || amount == 0)
            return;

        var exists = await _db.BillingLineItems.AnyAsync(li =>
            li.CompanyBillingPeriodId == billingPeriod.Id &&
            li.BookingId == booking.Id &&
            li.LineType == BillingLineType.PerBooking, cancellationToken);
        if (exists)
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
    }

    private async Task PostMonthlyBundleAsync(
        Booking booking,
        SubscriptionPlan plan,
        Guid companyId,
        CompanyBillingPeriod billingPeriod,
        DateTime anchorUtc,
        CancellationToken cancellationToken)
    {
        var monthStart = new DateTime(billingPeriod.YearUtc, billingPeriod.MonthUtc, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodAtMonth = ResolvePeriod(plan, monthStart);
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

        var rank = await GetBillableRankInMonthAsync(companyId, plan, billingPeriod.YearUtc, billingPeriod.MonthUtc, booking, cancellationToken);
        var included = periodAtMonth.IncludedBookingsPerMonth ?? 0;
        if (rank <= included)
            return;

        if (periodAtMonth.OverageChargePerBooking is not decimal over || over == 0m)
            return;

        var comp = booking.Id.ToString("N");
        var hasOver = await _db.BillingLineItems.AnyAsync(li =>
            li.CompanyBillingPeriodId == billingPeriod.Id &&
            li.BookingId == booking.Id &&
            li.LineType == BillingLineType.Overage &&
            li.Component == comp, cancellationToken);
        if (hasOver)
            return;

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
        DateTime anchorUtc,
        CancellationToken cancellationToken)
    {
        var period = ResolvePeriod(plan, anchorUtc);
        if (period == null)
            return;

        var rank = await GetBillableRankInMonthAsync(companyId, plan, billingPeriod.YearUtc, billingPeriod.MonthUtc, booking, cancellationToken);
        var rate = TieredPaygoRate(period.Tiers, rank);
        if (rate is not { } amount || amount == 0)
            return;

        var comp = booking.Id.ToString("N");
        var hasLine = await _db.BillingLineItems.AnyAsync(li =>
            li.CompanyBillingPeriodId == billingPeriod.Id &&
            li.BookingId == booking.Id &&
            li.LineType == BillingLineType.TieredMarginal &&
            li.Component == comp, cancellationToken);
        if (!hasLine)
        {
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
        }

        await ReconcileTieredPaygoMonthAsync(plan, companyId, billingPeriod, booking, cancellationToken);
    }

    private async Task ReconcileTieredPaygoMonthAsync(
        SubscriptionPlan plan,
        Guid companyId,
        CompanyBillingPeriod billingPeriod,
        Booking volumeSubject,
        CancellationToken cancellationToken)
    {
        var ordered = await LoadBillableBookingsInMonthOrderedAsync(companyId, plan, billingPeriod.YearUtc, billingPeriod.MonthUtc, volumeSubject, cancellationToken);
        for (var i = 0; i < ordered.Count; i++)
        {
            var b = ordered[i];
            var rank = i + 1;
            var anchor = GetAnchorUtc(b, plan);
            var period = ResolvePeriod(plan, anchor);
            if (period == null)
                continue;
            var expected = TieredPaygoRate(period.Tiers, rank) ?? 0;
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
                SubscriptionPlanPricingPeriodId = period.Id,
                CreatedAtUtc = DateTime.UtcNow
            });
        }
    }

    private async Task PostTieredMonthlyByUsageAsync(
        Booking booking,
        SubscriptionPlan plan,
        Guid companyId,
        CompanyBillingPeriod billingPeriod,
        DateTime anchorUtc,
        CancellationToken cancellationToken)
    {
        var monthStart = new DateTime(billingPeriod.YearUtc, billingPeriod.MonthUtc, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodAtMonth = ResolvePeriod(plan, monthStart);
        if (periodAtMonth == null || periodAtMonth.Tiers.Count == 0)
            return;

        var count = await _db.Bookings.CountAsync(b =>
            b.CompanyId == companyId &&
            !b.IsDraft &&
            !b.IsTestBooking &&
            b.FirstBillableAtUtc != null &&
            b.FirstBillableAtUtc >= monthStart &&
            b.FirstBillableAtUtc < monthStart.AddMonths(1), cancellationToken);

        var target = TieredMonthlyFee(periodAtMonth.Tiers, count) ?? 0;
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

    private async Task<int> GetBillableRankInMonthAsync(
        Guid companyId,
        SubscriptionPlan plan,
        int yearUtc,
        int monthUtc,
        Booking booking,
        CancellationToken cancellationToken)
    {
        var ordered = await LoadBillableBookingsInMonthOrderedAsync(companyId, plan, yearUtc, monthUtc, booking, cancellationToken);
        var idx = ordered.FindIndex(b => b.Id == booking.Id);
        return idx < 0 ? ordered.Count + 1 : idx + 1;
    }

    /// <summary>
    /// Includes <paramref name="volumeSubject"/> in the ordered set even before SaveChanges (DB query alone would miss in-flight FirstBillableAtUtc).
    /// </summary>
    private async Task<List<Booking>> LoadBillableBookingsInMonthOrderedAsync(
        Guid companyId,
        SubscriptionPlan plan,
        int yearUtc,
        int monthUtc,
        Booking volumeSubject,
        CancellationToken cancellationToken)
    {
        var monthStart = new DateTime(yearUtc, monthUtc, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var query = _db.Bookings.Where(b =>
            b.CompanyId == companyId &&
            b.Id != volumeSubject.Id &&
            !b.IsDraft &&
            !b.IsTestBooking &&
            b.FirstBillableAtUtc != null &&
            b.FirstBillableAtUtc >= monthStart &&
            b.FirstBillableAtUtc < monthEnd);

        var list = await query.ToListAsync(cancellationToken);

        if (volumeSubject.CompanyId == companyId &&
            !volumeSubject.IsTestBooking &&
            volumeSubject.FirstBillableAtUtc is { } f &&
            f >= monthStart &&
            f < monthEnd)
        {
            list.Add(volumeSubject);
        }

        list.Sort((a, b) =>
        {
            var ta = SortAnchor(a, plan);
            var tb = SortAnchor(b, plan);
            var c = ta.CompareTo(tb);
            return c != 0 ? c : string.CompareOrdinal(a.Id.ToString("N"), b.Id.ToString("N"));
        });

        return list;
    }

    private static DateTime SortAnchor(Booking b, SubscriptionPlan plan) =>
        plan.ChargeTimeAnchor == ChargeTimeAnchor.CreatedAtUtc
            ? b.CreatedAtUtc
            : b.FirstBillableAtUtc ?? b.CreatedAtUtc;

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
