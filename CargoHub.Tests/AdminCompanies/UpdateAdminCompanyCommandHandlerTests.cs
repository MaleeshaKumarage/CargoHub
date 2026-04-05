using CargoHub.Application.AdminCompanies;
using CargoHub.Application.Billing.AdminPlans;
using CargoHub.Application.Company;
using Moq;
using Xunit;
using CompanyEntity = CargoHub.Domain.Companies.Company;

namespace CargoHub.Tests.AdminCompanies;

public class UpdateAdminCompanyCommandHandlerTests
{
    private static UpdateAdminCompanyCommandHandler CreateHandler(
        out Mock<ICompanyRepository> repo,
        out Mock<ICompanyAdminInviteIssuer> invites,
        out Mock<ICompanyUserMetrics> metrics,
        out Mock<IAdminCompanyLimitUserOperations> limits,
        out Mock<ISubscriptionPlanAdminRepository> planRepo)
    {
        repo = new Mock<ICompanyRepository>();
        invites = new Mock<ICompanyAdminInviteIssuer>();
        metrics = new Mock<ICompanyUserMetrics>();
        limits = new Mock<IAdminCompanyLimitUserOperations>();
        planRepo = new Mock<ISubscriptionPlanAdminRepository>();
        planRepo
            .Setup(x => x.PlanExistsAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        return new UpdateAdminCompanyCommandHandler(
            repo.Object,
            invites.Object,
            metrics.Object,
            limits.Object,
            planRepo.Object);
    }

    private static CompanyEntity TrackedCompany(Guid id, string bid = "BIZ-1") => new()
    {
        Id = id,
        Name = "Co",
        BusinessId = bid,
        CompanyId = "cid",
        MaxUserAccounts = 10,
        MaxAdminAccounts = 3
    };

    [Fact]
    public async Task Handle_MaxUsersBelowOne_Fails()
    {
        var h = CreateHandler(out _, out _, out _, out _, out _);
        var r = await h.Handle(new UpdateAdminCompanyCommand(Guid.NewGuid(), 0, null, false, null, null), default);
        Assert.False(r.Success);
        Assert.Equal("InvalidMaxUsers", r.ErrorCode);
    }

    [Fact]
    public async Task Handle_MaxAdminsBelowOne_Fails()
    {
        var h = CreateHandler(out _, out _, out _, out _, out _);
        var r = await h.Handle(new UpdateAdminCompanyCommand(Guid.NewGuid(), null, 0, false, null, null), default);
        Assert.False(r.Success);
        Assert.Equal("InvalidMaxAdmins", r.ErrorCode);
    }

    [Fact]
    public async Task Handle_CompanyNotFound_Fails()
    {
        var h = CreateHandler(out var repo, out _, out _, out _, out _);
        var id = Guid.NewGuid();
        repo.Setup(x => x.GetByIdForUpdateAsync(id, default)).ReturnsAsync((CompanyEntity?)null);

        var r = await h.Handle(new UpdateAdminCompanyCommand(id, 5, 2, false, null, null), default);
        Assert.False(r.Success);
        Assert.Equal("NotFound", r.ErrorCode);
    }

    [Fact]
    public async Task Handle_LimitReduction_NeedsMoreDemotions_ReturnsConflict()
    {
        var id = Guid.NewGuid();
        var h = CreateHandler(out var repo, out _, out var metrics, out _, out _);
        repo.Setup(x => x.GetByIdForUpdateAsync(id, default)).ReturnsAsync(TrackedCompany(id));
        metrics.Setup(x => x.CountActiveUsersForBusinessIdAsync("BIZ-1", default)).ReturnsAsync(3);
        metrics.Setup(x => x.CountAdminsForBusinessIdAsync("BIZ-1", default)).ReturnsAsync(4);

        var r = await h.Handle(new UpdateAdminCompanyCommand(id, 10, 2, false, null, null), default);

        Assert.False(r.Success);
        Assert.Equal("LimitReductionRequired", r.ErrorCode);
        Assert.NotNull(r.LimitReductionRequired);
        Assert.Equal(2, r.LimitReductionRequired!.MinimumAdminsToDemote);
    }

    [Fact]
    public async Task Handle_LimitReduction_NeedsMoreDeactivations_ReturnsConflict()
    {
        var id = Guid.NewGuid();
        var h = CreateHandler(out var repo, out _, out var metrics, out _, out _);
        repo.Setup(x => x.GetByIdForUpdateAsync(id, default)).ReturnsAsync(TrackedCompany(id));
        metrics.Setup(x => x.CountActiveUsersForBusinessIdAsync("BIZ-1", default)).ReturnsAsync(12);
        metrics.Setup(x => x.CountAdminsForBusinessIdAsync("BIZ-1", default)).ReturnsAsync(1);

        var r = await h.Handle(new UpdateAdminCompanyCommand(id, 5, 3, false, null, null), default);

        Assert.False(r.Success);
        Assert.Equal("LimitReductionRequired", r.ErrorCode);
        Assert.True(r.LimitReductionRequired!.MinimumUsersToDeactivate > 0);
    }

    [Fact]
    public async Task Handle_UnexpectedDemotions_Fails()
    {
        var id = Guid.NewGuid();
        var h = CreateHandler(out var repo, out _, out var metrics, out _, out _);
        repo.Setup(x => x.GetByIdForUpdateAsync(id, default)).ReturnsAsync(TrackedCompany(id));
        metrics.Setup(x => x.CountActiveUsersForBusinessIdAsync("BIZ-1", default)).ReturnsAsync(1);
        metrics.Setup(x => x.CountAdminsForBusinessIdAsync("BIZ-1", default)).ReturnsAsync(1);

        var r = await h.Handle(
            new UpdateAdminCompanyCommand(id, 10, 3, false, null, new[] { "user-1" }),
            default);

        Assert.False(r.Success);
        Assert.Equal("UnexpectedDemotions", r.ErrorCode);
    }

    [Fact]
    public async Task Handle_UnexpectedDeactivations_Fails()
    {
        var id = Guid.NewGuid();
        var h = CreateHandler(out var repo, out _, out var metrics, out _, out _);
        repo.Setup(x => x.GetByIdForUpdateAsync(id, default)).ReturnsAsync(TrackedCompany(id));
        metrics.Setup(x => x.CountActiveUsersForBusinessIdAsync("BIZ-1", default)).ReturnsAsync(1);
        metrics.Setup(x => x.CountAdminsForBusinessIdAsync("BIZ-1", default)).ReturnsAsync(1);

        var r = await h.Handle(
            new UpdateAdminCompanyCommand(id, 10, 3, false, new[] { "u1" }, null),
            default);

        Assert.False(r.Success);
        Assert.Equal("UnexpectedDeactivations", r.ErrorCode);
    }

    [Fact]
    public async Task Handle_DemoteFails_ReturnsError()
    {
        var id = Guid.NewGuid();
        var h = CreateHandler(out var repo, out _, out var metrics, out var limits, out _);
        repo.Setup(x => x.GetByIdForUpdateAsync(id, default)).ReturnsAsync(TrackedCompany(id));
        metrics.Setup(x => x.CountActiveUsersForBusinessIdAsync("BIZ-1", default)).ReturnsAsync(1);
        metrics.Setup(x => x.CountAdminsForBusinessIdAsync("BIZ-1", default)).ReturnsAsync(4);
        limits.Setup(x => x.DemoteAdminsAsync("BIZ-1", It.IsAny<IReadOnlyList<string>>(), default))
            .ReturnsAsync("boom");

        var r = await h.Handle(
            new UpdateAdminCompanyCommand(id, 10, 2, false, null, new[] { "a", "b" }),
            default);

        Assert.False(r.Success);
        Assert.Equal("DemoteFailed", r.ErrorCode);
        Assert.Equal("boom", r.Message);
    }

    [Fact]
    public async Task Handle_DeactivateFails_ReturnsError()
    {
        var id = Guid.NewGuid();
        var h = CreateHandler(out var repo, out _, out var metrics, out var limits, out _);
        repo.Setup(x => x.GetByIdForUpdateAsync(id, default)).ReturnsAsync(TrackedCompany(id));
        metrics.Setup(x => x.CountActiveUsersForBusinessIdAsync("BIZ-1", default)).ReturnsAsync(12);
        metrics.Setup(x => x.CountAdminsForBusinessIdAsync("BIZ-1", default)).ReturnsAsync(1);
        limits.Setup(x => x.DeactivateUsersAsync("BIZ-1", It.IsAny<IReadOnlyList<string>>(), default))
            .ReturnsAsync("nope");

        var deactivateIds = Enumerable.Range(1, 7).Select(i => $"u{i}").ToList();
        var r = await h.Handle(
            new UpdateAdminCompanyCommand(id, 5, 3, false, deactivateIds, null),
            default);

        Assert.False(r.Success);
        Assert.Equal("DeactivateFailed", r.ErrorCode);
    }

    [Fact]
    public async Task Handle_Success_UpdatesLimits()
    {
        var id = Guid.NewGuid();
        var company = TrackedCompany(id);
        var h = CreateHandler(out var repo, out _, out var metrics, out _, out _);
        repo.Setup(x => x.GetByIdForUpdateAsync(id, default)).ReturnsAsync(company);
        metrics.Setup(x => x.CountActiveUsersForBusinessIdAsync("BIZ-1", default)).ReturnsAsync(1);
        metrics.Setup(x => x.CountAdminsForBusinessIdAsync("BIZ-1", default)).ReturnsAsync(1);
        repo.Setup(x => x.UpdateAsync(company, default)).ReturnsAsync(company);
        repo.Setup(x => x.GetByIdAsync(id, default)).ReturnsAsync(company);

        var r = await h.Handle(new UpdateAdminCompanyCommand(id, 7, 2, false, null, null), default);

        Assert.True(r.Success);
        Assert.Equal(7, r.Company!.MaxUserAccounts);
        Assert.Equal(2, r.Company.MaxAdminAccounts);
    }

    [Fact]
    public async Task Handle_ResendInvite_NoBusinessId_Fails()
    {
        var id = Guid.NewGuid();
        var company = TrackedCompany(id, "");
        var h = CreateHandler(out var repo, out _, out var metrics, out _, out _);
        repo.Setup(x => x.GetByIdForUpdateAsync(id, default)).ReturnsAsync(company);
        metrics.Setup(x => x.CountActiveUsersForBusinessIdAsync("", default)).ReturnsAsync(0);
        metrics.Setup(x => x.CountAdminsForBusinessIdAsync("", default)).ReturnsAsync(0);
        repo.Setup(x => x.UpdateAsync(company, default)).ReturnsAsync(company);

        var r = await h.Handle(new UpdateAdminCompanyCommand(id, null, null, true, null, null), default);

        Assert.False(r.Success);
        Assert.Equal("BusinessIdRequired", r.ErrorCode);
    }

    [Fact]
    public async Task Handle_ResendInvite_AlreadyHasAdmin_Fails()
    {
        var id = Guid.NewGuid();
        var company = TrackedCompany(id);
        var h = CreateHandler(out var repo, out _, out var metrics, out _, out _);
        repo.Setup(x => x.GetByIdForUpdateAsync(id, default)).ReturnsAsync(company);
        metrics.Setup(x => x.CountActiveUsersForBusinessIdAsync("BIZ-1", default)).ReturnsAsync(1);
        metrics.Setup(x => x.CountAdminsForBusinessIdAsync("BIZ-1", default)).ReturnsAsync(1);
        repo.Setup(x => x.UpdateAsync(company, default)).ReturnsAsync(company);

        var r = await h.Handle(new UpdateAdminCompanyCommand(id, null, null, true, null, null), default);

        Assert.False(r.Success);
        Assert.Equal("AlreadyHasAdmin", r.ErrorCode);
    }

    [Fact]
    public async Task Handle_ResendInvite_Success_CallsIssuer()
    {
        var id = Guid.NewGuid();
        var company = TrackedCompany(id);
        company.InitialAdminInviteEmail = "a@test.com";
        var h = CreateHandler(out var repo, out var invites, out var metrics, out _, out _);
        repo.Setup(x => x.GetByIdForUpdateAsync(id, default)).ReturnsAsync(company);
        metrics.Setup(x => x.CountActiveUsersForBusinessIdAsync("BIZ-1", default)).ReturnsAsync(1);
        metrics.SetupSequence(x => x.CountAdminsForBusinessIdAsync("BIZ-1", default))
            .ReturnsAsync(1)
            .ReturnsAsync(0)
            .ReturnsAsync(0);

        repo.Setup(x => x.UpdateAsync(company, default)).ReturnsAsync(company);
        repo.Setup(x => x.GetByIdAsync(id, default)).ReturnsAsync(company);

        var r = await h.Handle(new UpdateAdminCompanyCommand(id, null, null, true, null, null), default);

        Assert.True(r.Success);
        invites.Verify(
            x => x.TryIssueInitialAdminInvitesAsync(id, "BIZ-1", It.IsAny<IReadOnlyList<string>?>(), default),
            Times.Once);
    }

    [Fact]
    public async Task Handle_NotFoundAfterUpdate_Fails()
    {
        var id = Guid.NewGuid();
        var company = TrackedCompany(id);
        var h = CreateHandler(out var repo, out _, out var metrics, out _, out _);
        repo.Setup(x => x.GetByIdForUpdateAsync(id, default)).ReturnsAsync(company);
        metrics.Setup(x => x.CountActiveUsersForBusinessIdAsync("BIZ-1", default)).ReturnsAsync(1);
        metrics.Setup(x => x.CountAdminsForBusinessIdAsync("BIZ-1", default)).ReturnsAsync(1);
        repo.Setup(x => x.UpdateAsync(company, default)).ReturnsAsync(company);
        repo.Setup(x => x.GetByIdAsync(id, default)).ReturnsAsync((CompanyEntity?)null);

        var r = await h.Handle(new UpdateAdminCompanyCommand(id, 8, null, false, null, null), default);

        Assert.False(r.Success);
        Assert.Equal("NotFound", r.ErrorCode);
    }

    [Fact]
    public async Task Handle_EmptyBusinessId_UsesZeroCounts()
    {
        var id = Guid.NewGuid();
        var company = TrackedCompany(id, "");
        var h = CreateHandler(out var repo, out _, out var metrics, out _, out _);
        repo.Setup(x => x.GetByIdForUpdateAsync(id, default)).ReturnsAsync(company);
        repo.Setup(x => x.UpdateAsync(company, default)).ReturnsAsync(company);
        repo.Setup(x => x.GetByIdAsync(id, default)).ReturnsAsync(company);

        var r = await h.Handle(new UpdateAdminCompanyCommand(id, 5, null, false, null, null), default);

        Assert.True(r.Success);
        metrics.Verify(x => x.CountActiveUsersForBusinessIdAsync(It.IsAny<string>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_UpdatesSubscriptionPlanId_WhenProvided()
    {
        var id = Guid.NewGuid();
        var company = TrackedCompany(id);
        var h = CreateHandler(out var repo, out _, out var metrics, out _, out _);
        repo.Setup(x => x.GetByIdForUpdateAsync(id, default)).ReturnsAsync(company);
        metrics.Setup(x => x.CountActiveUsersForBusinessIdAsync("BIZ-1", default)).ReturnsAsync(1);
        metrics.Setup(x => x.CountAdminsForBusinessIdAsync("BIZ-1", default)).ReturnsAsync(1);
        repo.Setup(x => x.UpdateAsync(company, default)).ReturnsAsync(company);
        repo.Setup(x => x.GetByIdAsync(id, default)).ReturnsAsync(company);
        var newPlan = Guid.NewGuid();
        var r = await h.Handle(new UpdateAdminCompanyCommand(id, null, null, false, null, null, newPlan), default);
        Assert.True(r.Success);
        Assert.Equal(newPlan, company.SubscriptionPlanId);
        Assert.Equal(newPlan, r.Company!.SubscriptionPlanId);
    }

    [Fact]
    public async Task Handle_UnknownSubscriptionPlanId_Fails()
    {
        var id = Guid.NewGuid();
        var company = TrackedCompany(id);
        var h = CreateHandler(out var repo, out _, out var metrics, out _, out var planRepo);
        planRepo.Setup(x => x.PlanExistsAsync(It.IsAny<Guid>(), default)).ReturnsAsync(false);
        repo.Setup(x => x.GetByIdForUpdateAsync(id, default)).ReturnsAsync(company);
        metrics.Setup(x => x.CountActiveUsersForBusinessIdAsync("BIZ-1", default)).ReturnsAsync(1);
        metrics.Setup(x => x.CountAdminsForBusinessIdAsync("BIZ-1", default)).ReturnsAsync(1);
        var badPlan = Guid.NewGuid();
        var r = await h.Handle(new UpdateAdminCompanyCommand(id, null, null, false, null, null, badPlan), default);
        Assert.False(r.Success);
        Assert.Equal("SubscriptionPlanNotFound", r.ErrorCode);
    }

    [Fact]
    public async Task Handle_EmptyGuidSubscriptionPlan_Fails()
    {
        var id = Guid.NewGuid();
        var company = TrackedCompany(id);
        var h = CreateHandler(out var repo, out _, out var metrics, out _, out _);
        repo.Setup(x => x.GetByIdForUpdateAsync(id, default)).ReturnsAsync(company);
        metrics.Setup(x => x.CountActiveUsersForBusinessIdAsync("BIZ-1", default)).ReturnsAsync(1);
        metrics.Setup(x => x.CountAdminsForBusinessIdAsync("BIZ-1", default)).ReturnsAsync(1);
        var r = await h.Handle(
            new UpdateAdminCompanyCommand(id, null, null, false, null, null, Guid.Empty),
            default);
        Assert.False(r.Success);
        Assert.Equal("InvalidSubscriptionPlan", r.ErrorCode);
    }
}
