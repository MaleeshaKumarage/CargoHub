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
}

public sealed class CountByKeyDto
{
    public string Key { get; set; } = "";
    public int Count { get; set; }
}
