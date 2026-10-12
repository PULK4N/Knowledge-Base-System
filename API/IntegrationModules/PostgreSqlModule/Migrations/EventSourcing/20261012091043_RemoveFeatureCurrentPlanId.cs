using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSqlModule.Migrations.EventSourcing
{
    /// <inheritdoc />
    public partial class RemoveFeatureCurrentPlanId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentPlanId",
                table: "FeatureSummaries");

            migrationBuilder.DropColumn(
                name: "CurrentPlanId",
                table: "FeatureSearchEntries");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentPlanId",
                table: "FeatureSummaries",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentPlanId",
                table: "FeatureSearchEntries",
                type: "uuid",
                nullable: true);
        }
    }
}
