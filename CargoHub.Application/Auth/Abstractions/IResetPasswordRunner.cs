namespace CargoHub.Application.Auth.Abstractions;

public interface IResetPasswordRunner
{
    Task<(bool success, string? errorCode, string? message)> RunAsync(string token, string newPassword, CancellationToken cancellationToken = default);
}
