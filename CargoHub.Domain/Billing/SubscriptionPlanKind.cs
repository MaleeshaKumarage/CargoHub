namespace CargoHub.Domain.Billing;

/// <summary>Subscription product template assigned to a company.</summary>
public enum SubscriptionPlanKind
{
    Trial = 0,
    PayPerBooking = 1,
    TieredPayPerBooking = 2,
    MonthlyBundle = 3,
    TieredMonthlyByUsage = 4,
}
