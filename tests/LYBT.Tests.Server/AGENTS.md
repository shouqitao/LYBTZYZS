# LYBT.Tests.Server - Server Integration Tests

**Purpose**: Integration-first server tests with real SQL Server + Respawn, zero mock.

## Structure

```
LYBT.Tests.Server/
├── Features/            # 28 feature tests
├── UserJourneys/        # 12 user journey tests
├── _Infrastructure/     # 9 test fixtures, ServerFixture, IntegrationTestBase
├── RateLimiting/        # Rate limiting tests with dedicated fixture
├── PureLogic/           # 8 pure logic tests (no DB)
└── TestDataBuilders/    # 6 test data builders
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Test base | `_Infrastructure/IntegrationTestBase.cs` | Base class for integration tests |
| Server fixture | `_Infrastructure/ServerFixture.cs` | SQL Server setup + Respawn |
| Feature tests | `Features/` | 28 feature-specific test classes |
| User journeys | `UserJourneys/` | End-to-end user flow tests |
| Data builders | `_Infrastructure/TestDataBuilders/` | Fluent test data construction |

## CONVENTIONS

- **Testing Trophy** — Integration-first, real SQL Server, zero mock
- **Respawn** — Database reset between tests via Respawn checkpoints
- **xUnit** — Test framework with collection fixtures for parallel execution
- **NSubstitute banned** — AntiMockRuleTests enforce no mocking in server tests
- **Test data builders** — Fluent builder pattern for test data construction

## ANTI-PATTERNS

- **NSubstitute/Moq** — Architecture test `AntiMockRuleTests` forbids mocking
- **EF InMemory** — Banned for server tests (AntiMockRuleTests)
- **Shared state between tests** — Each test gets fresh database via Respawn
