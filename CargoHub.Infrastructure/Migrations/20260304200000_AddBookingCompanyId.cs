using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingCompanyId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CompanyId",
                schema: "bookings",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CompanyId_CreatedAtUtc",
                schema: "bookings",
                table: "Bookings",
                columns: new[] { "CompanyId", "CreatedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Companies_CompanyId",
                schema: "bookings",
                table: "Bookings",
                column: "CompanyId",
                principalSchema: "companies",
                principalTable: "Companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Companies_CompanyId",
                schema: "bookings",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CompanyId_CreatedAtUtc",
                schema: "bookings",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                schema: "bookings",
                table: "Bookings");
        }
    }
}
