# PRD Template (CCPM)

- Date: YYYY-MM-DD
- PM: ccpm (Claude Code Project Manager)
- Scope: <paths/solutions/upstream-downstream>

## Background
- Problem statement with evidence (file paths and line references).

## Goals
- Measurable targets (build/perf/stability/UX/process).

## Non-Goals
- Explicitly out-of-scope items to prevent scope creep.

## User Stories
- Role + Need + Value.

## Scope
- In Scope:
  - …
- Out of Scope:
  - …

## Requirements
- R1 … (Must)
- R2 … (Should)
- R3 … (Could)

## Success Metrics
- … (quantitative)

## Acceptance Criteria
- Steps/commands + expected outcomes (0 errors, artifact path, behavior correctness).
- Implementation MUST strictly follow this PRD. Any deviation requires PRD update and approval before coding.

## Milestones
- Commit 1: …
- Commit 2: …
- Commit 3: …

## Risks & Mitigations
- Risk → Mitigation …

## Dependencies & Preconditions
- SDK/tools/services/env/creds.

## Rollback
- By-commit rollback and data revert strategy.

## Testing
- Build/unit/integration/smoke/manual checklist.

## Deliverables
- Code/config/docs/scripts/instructions.
- Completion Summary including: changes overview, tests & validation, updated READMEs list with links, risks & follow-ups.
- Completion Summary path: `docs/prds-summary/PRD-<endpoint>[-<topic>]-<YYYYMMDD>-SUMMARY.md`.

Note: For formal PRD docs under `docs/ccpm/`, prefer short endpoint-first names, e.g. `PRD-server-YYYYMMDD.md` or `PRD-desktop-quickfix-YYYYMMDD.md`.
