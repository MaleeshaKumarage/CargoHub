using CargoHub.Application.Auth;
using CargoHub.Application.Billing.Admin;
using CargoHub.Application.Billing.AdminInvoicing;
using CargoHub.Application.Couriers;
using CargoHub.Domain.Billing;
using CargoHub.Infrastructure.Billing;
using CargoHub.Infrastructure.Identity;
using CargoHub.Infrastructure.Persistence;
using CompanyEntity = CargoHub.Domain.Companies.Company;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CargoHub.Tests.Billing;

public sealed class AdminBillingInvoiceOperationsTests
{
    private static ServiceProvider CreateServiceProvider(out string dbName)
    {
        var name = Guid.NewGuid().ToString("N");
        dbName = name;
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(name));
        services
            .AddIdentity<ApplicationUser, IdentityRole>(o =>
            {
                o.Password.RequireDigit = true;
                o.Password.RequireLowercase = true;
                o.Password.RequireUppercase = false;
                o.Password.RequireNonAlphanumeric = false;
                o.Password.RequiredLength = 6;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();
        var sp = services.BuildServiceProvider();
        using var scope = sp.CreateScope();
        scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
        return sp;
    }

    private static async Task EnsureRolesAsync(RoleManager<IdentityRole> roles)
    {
        foreach (var name in new[] { RoleNames.SuperAdmin, RoleNames.Admin, RoleNames.User })
        {
            if (!await roles.RoleExistsAsync(name))
                await roles.CreateAsync(new IdentityRole(name));
        }
    }

    private static AdminBillingInvoiceOperations CreateOps(
        ApplicationDbContext db,
        UserManager<ApplicationUser> users,
        IAdminBillingReader reader,
        IEmailSender email,
        IBillingInvoicePdfGenerator pdf) =>
        new(db, users, reader, email, pdf);

    [Fact]
    public async Task SendInvoiceEmailAsync_recipient_empty_fails()
    {
        using var sp = CreateServiceProvider(out _);
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var mockR = new Mock<IAdminBillingReader>();
        var mockE = new Mock<IEmailSender>();
        var mockP = new Mock<IBillingInvoicePdfGenerator>();
        var ops = CreateOps(db, users, mockR.Object, mockE.Object, mockP.Object);

        var r = await ops.SendInvoiceEmailAsync(Guid.NewGuid(), "  ", "sa");
        Assert.False(r.Success);
        Assert.Equal("RecipientRequired", r.ErrorCode);
    }

    [Fact]
    public async Task SendInvoiceEmailAsync_period_not_found_fails()
    {
        using var sp = CreateServiceProvider(out _);
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var mockR = new Mock<IAdminBillingReader>();
        mockR.Setup(x => x.GetInvoicePdfModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BillingInvoicePdfModel?)null);
        var ops = CreateOps(db, users, mockR.Object, Mock.Of<IEmailSender>(), Mock.Of<IBillingInvoicePdfGenerator>());

        var r = await ops.SendInvoiceEmailAsync(Guid.NewGuid(), "user1", "sa");
        Assert.False(r.Success);
        Assert.Equal("NotFound", r.ErrorCode);
    }

    [Fact]
    public async Task SendInvoiceEmailAsync_company_row_missing_fails()
    {
        using var sp = CreateServiceProvider(out _);
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var companyId = Guid.NewGuid();
        var mockR = new Mock<IAdminBillingReader>();
        mockR.Setup(x => x.GetInvoicePdfModelAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingInvoicePdfModel
            {
                PeriodId = Guid.NewGuid(),
                CompanyId = companyId,
                CompanyName = "X",
                YearUtc = 2026,
                MonthUtc = 4,
                Currency = "EUR",
                Status = "Open",
                PayableTotal = 1m,
                LedgerTotal = 1m,
                Lines = Array.Empty<BillingInvoicePdfLineModel>()
            });
        var ops = CreateOps(db, users, mockR.Object, Mock.Of<IEmailSender>(), Mock.Of<IBillingInvoicePdfGenerator>());

        var r = await ops.SendInvoiceEmailAsync(Guid.NewGuid(), "u1", "sa");
        Assert.False(r.Success);
        Assert.Equal("CompanyNotFound", r.ErrorCode);
    }

    [Fact]
    public async Task SendInvoiceEmailAsync_user_validation_failures()
    {
        using var sp = CreateServiceProvider(out _);
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRolesAsync(roles);

        var companyId = Guid.NewGuid();
        db.Companies.Add(new CompanyEntity { Id = companyId, CompanyId = Guid.NewGuid().ToString("N"), BusinessId = "BIZ-1" });
        await db.SaveChangesAsync();

        var periodId = Guid.NewGuid();
        var mockR = new Mock<IAdminBillingReader>();
        mockR.Setup(x => x.GetInvoicePdfModelAsync(periodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingInvoicePdfModel
            {
                PeriodId = periodId,
                CompanyId = companyId,
                CompanyName = "Co",
                YearUtc = 2026,
                MonthUtc = 4,
                Currency = "EUR",
                Status = "Open",
                PayableTotal = 1m,
                LedgerTotal = 2m,
                Lines = Array.Empty<BillingInvoicePdfLineModel>()
            });

        var mockE = new Mock<IEmailSender>();
        var mockP = new Mock<IBillingInvoicePdfGenerator>();
        var ops = CreateOps(db, users, mockR.Object, mockE.Object, mockP.Object);

        var rUnknown = await ops.SendInvoiceEmailAsync(periodId, Guid.NewGuid().ToString(), "sa");
        Assert.Equal("RecipientNotFound", rUnknown.ErrorCode);

        var uInactive = new ApplicationUser
        {
            UserName = "a1@test.com",
            Email = "a1@test.com",
            EmailConfirmed = true,
            IsActive = false,
            BusinessId = "BIZ-1"
        };
        await users.CreateAsync(uInactive, "Test1!x");
        await users.AddToRoleAsync(uInactive, RoleNames.Admin);
        var rInactive = await ops.SendInvoiceEmailAsync(periodId, uInactive.Id, "sa");
        Assert.Equal("RecipientInactive", rInactive.ErrorCode);

        var uUserRole = new ApplicationUser
        {
            UserName = "a2@test.com",
            Email = "a2@test.com",
            EmailConfirmed = true,
            IsActive = true,
            BusinessId = "BIZ-1"
        };
        await users.CreateAsync(uUserRole, "Test1!x");
        await users.AddToRoleAsync(uUserRole, RoleNames.User);
        var rNotAdmin = await ops.SendInvoiceEmailAsync(periodId, uUserRole.Id, "sa");
        Assert.Equal("RecipientNotAdmin", rNotAdmin.ErrorCode);

        var uNoBiz = new ApplicationUser
        {
            UserName = "a3@test.com",
            Email = "a3@test.com",
            EmailConfirmed = true,
            IsActive = true,
            BusinessId = null
        };
        await users.CreateAsync(uNoBiz, "Test1!x");
        await users.AddToRoleAsync(uNoBiz, RoleNames.Admin);
        var rNoBiz = await ops.SendInvoiceEmailAsync(periodId, uNoBiz.Id, "sa");
        Assert.Equal("BusinessIdMismatch", rNoBiz.ErrorCode);

        db.Companies.Remove(db.Companies.First(c => c.Id == companyId));
        db.Companies.Add(new CompanyEntity { Id = companyId, CompanyId = "c2", BusinessId = null });
        await db.SaveChangesAsync();
        var uOkBiz = new ApplicationUser
        {
            UserName = "a4@test.com",
            Email = "a4@test.com",
            EmailConfirmed = true,
            IsActive = true,
            BusinessId = "BIZ-1"
        };
        await users.CreateAsync(uOkBiz, "Test1!x");
        await users.AddToRoleAsync(uOkBiz, RoleNames.Admin);
        var rCoNoBiz = await ops.SendInvoiceEmailAsync(periodId, uOkBiz.Id, "sa");
        Assert.Equal("BusinessIdMismatch", rCoNoBiz.ErrorCode);

        db.Companies.Remove(db.Companies.First(c => c.Id == companyId));
        db.Companies.Add(new CompanyEntity { Id = companyId, CompanyId = "c3", BusinessId = "BIZ-1" });
        await db.SaveChangesAsync();

        var uWrong = new ApplicationUser
        {
            UserName = "a5@test.com",
            Email = "a5@test.com",
            EmailConfirmed = true,
            IsActive = true,
            BusinessId = "OTHER"
        };
        await users.CreateAsync(uWrong, "Test1!x");
        await users.AddToRoleAsync(uWrong, RoleNames.Admin);
        var rWrong = await ops.SendInvoiceEmailAsync(periodId, uWrong.Id, "sa");
        Assert.Equal("RecipientWrongCompany", rWrong.ErrorCode);

        var uNoEmail = new ApplicationUser
        {
            UserName = "a6@test.com",
            Email = null,
            EmailConfirmed = true,
            IsActive = true,
            BusinessId = "BIZ-1"
        };
        await users.CreateAsync(uNoEmail, "Test1!x");
        await users.AddToRoleAsync(uNoEmail, RoleNames.Admin);
        var rNoEmail = await ops.SendInvoiceEmailAsync(periodId, uNoEmail.Id, "sa");
        Assert.Equal("RecipientNoEmail", rNoEmail.ErrorCode);
    }

    [Fact]
    public async Task SendInvoiceEmailAsync_success_sends_and_persists_audit()
    {
        using var sp = CreateServiceProvider(out _);
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRolesAsync(roles);

        var companyId = Guid.NewGuid();
        db.Companies.Add(new CompanyEntity { Id = companyId, CompanyId = Guid.NewGuid().ToString("N"), BusinessId = "BIZ-OK" });
        await db.SaveChangesAsync();

        var periodId = Guid.NewGuid();
        var mockR = new Mock<IAdminBillingReader>();
        mockR.Setup(x => x.GetInvoicePdfModelAsync(periodId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BillingInvoicePdfModel
            {
                PeriodId = periodId,
                CompanyId = companyId,
                CompanyName = "Acme",
                YearUtc = 2026,
                MonthUtc = 5,
                Currency = "EUR",
                Status = "Open",
                PayableTotal = 10m,
                LedgerTotal = 12m,
                Lines = Array.Empty<BillingInvoicePdfLineModel>()
            });

        var mockE = new Mock<IEmailSender>();
        var mockP = new Mock<IBillingInvoicePdfGenerator>();
        mockP.Setup(x => x.GeneratePdf(It.IsAny<BillingInvoicePdfModel>())).Returns(new byte[] { 9, 9 });

        var admin = new ApplicationUser
        {
            UserName = "adminok@test.com",
            Email = "adminok@test.com",
            EmailConfirmed = true,
            IsActive = true,
            BusinessId = "BIZ-OK"
        };
        await users.CreateAsync(admin, "Test1!x");
        await users.AddToRoleAsync(admin, RoleNames.Admin);

        var ops = CreateOps(db, users, mockR.Object, mockE.Object, mockP.Object);
        var r = await ops.SendInvoiceEmailAsync(periodId, admin.Id, "super-admin-id");
        Assert.True(r.Success);

        mockE.Verify(
            x => x.SendAsync(
                "adminok@test.com",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.Is<IReadOnlyList<EmailAttachment>>(a => a.Count == 1 && a[0].Content.Length == 2),
                It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.Equal(1, await db.SubscriptionInvoiceSends.CountAsync(s => s.CompanyBillingPeriodId == periodId));
    }

    [Fact]
    public async Task UpdateLineExcludedAsync_not_found()
    {
        using var sp = CreateServiceProvider(out _);
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var ops = CreateOps(db, users, Mock.Of<IAdminBillingReader>(), Mock.Of<IEmailSender>(), Mock.Of<IBillingInvoicePdfGenerator>());

        var r = await ops.UpdateLineExcludedAsync(Guid.NewGuid(), true, "sa");
        Assert.False(r.Success);
        Assert.Equal("NotFound", r.ErrorCode);
    }

    [Fact]
    public async Task UpdateLineExcludedAsync_updates_row()
    {
        using var sp = CreateServiceProvider(out _);
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var planId = Guid.NewGuid();
        var periodPricingId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
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
        db.Companies.Add(new CompanyEntity { Id = companyId, CompanyId = Guid.NewGuid().ToString("N") });
        var billingPeriodId = Guid.NewGuid();
        db.CompanyBillingPeriods.Add(new CompanyBillingPeriod
        {
            Id = billingPeriodId,
            CompanyId = companyId,
            YearUtc = 2026,
            MonthUtc = 6,
            Currency = "EUR",
            Status = CompanyBillingPeriodStatus.Open
        });
        var lineId = Guid.NewGuid();
        db.BillingLineItems.Add(new BillingLineItem
        {
            Id = lineId,
            CompanyBillingPeriodId = billingPeriodId,
            LineType = BillingLineType.Adjustment,
            Amount = 3m,
            Currency = "EUR",
            SubscriptionPlanId = planId,
            SubscriptionPlanPricingPeriodId = periodPricingId,
            CreatedAtUtc = DateTime.UtcNow,
            ExcludedFromInvoice = false
        });
        await db.SaveChangesAsync();

        var ops = CreateOps(db, users, Mock.Of<IAdminBillingReader>(), Mock.Of<IEmailSender>(), Mock.Of<IBillingInvoicePdfGenerator>());
        var r = await ops.UpdateLineExcludedAsync(lineId, true, "super-admin");
        Assert.True(r.Success);

        var line = await db.BillingLineItems.FirstAsync(l => l.Id == lineId);
        Assert.True(line.ExcludedFromInvoice);
        Assert.Equal("super-admin", line.InvoiceExclusionUpdatedByUserId);
        Assert.NotNull(line.InvoiceExclusionUpdatedAtUtc);
    }
}
