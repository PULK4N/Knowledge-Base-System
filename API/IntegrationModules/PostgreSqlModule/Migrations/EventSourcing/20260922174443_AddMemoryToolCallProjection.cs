using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSqlModule.Migrations.EventSourcing
{
    /// <inheritdoc />
    public partial class AddMemoryToolCallProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemoryToolCallEntries",
                columns: table => new
                {
                    MemoryAggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    PromptId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToolCallIndex = table.Column<int>(type: "integer", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ToolName = table.Column<string>(type: "text", nullable: false),
                    ToolUseId = table.Column<string>(type: "text", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryToolCallEntries", x => new { x.MemoryAggregateId, x.PromptId, x.ToolCallIndex });
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemoryToolCallEntries_MemoryAggregateId_Timestamp",
                table: "MemoryToolCallEntries",
                columns: new[] { "MemoryAggregateId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_MemoryToolCallEntries_ThreadId",
                table: "MemoryToolCallEntries",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_MemoryToolCallEntries_ToolName",
                table: "MemoryToolCallEntries",
                column: "ToolName");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemoryToolCallEntries");
        }
    }
}
