-- Apply AddBookingIsDraft migration manually if needed.
-- Run against your app database (e.g. portal).

-- Add IsDraft column (default false = existing rows are completed)
ALTER TABLE bookings."Bookings"
  ADD COLUMN IF NOT EXISTS "IsDraft" boolean NOT NULL DEFAULT false;

-- Index for listing drafts by customer
CREATE INDEX IF NOT EXISTS "IX_Bookings_Customer_IsDraft_CreatedAt"
  ON bookings."Bookings" ("CustomerId", "IsDraft", "CreatedAtUtc");
