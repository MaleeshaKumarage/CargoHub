using CargoHub.Application.FreelanceRiders;
using CargoHub.Domain.FreelanceRiders;
using CargoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Infrastructure.FreelanceRiders;

public sealed class FreelanceRiderRepository : IFreelanceRiderRepository
{
    private readonly ApplicationDbContext _db;

    public FreelanceRiderRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<FreelanceRider?> GetByIdAsync(Guid id, bool includeAreas, CancellationToken cancellationToken = default)
    {
        var q = _db.FreelanceRiders.AsQueryable();
        if (includeAreas)
            q = q.Include(r => r.ServiceAreas);
        return await q.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
    }

    public async Task<FreelanceRider?> GetByUserIdAsync(string userId, bool includeAreas, CancellationToken cancellationToken = default)
    {
        var q = _db.FreelanceRiders.AsQueryable();
        if (includeAreas)
            q = q.Include(r => r.ServiceAreas);
        return await q.FirstOrDefaultAsync(r => r.ApplicationUserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<FreelanceRider>> ListAllAsync(CancellationToken cancellationToken = default)
    {
        return await _db.FreelanceRiders
            .Include(r => r.ServiceAreas)
            .OrderBy(r => r.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(FreelanceRider rider, CancellationToken cancellationToken = default)
    {
        _db.FreelanceRiders.Add(rider);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(FreelanceRider rider, CancellationToken cancellationToken = default)
    {
        _db.FreelanceRiders.Update(rider);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FreelanceRider>> FindMatchingActiveAsync(
        string shipperPostalNormalized,
        string receiverPostalNormalized,
        Guid? bookingCompanyId,
        CancellationToken cancellationToken = default)
    {
        var ship = RiderPostalNormalizer.Normalize(shipperPostalNormalized);
        var recv = RiderPostalNormalizer.Normalize(receiverPostalNormalized);
        if (string.IsNullOrEmpty(ship) || string.IsNullOrEmpty(recv))
            return Array.Empty<FreelanceRider>();

        return await _db.FreelanceRiders
            .AsNoTracking()
            .Include(r => r.ServiceAreas)
            .Where(r => r.Status == FreelanceRiderStatus.Active)
            .Where(r => r.CompanyId == null || (bookingCompanyId.HasValue && r.CompanyId == bookingCompanyId))
            .Where(r => r.ServiceAreas.Any(a => a.PostalCodeNormalized == ship))
            .Where(r => r.ServiceAreas.Any(a => a.PostalCodeNormalized == recv))
            .OrderBy(r => r.DisplayName)
            .ToListAsync(cancellationToken);
    }
}
