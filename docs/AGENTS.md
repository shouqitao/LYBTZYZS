<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# docs

## Purpose

Central documentation hub for the LYBTZYZS TCM clinic management system. Contains ~55 documentation files organized into 6 main sections covering product vision, requirements (PRD), architecture (including 8 ADRs), API reference, development guides, and operations. Also hosts active design/plan documents, code review notes, and training materials.

## Key Files

| File | Description |
|------|-------------|
| `README.md` | Documentation index with quick navigation for new developers, API developers, and architects |
| `api-endpoint-comparison.md` | Comparison of local vs remote API endpoints |
| `api-endpoint-gap-report.md` | Gap analysis between local and remote API surfaces |
| `local-api-alignment-plan.md` | Plan for aligning LocalWebAPI with remote WebAPI |
| `remote-vs-local-api-gap-report.md` | Detailed gap report for dual-mode API parity |

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| `01-product/` | Product vision, feature overview, user roles, clinical workflow (5 files) |
| `02-requirements/` | PRD with 14 modules, 131 User Stories, NFR, UI specifications (17 files) |
| `03-architecture/` | System architecture, data model, dual-mode design, 8 ADRs, error handling architecture (15 files) |
| `03-architecture/decisions/` | Architecture Decision Records (ADR-0001 through ADR-0008) |
| `03-architecture/localwebapi/` | LocalWebAPI-specific architecture docs |
| `04-api-reference/` | All API endpoint documentation (10 files) |
| `05-development/` | Quick start, coding standards, design patterns, testing guides (5 files) |
| `06-operations/` | Deployment, configuration, monitoring, logging (3 files) |
| `plans/` | Active and archived design/plan documents (~50 files) |
| `archives/` | Archived documentation |
| `code-review/` | Code review notes |
| `deployment/` | Deployment-specific guides |
| `planning/` | Planning documents |
| `requirements/` | Additional requirements docs |
| `superpowers/` | Superpowers skill documentation |
| `testing/` | Testing guides and strategies |
| `training/` | Training materials |

## For AI Agents

### Working In This Directory

- Documentation uses Chinese for prose, English for technical identifiers.
- Requirements use `US-XXX` numbering (User Story), architecture decisions use `ADR-XXX`.
- Each document includes a change log table at the bottom.
- `docs/plans/` contains active planning documents with date-prefixed filenames (e.g., `2026-05-04-code-review-fixes.md`).
- When adding new architecture decisions, create them in `docs/03-architecture/decisions/` following the ADR template.

### Testing Requirements

- No automated tests for documentation. Validate links manually.
- Use `scripts/docs-code-sync-check.ps1` to verify documentation stays in sync with code.
- Use `scripts/docs-maintenance-check.ps1` for documentation health checks.

### Common Patterns

- **Date-prefixed plans** -- Plan files use `YYYY-MM-DD-description.md` naming.
- **Section numbering** -- Major docs use `01-` through `06-` prefix for ordering.
- **ADR format** -- Architecture Decision Records follow standard ADR template with Context, Decision, Consequences sections.

## Dependencies

### Internal

- References code structure across the entire solution for documentation purposes.
- `scripts/` -- Documentation maintenance scripts.

### External

- *(none)* -- Pure documentation, no package dependencies

<!-- MANUAL: -->
