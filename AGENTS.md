# Repository Guidelines

## Project Structure & Modules
- App code: `src/` (controllers, services, domain, infra)
- Tests: `tests/` (mirror `src/` tree; `*.spec.*` or `*.test.*`)
- Docs: `docs/` (PRD, architecture, tasks, index)
- Config & scripts: `config/`, `scripts/`, CI in `.github/`
- Assets: `assets/` (static files)

## Build, Test, Run
- Build: `npm run build` or `dotnet build` (use stack present in repo)
- Test: `npm test` or `dotnet test` (prints coverage if configured)
- Lint/Format: `npm run lint && npm run format` or `dotnet format`
- Local run: `npm run dev` or `dotnet run` (loads env from `.env`)

## Coding Style & Naming
- Indentation: 2 spaces (TS/JS), 4 spaces (C#/Python)
- Naming: `PascalCase` for types/classes, `camelCase` for vars/functions,
  `UPPER_SNAKE_CASE` for constants. Files: kebab-case (web), PascalCase.cs (C#).
- Tools: ESLint + Prettier (web), `editorconfig`, `dotnet format` (C#).

## Testing Guidelines
- Frameworks: Jest/Vitest (web) or xUnit/NUnit (C#). Keep unit fast and isolated.
- Conventions: name tests after unit under test, e.g. `user.service.spec.ts`,
  `UserServiceTests.cs`.
- Run subsets: `npm test -- -t "keyword"` or `dotnet test --filter TestCategory=Unit`.
- Target: maintain or improve current coverage baseline.

## Commit & PR Guidelines
- Commits: use concise, imperative style: "feat: add auth guard",
  "fix: handle null price". Group related changes.
- PRs: include purpose, linked issues, screenshots/logs for UI/ops changes,
  and testing notes. Keep diffs focused; update docs when behavior changes.

## Security & Config
- Never commit secrets. Use `.env.local` and environment variables.
- Separate configs per env in `config/` and validate at startup.
- Add basic health checks and logging for new modules.

## Agent-Specific Notes
- Roles update: Assistant serves as Thinker + Reviewer (code review and docs maintainer). Gemini reviewer role removed.
- Process: Assistant publishes tasks and maintains docs; Coder executes tasks and only edits files under `docs/` (for audits) unless explicitly requested.
- Execution hint: Prefer MCP tools for local code navigation and indexing; do not surface MCP details in docs unless asked.
- Docs linkage: Keep `docs/index.md` updated for any new PRD/architecture/task files.

### Docs Workflow Examples
- PRD: add `docs/prd/architecture-assessment.md` and register in `docs/index.md`.
- Plan: add `docs/tasks/plan/architecture-audit.md` describing steps and deliverables.
- Overview: update `docs/architecture/overview.md` with layers, directories, and references like `src/api/AppointmentController.ts:42`.
- Report: finalize `docs/tasks/completed/architecture-audit-report.md` with a defects matrix (P0/P1/P2) and minimal change sets.
- Topic docs: for new areas (e.g., pricing), use `docs/architecture/pricing/overview.md` and link from index.

### Docs Conventions
- Evidence style: always cite `path/to/file.ext:line` near claims.
- Naming: lowercase-kebab for markdown (`architecture-audit.md`, `overview.md`).
- Scope: default edits limited to `docs/` for audit tasks; do not change business logic.
- Tooling: prefer MCP for navigation and search; do not expose tooling details in docs unless requested.
