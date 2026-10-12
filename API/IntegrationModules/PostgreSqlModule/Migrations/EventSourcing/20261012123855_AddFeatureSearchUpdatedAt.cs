using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSqlModule.Migrations.EventSourcing
{
    /// <inheritdoc />
    public partial class AddFeatureSearchUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "FeatureSearchEntries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_FeatureSearchEntries_ProjectId_UpdatedAt_FeatureAggregateId",
                table: "FeatureSearchEntries",
                columns: new[] { "ProjectId", "UpdatedAt", "FeatureAggregateId" },
                filter: "\"IsDeleted\" = FALSE");

            migrationBuilder.CreateIndex(
                name: "IX_FeatureSearchEntries_UpdatedAt_FeatureAggregateId",
                table: "FeatureSearchEntries",
                columns: new[] { "UpdatedAt", "FeatureAggregateId" },
                filter: "\"IsDeleted\" = FALSE");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_FeatureSearchEntries_ProjectId_UpdatedAt_FeatureAggregateId",
                table: "FeatureSearchEntries");

            migrationBuilder.DropIndex(
                name: "IX_FeatureSearchEntries_UpdatedAt_FeatureAggregateId",
                table: "FeatureSearchEntries");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "FeatureSearchEntries");
        }
    }
}
