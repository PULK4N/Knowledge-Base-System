using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;

#nullable disable

namespace PostgreSqlModule.Migrations.EventSourcing
{
    /// <inheritdoc />
    public partial class AddMemoryToolCallFullTextSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InputText",
                table: "MemoryToolCallEntries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "OutputText",
                table: "MemoryToolCallEntries",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "InputSearchVector",
                table: "MemoryToolCallEntries",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "simple")
                .Annotation("Npgsql:TsVectorProperties", new[] { "InputText" });

            migrationBuilder.AddColumn<NpgsqlTsVector>(
                name: "OutputSearchVector",
                table: "MemoryToolCallEntries",
                type: "tsvector",
                nullable: false)
                .Annotation("Npgsql:TsVectorConfig", "simple")
                .Annotation("Npgsql:TsVectorProperties", new[] { "OutputText" });

            migrationBuilder.CreateIndex(
                name: "IX_MemoryToolCallEntries_InputSearchVector",
                table: "MemoryToolCallEntries",
                column: "InputSearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_MemoryToolCallEntries_OutputSearchVector",
                table: "MemoryToolCallEntries",
                column: "OutputSearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_MemoryToolCallEntries_PromptId_ToolCallIndex",
                table: "MemoryToolCallEntries",
                columns: new[] { "PromptId", "ToolCallIndex" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MemoryToolCallEntries_InputSearchVector",
                table: "MemoryToolCallEntries");

            migrationBuilder.DropIndex(
                name: "IX_MemoryToolCallEntries_OutputSearchVector",
                table: "MemoryToolCallEntries");

            migrationBuilder.DropIndex(
                name: "IX_MemoryToolCallEntries_PromptId_ToolCallIndex",
                table: "MemoryToolCallEntries");

            migrationBuilder.DropColumn(
                name: "InputSearchVector",
                table: "MemoryToolCallEntries");

            migrationBuilder.DropColumn(
                name: "InputText",
                table: "MemoryToolCallEntries");

            migrationBuilder.DropColumn(
                name: "OutputSearchVector",
                table: "MemoryToolCallEntries");

            migrationBuilder.DropColumn(
                name: "OutputText",
                table: "MemoryToolCallEntries");
        }
    }
}
