using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddBookingImportFileMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Theme is added by migration 20260317053643_AddUserThemePreference; do not duplicate (breaks deploy on DBs that already have the column).

            migrationBuilder.CreateTable(
                name: "BookingImportFileMappings",
                schema: "companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileNameKey = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    HeaderSignature = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: false),
                    ColumnMapJson = table.Column<string>(type: "text", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingImportFileMappings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BookingImportFileMappings_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "companies",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BookingImportFileMappings_Company_File_Headers",
                schema: "companies",
                table: "BookingImportFileMappings",
                columns: new[] { "CompanyId", "FileNameKey", "HeaderSignature" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BookingImportFileMappings",
                schema: "companies");
        }
    }
}
