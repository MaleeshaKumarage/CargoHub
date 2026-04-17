using CargoHub.Application.Auth.Abstractions;
using CargoHub.Application.Auth.Commands;
using CargoHub.Application.Auth.Dtos;
using MediatR;

namespace CargoHub.Application.Auth.Handlers;

public sealed class AcceptFreelanceRiderInviteCommandHandler : IRequestHandler<AcceptFreelanceRiderInviteCommand, RegisterResult>
{
    private readonly IAcceptFreelanceRiderInviteRunner _runner;

    public AcceptFreelanceRiderInviteCommandHandler(IAcceptFreelanceRiderInviteRunner runner)
    {
        _runner = runner;
    }

    public Task<RegisterResult> Handle(AcceptFreelanceRiderInviteCommand request, CancellationToken cancellationToken)
    {
        var r = request.Request;
        return _runner.RunAsync(r.Token, r.Password, r.UserName, cancellationToken);
    }
}
