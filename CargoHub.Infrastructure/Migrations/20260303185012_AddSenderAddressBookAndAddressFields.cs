using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSenderAddressBookAndAddressFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Companies_SenderAddressBook",
                schema: "companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Address1 = table.Column<string>(type: "text", nullable: false),
                    Address2 = table.Column<string>(type: "text", nullable: true),
                    PostalCode = table.Column<string>(type: "text", nullable: false),
                    City = table.Column<string>(type: "text", nullable: false),
                    Country = table.Column<string>(type: "text", nullable: false),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberMobile = table.Column<bool>(type: "boolean", nullable: false),
                    ContactPersonName = table.Column<string>(type: "text", nullable: true),
                    Email = table.Column<string>(type: "text", nullable: true),
                    County = table.Column<string>(type: "text", nullable: true),
                    VatNo = table.Column<string>(type: "text", nullable: true),
                    CustomerNumber = table.Column<string>(type: "text", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies_SenderAddressBook", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Companies_SenderAddressBook_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "companies",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_SenderAddressBook_CompanyId",
                schema: "companies",
                table: "Companies_SenderAddressBook",
                column: "CompanyId");

            migrationBuilder.AddColumn<string>(
                name: "VatNo",
                schema: "companies",
                table: "Companies_AddressBook",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerNumber",
                schema: "companies",
                table: "Companies_AddressBook",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VatNo",
                schema: "companies",
                table: "Companies_PickUpAddressBook",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerNumber",
                schema: "companies",
                table: "Companies_PickUpAddressBook",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultShipperAddress_VatNo",
                schema: "companies",
                table: "Companies",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DefaultShipperAddress_CustomerNumber",
                schema: "companies",
                table: "Companies",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Companies_SenderAddressBook",
                schema: "companies");

            migrationBuilder.DropColumn(
                name: "VatNo",
                schema: "companies",
                table: "Companies_AddressBook");

            migrationBuilder.DropColumn(
                name: "CustomerNumber",
                schema: "companies",
                table: "Companies_AddressBook");

            migrationBuilder.DropColumn(
                name: "VatNo",
                schema: "companies",
                table: "Companies_PickUpAddressBook");

            migrationBuilder.DropColumn(
                name: "CustomerNumber",
                schema: "companies",
                table: "Companies_PickUpAddressBook");

            migrationBuilder.DropColumn(
                name: "DefaultShipperAddress_VatNo",
                schema: "companies",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "DefaultShipperAddress_CustomerNumber",
                schema: "companies",
                table: "Companies");
        }
    }
}
