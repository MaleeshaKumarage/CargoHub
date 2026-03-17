namespace HiavaNet.Application.Auth.Abstractions;

public interface IRequestPasswordResetRunner
{
    Task<(bool success, string? errorCode, string? message)> RunAsync(string email, string? env, CancellationToken cancellationToken = default);
}
