using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PostgreSqlModule.Migrations.EventSourcing
{
    /// <inheritdoc />
    public partial class ReplaceUniqueEventConstraintsWithUniqueConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UniqueEventConstraints");

            migrationBuilder.CreateTable(
                name: "UniqueConstraints",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConstraintName = table.Column<string>(type: "text", nullable: false),
                    Hash = table.Column<byte[]>(type: "bytea", fixedLength: true, maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UniqueConstraints", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UniqueConstraints_AggregateId",
                table: "UniqueConstraints",
                column: "AggregateId");

            migrationBuilder.CreateIndex(
                name: "IX_UniqueConstraints_Hash",
                table: "UniqueConstraints",
                column: "Hash",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UniqueConstraints");

            migrationBuilder.CreateTable(
                name: "UniqueEventConstraints",
                columns: table => new
                {
                    ConstraintHash = table.Column<byte[]>(type: "bytea", fixedLength: true, maxLength: 32, nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ConstraintName = table.Column<string>(type: "text", nullable: false),
                    OrderNumber = table.Column<long>(type: "bigint", nullable: false),
                    StateMachineId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UniqueEventConstraints", x => x.ConstraintHash);
                });
        }
    }
}
