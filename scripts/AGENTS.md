<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# scripts

## Purpose

Automation scripts directory containing ~130+ scripts for building, testing, deploying, maintaining, and operating the LYBTZYZS system. Scripts span PowerShell (.ps1), Batch (.bat), Python (.py), Shell (.sh), and SQL (.sql) formats. Covers the full development lifecycle: build verification, test execution, database management, deployment, health checks, code quality, and documentation maintenance.

## Key Files

| File | Description |
|------|-------------|
| `build.bat` | Interactive build manager for the full solution |
| `build-check.bat` | Build verification and pre-build checks |
| `quick-compile.bat` | Quick compilation check |
| `run-webapi.ps1` | Start WebAPI server |
| `stop-webapi.ps1` | Stop WebAPI server |
| `run-tests-local.ps1` | Run all tests locally |
| `run-postman-tests.ps1` | CI-integrated Postman/Newman test runner |
| `cleanup.ps1` | Clean temporary files |
| `health-check.ps1` | WebAPI health check |
| `smoke-test.ps1` | Comprehensive smoke test suite |
| `quality-check.ps1` | Code quality checks |
| `validate-production-config.ps1` | Production configuration validation |
| `README.md` | Script inventory and usage guide |

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| `analysis/` | Code analysis scripts |
| `archive/` | Archived/deprecated scripts |
| `auth/` | Authentication testing scripts |
| `BcryptGenerator/` | BCrypt hash generation utility |
| `config/` | Configuration management scripts |
| `database/` | Database management scripts |
| `deploy/` | Deployment scripts |
| `deploy-server/` | Server deployment scripts |
| `deployment/` | Additional deployment scripts |
| `DiagnoseAdminHash/` | Admin password hash diagnostics |
| `documentation/` | Documentation generation scripts |
| `documentation-analysis/` | Documentation analysis scripts |
| `documentation-maintenance/` | Documentation maintenance scripts |
| `health/` | Health check scripts |
| `maintenance/` | Maintenance scripts |
| `migrations/` | Database migration scripts |
| `operations/` | Operational scripts |
| `ResetPassword/` | Password reset utilities |
| `run/` | Run/start scripts |
| `Security/` | Security-related scripts |
| `smoke/` | Smoke test scripts |
| `sql/` | SQL scripts (schema fixes, seed data) |
| `templates/` | Script templates |
| `test/` | Test execution scripts |
| `testing/` | Testing utilities |
| `tests/` | Additional test scripts |
| `tools/` | Developer tools |
| `uat/` | UAT testing scripts |
| `validation/` | Validation scripts |

## For AI Agents

### Working In This Directory

- All scripts should be executed from the project root directory (`D:\source\repos\LYBTZYZS`), not from the scripts directory.
- PowerShell scripts may require `Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser` on first use.
- Scripts use UTF-8 encoding. Avoid modifying encoding when editing.
- Naming convention: lowercase with hyphens for PowerShell (`script-name.ps1`), underscores for SQL (`script_name.sql`).
- Key daily-use scripts: `run-webapi.ps1`, `stop-webapi.ps1`, `run-tests-local.ps1`, `cleanup.ps1`, `build.bat`.

### Testing Requirements

- Scripts themselves are not unit tested but are validated via `quality-check.ps1` and `health-check.ps1`.
- Use `run-postman-tests.ps1` for API contract testing via Newman/Postman collections.
- Use `smoke-test.ps1` for end-to-end smoke testing after deployment.

### Common Patterns

- **PowerShell + Batch dual** -- Critical scripts exist in both `.ps1` (PowerShell) and `.bat` (CMD) formats.
- **SQL scripts** -- Schema fixes, seed data, and migration scripts use `.sql` extension.
- **Error handling** -- Production scripts include try/catch and exit codes for CI integration.
- **Parameter-based** -- Most scripts accept parameters for flexibility (e.g., `-WhatIf`, `-Force`).

## Dependencies

### Internal

- Requires the full LYBTZYZS solution to be present for build/test/deploy scripts.
- References `src/` project paths for build and publish operations.
- References `tests/` for test execution scripts.

### External

- .NET SDK (dotnet CLI)
- PowerShell 7+ (pwsh)
- SQL Server / SQL Server Management Studio (for SQL scripts)
- Newman (for Postman test runner)
- Python 3.x (for Python scripts)

<!-- MANUAL: -->
