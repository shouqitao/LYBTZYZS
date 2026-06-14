# scripts

## Purpose

Automation scripts for building, testing, deploying, and operating the LYBTZYZS system. Scripts are organized by function: deployment, operations, security, validation, and daily development.

## Key Files

| File | Description |
|------|-------------|
| `run-webapi.ps1` | Start WebAPI server (published or dev) |
| `stop-webapi.ps1` | Stop WebAPI server |
| `health-check.ps1` | WebAPI health check |
| `smoke-test.ps1` | End-to-end smoke test |
| `quality-check.ps1` | Code quality checks |
| `validate-production-config.ps1` | Production configuration validation |
| `cleanup.ps1` | Clean temporary files |
| `clean-test-results.ps1` | Clean test output directories |
| `check-webapi-startup.ps1` | Diagnose WebAPI startup issues |
| `setup-hooks.ps1` | Install git hooks (.githooks) |

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| `deploy-server/` | Server deployment (service registration, launch) |
| `deploy/` | Deployment environment verification |
| `deployment/` | Production deployment scripts (deploy, backup, rollback) |
| `operations/` | Operational scripts (backup/restore, JWT rotation, security audit) |
| `Security/` | Secret management |
| `validation/` | Smoke test and test matrix scripts |
| `templates/` | Code templates (Module, Repository) |
| `ResetPassword/` | Password reset utility tool |

## For AI Agents

- All scripts execute from project root (`D:\source\repos\LYBTZYZS`).
- PowerShell scripts may require `Set-ExecutionPolicy RemoteSigned -Scope CurrentUser`.
- Scripts use UTF-8 encoding.
- Naming: lowercase-hyphens for PowerShell (`script-name.ps1`).
