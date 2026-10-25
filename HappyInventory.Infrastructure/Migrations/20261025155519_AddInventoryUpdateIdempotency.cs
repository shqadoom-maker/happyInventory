using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyInventory.Infrastructure.Migrations
{
    public partial class AddInventoryUpdateIdempotency : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InventoryUpdateRecords",
                columns: table => new
                {
                    IdempotencyKey = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    WarehouseId = table.Column<int>(type: "INTEGER", nullable: false),
                    SKUCode = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Quantity = table.Column<int>(type: "INTEGER", nullable: false),
                    WarehouseItemId = table.Column<int>(type: "INTEGER", nullable: false),
                    ItemName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    PreviousQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    RemainingQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryUpdateRecords", x => x.IdempotencyKey);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InventoryUpdateRecords_CreatedUtc",
                table: "InventoryUpdateRecords",
                column: "CreatedUtc");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InventoryUpdateRecords");
        }
    }
}
