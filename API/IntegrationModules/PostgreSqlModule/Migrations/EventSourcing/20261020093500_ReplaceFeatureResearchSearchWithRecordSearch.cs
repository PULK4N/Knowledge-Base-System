using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NpgsqlTypes;
using Pgvector;

#nullable disable

namespace PostgreSqlModule.Migrations.EventSourcing
{
    /// <inheritdoc />
    public partial class ReplaceFeatureResearchSearchWithRecordSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeatureResearchSearchEntries");

            migrationBuilder.CreateTable(
                name: "FeatureRecordSearchEntries",
                columns: table => new
                {
                    FeatureAggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChunkIndex = table.Column<int>(type: "integer", nullable: false),
                    FeatureName = table.Column<string>(type: "text", nullable: false),
                    RecordType = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    SourceType = table.Column<string>(type: "text", nullable: false),
                    SourceReference = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(1024)", nullable: false),
                    SearchVector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: false)
                        .Annotation("Npgsql:TsVectorConfig", "simple")
                        .Annotation("Npgsql:TsVectorProperties", new[] { "FeatureName", "RecordType", "Title", "SourceType", "SourceReference", "Text" })
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureRecordSearchEntries", x => new { x.FeatureAggregateId, x.RecordId, x.ChunkIndex });
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureRecordSearchEntries_Embedding",
                table: "FeatureRecordSearchEntries",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureRecordSearchEntries_SearchVector",
                table: "FeatureRecordSearchEntries",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeatureRecordSearchEntries");

            migrationBuilder.CreateTable(
                name: "FeatureResearchSearchEntries",
                columns: table => new
                {
                    FeatureAggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ResearchDiscoveryId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChunkIndex = table.Column<int>(type: "integer", nullable: false),
                    Embedding = table.Column<Vector>(type: "vector(1024)", nullable: false),
                    FeatureName = table.Column<string>(type: "text", nullable: false),
                    SearchVector = table.Column<NpgsqlTsVector>(type: "tsvector", nullable: false)
                        .Annotation("Npgsql:TsVectorConfig", "simple")
                        .Annotation("Npgsql:TsVectorProperties", new[] { "FeatureName", "Title", "SourceType", "SourceReference", "Text" }),
                    SourceReference = table.Column<string>(type: "text", nullable: false),
                    SourceType = table.Column<string>(type: "text", nullable: false),
                    Text = table.Column<string>(type: "text", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureResearchSearchEntries", x => new { x.FeatureAggregateId, x.ResearchDiscoveryId, x.ChunkIndex });
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureResearchSearchEntries_Embedding",
                table: "FeatureResearchSearchEntries",
                column: "Embedding")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureResearchSearchEntries_SearchVector",
                table: "FeatureResearchSearchEntries",
                column: "SearchVector")
                .Annotation("Npgsql:IndexMethod", "GIN");
        }
    }
}
