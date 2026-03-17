using CargoHub.Application.Auth.Abstractions;
using CargoHub.Application.Auth.Commands;
using CargoHub.Application.Auth.Dtos;
using MediatR;

namespace CargoHub.Application.Auth.Handlers;

public sealed class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, AuthResult>
{
    private readonly IResetPasswordRunner _runner;

    public ResetPasswordCommandHandler(IResetPasswordRunner runner)
    {
        _runner = runner;
    }

    public async Task<AuthResult> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var (success, errorCode, message) = await _runner.RunAsync(request.Request.Token, request.Request.NewPassword, cancellationToken);
        return new AuthResult
        {
            Success = success,
            Message = message,
            ErrorCode = errorCode
        };
    }
}
