using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSqlModule.Migrations.EventSourcing
{
    /// <inheritdoc />
    public partial class AddSkillUseScenarios : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UseScenariosJson",
                table: "SkillSummaries",
                type: "text",
                nullable: false,
                defaultValue: "[]");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UseScenariosJson",
                table: "SkillSummaries");
        }
    }
}
