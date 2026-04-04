using CargoHub.Application.Auth.Dtos;

namespace CargoHub.Application.Auth.Abstractions;

/// <summary>
/// Completes company admin invite (new user or existing user + password check).
/// </summary>
public interface IAcceptCompanyAdminInviteRunner
{
    Task<RegisterResult> RunAsync(string rawToken, string email, string password, string userName, CancellationToken cancellationToken = default);
}
