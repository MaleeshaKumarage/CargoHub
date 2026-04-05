namespace CargoHub.Domain.Billing;

/// <summary>Posted subscription billing line classification.</summary>
public enum BillingLineType
{
    PerBooking = 0,
    MonthlyBase = 1,
    Overage = 2,
    TieredMarginal = 3,
    TieredMonthlyFee = 4,
    Adjustment = 5,
    PeriodAdjustment = 6,
}
