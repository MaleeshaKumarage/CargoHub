using CargoHub.Application.Billing;
using CargoHub.Infrastructure.Billing;
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
}
