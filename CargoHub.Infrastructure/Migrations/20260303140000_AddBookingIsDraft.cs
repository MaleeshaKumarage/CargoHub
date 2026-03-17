using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    [Migration("20260303140000_AddBookingIsDraft")]
    public partial class AddBookingIsDraft : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsDraft",
                schema: "bookings",
                table: "Bookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Customer_IsDraft_CreatedAt",
                schema: "bookings",
                table: "Bookings",
                columns: new[] { "CustomerId", "IsDraft", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Bookings_Customer_IsDraft_CreatedAt",
                schema: "bookings",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "IsDraft",
                schema: "bookings",
                table: "Bookings");
        }
    }
}
