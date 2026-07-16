# View Completeness Fix Design

> Generated 2026-07-16 via comprehensive navigation + API coverage audit

## [S1] Problem

Desktop client has navigation gaps: DeploymentView is an orphan (registered but unreachable), ReportsModule is not loaded by any role (sidebar never shows), and AuditLogView is missing from the lazy-load map. Additionally, several server API endpoints lack corresponding Desktop client methods and UI buttons.

## [S2] Scope

| # | Item | Type | Priority |
|---|------|------|----------|
| S3 | DeploymentView navigation fix | Bug fix | P0 |
| S4 | ReportsModule role registration | Bug fix | P0 |
| S5 | AuditLogView lazy-load fix | Bug fix | P1 |
| S6 | Missing API client methods | New API | P1 |
| S7 | Missing UI buttons (restore, batch ops) | New UI | P1 |

## [S3] DeploymentView Navigation Fix

### Context

`DeploymentView` is registered in `SysadminModule` but `NavigationManager.BuildNavigationItems()` does not add a sidebar entry for it. The view is completely unreachable.

### Design

**File: `Shell/Services/NavigationManager.cs`**
Add "部署管理" entry in the SuperAdmin section (after "日志控制"):

```csharp
if (modules.Contains("SysadminModule"))
{
    items.Add(new NavItem("系统运维主页", ViewNames.SysadminHome, "Home"));
    items.Add(new NavItem("诊所信息", ViewNames.SystemSettings, "Settings"));
    items.Add(new NavItem("日志控制", ViewNames.LogLevelControl, "Settings"));
    items.Add(new NavItem("部署管理", ViewNames.Deployment, "Settings"));  // NEW
}
```

**File: `Roles/LYBT.Desktop.Sysadmin/ViewModels/DeploymentViewModel.cs`**
Add GoBack command:

```csharp
private readonly INavigationCoordinator _navigationCoordinator;

[RelayCommand]
private void GoBack() => _navigationCoordinator.NavigateBack();
```

Update constructor to accept `INavigationCoordinator`.

**File: `Roles/LYBT.Desktop.Sysadmin/Views/DeploymentView.xaml`**
Add back button in header area.

## [S4] ReportsModule Role Registration

### Context

`ReportsModule` is never loaded because no role definition includes it in `RequiredModules`. The sidebar "统计报表" item never appears.

### Design

**File: `Core/LYBT.Desktop.Infrastructure/Roles/Definitions/AdminRoleDefinition.cs`**
Add `"ReportsModule"` to `RequiredModules`.

**File: `Core/LYBT.Desktop.Infrastructure/Roles/Definitions/DoctorRoleDefinition.cs`**
Add `"ReportsModule"` to `RequiredModules`.

**File: `Shell/Services/NavigationManager.cs`**
Verify the sidebar builder already handles `ReportsModule` (it does at line 116-117).

## [S5] AuditLogView Lazy-Load Fix

### Context

`AuditLogView` is registered in `MedicalCaseModule` but not in `ViewToModuleMap`. If a user navigates to AuditLog before MedicalCaseModule is loaded, the lazy-load won't trigger.

### Design

**File: `Core/LYBT.Desktop.Navigation/NavigationCoordinator.cs`**
Add to `ViewToModuleMap`:

```csharp
{ ViewNames.AuditLog, "MedicalCaseModule" },
{ ViewNames.SystemSettings, "AdminModule" },
{ ViewNames.LogLevelControl, "SysadminModule" },
{ ViewNames.Deployment, "SysadminModule" },
```

## [S6] Missing API Client Methods

### Context

Server has several endpoints that Desktop has no client methods for. These are needed for restore, batch operations, reference checks, and validation features.

### Design

**Add to `LYBT.Desktop.Contracts/Api/` Refit interfaces:**

| Interface | Method | Server Endpoint |
|-----------|--------|-----------------|
| `IPatientApi` | `RestoreAsync(Guid id)` | `POST /patients/{id}/restore` |
| `IPatientApi` | `CheckReferenceAsync(Guid id)` | `GET /patients/{id}/check-reference` |
| `IHerbApi` | `RestoreAsync(Guid id)` | `POST /herbs/{id}/restore` |
| `IFormulaApi` | `RestoreAsync(Guid id)` | `POST /formulas/{id}/restore` |
| `IFormulaApi` | `GetPendingValidationAsync()` | `GET /formulas/pending-validation` |
| `IFormulaApi` | `ValidateHerbAsync(Guid formulaId, Guid herbItemId, ValidateFormulaHerbInputDto request)` | `POST /formulas/{formulaId}/herbs/{herbItemId}/validate` |
| `IUserApi` | `RestoreAsync(Guid id)` | `POST /users/{id}/restore` |
| `IUserApi` | `BatchEnableAsync(BatchDeleteInputDto request)` | `POST /users/batch-enable` |
| `IUserApi` | `BatchDisableAsync(BatchDeleteInputDto request)` | `POST /users/batch-disable` |
| `IMedicalCaseApi` | `GetPermissionsAsync(Guid id)` | `GET /medicalcases/{id}/permissions` |
| `IMedicalCaseApi` | `RecordPrintAsync(Guid id, RecordPrintRequest request)` | `PUT /medicalcases/{id}/print-completed` |

**Add to `LYBT.Desktop.Contracts/ApiClient/IApiClient.cs` corresponding methods.**

**Implement in `LYBT.Desktop.Foundation/Http/HttpClientApiClient.cs` and `Http/Clients/` wrappers.**

## [S7] Missing UI Buttons

### Context

Server supports restore, batch-enable/disable, but Desktop has no UI buttons for these operations.

### Design

**Restore buttons:**
In each MasterDetail view's context menu or toolbar, add "恢复" button that calls `RestoreAsync` on the selected soft-deleted item.

**Batch enable/disable:**
Already implemented in Task 3 (S6 from previous spec). The base class `MasterDetailViewModelBase` has `BatchEnableCommand` and `BatchDisableCommand`. Subclasses need to override `SetItemEnabledAsync` to call the service API.

**Files to modify for restore:**
- `LYBT.Desktop.Patients/Controls/PatientMasterDetailControl.xaml` — add restore menu item
- `LYBT.Desktop.Herbs/Controls/HerbMasterDetailControl.xaml` — add restore menu item
- `LYBT.Desktop.Formula/Controls/FormulaMasterDetailControl.xaml` — add restore menu item
- `LYBT.Desktop.Users/Controls/UserMasterDetailControl.xaml` — add restore menu item

**Files to modify for batch enable/disable service calls:**
Each `*MasterDetailViewModel` needs to override `SetItemEnabledAsync` to call the appropriate API.

## [S8] Implementation Order

| Phase | Items | Rationale |
|-------|-------|-----------|
| 1 | S3 + S4 + S5 | Navigation fixes (P0/P1 bugs) |
| 2 | S6 | API client methods (foundation for UI) |
| 3 | S7 | UI buttons (depends on S6) |

## [S9] Testing Strategy

- **S3:** Login as SuperAdmin → verify "部署管理" appears in sidebar → click → verify DeploymentView loads → click back → verify returns
- **S4:** Login as Admin or Doctor → verify "统计报表" appears in sidebar → click → verify ReportsHomeView loads
- **S5:** Navigate to AuditLog from MedicalCaseWorkspace → verify no errors
- **S6:** Build must pass with 0 errors
- **S7:** Manual test restore/batch operations on each entity type
- **Build:** `dotnet build LYBTZYZS.sln` must pass after each phase
