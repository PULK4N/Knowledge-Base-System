using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSqlModule.Migrations.EventSourcing
{
    /// <inheritdoc />
    public partial class AddMemoryPromptModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<List<string>>(
                name: "Models",
                table: "MemorySummaryEntries",
                type: "text[]",
                nullable: false,
                defaultValueSql: "ARRAY[]::text[]");

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "MemorySearchEntries",
                type: "text",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemorySummaryEntries_Models",
                table: "MemorySummaryEntries",
                column: "Models")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_MemorySearchEntries_Model",
                table: "MemorySearchEntries",
                column: "Model");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MemorySummaryEntries_Models",
                table: "MemorySummaryEntries");

            migrationBuilder.DropIndex(
                name: "IX_MemorySearchEntries_Model",
                table: "MemorySearchEntries");

            migrationBuilder.DropColumn(
                name: "Models",
                table: "MemorySummaryEntries");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "MemorySearchEntries");
        }
    }
}
