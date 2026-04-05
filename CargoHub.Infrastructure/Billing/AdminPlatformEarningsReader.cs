using CargoHub.Application.Billing.Admin;
using CargoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Infrastructure.Billing;

public sealed class AdminPlatformEarningsReader : IAdminPlatformEarningsReader
{
    private readonly ApplicationDbContext _db;

    public AdminPlatformEarningsReader(ApplicationDbContext db) => _db = db;

    private static bool IsEur(string? currency) =>
        string.Equals(currency, "EUR", StringComparison.OrdinalIgnoreCase);

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
            where !l.ExcludedFromInvoice && IsEur(l.Currency)
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
                  && IsEur(l.Currency)
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
                  && IsEur(l.Currency)
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
