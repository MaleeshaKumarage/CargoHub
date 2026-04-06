using CargoHub.Domain.Billing;
using CargoHub.Domain.Bookings;
using CargoHub.Domain.Companies;
using CargoHub.Infrastructure.Identity;
using CompanyEntity = CargoHub.Domain.Companies.Company;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
namespace CargoHub.Infrastructure.Persistence;

/// <summary>
/// Central EF Core DbContext for the application.
/// Combines ASP.NET Core Identity tables with domain aggregates
/// so that authentication/authorization and business data share the same database.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    /// <summary>
    /// Bookings created in the system.
    /// </summary>
    public DbSet<Booking> Bookings => Set<Booking>();

    /// <summary>
    /// When each booking status (Draft, Waybill, etc.) was reached. Separate table for tracking.
    /// </summary>
    public DbSet<BookingStatusHistory> BookingStatusHistory => Set<BookingStatusHistory>();

    /// <summary>
    /// Company configuration and address book entries.
    /// </summary>
    public DbSet<CompanyEntity> Companies => Set<CompanyEntity>();

    /// <summary>Pending company admin invitations (email link + token).</summary>
    public DbSet<CompanyAdminInvite> CompanyAdminInvites => Set<CompanyAdminInvite>();

    /// <summary>Saved booking import column maps per company and file layout.</summary>
    public DbSet<BookingImportFileMapping> BookingImportFileMappings => Set<BookingImportFileMapping>();

    /// <summary>Idempotency for daily per-company booking digest emails.</summary>
    public DbSet<DailyDigestSendLog> DailyDigestSendLogs => Set<DailyDigestSendLog>();

    public DbSet<SubscriptionPlan> SubscriptionPlans => Set<SubscriptionPlan>();

    public DbSet<SubscriptionPlanPricingPeriod> SubscriptionPlanPricingPeriods => Set<SubscriptionPlanPricingPeriod>();

    public DbSet<SubscriptionPlanPricingTier> SubscriptionPlanPricingTiers => Set<SubscriptionPlanPricingTier>();

    public DbSet<CompanyBillingPeriod> CompanyBillingPeriods => Set<CompanyBillingPeriod>();

    public DbSet<BillingLineItem> BillingLineItems => Set<BillingLineItem>();

    public DbSet<SubscriptionInvoiceSend> SubscriptionInvoiceSends => Set<SubscriptionInvoiceSend>();

    public DbSet<CompanySubscriptionAssignment> CompanySubscriptionAssignments => Set<CompanySubscriptionAssignment>();

    public DbSet<BillingPeriodExcludedBooking> BillingPeriodExcludedBookings => Set<BillingPeriodExcludedBooking>();

    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Configure entity mappings and schema details.
    /// For now we only define a minimal Booking structure; this can be refined
    /// as we port more logic from the existing Node.js backend.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Identity tables in auth schema
        builder.Entity<ApplicationUser>().ToTable("AspNetUsers", DbSchemas.Auth);
        builder.Entity<IdentityRole>().ToTable("AspNetRoles", DbSchemas.Auth);
        builder.Entity<IdentityUserRole<string>>().ToTable("AspNetUserRoles", DbSchemas.Auth);
        builder.Entity<IdentityUserClaim<string>>().ToTable("AspNetUserClaims", DbSchemas.Auth);
        builder.Entity<IdentityUserLogin<string>>().ToTable("AspNetUserLogins", DbSchemas.Auth);
        builder.Entity<IdentityUserToken<string>>().ToTable("AspNetUserTokens", DbSchemas.Auth);
        builder.Entity<IdentityRoleClaim<string>>().ToTable("AspNetRoleClaims", DbSchemas.Auth);

        builder.Entity<Booking>(entity =>
        {
            entity.ToTable("Bookings", DbSchemas.Bookings);
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Id)
                  .ValueGeneratedOnAdd();

            entity.Property(b => b.CustomerId)
                  .IsRequired()
                  .HasMaxLength(64);

            entity.Property(b => b.ShipmentNumber)
                  .HasMaxLength(64);

            entity.Property(b => b.CreatedAtUtc)
                  .IsRequired();

            entity.Property(b => b.Enabled)
                  .IsRequired();

            entity.Property(b => b.IsDraft)
                  .IsRequired();

            entity.HasIndex(b => new { b.CustomerId, b.Enabled, b.CreatedAtUtc })
                  .HasDatabaseName("IX_Bookings_Customer_Enabled_CreatedAt");

            entity.HasIndex(b => new { b.CustomerId, b.IsDraft, b.CreatedAtUtc })
                  .HasDatabaseName("IX_Bookings_Customer_IsDraft_CreatedAt");

            entity.HasIndex(b => b.ShipmentNumber)
                  .HasDatabaseName("IX_Bookings_ShipmentNumber");

            entity.Property(b => b.CompanyId);
            entity.Property(b => b.FirstBillableAtUtc);
            entity.HasIndex(b => new { b.CompanyId, b.CreatedAtUtc })
                  .HasDatabaseName("IX_Bookings_CompanyId_CreatedAtUtc");
            entity.HasIndex(b => new { b.CompanyId, b.FirstBillableAtUtc })
                  .HasDatabaseName("IX_Bookings_CompanyId_FirstBillableAtUtc");
            entity.HasOne<CompanyEntity>()
                  .WithMany()
                  .HasForeignKey(b => b.CompanyId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.OwnsOne(b => b.Header);
            entity.OwnsOne(b => b.Shipment);
            entity.OwnsOne(b => b.Shipper);
            entity.OwnsOne(b => b.Receiver);
            entity.OwnsOne(b => b.Payer);
            entity.OwnsOne(b => b.PickUpAddress);
            entity.OwnsOne(b => b.DeliveryPoint);
            entity.OwnsOne(b => b.ShippingInfo);
            entity.OwnsMany(b => b.Packages, pkg =>
            {
                pkg.WithOwner().HasForeignKey("BookingId");
                pkg.Property(p => p.Id);
                pkg.HasKey("BookingId", "Id");
            });
            entity.OwnsMany(b => b.Updates, upd =>
            {
                upd.WithOwner().HasForeignKey("BookingId");
                upd.Property<int>("Id");
                upd.HasKey("BookingId", "Id");
            });
        });

        builder.Entity<BookingStatusHistory>(entity =>
        {
            entity.ToTable("BookingStatusHistory", DbSchemas.Bookings);
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Id).ValueGeneratedOnAdd();
            entity.Property(x => x.Status).IsRequired().HasMaxLength(32);
            entity.Property(x => x.Source).HasMaxLength(64);
            entity.HasIndex(x => x.BookingId).HasDatabaseName("IX_BookingStatusHistory_BookingId");
            entity.HasIndex(x => new { x.BookingId, x.Status }).HasDatabaseName("IX_BookingStatusHistory_BookingId_Status");
        });

        builder.Entity<CompanyEntity>(entity =>
        {
            entity.ToTable("Companies", DbSchemas.Companies);
            entity.HasKey(c => c.Id);
            entity.Property(c => c.CompanyId)
                  .IsRequired()
                  .HasMaxLength(64);

            entity.Property(c => c.CustomerId)
                  .HasMaxLength(64);

            entity.Property(c => c.Counter)
                  .IsRequired();

            entity.HasIndex(c => c.CompanyId)
                  .HasDatabaseName("IX_Companies_CompanyId");

            entity.OwnsOne(c => c.Configurations);

            entity.OwnsOne(c => c.DefaultShipperAddress);

            entity.OwnsMany(c => c.SenderAddressBook, address =>
            {
                address.WithOwner().HasForeignKey("CompanyId");
                address.Property(a => a.Id).ValueGeneratedOnAdd();
                address.HasKey(a => a.Id);
            });

            entity.OwnsMany(c => c.AddressBook, address =>
            {
                address.WithOwner().HasForeignKey("CompanyId");
                address.Property(a => a.Id).ValueGeneratedOnAdd();
                address.HasKey(a => a.Id);
            });

            entity.OwnsMany(c => c.PickUpAddressBook, address =>
            {
                address.WithOwner().HasForeignKey("CompanyId");
                address.Property(a => a.Id).ValueGeneratedOnAdd();
                address.HasKey(a => a.Id);
            });

            entity.OwnsMany(c => c.AgreementNumbers, agreement =>
            {
                agreement.WithOwner().HasForeignKey("CompanyId");
                agreement.Property(a => a.Id).ValueGeneratedOnAdd();
                agreement.HasKey(a => a.Id);
            });

            entity.Property(c => c.MaxUserAccounts);
            entity.Property(c => c.MaxAdminAccounts);
            entity.Property(c => c.InitialAdminInviteEmail).HasMaxLength(256);
            entity.Property(c => c.InitialAdminInviteEmailsJson);
            entity.Property(c => c.SubscriptionPlanId);
            entity.Property(c => c.IsActive).IsRequired();
            entity.Property(c => c.AdminInviteFirstAcceptedAtUtc);
            entity.HasOne<SubscriptionPlan>()
                  .WithMany()
                  .HasForeignKey(c => c.SubscriptionPlanId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasIndex(c => c.SubscriptionPlanId)
                  .HasDatabaseName("IX_Companies_SubscriptionPlanId");
        });

        builder.Entity<CompanyAdminInvite>(entity =>
        {
            entity.ToTable("CompanyAdminInvites", DbSchemas.Companies);
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).IsRequired().HasMaxLength(256);
            entity.Property(x => x.NormalizedEmail).IsRequired().HasMaxLength(256);
            entity.Property(x => x.TokenHash).IsRequired().HasMaxLength(64);
            entity.Property(x => x.ExpiresAt).IsRequired();
            entity.Property(x => x.CreatedAt).IsRequired();
            entity.HasIndex(x => x.TokenHash).IsUnique().HasDatabaseName("IX_CompanyAdminInvites_TokenHash");
            entity.HasIndex(x => x.CompanyId).HasDatabaseName("IX_CompanyAdminInvites_CompanyId");
            entity.HasOne(x => x.Company)
                .WithMany()
                .HasForeignKey(x => x.CompanyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<BookingImportFileMapping>(entity =>
        {
            entity.ToTable("BookingImportFileMappings", DbSchemas.Companies);
            entity.HasKey(x => x.Id);
            entity.Property(x => x.FileNameKey).IsRequired().HasMaxLength(512);
            entity.Property(x => x.HeaderSignature).IsRequired().HasMaxLength(8000);
            entity.Property(x => x.ColumnMapJson).IsRequired();
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.UpdatedAtUtc).IsRequired();
            entity.HasIndex(x => new { x.CompanyId, x.FileNameKey, x.HeaderSignature })
                .IsUnique()
                .HasDatabaseName("IX_BookingImportFileMappings_Company_File_Headers");
            entity.HasOne<CompanyEntity>()
                  .WithMany()
                  .HasForeignKey(x => x.CompanyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<DailyDigestSendLog>(entity =>
        {
            entity.ToTable("DailyDigestSendLogs", DbSchemas.Bookings);
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TimeZoneId).IsRequired().HasMaxLength(128);
            entity.Property(x => x.DigestDateLocal).HasColumnType("date");
            entity.Property(x => x.SentAtUtc).IsRequired();
            entity.HasIndex(x => new { x.CompanyId, x.DigestDateLocal, x.TimeZoneId })
                .IsUnique()
                .HasDatabaseName("IX_DailyDigestSendLogs_Company_Date_Tz");
            entity.HasOne<CompanyEntity>()
                  .WithMany()
                  .HasForeignKey(x => x.CompanyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SubscriptionPlan>(entity =>
        {
            entity.ToTable("SubscriptionPlans", DbSchemas.Companies);
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(256);
            entity.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            entity.Property(x => x.Kind).HasConversion<int>();
            entity.Property(x => x.ChargeTimeAnchor).HasConversion<int>();
            entity.HasIndex(x => x.IsActive).HasDatabaseName("IX_SubscriptionPlans_IsActive");
        });

        builder.Entity<SubscriptionPlanPricingPeriod>(entity =>
        {
            entity.ToTable("SubscriptionPlanPricingPeriods", DbSchemas.Companies);
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EffectiveFromUtc).IsRequired();
            entity.Property(x => x.ChargePerBooking).HasPrecision(18, 4);
            entity.Property(x => x.MonthlyFee).HasPrecision(18, 4);
            entity.Property(x => x.OverageChargePerBooking).HasPrecision(18, 4);
            entity.HasIndex(x => new { x.SubscriptionPlanId, x.EffectiveFromUtc })
                  .IsUnique()
                  .HasDatabaseName("IX_SubscriptionPlanPricingPeriods_Plan_EffectiveFrom");
            entity.HasOne(x => x.SubscriptionPlan)
                  .WithMany(p => p.PricingPeriods)
                  .HasForeignKey(x => x.SubscriptionPlanId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SubscriptionPlanPricingTier>(entity =>
        {
            entity.ToTable("SubscriptionPlanPricingTiers", DbSchemas.Companies);
            entity.HasKey(x => x.Id);
            entity.Property(x => x.ChargePerBooking).HasPrecision(18, 4);
            entity.Property(x => x.MonthlyFee).HasPrecision(18, 4);
            entity.HasIndex(x => new { x.SubscriptionPlanPricingPeriodId, x.Ordinal })
                  .IsUnique()
                  .HasDatabaseName("IX_SubscriptionPlanPricingTiers_Period_Ordinal");
            entity.HasOne(x => x.PricingPeriod)
                  .WithMany(p => p.Tiers)
                  .HasForeignKey(x => x.SubscriptionPlanPricingPeriodId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CompanyBillingPeriod>(entity =>
        {
            entity.ToTable("CompanyBillingPeriods", DbSchemas.Companies);
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.HasIndex(x => new { x.CompanyId, x.YearUtc, x.MonthUtc })
                  .IsUnique()
                  .HasDatabaseName("IX_CompanyBillingPeriods_Company_Year_Month");
            entity.HasOne<CompanyEntity>()
                  .WithMany()
                  .HasForeignKey(x => x.CompanyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<CompanySubscriptionAssignment>(entity =>
        {
            entity.ToTable("CompanySubscriptionAssignments", DbSchemas.Companies);
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EffectiveFromUtc).IsRequired();
            entity.Property(x => x.SetByUserId).HasMaxLength(128);
            entity.HasIndex(x => new { x.CompanyId, x.EffectiveFromUtc })
                  .HasDatabaseName("IX_CompanySubscriptionAssignments_Company_EffectiveFrom");
            entity.HasOne<CompanyEntity>()
                  .WithMany()
                  .HasForeignKey(x => x.CompanyId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<BillingPeriodExcludedBooking>(entity =>
        {
            entity.ToTable("BillingPeriodExcludedBookings", DbSchemas.Companies);
            entity.HasKey(x => new { x.CompanyBillingPeriodId, x.BookingId });
            entity.HasIndex(x => x.BookingId).HasDatabaseName("IX_BillingPeriodExcludedBookings_BookingId");
            entity.HasOne(x => x.CompanyBillingPeriod)
                  .WithMany(p => p.ExcludedBookings)
                  .HasForeignKey(x => x.CompanyBillingPeriodId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Booking>()
                  .WithMany()
                  .HasForeignKey(x => x.BookingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<BillingLineItem>(entity =>
        {
            entity.ToTable("BillingLineItems", DbSchemas.Companies);
            entity.HasKey(x => x.Id);
            entity.Property(x => x.LineType).HasConversion<int>();
            entity.Property(x => x.Component).HasMaxLength(64);
            entity.Property(x => x.Amount).HasPrecision(18, 4);
            entity.Property(x => x.Currency).IsRequired().HasMaxLength(3);
            entity.Property(x => x.CreatedAtUtc).IsRequired();
            entity.Property(x => x.InvoiceExclusionUpdatedByUserId).HasMaxLength(128);
            entity.HasIndex(x => x.CompanyBillingPeriodId).HasDatabaseName("IX_BillingLineItems_CompanyBillingPeriodId");
            entity.HasIndex(x => x.BookingId).HasDatabaseName("IX_BillingLineItems_BookingId");
            entity.HasOne(x => x.CompanyBillingPeriod)
                  .WithMany(p => p.LineItems)
                  .HasForeignKey(x => x.CompanyBillingPeriodId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<Booking>()
                  .WithMany()
                  .HasForeignKey(x => x.BookingId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<SubscriptionInvoiceSend>(entity =>
        {
            entity.ToTable("SubscriptionInvoiceSends", DbSchemas.Companies);
            entity.HasKey(x => x.Id);
            entity.Property(x => x.SentBySuperAdminUserId).IsRequired().HasMaxLength(128);
            entity.Property(x => x.RecipientAdminUserId).IsRequired().HasMaxLength(128);
            entity.Property(x => x.RecipientEmailSnapshot).IsRequired().HasMaxLength(256);
            entity.Property(x => x.LedgerTotalSnapshot).HasPrecision(18, 4);
            entity.Property(x => x.InvoiceTotalSnapshot).HasPrecision(18, 4);
            entity.HasIndex(x => x.CompanyBillingPeriodId).HasDatabaseName("IX_SubscriptionInvoiceSends_CompanyBillingPeriodId");
            entity.HasOne(x => x.CompanyBillingPeriod)
                  .WithMany(p => p.InvoiceSends)
                  .HasForeignKey(x => x.CompanyBillingPeriodId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

