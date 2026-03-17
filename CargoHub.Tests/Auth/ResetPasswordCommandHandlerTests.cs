using CargoHub.Application.Auth.Abstractions;
using CargoHub.Application.Auth.Commands;
using CargoHub.Application.Auth.Dtos;
using CargoHub.Application.Auth.Handlers;
using Moq;
using Xunit;

namespace CargoHub.Tests.Auth;

public class ResetPasswordCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRunnerSucceeds_ReturnsSuccess()
    {
        var runner = new Mock<IResetPasswordRunner>();
        runner.Setup(r => r.RunAsync("token123", "NewP@ss1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, (string?)null, "Password reset."));

        var handler = new ResetPasswordCommandHandler(runner.Object);
        var result = await handler.Handle(new ResetPasswordCommand(new ResetPasswordRequest { Token = "token123", NewPassword = "NewP@ss1" }), default);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Handle_WhenRunnerFails_ReturnsFailure()
    {
        var runner = new Mock<IResetPasswordRunner>();
        runner.Setup(r => r.RunAsync("expired", "NewP@ss1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "InvalidToken", "Token expired or invalid."));

        var handler = new ResetPasswordCommandHandler(runner.Object);
        var result = await handler.Handle(new ResetPasswordCommand(new ResetPasswordRequest { Token = "expired", NewPassword = "NewP@ss1" }), default);

        Assert.False(result.Success);
        Assert.Equal("InvalidToken", result.ErrorCode);
    }
}
