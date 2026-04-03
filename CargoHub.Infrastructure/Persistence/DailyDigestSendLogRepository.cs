using CargoHub.Application.Bookings;
using CargoHub.Domain.Bookings;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CargoHub.Infrastructure.Persistence;

public sealed class DailyDigestSendLogRepository : IDailyDigestSendLogRepository
{
    private readonly ApplicationDbContext _db;

    public DailyDigestSendLogRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<bool> TryClaimAsync(Guid companyId, DateOnly digestDateLocal, string timeZoneId, CancellationToken cancellationToken = default)
    {
        var tz = (timeZoneId ?? "").Trim();
        if (tz.Length > 128)
            tz = tz[..128];

        var exists = await _db.DailyDigestSendLogs.AnyAsync(
            x => x.CompanyId == companyId && x.DigestDateLocal == digestDateLocal && x.TimeZoneId == tz,
            cancellationToken);
        if (exists)
            return false;

        var row = new DailyDigestSendLog
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            DigestDateLocal = digestDateLocal,
            TimeZoneId = tz,
            SentAtUtc = DateTime.UtcNow
        };
        _db.DailyDigestSendLogs.Add(row);
        try
        {
            await _db.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            return false;
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
    {
        if (ex.InnerException is PostgresException pg && pg.SqlState == "23505")
            return true;
        var msg = ex.InnerException?.Message ?? ex.Message;
        return msg.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase)
               || msg.Contains("duplicate", StringComparison.OrdinalIgnoreCase);
    }
}
