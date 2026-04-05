using CargoHub.Application.Billing.Admin;
using CargoHub.Domain.Billing;
using Microsoft.EntityFrameworkCore;

namespace CargoHub.Infrastructure.Persistence;

public sealed class CompanySubscriptionAssignmentRepository : ICompanySubscriptionAssignmentRepository
{
    private readonly ApplicationDbContext _db;

    public CompanySubscriptionAssignmentRepository(ApplicationDbContext db) => _db = db;

    public async Task RecordAsync(
        Guid companyId,
        Guid subscriptionPlanId,
        DateTime effectiveFromUtc,
        string? setByUserId,
        CancellationToken cancellationToken = default)
    {
        _db.CompanySubscriptionAssignments.Add(new CompanySubscriptionAssignment
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            SubscriptionPlanId = subscriptionPlanId,
            EffectiveFromUtc = effectiveFromUtc,
            SetByUserId = setByUserId
        });
        await _db.SaveChangesAsync(cancellationToken);
    }
}
