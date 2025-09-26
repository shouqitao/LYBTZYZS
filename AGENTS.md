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
- Role: Assistant acts as Task Publisher for this project. Responsible for drafting/issuing task MD documents, maintaining docs linkage, and performing high-level reviews. Gemini reviewer role is removed.
- Process: Assistant publishes tasks and maintains docs; Coder executes tasks and implements code/doc changes per task. Default scope for audit/refactor tasks limits changes to `docs/` unless explicitly authorized.
- Execution hint: Prefer MCP tools for navigation and search; do not expose tooling details in the docs unless requested.
- Docs linkage: Keep `docs/index.md` updated for all PRD/architecture/task files to ensure navigability.

### Task Publishing Policy
- All tasks are issued as Markdown under `docs/tasks/plan/` (planning) or `docs/tasks/completed/` (reports).
- Each task must state: goal, scope/boundaries, deliverables (files/paths), steps, acceptance criteria, and constraints.
- Evidence rule: when conclusions reference code, cite `path/to/file:line` for traceability.
- Changes to business logic require explicit authorization within the task.

### Claude Code-Oriented Task Template (Repository-Aware)
Use this template to issue executable tasks that Claude Code can follow precisely. Replace placeholders with actual paths from this repo.

Title
- <Concise task name>（e.g., “Implement JWT Login API” / “Refactor Appointment Service boundaries”）

Context (Repository facts)
- Solution/Projects: <.sln / csproj paths>
- Entrypoints: <src/Api/Program.cs:line>, DI registration: <path:line>
- Data: <DbContext path:line>, Entities: <paths>

Goal
- What to build/change in one paragraph; success criteria in bullets.

Scope & Boundaries
- Allowed files/dirs to change (explicit list with wildcards if needed)
- Do not touch: <layers/dirs/files>
- Tech constraints: keep current frameworks; no new external deps unless justified and approved.

Deliverables
- Code: list target files to add/modify (exact repo paths)
- Docs: update `README.md` section <name> and `docs/architecture/overview.md` if architecture changes
- Tests: xUnit tests under `tests/` with naming pattern `<TypeName>Tests.cs`

Acceptance Criteria
- API contract (method, route, request/response schema, status codes)
- Security/logging rules (e.g., BCrypt verify, JWT HS256, no sensitive logs)
- Performance/DB: no N+1; read paths AsNoTracking
- Build/Test commands succeed: `dotnet build`, `dotnet test`

Implementation Steps
1) Read code at <paths:lines> and summarize current behavior (1–2 bullets)
2) Propose minimal change set (files + signatures) for approval
3) Implement code within allowed scope; wire DI in <Program.cs:line>
4) Add tests (success, invalid input, error path)
5) Update docs (README + docs/index.md links if needed)
6) Provide PR summary: changes, risks, verification, rollback

Evidence & References
- Cite all findings with `path:line`
- Commands to validate locally (copy-paste ready)

Notes for Claude Code
- Prefer small diffs and minimal change sets first; request confirmation before broader refactors.
- Use repository paths exactly as listed; do not invent files or structures.

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
