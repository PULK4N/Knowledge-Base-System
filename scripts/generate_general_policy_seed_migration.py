#!/usr/bin/env python3
"""Fill an empty EF migration's Up method with seeded general policies.

Each Markdown file in the policy folder becomes one GeneralPolicyAddedV1
event plus its outbox message, in file-name order. The inserts only run when
the general policies state machine has no events yet, so existing databases
are left untouched. With --append, each policy is instead added after the
aggregate's latest event, once, guarded by its policy ID, so later seed
migrations reach existing databases too. IDs are PostgreSQL-friendly UUID v7
values generated once and written into the migration.

Example:
    ./scripts/generate_general_policy_seed_migration.py \
        ~/Pictures/policies/general \
        API/IntegrationModules/PostgreSqlModule/Migrations/EventSourcing/\
20261107174040_SeedGeneralPolicies.cs
"""

from __future__ import annotations

import argparse
import json
import os
import re
import sys
import uuid
from datetime import datetime, timezone
from pathlib import Path

from add_general_policy import read_policy_markdown

GENERAL_POLICIES_AGGREGATE_ID = "00000000-0000-0000-0000-000000000002"
STATE_MACHINE_ID = "general-policies-state-machine"
EVENT_NAME = "GeneralPolicyAddedV1"
NEW_MESSAGE_STATUS = 0
ORDER_NUMBER_PLACEHOLDER = 0
SQL_QUOTE_TAG = "$policy$"
EMPTY_UP_METHOD = re.compile(
    r"(protected override void Up\(MigrationBuilder migrationBuilder\)"
    r"\s*\{)\s*(\})"
)


def parse_args(argv: list[str] | None = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Write general policy seed inserts into an empty migration."
        )
    )
    parser.add_argument("policy_directory", type=Path)
    parser.add_argument("migration_file", type=Path)
    parser.add_argument(
        "--append",
        action="store_true",
        help=(
            "Add each policy after the latest general policies event instead "
            "of seeding an empty state machine."
        ),
    )
    return parser.parse_args(argv)


def uuid7(unix_milliseconds: int) -> str:
    """Builds an RFC 9562 UUID v7, the layout UUIDNext uses for PostgreSQL."""
    value = (unix_milliseconds & ((1 << 48) - 1)) << 80
    value |= int.from_bytes(os.urandom(10), "big")
    value &= ~(0xF << 76)
    value |= 0x7 << 76
    value &= ~(0x3 << 62)
    value |= 0x2 << 62
    return str(uuid.UUID(int=value))


def compact_json(value: object) -> str:
    return json.dumps(value, separators=(",", ":"))


def sql_text(value: str) -> str:
    if SQL_QUOTE_TAG in value:
        raise ValueError(f"Text contains the SQL quote tag {SQL_QUOTE_TAG}.")
    return f"{SQL_QUOTE_TAG}{value}{SQL_QUOTE_TAG}"


def read_policies(policy_directory: Path) -> list[tuple[str, str]]:
    if not policy_directory.is_dir():
        raise ValueError(f"Policy folder was not found: {policy_directory}")

    markdown_files = sorted(policy_directory.glob("*.md"))
    if not markdown_files:
        raise ValueError(f"No .md files found in: {policy_directory}")
    return [read_policy_markdown(file) for file in markdown_files]


def build_seed_sql(
    policies: list[tuple[str, str]],
    timestamp: datetime,
) -> str:
    base_milliseconds = int(timestamp.timestamp() * 1000)
    executor_id = uuid7(base_milliseconds)
    timestamp_text = timestamp.strftime("%Y-%m-%dT%H:%M:%S.%fZ")
    event_rows = []
    message_rows = []

    for order_number, (title, description) in enumerate(policies, start=1):
        policy_id = uuid7(base_milliseconds + order_number)
        event_data = compact_json(
            {
                "Policy": {
                    "PolicyId": {"Value": policy_id},
                    "Title": title,
                    "Description": description,
                }
            }
        )
        execution_info = compact_json(
            {
                "Timestamp": timestamp_text,
                "AggregateId": {"Value": GENERAL_POLICIES_AGGREGATE_ID},
                "EventExecutor": {"Value": executor_id},
                "OnBehalfOf": None,
                "OrderNumber": order_number,
                "EventName": EVENT_NAME,
                "StateMachineId": STATE_MACHINE_ID,
                "NewState": None,
            }
        )
        event_rows.append(
            f"('{GENERAL_POLICIES_AGGREGATE_ID}', {order_number}, "
            f"'{timestamp_text}', '{executor_id}', '{EVENT_NAME}', "
            f"'{STATE_MACHINE_ID}', {sql_text(event_data)})"
        )
        message_rows.append(
            f"('{GENERAL_POLICIES_AGGREGATE_ID}', {sql_text(execution_info)}, "
            f"{sql_text(event_data)}, 0, {NEW_MESSAGE_STATUS})"
        )

    separator = ",\n        "
    return (
        "DO $seed$\n"
        "BEGIN\n"
        "    IF NOT EXISTS (\n"
        '        SELECT 1 FROM "SerializedEventPayload"\n'
        f"        WHERE \"StateMachineId\" = '{STATE_MACHINE_ID}'\n"
        "    ) THEN\n"
        '        INSERT INTO "SerializedEventPayload"\n'
        '            ("AggregateId", "OrderNumber", "Timestamp", '
        '"EventExecutor", "EventName", "StateMachineId", '
        '"SerializedJsonData")\n'
        "        VALUES\n"
        f"        {separator.join(event_rows)};\n"
        "\n"
        '        INSERT INTO "SerializedPayloadMessage"\n'
        '            ("AggregateId", "SerializedEventExecutionInfo", '
        '"SerializedEventData", "ExecutionAttempts", "Status")\n'
        "        VALUES\n"
        f"        {separator.join(message_rows)};\n"
        "    END IF;\n"
        "END\n"
        "$seed$;"
    )


def build_append_sql(
    policies: list[tuple[str, str]],
    timestamp: datetime,
) -> str:
    base_milliseconds = int(timestamp.timestamp() * 1000)
    executor_id = uuid7(base_milliseconds)
    timestamp_text = timestamp.strftime("%Y-%m-%dT%H:%M:%S.%fZ")
    blocks = []

    for index, (title, description) in enumerate(policies, start=1):
        policy_id = uuid7(base_milliseconds + index)
        event_data = compact_json(
            {
                "Policy": {
                    "PolicyId": {"Value": policy_id},
                    "Title": title,
                    "Description": description,
                }
            }
        )
        execution_info = compact_json(
            {
                "Timestamp": timestamp_text,
                "AggregateId": {"Value": GENERAL_POLICIES_AGGREGATE_ID},
                "EventExecutor": {"Value": executor_id},
                "OnBehalfOf": None,
                "OrderNumber": ORDER_NUMBER_PLACEHOLDER,
                "EventName": EVENT_NAME,
                "StateMachineId": STATE_MACHINE_ID,
                "NewState": None,
            }
        )
        order_number_json = f'"OrderNumber":{ORDER_NUMBER_PLACEHOLDER}'
        blocks.append(
            "    IF NOT EXISTS (\n"
            '        SELECT 1 FROM "SerializedEventPayload"\n'
            f"        WHERE \"AggregateId\" = '{GENERAL_POLICIES_AGGREGATE_ID}'\n"
            f"        AND \"SerializedJsonData\" LIKE '%{policy_id}%'\n"
            "    ) THEN\n"
            '        SELECT COALESCE(MAX("OrderNumber"), 0) + 1\n'
            "        INTO next_order_number\n"
            '        FROM "SerializedEventPayload"\n'
            f"        WHERE \"AggregateId\" = '{GENERAL_POLICIES_AGGREGATE_ID}';\n"
            "\n"
            '        INSERT INTO "SerializedEventPayload"\n'
            '            ("AggregateId", "OrderNumber", "Timestamp", '
            '"EventExecutor", "EventName", "StateMachineId", '
            '"SerializedJsonData")\n'
            "        VALUES\n"
            f"        ('{GENERAL_POLICIES_AGGREGATE_ID}', next_order_number, "
            f"'{timestamp_text}', '{executor_id}', '{EVENT_NAME}', "
            f"'{STATE_MACHINE_ID}', {sql_text(event_data)});\n"
            "\n"
            '        INSERT INTO "SerializedPayloadMessage"\n'
            '            ("AggregateId", "SerializedEventExecutionInfo", '
            '"SerializedEventData", "ExecutionAttempts", "Status")\n'
            "        VALUES\n"
            f"        ('{GENERAL_POLICIES_AGGREGATE_ID}', replace("
            f"{sql_text(execution_info)}, {sql_text(order_number_json)}, "
            "'\"OrderNumber\":' || next_order_number), "
            f"{sql_text(event_data)}, 0, {NEW_MESSAGE_STATUS});\n"
            "    END IF;"
        )

    return (
        "DO $seed$\n"
        "DECLARE\n"
        "    next_order_number bigint;\n"
        "BEGIN\n"
        + "\n\n".join(blocks)
        + "\nEND\n"
        "$seed$;"
    )


def csharp_raw_string(text: str, indentation: str) -> str:
    longest_quote_run = max(
        (len(run) for run in re.findall(r'"+', text)),
        default=0,
    )
    delimiter = '"' * max(3, longest_quote_run + 1)
    indented = "\n".join(
        f"{indentation}{line}" if line else "" for line in text.splitlines()
    )
    return f"{delimiter}\n{indented}\n{indentation}{delimiter}"


def write_migration(migration_file: Path, seed_sql: str) -> None:
    source = migration_file.read_text(encoding="utf-8-sig")
    statement_indentation = " " * 12
    statement = (
        f"migrationBuilder.Sql(\n{statement_indentation}"
        f"{csharp_raw_string(seed_sql, statement_indentation)});"
    )
    updated, replacements = EMPTY_UP_METHOD.subn(
        lambda match: (
            f"{match.group(1)}\n{statement_indentation}{statement}\n"
            f"        {match.group(2)}"
        ),
        source,
        count=1,
    )
    if replacements != 1:
        raise ValueError(
            f"No empty Up method found in migration: {migration_file}"
        )
    migration_file.write_text(updated, encoding="utf-8-sig")


def main(argv: list[str] | None = None) -> int:
    args = parse_args(argv)
    try:
        policies = read_policies(args.policy_directory)
        build_sql = build_append_sql if args.append else build_seed_sql
        seed_sql = build_sql(policies, datetime.now(timezone.utc))
        write_migration(args.migration_file, seed_sql)
        print(f"Seeded {len(policies)} policies into {args.migration_file}")
        return 0
    except (OSError, UnicodeError, ValueError) as error:
        print(f"Generating seed migration failed: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
