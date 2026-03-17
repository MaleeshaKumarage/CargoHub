using HiavaNet.Application.Auth.Abstractions;
using HiavaNet.Application.Auth.Commands;
using HiavaNet.Application.Auth.Dtos;
using MediatR;

namespace HiavaNet.Application.Auth.Handlers;

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
