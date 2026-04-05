using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionAndBillingTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionPlanId",
                schema: "companies",
                table: "Companies",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FirstBillableAtUtc",
                schema: "bookings",
                table: "Bookings",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompanyBillingPeriods",
                schema: "companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false),
                    YearUtc = table.Column<int>(type: "integer", nullable: false),
                    MonthUtc = table.Column<int>(type: "integer", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyBillingPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyBillingPeriods_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalSchema: "companies",
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                schema: "companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Kind = table.Column<int>(type: "integer", nullable: false),
                    ChargeTimeAnchor = table.Column<int>(type: "integer", nullable: false),
                    TrialBookingAllowance = table.Column<int>(type: "integer", nullable: true),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BillingLineItems",
                schema: "companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyBillingPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: true),
                    LineType = table.Column<int>(type: "integer", nullable: false),
                    Component = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    SubscriptionPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionPlanPricingPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ExcludedFromInvoice = table.Column<bool>(type: "boolean", nullable: false),
                    InvoiceExclusionUpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    InvoiceExclusionUpdatedByUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BillingLineItems_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalSchema: "bookings",
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BillingLineItems_CompanyBillingPeriods_CompanyBillingPeriod~",
                        column: x => x.CompanyBillingPeriodId,
                        principalSchema: "companies",
                        principalTable: "CompanyBillingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionInvoiceSends",
                schema: "companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyBillingPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    SentAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SentBySuperAdminUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RecipientAdminUserId = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RecipientEmailSnapshot = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    LedgerTotalSnapshot = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    InvoiceTotalSnapshot = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionInvoiceSends", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionInvoiceSends_CompanyBillingPeriods_CompanyBilli~",
                        column: x => x.CompanyBillingPeriodId,
                        principalSchema: "companies",
                        principalTable: "CompanyBillingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlanPricingPeriods",
                schema: "companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionPlanId = table.Column<Guid>(type: "uuid", nullable: false),
                    EffectiveFromUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ChargePerBooking = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    MonthlyFee = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    IncludedBookingsPerMonth = table.Column<int>(type: "integer", nullable: true),
                    OverageChargePerBooking = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlanPricingPeriods", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionPlanPricingPeriods_SubscriptionPlans_Subscripti~",
                        column: x => x.SubscriptionPlanId,
                        principalSchema: "companies",
                        principalTable: "SubscriptionPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SubscriptionPlanPricingTiers",
                schema: "companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionPlanPricingPeriodId = table.Column<Guid>(type: "uuid", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false),
                    InclusiveMaxBookingsInPeriod = table.Column<int>(type: "integer", nullable: true),
                    ChargePerBooking = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    MonthlyFee = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlanPricingTiers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubscriptionPlanPricingTiers_SubscriptionPlanPricingPeriods~",
                        column: x => x.SubscriptionPlanPricingPeriodId,
                        principalSchema: "companies",
                        principalTable: "SubscriptionPlanPricingPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Companies_SubscriptionPlanId",
                schema: "companies",
                table: "Companies",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CompanyId_FirstBillableAtUtc",
                schema: "bookings",
                table: "Bookings",
                columns: new[] { "CompanyId", "FirstBillableAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_BillingLineItems_BookingId",
                schema: "companies",
                table: "BillingLineItems",
                column: "BookingId");

            migrationBuilder.CreateIndex(
                name: "IX_BillingLineItems_CompanyBillingPeriodId",
                schema: "companies",
                table: "BillingLineItems",
                column: "CompanyBillingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyBillingPeriods_Company_Year_Month",
                schema: "companies",
                table: "CompanyBillingPeriods",
                columns: new[] { "CompanyId", "YearUtc", "MonthUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionInvoiceSends_CompanyBillingPeriodId",
                schema: "companies",
                table: "SubscriptionInvoiceSends",
                column: "CompanyBillingPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlanPricingPeriods_Plan_EffectiveFrom",
                schema: "companies",
                table: "SubscriptionPlanPricingPeriods",
                columns: new[] { "SubscriptionPlanId", "EffectiveFromUtc" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlanPricingTiers_Period_Ordinal",
                schema: "companies",
                table: "SubscriptionPlanPricingTiers",
                columns: new[] { "SubscriptionPlanPricingPeriodId", "Ordinal" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_IsActive",
                schema: "companies",
                table: "SubscriptionPlans",
                column: "IsActive");

            migrationBuilder.AddForeignKey(
                name: "FK_Companies_SubscriptionPlans_SubscriptionPlanId",
                schema: "companies",
                table: "Companies",
                column: "SubscriptionPlanId",
                principalSchema: "companies",
                principalTable: "SubscriptionPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Companies_SubscriptionPlans_SubscriptionPlanId",
                schema: "companies",
                table: "Companies");

            migrationBuilder.DropTable(
                name: "BillingLineItems",
                schema: "companies");

            migrationBuilder.DropTable(
                name: "SubscriptionInvoiceSends",
                schema: "companies");

            migrationBuilder.DropTable(
                name: "SubscriptionPlanPricingTiers",
                schema: "companies");

            migrationBuilder.DropTable(
                name: "CompanyBillingPeriods",
                schema: "companies");

            migrationBuilder.DropTable(
                name: "SubscriptionPlanPricingPeriods",
                schema: "companies");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans",
                schema: "companies");

            migrationBuilder.DropIndex(
                name: "IX_Companies_SubscriptionPlanId",
                schema: "companies",
                table: "Companies");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CompanyId_FirstBillableAtUtc",
                schema: "bookings",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "SubscriptionPlanId",
                schema: "companies",
                table: "Companies");

            migrationBuilder.DropColumn(
                name: "FirstBillableAtUtc",
                schema: "bookings",
                table: "Bookings");
        }
    }
}
