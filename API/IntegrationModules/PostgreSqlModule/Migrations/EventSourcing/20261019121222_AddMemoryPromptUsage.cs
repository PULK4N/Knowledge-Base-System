using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSqlModule.Migrations.EventSourcing
{
    /// <inheritdoc />
    public partial class AddMemoryPromptUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "CacheWriteInputTokens",
                table: "MemorySummaryEntries",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CachedInputTokens",
                table: "MemorySummaryEntries",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "InputTokens",
                table: "MemorySummaryEntries",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "OutputTokens",
                table: "MemorySummaryEntries",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReasoningOutputTokens",
                table: "MemorySummaryEntries",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CacheWriteInputTokens",
                table: "MemoryConversationEntries",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CachedInputTokens",
                table: "MemoryConversationEntries",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "InputTokens",
                table: "MemoryConversationEntries",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "OutputTokens",
                table: "MemoryConversationEntries",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReasoningOutputTokens",
                table: "MemoryConversationEntries",
                type: "bigint",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CacheWriteInputTokens",
                table: "MemorySummaryEntries");

            migrationBuilder.DropColumn(
                name: "CachedInputTokens",
                table: "MemorySummaryEntries");

            migrationBuilder.DropColumn(
                name: "InputTokens",
                table: "MemorySummaryEntries");

            migrationBuilder.DropColumn(
                name: "OutputTokens",
                table: "MemorySummaryEntries");

            migrationBuilder.DropColumn(
                name: "ReasoningOutputTokens",
                table: "MemorySummaryEntries");

            migrationBuilder.DropColumn(
                name: "CacheWriteInputTokens",
                table: "MemoryConversationEntries");

            migrationBuilder.DropColumn(
                name: "CachedInputTokens",
                table: "MemoryConversationEntries");

            migrationBuilder.DropColumn(
                name: "InputTokens",
                table: "MemoryConversationEntries");

            migrationBuilder.DropColumn(
                name: "OutputTokens",
                table: "MemoryConversationEntries");

            migrationBuilder.DropColumn(
                name: "ReasoningOutputTokens",
                table: "MemoryConversationEntries");
        }
    }
}
