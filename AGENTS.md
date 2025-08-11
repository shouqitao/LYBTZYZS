# Repository Guidelines

## Project Structure & Module Organization
- Source code: `src/` (alternatively `app/` or `lib/` when present).
- Tests: `tests/` or `__tests__/`; integration samples under `examples/`.
- Assets and configs: `assets/`, `public/`, `config/`, and root dotfiles.
- Scripts: `scripts/` for repeatable tasks; prefer small, composable scripts.

Tip: Check root files to infer stack: `package.json` (Node), `pyproject.toml` (Python), `.sln/.csproj` (Dotnet), `Makefile` (generic tasks).

## Build, Test, and Development Commands
- Install deps: `npm ci` | `pip install -e .` | `dotnet restore` | `make deps`.
- Build: `npm run build` | `python -m build` | `dotnet build -c Release` | `make build`.
- Test: `npm test` | `pytest -q` | `dotnet test` | `make test`.
- Run locally: `npm start` | `python -m <package>` | `dotnet run --project <Project>`.

Choose commands based on the toolchain files present in the repo root.

## Coding Style & Naming Conventions
- Formatting: use configured tools if present: `prettier`, `eslint`, `black`, `ruff`, `dotnet format`.
- Indentation: spaces (2 for JS/TS, 4 for Python/.NET); wrap at ~100 cols.
- Naming: files and modules `kebab-case` (web) or `snake_case` (Python); C# types `PascalCase`, methods `PascalCase`, locals `camelCase`.
- Run linters/formatters before committing: e.g., `npm run lint && npm run format`, `ruff check --fix`, or `dotnet format`.

## Testing Guidelines
- Place unit tests near code or under `tests/` mirroring package paths.
- Names: Python `test_*.py`; JS/TS `*.test.ts`; .NET `*Tests.cs`.
- Aim for meaningful coverage on core modules; include regression tests for bugs.
- Run the full suite and ensure it’s green before PRs.

## Commit & Pull Request Guidelines
- Commits: small, focused; prefer Conventional Commits (e.g., `feat: add auth hook`).
- Messages: imperative mood; include context and rationale.
- PRs: clear description, linked issues (`Fixes #123`), steps to test, and screenshots/logs when UI or DX changes.
- Keep CI passing; request review once ready, not WIP.

## Security & Configuration
- Never commit secrets; use `.env.local` or user secrets. Add example config in `.env.example`.
- Validate inputs and handle errors; avoid broad exception catching.
- Pin critical dependency versions and document migrations in `CHANGELOG.md`.

