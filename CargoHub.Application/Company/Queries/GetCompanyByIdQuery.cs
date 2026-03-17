using MediatR;
using CompanyEntity = CargoHub.Domain.Companies.Company;

namespace CargoHub.Application.Company.Queries;

public sealed record GetCompanyByIdQuery(Guid Id) : IRequest<CompanyEntity?>;
