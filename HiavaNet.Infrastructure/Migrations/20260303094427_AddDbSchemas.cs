using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HiavaNet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDbSchemas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "companies");

            migrationBuilder.EnsureSchema(
                name: "auth");

            migrationBuilder.EnsureSchema(
                name: "bookings");

            migrationBuilder.EnsureSchema(
                name: "couriers");

            migrationBuilder.RenameTable(
                name: "Companies_PickUpAddressBook",
                newName: "Companies_PickUpAddressBook",
                newSchema: "companies");

            migrationBuilder.RenameTable(
                name: "Companies_AddressBook",
                newName: "Companies_AddressBook",
                newSchema: "companies");

            migrationBuilder.RenameTable(
                name: "Companies",
                newName: "Companies",
                newSchema: "companies");

            migrationBuilder.RenameTable(
                name: "BookingUpdate",
                newName: "BookingUpdate",
                newSchema: "bookings");

            migrationBuilder.RenameTable(
                name: "Bookings",
                newName: "Bookings",
                newSchema: "bookings");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                newName: "AspNetUserTokens",
                newSchema: "auth");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                newName: "AspNetUsers",
                newSchema: "auth");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                newName: "AspNetUserRoles",
                newSchema: "auth");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                newName: "AspNetUserLogins",
                newSchema: "auth");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                newName: "AspNetUserClaims",
                newSchema: "auth");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                newName: "AspNetRoles",
                newSchema: "auth");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                newName: "AspNetRoleClaims",
                newSchema: "auth");

            migrationBuilder.RenameTable(
                name: "AgreementNumber",
                newName: "AgreementNumber",
                newSchema: "companies");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // couriers schema has no tables; no need to move anything back
            migrationBuilder.RenameTable(
                name: "Companies_PickUpAddressBook",
                schema: "companies",
                newName: "Companies_PickUpAddressBook");

            migrationBuilder.RenameTable(
                name: "Companies_AddressBook",
                schema: "companies",
                newName: "Companies_AddressBook");

            migrationBuilder.RenameTable(
                name: "Companies",
                schema: "companies",
                newName: "Companies");

            migrationBuilder.RenameTable(
                name: "BookingUpdate",
                schema: "bookings",
                newName: "BookingUpdate");

            migrationBuilder.RenameTable(
                name: "Bookings",
                schema: "bookings",
                newName: "Bookings");

            migrationBuilder.RenameTable(
                name: "AspNetUserTokens",
                schema: "auth",
                newName: "AspNetUserTokens");

            migrationBuilder.RenameTable(
                name: "AspNetUsers",
                schema: "auth",
                newName: "AspNetUsers");

            migrationBuilder.RenameTable(
                name: "AspNetUserRoles",
                schema: "auth",
                newName: "AspNetUserRoles");

            migrationBuilder.RenameTable(
                name: "AspNetUserLogins",
                schema: "auth",
                newName: "AspNetUserLogins");

            migrationBuilder.RenameTable(
                name: "AspNetUserClaims",
                schema: "auth",
                newName: "AspNetUserClaims");

            migrationBuilder.RenameTable(
                name: "AspNetRoles",
                schema: "auth",
                newName: "AspNetRoles");

            migrationBuilder.RenameTable(
                name: "AspNetRoleClaims",
                schema: "auth",
                newName: "AspNetRoleClaims");

            migrationBuilder.RenameTable(
                name: "AgreementNumber",
                schema: "companies",
                newName: "AgreementNumber");
        }
    }
}
