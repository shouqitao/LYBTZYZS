<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# Desktop

## Purpose
WPF/Prism.DryIoc desktop application for the LYBTZYZS TCM clinic management system. Implements a modular MVVM architecture with Prism regions for navigation, supporting dual-mode operation (remote SQL Server via HTTP API or local embedded SQL Server LocalDB). Contains core infrastructure libraries, business modules (Auth, Patients, Herbs, Formula, MedicalCase, etc.), role-based workspaces (Admin, Clinical, Receptionist), and the shell entry point.

## Key Files
| File | Description |
|------|-------------|
| GlobalUsings.cs | Global using directives for the desktop project tree |
| DESKTOP_ARCHITECTURE_STANDARD.md | Architecture standards and patterns documentation |
| README.md | Desktop project overview |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| [Core/](Core/AGENTS.md) | Core libraries — Contracts, Foundation, Infrastructure, Models, LocalData, Printing, CardReader, Utilities |
| [Modules/](Modules/AGENTS.md) | Business modules — Auth, Patients, Herbs, Formula, MedicalCase, Registration, Sync, Users |
| [Roles/](Roles/AGENTS.md) | Role-based workspaces — Admin, Clinical, Receptionist |
| Resources/ | XAML resources — Dictionaries and Strings |
| Shell/ | PrismApplication entry point (`LYBT.Desktop.Shell`) |
| LocalWebAPI/ | Embedded ASP.NET Core host for local mode |

## For AI Agents

### Working In This Directory
- Dependency direction: `Shell -> Roles -> Modules -> Core(Infrastructure -> Foundation -> Contracts)`
- Business modules MUST NOT reference each other; cross-module communication via shared services or event aggregation.
- All ViewModels inherit from `UnifiedViewModelBase` or `UnifiedListViewModelBase<T>`.
- Data access uses `I{Entity}Repository` for CRUD and `I{Entity}DataManager` for aggregates.
- Object mapping: Riok.Mapperly (compile-time) + AutoMapper (runtime fallback).
- Module registration via Prism `IModule` interface in `{Domain}Module.cs`.

### Testing Requirements
- `dotnet test tests/LYBT.Tests.Desktop/` — ~760 tests, SQLite InMemory + real Repository
- Tests target `net8.0-windows`; cannot mix with Server test projects.

### Common Patterns
- **ViewModel base classes**: `UnifiedViewModelBase`, `UnifiedListViewModelBase<T>`
- **Repository pattern**: `I{Entity}Repository` for CRUD, `I{Entity}DataManager` for aggregate roots
- **Navigation**: Prism Region-based navigation between modules
- **Dual-mode**: Remote (HTTP API) vs Local (SQL Server LocalDB), sharing Service/Repository layer

## Dependencies

### Internal
- [Shared/](../../../Shared/AGENTS.md) — `LYBT.Shared.Models`, `LYBT.Shared.Validators`, `LYBT.Shared.Components`, `LYBT.Shared.Configuration`

### External
- Prism.DryIoc
- AutoMapper
- Riok.Mapperly
- QuestPDF (printing)
- Refit (HTTP client)

<!-- MANUAL: -->
