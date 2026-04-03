using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CompanyAdminInvitesAndLimits : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InitialAdminInviteEmail",
                schema: "companies",
                table: "Companies",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxAdminAccounts",
                schema: "companies",
                table: "Companies",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MaxUserAccounts",
                schema: "companies",
                table: "Companies",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompanyAdminInvites",
                schema: "companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyAdminInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyAdminInvites_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "companies",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAdminInvites_CompanyId",
                schema: "companies",
                table: "CompanyAdminInvites",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyAdminInvites_TokenHash",
                schema: "companies",
                table: "CompanyAdminInvites",
                column: "TokenHash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CompanyAdminInvites",
                schema: "companies");

            migrationBuilder.DropColumn(
                name: "InitialAdminInviteEmail",
                schema: "companies",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "MaxAdminAccounts",
                schema: "companies",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "MaxUserAccounts",
                schema: "companies",
                table: "Companies");
        }
    }
}
