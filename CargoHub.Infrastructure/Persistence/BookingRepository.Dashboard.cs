using CargoHub.Application.Bookings.Dtos;
using CargoHub.Domain.Bookings;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Infrastructure.Persistence;

public sealed partial class BookingRepository
{
    public async Task<DashboardBookingStatsDto> GetDashboardStatsAsync(
        string? customerId,
        string? scope,
        int? heatmapYear = null,
        int? heatmapMonth = null,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var startOfToday = DateOnly.FromDateTime(now).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfYear = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var start30 = now.Date.AddDays(-30);

        var normalizedScope = string.IsNullOrWhiteSpace(scope) ? "all" : scope.Trim().ToLowerInvariant();

        IQueryable<Booking> forCustomer = string.IsNullOrEmpty(customerId)
            ? _db.Bookings.AsNoTracking()
            : _db.Bookings.AsNoTracking().Where(b => b.CustomerId == customerId);

        var baseQuery = ApplyDashboardScope(forCustomer, normalizedScope);

        var countToday = await baseQuery.CountAsync(b => b.CreatedAtUtc >= startOfToday, cancellationToken);
        var countMonth = await baseQuery.CountAsync(b => b.CreatedAtUtc >= startOfMonth, cancellationToken);
        var countYear = await baseQuery.CountAsync(b => b.CreatedAtUtc >= startOfYear, cancellationToken);

        var cutoffStuck = now.AddDays(-7);
        var possiblyStuckCount = await forCustomer
            .Where(b => !b.IsDraft && b.CreatedAtUtc < cutoffStuck)
            .Where(b => !_db.BookingStatusHistory.Any(h => h.BookingId == b.Id && h.Status == BookingStatus.Delivered))
            .CountAsync(cancellationToken);

        var countLast30 = await baseQuery.CountAsync(b => b.CreatedAtUtc >= start30, cancellationToken);
        var avgPerDayLast30 = countLast30 / 30.0;
        var dayOfMonth = now.Day;
        var dayOfYear = now.DayOfYear;
        var avgPerDayThisMonth = countMonth / (double)Math.Max(1, dayOfMonth);
        var avgPerDayThisYear = countYear / (double)Math.Max(1, dayOfYear);

        var rows = await baseQuery
            .Select(b => new DashboardRow(
                b.Id,
                b.CreatedAtUtc,
                b.Shipment.CarrierId,
                b.Header.PostalService,
                b.Shipment.Service,
                b.PickUpAddress.City,
                b.Shipper.City,
                b.DeliveryPoint.City,
                b.Receiver.City))
            .ToListAsync(cancellationToken);

        static string Norm(string? s) => string.IsNullOrWhiteSpace(s) ? "(Not set)" : s.Trim();

        var byCourier = rows
            .GroupBy(r => Norm(r.CarrierId ?? r.PostalService))
            .Select(g => new CountByKeyDto { Key = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        var fromCities = rows
            .GroupBy(r => Norm(r.PickUpCity ?? r.ShipperCity))
            .Select(g => new CountByKeyDto { Key = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        var toCities = rows
            .GroupBy(r => Norm(r.DeliveryCity ?? r.ReceiverCity))
            .Select(g => new CountByKeyDto { Key = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        var sunburst = BuildCarrierServiceSunburst(rows);
        var sankey = BuildSankey(rows);
        var daily = BuildDailySeries(rows, now);
        var calY = heatmapYear ?? now.Year;
        var calM = heatmapMonth ?? now.Month;
        if (calM is < 1 or > 12)
        {
            calY = now.Year;
            calM = now.Month;
        }

        var firstSelected = new DateTime(calY, calM, 1, 0, 0, 0, DateTimeKind.Utc);
        var firstNow = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        if (firstSelected > firstNow)
        {
            calY = now.Year;
            calM = now.Month;
        }

        var calendarMonth = BuildBookingsPerDayForMonth(rows, calY, calM);
        var completedMonthTask = BuildBookingCountsPerCalendarMonthAsync(
            forCustomer.Where(b => !b.IsDraft),
            calY,
            calM,
            cancellationToken);
        var draftsMonthTask = BuildBookingCountsPerCalendarMonthAsync(
            forCustomer.Where(b => b.IsDraft),
            calY,
            calM,
            cancellationToken);
        await Task.WhenAll(completedMonthTask, draftsMonthTask).ConfigureAwait(false);
        var completedCalendarMonth = await completedMonthTask.ConfigureAwait(false);
        var draftsCalendarMonth = await draftsMonthTask.ConfigureAwait(false);
        var delivery = await BuildDeliveryTimeDistributionAsync(baseQuery, cancellationToken);
        var exceptionHeat = await BuildExceptionHeatmapAsync(forCustomer, normalizedScope, cancellationToken);

        return new DashboardBookingStatsDto
        {
            Scope = normalizedScope,
            CountToday = countToday,
            CountMonth = countMonth,
            CountYear = countYear,
            ByCourier = byCourier,
            FromCities = fromCities,
            ToCities = toCities,
            CarrierServiceSunburst = sunburst,
            LaneSankey = sankey,
            BookingsPerDayLast30 = daily,
            BookingsPerDayCurrentMonth = calendarMonth,
            CompletedBookingsPerDayCurrentMonth = completedCalendarMonth,
            DraftsPerDayCurrentMonth = draftsCalendarMonth,
            Kpi = new KpiExtendedDto
            {
                AvgPerDayLast30 = Math.Round(avgPerDayLast30, 2),
                AvgPerDayThisMonth = Math.Round(avgPerDayThisMonth, 2),
                AvgPerDayThisYear = Math.Round(avgPerDayThisYear, 2),
                PossiblyStuckCount = possiblyStuckCount
            },
            DeliveryTime = delivery,
            ExceptionSignalsHeatmap = exceptionHeat
        };
    }

    private static IQueryable<Booking> ApplyDashboardScope(IQueryable<Booking> forCustomer, string normalizedScope) =>
        normalizedScope switch
        {
            "drafts" => forCustomer.Where(b => b.IsDraft),
            _ => forCustomer.Where(b => !b.IsDraft)
        };

    private sealed record DashboardRow(
        Guid Id,
        DateTime CreatedAtUtc,
        string? CarrierId,
        string? PostalService,
        string? Service,
        string? PickUpCity,
        string? ShipperCity,
        string? DeliveryCity,
        string? ReceiverCity);

    private static SunburstNodeDto? BuildCarrierServiceSunburst(List<DashboardRow> rows)
    {
        if (rows.Count == 0) return null;
        static string Norm(string? s) => string.IsNullOrWhiteSpace(s) ? "(Not set)" : s.Trim();
        var byCarrier = rows.GroupBy(r => Norm(r.CarrierId ?? r.PostalService));
        var children = new List<SunburstNodeDto>();
        foreach (var cg in byCarrier.OrderByDescending(g => g.Count()))
        {
            var serviceGroups = cg.GroupBy(r => Norm(r.Service ?? "(default)"))
                .Select(sg => new SunburstNodeDto
                {
                    Name = sg.Key,
                    Value = sg.Count()
                })
                .OrderByDescending(x => x.Value)
                .ToList();
            children.Add(new SunburstNodeDto
            {
                Name = cg.Key,
                Value = cg.Count(),
                Children = serviceGroups
            });
        }

        return new SunburstNodeDto { Name = "Carriers", Value = rows.Count, Children = children };
    }

    private static SankeyGraphDto BuildSankey(List<DashboardRow> rows)
    {
        var result = new SankeyGraphDto();
        if (rows.Count == 0) return result;
        static string Norm(string? s) => string.IsNullOrWhiteSpace(s) ? "(Not set)" : s.Trim();

        var oc = new Dictionary<(string O, string C), int>();
        var cd = new Dictionary<(string C, string D), int>();
        foreach (var r in rows)
        {
            var o = "O:" + Norm(r.PickUpCity ?? r.ShipperCity);
            var c = "C:" + Norm(r.CarrierId ?? r.PostalService);
            var d = "D:" + Norm(r.DeliveryCity ?? r.ReceiverCity);
            oc.TryGetValue((o, c), out var v1);
            oc[(o, c)] = v1 + 1;
            cd.TryGetValue((c, d), out var v2);
            cd[(c, d)] = v2 + 1;
        }

        var names = new HashSet<string>();
        foreach (var kv in oc)
        {
            names.Add(kv.Key.O);
            names.Add(kv.Key.C);
        }

        foreach (var kv in cd)
        {
            names.Add(kv.Key.C);
            names.Add(kv.Key.D);
        }

        result.Nodes = names.OrderBy(x => x).Select(n => new SankeyNodeDto { Name = n }).ToList();
        foreach (var kv in oc.OrderByDescending(x => x.Value))
            result.Links.Add(new SankeyLinkDto { Source = kv.Key.O, Target = kv.Key.C, Value = kv.Value });
        foreach (var kv in cd.OrderByDescending(x => x.Value))
            result.Links.Add(new SankeyLinkDto { Source = kv.Key.C, Target = kv.Key.D, Value = kv.Value });
        return result;
    }

    private static List<DailyCountDto> BuildDailySeries(List<DashboardRow> rows, DateTime nowUtc)
    {
        var list = new List<DailyCountDto>();
        for (var i = 29; i >= 0; i--)
        {
            var d = DateOnly.FromDateTime(nowUtc.AddDays(-i));
            list.Add(new DailyCountDto { Date = d.ToString("yyyy-MM-dd"), Count = 0 });
        }

        var idx = list.ToDictionary(x => x.Date, x => x);
        foreach (var r in rows)
        {
            var key = DateOnly.FromDateTime(r.CreatedAtUtc).ToString("yyyy-MM-dd");
            if (idx.TryGetValue(key, out var cell))
                cell.Count++;
        }

        return list;
    }

    private static List<DailyCountDto> BuildBookingsPerDayForMonth(List<DashboardRow> rows, int year, int month)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var dict = new Dictionary<string, int>();
        foreach (var r in rows)
        {
            var key = DateOnly.FromDateTime(r.CreatedAtUtc).ToString("yyyy-MM-dd");
            if (!IsDateInUtcMonth(key, year, month))
                continue;
            dict.TryGetValue(key, out var n);
            dict[key] = n + 1;
        }

        return BuildDailyListForMonth(year, month, dict);
    }

    private static bool IsDateInUtcMonth(string yyyyMmDd, int year, int month)
    {
        if (!DateOnly.TryParse(yyyyMmDd, out var d))
            return false;
        return d.Year == year && d.Month == month;
    }

    private static List<DailyCountDto> BuildDailyListForMonth(int year, int month, Dictionary<string, int> countsByDate)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var list = new List<DailyCountDto>();
        for (var d = 1; d <= daysInMonth; d++)
        {
            var date = new DateOnly(year, month, d);
            var key = date.ToString("yyyy-MM-dd");
            list.Add(new DailyCountDto { Date = key, Count = countsByDate.GetValueOrDefault(key) });
        }

        return list;
    }

    private static async Task<List<DailyCountDto>> BuildBookingCountsPerCalendarMonthAsync(
        IQueryable<Booking> query,
        int year,
        int month,
        CancellationToken cancellationToken)
    {
        var monthStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);
        // Load timestamps only; GroupBy(DateOnly.FromDateTime(...)) is not reliably translatable to SQL.
        var createdList = await query
            .Where(b => b.CreatedAtUtc >= monthStart && b.CreatedAtUtc < monthEnd)
            .Select(b => b.CreatedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        var dict = createdList
            .GroupBy(d => DateOnly.FromDateTime(d))
            .ToDictionary(g => g.Key.ToString("yyyy-MM-dd"), g => g.Count());
        return BuildDailyListForMonth(year, month, dict);
    }

    private async Task<DeliveryTimeDistributionDto> BuildDeliveryTimeDistributionAsync(
        IQueryable<Booking> scopedBookings,
        CancellationToken cancellationToken)
    {
        var pairs = await _db.BookingStatusHistory
            .AsNoTracking()
            .Where(h => h.Status == BookingStatus.Delivered)
            .Join(
                scopedBookings.Select(b => new { b.Id, b.CreatedAtUtc }),
                h => h.BookingId,
                b => b.Id,
                (h, b) => new { b.CreatedAtUtc, DeliveredAt = h.OccurredAtUtc })
            .ToListAsync(cancellationToken);

        var hours = pairs
            .Select(p => (p.DeliveredAt - p.CreatedAtUtc).TotalHours)
            .Where(h => h >= 0 && h < 24 * 365)
            .OrderBy(h => h)
            .ToList();

        var dto = new DeliveryTimeDistributionDto { SampleSize = hours.Count };
        if (hours.Count == 0)
            return dto;

        dto.MinHours = hours[0];
        dto.MaxHours = hours[^1];
        dto.MedianHours = PercentileSorted(hours, 0.5);
        dto.Q1Hours = PercentileSorted(hours, 0.25);
        dto.Q3Hours = PercentileSorted(hours, 0.75);
        var iqr = dto.Q3Hours - dto.Q1Hours;
        var low = dto.Q1Hours - 1.5 * iqr;
        var high = dto.Q3Hours + 1.5 * iqr;
        dto.OutlierCount = hours.Count(h => h < low || h > high);
        dto.SampleHours = hours.Take(120).ToList();
        return dto;
    }

    private static double PercentileSorted(List<double> sorted, double p)
    {
        if (sorted.Count == 0) return 0;
        var pos = (sorted.Count - 1) * p;
        var lo = (int)Math.Floor(pos);
        var hi = (int)Math.Ceiling(pos);
        if (lo == hi) return sorted[lo];
        return sorted[lo] + (pos - lo) * (sorted[hi] - sorted[lo]);
    }

    private async Task<HeatmapGridDto> BuildExceptionHeatmapAsync(
        IQueryable<Booking> forCustomer,
        string normalizedScope,
        CancellationToken cancellationToken)
    {
        var scoped = ApplyDashboardScope(forCustomer, normalizedScope);
        var since = DateTime.UtcNow.AddDays(-90);
        var rows = await scoped
            .SelectMany(b => b.Updates, (b, u) => new { u.Status, u.CreatedAtUtc })
            .Where(x => x.CreatedAtUtc >= since)
            .ToListAsync(cancellationToken);

        static bool IsSignal(string? s)
        {
            if (string.IsNullOrEmpty(s)) return false;
            var t = s.ToLowerInvariant();
            return t.Contains("exception") || t.Contains("delay") || t.Contains("error") || t.Contains("fail");
        }

        var cells = new int[7, 24];
        foreach (var r in rows.Where(x => IsSignal(x.Status)))
        {
            var utc = r.CreatedAtUtc;
            var dow = (int)utc.DayOfWeek;
            var h = utc.Hour;
            cells[dow, h]++;
        }

        var flat = new List<HeatmapCellDto>();
        var max = 0;
        for (var d = 0; d < 7; d++)
        for (var h = 0; h < 24; h++)
        {
            var c = cells[d, h];
            if (c > max) max = c;
            flat.Add(new HeatmapCellDto { DayOfWeek = d, Hour = h, Count = c });
        }

        return new HeatmapGridDto { Cells = flat, MaxCount = max };
    }
}
