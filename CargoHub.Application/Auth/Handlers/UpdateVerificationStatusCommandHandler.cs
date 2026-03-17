using CargoHub.Application.Auth.Abstractions;
using CargoHub.Application.Auth.Commands;
using CargoHub.Application.Auth.Dtos;
using MediatR;

namespace CargoHub.Application.Auth.Handlers;

public sealed class UpdateVerificationStatusCommandHandler : IRequestHandler<UpdateVerificationStatusCommand, AuthResult>
{
    private readonly IUpdateVerificationStatusRunner _runner;

    public UpdateVerificationStatusCommandHandler(IUpdateVerificationStatusRunner runner)
    {
        _runner = runner;
    }

    public async Task<AuthResult> Handle(UpdateVerificationStatusCommand request, CancellationToken cancellationToken)
    {
        var (success, errorCode, message) = await _runner.RunAsync(request.Request.UserID, request.Request.Verification_status, cancellationToken);
        return new AuthResult
        {
            Success = success,
            Message = message,
            ErrorCode = errorCode
        };
    }
}
