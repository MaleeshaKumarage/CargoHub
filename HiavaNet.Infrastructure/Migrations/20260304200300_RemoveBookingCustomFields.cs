using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HiavaNet.Infrastructure.Migrations
{
    /// <summary>
    /// Drops CustomFields_* columns from bookings.&quot;Bookings&quot; (custom fields feature removed).
    /// </summary>
    public partial class RemoveBookingCustomFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            var columns = new[]
            {
                "CustomFields_CustomString1", "CustomFields_CustomString2", "CustomFields_CustomString3", "CustomFields_CustomString4", "CustomFields_CustomString5",
                "CustomFields_CustomString6", "CustomFields_CustomString7", "CustomFields_CustomString8", "CustomFields_CustomString9", "CustomFields_CustomString10",
                "CustomFields_CustomString11", "CustomFields_CustomString12", "CustomFields_CustomString13", "CustomFields_CustomString14", "CustomFields_CustomString15",
                "CustomFields_CustomString16", "CustomFields_CustomString17", "CustomFields_CustomString18", "CustomFields_CustomString19", "CustomFields_CustomString20",
                "CustomFields_CustomDecimal1", "CustomFields_CustomDecimal2", "CustomFields_CustomDecimal3", "CustomFields_CustomDecimal4", "CustomFields_CustomDecimal5",
                "CustomFields_CustomDate1", "CustomFields_CustomDate2", "CustomFields_CustomDate3", "CustomFields_CustomDate4", "CustomFields_CustomDate5"
            };
            foreach (var col in columns)
            {
                migrationBuilder.Sql($"ALTER TABLE bookings.\"Bookings\" DROP COLUMN IF EXISTS \"{col}\";");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(name: "CustomFields_CustomString1", schema: "bookings", table: "Bookings", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CustomFields_CustomString2", schema: "bookings", table: "Bookings", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CustomFields_CustomString3", schema: "bookings", table: "Bookings", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CustomFields_CustomString4", schema: "bookings", table: "Bookings", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CustomFields_CustomString5", schema: "bookings", table: "Bookings", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CustomFields_CustomString6", schema: "bookings", table: "Bookings", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CustomFields_CustomString7", schema: "bookings", table: "Bookings", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CustomFields_CustomString8", schema: "bookings", table: "Bookings", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CustomFields_CustomString9", schema: "bookings", table: "Bookings", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CustomFields_CustomString10", schema: "bookings", table: "Bookings", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CustomFields_CustomString11", schema: "bookings", table: "Bookings", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CustomFields_CustomString12", schema: "bookings", table: "Bookings", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CustomFields_CustomString13", schema: "bookings", table: "Bookings", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CustomFields_CustomString14", schema: "bookings", table: "Bookings", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CustomFields_CustomString15", schema: "bookings", table: "Bookings", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CustomFields_CustomString16", schema: "bookings", table: "Bookings", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CustomFields_CustomString17", schema: "bookings", table: "Bookings", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CustomFields_CustomString18", schema: "bookings", table: "Bookings", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CustomFields_CustomString19", schema: "bookings", table: "Bookings", type: "text", nullable: true);
            migrationBuilder.AddColumn<string>(name: "CustomFields_CustomString20", schema: "bookings", table: "Bookings", type: "text", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "CustomFields_CustomDecimal1", schema: "bookings", table: "Bookings", type: "numeric", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "CustomFields_CustomDecimal2", schema: "bookings", table: "Bookings", type: "numeric", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "CustomFields_CustomDecimal3", schema: "bookings", table: "Bookings", type: "numeric", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "CustomFields_CustomDecimal4", schema: "bookings", table: "Bookings", type: "numeric", nullable: true);
            migrationBuilder.AddColumn<decimal>(name: "CustomFields_CustomDecimal5", schema: "bookings", table: "Bookings", type: "numeric", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "CustomFields_CustomDate1", schema: "bookings", table: "Bookings", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "CustomFields_CustomDate2", schema: "bookings", table: "Bookings", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "CustomFields_CustomDate3", schema: "bookings", table: "Bookings", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "CustomFields_CustomDate4", schema: "bookings", table: "Bookings", type: "timestamp with time zone", nullable: true);
            migrationBuilder.AddColumn<DateTime>(name: "CustomFields_CustomDate5", schema: "bookings", table: "Bookings", type: "timestamp with time zone", nullable: true);
        }
    }
}
