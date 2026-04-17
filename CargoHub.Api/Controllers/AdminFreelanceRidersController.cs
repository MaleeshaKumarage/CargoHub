using CargoHub.Application.Auth;
using CargoHub.Application.FreelanceRiders;
using CargoHub.Domain.FreelanceRiders;
using CargoHub.Infrastructure.FreelanceRiders;
using CargoHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Api.Controllers;

[ApiController]
[Route("api/v1/admin/freelance-riders")]
[Authorize(Roles = RoleNames.SuperAdmin)]
public class AdminFreelanceRidersController : ControllerBase
{
    private readonly IFreelanceRiderRepository _riders;
    private readonly ApplicationDbContext _db;
    private readonly FreelanceRiderInviteIssuer _inviteIssuer;

    public AdminFreelanceRidersController(
        IFreelanceRiderRepository riders,
        ApplicationDbContext db,
        FreelanceRiderInviteIssuer inviteIssuer)
    {
        _riders = riders;
        _db = db;
        _inviteIssuer = inviteIssuer;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<object>>> List(CancellationToken cancellationToken)
    {
        var list = await _riders.ListAllAsync(cancellationToken);
        var companies = await _db.Companies.AsNoTracking()
            .Where(c => list.Select(r => r.CompanyId).OfType<Guid>().Distinct().Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name ?? c.BusinessId ?? c.CompanyId, cancellationToken);

        var result = list.Select(r => new
        {
            id = r.Id,
            businessId = r.BusinessId,
            displayName = r.DisplayName,
            phone = r.Phone,
            email = r.Email,
            status = r.Status.ToString(),
            companyId = r.CompanyId,
            companyLabel = r.CompanyId.HasValue && companies.TryGetValue(r.CompanyId.Value, out var n) ? n : (string?)null,
            applicationUserId = r.ApplicationUserId,
            postalCodes = r.ServiceAreas.Select(a => a.PostalCodeNormalized).ToList()
        });
        return Ok(result);
    }

    public sealed class CreateFreelanceRiderBody
    {
        public string BusinessId { get; set; } = "";
        public string DisplayName { get; set; } = "";
        public string Phone { get; set; } = "";
        public string Email { get; set; } = "";
        public Guid? CompanyId { get; set; }
        public List<string>? PostalCodes { get; set; }
        public bool SendInvite { get; set; }
    }

    [HttpPost]
    public async Task<ActionResult<object>> Create([FromBody] CreateFreelanceRiderBody body, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(body.BusinessId) || string.IsNullOrWhiteSpace(body.Email))
            return BadRequest(new { message = "businessId and email are required." });

        var now = DateTimeOffset.UtcNow;
        var rider = new FreelanceRider
        {
            Id = Guid.NewGuid(),
            BusinessId = body.BusinessId.Trim(),
            DisplayName = (body.DisplayName ?? "").Trim(),
            Phone = (body.Phone ?? "").Trim(),
            Email = body.Email.Trim(),
            NormalizedEmail = body.Email.Trim().ToUpperInvariant(),
            Status = FreelanceRiderStatus.PendingInvite,
            CompanyId = body.CompanyId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        foreach (var pc in body.PostalCodes ?? Enumerable.Empty<string>())
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

        await _riders.AddAsync(rider, cancellationToken);

        if (body.SendInvite)
            await _inviteIssuer.IssueInviteAsync(rider.Id, cancellationToken);

        return Ok(new { id = rider.Id });
    }

    public sealed class PatchFreelanceRiderBody
    {
        public string? DisplayName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public FreelanceRiderStatus? Status { get; set; }
        public Guid? CompanyId { get; set; }
        public bool ClearCompany { get; set; }
        public List<string>? PostalCodes { get; set; }
    }

    [HttpPatch("{id:guid}")]
    public async Task<ActionResult> Patch(Guid id, [FromBody] PatchFreelanceRiderBody body, CancellationToken cancellationToken)
    {
        var rider = await _db.FreelanceRiders.Include(r => r.ServiceAreas).FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (rider == null)
            return NotFound();

        if (body.DisplayName != null) rider.DisplayName = body.DisplayName;
        if (body.Phone != null) rider.Phone = body.Phone;
        if (body.Email != null)
        {
            rider.Email = body.Email.Trim();
            rider.NormalizedEmail = rider.Email.ToUpperInvariant();
        }
        if (body.Status.HasValue) rider.Status = body.Status.Value;
        if (body.ClearCompany)
            rider.CompanyId = null;
        else if (body.CompanyId.HasValue)
            rider.CompanyId = body.CompanyId;

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

    [HttpPost("{id:guid}/invite")]
    public async Task<ActionResult> SendInvite(Guid id, CancellationToken cancellationToken)
    {
        var raw = await _inviteIssuer.IssueInviteAsync(id, cancellationToken);
        if (raw == null)
            return NotFound(new { message = "Rider not found or has no email." });
        return Ok(new { sent = true });
    }
}
