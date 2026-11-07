using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Bariza.Infrastructure.Migrations
{
    public partial class AddSystemLogs : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SystemLogs",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TimestampUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Level = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Message = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false),
                    MessageTemplate = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    Exception = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                    SourceContext = table.Column<string>(type: "TEXT", maxLength: 256, nullable: true),
                    UserId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    UserName = table.Column<string>(type: "TEXT", maxLength: 120, nullable: true),
                    RequestPath = table.Column<string>(type: "TEXT", maxLength: 512, nullable: true),
                    RequestMethod = table.Column<string>(type: "TEXT", maxLength: 16, nullable: true),
                    StatusCode = table.Column<int>(type: "INTEGER", nullable: true),
                    TraceId = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    Properties = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_Level_TimestampUtc",
                table: "SystemLogs",
                columns: new[] { "Level", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_SourceContext",
                table: "SystemLogs",
                column: "SourceContext");

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_TimestampUtc",
                table: "SystemLogs",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_SystemLogs_UserId",
                table: "SystemLogs",
                column: "UserId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SystemLogs");
        }
    }
}
