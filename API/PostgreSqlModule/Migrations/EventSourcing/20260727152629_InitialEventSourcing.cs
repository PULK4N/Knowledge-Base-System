using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PostgreSqlModule.Migrations.EventSourcing
{
    /// <inheritdoc />
    public partial class InitialEventSourcing : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SerializedEventPayload",
                columns: table => new
                {
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<long>(type: "bigint", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EventExecutor = table.Column<Guid>(type: "uuid", nullable: false),
                    EventName = table.Column<string>(type: "text", nullable: false),
                    StateMachineId = table.Column<string>(type: "text", nullable: false),
                    SerializedJsonData = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SerializedEventPayload", x => new { x.AggregateId, x.OrderNumber });
                });

            migrationBuilder.CreateTable(
                name: "SerializedPayloadMessage",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    SerializedEventExecutionInfo = table.Column<string>(type: "text", nullable: false),
                    SerializedEventData = table.Column<string>(type: "text", nullable: false),
                    ExecutionAttempts = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SerializedPayloadMessage", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UniqueEventConstraints",
                columns: table => new
                {
                    ConstraintHash = table.Column<byte[]>(type: "bytea", fixedLength: true, maxLength: 32, nullable: false),
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<long>(type: "bigint", nullable: false),
                    ConstraintName = table.Column<string>(type: "text", nullable: false),
                    StateMachineId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UniqueEventConstraints", x => x.ConstraintHash);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SerializedEventPayload");

            migrationBuilder.DropTable(
                name: "SerializedPayloadMessage");

            migrationBuilder.DropTable(
                name: "UniqueEventConstraints");
        }
    }
}
