using MediatR;
using CompanyEntity = CargoHub.Domain.Companies.Company;

namespace CargoHub.Application.Company.Commands;

/// <summary>
/// Creates a new company, optionally linked to the current user (CustomerId).
/// </summary>
public sealed record CreateCompanyCommand(CompanyEntity Company, string? CustomerId) : IRequest<CompanyEntity>;
