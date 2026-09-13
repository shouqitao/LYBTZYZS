<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# Core (Desktop)

## Purpose
Core infrastructure libraries for the WPF desktop client. Provides interface contracts, HTTP/security infrastructure, WPF services and controls, client-side UI models, printing support, hardware integration, and shared utility types. These libraries form the foundation layer that all business modules depend on.

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| LYBT.Desktop.Contracts/ | Interface definitions + shared DTOs (CommandResult, BreadcrumbItem, AuthState, CacheEvents) |
| LYBT.Desktop.Foundation/ | HTTP clients, security/auth, configuration, ExcelHelper |
| LYBT.Desktop.Infrastructure/ | WPF services — ViewModel base classes, Dialog, Navigation (NavigationCoordinator, RegionMonitor), Behaviors, Services |
| LYBT.Desktop.Controls/ | WPF presentation — custom controls, themes, converters, helpers |
| LYBT.Desktop.Printing/ | Print service — QuestPDF-based document generation |

> **注（2026-08-08 A-18 P1-7 修正，D4/D13；A-21 C1 更新）**: 原文档列出的 `LYBT.Desktop.LocalData` 与 `LYBT.Desktop.CardReader` 独立项目从未建立。本地模式数据访问实际走 HTTP（`HttpClientApiClient` → LocalWebAPI，统一 `SwitchingApiClient` 双轨）；休眠的 `LYBT.Desktop.Infrastructure/LocalData/Context/LocalDbContext.cs` 已于 A-21 C1 废弃并移入测试项目 `LYBT.Tests.Desktop/_Infrastructure/LocalDbContext.cs`（生产零引用）。CardReader 硬件集成未实现。

## For AI Agents

### Working In This Directory
- Dependency order: `Contracts <- Foundation <- Infrastructure <- Controls` (unidirectional).
- `Contracts` defines interfaces + shared types (CommandResult, AuthState, BreadcrumbItem, CacheEvents).
- `Foundation` implements HTTP, auth, config; depends on `Contracts`.
- `Infrastructure` provides ViewModel base classes, navigation, services; depends on `Foundation` + `Controls`.
- `Controls` provides WPF presentation assets; depends on `Contracts` + `Foundation` (no Infrastructure dependency).
- Local mode data access uses the unified HTTP path (`SwitchingApiClient` → `HttpClientApiClient` → LocalWebAPI), same Service/Repository layer as remote (ADR-0010).
- When adding a new interface, place it in `Contracts`; implement it in `Foundation` or `Infrastructure`.
- WPF controls and converters belong in `Controls`; ViewModel base classes belong in `Infrastructure`.
- Shared DTOs (used across modules) belong in `Contracts`.

### Common Patterns
- **Repository interfaces**: `I{Entity}Repository<T>` in Contracts, implemented in Foundation (HTTP, via `SwitchingApiClient`)
- **Connection mode**: URL-driven dual-mode via `IConnectionSettingsService` + `SwitchingApiClient` (ADR-0009)
- **Refit interfaces**: `IApi` in Contracts defines all HTTP endpoints (remote mode)

## Dependencies

### Internal
- [Shared/](../../../Shared/AGENTS.md) — `LYBT.Shared.Models`, `LYBT.Shared.Configuration`

### External
- Refit (HTTP client generation)
- Prism.Core (MVVM, navigation)
- QuestPDF (Printing)

<!-- MANUAL: -->
