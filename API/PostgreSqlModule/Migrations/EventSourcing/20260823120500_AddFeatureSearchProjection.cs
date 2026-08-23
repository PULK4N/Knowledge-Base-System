using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PostgreSqlModule.Migrations.EventSourcing
{
    /// <inheritdoc />
    public partial class AddFeatureSearchProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .Annotation("Npgsql:PostgresExtension:vector", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "FeatureSearchEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FeatureAggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    NormalizedName = table.Column<string>(type: "text", nullable: false),
                    Summary = table.Column<string>(type: "text", nullable: false),
                    SearchText = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<string>(type: "text", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CurrentPlanId = table.Column<Guid>(type: "uuid", nullable: true),
                    PlanCount = table.Column<int>(type: "integer", nullable: false),
                    RecordCount = table.Column<int>(type: "integer", nullable: false),
                    ProjectedOrderNumber = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureSearchEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FeatureSearchEntries_FeatureAggregateId",
                table: "FeatureSearchEntries",
                column: "FeatureAggregateId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeatureSearchEntries_NormalizedName_Name_FeatureAggregateId",
                table: "FeatureSearchEntries",
                columns: new[] { "NormalizedName", "Name", "FeatureAggregateId" },
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureSearchEntries_PlanCount_FeatureAggregateId",
                table: "FeatureSearchEntries",
                columns: new[] { "PlanCount", "FeatureAggregateId" },
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureSearchEntries_ProjectId_NormalizedName_Name_FeatureA~",
                table: "FeatureSearchEntries",
                columns: new[] { "ProjectId", "NormalizedName", "Name", "FeatureAggregateId" },
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureSearchEntries_ProjectId_PlanCount_FeatureAggregateId",
                table: "FeatureSearchEntries",
                columns: new[] { "ProjectId", "PlanCount", "FeatureAggregateId" },
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureSearchEntries_ProjectId_RecordCount_FeatureAggregate~",
                table: "FeatureSearchEntries",
                columns: new[] { "ProjectId", "RecordCount", "FeatureAggregateId" },
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureSearchEntries_RecordCount_FeatureAggregateId",
                table: "FeatureSearchEntries",
                columns: new[] { "RecordCount", "FeatureAggregateId" },
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureSearchEntries_SearchText",
                table: "FeatureSearchEntries",
                column: "SearchText",
                filter: "\"IsDeleted\" = FALSE")
                .Annotation("Npgsql:IndexMethod", "GIN")
                .Annotation("Npgsql:IndexOperators", new[] { "gin_trgm_ops" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FeatureSearchEntries");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:pg_trgm", ",,")
                .OldAnnotation("Npgsql:PostgresExtension:vector", ",,");
        }
    }
}
