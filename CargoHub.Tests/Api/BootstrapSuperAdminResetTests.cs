using CargoHub.Api;
using CargoHub.Application.Auth;
using CargoHub.Infrastructure.Identity;
using CargoHub.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace CargoHub.Tests.Api;

public class BootstrapSuperAdminResetTests
{
    [Fact]
    public async Task ExecuteAsync_WhenNoSuperAdmins_ReturnsZeros()
    {
        using var sp = CreateServices();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureRolesAsync(sp);

        var result = await BootstrapSuperAdminReset.ExecuteAsync(userManager, deleteSuperAdminUsers: false);

        Assert.Equal((0, 0), result);
    }

    [Fact]
    public async Task ExecuteAsync_RemoveRoleOnly_LeavesUserWithoutSuperAdmin()
    {
        using var sp = CreateServices();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureRolesAsync(sp);

        var user = new ApplicationUser
        {
            UserName = "sa1@example.com",
            Email = "sa1@example.com",
            DisplayName = "SA",
            EmailConfirmed = true,
            IsActive = true
        };
        await userManager.CreateAsync(user, "Test1!x");
        await userManager.AddToRoleAsync(user, RoleNames.SuperAdmin);

        var (cleared, deleted) = await BootstrapSuperAdminReset.ExecuteAsync(userManager, deleteSuperAdminUsers: false);

        Assert.Equal(1, cleared);
        Assert.Equal(0, deleted);
        var reloaded = await userManager.FindByIdAsync(user.Id);
        Assert.NotNull(reloaded);
        var roles = await userManager.GetRolesAsync(reloaded);
        Assert.DoesNotContain(RoleNames.SuperAdmin, roles);
    }

    [Fact]
    public async Task ExecuteAsync_DeleteUsers_RemovesUsers()
    {
        using var sp = CreateServices();
        var userManager = sp.GetRequiredService<UserManager<ApplicationUser>>();
        await EnsureRolesAsync(sp);

        var user = new ApplicationUser
        {
            UserName = "sa2@example.com",
            Email = "sa2@example.com",
            DisplayName = "SA2",
            EmailConfirmed = true,
            IsActive = true
        };
        await userManager.CreateAsync(user, "Test1!x");
        await userManager.AddToRoleAsync(user, RoleNames.SuperAdmin);

        var (cleared, deleted) = await BootstrapSuperAdminReset.ExecuteAsync(userManager, deleteSuperAdminUsers: true);

        Assert.Equal(1, cleared);
        Assert.Equal(1, deleted);
        Assert.Null(await userManager.FindByEmailAsync("sa2@example.com"));
    }

    private static async Task EnsureRolesAsync(ServiceProvider sp)
    {
        var roleManager = sp.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var name in new[] { RoleNames.SuperAdmin, RoleNames.Admin, RoleNames.User })
        {
            if (!await roleManager.RoleExistsAsync(name))
                await roleManager.CreateAsync(new IdentityRole(name));
        }
    }

    private static ServiceProvider CreateServices()
    {
        var dbName = Guid.NewGuid().ToString("N");
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<ApplicationDbContext>(o => o.UseInMemoryDatabase(dbName));
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
        using (var scope = sp.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
        }

        return sp;
    }
}
