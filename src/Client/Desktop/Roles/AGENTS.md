<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# Roles (Desktop)

## Purpose
Role-based workspace modules for the WPF desktop client. Each role defines a distinct user experience with role-specific navigation, views, and permissions. Workspaces are loaded based on the authenticated user's role and provide the top-level layout and navigation structure.

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| LYBT.Desktop.Admin/ | Administrator workspace — `AdminHomeView` / `SystemSettingsView` / `UserManagementView` + `Sysadmin/` ops sub-module（SuperAdmin） |
| LYBT.Desktop.Clinical/ | Doctor 与前台工作台 — 临床工作流（患者选择 / 医案工作区）+ 4 个薄包装管理视图 + `Receptionist/` 前台主页 |

> 前台（Receptionist）已并入 `LYBT.Desktop.Clinical`，不存在独立的 `LYBT.Desktop.Receptionist` 项目。角色定义见 `Core/LYBT.Desktop.Infrastructure/Roles/Definitions/`（Admin / SuperAdmin / Doctor / Receptionist）。

## For AI Agents

### Working In This Directory
- Roles depend on Modules (for views and Controls) but Modules MUST NOT depend on Roles. The only exception is Admin → Clinical navigation by view name (no compile-time reference).
- Each role's home view is declared in its `*RoleDefinition.HomeViewName`: Admin → `AdminHomeView`、SuperAdmin → `SysadminHomeView`、Doctor → `ClinicalWorkspaceView`、Receptionist → `ReceptionistHomeView`；未注册角色 fallback 到 `ClinicalHomeView`。
- Role selection happens at login based on the authenticated user's assigned role.
- When adding a new role, add an `IRoleDefinition` to `RoleRegistry` and register it in `Shell/Extensions/ServiceCollectionExtensions.cs`.

### Common Patterns
- **Thin wrapper views**: management pages are `UserControl` wrappers embedding a business module's `Controls/` (View 在角色台，Control 在业务模块)
- **Navigation**: via `INavigationCoordinator.NavigateTo(ViewNames.X, parameters)`; module lazy loading keyed by `ModuleLazyLoader`

## Dependencies

### Internal
- [Modules/](../Modules/AGENTS.md) — Business module views and Controls
- [Core/](../Core/AGENTS.md) — Contracts, Foundation, Infrastructure

<!-- MANUAL: -->
