using MediatR;
using CompanyEntity = HiavaNet.Domain.Companies.Company;

namespace HiavaNet.Application.Company.Queries;

public sealed record GetCompanyByIdQuery(Guid Id) : IRequest<CompanyEntity?>;
