using System.Security.Claims;
using CargoHub.Application.Auth;
using CargoHub.Application.Bookings.Dtos;
using CargoHub.Application.Bookings.Queries;
using CargoHub.Application.FreelanceRiders;
using CargoHub.Domain.FreelanceRiders;
using CargoHub.Infrastructure.Identity;
using CargoHub.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Api.Controllers;

/// <summary>Freelance rider portal APIs (deliveries, profile, accept).</summary>
[ApiController]
[Route("api/v1/portal/rider")]
[Authorize(Roles = RoleNames.Rider)]
public class PortalRiderController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IFreelanceRiderRepository _riders;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMediator _mediator;

    public PortalRiderController(
        ApplicationDbContext db,
        IFreelanceRiderRepository riders,
        UserManager<ApplicationUser> userManager,
        IMediator mediator)
    {
        _db = db;
        _riders = riders;
        _userManager = userManager;
        _mediator = mediator;
    }

    private string? UserId => User.FindFirstValue(ClaimTypes.NameIdentifier);

    [HttpGet("me")]
    public async Task<ActionResult<object>> GetMe(CancellationToken cancellationToken)
    {
        var uid = UserId;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();
        var rider = await _riders.GetByUserIdAsync(uid, true, cancellationToken);
        if (rider == null)
            return NotFound();
        return Ok(new
        {
            id = rider.Id,
            businessId = rider.BusinessId,
            displayName = rider.DisplayName,
            phone = rider.Phone,
            email = rider.Email,
            postalCodes = rider.ServiceAreas.Select(a => a.PostalCodeNormalized).ToList(),
        });
    }

    public sealed class PatchRiderMeBody
    {
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public List<string>? PostalCodes { get; set; }
    }

    [HttpPatch("me")]
    public async Task<ActionResult> PatchMe([FromBody] PatchRiderMeBody body, CancellationToken cancellationToken)
    {
        var uid = UserId;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();
        var rider = await _db.FreelanceRiders.Include(r => r.ServiceAreas)
            .FirstOrDefaultAsync(r => r.ApplicationUserId == uid, cancellationToken);
        if (rider == null) return NotFound();

        if (body.Phone != null) rider.Phone = body.Phone.Trim();
        if (body.Email != null)
        {
            rider.Email = body.Email.Trim();
            rider.NormalizedEmail = rider.Email.ToUpperInvariant();
            var identity = await _userManager.FindByIdAsync(uid);
            if (identity != null)
            {
                identity.Email = rider.Email;
                identity.UserName = rider.Email;
                await _userManager.UpdateAsync(identity);
            }
        }

        if (body.PostalCodes != null)
        {
            _db.FreelanceRiderServiceAreas.RemoveRange(rider.ServiceAreas);
            rider.ServiceAreas.Clear();
            foreach (var pc in body.PostalCodes)
            {
                var n = RiderPostalNormalizer.Normalize(pc);
                if (string.IsNullOrEmpty(n)) continue;
                rider.ServiceAreas.Add(new FreelanceRiderServiceArea
                {
                    Id = Guid.NewGuid(),
                    FreelanceRiderId = rider.Id,
                    PostalCodeNormalized = n
                });
            }
        }

        rider.UpdatedAtUtc = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("bookings")]
    public async Task<ActionResult<List<BookingDetailDto>>> ListBookings(CancellationToken cancellationToken)
    {
        var uid = UserId;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();
        var rider = await _riders.GetByUserIdAsync(uid, false, cancellationToken);
        if (rider == null) return NotFound();

        var now = DateTime.UtcNow;
        var ids = await _db.Bookings.AsNoTracking()
            .Where(b =>
                !b.IsDraft &&
                (
                    (b.FreelanceRiderId == rider.Id && b.FreelanceRiderAcceptedAtUtc != null) ||
                    (b.FreelanceRiderId == rider.Id &&
                     b.FreelanceRiderAcceptedAtUtc == null &&
                     b.FreelanceRiderAssignmentDeadlineUtc != null &&
                     b.FreelanceRiderAssignmentDeadlineUtc >= now)
                ))
            .OrderByDescending(b => b.UpdatedAtUtc)
            .Select(b => b.Id)
            .ToListAsync(cancellationToken);

        var details = new List<BookingDetailDto>();
        foreach (var id in ids)
        {
            var d = await _mediator.Send(new GetBookingByIdQuery(id, null), cancellationToken);
            if (d != null) details.Add(d);
        }
        return Ok(details);
    }

    [HttpGet("bookings/{id:guid}")]
    public async Task<ActionResult<BookingDetailDto>> GetBooking(Guid id, CancellationToken cancellationToken)
    {
        var uid = UserId;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();
        var rider = await _riders.GetByUserIdAsync(uid, false, cancellationToken);
        if (rider == null) return NotFound();

        var booking = await _mediator.Send(new GetBookingByIdQuery(id, null), cancellationToken);
        if (booking == null || booking.FreelanceRiderId != rider.Id)
            return NotFound();
        return Ok(booking);
    }

    [HttpPost("bookings/{id:guid}/accept")]
    public async Task<ActionResult> AcceptBooking(Guid id, CancellationToken cancellationToken)
    {
        var uid = UserId;
        if (string.IsNullOrEmpty(uid)) return Unauthorized();
        var rider = await _riders.GetByUserIdAsync(uid, false, cancellationToken);
        if (rider == null) return NotFound();

        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == id && !b.IsDraft, cancellationToken);
        if (booking == null || booking.FreelanceRiderId != rider.Id)
            return NotFound();
        var now = DateTime.UtcNow;
        if (booking.FreelanceRiderAcceptedAtUtc != null)
            return Ok();
        if (booking.FreelanceRiderAssignmentDeadlineUtc == null || booking.FreelanceRiderAssignmentDeadlineUtc < now)
            return BadRequest(new { message = "Assignment is no longer pending or has expired." });

        booking.FreelanceRiderAcceptedAtUtc = now;
        booking.UpdatedAtUtc = now;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok();
    }
}
