# Code reviewers — implementation and verification

## Implemented flow

The coordinator discovers all inputs from the checkout and KB; each reviewer can also be invoked directly with saved parameters.

```mermaid
flowchart TD
    A[run_code_review.py: no arguments] --> B[Repository and current branch]
    B --> C[Feature review-context API: feature, plan and actual origin]
    C --> D[One captured branch-tip diff]
    D --> E[Shared session per model and reasoning level]
    E --> F[Batches from settings: initially 5 reviewers]
    F --> G[run_codex_reviewers.py: independent cached-session forks]
    G --> H[Readable inputs, individual attempts, status and report.md]
```

## Settings

Settings are event-sourced without a projection: the settings query replays the singleton settings aggregate. The initial values are seeded as a paired event and outbox row; the outbox row is completed without publishing because the state machine declares no projections.

```mermaid
flowchart LR
    A[Migration: SettingsInitializedV1 and outbox row] --> B[settings-state-machine]
    C[Administration: Code reviewers] --> D[Settings update API]
    D --> B
    B --> E[(Settings event stream)]
    E -->|replay| G[Settings query API]
    G --> C
    G --> H[Coordinator]
```

## Direct reruns

The calling agent uses the saved runner parameters; the runner does not fetch feature or plan data again.

```mermaid
flowchart LR
    A[Failed reviewer details in coordinator output] --> B[Cache ID, model, reasoning and input file]
    B --> C[Direct reviewer invocation with expected HEAD]
    C --> D{Checkout and cache match?}
    D -->|Yes| E[New attempt directory and result]
    D -->|No| F[Explicit failure with diagnostics]
```

## Verification

| Check | Result |
| --- | --- |
| Settings domain, event-replay query and real HTTP API | 8 tests passed |
| Feature application, including origin resolution | 68 tests passed |
| Feature API regression checks | 4 tests passed |
| Administration frontend | 32 tests passed |
| Python discovery, batches, failures, reruns and cancellation | 7 tests passed |
| Native coordinator with a fixture KB and temporary Git checkout | Project and feature reviewers completed using one shared session |
| Native cached-input usage on that fixture | Approximately 98% for both reviewers |
| Direct native rerun after fixture KB shutdown | Completed; previous attempt preserved |
| PostgreSQL migration in a scratch database | One initial event, one outbox row, batch size 5; no projection insert; seed guard did not duplicate rows |
| Playwright against rebuilt frontend | Load error, initial value, validation, save/reload, failed-save recovery and mobile layout passed using an isolated HTTP fixture |
| Frontend container | Rebuilt and running on port 4200 |

## Review and operational notes

| Item | Outcome |
| --- | --- |
| Request record validation | Corrected attribute placement; HTTP regression test passes |
| Controller discovery | Explicitly registered the settings controller assembly |
| Missing branch-origin references | Rejected instead of falling back to another branch |
| Python 3.8 text writes | Compatible file API used |
| Existing event-sourcing framework | No source changes |
| Windows runtime | Not executed on this Linux host; platform-specific process handling retained and portable script paths used |
| Existing frontend CSS budget warning | Feature details CSS warning remains unrelated to this change |

## Activate the backend

Backend rollout remains with the user; the running API container was not rebuilt or restarted.

```mermaid
flowchart LR
    A[Committed backend changes] --> B[User rebuilds API]
    B --> C[Startup applies settings migration]
    C --> D[Outbox projects initial settings]
    D --> E[Administration and coordinator use new APIs]
    E --> F[Configure project review models and reviewers]
```

See [the script usage guide](../../scripts/codex/README.md) for prerequisites, environment variables, artifacts and direct invocation details.
