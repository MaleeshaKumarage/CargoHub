using CargoHub.Domain.Billing;
using CargoHub.Domain.Companies;
using CargoHub.Infrastructure.Billing;
using CargoHub.Infrastructure.Persistence;
using Xunit;
using CompanyEntity = CargoHub.Domain.Companies.Company;

namespace CargoHub.Tests.Billing;

public sealed class CompanySubscriptionPlanResolverTests : IDisposable
{
    private readonly TestDbFixture _fixture = new();

    public void Dispose() => _fixture.Dispose();

    [Fact]
    public async Task ResolvePlanIdAtAsync_UsesCompanyRow_WhenNoAssignments()
    {
        using var ctx = _fixture.CreateContext();
        var planId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            CompanyId = "co-1",
            Name = "C",
            BusinessId = "1111111-1",
            CustomerId = "x",
            SubscriptionPlanId = planId,
        });
        await ctx.SaveChangesAsync();

        var resolved = await CompanySubscriptionPlanResolver.ResolvePlanIdAtAsync(ctx, companyId, DateTime.UtcNow, default);
        Assert.Equal(planId, resolved);
    }

    [Fact]
    public async Task ResolvePlanIdAtAsync_PrefersLatestAssignment_OnOrBeforeInstant()
    {
        using var ctx = _fixture.CreateContext();
        var planCompany = Guid.NewGuid();
        var planOld = Guid.NewGuid();
        var planNew = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            CompanyId = "co-2",
            Name = "C",
            BusinessId = "2222222-2",
            CustomerId = "x",
            SubscriptionPlanId = planCompany,
        });
        ctx.CompanySubscriptionAssignments.AddRange(
            new CompanySubscriptionAssignment
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                SubscriptionPlanId = planOld,
                EffectiveFromUtc = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            },
            new CompanySubscriptionAssignment
            {
                Id = Guid.NewGuid(),
                CompanyId = companyId,
                SubscriptionPlanId = planNew,
                EffectiveFromUtc = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
            });
        await ctx.SaveChangesAsync();

        var anchor = new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc);
        var resolved = await CompanySubscriptionPlanResolver.ResolvePlanIdAtAsync(ctx, companyId, anchor, default);
        Assert.Equal(planNew, resolved);
    }

    [Fact]
    public async Task ResolvePlanIdAtAsync_IgnoresFutureAssignments()
    {
        using var ctx = _fixture.CreateContext();
        var planCompany = Guid.NewGuid();
        var planFuture = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            CompanyId = "co-3",
            Name = "C",
            BusinessId = "3333333-3",
            CustomerId = "x",
            SubscriptionPlanId = planCompany,
        });
        ctx.CompanySubscriptionAssignments.Add(new CompanySubscriptionAssignment
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            SubscriptionPlanId = planFuture,
            EffectiveFromUtc = new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        });
        await ctx.SaveChangesAsync();

        var anchor = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var resolved = await CompanySubscriptionPlanResolver.ResolvePlanIdAtAsync(ctx, companyId, anchor, default);
        Assert.Equal(planCompany, resolved);
    }

    [Fact]
    public async Task ResolvePlanIdAtAsync_ReturnsNull_WhenCompanyMissing()
    {
        using var ctx = _fixture.CreateContext();
        var resolved = await CompanySubscriptionPlanResolver.ResolvePlanIdAtAsync(ctx, Guid.NewGuid(), DateTime.UtcNow, default);
        Assert.Null(resolved);
    }
}
