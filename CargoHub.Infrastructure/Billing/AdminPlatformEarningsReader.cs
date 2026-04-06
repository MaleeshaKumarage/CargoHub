using System.Globalization;
using CargoHub.Application.Billing.Admin;
using CargoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Infrastructure.Billing;

public sealed class AdminPlatformEarningsReader : IAdminPlatformEarningsReader
{
    private readonly ApplicationDbContext _db;

    public AdminPlatformEarningsReader(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<PlatformEarningsSeriesPointDto>> GetSeriesAsync(
        PlatformEarningsSeriesRange range,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var todayUtc = now.Date;

        return range switch
        {
            PlatformEarningsSeriesRange.Yesterday => await GetDailySeriesAsync(
                todayUtc.AddDays(-1),
                todayUtc,
                cancellationToken),
            PlatformEarningsSeriesRange.Last7Days => await GetDailySeriesAsync(
                todayUtc.AddDays(-6),
                todayUtc.AddDays(1),
                cancellationToken),
            PlatformEarningsSeriesRange.LastMonth =>
                await GetDailySeriesAsync(
                    new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-1),
                    new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc),
                    cancellationToken),
            PlatformEarningsSeriesRange.Last6Months => MapMonthsToSeries(await GetMonthlyTotalsAsync(6, cancellationToken)),
            PlatformEarningsSeriesRange.LastYear => MapMonthsToSeries(await GetMonthlyTotalsAsync(12, cancellationToken)),
            _ => throw new ArgumentOutOfRangeException(nameof(range), range, null),
        };
    }

    private static IReadOnlyList<PlatformEarningsSeriesPointDto> MapMonthsToSeries(
        IReadOnlyList<PlatformEarningsMonthDto> months) =>
        months
            .Select(m => new PlatformEarningsSeriesPointDto
            {
                Period = $"{m.YearUtc}-{m.MonthUtc:D2}",
                TotalEur = m.TotalEur,
            })
            .ToList();

    private async Task<IReadOnlyList<PlatformEarningsSeriesPointDto>> GetDailySeriesAsync(
        DateTime startUtcInclusive,
        DateTime endUtcExclusive,
        CancellationToken cancellationToken)
    {
        var raw = await (
            from l in _db.BillingLineItems.AsNoTracking()
            join p in _db.CompanyBillingPeriods.AsNoTracking() on l.CompanyBillingPeriodId equals p.Id
            where !l.ExcludedFromInvoice
                  && l.Currency.ToUpper() == "EUR"
                  && l.CreatedAtUtc >= startUtcInclusive
                  && l.CreatedAtUtc < endUtcExclusive
            group l by new { l.CreatedAtUtc.Year, l.CreatedAtUtc.Month, l.CreatedAtUtc.Day } into g
            select new { g.Key.Year, g.Key.Month, g.Key.Day, Total = g.Sum(x => x.Amount) }
        ).ToListAsync(cancellationToken);

        var dict = raw.ToDictionary(
            x => new DateTime(x.Year, x.Month, x.Day, 0, 0, 0, DateTimeKind.Utc),
            x => x.Total);

        var result = new List<PlatformEarningsSeriesPointDto>();
        for (var d = startUtcInclusive.Date; d < endUtcExclusive; d = d.AddDays(1))
        {
            dict.TryGetValue(d, out var total);
            result.Add(new PlatformEarningsSeriesPointDto
            {
                Period = d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                TotalEur = total,
            });
        }

        return result;
    }

    public async Task<IReadOnlyList<PlatformEarningsMonthDto>> GetMonthlyTotalsAsync(
        int months,
        CancellationToken cancellationToken = default)
    {
        var n = Math.Clamp(months, 1, 120);
        var now = DateTime.UtcNow;
        var end = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var start = end.AddMonths(-(n - 1));

        var aggregated = await (
            from l in _db.BillingLineItems.AsNoTracking()
            join p in _db.CompanyBillingPeriods.AsNoTracking() on l.CompanyBillingPeriodId equals p.Id
            where !l.ExcludedFromInvoice && l.Currency.ToUpper() == "EUR"
            group l by new { p.YearUtc, p.MonthUtc } into g
            select new { g.Key.YearUtc, g.Key.MonthUtc, Total = g.Sum(x => x.Amount) }
        ).ToListAsync(cancellationToken);

        var dict = aggregated.ToDictionary(x => (x.YearUtc, x.MonthUtc), x => x.Total);
        var result = new List<PlatformEarningsMonthDto>();
        for (var d = start; d <= end; d = d.AddMonths(1))
        {
            var y = d.Year;
            var m = d.Month;
            dict.TryGetValue((y, m), out var total);
            result.Add(new PlatformEarningsMonthDto { YearUtc = y, MonthUtc = m, TotalEur = total });
        }

        return result;
    }

    public async Task<IReadOnlyList<PlatformEarningsCompanyDto>> GetByCompanyForMonthAsync(
        int yearUtc,
        int monthUtc,
        CancellationToken cancellationToken = default)
    {
        var rows = await (
            from l in _db.BillingLineItems.AsNoTracking()
            join p in _db.CompanyBillingPeriods.AsNoTracking() on l.CompanyBillingPeriodId equals p.Id
            join c in _db.Companies.AsNoTracking() on p.CompanyId equals c.Id
            where !l.ExcludedFromInvoice
                  && l.Currency.ToUpper() == "EUR"
                  && p.YearUtc == yearUtc
                  && p.MonthUtc == monthUtc
            group l by new { p.CompanyId, c.Name } into g
            select new PlatformEarningsCompanyDto
            {
                CompanyId = g.Key.CompanyId,
                CompanyName = g.Key.Name ?? "",
                AmountEur = g.Sum(x => x.Amount)
            }
        ).ToListAsync(cancellationToken);

        return rows.OrderByDescending(r => r.AmountEur).ToList();
    }

    public async Task<IReadOnlyList<PlatformEarningsSubscriptionDto>> GetBySubscriptionForMonthAsync(
        int yearUtc,
        int monthUtc,
        CancellationToken cancellationToken = default)
    {
        var rows = await (
            from l in _db.BillingLineItems.AsNoTracking()
            join p in _db.CompanyBillingPeriods.AsNoTracking() on l.CompanyBillingPeriodId equals p.Id
            where !l.ExcludedFromInvoice
                  && l.Currency.ToUpper() == "EUR"
                  && p.YearUtc == yearUtc
                  && p.MonthUtc == monthUtc
            group l by l.SubscriptionPlanId into g
            select new { PlanId = g.Key, Amount = g.Sum(x => x.Amount) }
        ).ToListAsync(cancellationToken);

        if (rows.Count == 0)
            return Array.Empty<PlatformEarningsSubscriptionDto>();

        var planIds = rows.Select(r => r.PlanId).Distinct().ToList();
        var names = await _db.SubscriptionPlans.AsNoTracking()
            .Where(sp => planIds.Contains(sp.Id))
            .ToDictionaryAsync(sp => sp.Id, sp => sp.Name, cancellationToken);

        var total = rows.Sum(r => r.Amount);
        if (total <= 0m)
            return Array.Empty<PlatformEarningsSubscriptionDto>();

        return rows
            .OrderByDescending(r => r.Amount)
            .Select(r => new PlatformEarningsSubscriptionDto
            {
                PlanId = r.PlanId,
                PlanName = names.TryGetValue(r.PlanId, out var nm) ? nm : "Unknown plan",
                AmountEur = r.Amount,
                Percent = Math.Round(r.Amount / total * 100m, 2, MidpointRounding.AwayFromZero)
            })
            .ToList();
    }
}
