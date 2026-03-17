using CargoHub.Application.Auth.Abstractions;
using CargoHub.Application.Auth.Commands;
using CargoHub.Application.Auth.Dtos;
using CargoHub.Application.Auth.Handlers;
using Moq;
using Xunit;

namespace CargoHub.Tests.Auth;

public class VerifyEmailCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRunnerSucceeds_ReturnsSuccess()
    {
        var runner = new Mock<IVerifyEmailRunner>();
        runner.Setup(r => r.RunAsync("code123", It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, (string?)null, "Email verified."));

        var handler = new VerifyEmailCommandHandler(runner.Object);
        var result = await handler.Handle(new VerifyEmailCommand(new VerifyRequest { Code = "code123" }), default);

        Assert.True(result.Success);
        Assert.Equal("Email verified.", result.Message);
    }

    [Fact]
    public async Task Handle_WhenRunnerFails_ReturnsFailure()
    {
        var runner = new Mock<IVerifyEmailRunner>();
        runner.Setup(r => r.RunAsync("bad", It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "InvalidCode", "Invalid verification code."));

        var handler = new VerifyEmailCommandHandler(runner.Object);
        var result = await handler.Handle(new VerifyEmailCommand(new VerifyRequest { Code = "bad" }), default);

        Assert.False(result.Success);
        Assert.Equal("InvalidCode", result.ErrorCode);
    }
}
