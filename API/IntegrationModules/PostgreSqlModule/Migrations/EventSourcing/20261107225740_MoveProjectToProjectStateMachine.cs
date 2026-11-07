using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSqlModule.Migrations.EventSourcing
{
    /// <inheritdoc />
    public partial class MoveProjectToProjectStateMachine : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
            """
            UPDATE "SerializedEventPayload"
            SET "StateMachineId" = 'project-state-machine'
            WHERE "StateMachineId" = 'project-policies-state-machine';
            """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
            """
            UPDATE "SerializedEventPayload"
            SET "StateMachineId" = 'project-policies-state-machine'
            WHERE "StateMachineId" = 'project-state-machine';
            """);
        }
    }
}
