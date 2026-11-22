using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSqlModule.Migrations.EventSourcing
{
    /// <inheritdoc />
    public partial class AddAccountList : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AccountList",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserName = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    IsAdministrator = table.Column<bool>(type: "boolean", nullable: false),
                    IsAgent = table.Column<bool>(type: "boolean", nullable: false),
                    Permissions = table.Column<List<string>>(type: "text[]", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProjectedOrderNumber = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountList", x => x.AccountId);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountList");
        }
    }
}
