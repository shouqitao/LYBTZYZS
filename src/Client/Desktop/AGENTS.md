<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# Desktop

## Purpose
WPF/Prism.DryIoc desktop application for the LYBTZYZS TCM clinic management system. Implements a modular MVVM architecture with Prism regions for navigation, supporting dual-mode operation (remote SQL Server via HTTP API or local embedded SQL Server LocalDB). Contains core infrastructure libraries, business modules (Auth, Patients, Catalog, MedicalCase, Registrations, Users), role-based workspaces (Admin, Clinical — 含前台), and the shell entry point.

## Key Files
| File | Description |
|------|-------------|
| GlobalUsings.cs | Global using directives for the desktop project tree |
| DESKTOP_ARCHITECTURE_STANDARD.md | Architecture standards and patterns documentation |
| README.md | Desktop project overview |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| [Core/](Core/AGENTS.md) | Core libraries — Contracts, Foundation, Infrastructure, Controls, Printing |
| [Modules/](Modules/AGENTS.md) | Business modules — Auth, Patients, Catalog, MedicalCase, Registrations, Users |
| [Roles/](Roles/AGENTS.md) | Role-based workspaces — Admin (含 Sysadmin), Clinical (含 Receptionist) |
| Resources/ | XAML resources — Dictionaries and Strings |
| Shell/ | PrismApplication entry point (`LYBT.Desktop.Shell`) |
| LocalWebAPI/ | Embedded ASP.NET Core host for local mode |

## For AI Agents

### Working In This Directory
- Dependency direction: `Shell -> Roles -> Modules -> Core(Infrastructure -> Foundation -> Contracts)`
- Business modules MUST NOT reference each other at compile time; cross-module communication via `LYBT.Desktop.Contracts` interfaces or Prism `IEventAggregator`. `[ModuleDependency]` only declares runtime load order.
- All ViewModels inherit from `NavigableViewModelBase` or `MasterDetailViewModelBase<TListDto, TDetailModel>`; edit sub-VMs use `EditorViewModelBase<TContext>`.
- Data access: `I{Entity}Repository`（`LYBT.Desktop.Contracts.Repositories`）, implemented via `EntityApiClientRepositoryBase<...>` routed through `IApiClient` sub-interfaces.
- Object mapping: Riok.Mapperly (compile-time only; AutoMapper is forbidden per root AGENTS.md).
- Module registration via Prism `IModule` interface in `{Domain}Module.cs`; the module catalog lives in `Shell/App.xaml.cs` → `ConfigureModuleCatalog`.
- Sidebar navigation comes from `NavigationManager.BuildNavigationItems`（按角色生成，每条角色 3 项，`Group` ∈ 临床/目录/管理），个人资料入口在顶栏 Header。

### Testing Requirements
- `dotnet test tests/LYBT.Tests.Desktop/` — ~760 tests, SQL Server LocalDB + real Repository
- Tests target `net8.0-windows`; cannot mix with Server test projects.

### Common Patterns
- **ViewModel base classes**: `NavigableViewModelBase`, `MasterDetailViewModelBase<TListDto, TDetailModel>`, `EditorViewModelBase<TContext>`
- **Repository pattern**: `I{Entity}Repository` for CRUD, `I{Entity}Service` for orchestration
- **Navigation**: Prism Region-based navigation via `INavigationCoordinator` + `ViewNames` constants
- **Dual-mode**: Remote (HTTP API) vs Local (SQL Server LocalDB), sharing Service/Repository layer

## Dependencies

### Internal
- [Shared/](../../Shared/AGENTS.md) — `LYBT.Shared.Models` (DTOs), `LYBT.Shared.ExceptionHandling`, `LYBT.Shared.Configuration`

### External
- Prism.DryIoc
- CommunityToolkit.Mvvm
- Riok.Mapperly
- QuestPDF (printing)
- Refit (HTTP client)

<!-- MANUAL: -->
