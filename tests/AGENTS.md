<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# tests

## Purpose
Test projects for the LYBTZYZS solution. Implements a Testing Trophy architecture with ~2021+ tests across multiple projects: server integration tests (real SQL Server + Respawn, zero mock), desktop unit/integration tests (SQLite InMemory), architecture guard tests, and API integration tests.

## Key Files
| File | Description |
|------|-------------|
| .runsettings | Test run configuration (timeouts, parallelism, environment) |
| Directory.Build.props | Shared build properties for all test projects |
| Directory.Build.targets | Shared build targets for all test projects |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| LYBT.Tests.Server/ | Server integration tests — ~1185 tests, real SQL Server + Respawn reset, zero mock |
| LYBT.Tests.Desktop/ | Desktop tests — ~760 tests, SQLite InMemory + real Repository pattern |
| LYBT.Tests.Architecture/ | Architecture guard tests — ~76 tests enforcing dependency rules, naming conventions, anti-mock policies |
| LYBT.Tests.Server.Unit/ | Server unit tests (subset) |
| LYBT.Tests.Integration/ | Integration test infrastructure |
| LYBT.Tests.Integration.Server/ | Server API integration tests |
| LYBTZYZS/ | End-to-end or solution-level tests |
| postman/ | Postman/Newman API test collections |

## For AI Agents

### Working In This Directory
- Run all tests: `dotnet test LYBTZYZS.sln --filter "FullyQualifiedName~LYBT.Tests"`
- Run individual projects: `dotnet test tests/LYBT.Tests.Server/`
- Run single test: `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~ClassName.MethodName"`
- Run module-specific: `dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~MedicalCase"`

### Testing Requirements
- **Server tests**: Use real SQL Server database with Respawn for clean-state between tests. ZERO mocks.
- **Desktop tests**: Use SQLite InMemory provider with real Repository implementations.
- **Architecture tests**: Verify dependency direction rules, naming conventions, and anti-mock policies (e.g., `P10_Services_Should_Not_Directly_Inject_AppDbContext`).
- **Postman/Newman**: Run via `scripts/run-tests-local.ps1` for API contract testing.

### Common Patterns
- **Test fixture**: xUnit class fixtures for database setup/teardown
- **Respawn**: Database reset between server integration tests
- **Builder pattern**: Test data builders for entity construction
- **Architecture guards**: Compile-time and runtime checks for architectural invariants

## Dependencies

### Internal
- [src/Server/](../src/Server/AGENTS.md) — All server projects under test
- [src/Client/Desktop/](../src/Client/Desktop/AGENTS.md) — All desktop projects under test
- [src/Shared/](../src/Shared/AGENTS.md) — Shared libraries under test

### External
- xUnit (test framework)
- Respawn (database reset)
- FluentAssertions
- Moq (limited, architecture tests enforce zero-mock in server tests)
- Newman (Postman CLI runner)

<!-- MANUAL: -->
