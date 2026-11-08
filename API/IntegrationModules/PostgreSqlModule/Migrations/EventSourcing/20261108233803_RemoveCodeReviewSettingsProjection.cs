using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSqlModule.Migrations.EventSourcing
{
    /// <inheritdoc />
    public partial class RemoveCodeReviewSettingsProjection : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodeReviewSettings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CodeReviewSettings",
                columns: table => new
                {
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchSize = table.Column<int>(type: "integer", nullable: false),
                    OrderNumber = table.Column<long>(type: "bigint", nullable: false),
                    TaskMessage = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeReviewSettings", x => x.AggregateId);
                    table.CheckConstraint("CK_CodeReviewSettings_BatchSize", "\"BatchSize\" > 0");
                });
        }
    }
}
