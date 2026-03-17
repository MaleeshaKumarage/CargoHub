using CargoHub.Application.Company;
using CargoHub.Domain.Companies;
using MediatR;
using CompanyEntity = CargoHub.Domain.Companies.Company;

namespace CargoHub.Application.Company.Commands;

public sealed class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, CompanyEntity>
{
    private readonly ICompanyRepository _repository;

    public CreateCompanyCommandHandler(ICompanyRepository repository)
    {
        _repository = repository;
    }

    public async Task<CompanyEntity> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = request.Company;
        if (company.Id == Guid.Empty)
            company.Id = Guid.NewGuid();
        if (string.IsNullOrWhiteSpace(company.CompanyId))
            company.CompanyId = company.Id.ToString("N"); // Auto-generated logical id (GUID without dashes), aligns with booking-backend
        company.CustomerId = request.CustomerId;
        return await _repository.CreateAsync(company, cancellationToken);
    }
}
