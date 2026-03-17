using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace HiavaNet.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    BusinessId = table.Column<string>(type: "text", nullable: true),
                    GsOne = table.Column<string>(type: "text", nullable: true),
                    CustomerMappingId = table.Column<string>(type: "text", nullable: true),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PluginId = table.Column<string>(type: "text", nullable: true),
                    ExternalId = table.Column<string>(type: "text", nullable: true),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    ShipmentNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    WaybillNumber = table.Column<string>(type: "text", nullable: true),
                    CustomerName = table.Column<string>(type: "text", nullable: true),
                    IsTestBooking = table.Column<bool>(type: "boolean", nullable: false),
                    IsFavourite = table.Column<bool>(type: "boolean", nullable: false),
                    Base64Pdf = table.Column<string>(type: "text", nullable: true),
                    EtaSeconds = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Header_SenderId = table.Column<string>(type: "text", nullable: false),
                    Header_CompanyId = table.Column<string>(type: "text", nullable: true),
                    Header_ReferenceNumber = table.Column<string>(type: "text", nullable: true),
                    Header_CustomerReference = table.Column<string>(type: "text", nullable: true),
                    Header_PostalService = table.Column<string>(type: "text", nullable: true),
                    Shipment_Service = table.Column<string>(type: "text", nullable: true),
                    Shipment_CarrierId = table.Column<string>(type: "text", nullable: true),
                    Shipment_ShipmentDateTimeUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Shipment_ParcelCount = table.Column<int>(type: "integer", nullable: false),
                    Shipper_Name = table.Column<string>(type: "text", nullable: false),
                    Shipper_Address1 = table.Column<string>(type: "text", nullable: false),
                    Shipper_Address2 = table.Column<string>(type: "text", nullable: true),
                    Shipper_PostalCode = table.Column<string>(type: "text", nullable: false),
                    Shipper_City = table.Column<string>(type: "text", nullable: false),
                    Shipper_Country = table.Column<string>(type: "text", nullable: false),
                    Shipper_Email = table.Column<string>(type: "text", nullable: true),
                    Shipper_PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    Shipper_ContactPersonName = table.Column<string>(type: "text", nullable: true),
                    Receiver_Name = table.Column<string>(type: "text", nullable: false),
                    Receiver_Address1 = table.Column<string>(type: "text", nullable: false),
                    Receiver_Address2 = table.Column<string>(type: "text", nullable: true),
                    Receiver_PostalCode = table.Column<string>(type: "text", nullable: false),
                    Receiver_City = table.Column<string>(type: "text", nullable: false),
                    Receiver_Country = table.Column<string>(type: "text", nullable: false),
                    Receiver_Email = table.Column<string>(type: "text", nullable: true),
                    Receiver_PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    Receiver_ContactPersonName = table.Column<string>(type: "text", nullable: true),
                    Payer_Name = table.Column<string>(type: "text", nullable: true),
                    Payer_Address1 = table.Column<string>(type: "text", nullable: true),
                    Payer_Address2 = table.Column<string>(type: "text", nullable: true),
                    Payer_PostalCode = table.Column<string>(type: "text", nullable: true),
                    Payer_City = table.Column<string>(type: "text", nullable: true),
                    Payer_Country = table.Column<string>(type: "text", nullable: true),
                    Payer_Email = table.Column<string>(type: "text", nullable: true),
                    Payer_PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    Payer_ContactPersonName = table.Column<string>(type: "text", nullable: true),
                    PickUpAddress_Name = table.Column<string>(type: "text", nullable: false),
                    PickUpAddress_Address1 = table.Column<string>(type: "text", nullable: false),
                    PickUpAddress_Address2 = table.Column<string>(type: "text", nullable: true),
                    PickUpAddress_PostalCode = table.Column<string>(type: "text", nullable: false),
                    PickUpAddress_City = table.Column<string>(type: "text", nullable: false),
                    PickUpAddress_Country = table.Column<string>(type: "text", nullable: false),
                    PickUpAddress_Email = table.Column<string>(type: "text", nullable: true),
                    PickUpAddress_PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PickUpAddress_ContactPersonName = table.Column<string>(type: "text", nullable: true),
                    DeliveryPoint_Name = table.Column<string>(type: "text", nullable: false),
                    DeliveryPoint_Address1 = table.Column<string>(type: "text", nullable: false),
                    DeliveryPoint_Address2 = table.Column<string>(type: "text", nullable: true),
                    DeliveryPoint_PostalCode = table.Column<string>(type: "text", nullable: false),
                    DeliveryPoint_City = table.Column<string>(type: "text", nullable: false),
                    DeliveryPoint_Country = table.Column<string>(type: "text", nullable: false),
                    DeliveryPoint_Email = table.Column<string>(type: "text", nullable: true),
                    DeliveryPoint_PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    DeliveryPoint_ContactPersonName = table.Column<string>(type: "text", nullable: true),
                    ShippingInfo_GrossWeight = table.Column<string>(type: "text", nullable: false),
                    ShippingInfo_GrossVolume = table.Column<string>(type: "text", nullable: false),
                    ShippingInfo_PackageQuantity = table.Column<string>(type: "text", nullable: false),
                    ShippingInfo_PickupHandlingInstructions = table.Column<string>(type: "text", nullable: true),
                    ShippingInfo_DeliveryHandlingInstructions = table.Column<string>(type: "text", nullable: true),
                    ShippingInfo_GeneralInstructions = table.Column<string>(type: "text", nullable: true),
                    ShippingInfo_ServiceSpecification = table.Column<string>(type: "text", nullable: true),
                    ShippingInfo_NoDgPackages = table.Column<string>(type: "text", nullable: true),
                    ShippingInfo_DeliveryWithoutSignature = table.Column<bool>(type: "boolean", nullable: false),
                    ShippingInfo_LoadMeter = table.Column<string>(type: "text", nullable: true),
                    ShippingInfo_RouteInformation = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Companies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CompanyId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CustomerId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    BusinessId = table.Column<string>(type: "text", nullable: true),
                    SenderNumber = table.Column<string>(type: "text", nullable: true),
                    DivisionCode = table.Column<string>(type: "text", nullable: true),
                    Counter = table.Column<int>(type: "integer", nullable: false),
                    DefaultShipperAddress_Id = table.Column<Guid>(type: "uuid", nullable: true),
                    DefaultShipperAddress_Name = table.Column<string>(type: "text", nullable: true),
                    DefaultShipperAddress_Address1 = table.Column<string>(type: "text", nullable: true),
                    DefaultShipperAddress_Address2 = table.Column<string>(type: "text", nullable: true),
                    DefaultShipperAddress_PostalCode = table.Column<string>(type: "text", nullable: true),
                    DefaultShipperAddress_City = table.Column<string>(type: "text", nullable: true),
                    DefaultShipperAddress_Country = table.Column<string>(type: "text", nullable: true),
                    DefaultShipperAddress_PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    DefaultShipperAddress_PhoneNumberMobile = table.Column<bool>(type: "boolean", nullable: true),
                    DefaultShipperAddress_ContactPersonName = table.Column<string>(type: "text", nullable: true),
                    DefaultShipperAddress_Email = table.Column<string>(type: "text", nullable: true),
                    DefaultShipperAddress_County = table.Column<string>(type: "text", nullable: true),
                    Configurations_DefaultPostalService = table.Column<string>(type: "text", nullable: true),
                    Configurations_ShipperVatNo = table.Column<string>(type: "text", nullable: true),
                    Configurations_PickUpAddressVatNo = table.Column<string>(type: "text", nullable: true),
                    Configurations_FreightPayer = table.Column<string>(type: "text", nullable: true),
                    Configurations_Service = table.Column<string>(type: "text", nullable: true),
                    Configurations_PhoneNumber = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BookingUpdate",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    BookingId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    TransportPhase = table.Column<string>(type: "text", nullable: true),
                    RawMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BookingUpdate", x => new { x.BookingId, x.Id });
                    table.ForeignKey(
                        name: "FK_BookingUpdate_Bookings_BookingId",
                        column: x => x.BookingId,
                        principalTable: "Bookings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AgreementNumber",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PostalService = table.Column<string>(type: "text", nullable: false),
                    Service = table.Column<string>(type: "text", nullable: false),
                    Number = table.Column<string>(type: "text", nullable: false),
                    Counter = table.Column<int>(type: "integer", nullable: true),
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AgreementNumber", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AgreementNumber_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Companies_AddressBook",
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
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies_AddressBook", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Companies_AddressBook_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Companies_PickUpAddressBook",
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
                    CompanyId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Companies_PickUpAddressBook", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Companies_PickUpAddressBook_Companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "Companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AgreementNumber_CompanyId",
                table: "AgreementNumber",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_Customer_Enabled_CreatedAt",
                table: "Bookings",
                columns: new[] { "CustomerId", "Enabled", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_ShipmentNumber",
                table: "Bookings",
                column: "ShipmentNumber");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_CompanyId",
                table: "Companies",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_AddressBook_CompanyId",
                table: "Companies_AddressBook",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_Companies_PickUpAddressBook_CompanyId",
                table: "Companies_PickUpAddressBook",
                column: "CompanyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AgreementNumber");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "BookingUpdate");

            migrationBuilder.DropTable(
                name: "Companies_AddressBook");

            migrationBuilder.DropTable(
                name: "Companies_PickUpAddressBook");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "Companies");
        }
    }
}
