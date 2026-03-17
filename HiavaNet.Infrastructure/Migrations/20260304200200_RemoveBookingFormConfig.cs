using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HiavaNet.Infrastructure.Migrations
{
    /// <summary>
    /// Drops the BookingFormConfig column from companies.&quot;Companies&quot; (design booking form feature removed).
    /// </summary>
    public partial class RemoveBookingFormConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BookingFormConfig",
                schema: "companies",
                table: "Companies");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BookingFormConfig",
                schema: "companies",
                table: "Companies",
                type: "jsonb",
                nullable: true);
        }
    }
}
