using CargoHub.Application.Billing.Admin;
using CargoHub.Domain.Billing;
using CargoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Infrastructure.Billing;

public sealed class BillingPeriodBookingExclusionMutator : IBillingPeriodBookingExclusionMutator
{
    private readonly ApplicationDbContext _db;
    private readonly IBillingPeriodRegenerationService _regeneration;

    public BillingPeriodBookingExclusionMutator(
        ApplicationDbContext db,
        IBillingPeriodRegenerationService regeneration)
    {
        _db = db;
        _regeneration = regeneration;
    }

    public async Task<BillingPeriodBookingExclusionResult> SetExcludedAsync(
        Guid companyBillingPeriodId,
        Guid bookingId,
        bool excluded,
        string? superAdminUserId,
        CancellationToken cancellationToken = default)
    {
        _ = superAdminUserId;
        var period = await _db.CompanyBillingPeriods
            .FirstOrDefaultAsync(p => p.Id == companyBillingPeriodId, cancellationToken);
        if (period == null)
            return BillingPeriodBookingExclusionResult.Fail("NotFound", "Billing period not found.");

        var monthStart = new DateTime(period.YearUtc, period.MonthUtc, 1, 0, 0, 0, DateTimeKind.Utc);
        var monthEnd = monthStart.AddMonths(1);

        var booking = await _db.Bookings.FirstOrDefaultAsync(
            b => b.Id == bookingId && b.CompanyId == period.CompanyId,
            cancellationToken);
        if (booking == null)
            return BillingPeriodBookingExclusionResult.Fail("BookingNotFound", "Booking not found for this company.");

        if (booking.FirstBillableAtUtc is not { } fb ||
            fb < monthStart ||
            fb >= monthEnd)
            return BillingPeriodBookingExclusionResult.Fail("BookingNotInPeriod", "Booking is not billable in this UTC month.");

        var row = await _db.BillingPeriodExcludedBookings
            .FirstOrDefaultAsync(
                x => x.CompanyBillingPeriodId == companyBillingPeriodId && x.BookingId == bookingId,
                cancellationToken);

        if (excluded)
        {
            if (row == null)
            {
                _db.BillingPeriodExcludedBookings.Add(new BillingPeriodExcludedBooking
                {
                    CompanyBillingPeriodId = companyBillingPeriodId,
                    BookingId = bookingId
                });
                await _db.SaveChangesAsync(cancellationToken);
            }
        }
        else if (row != null)
        {
            _db.BillingPeriodExcludedBookings.Remove(row);
            await _db.SaveChangesAsync(cancellationToken);
        }

        await _regeneration.RegenerateAsync(companyBillingPeriodId, cancellationToken);

        return BillingPeriodBookingExclusionResult.Ok();
    }
}
