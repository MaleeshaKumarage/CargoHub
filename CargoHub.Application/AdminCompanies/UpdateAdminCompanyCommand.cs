using MediatR;

namespace CargoHub.Application.AdminCompanies;

public sealed record UpdateAdminCompanyCommand(
    Guid CompanyId,
    int? MaxUserAccounts,
    int? MaxAdminAccounts,
    bool ResendAdminInvite) : IRequest<AdminCompanyMutationResult>;
