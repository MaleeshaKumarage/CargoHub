using HiavaNet.Application.Bookings.Dtos;
using BookingEntity = HiavaNet.Domain.Bookings.Booking;

namespace HiavaNet.Application.Bookings;

/// <summary>
/// Booking persistence. Implemented in Infrastructure.
/// </summary>
public interface IBookingRepository
{
    /// <summary>List completed bookings only (IsDraft == false).</summary>
    Task<List<BookingEntity>> ListByCustomerIdAsync(string customerId, int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    /// <summary>List all completed bookings (all customers). Used by SuperAdmin.</summary>
    Task<List<BookingEntity>> ListAllAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    /// <summary>List draft bookings (IsDraft == true).</summary>
    Task<List<BookingEntity>> ListDraftsByCustomerIdAsync(string customerId, int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    /// <summary>List all draft bookings (all customers). Used by SuperAdmin.</summary>
    Task<List<BookingEntity>> ListAllDraftsAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default);
    Task<BookingEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    /// <summary>Get by id with tracking for update/confirm. Returns null if not found or not owned by customer.</summary>
    Task<BookingEntity?> GetByIdWithTrackingAsync(Guid id, string customerId, CancellationToken cancellationToken = default);
    Task<BookingEntity> AddAsync(BookingEntity booking, CancellationToken cancellationToken = default);
    Task UpdateAsync(BookingEntity booking, CancellationToken cancellationToken = default);
    /// <summary>Marks draft as completed (IsDraft = false, Enabled = true). Returns true if updated.</summary>
    Task<bool> ConfirmDraftAsync(Guid id, string customerId, CancellationToken cancellationToken = default);
    /// <summary>Record a status event (Draft, Waybill, etc.) for tracking.</summary>
    Task AddStatusEventAsync(Guid bookingId, string status, string? source, CancellationToken cancellationToken = default);
    /// <summary>Add status event only if this status is not already recorded for the booking (idempotent). Returns true if added.</summary>
    Task<bool> TryAddStatusEventAsync(Guid bookingId, string status, string? source, CancellationToken cancellationToken = default);
    /// <summary>Get status history for a booking, ordered by OccurredAtUtc ascending.</summary>
    Task<List<BookingStatusEventDto>> GetStatusHistoryAsync(Guid bookingId, CancellationToken cancellationToken = default);
    /// <summary>Get status history for multiple bookings in one query. Keys are booking IDs; values are ordered by OccurredAtUtc.</summary>
    Task<Dictionary<Guid, List<BookingStatusEventDto>>> GetStatusHistoryForBookingIdsAsync(IEnumerable<Guid> bookingIds, CancellationToken cancellationToken = default);
    /// <summary>When customerId is null, returns stats for all bookings (Super Admin). Otherwise for that customer only.</summary>
    Task<DashboardBookingStatsDto> GetDashboardStatsAsync(string? customerId, CancellationToken cancellationToken = default);
}
