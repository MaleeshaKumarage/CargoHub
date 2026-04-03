namespace CargoHub.Application.Bookings;

/// <summary>Idempotent claim so the same company/day/timezone is not processed twice.</summary>
public interface IDailyDigestSendLogRepository
{
    /// <summary>Returns true if this process claimed the slot (insert succeeded); false if already claimed.</summary>
    Task<bool> TryClaimAsync(Guid companyId, DateOnly digestDateLocal, string timeZoneId, CancellationToken cancellationToken = default);
}
