using CargoHub.Application.Company;
using CargoHub.Domain.Companies;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Infrastructure.Persistence;

public sealed class CompanyAdminInviteRepository : ICompanyAdminInviteRepository
{
    private readonly ApplicationDbContext _db;

    public CompanyAdminInviteRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task AddAsync(CompanyAdminInvite invite, CancellationToken cancellationToken = default)
    {
        _db.CompanyAdminInvites.Add(invite);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<CompanyAdminInvite?> GetActiveByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        return _db.CompanyAdminInvites
            .Include(i => i.Company)
            .FirstOrDefaultAsync(
                i => i.TokenHash == tokenHash && i.ConsumedAt == null && i.ExpiresAt > now,
                cancellationToken);
    }

    public async Task MarkConsumedAsync(Guid inviteId, CancellationToken cancellationToken = default)
    {
        var row = await _db.CompanyAdminInvites.FirstOrDefaultAsync(i => i.Id == inviteId, cancellationToken);
        if (row == null) return;
        row.ConsumedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokePendingForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var pending = await _db.CompanyAdminInvites
            .Where(i => i.CompanyId == companyId && i.ConsumedAt == null)
            .ToListAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        foreach (var i in pending)
            i.ConsumedAt = now;
        if (pending.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokePendingForCompanyAndEmailAsync(Guid companyId, string normalizedEmail, CancellationToken cancellationToken = default)
    {
        var norm = normalizedEmail.Trim().ToUpperInvariant();
        var pending = await _db.CompanyAdminInvites
            .Where(i => i.CompanyId == companyId && i.ConsumedAt == null && i.NormalizedEmail == norm)
            .ToListAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        foreach (var i in pending)
            i.ConsumedAt = now;
        if (pending.Count > 0)
            await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<int> CountPendingValidInvitesAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        return _db.CompanyAdminInvites
            .CountAsync(
                i => i.CompanyId == companyId && i.ConsumedAt == null && i.ExpiresAt > now,
                cancellationToken);
    }

    public async Task<DateTimeOffset?> GetLastInviteCreatedAtAsync(Guid companyId, CancellationToken cancellationToken = default)
    {
        if (!await _db.CompanyAdminInvites.AnyAsync(i => i.CompanyId == companyId, cancellationToken))
            return null;
        return await _db.CompanyAdminInvites
            .Where(i => i.CompanyId == companyId)
            .MaxAsync(i => i.CreatedAt, cancellationToken);
    }
}
