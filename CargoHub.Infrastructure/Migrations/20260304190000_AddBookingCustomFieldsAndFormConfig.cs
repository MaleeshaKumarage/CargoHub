using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingCustomFieldsAndFormConfig : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CustomFields_CustomString1",
                schema: "bookings",
                table: "Bookings",
                type: "text",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "CustomFields_CustomString2",
                schema: "bookings",
                table: "Bookings",
                type: "text",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "CustomFields_CustomString3",
                schema: "bookings",
                table: "Bookings",
                type: "text",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "CustomFields_CustomString4",
                schema: "bookings",
                table: "Bookings",
                type: "text",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "CustomFields_CustomString5",
                schema: "bookings",
                table: "Bookings",
                type: "text",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "CustomFields_CustomString6",
                schema: "bookings",
                table: "Bookings",
                type: "text",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "CustomFields_CustomString7",
                schema: "bookings",
                table: "Bookings",
                type: "text",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "CustomFields_CustomString8",
                schema: "bookings",
                table: "Bookings",
                type: "text",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "CustomFields_CustomString9",
                schema: "bookings",
                table: "Bookings",
                type: "text",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "CustomFields_CustomString10",
                schema: "bookings",
                table: "Bookings",
                type: "text",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "CustomFields_CustomString11",
                schema: "bookings",
                table: "Bookings",
                type: "text",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "CustomFields_CustomString12",
                schema: "bookings",
                table: "Bookings",
                type: "text",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "CustomFields_CustomString13",
                schema: "bookings",
                table: "Bookings",
                type: "text",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "CustomFields_CustomString14",
                schema: "bookings",
                table: "Bookings",
                type: "text",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "CustomFields_CustomString15",
                schema: "bookings",
                table: "Bookings",
                type: "text",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "CustomFields_CustomString16",
                schema: "bookings",
                table: "Bookings",
                type: "text",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "CustomFields_CustomString17",
                schema: "bookings",
                table: "Bookings",
                type: "text",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "CustomFields_CustomString18",
                schema: "bookings",
                table: "Bookings",
                type: "text",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "CustomFields_CustomString19",
                schema: "bookings",
                table: "Bookings",
                type: "text",
                nullable: true);
            migrationBuilder.AddColumn<string>(
                name: "CustomFields_CustomString20",
                schema: "bookings",
                table: "Bookings",
                type: "text",
                nullable: true);
            migrationBuilder.AddColumn<decimal>(
                name: "CustomFields_CustomDecimal1",
                schema: "bookings",
                table: "Bookings",
                type: "numeric",
                nullable: true);
            migrationBuilder.AddColumn<decimal>(
                name: "CustomFields_CustomDecimal2",
                schema: "bookings",
                table: "Bookings",
                type: "numeric",
                nullable: true);
            migrationBuilder.AddColumn<decimal>(
                name: "CustomFields_CustomDecimal3",
                schema: "bookings",
                table: "Bookings",
                type: "numeric",
                nullable: true);
            migrationBuilder.AddColumn<decimal>(
                name: "CustomFields_CustomDecimal4",
                schema: "bookings",
                table: "Bookings",
                type: "numeric",
                nullable: true);
            migrationBuilder.AddColumn<decimal>(
                name: "CustomFields_CustomDecimal5",
                schema: "bookings",
                table: "Bookings",
                type: "numeric",
                nullable: true);
            migrationBuilder.AddColumn<DateTime>(
                name: "CustomFields_CustomDate1",
                schema: "bookings",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTime>(
                name: "CustomFields_CustomDate2",
                schema: "bookings",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTime>(
                name: "CustomFields_CustomDate3",
                schema: "bookings",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTime>(
                name: "CustomFields_CustomDate4",
                schema: "bookings",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: true);
            migrationBuilder.AddColumn<DateTime>(
                name: "CustomFields_CustomDate5",
                schema: "bookings",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BookingFormConfig",
                schema: "companies",
                table: "Companies",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "CustomFields_CustomString1", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomString2", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomString3", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomString4", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomString5", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomString6", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomString7", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomString8", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomString9", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomString10", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomString11", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomString12", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomString13", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomString14", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomString15", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomString16", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomString17", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomString18", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomString19", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomString20", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomDecimal1", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomDecimal2", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomDecimal3", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomDecimal4", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomDecimal5", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomDate1", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomDate2", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomDate3", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomDate4", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "CustomFields_CustomDate5", schema: "bookings", table: "Bookings");
            migrationBuilder.DropColumn(name: "BookingFormConfig", schema: "companies", table: "Companies");
        }
    }
}
