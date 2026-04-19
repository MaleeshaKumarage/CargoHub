using CargoHub.Domain.FreelanceRiders;

namespace CargoHub.Application.FreelanceRiders;

public interface IFreelanceRiderInviteRepository
{
    Task AddAsync(FreelanceRiderInvite invite, CancellationToken cancellationToken = default);

    Task<FreelanceRiderInvite?> GetActiveByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    Task MarkConsumedAsync(Guid inviteId, CancellationToken cancellationToken = default);

    Task RevokePendingForRiderAndEmailAsync(Guid freelanceRiderId, string normalizedEmail, CancellationToken cancellationToken = default);
}
