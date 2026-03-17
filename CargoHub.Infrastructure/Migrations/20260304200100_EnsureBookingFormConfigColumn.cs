using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoHub.Infrastructure.Migrations
{
    /// <summary>
    /// Ensures companies.&quot;Companies&quot; has BookingFormConfig column (idempotent).
    /// Use if the column is missing due to partial apply or different DB.
    /// </summary>
    public partial class EnsureBookingFormConfigColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
DO $$
BEGIN
  IF NOT EXISTS (
    SELECT 1 FROM information_schema.columns
    WHERE table_schema = 'companies' AND table_name = 'Companies' AND column_name = 'BookingFormConfig'
  ) THEN
    ALTER TABLE companies.""Companies"" ADD COLUMN ""BookingFormConfig"" jsonb NULL;
  END IF;
END $$;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookingFormConfig",
                schema: "companies",
                table: "Companies");
        }
    }
}
