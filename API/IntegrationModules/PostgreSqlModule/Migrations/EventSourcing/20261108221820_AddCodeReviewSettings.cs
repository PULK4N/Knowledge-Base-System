using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PostgreSqlModule.Migrations.EventSourcing
{
    /// <inheritdoc />
    public partial class AddCodeReviewSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CodeReviewSettings",
                columns: table => new
                {
                    AggregateId = table.Column<Guid>(type: "uuid", nullable: false),
                    BatchSize = table.Column<int>(type: "integer", nullable: false),
                    TaskMessage = table.Column<string>(type: "text", nullable: false),
                    OrderNumber = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CodeReviewSettings", x => x.AggregateId);
                    table.CheckConstraint("CK_CodeReviewSettings_BatchSize", "\"BatchSize\" > 0");
                });
            migrationBuilder.Sql(
                """
                DO $seed$
                BEGIN
                    IF NOT EXISTS (SELECT 1 FROM "SerializedEventPayload"
                        WHERE "AggregateId" = '00000000-0000-0000-0000-000000000004') THEN
                        INSERT INTO "SerializedEventPayload"
                            ("AggregateId", "OrderNumber", "Timestamp", "EventExecutor", "EventName", "StateMachineId", "SerializedJsonData")
                        VALUES ('00000000-0000-0000-0000-000000000004', 1, '2026-11-08T22:18:20Z',
                            '10000000-0000-0000-0000-000000000000', 'SettingsInitializedV1', 'settings-state-machine', $data${"BatchSize":5,"TaskMessage":"You are a code reviewer provided with feature details, a selected plan, review notes and code differences. For a project reviewer, use the supplied skill and optional reviewer note to find concrete code errors that indicate the skill was not followed properly. If the skill is not applicable, return \"Nothing found (skill not applicable)\". For a feature reviewer, use the supplied reviewer instructions to find concrete code errors; no skill is required. If no errors are found, return \"Nothing found\". For each actionable finding, include severity, file and line, the violated rule or instruction, concrete impact and the required change. Review the code without modifying it."}$data$);
                        INSERT INTO "SerializedPayloadMessage"
                            ("AggregateId", "SerializedEventExecutionInfo", "SerializedEventData", "ExecutionAttempts", "Status")
                        VALUES ('00000000-0000-0000-0000-000000000004', $info${"Timestamp":"2026-11-08T22:18:20Z","AggregateId":{"Value":"00000000-0000-0000-0000-000000000004"},"EventExecutor":{"Value":"10000000-0000-0000-0000-000000000000"},"OnBehalfOf":null,"OrderNumber":1,"EventName":"SettingsInitializedV1","StateMachineId":"settings-state-machine","NewState":null}$info$, $data${"BatchSize":5,"TaskMessage":"You are a code reviewer provided with feature details, a selected plan, review notes and code differences. For a project reviewer, use the supplied skill and optional reviewer note to find concrete code errors that indicate the skill was not followed properly. If the skill is not applicable, return \"Nothing found (skill not applicable)\". For a feature reviewer, use the supplied reviewer instructions to find concrete code errors; no skill is required. If no errors are found, return \"Nothing found\". For each actionable finding, include severity, file and line, the violated rule or instruction, concrete impact and the required change. Review the code without modifying it."}$data$, 0, 0);
                    END IF;
                END
                $seed$;
                """);

        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CodeReviewSettings");
        }
    }
}
