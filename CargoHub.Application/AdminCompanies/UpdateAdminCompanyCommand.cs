using MediatR;

namespace CargoHub.Application.AdminCompanies;

public sealed record UpdateAdminCompanyCommand(
    Guid CompanyId,
    int? MaxUserAccounts,
    int? MaxAdminAccounts,
    bool ResendAdminInvite,
    IReadOnlyList<string>? DeactivateUserIds,
    IReadOnlyList<string>? DemoteAdminUserIds,
    Guid? SubscriptionPlanId = null,
    bool? IsActive = null,
    /// <summary>When set, replaces stored initial admin invite targets (only when company has no Admin yet).</summary>
    IReadOnlyList<string>? InitialAdminEmails = null) : IRequest<AdminCompanyMutationResult>;
