namespace CargoHub.Application.Billing;

/// <summary>Stable id for seeded default trial plan (see <see cref="SubscriptionPlanSeed"/>).</summary>
public static class SubscriptionBillingConstants
{
    public static readonly Guid DefaultTrialPlanId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaa1");

    /// <summary>Non-test bookings require a portal company context.</summary>
    public const string CompanyRequiredForBookingErrorCode = "CompanyRequiredForBooking";
}
