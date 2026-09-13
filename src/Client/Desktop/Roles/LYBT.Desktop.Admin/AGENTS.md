# LYBT.Desktop.Admin - Desktop Admin Workspace

**Purpose**: Admin/SuperAdmin role workspace. Owns a small set of views (admin home, system settings, user management) and the `Sysadmin/` operations sub-module. Most management views it links to live in `Roles/LYBT.Desktop.Clinical` (thin wrappers over business-module Controls).

## Structure

```
LYBT.Desktop.Admin/
├── Views/               # AdminHomeView, SystemSettingsView, UserManagementView (thin wrapper over Users Control)
├── ViewModels/          # AdminHomeViewModel, SystemSettingsViewModel
├── Services/            # ISystemSettingsService/SystemSettingsService, IServerConfigurationService/…, IDeploymentService, IDiagnosticsService
├── Sysadmin/            # SysadminModule: SysadminHomeView + LogLevelControlView + DeploymentView +
│                        #   BackupManagementView + SecurityAuditLogView (+ 8 VMs, 2 services, Models/DashboardStatus)
└── AdminModule.cs       # Prism IModule registration
```

## WHERE TO LOOK

| Task | Location | Notes |
|------|----------|-------|
| Module registration | `AdminModule.cs` | No `[ModuleDependency]`; registers 2 VMs, `IServerConfigurationService`, 3 navigation views |
| Dashboard | `Views/AdminHomeView.xaml` | Admin role home (`AdminRoleDefinition.HomeViewName`) |
| Ops console | `Sysadmin/Views/SysadminHomeView.xaml` | SuperAdmin home; embeds ConfigCenter / ServerConfig / CardReaderDiagnostics panel VMs |
| Embedded control | `Views/UserManagementView.xaml` | Embeds `LYBT.Desktop.Users.Controls.UserMasterDetailControl`; supports `DefaultRoleFilter` nav param |
| Settings services | `Services/` | Local JSON (`system-settings.json`) + `clinic-settings.json` hot reload + server config sections |

## CONVENTIONS

- **Control reuse** — `UserManagementView` is a `UserControl` wrapper over the business module's Control (View 在角色台，Control 在业务模块)
- **Card navigation by view name** — `AdminHomeViewModel` navigates with `ViewNames` constants only; `HerbManagement`/`FormulaManagement`/`PatientManagement`/`MedicalCaseManagement` are registered by `ClinicalModule`
- **Panel sub-VMs** — `ConfigurationCenterViewModel` / `ServerConfigSectionViewModel` / `CardReaderDiagnosticsViewModel` are hosted as properties of `SysadminHomeViewModel`; they have no standalone views
- **Role-based loading** — `AdminModule` and `SysadminModule` are both loaded `WhenAvailable`

## ANTI-PATTERNS

- **Creating `AdminDashboardView`/`ClinicalDashboardView`-style names** — actual names are `AdminHomeView` / `SysadminHomeView`
- **Duplicating management views in Admin** — reuse the Clinical thin wrappers via navigation instead
- **Business logic in workspace** — logic stays in business modules; the workspace is layout only
