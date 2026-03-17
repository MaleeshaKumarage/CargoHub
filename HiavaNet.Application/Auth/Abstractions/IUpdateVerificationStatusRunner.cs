namespace HiavaNet.Application.Auth.Abstractions;

public interface IUpdateVerificationStatusRunner
{
    Task<(bool success, string? errorCode, string? message)> RunAsync(string userId, string verificationStatus, CancellationToken cancellationToken = default);
}
