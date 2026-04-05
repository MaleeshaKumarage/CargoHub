using MediatR;

namespace CargoHub.Application.AdminCompanies;

public sealed record UpdateAdminCompanyCommand(
    Guid CompanyId,
    int? MaxUserAccounts,
    int? MaxAdminAccounts,
    bool ResendAdminInvite,
    IReadOnlyList<string>? DeactivateUserIds,
    IReadOnlyList<string>? DemoteAdminUserIds,
    Guid? SubscriptionPlanId = null) : IRequest<AdminCompanyMutationResult>;
