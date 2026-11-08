using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSqlModule.Migrations.EventSourcing
{
    /// <inheritdoc />
    public partial class SeedSkillUseScenarioPolicies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
            """
            DO $seed$
            DECLARE
                next_order_number bigint;
            BEGIN
                IF NOT EXISTS (
                    SELECT 1 FROM "SerializedEventPayload"
                    WHERE "AggregateId" = '00000000-0000-0000-0000-000000000002'
                    AND "SerializedJsonData" LIKE '%01a1bb03-e1af-7b8b-bd69-9df2bed886fc%'
                ) THEN
                    SELECT COALESCE(MAX("OrderNumber"), 0) + 1
                    INTO next_order_number
                    FROM "SerializedEventPayload"
                    WHERE "AggregateId" = '00000000-0000-0000-0000-000000000002';

                    INSERT INTO "SerializedEventPayload"
                        ("AggregateId", "OrderNumber", "Timestamp", "EventExecutor", "EventName", "StateMachineId", "SerializedJsonData")
                    VALUES
                    ('00000000-0000-0000-0000-000000000002', next_order_number, '2026-11-08T11:55:56.206524Z', '01a1bb03-e1ae-7e1a-a7c5-4346f6896ff9', 'GeneralPolicyAddedV1', 'general-policies-state-machine', $policy${"Policy":{"PolicyId":{"Value":"01a1bb03-e1af-7b8b-bd69-9df2bed886fc"},"Title":"Load skills by their use scenarios","Description":"A skill's use scenarios list the kinds of work the skill applies to, independent of the subject. At the start of every task, match the work you are about to do against the use scenarios of the current project's linked skills, and load every matching skill before starting."}}$policy$);

                    INSERT INTO "SerializedPayloadMessage"
                        ("AggregateId", "SerializedEventExecutionInfo", "SerializedEventData", "ExecutionAttempts", "Status")
                    VALUES
                    ('00000000-0000-0000-0000-000000000002', replace($policy${"Timestamp":"2026-11-08T11:55:56.206524Z","AggregateId":{"Value":"00000000-0000-0000-0000-000000000002"},"EventExecutor":{"Value":"01a1bb03-e1ae-7e1a-a7c5-4346f6896ff9"},"OnBehalfOf":null,"OrderNumber":0,"EventName":"GeneralPolicyAddedV1","StateMachineId":"general-policies-state-machine","NewState":null}$policy$, $policy$"OrderNumber":0$policy$, '"OrderNumber":' || next_order_number), $policy${"Policy":{"PolicyId":{"Value":"01a1bb03-e1af-7b8b-bd69-9df2bed886fc"},"Title":"Load skills by their use scenarios","Description":"A skill's use scenarios list the kinds of work the skill applies to, independent of the subject. At the start of every task, match the work you are about to do against the use scenarios of the current project's linked skills, and load every matching skill before starting."}}$policy$, 0, 0);
                END IF;

                IF NOT EXISTS (
                    SELECT 1 FROM "SerializedEventPayload"
                    WHERE "AggregateId" = '00000000-0000-0000-0000-000000000002'
                    AND "SerializedJsonData" LIKE '%01a1bb03-e1b0-7cc5-8443-5e41f27ce357%'
                ) THEN
                    SELECT COALESCE(MAX("OrderNumber"), 0) + 1
                    INTO next_order_number
                    FROM "SerializedEventPayload"
                    WHERE "AggregateId" = '00000000-0000-0000-0000-000000000002';

                    INSERT INTO "SerializedEventPayload"
                        ("AggregateId", "OrderNumber", "Timestamp", "EventExecutor", "EventName", "StateMachineId", "SerializedJsonData")
                    VALUES
                    ('00000000-0000-0000-0000-000000000002', next_order_number, '2026-11-08T11:55:56.206524Z', '01a1bb03-e1ae-7e1a-a7c5-4346f6896ff9', 'GeneralPolicyAddedV1', 'general-policies-state-machine', $policy${"Policy":{"PolicyId":{"Value":"01a1bb03-e1b0-7cc5-8443-5e41f27ce357"},"Title":"Write short skill use scenarios","Description":"Write each use scenario as one short line naming a kind of work, not a subject or a skill summary, for example \"Writing or reviewing Angular code\". Keep it to a few words, with no explanations or examples. When you load or link a skill that has no use scenarios, propose its scenarios in chat and set them with `skill_use_scenarios_set` after the user approves."}}$policy$);

                    INSERT INTO "SerializedPayloadMessage"
                        ("AggregateId", "SerializedEventExecutionInfo", "SerializedEventData", "ExecutionAttempts", "Status")
                    VALUES
                    ('00000000-0000-0000-0000-000000000002', replace($policy${"Timestamp":"2026-11-08T11:55:56.206524Z","AggregateId":{"Value":"00000000-0000-0000-0000-000000000002"},"EventExecutor":{"Value":"01a1bb03-e1ae-7e1a-a7c5-4346f6896ff9"},"OnBehalfOf":null,"OrderNumber":0,"EventName":"GeneralPolicyAddedV1","StateMachineId":"general-policies-state-machine","NewState":null}$policy$, $policy$"OrderNumber":0$policy$, '"OrderNumber":' || next_order_number), $policy${"Policy":{"PolicyId":{"Value":"01a1bb03-e1b0-7cc5-8443-5e41f27ce357"},"Title":"Write short skill use scenarios","Description":"Write each use scenario as one short line naming a kind of work, not a subject or a skill summary, for example \"Writing or reviewing Angular code\". Keep it to a few words, with no explanations or examples. When you load or link a skill that has no use scenarios, propose its scenarios in chat and set them with `skill_use_scenarios_set` after the user approves."}}$policy$, 0, 0);
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
