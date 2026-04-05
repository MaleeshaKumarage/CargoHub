using Microsoft.EntityFrameworkCore;

namespace CargoHub.Infrastructure.Persistence;

/// <summary>
/// Runs idempotent SQL at startup so critical schema exists even when migrations
/// were partially applied, run against a different DB, or the column was dropped.
/// Call after Database.Migrate() in Program.cs.
/// </summary>
public static class SchemaEnsure
{
    /// <summary>
    /// Ensures columns that the model expects exist. Safe to run every startup; no-op when schema is already correct.
    /// Fixes DBs where migrations were marked applied but column is missing (e.g. different DB, partial apply).
    /// </summary>
    public static void EnsureCriticalSchema(this ApplicationDbContext db)
    {
        EnsureBookingsIsDraft(db);
        EnsureBookingsCompanyId(db);
        EnsureCompaniesName(db);
        EnsureCompaniesInitialAdminInviteEmailsJson(db);
        EnsureCompaniesBookingFieldRulesJson(db);
        EnsureDropBookingCustomFields(db);
    }

    /// <summary>Adds Name column to companies.Companies if missing (migration AddCompanyName). Idempotent.</summary>
    private static void EnsureCompaniesName(ApplicationDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            DO $$
            BEGIN
              IF NOT EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'companies' AND table_name = 'Companies' AND column_name = 'Name'
              ) THEN
                ALTER TABLE companies."Companies" ADD COLUMN "Name" text NULL;
              END IF;
            END $$;
            """);
    }

    /// <summary>Adds JSON list column for multiple admin invite emails. Idempotent.</summary>
    private static void EnsureCompaniesInitialAdminInviteEmailsJson(ApplicationDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            DO $$
            BEGIN
              IF NOT EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'companies' AND table_name = 'Companies' AND column_name = 'InitialAdminInviteEmailsJson'
              ) THEN
                ALTER TABLE companies."Companies" ADD COLUMN "InitialAdminInviteEmailsJson" text NULL;
              END IF;
            END $$;
            """);
    }

    /// <summary>Adds owned Configurations JSON column for portal booking field rules. Idempotent.</summary>
    private static void EnsureCompaniesBookingFieldRulesJson(ApplicationDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            DO $$
            BEGIN
              IF NOT EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'companies' AND table_name = 'Companies' AND column_name = 'Configurations_BookingFieldRulesJson'
              ) THEN
                ALTER TABLE companies."Companies" ADD COLUMN "Configurations_BookingFieldRulesJson" text NULL;
              END IF;
            END $$;
            """);
    }

    /// <summary>Drops CustomFields_* columns from Bookings if present (custom fields feature removed). Idempotent.</summary>
    private static void EnsureDropBookingCustomFields(ApplicationDbContext db)
    {
        // Single statement with no interpolation — avoids EF1002 (SQL injection) warning.
        // Column names are fixed identifiers; PostgreSQL does not support parameterized identifiers.
        db.Database.ExecuteSqlRaw("""
            ALTER TABLE bookings."Bookings"
            DROP COLUMN IF EXISTS "CustomFields_CustomString1",
            DROP COLUMN IF EXISTS "CustomFields_CustomString2",
            DROP COLUMN IF EXISTS "CustomFields_CustomString3",
            DROP COLUMN IF EXISTS "CustomFields_CustomString4",
            DROP COLUMN IF EXISTS "CustomFields_CustomString5",
            DROP COLUMN IF EXISTS "CustomFields_CustomString6",
            DROP COLUMN IF EXISTS "CustomFields_CustomString7",
            DROP COLUMN IF EXISTS "CustomFields_CustomString8",
            DROP COLUMN IF EXISTS "CustomFields_CustomString9",
            DROP COLUMN IF EXISTS "CustomFields_CustomString10",
            DROP COLUMN IF EXISTS "CustomFields_CustomString11",
            DROP COLUMN IF EXISTS "CustomFields_CustomString12",
            DROP COLUMN IF EXISTS "CustomFields_CustomString13",
            DROP COLUMN IF EXISTS "CustomFields_CustomString14",
            DROP COLUMN IF EXISTS "CustomFields_CustomString15",
            DROP COLUMN IF EXISTS "CustomFields_CustomString16",
            DROP COLUMN IF EXISTS "CustomFields_CustomString17",
            DROP COLUMN IF EXISTS "CustomFields_CustomString18",
            DROP COLUMN IF EXISTS "CustomFields_CustomString19",
            DROP COLUMN IF EXISTS "CustomFields_CustomString20",
            DROP COLUMN IF EXISTS "CustomFields_CustomDecimal1",
            DROP COLUMN IF EXISTS "CustomFields_CustomDecimal2",
            DROP COLUMN IF EXISTS "CustomFields_CustomDecimal3",
            DROP COLUMN IF EXISTS "CustomFields_CustomDecimal4",
            DROP COLUMN IF EXISTS "CustomFields_CustomDecimal5",
            DROP COLUMN IF EXISTS "CustomFields_CustomDate1",
            DROP COLUMN IF EXISTS "CustomFields_CustomDate2",
            DROP COLUMN IF EXISTS "CustomFields_CustomDate3",
            DROP COLUMN IF EXISTS "CustomFields_CustomDate4",
            DROP COLUMN IF EXISTS "CustomFields_CustomDate5";
            """);
    }

    private static void EnsureBookingsIsDraft(ApplicationDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            DO $$
            BEGIN
              IF NOT EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'bookings' AND table_name = 'Bookings' AND column_name = 'IsDraft'
              ) THEN
                ALTER TABLE bookings."Bookings" ADD COLUMN "IsDraft" boolean NOT NULL DEFAULT false;
                CREATE INDEX "IX_Bookings_Customer_IsDraft_CreatedAt"
                  ON bookings."Bookings" ("CustomerId", "IsDraft", "CreatedAtUtc");
              END IF;
            END $$;
            """);
    }

    private static void EnsureBookingsCompanyId(ApplicationDbContext db)
    {
        db.Database.ExecuteSqlRaw("""
            DO $$
            BEGIN
              IF NOT EXISTS (
                SELECT 1 FROM information_schema.columns
                WHERE table_schema = 'bookings' AND table_name = 'Bookings' AND column_name = 'CompanyId'
              ) THEN
                ALTER TABLE bookings."Bookings" ADD COLUMN "CompanyId" uuid NULL;
                CREATE INDEX "IX_Bookings_CompanyId_CreatedAtUtc"
                  ON bookings."Bookings" ("CompanyId", "CreatedAtUtc");
                ALTER TABLE bookings."Bookings"
                  ADD CONSTRAINT "FK_Bookings_Companies_CompanyId"
                  FOREIGN KEY ("CompanyId") REFERENCES companies."Companies" ("Id") ON DELETE CASCADE;
              END IF;
            END $$;
            """);
    }
}
