using CargoHub.Application.FreelanceRiders;
using CargoHub.Domain.FreelanceRiders;
using CargoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Infrastructure.FreelanceRiders;

public sealed class FreelanceRiderInviteRepository : IFreelanceRiderInviteRepository
{
    private readonly ApplicationDbContext _db;

    public FreelanceRiderInviteRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(FreelanceRiderInvite invite, CancellationToken cancellationToken = default)
    {
        _db.FreelanceRiderInvites.Add(invite);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<FreelanceRiderInvite?> GetActiveByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        return await _db.FreelanceRiderInvites
            .Include(i => i.FreelanceRider)
            .Where(i => i.TokenHash == tokenHash && i.ConsumedAt == null && i.ExpiresAt > now)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task MarkConsumedAsync(Guid inviteId, CancellationToken cancellationToken = default)
    {
        var row = await _db.FreelanceRiderInvites.FirstOrDefaultAsync(i => i.Id == inviteId, cancellationToken);
        if (row == null) return;
        row.ConsumedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokePendingForRiderAndEmailAsync(Guid freelanceRiderId, string normalizedEmail, CancellationToken cancellationToken = default)
    {
        var pending = await _db.FreelanceRiderInvites
            .Where(i => i.FreelanceRiderId == freelanceRiderId && i.NormalizedEmail == normalizedEmail && i.ConsumedAt == null)
            .ToListAsync(cancellationToken);
        foreach (var i in pending)
            i.ConsumedAt = DateTimeOffset.UtcNow;
        if (pending.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);
    }
}
