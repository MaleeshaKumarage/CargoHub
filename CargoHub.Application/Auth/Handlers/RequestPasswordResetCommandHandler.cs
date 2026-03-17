using CargoHub.Application.Auth.Abstractions;
using CargoHub.Application.Auth.Commands;
using CargoHub.Application.Auth.Dtos;
using MediatR;

namespace CargoHub.Application.Auth.Handlers;

public sealed class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, AuthResult>
{
    private readonly IRequestPasswordResetRunner _runner;

    public RequestPasswordResetCommandHandler(IRequestPasswordResetRunner runner)
    {
        _runner = runner;
    }

    public async Task<AuthResult> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var (success, errorCode, message) = await _runner.RunAsync(request.Request.Email, request.Environment, cancellationToken);
        return new AuthResult
        {
            Success = success,
            Message = message,
            ErrorCode = errorCode
        };
    }
}
