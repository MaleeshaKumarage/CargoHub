using CargoHub.Application.AdminEmail;
using CargoHub.Application.Auth;
using CargoHub.Application.Company;
using CargoHub.Application.Couriers;
using CompanyEntity = CargoHub.Domain.Companies.Company;
using CargoHub.Infrastructure.AdminEmail;
using CargoHub.Infrastructure.Identity;
using CargoHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace CargoHub.Tests.AdminEmail;

public sealed class AdminReleaseNotesBroadcasterTests
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

    [Fact]
    public async Task TryBroadcastAsync_empty_subject_returns_error()
    {
        using var sp = CreateServiceProvider(out _);
        using var scope = sp.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var mockC = new Mock<ICompanyRepository>();
        var mockE = new Mock<IEmailSender>();
        var broadcaster = new AdminReleaseNotesBroadcaster(users, mockC.Object, mockE.Object);

        var (result, err) = await broadcaster.TryBroadcastAsync(new ReleaseNotesBroadcastRequest
        {
            Subject = "  ",
            BodyPlain = "hi",
            AllCompanies = true,
            AllRoles = true,
        });

        Assert.Null(result);
        Assert.Equal("Subject is required.", err);
        mockE.Verify(
            x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task TryBroadcastAsync_filters_inactive_and_company_and_role()
    {
        using var sp = CreateServiceProvider(out _);
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRolesAsync(roles);

        var companyId = Guid.NewGuid();
        db.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            CompanyId = Guid.NewGuid().ToString("N"),
            Name = "Co",
            BusinessId = "BIZ100",
        });
        await db.SaveChangesAsync();

        async Task<string> AddUserAsync(string email, string businessId, string role, bool active)
        {
            var u = new ApplicationUser
            {
                UserName = email,
                Email = email,
                BusinessId = businessId,
                IsActive = active,
                DisplayName = email,
            };
            var r = await users.CreateAsync(u, "Test12!");
            Assert.True(r.Succeeded);
            await users.AddToRoleAsync(u, role);
            return u.Id;
        }

        await AddUserAsync("active-admin@x.com", "BIZ100", RoleNames.Admin, active: true);
        await AddUserAsync("inactive@x.com", "BIZ100", RoleNames.Admin, active: false);
        await AddUserAsync("wrong-co@x.com", "OTHER", RoleNames.Admin, active: true);
        await AddUserAsync("user-only@x.com", "BIZ100", RoleNames.User, active: true);

        var companyRow = await db.Companies.FindAsync(companyId);
        Assert.NotNull(companyRow);
        var mockC = new Mock<ICompanyRepository>();
        mockC.Setup(x => x.GetByIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(companyRow);

        var sent = new List<(string To, string Subject, string Html)>();
        var mockE = new Mock<IEmailSender>();
        mockE
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, string, CancellationToken>((to, subj, html, _) => sent.Add((to, subj, html)))
            .Returns(Task.CompletedTask);

        var broadcaster = new AdminReleaseNotesBroadcaster(users, mockC.Object, mockE.Object);
        var (result, err) = await broadcaster.TryBroadcastAsync(new ReleaseNotesBroadcastRequest
        {
            Subject = "Rel",
            BodyPlain = "Line1\n\nLine2",
            AllCompanies = false,
            CompanyIds = new[] { companyId },
            AllRoles = false,
            Roles = new[] { RoleNames.Admin },
        });

        Assert.Null(err);
        Assert.NotNull(result);
        Assert.Equal(1, result!.RecipientCount);
        Assert.Equal(1, result.SentCount);
        Assert.Single(sent);
        Assert.Equal("active-admin@x.com", sent[0].To);
        Assert.Equal("Rel", sent[0].Subject);
        Assert.Contains("white-space: pre-wrap", sent[0].Html);
        Assert.Contains("Line1", sent[0].Html);
    }

    [Fact]
    public async Task TryBroadcastAsync_unknown_company_returns_error()
    {
        using var sp = CreateServiceProvider(out _);
        using var scope = sp.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var missingId = Guid.NewGuid();
        var mockC = new Mock<ICompanyRepository>();
        mockC.Setup(x => x.GetByIdAsync(missingId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CompanyEntity?)null);

        var broadcaster = new AdminReleaseNotesBroadcaster(users, mockC.Object, Mock.Of<IEmailSender>());
        var (result, err) = await broadcaster.TryBroadcastAsync(new ReleaseNotesBroadcastRequest
        {
            Subject = "S",
            BodyPlain = "B",
            AllCompanies = false,
            CompanyIds = new[] { missingId },
            AllRoles = true,
        });

        Assert.Null(result);
        Assert.Contains("Company not found", err);
    }

    [Fact]
    public async Task TryBroadcastAsync_no_recipients_returns_error()
    {
        using var sp = CreateServiceProvider(out _);
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleMgr = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRolesAsync(roleMgr);

        var companyId = Guid.NewGuid();
        db.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            CompanyId = Guid.NewGuid().ToString("N"),
            Name = "Co",
            BusinessId = "EMPTYBIZ",
        });
        await db.SaveChangesAsync();

        var mockC = new Mock<ICompanyRepository>();
        mockC.Setup(x => x.GetByIdAsync(companyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(db.Companies.First(c => c.Id == companyId));

        var broadcaster = new AdminReleaseNotesBroadcaster(users, mockC.Object, Mock.Of<IEmailSender>());
        var (result, err) = await broadcaster.TryBroadcastAsync(new ReleaseNotesBroadcastRequest
        {
            Subject = "S",
            BodyPlain = "B",
            AllCompanies = false,
            CompanyIds = new[] { companyId },
            AllRoles = true,
        });

        Assert.Null(result);
        Assert.Equal("No recipients match the selected filters.", err);
    }

    [Fact]
    public async Task TryBroadcastAsync_whitespace_body_returns_error()
    {
        using var sp = CreateServiceProvider(out _);
        using var scope = sp.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var broadcaster = new AdminReleaseNotesBroadcaster(users, Mock.Of<ICompanyRepository>(), Mock.Of<IEmailSender>());
        var (result, err) = await broadcaster.TryBroadcastAsync(new ReleaseNotesBroadcastRequest
        {
            Subject = "Ok",
            BodyPlain = "   ",
            AllCompanies = true,
            AllRoles = true,
        });
        Assert.Null(result);
        Assert.Equal("Body is required.", err);
    }

    [Fact]
    public async Task TryBroadcastAsync_oversized_body_returns_error()
    {
        using var sp = CreateServiceProvider(out _);
        using var scope = sp.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var broadcaster = new AdminReleaseNotesBroadcaster(users, Mock.Of<ICompanyRepository>(), Mock.Of<IEmailSender>());
        var (result, err) = await broadcaster.TryBroadcastAsync(new ReleaseNotesBroadcastRequest
        {
            Subject = "Ok",
            BodyPlain = new string('x', 100_001),
            AllCompanies = true,
            AllRoles = true,
        });
        Assert.Null(result);
        Assert.Contains("Body exceeds maximum length", err);
    }

    [Fact]
    public async Task TryBroadcastAsync_no_company_ids_when_not_all_companies_returns_error()
    {
        using var sp = CreateServiceProvider(out _);
        using var scope = sp.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var broadcaster = new AdminReleaseNotesBroadcaster(users, Mock.Of<ICompanyRepository>(), Mock.Of<IEmailSender>());
        var (result, err) = await broadcaster.TryBroadcastAsync(new ReleaseNotesBroadcastRequest
        {
            Subject = "S",
            BodyPlain = "B",
            AllCompanies = false,
            CompanyIds = Array.Empty<Guid>(),
            AllRoles = true,
        });
        Assert.Null(result);
        Assert.Contains("Select at least one company", err);
    }

    [Fact]
    public async Task TryBroadcastAsync_company_without_business_id_returns_error()
    {
        using var sp = CreateServiceProvider(out _);
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var companyId = Guid.NewGuid();
        db.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            CompanyId = Guid.NewGuid().ToString("N"),
            Name = "No Biz",
            BusinessId = "  ",
        });
        await db.SaveChangesAsync();
        var row = await db.Companies.FindAsync(companyId);
        Assert.NotNull(row);
        var mockC = new Mock<ICompanyRepository>();
        mockC.Setup(x => x.GetByIdAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync(row);
        var broadcaster = new AdminReleaseNotesBroadcaster(users, mockC.Object, Mock.Of<IEmailSender>());
        var (result, err) = await broadcaster.TryBroadcastAsync(new ReleaseNotesBroadcastRequest
        {
            Subject = "S",
            BodyPlain = "B",
            AllCompanies = false,
            CompanyIds = new[] { companyId },
            AllRoles = true,
        });
        Assert.Null(result);
        Assert.Contains("no business ID", err);
    }

    [Fact]
    public async Task TryBroadcastAsync_no_roles_when_not_all_roles_returns_error()
    {
        using var sp = CreateServiceProvider(out _);
        using var scope = sp.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var broadcaster = new AdminReleaseNotesBroadcaster(users, Mock.Of<ICompanyRepository>(), Mock.Of<IEmailSender>());
        var (result, err) = await broadcaster.TryBroadcastAsync(new ReleaseNotesBroadcastRequest
        {
            Subject = "S",
            BodyPlain = "B",
            AllCompanies = true,
            AllRoles = false,
            Roles = Array.Empty<string>(),
        });
        Assert.Null(result);
        Assert.Contains("Select at least one role", err);
    }

    [Fact]
    public async Task TryBroadcastAsync_invalid_role_returns_error()
    {
        using var sp = CreateServiceProvider(out _);
        using var scope = sp.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var broadcaster = new AdminReleaseNotesBroadcaster(users, Mock.Of<ICompanyRepository>(), Mock.Of<IEmailSender>());
        var (result, err) = await broadcaster.TryBroadcastAsync(new ReleaseNotesBroadcastRequest
        {
            Subject = "S",
            BodyPlain = "B",
            AllCompanies = true,
            AllRoles = false,
            Roles = new[] { "NotARealRole" },
        });
        Assert.Null(result);
        Assert.Contains("Invalid role", err);
    }

    [Fact]
    public async Task TryBroadcastAsync_email_send_failure_is_recorded()
    {
        using var sp = CreateServiceProvider(out _);
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRolesAsync(roles);

        var companyId = Guid.NewGuid();
        db.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            CompanyId = Guid.NewGuid().ToString("N"),
            Name = "Co",
            BusinessId = "B200",
        });
        await db.SaveChangesAsync();

        var u = new ApplicationUser
        {
            UserName = "u@x.com",
            Email = "u@x.com",
            BusinessId = "B200",
            IsActive = true,
            DisplayName = "U",
        };
        Assert.True((await users.CreateAsync(u, "Test12!")).Succeeded);
        await users.AddToRoleAsync(u, RoleNames.User);

        var companyRow = await db.Companies.FindAsync(companyId);
        Assert.NotNull(companyRow);
        var mockC = new Mock<ICompanyRepository>();
        mockC.Setup(x => x.GetByIdAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync(companyRow);

        var mockE = new Mock<IEmailSender>();
        mockE
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("smtp down"));

        var broadcaster = new AdminReleaseNotesBroadcaster(users, mockC.Object, mockE.Object);
        var (result, err) = await broadcaster.TryBroadcastAsync(new ReleaseNotesBroadcastRequest
        {
            Subject = "S",
            BodyPlain = "B",
            AllCompanies = false,
            CompanyIds = new[] { companyId },
            AllRoles = true,
        });

        Assert.Null(err);
        Assert.NotNull(result);
        Assert.Equal(1, result!.RecipientCount);
        Assert.Equal(0, result.SentCount);
        Assert.Single(result.Failures);
        Assert.Equal("u@x.com", result.Failures[0].Email);
        Assert.Contains("smtp down", result.Failures[0].Message);
    }

    [Fact]
    public async Task TryBroadcastAsync_ignores_empty_guid_in_company_list()
    {
        using var sp = CreateServiceProvider(out _);
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRolesAsync(roles);

        var companyId = Guid.NewGuid();
        db.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            CompanyId = Guid.NewGuid().ToString("N"),
            Name = "Co",
            BusinessId = "B300",
        });
        await db.SaveChangesAsync();

        var u = new ApplicationUser
        {
            UserName = "r@x.com",
            Email = "r@x.com",
            BusinessId = "B300",
            IsActive = true,
            DisplayName = "R",
        };
        Assert.True((await users.CreateAsync(u, "Test12!")).Succeeded);
        await users.AddToRoleAsync(u, RoleNames.User);

        var companyRow = await db.Companies.FindAsync(companyId);
        Assert.NotNull(companyRow);
        var mockC = new Mock<ICompanyRepository>();
        mockC.Setup(x => x.GetByIdAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync(companyRow);

        var mockE = new Mock<IEmailSender>();
        mockE
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var broadcaster = new AdminReleaseNotesBroadcaster(users, mockC.Object, mockE.Object);
        var (result, err) = await broadcaster.TryBroadcastAsync(new ReleaseNotesBroadcastRequest
        {
            Subject = "S",
            BodyPlain = "B",
            AllCompanies = false,
            CompanyIds = new[] { Guid.Empty, companyId },
            AllRoles = true,
        });

        Assert.Null(err);
        Assert.NotNull(result);
        Assert.Equal(1, result!.SentCount);
        mockC.Verify(x => x.GetByIdAsync(companyId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TryBroadcastAsync_sends_once_per_distinct_email()
    {
        using var sp = CreateServiceProvider(out _);
        using var scope = sp.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        await EnsureRolesAsync(roles);

        var companyId = Guid.NewGuid();
        db.Companies.Add(new CompanyEntity
        {
            Id = companyId,
            CompanyId = Guid.NewGuid().ToString("N"),
            Name = "Co",
            BusinessId = "B400",
        });
        await db.SaveChangesAsync();

        async Task AddAsync(string email, string userName)
        {
            var x = new ApplicationUser
            {
                UserName = userName,
                Email = email,
                BusinessId = "B400",
                IsActive = true,
                DisplayName = userName,
            };
            Assert.True((await users.CreateAsync(x, "Test12!")).Succeeded);
            await users.AddToRoleAsync(x, RoleNames.Admin);
        }

        await AddAsync("dup@x.com", "u1");
        await AddAsync("DUP@x.com", "u2");

        var companyRow = await db.Companies.FindAsync(companyId);
        Assert.NotNull(companyRow);
        var mockC = new Mock<ICompanyRepository>();
        mockC.Setup(x => x.GetByIdAsync(companyId, It.IsAny<CancellationToken>())).ReturnsAsync(companyRow);

        var mockE = new Mock<IEmailSender>();
        mockE
            .Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var broadcaster = new AdminReleaseNotesBroadcaster(users, mockC.Object, mockE.Object);
        var (result, err) = await broadcaster.TryBroadcastAsync(new ReleaseNotesBroadcastRequest
        {
            Subject = "S",
            BodyPlain = "B",
            AllCompanies = false,
            CompanyIds = new[] { companyId },
            AllRoles = true,
        });

        Assert.Null(err);
        Assert.NotNull(result);
        Assert.Equal(1, result!.RecipientCount);
        Assert.Equal(1, result.SentCount);
        mockE.Verify(
            x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
