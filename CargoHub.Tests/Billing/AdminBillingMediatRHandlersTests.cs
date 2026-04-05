using CargoHub.Application.Billing.Admin;
using CargoHub.Application.Billing.AdminInvoicing;
using CargoHub.Application.Billing.AdminPlans;
using Moq;
using Xunit;

namespace CargoHub.Tests.Billing;

public sealed class AdminBillingMediatRHandlersTests
{
    [Fact]
    public async Task GetBillingPeriodDetailQueryHandler_delegates()
    {
        var mock = new Mock<IAdminBillingReader>();
        mock.Setup(x => x.GetBillingPeriodDetailAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingPeriodDetailDto?)null);
        var h = new GetBillingPeriodDetailQueryHandler(mock.Object);
        await h.Handle(new GetBillingPeriodDetailQuery(Guid.NewGuid()), default);
        mock.Verify(x => x.GetBillingPeriodDetailAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListAdminSubscriptionPlansQueryHandler_delegates()
    {
        var mock = new Mock<IAdminBillingReader>();
        mock.Setup(x => x.ListSubscriptionPlansAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AdminSubscriptionPlanSummaryDto>());
        var h = new ListAdminSubscriptionPlansQueryHandler(mock.Object);
        await h.Handle(new ListAdminSubscriptionPlansQuery(), default);
        mock.Verify(x => x.ListSubscriptionPlansAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListCompanyBillingPeriodsQueryHandler_delegates()
    {
        var mock = new Mock<IAdminBillingReader>();
        mock.Setup(x => x.ListBillingPeriodsForCompanyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CompanyBillingPeriodSummaryDto>());
        var h = new ListCompanyBillingPeriodsQueryHandler(mock.Object);
        await h.Handle(new ListCompanyBillingPeriodsQuery(Guid.NewGuid()), default);
        mock.Verify(x => x.ListBillingPeriodsForCompanyAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetBillingInvoicePdfModelQueryHandler_delegates()
    {
        var mock = new Mock<IAdminBillingReader>();
        mock.Setup(x => x.GetInvoicePdfModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingInvoicePdfModel?)null);
        var h = new GetBillingInvoicePdfModelQueryHandler(mock.Object);
        await h.Handle(new GetBillingInvoicePdfModelQuery(Guid.NewGuid()), default);
        mock.Verify(x => x.GetInvoicePdfModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendBillingPeriodInvoiceEmailCommandHandler_delegates()
    {
        var mock = new Mock<IAdminBillingInvoiceOperations>();
        mock.Setup(x => x.SendInvoiceEmailAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(SendInvoiceEmailResult.Ok());
        var h = new SendBillingPeriodInvoiceEmailCommandHandler(mock.Object);
        await h.Handle(new SendBillingPeriodInvoiceEmailCommand(Guid.NewGuid(), "u1", "sa"), default);
        mock.Verify(x => x.SendInvoiceEmailAsync(It.IsAny<Guid>(), "u1", "sa", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateBillingLineExcludedCommandHandler_delegates()
    {
        var mock = new Mock<IAdminBillingInvoiceOperations>();
        mock.Setup(x => x.UpdateLineExcludedAsync(It.IsAny<Guid>(), It.IsAny<bool>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(UpdateLineExcludedResult.Ok());
        var h = new UpdateBillingLineExcludedCommandHandler(mock.Object);
        await h.Handle(new UpdateBillingLineExcludedCommand(Guid.NewGuid(), true, "sa"), default);
        mock.Verify(x => x.UpdateLineExcludedAsync(It.IsAny<Guid>(), true, "sa", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetAdminSubscriptionPlanDetailQueryHandler_delegates()
    {
        var mock = new Mock<ISubscriptionPlanAdminRepository>();
        mock.Setup(x => x.GetPlanDetailAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AdminSubscriptionPlanDetailDto?)null);
        var h = new GetAdminSubscriptionPlanDetailQueryHandler(mock.Object);
        await h.Handle(new GetAdminSubscriptionPlanDetailQuery(Guid.NewGuid()), default);
        mock.Verify(x => x.GetPlanDetailAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Plan_command_handlers_delegate_to_repository()
    {
        var id = Guid.NewGuid();
        var repo = new Mock<ISubscriptionPlanAdminRepository>();
        repo.Setup(x => x.CreatePlanAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(id);
        repo.Setup(x => x.DeletePlanAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminPlanMutationResult.Ok());
        repo.Setup(x => x.AddPricingPeriodAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<decimal?>(), It.IsAny<decimal?>(), It.IsAny<int?>(), It.IsAny<decimal?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminPlanMutationResult.Ok());
        repo.Setup(x => x.UpdatePricingPeriodAsync(It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<decimal?>(), It.IsAny<decimal?>(), It.IsAny<int?>(), It.IsAny<decimal?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminPlanMutationResult.Ok());
        repo.Setup(x => x.DeletePricingPeriodAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminPlanMutationResult.Ok());
        repo.Setup(x => x.ReplaceTiersAsync(It.IsAny<Guid>(), It.IsAny<IReadOnlyList<AdminPricingTierInput>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminPlanMutationResult.Ok());
        repo.Setup(x => x.UpdatePlanAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AdminPlanMutationResult.Ok());

        await new AddAdminPricingPeriodCommandHandler(repo.Object).Handle(
            new AddAdminPricingPeriodCommand(Guid.NewGuid(), DateTime.UtcNow, null, null, null, null), default);
        await new UpdateAdminPricingPeriodCommandHandler(repo.Object).Handle(
            new UpdateAdminPricingPeriodCommand(Guid.NewGuid(), DateTime.UtcNow, null, null, null, null), default);
        await new DeleteAdminPricingPeriodCommandHandler(repo.Object).Handle(new DeleteAdminPricingPeriodCommand(Guid.NewGuid()), default);
        await new DeleteAdminSubscriptionPlanCommandHandler(repo.Object).Handle(new DeleteAdminSubscriptionPlanCommand(Guid.NewGuid()), default);
        await new ReplaceAdminPricingPeriodTiersCommandHandler(repo.Object).Handle(
            new ReplaceAdminPricingPeriodTiersCommand(Guid.NewGuid(), Array.Empty<AdminPricingTierInput>()), default);

        var create = await new CreateAdminSubscriptionPlanCommandHandler(repo.Object).Handle(
            new CreateAdminSubscriptionPlanCommand("N", "PayPerBooking", "CreatedAtUtc", null, "EUR", true), default);
        Assert.True(create.Success);

        var update = await new UpdateAdminSubscriptionPlanCommandHandler(repo.Object).Handle(
            new UpdateAdminSubscriptionPlanCommand(Guid.NewGuid(), "N", "PayPerBooking", "CreatedAtUtc", null, "EUR", true), default);
        Assert.True(update.Success);
    }

    [Fact]
    public async Task CreateAdminSubscriptionPlanCommandHandler_validation_branches()
    {
        var repo = new Mock<ISubscriptionPlanAdminRepository>();
        var h = new CreateAdminSubscriptionPlanCommandHandler(repo.Object);

        Assert.False((await h.Handle(new CreateAdminSubscriptionPlanCommand("", "PayPerBooking", "CreatedAtUtc", null, "EUR", true), default)).Success);
        Assert.False((await h.Handle(new CreateAdminSubscriptionPlanCommand("N", "X", "CreatedAtUtc", null, "EUR", true), default)).Success);
        Assert.False((await h.Handle(new CreateAdminSubscriptionPlanCommand("N", "PayPerBooking", "X", null, "EUR", true), default)).Success);
        Assert.False((await h.Handle(new CreateAdminSubscriptionPlanCommand("N", "Trial", "CreatedAtUtc", null, "EUR", true), default)).Success);
        Assert.False((await h.Handle(new CreateAdminSubscriptionPlanCommand("N", "PayPerBooking", "CreatedAtUtc", null, "EURO", true), default)).Success);

        repo.Setup(x => x.CreatePlanAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db"));
        var fail = await h.Handle(new CreateAdminSubscriptionPlanCommand("N", "PayPerBooking", "CreatedAtUtc", null, "EUR", true), default);
        Assert.False(fail.Success);
        Assert.Equal("CreateFailed", fail.ErrorCode);
    }

    [Fact]
    public async Task GetPlatformEarningsMonthlyQueryHandler_delegates()
    {
        var mock = new Mock<IAdminPlatformEarningsReader>();
        mock.Setup(x => x.GetMonthlyTotalsAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PlatformEarningsMonthDto>());
        var h = new GetPlatformEarningsMonthlyQueryHandler(mock.Object);
        await h.Handle(new GetPlatformEarningsMonthlyQuery(12), default);
        mock.Verify(x => x.GetMonthlyTotalsAsync(12, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPlatformEarningsByCompanyQueryHandler_delegates()
    {
        var mock = new Mock<IAdminPlatformEarningsReader>();
        mock.Setup(x => x.GetByCompanyForMonthAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PlatformEarningsCompanyDto>());
        var h = new GetPlatformEarningsByCompanyQueryHandler(mock.Object);
        await h.Handle(new GetPlatformEarningsByCompanyQuery(2025, 4), default);
        mock.Verify(x => x.GetByCompanyForMonthAsync(2025, 4, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetPlatformEarningsBySubscriptionQueryHandler_delegates()
    {
        var mock = new Mock<IAdminPlatformEarningsReader>();
        mock.Setup(x => x.GetBySubscriptionForMonthAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<PlatformEarningsSubscriptionDto>());
        var h = new GetPlatformEarningsBySubscriptionQueryHandler(mock.Object);
        await h.Handle(new GetPlatformEarningsBySubscriptionQuery(2025, 4), default);
        mock.Verify(x => x.GetBySubscriptionForMonthAsync(2025, 4, It.IsAny<CancellationToken>()), Times.Once);
    }
}
