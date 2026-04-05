using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionAssignmentsAndBillingExclusions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BillingPeriodExcludedBookings",
                schema: "companies",
                columns: table => new
                {
                    CompanyBillingPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingPeriodExcludedBookings", x => new { x.CompanyBillingPeriodId, x.BookingId });
                    table.ForeignKey(
                        name: "FK_BillingPeriodExcludedBookings_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "bookings",
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillingPeriodExcludedBookings_CompanyBillingPeriods_Company~",
                        column: x => x.CompanyBillingPeriodId,
                        principalSchema: "companies",
                        principalTable: "CompanyBillingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CompanySubscriptionAssignments",
                schema: "companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SetByUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanySubscriptionAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanySubscriptionAssignments_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "companies",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BillingPeriodExcludedBookings_BookingId",
                schema: "companies",
                table: "BillingPeriodExcludedBookings",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanySubscriptionAssignments_Company_EffectiveFrom",
                schema: "companies",
                table: "CompanySubscriptionAssignments",
                columns: new[] { "CompanyId", "EffectiveFromUtc" });

            // Seed one history row per company so past bookings can resolve a plan before explicit changes.
            migrationBuilder.Sql("""
                INSERT INTO companies."CompanySubscriptionAssignments" ("Id", "CompanyId", "SubscriptionPlanId", "EffectiveFromUtc", "SetByUserId")
                SELECT gen_random_uuid(), c."Id", c."SubscriptionPlanId", TIMESTAMPTZ '2000-01-01T00:00:00Z', NULL
                FROM companies."Companies" c
                WHERE c."SubscriptionPlanId" IS NOT NULL;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillingPeriodExcludedBookings",
                schema: "companies");

            migrationBuilder.DropTable(
                name: "CompanySubscriptionAssignments",
                schema: "companies");
        }
    }
}
