#!/usr/bin/env python3
"""Add requirements, definitions of done and Playwright scripts from a JSON file.

The file uses local keys so items can link to each other before they have IDs:

{
  "requirements": [
    {"key": "copy", "type": "Functional", "description": "...", "externalId": "", "source": ""}
  ],
  "definitionsOfDone": [
    {"key": "copied", "description": "...", "requirements": ["copy"],
     "state": "Confirmed", "stateDescription": "How it was checked"}
  ],
  "playwrightScripts": [
    {"path": "e2e/copy.spec.ts", "definitionsOfDone": ["copied"], "snapshotFile": "e2e/copy.spec.ts"}
  ]
}

Run from the Knowledge-Base-System repository root:
  python scripts/add_plan_specification.py <featureId> <file.json> [--plan-id <planId>]
Without --plan-id the items go to the feature-wide specification.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path

from migrate_codex_skill import DEFAULT_BASE_URL, post_json
from update_feature_plan import uuid_argument


def main(argv: list[str] | None = None) -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("feature_id", type=uuid_argument)
    parser.add_argument("json_file", type=Path)
    parser.add_argument("--plan-id", type=uuid_argument, help="Target plan; omit for the feature")
    parser.add_argument("--base-url", default=DEFAULT_BASE_URL)
    parser.add_argument("--timeout", type=float, default=30.0)
    args = parser.parse_args(argv)

    spec = json.loads(args.json_file.read_text(encoding="utf-8"))
    scope = f"plans/{args.plan_id}/" if args.plan_id else ""
    base = f"{args.base_url.rstrip('/')}/api/features/{args.feature_id}/{scope}"

    def post(path: str, payload: dict[str, object]) -> dict[str, object]:
        return post_json(base + path, payload, args.timeout)

    def created(items: list[dict[str, object]], response: dict[str, object]) -> dict[str, str]:
        ids = response["items"]
        print("\n".join(f"{item['label']}  {item['id']}" for item in ids))
        return {item["key"]: result["id"] for item, result in zip(items, ids) if item.get("key")}

    requirement_ids: dict[str, str] = {}
    requirements = spec.get("requirements", [])
    if requirements:
        requirement_ids = created(requirements, post("requirements", {"requirements": [
            {key: item.get(key) for key in ("type", "description", "externalId", "source")}
            for item in requirements
        ]}))

    dod_ids: dict[str, str] = {}
    dods = spec.get("definitionsOfDone", [])
    if dods:
        dod_ids = created(dods, post("definitions-of-done", {"definitionsOfDone": [
            {"description": item["description"],
             "requirementIds": [requirement_ids[key] for key in item.get("requirements", [])]}
            for item in dods
        ]}))
        states = [
            {"id": dod_ids[item["key"]], "state": item["state"], "stateDescription": item.get("stateDescription", "")}
            for item in dods if item.get("state")
        ]
        if states:
            post("definitions-of-done/states", {"states": states})
            print(f"Set the state of {len(states)} definition(s) of done.")

    scripts = spec.get("playwrightScripts", [])
    if scripts:
        created(scripts, post("playwright-scripts", {"scripts": [
            {"path": item["path"],
             "definitionOfDoneIds": [dod_ids[key] for key in item.get("definitionsOfDone", [])],
             "snapshotContent": Path(item["snapshotFile"]).read_text(encoding="utf-8")
             if item.get("snapshotFile") else None}
            for item in scripts
        ]}))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
