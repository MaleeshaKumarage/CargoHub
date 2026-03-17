using CargoHub.Application.Company;
using CargoHub.Application.Company.Commands;
using CargoHub.Domain.Companies;
using Moq;
using Xunit;
using CompanyEntity = CargoHub.Domain.Companies.Company;

namespace CargoHub.Tests.Company;

public class CreateCompanyCommandHandlerTests
{
    [Fact]
    public async Task Handle_CreatesCompanyWithRepository()
    {
        var company = new CompanyEntity { Name = "Acme Oy", BusinessId = "1234567-8" };
        var repo = new Mock<ICompanyRepository>();
        repo.Setup(r => r.CreateAsync(It.IsAny<CompanyEntity>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompanyEntity c, CancellationToken _) => c);

        var handler = new CreateCompanyCommandHandler(repo.Object);
        var result = await handler.Handle(new CreateCompanyCommand(company, "cust-1"), default);

        Assert.NotNull(result);
        Assert.Equal("cust-1", result.CustomerId);
        Assert.Equal("Acme Oy", result.Name);
    }

    [Fact]
    public async Task Handle_WhenIdEmpty_AssignsNewGuid()
    {
        var company = new CompanyEntity { Id = Guid.Empty, Name = "Test" };
        CompanyEntity? captured = null;
        var repo = new Mock<ICompanyRepository>();
        repo.Setup(r => r.CreateAsync(It.IsAny<CompanyEntity>(), It.IsAny<CancellationToken>()))
            .Callback<CompanyEntity, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync((CompanyEntity c, CancellationToken _) => c);

        var handler = new CreateCompanyCommandHandler(repo.Object);
        await handler.Handle(new CreateCompanyCommand(company, null), default);

        Assert.NotNull(captured);
        Assert.NotEqual(Guid.Empty, captured.Id);
    }

    [Fact]
    public async Task Handle_WhenCompanyIdAlreadySet_KeepsIt()
    {
        var company = new CompanyEntity { Id = Guid.NewGuid(), CompanyId = "existing-id", Name = "Test" };
        CompanyEntity? captured = null;
        var repo = new Mock<ICompanyRepository>();
        repo.Setup(r => r.CreateAsync(It.IsAny<CompanyEntity>(), It.IsAny<CancellationToken>()))
            .Callback<CompanyEntity, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync((CompanyEntity c, CancellationToken _) => c);

        var handler = new CreateCompanyCommandHandler(repo.Object);
        await handler.Handle(new CreateCompanyCommand(company, null), default);

        Assert.NotNull(captured);
        Assert.Equal("existing-id", captured.CompanyId);
    }

    [Fact]
    public async Task Handle_WhenCompanyIdEmpty_AssignsGuidAsN()
    {
        var company = new CompanyEntity { Id = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890"), CompanyId = "", Name = "Test" };
        CompanyEntity? captured = null;
        var repo = new Mock<ICompanyRepository>();
        repo.Setup(r => r.CreateAsync(It.IsAny<CompanyEntity>(), It.IsAny<CancellationToken>()))
            .Callback<CompanyEntity, CancellationToken>((c, _) => captured = c)
            .ReturnsAsync((CompanyEntity c, CancellationToken _) => c);

        var handler = new CreateCompanyCommandHandler(repo.Object);
        await handler.Handle(new CreateCompanyCommand(company, null), default);

        Assert.NotNull(captured);
        Assert.Equal("a1b2c3d4e5f67890abcdef1234567890", captured.CompanyId);
    }
}
