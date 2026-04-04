using MediatR;

namespace CargoHub.Application.AdminCompanies;

public sealed record CreateAdminCompanyCommand(
    string Name,
    string BusinessId,
    int? MaxUserAccounts,
    int? MaxAdminAccounts,
    IReadOnlyList<string>? InitialAdminEmails) : IRequest<AdminCompanyMutationResult>;
