using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingStatusHistory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw SQL so the migration is idempotent (IF NOT EXISTS). Ensures the table
            // is created even if this migration was previously skipped or partially applied.
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS bookings.""BookingStatusHistory"" (
                    ""Id"" uuid NOT NULL,
                    ""BookingId"" uuid NOT NULL,
                    ""Status"" character varying(32) NOT NULL,
                    ""OccurredAtUtc"" timestamp with time zone NOT NULL,
                    ""Source"" character varying(64),
                    CONSTRAINT ""PK_BookingStatusHistory"" PRIMARY KEY (""Id"")
                );
            ");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_BookingStatusHistory_BookingId"" ON bookings.""BookingStatusHistory"" (""BookingId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_BookingStatusHistory_BookingId_Status"" ON bookings.""BookingStatusHistory"" (""BookingId"", ""Status"");");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingStatusHistory",
                schema: "bookings");
        }
    }
}
