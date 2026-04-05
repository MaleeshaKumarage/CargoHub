namespace CargoHub.Domain.Billing;

/// <summary>Bookings excluded from regenerated invoice lines for a UTC month bucket.</summary>
public class BillingPeriodExcludedBooking
{
    public Guid CompanyBillingPeriodId { get; set; }

    public CompanyBillingPeriod? CompanyBillingPeriod { get; set; }

    public Guid BookingId { get; set; }
}
