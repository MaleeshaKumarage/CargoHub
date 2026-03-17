using CargoHub.Application.Auth.Abstractions;
using CargoHub.Application.Auth.Commands;
using CargoHub.Application.Auth.Dtos;
using CargoHub.Application.Auth.Handlers;
using Moq;
using Xunit;

namespace CargoHub.Tests.Auth;

public class UpdateVerificationStatusCommandHandlerTests
{
    [Fact]
    public async Task Handle_WhenRunnerSucceeds_ReturnsSuccess()
    {
        var runner = new Mock<IUpdateVerificationStatusRunner>();
        runner.Setup(r => r.RunAsync("user-1", "verified", It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, (string?)null, "Status updated."));

        var handler = new UpdateVerificationStatusCommandHandler(runner.Object);
        var result = await handler.Handle(new UpdateVerificationStatusCommand(new UpdateVerificationStatusRequest { UserID = "user-1", Verification_status = "verified" }), default);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task Handle_WhenRunnerFails_ReturnsFailure()
    {
        var runner = new Mock<IUpdateVerificationStatusRunner>();
        runner.Setup(r => r.RunAsync("unknown", "verified", It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, "NotFound", "User not found."));

        var handler = new UpdateVerificationStatusCommandHandler(runner.Object);
        var result = await handler.Handle(new UpdateVerificationStatusCommand(new UpdateVerificationStatusRequest { UserID = "unknown", Verification_status = "verified" }), default);

        Assert.False(result.Success);
        Assert.Equal("NotFound", result.ErrorCode);
    }
}
