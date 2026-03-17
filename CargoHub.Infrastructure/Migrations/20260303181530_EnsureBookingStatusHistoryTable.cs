using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CargoHub.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnsureBookingStatusHistoryTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Ensure BookingStatusHistory exists (idempotent). Fixes DBs where an earlier migration was applied but the table was never created.
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS bookings.""BookingStatusHistory"" (
                    ""Id"" uuid NOT NULL,
                    ""BookingId"" uuid NOT NULL,
                    ""Status"" character varying(32) NOT NULL,
                    ""OccurredAtUtc"" timestamp with time zone NOT NULL,
                    ""Source"" character varying(64),
                    CONSTRAINT ""PK_BookingStatusHistory"" PRIMARY KEY (""Id"")
                );
            ");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_BookingStatusHistory_BookingId"" ON bookings.""BookingStatusHistory"" (""BookingId"");");
            migrationBuilder.Sql(@"CREATE INDEX IF NOT EXISTS ""IX_BookingStatusHistory_BookingId_Status"" ON bookings.""BookingStatusHistory"" (""BookingId"", ""Status"");");

            // Idempotent: add columns only if they do not exist (works on all PostgreSQL versions).
            migrationBuilder.Sql(@"
                DO $$
                DECLARE
                    col text;
                    cols text[] := ARRAY[
                        'DeliveryPoint_CustomerNumber','DeliveryPoint_PhoneNumberMobile','DeliveryPoint_VatNo',
                        'Payer_CustomerNumber','Payer_PhoneNumberMobile','Payer_VatNo',
                        'PickUpAddress_CustomerNumber','PickUpAddress_PhoneNumberMobile','PickUpAddress_VatNo',
                        'Receiver_CustomerNumber','Receiver_PhoneNumberMobile','Receiver_VatNo',
                        'Shipment_FreightPayer','Shipment_HandlingInstructions','Shipment_ReceiverReference','Shipment_SenderReference',
                        'Shipper_CustomerNumber','Shipper_PhoneNumberMobile','Shipper_VatNo'
                    ];
                BEGIN
                    FOREACH col IN ARRAY cols
                    LOOP
                        IF NOT EXISTS (SELECT 1 FROM information_schema.columns WHERE table_schema = 'bookings' AND table_name = 'Bookings' AND column_name = col) THEN
                            EXECUTE format('ALTER TABLE bookings.""Bookings"" ADD COLUMN %I text', col);
                        END IF;
                    END LOOP;
                END $$;
            ");

            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS bookings.""BookingPackage"" (
                    ""BookingId"" uuid NOT NULL,
                    ""Id"" integer NOT NULL,
                    ""Weight"" text,
                    ""Volume"" text,
                    ""PackageType"" text,
                    ""Description"" text,
                    ""Length"" text,
                    ""Width"" text,
                    ""Height"" text,
                    CONSTRAINT ""PK_BookingPackage"" PRIMARY KEY (""BookingId"", ""Id""),
                    CONSTRAINT ""FK_BookingPackage_Bookings_BookingId"" FOREIGN KEY (""BookingId"") REFERENCES bookings.""Bookings"" (""Id"") ON DELETE CASCADE
                );
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS bookings.""BookingPackage"";");
            var bookingColumns = new[]
            {
                "DeliveryPoint_CustomerNumber", "DeliveryPoint_PhoneNumberMobile", "DeliveryPoint_VatNo",
                "Payer_CustomerNumber", "Payer_PhoneNumberMobile", "Payer_VatNo",
                "PickUpAddress_CustomerNumber", "PickUpAddress_PhoneNumberMobile", "PickUpAddress_VatNo",
                "Receiver_CustomerNumber", "Receiver_PhoneNumberMobile", "Receiver_VatNo",
                "Shipment_FreightPayer", "Shipment_HandlingInstructions", "Shipment_ReceiverReference", "Shipment_SenderReference",
                "Shipper_CustomerNumber", "Shipper_PhoneNumberMobile", "Shipper_VatNo"
            };
            foreach (var col in bookingColumns)
                migrationBuilder.Sql($@"ALTER TABLE bookings.""Bookings"" DROP COLUMN IF EXISTS ""{col}"";");
        }
    }
}
