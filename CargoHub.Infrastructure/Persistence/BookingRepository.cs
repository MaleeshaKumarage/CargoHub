using CargoHub.Application.Bookings;
using CargoHub.Application.Bookings.Dtos;
using CargoHub.Domain.Bookings;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Infrastructure.Persistence;

public sealed partial class BookingRepository : IBookingRepository
{
    private readonly ApplicationDbContext _db;

    public BookingRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Booking>> ListByCustomerIdAsync(string customerId, int skip = 0, int take = 100, BookingListFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Bookings
            .AsNoTracking()
            .Include(b => b.Packages)
            .Where(b => b.CustomerId == customerId && !b.IsDraft);
        query = ApplyFilter(query, filter);
        return await query
            .OrderByDescending(b => b.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Booking>> ListAllAsync(int skip = 0, int take = 100, BookingListFilter? filter = null, CancellationToken cancellationToken = default)
    {
        var query = _db.Bookings
            .AsNoTracking()
            .Include(b => b.Packages)
            .Where(b => !b.IsDraft);
        query = ApplyFilter(query, filter);
        return await query
            .OrderByDescending(b => b.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    private static IQueryable<Booking> ApplyFilter(IQueryable<Booking> query, BookingListFilter? filter)
    {
        if (filter == null) return query;
        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var term = filter.Search.Trim().ToLower();
            query = query.Where(b =>
                (b.ShipmentNumber != null && b.ShipmentNumber.ToLower().Contains(term)) ||
                (b.WaybillNumber != null && b.WaybillNumber.ToLower().Contains(term)) ||
                (b.CustomerName != null && b.CustomerName.ToLower().Contains(term)));
        }
        if (filter.CreatedFrom.HasValue)
            query = query.Where(b => b.CreatedAtUtc >= filter.CreatedFrom.Value);
        if (filter.CreatedTo.HasValue)
            query = query.Where(b => b.CreatedAtUtc <= filter.CreatedTo.Value);
        if (filter.Enabled.HasValue)
            query = query.Where(b => b.Enabled == filter.Enabled.Value);
        return query;
    }

    public async Task<List<Booking>> ListDraftsByCustomerIdAsync(string customerId, int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        return await _db.Bookings
            .AsNoTracking()
            .Where(b => b.CustomerId == customerId && b.IsDraft)
            .OrderByDescending(b => b.UpdatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Booking>> ListAllDraftsAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        return await _db.Bookings
            .AsNoTracking()
            .Where(b => b.IsDraft)
            .OrderByDescending(b => b.UpdatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _db.Bookings
            .AsNoTracking()
            .Include(b => b.Packages)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
    }

    public async Task<Booking?> GetByIdWithTrackingAsync(Guid id, string customerId, CancellationToken cancellationToken = default)
    {
        return await _db.Bookings
            .Include(b => b.Packages)
            .FirstOrDefaultAsync(b => b.Id == id && b.CustomerId == customerId, cancellationToken);
    }

    public async Task<Booking> AddAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync(cancellationToken);
        return booking;
    }

    public async Task UpdateAsync(Booking booking, CancellationToken cancellationToken = default)
    {
        booking.UpdatedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ConfirmDraftAsync(Guid id, string customerId, CancellationToken cancellationToken = default)
    {
        var b = await _db.Bookings.FirstOrDefaultAsync(x => x.Id == id && x.CustomerId == customerId && x.IsDraft, cancellationToken);
        if (b == null) return false;
        b.IsDraft = false;
        b.Enabled = true;
        b.UpdatedAtUtc = DateTime.UtcNow;
        await _db.BookingStatusHistory.AddAsync(new BookingStatusHistory
        {
            Id = Guid.NewGuid(),
            BookingId = id,
            Status = BookingStatus.CompletedBooking,
            OccurredAtUtc = DateTime.UtcNow,
            Source = "draft_confirmed"
        }, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task AddStatusEventAsync(Guid bookingId, string status, string? source, CancellationToken cancellationToken = default)
    {
        await _db.BookingStatusHistory.AddAsync(new BookingStatusHistory
        {
            Id = Guid.NewGuid(),
            BookingId = bookingId,
            Status = status,
            OccurredAtUtc = DateTime.UtcNow,
            Source = source
        }, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TryAddStatusEventAsync(Guid bookingId, string status, string? source, CancellationToken cancellationToken = default)
    {
        var exists = await _db.BookingStatusHistory.AnyAsync(x => x.BookingId == bookingId && x.Status == status, cancellationToken);
        if (exists) return false;
        await AddStatusEventAsync(bookingId, status, source, cancellationToken);
        return true;
    }

    public async Task<List<BookingStatusEventDto>> GetStatusHistoryAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        return await _db.BookingStatusHistory
            .AsNoTracking()
            .Where(x => x.BookingId == bookingId)
            .OrderBy(x => x.OccurredAtUtc)
            .Select(x => new BookingStatusEventDto
            {
                Status = x.Status,
                OccurredAtUtc = x.OccurredAtUtc,
                Source = x.Source
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<Dictionary<Guid, List<BookingStatusEventDto>>> GetStatusHistoryForBookingIdsAsync(IEnumerable<Guid> bookingIds, CancellationToken cancellationToken = default)
    {
        var ids = bookingIds.ToList();
        if (ids.Count == 0) return new Dictionary<Guid, List<BookingStatusEventDto>>();
        var list = await _db.BookingStatusHistory
            .AsNoTracking()
            .Where(x => ids.Contains(x.BookingId))
            .OrderBy(x => x.OccurredAtUtc)
            .Select(x => new { x.BookingId, Status = x.Status, x.OccurredAtUtc, x.Source })
            .ToListAsync(cancellationToken);
        var result = ids.ToDictionary(id => id, _ => new List<BookingStatusEventDto>());
        foreach (var row in list)
        {
            result[row.BookingId].Add(new BookingStatusEventDto { Status = row.Status, OccurredAtUtc = row.OccurredAtUtc, Source = row.Source });
        }
        return result;
    }

}
