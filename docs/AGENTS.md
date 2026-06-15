<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# docs

## Purpose

Central documentation hub for the LYBTZYZS TCM clinic management system. Contains ~100+ documentation files organized into 7 main sections covering product vision, requirements (PRD), architecture (including 8 ADRs), API reference, development guides, operations, and domain concepts. Also hosts active design/plan documents and training materials.

## Key Files

| File | Description |
|------|-------------|
| `README.md` | Documentation index with quick navigation for new developers, API developers, and architects |

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| `01-product/` | Product vision, feature overview, user roles (4 files) |
| `02-requirements/` | PRD with 10 modules, 136 User Stories, NFR (12 files) |
| `03-architecture/` | System architecture, data model, dual-mode design, 8 ADRs, error handling, archive/, localwebapi/, decisions/ (25 files) |
| `03-architecture/decisions/` | Architecture Decision Records (ADR-0001 through ADR-0008) |
| `03-architecture/localwebapi/` | LocalWebAPI-specific architecture docs |
| `04-api-reference/` | All API endpoint documentation — printing, sync, health, diagnostics, configuration (13 files) |
| `05-development/` | Quick start, coding standards, design patterns, testing guides, standards/ (6 STD files), archive/ (12 files) |
| `06-operations/` | Deployment, configuration, monitoring, logging, archive/ (7 files) |
| `plans/` | Active design/plan documents |
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

### Common Patterns

- **Date-prefixed plans** -- Plan files use `YYYY-MM-DD-description.md` naming.
- **Section numbering** -- Major docs use `01-` through `06-` prefix for ordering.
- **ADR format** -- Architecture Decision Records follow standard ADR template with Context, Decision, Consequences sections.

## Dependencies

### Internal

- References code structure across the entire solution for documentation purposes.

### External

- *(none)* -- Pure documentation, no package dependencies

<!-- MANUAL: -->
