# Desktop View Audit Fix Design

> Generated 2026-07-16 via comprehensive API-to-View audit

## [S1] Problem

Desktop client has incomplete view coverage: 2 missing views, 3 broken features, and 1 enhancement opportunity. Server API has 68 endpoints across 13 controllers, but Desktop only covers ~90% of functionality. Key gaps: deployment management, audit logs, batch operations, and clinical pending queue.

## [S2] Scope

| # | Item | Type | Priority |
|---|------|------|----------|
| S3 | Deployment Management View | New view | P1 |
| S4 | Medical Case Audit Log View | New view | P1 |
| S5 | Batch Delete Fix | Bug fix | P0 |
| S6 | Batch Enable/Disable UI | New UI | P1 |
| S7 | Clinical PendingQueue Fix | Bug fix | P0 |
| S8 | Reports Date Picker | Enhancement | P2 |

## [S3] Deployment Management View

### Context

Server exposes `POST /api/v1/deploy/upload` (ZIP upload) and `POST /api/v1/deploy/restart` (hot restart). No Desktop view exists. Only SuperAdmin role should access this.

### Design

**Location:** `LYBT.Desktop.Sysadmin/Views/DeploymentView.xaml` + `ViewModels/DeploymentViewModel.cs`

**Registration:** Add `DeploymentView` to `SysadminModule.RegisterTypes()` as navigation target. Add `ViewNames.Deployment` constant.

**UI Layout:** Single-page card layout (consistent with LogLevelControlView style):

```
┌─────────────────────────────────────────┐
│  部署管理                                │
├─────────────────────────────────────────┤
│  ┌─ 上传更新包 ─────────────────────┐   │
│  │  [选择文件...] xxx.zip           │   │
│  │  [上传并部署]                     │   │
│  │  上传进度: ████████░░ 80%        │   │
│  │  状态: 上传成功 / 失败原因        │   │
│  └──────────────────────────────────┘   │
│                                         │
│  ┌─ 服务控制 ───────────────────────┐   │
│  │  [重启服务]                       │   │
│  │  状态: 重启中... / 重启成功       │   │
│  └──────────────────────────────────┘   │
└─────────────────────────────────────────┘
```

**ViewModel Commands:**
- `SelectFileCommand` — Open OpenFileDialog, filter `*.zip`
- `UploadCommand` — Call `_deployApi.UploadAsync(file)`, report progress
- `RestartCommand` — Call `_deployApi.RestartAsync()`, show confirmation dialog first

**API Client:** Add `IDeployApi` interface to `LYBT.Shared.Models.Contracts` with `UploadAsync(Stream)` and `RestartAsync()`.

**Navigation:** Add to Sysadmin role definition modules list. Add sidebar nav item "部署管理" under Sysadmin workspace.

### Files to Create/Modify

| Action | File |
|--------|------|
| Create | `LYBT.Desktop.Sysadmin/Views/DeploymentView.xaml` |
| Create | `LYBT.Desktop.Sysadmin/Views/DeploymentView.xaml.cs` |
| Create | `LYBT.Desktop.Sysadmin/ViewModels/DeploymentViewModel.cs` |
| Modify | `LYBT.Desktop.Sysadmin/SysadminModule.cs` — register navigation |
| Modify | `LYBT.Desktop.Infrastructure/Constants/ViewNames.cs` — add `Deployment` |
| Modify | `LYBT.Desktop.Infrastructure/Roles/Definitions/SuperAdminRoleDefinition.cs` — add module |

## [S4] Medical Case Audit Log View

### Context

Server exposes `GET /api/v1/medicalcases/{id}/audit-logs` (paged). No Desktop view exists. Useful for tracking who modified a medical case and when.

### Design

**Location:** `LYBT.Desktop.MedicalCase/Views/AuditLogView.xaml` + `ViewModels/AuditLogViewModel.cs`

**Trigger:** Button in MedicalCase detail toolbar → opens AuditLogView as a sub-panel or navigation.

**UI Layout:** Paged list within the medical case workspace:

```
┌─────────────────────────────────────────┐
│  审计日志 ← 返回医案                     │
├─────────────────────────────────────────┤
│  ┌─ 日志列表 (DataGrid) ────────────┐   │
│  │ 时间          操作人    操作类型   │   │
│  │ 2026-07-16   张医生    更新诊断   │   │
│  │ 2026-07-15   李护士    创建医案   │   │
│  │ ...                              │   │
│  └──────────────────────────────────┘   │
│  [上一页]  第 1/3 页  [下一页]          │
└─────────────────────────────────────────┘
```

**ViewModel:**
- `LoadAuditLogsAsync(medicalCaseId, page, pageSize)` — calls `_medicalCaseApi.GetAuditLogsAsync()`
- `PagedResult<AuditLogDto>` for pagination
- `NavigateBackCommand` — return to medical case detail

**Integration:** Add "审计日志" button to `MedicalCaseCommandsViewModel` toolbar. Navigate to `AuditLogView` with `medicalCaseId` parameter.

### Files to Create/Modify

| Action | File |
|--------|------|
| Create | `LYBT.Desktop.MedicalCase/Views/AuditLogView.xaml` |
| Create | `LYBT.Desktop.MedicalCase/Views/AuditLogView.xaml.cs` |
| Create | `LYBT.Desktop.MedicalCase/ViewModels/AuditLogViewModel.cs` |
| Modify | `LYBT.Desktop.MedicalCase/MedicalCaseModule.cs` — register navigation |
| Modify | `LYBT.Desktop.MedicalCase/ViewModels/Workspace/MedicalCaseCommandsViewModel.cs` — add button |

## [S5] Batch Delete Fix

### Context

All 5 MasterDetail views have a "批量删除" button bound to `BatchDeleteCommand`, but `MasterDetailViewModelBase.DeleteAsync()` only processes `SelectedItem` (single item). The DataGrid has checkboxes (`ShowCheckBoxColumn="True"`) but the selection is never used.

### Design

**Root cause:** `MasterDetailViewModelBase.DeleteAsync()` at line ~500 calls `DeleteItemAsync(SelectedItem)`.

**Fix:** Modify `DeleteAsync()` to:
1. Check if `SelectedItems` (the full checked set) has items
2. If multiple items selected → show confirmation: "即将删除 {N} 条记录，确认？"
3. If confirmed → call batch API endpoint (`BatchDeleteAsync(ids)`)
4. If single item selected → keep existing behavior (confirm + delete)
5. Refresh list after batch delete

**Base class changes:** `MasterDetailViewModelBase.cs`
- Add `BatchDeleteCommand` property (rebind from `DeleteCommand`)
- Modify `DeleteAsync()` to handle both single and batch scenarios
- Call `DeleteBatchAsync(IEnumerable<TListDto> items)` virtual method for batch
- Default implementation calls service `BatchDeleteAsync(ids)`

**Per-module service layer:** Ensure each service has `BatchDeleteAsync(IEnumerable<Guid> ids)`:
- `UserService` — already has `BatchDeleteAsync`
- `PatientService` — already has `BatchDeleteAsync`
- `HerbService` — already has `BatchDeleteAsync`
- `FormulaService` — already has `BatchDeleteAsync`
- `MedicalCaseService` — already has `BatchDeleteAsync`

### Files to Modify

| Action | File |
|--------|------|
| Modify | `LYBT.Desktop.Infrastructure/ViewModels/MasterDetailViewModelBase.cs` |

## [S6] Batch Enable/Disable UI

### Context

Server supports `POST /api/v1/{entity}/batch-enable` and `POST /api/v1/{entity}/batch-disable`. Service layer has methods but no UI buttons exist.

### Design

**UI:** Add "批量启用" and "批量禁用" buttons to each MasterDetail toolbar (next to existing "批量删除").

**Visibility rules:**
- Buttons visible only when items are selected (checkboxes checked)
- "批量启用" hidden if all selected items are already enabled
- "批量禁用" hidden if all selected items are already disabled

**DataGridToolbar changes:** Add `BatchEnableCommand` and `BatchDisableCommand` optional bindings.

**ViewModel:** Add to `MasterDetailViewModelBase`:
- `BatchEnableAsync()` — calls service `BatchEnableAsync(ids)`
- `BatchDisableAsync()` — calls service `BatchDisableAsync(ids)`
- Refresh list after operation

### Files to Modify

| Action | File |
|--------|------|
| Modify | `LYBT.Desktop.Controls/Controls/DataGridToolbar.xaml` — add buttons |
| Modify | `LYBT.Desktop.Controls/Controls/DataGridToolbar.xaml.cs` — add command deps |
| Modify | `LYBT.Desktop.Infrastructure/ViewModels/MasterDetailViewModelBase.cs` — add methods |

## [S7] Clinical PendingQueue Fix

### Context

`PendingQueueView.xaml` has real XAML layout (patient cards, "继续看诊" button), but `PendingQueueViewModel.RefreshQueueAsync()` is a no-op: clears queue, logs "共0条", returns `Task.CompletedTask`. Never calls any API.

### Design

**Fix `PendingQueueViewModel.RefreshQueueAsync()`:**
1. Get current doctor ID from authentication context
2. Call `_registrationService.GetQueueAsync(doctorId)` (same as Registration module)
3. Map result to `Queue` ObservableCollection
4. Handle loading/error states

**Reference implementation:** `RegistrationListViewModel.GetWaitingQueueAsync()` (line 282-317) — already does this correctly.

**Dependencies:** Inject `IRegistrationService` into `PendingQueueViewModel`.

### Files to Modify

| Action | File |
|--------|------|
| Modify | `LYBT.Desktop.Clinical/ViewModels/Workspace/PendingQueueViewModel.cs` |
| Modify | `LYBT.Desktop.Clinical/ClinicalModule.cs` — ensure IRegistrationService is available |

## [S8] Reports Date Picker Enhancement

### Context

`ReportsHomeView.xaml` shows 3 data cards (income, consultations, herb usage) but always shows today's data. No way to view historical data.

### Design

**UI:** Add a date picker row above the 3 cards:

```
┌─────────────────────────────────────────┐
│  统计报表                    [今天] 📅   │
├─────────────────────────────────────────┤
│  ┌─ 当日收入 ──┐ ┌─ 看诊量 ──┐ ┌─ 药材 ──┐│
│  │  ¥1,280.00  │ │    12     │ │   45   ││
│  │  挂号: ¥200 │ │  张: 5    │ │  当归:8││
│  │  药品: ¥1080│ │  李: 7    │ │  黄芪:6││
│  └─────────────┘ └───────────┘ └────────┘│
└─────────────────────────────────────────┘
```

**ViewModel changes:**
- Add `SelectedDate` property (default: `DateTime.Today`)
- Add `LoadDataCommand` rebind to refresh on date change
- Pass date to API calls (check if API supports date parameter — may need server-side change)

**Note:** If server API doesn't support date parameter, this item becomes P2 and can be deferred. The current "today only" view is functional.

### Files to Modify

| Action | File |
|--------|------|
| Modify | `LYBT.Desktop.Reports/Views/ReportsHomeView.xaml` — add DatePicker |
| Modify | `LYBT.Desktop.Reports/ViewModels/ReportsHomeViewModel.cs` — add date logic |

## [S9] Implementation Order

| Phase | Items | Rationale |
|-------|-------|-----------|
| 1 | S5 (Batch Delete Fix) + S7 (PendingQueue Fix) | P0 bugs — existing features are broken |
| 2 | S6 (Batch Enable/Disable UI) | Builds on S5 base class changes |
| 3 | S3 (Deployment View) + S4 (Audit Log View) | New views, independent of each other |
| 4 | S8 (Reports Date Picker) | Enhancement, lowest priority |

## [S10] Testing Strategy

- **S5/S6:** Manual test with sysadmin account — select multiple users/herbs/patients, verify batch delete/enable/disable
- **S7:** Login as Doctor → Clinical workspace → verify pending queue shows real data
- **S3:** Login as SuperAdmin → navigate to Deployment → verify upload/restart
- **S4:** Open MedicalCase detail → click Audit Logs → verify log list loads
- **S8:** Open Reports → change date → verify data refreshes (if API supports it)
- **Build:** `dotnet build LYBTZYZS.sln` must pass with 0 errors
