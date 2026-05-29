<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# Core (Desktop)

## Purpose
Core infrastructure libraries for the WPF desktop client. Provides interface contracts, HTTP/security infrastructure, WPF services, client-side UI models, SQLite local-mode data access, printing support, hardware integration, and utility classes. These libraries form the foundation layer that all business modules depend on.

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| LYBT.Desktop.Contracts/ | Interface definitions — Refit `IApi`, `IRepository<T>`, `IService`, navigation, dialog contracts |
| LYBT.Desktop.Foundation/ | Infrastructure layer — HTTP clients, security/auth, configuration, `ConnectionMode` management |
| LYBT.Desktop.Infrastructure/ | WPF services — Dialog service, Navigation, custom controls, value converters, region behaviors |
| LYBT.Desktop.Models/ | Client-side UI models — view models helpers, display models, state containers |
| LYBT.Desktop.LocalData/ | SQLite local-mode — `LocalDbContext`, local repositories for offline/embedded operation |
| LYBT.Desktop.Printing/ | Print service — QuestPDF-based document generation (prescriptions, reports) |
| LYBT.Desktop.CardReader/ | Hardware integration — ID card reader device support |
| LYBT.Desktop.Utilities/ | Utility classes — shared helpers and extensions |

## For AI Agents

### Working In This Directory
- Dependency order within Core: `Contracts <- Foundation <- Infrastructure` (unidirectional).
- `Contracts` defines interfaces only; no implementations.
- `Foundation` implements HTTP, auth, and config; depends on `Contracts`.
- `Infrastructure` provides WPF-specific services (dialogs, navigation, converters); depends on `Foundation`.
- `LocalData` provides the SQLite alternative to the remote HTTP API path.
- When adding a new interface, place it in `Contracts`; implement it in `Foundation` or `Infrastructure`.

### Common Patterns
- **Repository interfaces**: `I{Entity}Repository<T>` in Contracts, implemented in Foundation (HTTP) and LocalData (SQLite)
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
