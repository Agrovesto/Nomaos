using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Agrovesto.Backend.API.Migrations
{
    /// <inheritdoc />
    public partial class addedInputDeliveryTracker : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InputDeliveryTrackers",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WarehouseId = table.Column<long>(type: "bigint", nullable: false),
                    WarehouseInputInventoryId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    InputName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuantityLoaded = table.Column<long>(type: "bigint", nullable: false),
                    QuantityDelivered = table.Column<long>(type: "bigint", nullable: false),
                    QuantityReceived = table.Column<long>(type: "bigint", nullable: false),
                    DeliveredTo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeliveredBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeliveryContact = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PickupLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DeliveryLocation = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogisticType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LogisticNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PickUpDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    DeliveryDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InputDeliveryTrackers", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InputDeliveryTrackers");
        }
    }
}
