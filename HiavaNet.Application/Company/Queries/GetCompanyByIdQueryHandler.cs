using HiavaNet.Application.Company;
using MediatR;
using CompanyEntity = HiavaNet.Domain.Companies.Company;

namespace HiavaNet.Application.Company.Queries;

public sealed class GetCompanyByIdQueryHandler : IRequestHandler<GetCompanyByIdQuery, CompanyEntity?>
{
    private readonly ICompanyRepository _repository;

    public GetCompanyByIdQueryHandler(ICompanyRepository repository)
    {
        _repository = repository;
    }

    public Task<CompanyEntity?> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken)
    {
        return _repository.GetByIdAsync(request.Id, cancellationToken);
    }
}
