using System.Diagnostics;
using DotNetEnv;
using CargoHub.Api.Options;
using CargoHub.Api.Services;
using CargoHub.Application.Auth.Abstractions;
using CargoHub.Application.Auth.Handlers;
using CargoHub.Application.Billing;
using CargoHub.Application.Bookings;
using CargoHub.Application.AdminCompanies;
using CargoHub.Application.Company;
using CargoHub.Application.Subscriptions;
using CargoHub.Infrastructure.Auth;
using CargoHub.Infrastructure.Company;
using CargoHub.Infrastructure.Billing;
using CargoHub.Infrastructure.Couriers;
using CargoHub.Infrastructure.Identity;
using CargoHub.Infrastructure.Options;
using CargoHub.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

// Load .env from repo root before configuration (secrets stay out of appsettings)
var possibleEnvPaths = new[]
{
    Path.Combine(Directory.GetCurrentDirectory(), ".env"),
    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".env"),
    Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".env")
};
foreach (var p in possibleEnvPaths)
{
    var resolved = Path.GetFullPath(p);
    if (File.Exists(resolved))
    {
        Env.Load(resolved);
        break;
    }
}

// When running from Visual Studio (Development), ensure PostgreSQL in Docker is up so DB is ready.
if (string.Equals(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"), "Development", StringComparison.OrdinalIgnoreCase))
{
    var baseDir = AppContext.BaseDirectory;
    var dir = new DirectoryInfo(baseDir);
    for (var i = 0; i < 8 && dir != null; i++, dir = dir.Parent)
    {
        var composePath = Path.Combine(dir.FullName, "docker-compose.yml");
        if (File.Exists(composePath))
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "docker",
                        Arguments = "compose up -d --wait",
                        WorkingDirectory = dir.FullName,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit(TimeSpan.FromSeconds(120));
                break;
            }
            catch
            {
                // Docker not installed or not running; continue so app starts and fails on DB connect with a clear error
                break;
            }
        }
    }
}

var builder = WebApplication.CreateBuilder(args);

// In Development, always listen on 5299 so the portal (NEXT_PUBLIC_API_URL=http://localhost:5299) can reach the API.
// In Production (e.g. Render), use PORT from environment (default 8080 for local Docker).
if (builder.Environment.IsDevelopment())
    builder.WebHost.UseUrls("http://localhost:5299");
else
{
    var port = Environment.GetEnvironmentVariable("PORT") ?? "8080";
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Configure database connection for PostgreSQL.
// NOTE: update "DefaultConnection" in appsettings.json or user-secrets
// to point to your actual PostgreSQL instance.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                      ?? "Host=localhost;Port=5433;Database=portal;Username=postgres;Password=postgres";

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(connectionString, npgsql =>
        npgsql.MigrationsAssembly("CargoHub.Infrastructure"));
});

// Register infrastructure services that are shared across the application.
builder.Services.AddScoped<IJwtTokenFactory, JwtTokenFactory>();
builder.Services.Configure<PortalPublicOptions>(builder.Configuration.GetSection(PortalPublicOptions.SectionName));
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<IPortalCompanySubscriptionReader, PortalCompanySubscriptionReader>();
builder.Services.AddScoped<ICompanyAdminInviteRepository, CompanyAdminInviteRepository>();
builder.Services.AddScoped<ICompanyUserMetrics, CompanyUserMetrics>();
builder.Services.AddScoped<ICompanyAdminInviteIssuer, CompanyAdminInviteIssuer>();
builder.Services.AddScoped<IAcceptCompanyAdminInviteRunner, AcceptCompanyAdminInviteRunner>();
builder.Services.AddScoped<AdminCompanyUserPolicy>();
builder.Services.AddScoped<IAdminCompanyLimitUserOperations, AdminCompanyLimitUserOperations>();
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<ISubscriptionBillingOrchestrator, SubscriptionBillingOrchestrator>();
builder.Services.AddScoped<IImportFileMappingRepository, ImportFileMappingRepository>();
builder.Services.AddSingleton<BookingImportService>();
builder.Services.AddSingleton<BookingExportService>();
builder.Services.AddMemoryCache();
builder.Services.AddCourierClients(builder.Configuration);
builder.Services.AddSingleton<IPasswordResetTokenStore, PasswordResetTokenStore>();
builder.Services.AddSingleton<IVerificationCodeStore, VerificationCodeStore>();

// Register user registration and authentication services
builder.Services.AddScoped<IUserRegistrationService, UserRegistrationService>();
builder.Services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();

// Password reset and verification (Scope 02: Accounts and Auth)
builder.Services.AddScoped<IRequestPasswordResetRunner, RequestPasswordResetRunner>();
builder.Services.AddScoped<IResetPasswordRunner, ResetPasswordRunner>();
builder.Services.AddScoped<IVerifyEmailRunner, VerifyEmailRunner>();
builder.Services.AddScoped<IUpdateVerificationStatusRunner, UpdateVerificationStatusRunner>();

// Configure ASP.NET Core Identity so that authentication and user data
// are stored in the same PostgreSQL database as bookings.
builder.Services
    .AddIdentity<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole>(options =>
    {
        // Keep password rules reasonable; these can be tightened later
        // based on business requirements.
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configure JWT bearer authentication.
// The issuer, audience and signing key should be aligned with the existing
// Node.js token-utils implementation to keep clients compatible.
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "PortalIssuer";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "PortalAudience";
var jwtKey = builder.Configuration["Jwt:SigningKey"] ?? "PLEASE_CHANGE_ME_TO_A_LONG_SECURE_KEY";

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// Register MediatR for CQRS handlers living in the Application project.
builder.Services.AddMediatR(typeof(CargoHub.Application.AssemblyMarker).Assembly);

// CORS: allow the portal (e.g. Next.js dev server) to call the API.
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        var configuredOrigins = builder.Configuration.GetSection("Cors:PortalOrigins").Get<string[]>();
        if (configuredOrigins?.Length > 0)
        {
            policy.WithOrigins(configuredOrigins);
        }
        else
        {
            var single = builder.Configuration["Cors:PortalOrigin"];
            var origins = string.IsNullOrEmpty(single)
                ? new[] { "http://localhost:3000", "http://localhost:3001", "http://127.0.0.1:3000", "http://127.0.0.1:3001" }
                : new[] { single };
            policy.WithOrigins(origins);
        }
        policy.AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Branding: app name, logo, colors, waybill footer (single-tenant per deployment).
builder.Services.Configure<BrandingOptions>(builder.Configuration.GetSection(BrandingOptions.SectionName));
builder.Services.Configure<DailyDigestOptions>(builder.Configuration.GetSection(DailyDigestOptions.SectionName));
builder.Services.AddSingleton<WaybillPdfGenerator>();
builder.Services.AddSingleton<DailyBookingsDigestPdfGenerator>();
builder.Services.AddScoped<IDailyDigestSendLogRepository, DailyDigestSendLogRepository>();
builder.Services.AddScoped<IDailyBookingDigestOrchestrator, DailyBookingDigestOrchestrator>();
builder.Services.AddHostedService<DailyBookingDigestBackgroundService>();

// Register controllers; routes will later mirror the existing Node.js API surface.
builder.Services.AddControllers();

// Configure Swagger/OpenAPI for API exploration and documentation.
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// In Development, start the portal (Next.js) so "Start" in VS runs both API and UI.
if (app.Environment.IsDevelopment())
{
    var baseDir = AppContext.BaseDirectory;
    var solutionRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
    if (!Directory.Exists(Path.Combine(solutionRoot, "CargoHub.Api")))
        solutionRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", ".."));
    if (!Directory.Exists(Path.Combine(solutionRoot, "CargoHub.Api")))
        solutionRoot = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));
    var portalDir = Path.Combine(solutionRoot, "portal");
    if (Directory.Exists(portalDir))
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                var portalCmd = "/c \"cd /d \"" + portalDir + "\" && npm run dev\"";
                Process.Start(new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = portalCmd,
                    UseShellExecute = true,
                    WorkingDirectory = portalDir,
                    CreateNoWindow = false
                });
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "npm",
                    Arguments = "run dev",
                    UseShellExecute = false,
                    WorkingDirectory = portalDir,
                    CreateNoWindow = false
                });
            }
        }
        catch (Exception ex)
        {
            // Do not fail API startup if portal fails to start (e.g. npm not in PATH)
            Console.WriteLine("Portal could not be started: " + ex.Message);
        }
    }
}

// Run all pending EF Core migrations on every startup so the DB is always up to date.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var pending = db.Database.GetPendingMigrations().ToList();
    if (pending.Count > 0)
        Console.WriteLine("Applying {0} pending migration(s): {1}", pending.Count, string.Join(", ", pending));
    db.Database.Migrate();
    // Ensure critical columns exist even if a migration was skipped or DB was restored (idempotent).
    db.EnsureCriticalSchema();

    SubscriptionPlanSeed.EnsureDefaultTrialPlanAsync(db).GetAwaiter().GetResult();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    foreach (var roleName in new[] { CargoHub.Application.Auth.RoleNames.SuperAdmin, CargoHub.Application.Auth.RoleNames.Admin, CargoHub.Application.Auth.RoleNames.User })
    {
        if (roleManager.RoleExistsAsync(roleName).GetAwaiter().GetResult()) continue;
        roleManager.CreateAsync(new IdentityRole(roleName)).GetAwaiter().GetResult();
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

if (app.Environment.IsDevelopment())
{
    Console.WriteLine("API listening at http://localhost:5299");
    Console.WriteLine("Portal: set NEXT_PUBLIC_API_URL=http://localhost:5299 in portal/.env.local");
    Console.WriteLine("CORS allows: localhost:3000, localhost:3001, 127.0.0.1:3000, 127.0.0.1:3001");
}

app.Run();
