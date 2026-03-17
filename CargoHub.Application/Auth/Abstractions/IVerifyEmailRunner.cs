namespace CargoHub.Application.Auth.Abstractions;

public interface IVerifyEmailRunner
{
    Task<(bool success, string? errorCode, string? message)> RunAsync(string code, CancellationToken cancellationToken = default);
}
