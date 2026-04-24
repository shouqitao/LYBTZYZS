# LYBT.Tests.Desktop - Desktop Tests

**Purpose**: Desktop client tests with SQLite InMemory + real Repository.

## Structure

```
LYBT.Tests.Desktop/
├── EndToEnd/
│   ├── Modules/         # 13 end-to-end module tests
│   ├── Foundation/      # 7 foundation tests
│   └── Infrastructure/  # 8 infrastructure tests
├── PureLogic/
│   ├── MedicalCase/     # 9 medical case logic tests
│   └── Clinical/        # 9 clinical logic tests
├── UserJourneys/        # Desktop user journey tests
├── _Infrastructure/     # 5 test fixtures
└── Features/            # Feature-specific tests
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| End-to-end | `EndToEnd/Modules/` | 13 module E2E tests |
| Pure logic | `PureLogic/MedicalCase/` | MedicalCase domain logic tests |
| Test fixtures | `_Infrastructure/` | SQLite InMemory setup |
| User journeys | `UserJourneys/` | Desktop user flow tests |

## CONVENTIONS

- **SQLite InMemory** — Tests use SQLite InMemory database, not real SQL Server
- **Real Repository** — Repository layer is real, not mocked
- **net8.0-windows** — Target framework required for WPF tests
- **NSubstitute allowed** — Unlike server tests, desktop tests may use NSubstitute for UI layer

## ANTI-PATTERNS

- **net8.0 target** — Desktop tests MUST target `net8.0-windows` for WPF
- **Mocking Repository** — Repository layer should be real; mock only UI dependencies
