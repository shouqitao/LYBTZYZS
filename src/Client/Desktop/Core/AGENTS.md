<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# Core (Desktop)

## Purpose
Core infrastructure libraries for the WPF desktop client. Provides interface contracts, HTTP/security infrastructure, WPF services and controls, client-side UI models, SQL Server LocalDB local-mode data access, printing support, hardware integration, and shared utility types. These libraries form the foundation layer that all business modules depend on.

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| LYBT.Desktop.Contracts/ | Interface definitions + shared DTOs (CommandResult, BreadcrumbItem, AuthState, CacheEvents) |
| LYBT.Desktop.Foundation/ | HTTP clients, security/auth, configuration, ExcelHelper |
| LYBT.Desktop.Infrastructure/ | WPF services — ViewModel base classes, Dialog, Navigation (NavigationCoordinator, RegionMonitor), Behaviors, Services |
| LYBT.Desktop.Controls/ | WPF presentation — custom controls, themes, converters, helpers |
| LYBT.Desktop.LocalData/ | SQL Server LocalDB local-mode — `LocalDbContext`, local repositories |
| LYBT.Desktop.Printing/ | Print service — QuestPDF-based document generation |
| LYBT.Desktop.CardReader/ | Hardware integration — ID card reader device support |

## For AI Agents

### Working In This Directory
- Dependency order: `Contracts <- Foundation <- Infrastructure <- Controls` (unidirectional).
- `Contracts` defines interfaces + shared types (CommandResult, AuthState, BreadcrumbItem, CacheEvents).
- `Foundation` implements HTTP, auth, config; depends on `Contracts`.
- `Infrastructure` provides ViewModel base classes, navigation, services; depends on `Foundation` + `Controls`.
- `Controls` provides WPF presentation assets; depends on `Contracts` + `Foundation` (no Infrastructure dependency).
- `LocalData` provides the SQL Server LocalDB alternative to the remote HTTP API path.
- When adding a new interface, place it in `Contracts`; implement it in `Foundation` or `Infrastructure`.
- WPF controls and converters belong in `Controls`; ViewModel base classes belong in `Infrastructure`.
- Shared DTOs (used across modules) belong in `Contracts`.

### Common Patterns
- **Repository interfaces**: `I{Entity}Repository<T>` in Contracts, implemented in Foundation (HTTP) and LocalData (SQL Server LocalDB)
- **Connection mode**: `IConnectionModeService` determines remote vs local at runtime
- **Refit interfaces**: `IApi` in Contracts defines all HTTP endpoints

## Dependencies

### Internal
- [Shared/](../../../../Shared/AGENTS.md) — `LYBT.Shared.Models`, `LYBT.Shared.Configuration`, `LYBT.Shared.Primitives`

### External
- Refit (HTTP client generation)
- Prism.Core (MVVM, navigation)
- Microsoft.EntityFrameworkCore.Sqlite (LocalData)
- QuestPDF (Printing)

<!-- MANUAL: -->
