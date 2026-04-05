using CargoHub.Application.Billing;
using CargoHub.Infrastructure.Billing;
using CompanyEntity = CargoHub.Domain.Companies.Company;
using CargoHub.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace CargoHub.Tests.Billing;

public class SubscriptionPlanSeedTests
{
    [Fact]
    public async Task EnsureDefaultTrialPlanAsync_IsIdempotent()
    {
        var fixture = new TestDbFixture();
        using var ctx = fixture.CreateContext();
        await SubscriptionPlanSeed.EnsureDefaultTrialPlanAsync(ctx);
        await SubscriptionPlanSeed.EnsureDefaultTrialPlanAsync(ctx);
        var count = await ctx.SubscriptionPlans.CountAsync(p => p.Id == SubscriptionBillingConstants.DefaultTrialPlanId);
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task AssignDefaultTrialToCompaniesWithoutPlanAsync_SetsNullCompanies()
    {
        var fixture = new TestDbFixture();
        using var ctx = fixture.CreateContext();
        var companyId = Guid.NewGuid();
        ctx.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            Name = "Legacy",
            BusinessId = "LEG-1",
            CompanyId = companyId.ToString("N"),
            SubscriptionPlanId = null
        });
        await ctx.SaveChangesAsync();

        await SubscriptionPlanSeed.EnsureDefaultTrialPlanAsync(ctx);
        await SubscriptionPlanSeed.AssignDefaultTrialToCompaniesWithoutPlanAsync(ctx);

        var updated = await ctx.Companies.AsNoTracking().SingleAsync(c => c.Id == companyId);
        Assert.Equal(SubscriptionBillingConstants.DefaultTrialPlanId, updated.SubscriptionPlanId);
    }
}
