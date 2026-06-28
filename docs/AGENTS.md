<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-06-28 -->

# docs

## Purpose

Central documentation hub for the LYBTZYZS TCM clinic management system. Hosts the stable documentation set (6 sections, ~110 files), the `compose/` workflow output (specs/plans/reports, 90+ files), legacy `plans/`, and training materials. Documentation is the **design authority** — target-state descriptions are preserved even when code lags; mismatches are tagged `⚠️ 代码待对齐`.

## Key Files

| File | Description |
|------|-------------|
| `README.md` | Documentation index with quick navigation for new developers, API developers, and architects |
| `CONTRIBUTING.md` | Contribution guidelines (commit/branch/PR conventions) |
| `AGENTS.md` | This file — AI agent guide for the docs tree |

## Subdirectories

| Directory | Purpose | Files |
|-----------|---------|-------|
| `01-product/` | Product vision, feature overview, user roles | 4 |
| `02-requirements/` | PRD: 9 v1.0 modules, 138 User Stories, NFR, traceability matrix (Sync is v2.0) | 13 |
| `03-architecture/` | System architecture, data model, dual-mode design, 13 ADRs (ADR-0001~0013, 0013=SignalR), permissions matrix, business flows, `decisions/`, `archive/`, `localwebapi/` | 31 |
| `03-architecture/decisions/` | ADR-0001 through ADR-0013 |
| `03-architecture/localwebapi/` | LocalWebAPI-specific architecture docs |
| `04-api-reference/` | All API endpoint documentation (printing, health, diagnostics, configuration; Sync is v2.0) | 14 |
| `05-development/` | Quick start, coding standards, design patterns, testing guides, `standards/` (STD files), `archive/` | 26 |
| `06-operations/` | Deployment, configuration, monitoring, logging, `archive/` | 13 |
| `compose/` | Workflow output — `specs/` (design), `plans/` (implementation), `reports/` (audits), `archive/`. Naming: `YYYY-MM-DD-<slug>-{design|impl}.md` | 90+ |
| `plans/` | ⚠️ **Deprecated** (2026-06-28) — redirects to `compose/`. Legacy files only. | — |
| `training/` | Training materials | 1 |

## For AI Agents

### Working In This Directory

- Documentation uses Chinese for prose, English for technical identifiers.
- **Numbering conventions**:
  - User Stories: `US-{DOMAIN}-{NNN}` (e.g., `US-MC-017`, `US-SHELL-010`)
  - ADR: `ADR-NNNN` (four digits, e.g., `ADR-0001`)
- Each document includes a change log table at the bottom.
- **Workflow documents live in `docs/compose/`** (not `docs/plans/`). New specs/plans/reports follow the compose naming convention; completed items are archived to `compose/{specs,plans}/archive/`.
- When adding new architecture decisions, create them in `docs/03-architecture/decisions/` following the ADR template.

### Authoritative Facts (code-verified 2026-06-28)

| Fact | Value |
|------|-------|
| v1.0 US total | **138** (AUTH13+USER12+PAT13+HERB13+FORM13+MC19+REG8+PRINT4+Platform43) |
| v1.0 modules | **9** (Sync is v2.0) |
| ADR count | **13** (ADR-0001~0013) |
| LocalWebAPI embedded port | **5300** (`EmbeddedLocalWebApiService.cs:17`) |
| LocalWebAPI standalone debug port | 5290 (not active in embedded mode) |
| Remote WebAPI port | 5000 |
| Desktop test DB | **SQL Server LocalDB** (NOT SQLite) |
| ApiResponse fields | `success/message/data/errors/timestamp/requestId` (no `code`) |

### Documentation Authority Principle

Docs preserve **target state**. Where code differs:
- **v1.0 gap** (not yet implemented): tag `🚧 v1.0 待实现`
- **Code mismatch** (e.g., permission policy): keep target in docs, tag `⚠️ 代码当前为 X，待对齐`
- **v2.0 items** (Sync, SHELL-012 auto-update, replay detection, Patients import): tag `v2.0 规划`

### Testing Requirements

- No automated tests for documentation. Validate links manually.

### Common Patterns

- **Date-prefixed compose files** — `YYYY-MM-DD-<slug>-{design|impl}.md` in `compose/{specs,plans}/`.
- **Section numbering** — Major docs use `01-` through `06-` prefix for ordering.
- **ADR format** — Architecture Decision Records follow standard ADR template (Context, Decision, Consequences).

## Dependencies

### Internal

- References code structure across the entire solution for documentation purposes.

### External

- *(none)* -- Pure documentation, no package dependencies

<!-- MANUAL: -->
