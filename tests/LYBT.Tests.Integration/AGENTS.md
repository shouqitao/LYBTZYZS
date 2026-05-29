<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# LYBT.Tests.Integration

## Purpose

Desktop-to-Server full-chain integration tests. Tests the complete flow from Desktop Refit API client through to the WebAPI server with real SQL Server, validating that the dual-mode architecture works end-to-end. Covers authentication, patient CRUD, herb management, formula operations, medical case workflows, and boundary conditions.

## Key Files

| File | Description |
|------|-------------|
| `LYBT.Tests.Integration.csproj` | Project file; net8.0-windows, WPF; xUnit, FluentAssertions, WebApplicationFactory, Respawn, Refit; references both Server and Desktop projects |
| `GlobalUsings.cs` | Global usings for the test project |
| `xunit.runner.json` | xUnit runner configuration |

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| `_Infrastructure/` | Test infrastructure: `IntegrationFixture.cs` (14KB, WebApplicationFactory + SQL Server + Respawn + Refit client factory), `IntegrationTestBase.cs` (6KB, base class), `IntegrationTestCollection.cs` (collection fixture), `RemoteOnlyApiRouter.cs` (routes tests to remote API) |
| `Flows/` | End-to-end flow tests: AuthFlowTests, FormulaFlowTests, HerbFlowTests, MedicalCaseFlowTests, MedicalCaseBoundaryTests, PatientFlowTests, PatientBoundaryTests |

## For AI Agents

### Working In This Directory

- Tests the Desktop RemoteDataSource -> Server API -> SQL Server full chain using WebApplicationFactory.
- `IntegrationFixture` creates a unique SQL Server database per test run, seeds base data (sysadmin, admin, doctor), and provides authenticated Refit API clients.
- Requires `net8.0-windows` target framework because it references WPF Desktop projects.
- Uses real SQL Server + Respawn for database isolation (same pattern as LYBT.Tests.Server).
- Tests authenticate via the real login endpoint (`POST /api/v1/auth/login`), not mocked auth.

### Testing Requirements

- Run all integration tests: `dotnet test tests/LYBT.Tests.Integration/`
- Requires SQL Server running (LocalDB or full instance).
- Tests are slower than unit tests due to real database operations.
- Run specific flow: `dotnet test tests/LYBT.Tests.Integration/ --filter "FullyQualifiedName~PatientFlow"`
- Run boundary tests: `dotnet test tests/LYBT.Tests.Integration/ --filter "FullyQualifiedName~Boundary"`

### Common Patterns

- **WebApplicationFactory** -- Uses `WebApplicationFactory<Program>` for in-memory test server.
- **Respawn** -- Database reset between test runs via Respawn checkpoints.
- **Refit client** -- Creates real Refit HTTP clients pointing to the test server, testing the same code paths as the Desktop app.
- **Fixture-based** -- Uses xUnit collection fixtures (`IntegrationTestCollection`) for shared test infrastructure.
- **Fixed test users** -- Predictable user IDs (AdminUserId, DoctorUserId) for consistent test data.
- **Semaphore gate** -- `IntegrationFixture` uses `SemaphoreSlim` for thread-safe initialization.

## Dependencies

### Internal

- `LYBT.WebAPI` -- Server WebApplicationFactory target
- `LYBT.Infrastructure` -- Server DbContext
- `LYBT.Entities` -- Server entities
- `LYBT.Desktop.Contracts` -- Refit API interfaces
- `LYBT.Desktop.Infrastructure` -- Desktop HTTP infrastructure
- `LYBT.Desktop.Patients`, `LYBT.Desktop.Herbs`, `LYBT.Desktop.Formula`, `LYBT.Desktop.MedicalCase` -- Desktop module tests
- `LYBT.Shared.Models` -- DTOs
- `LYBT.Shared.Utilities` -- Utility classes

### External

- `Microsoft.NET.Test.Sdk` -- Test SDK
- `xunit` -- Test framework
- `xunit.runner.visualstudio` -- VS test runner
- `FluentAssertions` -- Assertion library
- `Microsoft.AspNetCore.Mvc.Testing` -- WebApplicationFactory
- `Microsoft.EntityFrameworkCore.SqlServer` -- SQL Server provider
- `Respawn` -- Database reset between tests
- `Refit` -- Type-safe HTTP client for Desktop API interfaces
- `System.IdentityModel.Tokens.Jwt` -- JWT token handling for auth tests

<!-- MANUAL: -->
