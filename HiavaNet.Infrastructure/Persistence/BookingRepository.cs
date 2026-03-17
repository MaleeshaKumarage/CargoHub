using HiavaNet.Application.Bookings;
using HiavaNet.Application.Bookings.Dtos;
using HiavaNet.Domain.Bookings;
using Microsoft.EntityFrameworkCore;

namespace HiavaNet.Infrastructure.Persistence;

public sealed class BookingRepository : IBookingRepository
{
    private readonly ApplicationDbContext _db;

    public BookingRepository(ApplicationDbContext db)
    {
        _db = db;
    }

    public async Task<List<Booking>> ListByCustomerIdAsync(string customerId, int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        return await _db.Bookings
            .AsNoTracking()
            .Where(b => b.CustomerId == customerId && !b.IsDraft)
            .OrderByDescending(b => b.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Booking>> ListAllAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default)
    {
        return await _db.Bookings
            .AsNoTracking()
            .Where(b => !b.IsDraft)
            .OrderByDescending(b => b.CreatedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
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

    public async Task<DashboardBookingStatsDto> GetDashboardStatsAsync(string? customerId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var startOfToday = DateOnly.FromDateTime(now).ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var startOfYear = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var baseQuery = string.IsNullOrEmpty(customerId)
            ? _db.Bookings.AsNoTracking().Where(b => !b.IsDraft)
            : _db.Bookings.AsNoTracking().Where(b => b.CustomerId == customerId && !b.IsDraft);

        var countToday = await baseQuery.CountAsync(b => b.CreatedAtUtc >= startOfToday, cancellationToken);
        var countMonth = await baseQuery.CountAsync(b => b.CreatedAtUtc >= startOfMonth, cancellationToken);
        var countYear = await baseQuery.CountAsync(b => b.CreatedAtUtc >= startOfYear, cancellationToken);

        var rows = await baseQuery
            .Select(b => new
            {
                CarrierId = b.Shipment.CarrierId,
                PostalService = b.Header.PostalService,
                PickUpCity = b.PickUpAddress.City,
                ShipperCity = b.Shipper.City,
                DeliveryCity = b.DeliveryPoint.City,
                ReceiverCity = b.Receiver.City
            })
            .ToListAsync(cancellationToken);

        static string Norm(string? s) => string.IsNullOrWhiteSpace(s) ? "(Not set)" : s.Trim();

        var byCourier = rows
            .GroupBy(r => Norm(r.CarrierId ?? r.PostalService))
            .Select(g => new CountByKeyDto { Key = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        var fromCities = rows
            .GroupBy(r => Norm(r.PickUpCity ?? r.ShipperCity))
            .Select(g => new CountByKeyDto { Key = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        var toCities = rows
            .GroupBy(r => Norm(r.DeliveryCity ?? r.ReceiverCity))
            .Select(g => new CountByKeyDto { Key = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .ToList();

        return new DashboardBookingStatsDto
        {
            CountToday = countToday,
            CountMonth = countMonth,
            CountYear = countYear,
            ByCourier = byCourier,
            FromCities = fromCities,
            ToCities = toCities
        };
    }
}
