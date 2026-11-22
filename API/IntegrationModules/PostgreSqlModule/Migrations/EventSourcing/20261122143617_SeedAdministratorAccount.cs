using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSqlModule.Migrations.EventSourcing
{
    /// <summary>
    /// Seeds the administrator account: its <c>AdministratorCreatedV1</c> event, the outbox message that
    /// projects it, and the unique username row that the state machine would write for <c>admin</c>.
    /// No command creates the administrator, so this migration is its only source.
    /// </summary>
    public partial class SeedAdministratorAccount : Migration
    {
        /// <summary>
        /// SHA-256 of the <c>UniqueAccountUserNameConstraint</c> for the username <c>admin</c>, as built by
        /// <c>UniqueConstraint</c>. A test checks it against the constraint creator.
        /// </summary>
        public const string AdministratorUserNameConstraintHash =
            "42cc1d4c30306b9e5af14146826e1d394568c6890f67dcc29726724405d16b03";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                $$"""
                DO $seed$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM "SerializedEventPayload"
                        WHERE "AggregateId" = '00000000-0000-0000-0000-000000000005') THEN
                        INSERT INTO "SerializedEventPayload"
                            ("AggregateId", "OrderNumber", "Timestamp", "EventExecutor", "EventName", "StateMachineId", "SerializedJsonData")
                        VALUES ('00000000-0000-0000-0000-000000000005', 1, '2026-11-22T14:36:17Z',
                            '10000000-0000-0000-0000-000000000000', 'AdministratorCreatedV1', 'identity-state-machine', $data${"UserName":"admin"}$data$);
                        INSERT INTO "SerializedPayloadMessage"
                            ("AggregateId", "SerializedEventExecutionInfo", "SerializedEventData", "ExecutionAttempts", "Status")
                        VALUES ('00000000-0000-0000-0000-000000000005', $info${"Timestamp":"2026-11-22T14:36:17Z","AggregateId":{"Value":"00000000-0000-0000-0000-000000000005"},"EventExecutor":{"Value":"10000000-0000-0000-0000-000000000000"},"OnBehalfOf":null,"OrderNumber":1,"EventName":"AdministratorCreatedV1","StateMachineId":"identity-state-machine","NewState":null}$info$, $data${"UserName":"admin"}$data$, 0, 0);
                        INSERT INTO "UniqueConstraints" ("AggregateId", "ConstraintName", "Hash")
                        VALUES ('00000000-0000-0000-0000-000000000005', 'UniqueAccountUserNameConstraint',
                            '\x{{AdministratorUserNameConstraintHash}}'::bytea);
                    END IF;
                END
                $seed$;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
