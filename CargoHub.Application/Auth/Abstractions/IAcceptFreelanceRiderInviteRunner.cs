using CargoHub.Application.Auth.Dtos;

namespace CargoHub.Application.Auth.Abstractions;

public interface IAcceptFreelanceRiderInviteRunner
{
    Task<RegisterResult> RunAsync(string rawToken, string password, string userName, CancellationToken cancellationToken = default);
}
