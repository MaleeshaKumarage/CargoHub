using CargoHub.Application.Company;
using CargoHub.Application.Company.Queries;
using CargoHub.Domain.Companies;
using Moq;
using Xunit;
using CompanyEntity = CargoHub.Domain.Companies.Company;

namespace CargoHub.Tests.Company;

public class GetCompanyByIdQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenCompanyExists_ReturnsCompany()
    {
        var id = Guid.NewGuid();
        var company = new CompanyEntity { Id = id, Name = "Acme Oy", BusinessId = "1234567-8" };
        var repo = new Mock<ICompanyRepository>();
        repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(company);

        var handler = new GetCompanyByIdQueryHandler(repo.Object);
        var result = await handler.Handle(new GetCompanyByIdQuery(id), default);

        Assert.NotNull(result);
        Assert.Equal(id, result.Id);
        Assert.Equal("Acme Oy", result.Name);
    }

    [Fact]
    public async Task Handle_WhenCompanyNotFound_ReturnsNull()
    {
        var id = Guid.NewGuid();
        var repo = new Mock<ICompanyRepository>();
        repo.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((CompanyEntity?)null);

        var handler = new GetCompanyByIdQueryHandler(repo.Object);
        var result = await handler.Handle(new GetCompanyByIdQuery(id), default);

        Assert.Null(result);
    }
}
