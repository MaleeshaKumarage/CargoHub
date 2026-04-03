using System.Security.Claims;
using CargoHub.Application.Auth.Abstractions;
using CargoHub.Application.Auth.Commands;
using CargoHub.Application.Auth.Dtos;
using CargoHub.Application.Auth.Handlers;
using CargoHub.Application.Company;
using CargoHub.Domain.Companies;
using Moq;
using Xunit;
using CompanyEntity = CargoHub.Domain.Companies.Company;

namespace CargoHub.Tests.Auth;

public class RegisterUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenBusinessIdWhitespace_ReturnsCompanyIdRequired()
    {
        var companyRepo = new Mock<ICompanyRepository>();
        var regService = new Mock<IUserRegistrationService>();
        var jwtFactory = new Mock<IJwtTokenFactory>();

        var handler = new RegisterUserCommandHandler(companyRepo.Object, regService.Object, jwtFactory.Object);
        var result = await handler.Handle(new RegisterUserCommand(new PortalRegisterRequest { BusinessId = "   " }), default);

        Assert.False(result.Success);
        Assert.Equal("CompanyIdRequired", result.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenBusinessIdEmpty_ReturnsCompanyIdRequired()
    {
        var companyRepo = new Mock<ICompanyRepository>();
        var regService = new Mock<IUserRegistrationService>();
        var jwtFactory = new Mock<IJwtTokenFactory>();

        var handler = new RegisterUserCommandHandler(companyRepo.Object, regService.Object, jwtFactory.Object);
        var result = await handler.Handle(new RegisterUserCommand(new PortalRegisterRequest { BusinessId = "" }), default);

        Assert.False(result.Success);
        Assert.Equal("CompanyIdRequired", result.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenCompanyNotFound_ReturnsCompanyNotFound()
    {
        var companyRepo = new Mock<ICompanyRepository>();
        companyRepo.Setup(r => r.GetByBusinessIdAsync("1234567-8", It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompanyEntity?)null);
        var regService = new Mock<IUserRegistrationService>();
        var jwtFactory = new Mock<IJwtTokenFactory>();

        var handler = new RegisterUserCommandHandler(companyRepo.Object, regService.Object, jwtFactory.Object);
        var result = await handler.Handle(new RegisterUserCommand(new PortalRegisterRequest { BusinessId = "1234567-8", Email = "a@b.com", Password = "P@ss1" }), default);

        Assert.False(result.Success);
        Assert.Equal("CompanyNotFound", result.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenCompanyExists_CreatesUserAndReturnsToken()
    {
        var company = new CompanyEntity { BusinessId = "1234567-8", Name = "Acme" };
        var companyRepo = new Mock<ICompanyRepository>();
        companyRepo.Setup(r => r.GetByBusinessIdAsync("1234567-8", It.IsAny<CancellationToken>())).ReturnsAsync(company);

        var regService = new Mock<IUserRegistrationService>();
        regService.Setup(r => r.CreateUserAsync("a@b.com", "P@ss1", It.IsAny<string>(), "1234567-8", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(("user-1", "a@b.com", "User", "1234567-8", "cust-1"));

        var jwtFactory = new Mock<IJwtTokenFactory>();
        jwtFactory.Setup(j => j.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Claim>>()))
            .Returns("jwt-token");

        var handler = new RegisterUserCommandHandler(companyRepo.Object, regService.Object, jwtFactory.Object);
        var result = await handler.Handle(new RegisterUserCommand(new PortalRegisterRequest
        {
            BusinessId = "1234567-8",
            Email = "a@b.com",
            Password = "P@ss1"
        }), default);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("jwt-token", result.Data.JwtToken);
        Assert.Equal("cust-1", result.Data.CustomerMappingId);
    }

    [Fact]
    public async Task Handle_WhenCreateUserThrowsInvalidOperation_ReturnsRegistrationFailedWithMessage()
    {
        var company = new CompanyEntity { BusinessId = "1234567-8", Name = "Acme" };
        var companyRepo = new Mock<ICompanyRepository>();
        companyRepo.Setup(r => r.GetByBusinessIdAsync("1234567-8", It.IsAny<CancellationToken>())).ReturnsAsync(company);

        var regService = new Mock<IUserRegistrationService>();
        regService
            .Setup(r => r.CreateUserAsync("dup@b.com", "P@ss1", It.IsAny<string>(), "1234567-8", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Failed to register user: Email 'dup@b.com' is already taken."));

        var jwtFactory = new Mock<IJwtTokenFactory>();

        var handler = new RegisterUserCommandHandler(companyRepo.Object, regService.Object, jwtFactory.Object);
        var result = await handler.Handle(new RegisterUserCommand(new PortalRegisterRequest
        {
            BusinessId = "1234567-8",
            Email = "dup@b.com",
            Password = "P@ss1",
            UserName = "Dup",
        }), default);

        Assert.False(result.Success);
        Assert.Equal("RegistrationFailed", result.ErrorCode);
        Assert.Equal("Email 'dup@b.com' is already taken.", result.Message);
        jwtFactory.Verify(j => j.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<Claim>>()), Times.Never);
    }
}
