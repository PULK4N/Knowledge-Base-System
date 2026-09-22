using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSqlModule.Migrations.EventSourcing
{
    /// <inheritdoc />
    public partial class AddMemoryToolCallContentSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_MemoryToolCallEntries_Description",
                table: "MemoryToolCallEntries",
                column: "Description")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_MemoryToolCallEntries_PayloadJson",
                table: "MemoryToolCallEntries",
                column: "PayloadJson")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_MemoryToolCallEntries_ToolName_Trigram",
                table: "MemoryToolCallEntries",
                column: "ToolName")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MemoryToolCallEntries_Description",
                table: "MemoryToolCallEntries");

            migrationBuilder.DropIndex(
                name: "IX_MemoryToolCallEntries_PayloadJson",
                table: "MemoryToolCallEntries");

            migrationBuilder.DropIndex(
                name: "IX_MemoryToolCallEntries_ToolName_Trigram",
                table: "MemoryToolCallEntries");
        }
    }
}
