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
  "fix: resolve memory leak", "docs: update API guide"
- PRs: squash if > 5 commits, ensure CI green before merge.
- Branch naming: `feat/short-desc`, `fix/issue-123`, `chore/update-deps`

## Security & CI/CD
- No secrets in code; use `.env` (ignored by git).
- CI: auto-run build/test/lint on PR; deploy on merge to main.
- Security: use Dependabot/Snyk, run `npm audit` or `dotnet list package --vulnerable`.

## Performance & Monitoring
- Optimize only after profiling; use caching & pagination for large data.
- Monitoring: log errors/perf via Sentry, Datadog, AppInsights, etc.
- Alerting: define SLAs (e.g., p99 latency < 200ms).

## Notes
- Module boundaries: avoid circular deps, define clear interfaces.
- Task management: see `docs/tasks/` for current sprint items.
- Docs: update architecture diagrams and API specs as code evolves.