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
    private static RegisterUserCommandHandler CreateHandler(
        ICompanyRepository companyRepo,
        IUserRegistrationService regService,
        IJwtTokenFactory jwtFactory,
        ICompanyUserMetrics? metrics = null)
    {
        if (metrics != null)
            return new RegisterUserCommandHandler(companyRepo, regService, jwtFactory, metrics);
        var mm = new Mock<ICompanyUserMetrics>();
        mm.Setup(x => x.CountActiveUsersForBusinessIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        return new RegisterUserCommandHandler(companyRepo, regService, jwtFactory, mm.Object);
    }

    [Fact]
    public async Task Handle_WhenBusinessIdWhitespace_ReturnsCompanyIdRequired()
    {
        var companyRepo = new Mock<ICompanyRepository>();
        var regService = new Mock<IUserRegistrationService>();
        var jwtFactory = new Mock<IJwtTokenFactory>();

        var handler = CreateHandler(companyRepo.Object, regService.Object, jwtFactory.Object);
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

        var handler = CreateHandler(companyRepo.Object, regService.Object, jwtFactory.Object);
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

        var handler = CreateHandler(companyRepo.Object, regService.Object, jwtFactory.Object);
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

        var handler = CreateHandler(companyRepo.Object, regService.Object, jwtFactory.Object);
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

        var handler = CreateHandler(companyRepo.Object, regService.Object, jwtFactory.Object);
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

    [Fact]
    public async Task Handle_WhenCreateUserThrowsInvalidOperation_WithoutKnownPrefix_ReturnsMessageAsIs()
    {
        var company = new CompanyEntity { BusinessId = "1234567-8", Name = "Acme" };
        var companyRepo = new Mock<ICompanyRepository>();
        companyRepo.Setup(r => r.GetByBusinessIdAsync("1234567-8", It.IsAny<CancellationToken>())).ReturnsAsync(company);

        var regService = new Mock<IUserRegistrationService>();
        regService
            .Setup(r => r.CreateUserAsync("x@b.com", "P@ss1", It.IsAny<string>(), "1234567-8", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Password does not meet requirements."));

        var jwtFactory = new Mock<IJwtTokenFactory>();

        var handler = CreateHandler(companyRepo.Object, regService.Object, jwtFactory.Object);
        var result = await handler.Handle(new RegisterUserCommand(new PortalRegisterRequest
        {
            BusinessId = "1234567-8",
            Email = "x@b.com",
            Password = "P@ss1",
            UserName = "X",
        }), default);

        Assert.False(result.Success);
        Assert.Equal("Password does not meet requirements.", result.Message);
    }

    [Fact]
    public async Task Handle_WhenCreateUserThrowsInvalidOperation_WhitespaceMessage_ReturnsRegistrationFailed()
    {
        var company = new CompanyEntity { BusinessId = "1234567-8", Name = "Acme" };
        var companyRepo = new Mock<ICompanyRepository>();
        companyRepo.Setup(r => r.GetByBusinessIdAsync("1234567-8", It.IsAny<CancellationToken>())).ReturnsAsync(company);

        var regService = new Mock<IUserRegistrationService>();
        regService
            .Setup(r => r.CreateUserAsync("y@b.com", "P@ss1", It.IsAny<string>(), "1234567-8", It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("   "));

        var handler = CreateHandler(companyRepo.Object, regService.Object, new Mock<IJwtTokenFactory>().Object);
        var result = await handler.Handle(new RegisterUserCommand(new PortalRegisterRequest
        {
            BusinessId = "1234567-8",
            Email = "y@b.com",
            Password = "P@ss1",
            UserName = "Y",
        }), default);

        Assert.False(result.Success);
        Assert.Equal("Registration failed.", result.Message);
    }

    [Fact]
    public async Task Handle_WhenUserCapReached_ReturnsCompanyUserLimitReached()
    {
        var company = new CompanyEntity { BusinessId = "1234567-8", Name = "Acme", MaxUserAccounts = 5 };
        var companyRepo = new Mock<ICompanyRepository>();
        companyRepo.Setup(r => r.GetByBusinessIdAsync("1234567-8", It.IsAny<CancellationToken>())).ReturnsAsync(company);

        var metrics = new Mock<ICompanyUserMetrics>();
        metrics.Setup(m => m.CountActiveUsersForBusinessIdAsync("1234567-8", It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        var handler = CreateHandler(companyRepo.Object, new Mock<IUserRegistrationService>().Object, new Mock<IJwtTokenFactory>().Object, metrics.Object);
        var result = await handler.Handle(new RegisterUserCommand(new PortalRegisterRequest
        {
            BusinessId = "1234567-8",
            Email = "new@b.com",
            Password = "P@ss1",
            UserName = "N",
        }), default);

        Assert.False(result.Success);
        Assert.Equal("CompanyUserLimitReached", result.ErrorCode);
    }
}
