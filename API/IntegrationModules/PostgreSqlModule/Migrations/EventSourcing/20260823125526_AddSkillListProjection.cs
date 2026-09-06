using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PostgreSqlModule.Migrations.EventSourcing
{
    /// <inheritdoc />
    public partial class AddSkillListProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SkillListEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SkillAggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NormalizedName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    SearchText = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    ReferenceCount = table.Column<int>(type: "integer", nullable: false),
                    AttachmentCount = table.Column<int>(type: "integer", nullable: false),
                    ProjectedOrderNumber = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillListEntries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkillListTags",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SkillListEntryId = table.Column<int>(type: "integer", nullable: false),
                    Tag = table.Column<string>(type: "text", nullable: false),
                    NormalizedTag = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillListTags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SkillListTags_SkillListEntries_SkillListEntryId",
                        column: x => x.SkillListEntryId,
                        principalTable: "SkillListEntries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SkillListEntries_AttachmentCount_SkillAggregateId",
                table: "SkillListEntries",
                columns: new[] { "AttachmentCount", "SkillAggregateId" },
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_SkillListEntries_NormalizedName_Name_SkillAggregateId",
                table: "SkillListEntries",
                columns: new[] { "NormalizedName", "Name", "SkillAggregateId" },
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_SkillListEntries_ReferenceCount_SkillAggregateId",
                table: "SkillListEntries",
                columns: new[] { "ReferenceCount", "SkillAggregateId" },
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_SkillListEntries_SearchText",
                table: "SkillListEntries",
                column: "SearchText",
                filter: "\"IsDeleted\" = FALSE")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_SkillListEntries_SkillAggregateId",
                table: "SkillListEntries",
                column: "SkillAggregateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillListTags_NormalizedTag_SkillListEntryId",
                table: "SkillListTags",
                columns: new[] { "NormalizedTag", "SkillListEntryId" });

            migrationBuilder.CreateIndex(
                name: "IX_SkillListTags_SkillListEntryId_NormalizedTag",
                table: "SkillListTags",
                columns: new[] { "SkillListEntryId", "NormalizedTag" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SkillListTags");

            migrationBuilder.DropTable(
                name: "SkillListEntries");
        }
    }
}
