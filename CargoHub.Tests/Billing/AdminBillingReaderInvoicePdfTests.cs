using CargoHub.Application.Billing.Admin;
using CargoHub.Domain.Billing;
using CargoHub.Infrastructure.Billing;
using CargoHub.Infrastructure.Persistence;
using CompanyEntity = CargoHub.Domain.Companies.Company;
using Moq;
using Xunit;

namespace CargoHub.Tests.Billing;

public sealed class AdminBillingReaderInvoicePdfTests
{
    [Fact]
    public async Task GetInvoicePdfModelAsync_sums_ledger_and_payable()
    {
        using var fixture = new TestDbFixture();
        using var db = fixture.CreateContext();

        var companyId = Guid.NewGuid();
        db.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            CompanyId = Guid.NewGuid().ToString("N"),
            Name = "Test Oy",
            BusinessId = "1234567-8"
        });

        var planId = Guid.NewGuid();
        var periodPricingId = Guid.NewGuid();
        db.SubscriptionPlans.Add(new SubscriptionPlan
        {
            Id = planId,
            Name = "P",
            Kind = SubscriptionPlanKind.PayPerBooking,
            ChargeTimeAnchor = ChargeTimeAnchor.CreatedAtUtc,
            Currency = "EUR",
            IsActive = true
        });
        db.SubscriptionPlanPricingPeriods.Add(new SubscriptionPlanPricingPeriod
        {
            Id = periodPricingId,
            SubscriptionPlanId = planId,
            EffectiveFromUtc = DateTime.UtcNow
        });

        var billingPeriodId = Guid.NewGuid();
        db.CompanyBillingPeriods.Add(new CompanyBillingPeriod
        {
            Id = billingPeriodId,
            CompanyId = companyId,
            YearUtc = 2026,
            MonthUtc = 4,
            Currency = "EUR",
            Status = CompanyBillingPeriodStatus.Open
        });

        db.BillingLineItems.Add(new BillingLineItem
        {
            Id = Guid.NewGuid(),
            CompanyBillingPeriodId = billingPeriodId,
            LineType = BillingLineType.PerBooking,
            Amount = 10m,
            Currency = "EUR",
            SubscriptionPlanId = planId,
            SubscriptionPlanPricingPeriodId = periodPricingId,
            CreatedAtUtc = DateTime.UtcNow,
            ExcludedFromInvoice = false
        });
        db.BillingLineItems.Add(new BillingLineItem
        {
            Id = Guid.NewGuid(),
            CompanyBillingPeriodId = billingPeriodId,
            LineType = BillingLineType.Adjustment,
            Amount = 5m,
            Currency = "EUR",
            SubscriptionPlanId = planId,
            SubscriptionPlanPricingPeriodId = periodPricingId,
            CreatedAtUtc = DateTime.UtcNow,
            ExcludedFromInvoice = true
        });

        await db.SaveChangesAsync();

        var breakMock = new Mock<IBillingMonthBreakdownReader>();
        breakMock
            .Setup(x => x.GetBreakdownAsync(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingMonthBreakdownDto?)null);
        var reader = new AdminBillingReader(db, breakMock.Object);
        var model = await reader.GetInvoicePdfModelAsync(billingPeriodId);

        Assert.NotNull(model);
        Assert.Equal(15m, model.LedgerTotal);
        Assert.Equal(10m, model.PayableTotal);
        Assert.Equal("Test Oy", model.CompanyName);
        Assert.Equal("1234567-8", model.BusinessId);
        Assert.Equal(2, model.Lines.Count);
    }
}
