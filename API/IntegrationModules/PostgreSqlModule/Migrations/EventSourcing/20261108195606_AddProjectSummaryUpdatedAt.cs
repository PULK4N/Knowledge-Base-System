using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSqlModule.Migrations.EventSourcing
{
    /// <inheritdoc />
    public partial class AddProjectSummaryUpdatedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "PolicyProjectSummaries",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateIndex(
                name: "IX_PolicyProjectSummaries_UpdatedAt_ProjectName_ProjectAggrega~",
                table: "PolicyProjectSummaries",
                columns: new[] { "UpdatedAt", "ProjectName", "ProjectAggregateId" },
                descending: new[] { true, false, false });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PolicyProjectSummaries_UpdatedAt_ProjectName_ProjectAggrega~",
                table: "PolicyProjectSummaries");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PolicyProjectSummaries");
        }
    }
}
