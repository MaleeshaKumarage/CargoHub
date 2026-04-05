using CargoHub.Application.Company.Commands;
using CargoHub.Application.Company.Queries;
using CargoHub.Domain.Companies;
using CargoHub.Infrastructure.Persistence;
using Xunit;
using CompanyEntity = CargoHub.Domain.Companies.Company;

namespace CargoHub.Tests.Integration;

public class CompanyRepositoryTestDbTests : IDisposable
{
    private readonly TestDbFixture _fixture;

    public CompanyRepositoryTestDbTests()
    {
        _fixture = new TestDbFixture();
    }

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task Create_ThenGetById_ReturnsCompany()
    {
        using var context = _fixture.CreateContext();
        var repo = new CompanyRepository(context);
        var company = new CompanyEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = "comp-1",
            Name = "Acme Oy",
            BusinessId = "1234567-8",
            CustomerId = "cust-1"
        };

        var created = await repo.CreateAsync(company, default);
        Assert.Equal(company.Id, created.Id);

        var loaded = await repo.GetByIdAsync(company.Id, default);
        Assert.NotNull(loaded);
        Assert.Equal("Acme Oy", loaded.Name);
        Assert.Equal("1234567-8", loaded.BusinessId);
    }

    [Fact]
    public async Task GetByBusinessId_WhenExists_ReturnsCompany()
    {
        using var context = _fixture.CreateContext();
        var repo = new CompanyRepository(context);
        var company = new CompanyEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = "comp-2",
            Name = "Test Ltd",
            BusinessId = "9876543-2",
            CustomerId = "cust-2"
        };
        await repo.CreateAsync(company, default);

        var loaded = await repo.GetByBusinessIdAsync("9876543-2", default);
        Assert.NotNull(loaded);
        Assert.Equal("Test Ltd", loaded.Name);
    }

    [Fact]
    public async Task CreateCompanyCommandHandler_WithRepository_PersistsCompany()
    {
        using var context = _fixture.CreateContext();
        var repo = new CompanyRepository(context);
        var handler = new CreateCompanyCommandHandler(repo);
        var company = new CompanyEntity { Name = "Handler Test", BusinessId = "1111111-1" };

        var result = await handler.Handle(new CreateCompanyCommand(company, "cust-handler"), default);
        Assert.NotNull(result);
        Assert.Equal("cust-handler", result.CustomerId);

        var loaded = await repo.GetByIdAsync(result.Id, default);
        Assert.NotNull(loaded);
        Assert.Equal("Handler Test", loaded.Name);
    }

    [Fact]
    public async Task GetByBusinessId_WhenEmpty_ReturnsNull()
    {
        using var context = _fixture.CreateContext();
        var repo = new CompanyRepository(context);
        var result = await repo.GetByBusinessIdAsync("", default);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByBusinessId_WhenWhitespace_ReturnsNull()
    {
        using var context = _fixture.CreateContext();
        var repo = new CompanyRepository(context);
        var result = await repo.GetByBusinessIdAsync("   ", default);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetEnabledCourierIds_SkipsBlankContractNumbers()
    {
        using var context = _fixture.CreateContext();
        var company = new CompanyEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = "ag-1",
            Name = "Agreements Oy",
            BusinessId = "5555555-5"
        };
        company.AgreementNumbers.Add(new AgreementNumber
        {
            Id = Guid.NewGuid(),
            PostalService = "DHLExpress",
            Number = "C-1",
            Service = ""
        });
        company.AgreementNumbers.Add(new AgreementNumber
        {
            Id = Guid.NewGuid(),
            PostalService = "Posti",
            Number = " ",
            Service = ""
        });
        context.Companies.Add(company);
        await context.SaveChangesAsync();

        var repo = new CompanyRepository(context);
        var enabled = await repo.GetEnabledCourierIdsForCompanyAsync(company.Id, default);
        Assert.Single(enabled);
        Assert.Contains("DHLExpress", enabled);

        var withAgreements = await repo.GetByBusinessIdWithAgreementsAsync("5555555-5", default);
        Assert.NotNull(withAgreements);
        Assert.Equal(2, withAgreements.AgreementNumbers.Count);
    }

    [Fact]
    public async Task UpdateAsync_PersistsBookingFieldRulesJson()
    {
        using var context = _fixture.CreateContext();
        var repo = new CompanyRepository(context);
        var company = new CompanyEntity
        {
            Id = Guid.NewGuid(),
            CompanyId = "rules-1",
            Name = "Rules Co",
            BusinessId = "2222222-2"
        };
        await repo.CreateAsync(company, default);
        var tracked = await repo.GetByIdForUpdateAsync(company.Id, default);
        Assert.NotNull(tracked);
        tracked.Configurations ??= new CompanyConfiguration();
        tracked.Configurations.BookingFieldRulesJson = """{"version":1,"sections":{"shipper":"mandatory"},"fields":{}}""";
        await repo.UpdateAsync(tracked, default);

        var loaded = await repo.GetByIdAsync(company.Id, default);
        Assert.NotNull(loaded);
        Assert.NotNull(loaded.Configurations?.BookingFieldRulesJson);
        Assert.Contains("shipper", loaded.Configurations.BookingFieldRulesJson, StringComparison.Ordinal);
    }
}
