using CargoHub.Application.Auth.Abstractions;
using CargoHub.Application.Auth.Commands;
using CargoHub.Application.Auth.Dtos;
using CargoHub.Application.Auth.Handlers;
using Moq;
using Xunit;

namespace CargoHub.Tests.Auth;

public class RequestPasswordResetCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRunnerSucceeds_ReturnsSuccess()
    {
        var runner = new Mock<IRequestPasswordResetRunner>();
        runner.Setup(r => r.RunAsync("user@test.com", "dev", It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, (string?)null, "Reset email sent."));

        var handler = new RequestPasswordResetCommandHandler(runner.Object);
        var result = await handler.Handle(new RequestPasswordResetCommand(new RequestPasswordResetRequest { Email = "user@test.com" }, "dev"), default);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Handle_WhenRunnerFails_ReturnsFailure()
    {
        var runner = new Mock<IRequestPasswordResetRunner>();
        runner.Setup(r => r.RunAsync("unknown@test.com", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "NotFound", "User not found."));

        var handler = new RequestPasswordResetCommandHandler(runner.Object);
        var result = await handler.Handle(new RequestPasswordResetCommand(new RequestPasswordResetRequest { Email = "unknown@test.com" }, null), default);

        Assert.False(result.Success);
        Assert.Equal("NotFound", result.ErrorCode);
    }
}
