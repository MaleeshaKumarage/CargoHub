using MediatR;
using CompanyEntity = HiavaNet.Domain.Companies.Company;

namespace HiavaNet.Application.Company.Commands;

/// <summary>
/// Creates a new company, optionally linked to the current user (CustomerId).
/// </summary>
public sealed record CreateCompanyCommand(CompanyEntity Company, string? CustomerId) : IRequest<CompanyEntity>;
