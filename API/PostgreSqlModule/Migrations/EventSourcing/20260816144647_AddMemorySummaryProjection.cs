using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSqlModule.Migrations.EventSourcing
{
    /// <inheritdoc />
    public partial class AddMemorySummaryProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MemorySummaryEntries",
                columns: table => new
                {
                    MemoryAggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uuid", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    PromptCount = table.Column<int>(type: "integer", nullable: false),
                    FirstPromptTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastPromptTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SummaryTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastActivityTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemorySummaryEntries", x => x.MemoryAggregateId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MemorySummaryEntries_LastActivityTimestamp",
                table: "MemorySummaryEntries",
                column: "LastActivityTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_MemorySummaryEntries_ThreadId",
                table: "MemorySummaryEntries",
                column: "ThreadId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MemorySummaryEntries");
        }
    }
}
