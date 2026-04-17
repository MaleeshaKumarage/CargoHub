using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FreelanceRidersAndBookingAssignment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "couriers");

            migrationBuilder.AddColumn<DateTime>(
                name: "FreelanceRiderAcceptedAtUtc",
                schema: "bookings",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FreelanceRiderAssignmentDeadlineUtc",
                schema: "bookings",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "FreelanceRiderAssignmentLapsed",
                schema: "bookings",
                table: "Bookings",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "FreelanceRiderId",
                schema: "bookings",
                table: "Bookings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FreelanceRiders",
                schema: "couriers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Phone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ApplicationUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreelanceRiders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FreelanceRiders_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "companies",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "FreelanceRiderInvites",
                schema: "couriers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FreelanceRiderId = table.Column<Guid>(type: "uuid", nullable: false),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    TokenHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExpiresAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConsumedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreelanceRiderInvites", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FreelanceRiderInvites_FreelanceRiders_FreelanceRiderId",
                        column: x => x.FreelanceRiderId,
                        principalSchema: "couriers",
                        principalTable: "FreelanceRiders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FreelanceRiderServiceAreas",
                schema: "couriers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FreelanceRiderId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostalCodeNormalized = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreelanceRiderServiceAreas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FreelanceRiderServiceAreas_FreelanceRiders_FreelanceRiderId",
                        column: x => x.FreelanceRiderId,
                        principalSchema: "couriers",
                        principalTable: "FreelanceRiders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_FreelanceRiderAssignmentDeadlineUtc",
                schema: "bookings",
                table: "Bookings",
                column: "FreelanceRiderAssignmentDeadlineUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_FreelanceRiderId",
                schema: "bookings",
                table: "Bookings",
                column: "FreelanceRiderId");

            migrationBuilder.CreateIndex(
                name: "IX_FreelanceRiderInvites_FreelanceRiderId",
                schema: "couriers",
                table: "FreelanceRiderInvites",
                column: "FreelanceRiderId");

            migrationBuilder.CreateIndex(
                name: "IX_FreelanceRiderInvites_TokenHash",
                schema: "couriers",
                table: "FreelanceRiderInvites",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FreelanceRiders_ApplicationUserId",
                schema: "couriers",
                table: "FreelanceRiders",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_FreelanceRiders_CompanyId",
                schema: "couriers",
                table: "FreelanceRiders",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_FreelanceRiders_NormalizedEmail",
                schema: "couriers",
                table: "FreelanceRiders",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "IX_FreelanceRiderServiceAreas_Rider_Postal",
                schema: "couriers",
                table: "FreelanceRiderServiceAreas",
                columns: new[] { "FreelanceRiderId", "PostalCodeNormalized" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_FreelanceRiders_FreelanceRiderId",
                schema: "bookings",
                table: "Bookings",
                column: "FreelanceRiderId",
                principalSchema: "couriers",
                principalTable: "FreelanceRiders",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_FreelanceRiders_FreelanceRiderId",
                schema: "bookings",
                table: "Bookings");

            migrationBuilder.DropTable(
                name: "FreelanceRiderInvites",
                schema: "couriers");

            migrationBuilder.DropTable(
                name: "FreelanceRiderServiceAreas",
                schema: "couriers");

            migrationBuilder.DropTable(
                name: "FreelanceRiders",
                schema: "couriers");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_FreelanceRiderAssignmentDeadlineUtc",
                schema: "bookings",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_FreelanceRiderId",
                schema: "bookings",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "FreelanceRiderAcceptedAtUtc",
                schema: "bookings",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "FreelanceRiderAssignmentDeadlineUtc",
                schema: "bookings",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "FreelanceRiderAssignmentLapsed",
                schema: "bookings",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "FreelanceRiderId",
                schema: "bookings",
                table: "Bookings");
        }
    }
}
