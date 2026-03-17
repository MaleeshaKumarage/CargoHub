using HiavaNet.Application.Auth.Abstractions;
using HiavaNet.Application.Auth.Commands;
using HiavaNet.Application.Auth.Dtos;
using MediatR;

namespace HiavaNet.Application.Auth.Handlers;

public sealed class VerifyEmailCommandHandler : IRequestHandler<VerifyEmailCommand, AuthResult>
{
    private readonly IVerifyEmailRunner _runner;

    public VerifyEmailCommandHandler(IVerifyEmailRunner runner)
    {
        _runner = runner;
    }

    public async Task<AuthResult> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var (success, errorCode, message) = await _runner.RunAsync(request.Request.Code, cancellationToken);
        return new AuthResult
        {
            Success = success,
            Message = message,
            ErrorCode = errorCode
        };
    }
}
