using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSqlModule.Migrations.EventSourcing
{
    /// <inheritdoc />
    public partial class AddMemoryConversationProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemoryConversationEntries",
                columns: table => new
                {
                    MemoryAggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    PromptId = table.Column<Guid>(type: "uuid", nullable: false),
                    HookIndex = table.Column<int>(type: "integer", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HookEventName = table.Column<string>(type: "text", nullable: false),
                    Role = table.Column<string>(type: "text", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false),
                    PayloadJson = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemoryConversationEntries", x => new { x.MemoryAggregateId, x.PromptId, x.HookIndex });
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemoryConversationEntries_MemoryAggregateId_Timestamp",
                table: "MemoryConversationEntries",
                columns: new[] { "MemoryAggregateId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_MemoryConversationEntries_ThreadId",
                table: "MemoryConversationEntries",
                column: "ThreadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemoryConversationEntries");
        }
    }
}
