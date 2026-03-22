namespace CargoHub.Application.Bookings.Dtos;

/// <summary>
/// Dashboard booking statistics for the current user's company (customer).
/// </summary>
public sealed class DashboardBookingStatsDto
{
    public int CountToday { get; set; }
    public int CountMonth { get; set; }
    public int CountYear { get; set; }
    /// <summary>Booking count per courier (CarrierId or PostalService). Sorted by count descending.</summary>
    public List<CountByKeyDto> ByCourier { get; set; } = new();
    /// <summary>Booking count per origin city (pickup/shipper). Sorted by count descending.</summary>
    public List<CountByKeyDto> FromCities { get; set; } = new();
    /// <summary>Booking count per destination city (delivery/receiver). Sorted by count descending.</summary>
    public List<CountByKeyDto> ToCities { get; set; } = new();

    /// <summary>Active scope: all (non-draft), drafts, or tests.</summary>
    public string Scope { get; set; } = "all";

    /// <summary>ECharts sunburst: carrier → service (nested).</summary>
    public SunburstNodeDto? CarrierServiceSunburst { get; set; }

    /// <summary>Origin → carrier → destination flows.</summary>
    public SankeyGraphDto LaneSankey { get; set; } = new();

    /// <summary>Last 30 days UTC, one bucket per calendar day.</summary>
    public List<DailyCountDto> BookingsPerDayLast30 { get; set; } = new();

    public KpiExtendedDto Kpi { get; set; } = new();

    /// <summary>Hours from booking creation to Delivered milestone (when available).</summary>
    public DeliveryTimeDistributionDto DeliveryTime { get; set; } = new();

    /// <summary>Booking creations by day-of-week (0=Sun … 6=Sat) and hour UTC.</summary>
    public HeatmapGridDto BookingVolumeHeatmap { get; set; } = new();

    /// <summary>Transport/update status signals (exception/delay/error) by DOW × hour UTC.</summary>
    public HeatmapGridDto ExceptionSignalsHeatmap { get; set; } = new();
}

public sealed class CountByKeyDto
{
    public string Key { get; set; } = "";
    public int Count { get; set; }
}

public sealed class SunburstNodeDto
{
    public string Name { get; set; } = "";
    public int Value { get; set; }
    public List<SunburstNodeDto>? Children { get; set; }
}

public sealed class SankeyGraphDto
{
    public List<SankeyNodeDto> Nodes { get; set; } = new();
    public List<SankeyLinkDto> Links { get; set; } = new();
}

public sealed class SankeyNodeDto
{
    public string Name { get; set; } = "";
}

public sealed class SankeyLinkDto
{
    public string Source { get; set; } = "";
    public string Target { get; set; } = "";
    public int Value { get; set; }
}

public sealed class DailyCountDto
{
    public string Date { get; set; } = "";
    public int Count { get; set; }
}

public sealed class KpiExtendedDto
{
    /// <summary>Average completed bookings per day over the last 30 UTC days (same scope as stats).</summary>
    public double AvgPerDayLast30 { get; set; }
    public int DraftCount { get; set; }
    public int TestBookingCount { get; set; }
    /// <summary>Estimated stuck: non-draft, not delivered in status history, older than 7 days.</summary>
    public int PossiblyStuckCount { get; set; }
}

/// <summary>Five-number summary + optional raw samples for box plot.</summary>
public sealed class DeliveryTimeDistributionDto
{
    public int SampleSize { get; set; }
    public double MinHours { get; set; }
    public double Q1Hours { get; set; }
    public double MedianHours { get; set; }
    public double Q3Hours { get; set; }
    public double MaxHours { get; set; }
    public int OutlierCount { get; set; }
    /// <summary>Subset of hours for charting (capped server-side).</summary>
    public List<double> SampleHours { get; set; } = new();
}

public sealed class HeatmapGridDto
{
    public List<HeatmapCellDto> Cells { get; set; } = new();
    public int MaxCount { get; set; }
}

public sealed class HeatmapCellDto
{
    /// <summary>0 = Sunday … 6 = Saturday (UTC).</summary>
    public int DayOfWeek { get; set; }
    public int Hour { get; set; }
    public int Count { get; set; }
}
