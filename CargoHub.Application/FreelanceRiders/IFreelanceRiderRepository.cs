using CargoHub.Domain.FreelanceRiders;

namespace CargoHub.Application.FreelanceRiders;

public interface IFreelanceRiderRepository
{
    Task<FreelanceRider?> GetByIdAsync(Guid id, bool includeAreas, CancellationToken cancellationToken = default);

    Task<FreelanceRider?> GetByUserIdAsync(string userId, bool includeAreas, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FreelanceRider>> ListAllAsync(CancellationToken cancellationToken = default);

    Task AddAsync(FreelanceRider rider, CancellationToken cancellationToken = default);

    Task UpdateAsync(FreelanceRider rider, CancellationToken cancellationToken = default);

    /// <summary>Active riders whose service areas contain both postals, filtered by company scope.</summary>
    Task<IReadOnlyList<FreelanceRider>> FindMatchingActiveAsync(
        string shipperPostalNormalized,
        string receiverPostalNormalized,
        Guid? bookingCompanyId,
        CancellationToken cancellationToken = default);
}
