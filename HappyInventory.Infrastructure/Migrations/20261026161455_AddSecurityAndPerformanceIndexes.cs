using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HappyInventory.Infrastructure.Migrations
{
    public partial class AddSecurityAndPerformanceIndexes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_WarehouseItems_Qty",
                table: "WarehouseItems",
                column: "Qty");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseItems_SKUCode",
                table: "WarehouseItems",
                column: "SKUCode");

            migrationBuilder.CreateIndex(
                name: "IX_WarehouseItems_WarehouseId",
                table: "WarehouseItems",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryUpdateRecords_SKUCode",
                table: "InventoryUpdateRecords",
                column: "SKUCode");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryUpdateRecords_WarehouseId",
                table: "InventoryUpdateRecords",
                column: "WarehouseId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WarehouseItems_Qty",
                table: "WarehouseItems");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseItems_SKUCode",
                table: "WarehouseItems");

            migrationBuilder.DropIndex(
                name: "IX_WarehouseItems_WarehouseId",
                table: "WarehouseItems");

            migrationBuilder.DropIndex(
                name: "IX_InventoryUpdateRecords_SKUCode",
                table: "InventoryUpdateRecords");

            migrationBuilder.DropIndex(
                name: "IX_InventoryUpdateRecords_WarehouseId",
                table: "InventoryUpdateRecords");
        }
    }
}
