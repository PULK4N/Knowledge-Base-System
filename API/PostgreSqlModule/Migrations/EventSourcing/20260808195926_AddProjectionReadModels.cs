using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using NpgsqlTypes;
using Pgvector;

#nullable disable

namespace PostgreSqlModule.Migrations.EventSourcing
{
    /// <inheritdoc />
    public partial class AddProjectionReadModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "GeneralPolicyTexts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneralPolicyTexts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MemorySearchEntries",
                columns: table => new
                {
                    MemoryAggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    PromptId = table.Column<Guid>(type: "uuid", nullable: false),
                    HookIndex = table.Column<int>(type: "integer", nullable: false),
                    ChunkIndex = table.Column<int>(type: "integer", nullable: false),
                    ThreadId = table.Column<Guid>(type: "uuid", nullable: false),
                    PromptStartTimestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    HookEventName = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(1024)", nullable: false),
                    SearchVector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: false)
                        .Annotation("Npgsql:TsVectorConfig", "simple")
                        .Annotation("Npgsql:TsVectorProperties", new[] { "HookEventName", "Text" })
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MemorySearchEntries", x => new { x.MemoryAggregateId, x.PromptId, x.HookIndex, x.ChunkIndex });
                });

            migrationBuilder.CreateTable(
                name: "ProjectPolicyTexts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectAggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectPolicyTexts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProjectPolicyTopics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProjectAggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    TopicName = table.Column<string>(type: "text", nullable: false),
                    TopicOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProjectPolicyTopics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SkillSearchEntries",
                columns: table => new
                {
                    SkillAggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    SourcePath = table.Column<string>(type: "text", nullable: false),
                    ChunkIndex = table.Column<int>(type: "integer", nullable: false),
                    SkillName = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(1024)", nullable: false),
                    SearchVector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: false)
                        .Annotation("Npgsql:TsVectorConfig", "simple")
                        .Annotation("Npgsql:TsVectorProperties", new[] { "SkillName", "SourcePath", "Text" })
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillSearchEntries", x => new { x.SkillAggregateId, x.SourcePath, x.ChunkIndex });
                });

            migrationBuilder.CreateTable(
                name: "SkillSummaries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SkillAggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SkillSummaries", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TopicPolicyTexts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TopicName = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TopicPolicyTexts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GeneralPolicyTexts_AggregateId",
                table: "GeneralPolicyTexts",
                column: "AggregateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MemorySearchEntries_Embedding",
                table: "MemorySearchEntries",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_MemorySearchEntries_PromptStartTimestamp",
                table: "MemorySearchEntries",
                column: "PromptStartTimestamp");

            migrationBuilder.CreateIndex(
                name: "IX_MemorySearchEntries_SearchVector",
                table: "MemorySearchEntries",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_MemorySearchEntries_ThreadId",
                table: "MemorySearchEntries",
                column: "ThreadId");

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPolicyTexts_ProjectAggregateId",
                table: "ProjectPolicyTexts",
                column: "ProjectAggregateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProjectPolicyTopics_ProjectAggregateId_TopicName",
                table: "ProjectPolicyTopics",
                columns: new[] { "ProjectAggregateId", "TopicName" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillSearchEntries_Embedding",
                table: "SkillSearchEntries",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_SkillSearchEntries_SearchVector",
                table: "SkillSearchEntries",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");

            migrationBuilder.CreateIndex(
                name: "IX_SkillSearchEntries_SkillName",
                table: "SkillSearchEntries",
                column: "SkillName");

            migrationBuilder.CreateIndex(
                name: "IX_SkillSummaries_Name",
                table: "SkillSummaries",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SkillSummaries_SkillAggregateId",
                table: "SkillSummaries",
                column: "SkillAggregateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TopicPolicyTexts_TopicName",
                table: "TopicPolicyTexts",
                column: "TopicName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GeneralPolicyTexts");

            migrationBuilder.DropTable(
                name: "MemorySearchEntries");

            migrationBuilder.DropTable(
                name: "ProjectPolicyTexts");

            migrationBuilder.DropTable(
                name: "ProjectPolicyTopics");

            migrationBuilder.DropTable(
                name: "SkillSearchEntries");

            migrationBuilder.DropTable(
                name: "SkillSummaries");

            migrationBuilder.DropTable(
                name: "TopicPolicyTexts");

            migrationBuilder.AlterDatabase()
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}
