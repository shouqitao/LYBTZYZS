<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# Modules (Desktop)

## Purpose
Business modules for the WPF desktop client. Each module encapsulates a domain area (Auth, Patients, Catalog, MedicalCase, Registrations, Users) and follows the Prism module pattern with `IModule` registration, region-based navigation, and ViewModels inheriting from `NavigableViewModelBase` or `MasterDetailViewModelBase`. Modules are strictly isolated — cross-module references are forbidden.

> Herbs 与 Formula 已合并为 **Catalog** 模块（药房目录）。Sync 模块为 v2.0 规划（v1.0 远程/本地数据孤立，见 docs/compose/specs/2026-06-28-docs-reconciliation-baseline.md §2），当前不存在。

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| LYBT.Desktop.Auth/ | Authentication and login module（LoginView + ServerConfig/FirstRunSetup 对话框；无 ModuleDependency） |
| LYBT.Desktop.Catalog/ | 药房目录（Herbs + Formula 合并）— 含跨模块 `IHerbSearchProvider`/`IFormulaSearchProvider` |
| LYBT.Desktop.MedicalCase/ | Medical case (consultation + prescription) — 聚合根；含 `Reports/` 报表子模块（STUB） |
| LYBT.Desktop.Patients/ | Patient management — list, detail, search, CRUD, 读卡建档, Excel 导入导出 |
| LYBT.Desktop.Registrations/ | 挂号队列（创建 → 等待 → 接诊/取消）+ SignalR 实时通知 |
| LYBT.Desktop.Users/ | User account management（MasterDetail + Password/Status Handler） |

## For AI Agents

### Working In This Directory
- Each module is a self-contained Prism `IModule` — registered in `{Domain}Module.cs`.
- Modules MUST NOT reference each other at compile time. Cross-module communication goes through `LYBT.Desktop.Contracts` interfaces (`Services.*` / `Repositories.*` / `CrossModule.*`) or Prism `IEventAggregator`.
  - `[ModuleDependency("...")]` only declares **runtime load order**, not a compile-time reference. Current declarations: `UsersModule` → Authentication；`PatientsModule` → Authentication, Users；`CatalogModule` → Authentication；`MedicalCaseModule` → Patients, Catalog；`RegistrationModule` → Authentication, Patients, Users. Navigation-parameter contracts (`WorkspaceMode`/`EditState`/`MedicalCaseNavigationParameters`) live in `LYBT.Desktop.Contracts`（A-18 批次2）, so Registrations no longer references any business module.
  - Role workspaces（`Roles/`）are the exception by design: they host views and embed other modules' `Controls/` (View 在角色台，Control 在业务模块).
- All ViewModels inherit from `NavigableViewModelBase` (single entity) or `MasterDetailViewModelBase<TListDto, TDetailModel>` (list/grid); edit sub-VMs use `EditorViewModelBase<TContext>`.
- Navigation uses Prism region-based navigation via `INavigationCoordinator.NavigateTo(ViewNames.X, parameters)`.
- Data access: inject `I{Entity}Repository` via `LYBT.Desktop.Contracts.Repositories`; repositories derive from `EntityApiClientRepositoryBase<...>` and route through `IApiClient` sub-interfaces.
- Dialogs are registered with `containerRegistry.RegisterDialog<TView, TViewModel>()`.
- Modules that expose embeddable UI provide `Controls/` (not `Views/`), and map the control to its VM via `ViewModelLocationProvider.Register(typeof(XControl).ToString(), typeof(XViewModel))`.

### Common Patterns
- **Module registration**: `public class {Domain}Module : IModule { void RegisterTypes(IContainerRegistry) {...} }`
- **ViewModel lifecycle**: `OnNavigatedToCore` / `OnNavigatedFromCore` (override the `*Core` hooks) for Prism navigation awareness
- **List pattern**: `MasterDetailViewModelBase<TListDto, TDetailModel>` with built-in paging, filtering, selection
- **Aggregate pattern**: MedicalCase module orchestrates Consultation + Prescription via `IMedicalCaseService` + `MedicalCaseEditContext`（编辑会话单例）

## Dependencies

### Internal
- [Core/](../Core/AGENTS.md) — `LYBT.Desktop.Contracts`, `LYBT.Desktop.Foundation`, `LYBT.Desktop.Infrastructure`, `LYBT.Desktop.Controls`, `LYBT.Desktop.Printing`
- [Shared/](../../../Shared/AGENTS.md) — `LYBT.Shared.Models` (DTOs), `LYBT.Shared.ExceptionHandling`

### External
- Prism.DryIoc (MVVM, DI, navigation)
- CommunityToolkit.Mvvm (`[ObservableProperty]`, `[RelayCommand]`)
- Riok.Mapperly (compile-time object mapping; AutoMapper is forbidden)
- ClosedXML (Patients Excel 导入导出, B-12)
- Microsoft.AspNetCore.SignalR.Client (Registrations US-REG-008)

<!-- MANUAL: -->
