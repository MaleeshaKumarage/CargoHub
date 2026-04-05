namespace CargoHub.Application.Billing.AdminPlans;

public interface ISubscriptionPlanAdminRepository
{
    Task<AdminSubscriptionPlanDetailDto?> GetPlanDetailAsync(Guid planId, CancellationToken cancellationToken = default);

    Task<bool> PlanExistsAsync(Guid planId, CancellationToken cancellationToken = default);

    Task<int> CountCompaniesUsingPlanAsync(Guid planId, CancellationToken cancellationToken = default);

    Task<Guid> CreatePlanAsync(
        string name,
        string kind,
        string chargeTimeAnchor,
        int? trialBookingAllowance,
        string currency,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<AdminPlanMutationResult> UpdatePlanAsync(
        Guid planId,
        string name,
        string kind,
        string chargeTimeAnchor,
        int? trialBookingAllowance,
        string currency,
        bool isActive,
        CancellationToken cancellationToken = default);

    Task<AdminPlanMutationResult> DeletePlanAsync(Guid planId, CancellationToken cancellationToken = default);

    Task<AdminPlanMutationResult> AddPricingPeriodAsync(
        Guid planId,
        DateTime effectiveFromUtc,
        decimal? chargePerBooking,
        decimal? monthlyFee,
        int? includedBookingsPerMonth,
        decimal? overageChargePerBooking,
        CancellationToken cancellationToken = default);

    Task<AdminPlanMutationResult> UpdatePricingPeriodAsync(
        Guid periodId,
        DateTime effectiveFromUtc,
        decimal? chargePerBooking,
        decimal? monthlyFee,
        int? includedBookingsPerMonth,
        decimal? overageChargePerBooking,
        CancellationToken cancellationToken = default);

    Task<AdminPlanMutationResult> DeletePricingPeriodAsync(Guid periodId, CancellationToken cancellationToken = default);

    Task<AdminPlanMutationResult> ReplaceTiersAsync(
        Guid periodId,
        IReadOnlyList<AdminPricingTierInput> tiers,
        CancellationToken cancellationToken = default);
}

/// <summary>Input row when replacing tiers (Id empty = new row).</summary>
public sealed class AdminPricingTierInput
{
    public Guid? Id { get; init; }
    public int Ordinal { get; init; }
    public int? InclusiveMaxBookingsInPeriod { get; init; }
    public decimal? ChargePerBooking { get; init; }
    public decimal? MonthlyFee { get; init; }
}
