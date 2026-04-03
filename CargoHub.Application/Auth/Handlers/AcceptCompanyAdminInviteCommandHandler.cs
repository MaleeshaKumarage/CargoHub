using CargoHub.Application.Auth.Abstractions;
using CargoHub.Application.Auth.Commands;
using CargoHub.Application.Auth.Dtos;
using MediatR;

namespace CargoHub.Application.Auth.Handlers;

public sealed class AcceptCompanyAdminInviteCommandHandler : IRequestHandler<AcceptCompanyAdminInviteCommand, RegisterResult>
{
    private readonly IAcceptCompanyAdminInviteRunner _runner;

    public AcceptCompanyAdminInviteCommandHandler(IAcceptCompanyAdminInviteRunner runner)
    {
        _runner = runner;
    }

    public Task<RegisterResult> Handle(AcceptCompanyAdminInviteCommand request, CancellationToken cancellationToken)
    {
        var body = request.Request;
        return _runner.RunAsync(body.Token, body.Password, body.UserName, cancellationToken);
    }
}
