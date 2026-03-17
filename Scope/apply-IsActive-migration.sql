-- Add IsActive column to AspNetUsers (auth schema)
-- Run this once if you see: column a."IsActive" does not exist
-- Use the same DB connection as your API (e.g. psql -h localhost -p 5433 -U postgres -d portal)

-- Add column (safe to run multiple times: will error if column already exists)
ALTER TABLE auth."AspNetUsers"
ADD COLUMN IF NOT EXISTS "IsActive" boolean NOT NULL DEFAULT true;

-- Record that this migration was applied (so EF Core won't try to run it again)
INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
VALUES ('20260303120000_AddUserIsActive', '8.0.10')
ON CONFLICT ("MigrationId") DO NOTHING;
