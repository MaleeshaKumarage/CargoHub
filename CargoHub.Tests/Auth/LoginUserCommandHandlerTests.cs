using System.Security.Claims;
using CargoHub.Application.Auth;
using CargoHub.Application.Auth.Abstractions;
using CargoHub.Application.Auth.Commands;
using CargoHub.Application.Auth.Dtos;
using CargoHub.Application.Auth.Handlers;
using Moq;
using Xunit;

namespace CargoHub.Tests.Auth;

public class LoginUserCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenCredentialsInvalid_ReturnsInvalidCredentials()
    {
        var authService = new Mock<IUserAuthenticationService>();
        authService.Setup(a => a.ValidateCredentialsAsync("user@test.com", "wrong", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthenticationResult.Failed());

        var jwtFactory = new Mock<IJwtTokenFactory>();

        var handler = new LoginUserCommandHandler(authService.Object, jwtFactory.Object);
        var result = await handler.Handle(new LoginUserCommand(new PortalLoginRequest { Account = "user@test.com", Password = "wrong" }), default);

        Assert.False(result.Success);
        Assert.Equal("InvalidCredentials", result.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhenCredentialsInvalidWithCompanyInactive_ReturnsCompanyInactive()
    {
        var authService = new Mock<IUserAuthenticationService>();
        authService.Setup(a => a.ValidateCredentialsAsync("user@test.com", "wrong", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthenticationResult.Failed("CompanyInactive", AuthMessages.CompanyInactive));

        var jwtFactory = new Mock<IJwtTokenFactory>();

        var handler = new LoginUserCommandHandler(authService.Object, jwtFactory.Object);
        var result = await handler.Handle(new LoginUserCommand(new PortalLoginRequest { Account = "user@test.com", Password = "wrong" }), default);

        Assert.False(result.Success);
        Assert.Equal("CompanyInactive", result.ErrorCode);
        Assert.Equal(AuthMessages.CompanyInactive, result.Message);
        jwtFactory.Verify(j => j.CreateToken(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<Claim[]>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCredentialsValid_ReturnsToken()
    {
        var authService = new Mock<IUserAuthenticationService>();
        authService.Setup(a => a.ValidateCredentialsAsync("user@test.com", "P@ss1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AuthenticationResult.Succeeded("user-1", "user@test.com", "User", "1234567-8", "cust-1", new List<string> { "User" }));

        var jwtFactory = new Mock<IJwtTokenFactory>();
        jwtFactory.Setup(j => j.CreateToken("user-1", "user@test.com", It.IsAny<Claim[]>()))
            .Returns("jwt-token");

        var handler = new LoginUserCommandHandler(authService.Object, jwtFactory.Object);
        var result = await handler.Handle(new LoginUserCommand(new PortalLoginRequest { Account = "user@test.com", Password = "P@ss1" }), default);

        Assert.True(result.Success);
        Assert.NotNull(result.Data);
        Assert.Equal("jwt-token", result.Data.JwtToken);
        Assert.Equal("cust-1", result.Data.CustomerMappingId);
    }
}
