using CargoHub.Application.AdminCompanies;
using CargoHub.Application.Billing;
using CargoHub.Application.Billing.Admin;
using CargoHub.Application.Company;
using Moq;
using Xunit;
using CompanyEntity = CargoHub.Domain.Companies.Company;

namespace CargoHub.Tests.AdminCompanies;

public class CreateAdminCompanyCommandHandlerTests
{
    private static CreateAdminCompanyCommandHandler CreateHandler(
        out Mock<ICompanyRepository> repo,
        out Mock<ICompanyAdminInviteIssuer> invites,
        out Mock<ICompanyUserMetrics> metrics)
    {
        repo = new Mock<ICompanyRepository>();
        invites = new Mock<ICompanyAdminInviteIssuer>();
        metrics = new Mock<ICompanyUserMetrics>();
        var assignments = new Mock<ICompanySubscriptionAssignmentRepository>();
        assignments
            .Setup(x => x.RecordAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        return new CreateAdminCompanyCommandHandler(repo.Object, invites.Object, metrics.Object, assignments.Object);
    }

    [Fact]
    public async Task Handle_EmptyName_Fails()
    {
        var h = CreateHandler(out _, out _, out _);
        var r = await h.Handle(new CreateAdminCompanyCommand("", "bid", 10, 2, null), default);
        Assert.False(r.Success);
        Assert.Equal("NameRequired", r.ErrorCode);
    }

    [Fact]
    public async Task Handle_WhitespaceOnlyName_Fails()
    {
        var h = CreateHandler(out _, out _, out _);
        var r = await h.Handle(new CreateAdminCompanyCommand("   ", "bid", 10, 2, null), default);
        Assert.False(r.Success);
        Assert.Equal("NameRequired", r.ErrorCode);
    }

    [Fact]
    public async Task Handle_TrimsNameAndBusinessId()
    {
        var h = CreateHandler(out var repo, out var invites, out var metrics);
        repo.Setup(x => x.GetByBusinessIdAsync("bid", default)).ReturnsAsync((CompanyEntity?)null);
        CompanyEntity? created = null;
        repo.Setup(x => x.CreateAsync(It.IsAny<CompanyEntity>(), default))
            .Callback<CompanyEntity, CancellationToken>((c, _) => created = c)
            .ReturnsAsync((CompanyEntity c, CancellationToken _) => c);
        repo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Guid id, CancellationToken _) => created!.Id == id ? created : null);
        metrics.Setup(x => x.CountAdminsForBusinessIdAsync("bid", default)).ReturnsAsync(0);

        var r = await h.Handle(
            new CreateAdminCompanyCommand("  Acme  ", "  bid  ", 10, 5, null),
            default);

        Assert.True(r.Success);
        Assert.Equal("Acme", r.Company!.Name);
        Assert.Equal("bid", r.Company.BusinessId);
        invites.Verify(
            x => x.TryIssueInitialAdminInvitesAsync(It.IsAny<Guid>(), "bid", null, default),
            Times.Once);
        Assert.Equal(SubscriptionBillingConstants.DefaultTrialPlanId, created!.SubscriptionPlanId);
    }

    [Fact]
    public async Task Handle_UsesExplicitSubscriptionPlanId_WhenProvided()
    {
        var h = CreateHandler(out var repo, out _, out var metrics);
        repo.Setup(x => x.GetByBusinessIdAsync("bid", default)).ReturnsAsync((CompanyEntity?)null);
        CompanyEntity? created = null;
        repo.Setup(x => x.CreateAsync(It.IsAny<CompanyEntity>(), default))
            .Callback<CompanyEntity, CancellationToken>((c, _) => created = c)
            .ReturnsAsync((CompanyEntity c, CancellationToken _) => c);
        repo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Guid id, CancellationToken _) => created!.Id == id ? created : null);
        metrics.Setup(x => x.CountAdminsForBusinessIdAsync("bid", default)).ReturnsAsync(0);
        var planId = Guid.NewGuid();
        var r = await h.Handle(new CreateAdminCompanyCommand("Co", "bid", 10, 2, null, planId), default);
        Assert.True(r.Success);
        Assert.Equal(planId, created!.SubscriptionPlanId);
    }

    [Fact]
    public async Task Handle_EmptyBusinessId_Fails()
    {
        var h = CreateHandler(out _, out _, out _);
        var r = await h.Handle(new CreateAdminCompanyCommand("Co", "  ", 10, 2, null), default);
        Assert.False(r.Success);
        Assert.Equal("BusinessIdRequired", r.ErrorCode);
    }

    [Fact]
    public async Task Handle_MaxUsersBelowOne_Fails()
    {
        var h = CreateHandler(out _, out _, out _);
        var r = await h.Handle(new CreateAdminCompanyCommand("Co", "bid", 0, 2, null), default);
        Assert.False(r.Success);
        Assert.Equal("InvalidMaxUsers", r.ErrorCode);
    }

    [Fact]
    public async Task Handle_MaxAdminsBelowOne_Fails()
    {
        var h = CreateHandler(out _, out _, out _);
        var r = await h.Handle(new CreateAdminCompanyCommand("Co", "bid", 10, 0, null), default);
        Assert.False(r.Success);
        Assert.Equal("InvalidMaxAdmins", r.ErrorCode);
    }

    [Fact]
    public async Task Handle_DuplicateBusinessId_Fails()
    {
        var h = CreateHandler(out var repo, out _, out _);
        repo.Setup(x => x.GetByBusinessIdAsync("dup", default)).ReturnsAsync(new CompanyEntity { BusinessId = "dup" });

        var r = await h.Handle(new CreateAdminCompanyCommand("Co", "dup", 10, 5, null), default);
        Assert.False(r.Success);
        Assert.Equal("BusinessIdExists", r.ErrorCode);
        repo.Verify(x => x.CreateAsync(It.IsAny<CompanyEntity>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_TooManyInviteEmails_Fails()
    {
        var h = CreateHandler(out var repo, out _, out _);
        repo.Setup(x => x.GetByBusinessIdAsync("bid", default)).ReturnsAsync((CompanyEntity?)null);

        var emails = new[] { "a@test.com", "b@test.com", "c@test.com" };
        var r = await h.Handle(new CreateAdminCompanyCommand("Co", "bid", 10, 2, emails), default);
        Assert.False(r.Success);
        Assert.Equal("TooManyInviteEmails", r.ErrorCode);
    }

    [Fact]
    public async Task Handle_Success_IssuesInvitesWhenNoAdmins()
    {
        var h = CreateHandler(out var repo, out var invites, out var metrics);
        repo.Setup(x => x.GetByBusinessIdAsync("1234567-8", default)).ReturnsAsync((CompanyEntity?)null);
        CompanyEntity? created = null;
        repo.Setup(x => x.CreateAsync(It.IsAny<CompanyEntity>(), default))
            .Callback<CompanyEntity, CancellationToken>((c, _) => created = c)
            .ReturnsAsync((CompanyEntity c, CancellationToken _) => c);
        repo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Guid id, CancellationToken _) => created!.Id == id ? created : null);
        metrics.Setup(x => x.CountAdminsForBusinessIdAsync("1234567-8", default)).ReturnsAsync(0);

        var r = await h.Handle(
            new CreateAdminCompanyCommand("Acme", "1234567-8", 50, 5, new[] { "admin@test.com" }),
            default);

        Assert.True(r.Success);
        Assert.NotNull(r.Company);
        Assert.Equal("Acme", r.Company!.Name);
        invites.Verify(
            x => x.TryIssueInitialAdminInvitesAsync(created!.Id, "1234567-8", It.IsAny<IReadOnlyList<string>>(), default),
            Times.Once);
    }

    [Fact]
    public async Task Handle_Success_SkipsInvitesWhenAdminsAlreadyExist()
    {
        var h = CreateHandler(out var repo, out var invites, out var metrics);
        repo.Setup(x => x.GetByBusinessIdAsync("bid", default)).ReturnsAsync((CompanyEntity?)null);
        CompanyEntity? created = null;
        repo.Setup(x => x.CreateAsync(It.IsAny<CompanyEntity>(), default))
            .Callback<CompanyEntity, CancellationToken>((c, _) => created = c)
            .ReturnsAsync((CompanyEntity c, CancellationToken _) => c);
        repo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync((Guid id, CancellationToken _) => created!.Id == id ? created : null);
        metrics.Setup(x => x.CountAdminsForBusinessIdAsync("bid", default)).ReturnsAsync(2);

        var r = await h.Handle(new CreateAdminCompanyCommand("Co", "bid", 10, 5, null), default);

        Assert.True(r.Success);
        invites.Verify(
            x => x.TryIssueInitialAdminInvitesAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<IReadOnlyList<string>?>(), default),
            Times.Never);
    }

    [Fact]
    public async Task Handle_Success_GetByIdNullAfterCreate_Fails()
    {
        var h = CreateHandler(out var repo, out _, out var metrics);
        repo.Setup(x => x.GetByBusinessIdAsync("bid", default)).ReturnsAsync((CompanyEntity?)null);
        repo.Setup(x => x.CreateAsync(It.IsAny<CompanyEntity>(), default))
            .ReturnsAsync((CompanyEntity c, CancellationToken _) => c);
        repo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), default)).ReturnsAsync((CompanyEntity?)null);
        metrics.Setup(x => x.CountAdminsForBusinessIdAsync("bid", default)).ReturnsAsync(0);

        var r = await h.Handle(new CreateAdminCompanyCommand("Co", "bid", 10, 5, null), default);

        Assert.False(r.Success);
        Assert.Equal("NotFound", r.ErrorCode);
    }
}
