# View Completeness Fix Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix navigation gaps (orphan views, missing module registrations) and add missing API client methods + UI buttons for restore/batch operations.

**Architecture:** Minimal changes to existing files. NavigationManager adds sidebar entries. Role definitions add ReportsModule. NavigationCoordinator adds lazy-load mappings. API interfaces extend with new Refit methods. UI adds context menu buttons.

**Tech Stack:** .NET 8, WPF, Prism.DryIoc, CommunityToolkit.Mvvm, Refit

## Global Constraints

- Target framework: `net8.0-windows`
- MVVM: CommunityToolkit.Mvvm `[ObservableProperty]`/`[RelayCommand]` for new code
- Navigation: Prism `RegisterForNavigation` + `INavigationCoordinator.NavigateTo()`
- API calls: Use Refit interfaces from `LYBT.Desktop.Contracts`
- Build verification: `dotnet build LYBTZYZS.sln` after each task

---

### Task 1: Fix DeploymentView Navigation Entry

**Covers:** [S3]

**Files:**
- Modify: `src/Client/Desktop/Shell/Services/NavigationManager.cs:119-123`
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/ViewModels/DeploymentViewModel.cs`
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/DeploymentView.xaml`

**Interfaces:**
- Consumes: `INavigationCoordinator`, `ViewNames.Deployment`
- Produces: DeploymentView reachable from SuperAdmin sidebar

- [ ] **Step 1: Add sidebar entry for DeploymentView in NavigationManager**

In `NavigationManager.cs`, after line 122 (`items.Add(CreateNavItem("日志控制", ViewNames.LogLevelControl, "Tune", "管理"));`), add:

```csharp
items.Add(CreateNavItem("部署管理", ViewNames.Deployment, "Upload", "管理"));
```

- [ ] **Step 2: Add INavigationCoordinator to DeploymentViewModel**

In `DeploymentViewModel.cs`, add field and constructor parameter:

```csharp
private readonly INavigationCoordinator _navigationCoordinator;
```

Update constructor:

```csharp
public DeploymentViewModel(IViewModelServices services, IDeployApi deployApi, INavigationCoordinator navigationCoordinator)
    : base(services)
{
    _deployApi = deployApi;
    _navigationCoordinator = navigationCoordinator;
    PageTitle = "部署管理";
}
```

Add GoBack command:

```csharp
[RelayCommand]
private void GoBack() => _navigationCoordinator.NavigateBack();
```

- [ ] **Step 3: Add back button to DeploymentView XAML**

In `DeploymentView.xaml`, add a back button in the header area:

```xml
<!-- Header -->
<StackPanel Orientation="Horizontal" Margin="0,0,0,16">
    <Button Command="{Binding GoBackCommand}" Style="{StaticResource MaterialDesignIconButton}"
            ToolTip="返回" Margin="0,0,8,0">
        <md:PackIcon Kind="ArrowLeft"/>
    </Button>
    <TextBlock Text="{Binding PageTitle}" Style="{StaticResource MaterialDesignTitleTextBlock}"
               VerticalAlignment="Center"/>
</StackPanel>
```

- [ ] **Step 4: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 5: Commit**

```bash
git add src/Client/Desktop/Shell/Services/NavigationManager.cs \
        src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/ViewModels/DeploymentViewModel.cs \
        src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/DeploymentView.xaml
git commit -m "fix(desktop): add DeploymentView to SuperAdmin sidebar navigation"
```

---

### Task 2: Fix ReportsModule Role Registration

**Covers:** [S4]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Roles/Definitions/AdminRoleDefinition.cs:17-25`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Roles/Definitions/DoctorRoleDefinition.cs:17-26`

**Interfaces:**
- Consumes: `"ReportsModule"` string constant
- Produces: ReportsModule loaded for Admin and Doctor roles

- [ ] **Step 1: Add ReportsModule to AdminRoleDefinition**

In `AdminRoleDefinition.cs`, add `"ReportsModule"` to the Modules array:

```csharp
private static readonly string[] Modules = new[]
{
    "UsersModule",
    "PatientsModule",
    "HerbsModule",
    "FormulaModule",
    "MedicalCaseModule",
    "ReportsModule"
};
```

- [ ] **Step 2: Add ReportsModule to DoctorRoleDefinition**

In `DoctorRoleDefinition.cs`, add `"ReportsModule"` to the Modules array:

```csharp
private static readonly string[] Modules = new[]
{
    "UsersModule",
    "PatientsModule",
    "HerbsModule",
    "FormulaModule",
    "MedicalCaseModule",
    "RegistrationModule",
    "ReportsModule"
};
```

- [ ] **Step 3: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Roles/Definitions/AdminRoleDefinition.cs \
        src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Roles/Definitions/DoctorRoleDefinition.cs
git commit -m "fix(desktop): add ReportsModule to Admin and Doctor role definitions"
```

---

### Task 3: Fix AuditLogView Lazy-Load Mapping

**Covers:** [S5]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationCoordinator.cs:40-53`

**Interfaces:**
- Consumes: `ViewNames.AuditLog`, `ViewNames.Deployment`, `ViewNames.LogLevelControl`, `ViewNames.SystemSettings`
- Produces: Lazy-load triggers for these views

- [ ] **Step 1: Add missing entries to ViewToModuleMap**

In `NavigationCoordinator.cs`, add to the `ViewToModuleMap` dictionary:

```csharp
{ ViewNames.AuditLog, "MedicalCaseModule" },
{ ViewNames.SystemSettings, "AdminModule" },
{ ViewNames.LogLevelControl, "SysadminModule" },
{ ViewNames.Deployment, "SysadminModule" },
```

- [ ] **Step 2: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 3: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationCoordinator.cs
git commit -m "fix(desktop): add AuditLog, SystemSettings, LogLevelControl, Deployment to ViewToModuleMap"
```

---

### Task 4: Add Missing API Client Methods (Restore, Batch, Validation)

**Covers:** [S6]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IPatientApi.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IHerbApi.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IFormulaApi.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IUserApi.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IMedicalCaseApi.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/ApiClient/IApiClient.cs` (if needed)
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/HttpClientApiClient.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/Clients/*.cs` (wrapper classes)

**Interfaces:**
- Consumes: Existing Refit pattern from `IDiagnosticsApi`, `IDeployApi`
- Produces: New API methods for restore, batch-enable/disable, validation

- [ ] **Step 1: Add restore methods to entity APIs**

In `IPatientApi.cs`, add:

```csharp
[Refit.Post("/api/v1/patients/{id}/restore")]
Task<ApiResponse<PatientDetailDto>> RestoreAsync(Guid id);
```

In `IHerbApi.cs`, add:

```csharp
[Refit.Post("/api/v1/herbs/{id}/restore")]
Task<ApiResponse<HerbDetailDto>> RestoreAsync(Guid id);
```

In `IFormulaApi.cs`, add:

```csharp
[Refit.Post("/api/v1/formulas/{id}/restore")]
Task<ApiResponse<FormulaDetailDto>> RestoreAsync(Guid id);
```

In `IUserApi.cs`, add:

```csharp
[Refit.Post("/api/v1/users/{id}/restore")]
Task<ApiResponse<UserDetailDto>> RestoreAsync(Guid id);

[Refit.Post("/api/v1/users/batch-enable")]
Task<ApiResponse<BatchOperationResultDto>> BatchEnableAsync([Refit.Body] BatchDeleteInputDto request);

[Refit.Post("/api/v1/users/batch-disable")]
Task<ApiResponse<BatchOperationResultDto>> BatchDisableAsync([Refit.Body] BatchDeleteInputDto request);
```

- [ ] **Step 2: Add validation methods to Formula API**

In `IFormulaApi.cs`, add:

```csharp
[Refit.Get("/api/v1/formulas/pending-validation")]
Task<ApiResponse<List<FormulaListDto>>> GetPendingValidationAsync();

[Refit.Post("/api/v1/formulas/{formulaId}/herbs/{herbItemId}/validate")]
Task<ApiResponse<FormulaHerbItemDto>> ValidateHerbAsync(
    Guid formulaId,
    Guid herbItemId,
    [Refit.Body] ValidateFormulaHerbInputDto request);
```

- [ ] **Step 3: Add permissions and print methods to MedicalCase API**

In `IMedicalCaseApi.cs`, add:

```csharp
[Refit.Get("/api/v1/medicalcases/{id}/permissions")]
Task<ApiResponse<MedicalCasePermissionsDto>> GetPermissionsAsync(Guid id);

[Refit.Put("/api/v1/medicalcases/{id}/print-completed")]
Task<ApiResponse<MedicalCaseDetailDto>> RecordPrintAsync(
    Guid id,
    [Refit.Body] RecordPrintRequest request);
```

- [ ] **Step 4: Implement in HttpClientApiClient and wrapper classes**

For each new API method, add the implementation in `HttpClientApiClient.cs` and the corresponding wrapper in `Http/Clients/`:

Example for `PatientApi` wrapper (`Http/Clients/PatientApiClient.cs`):

```csharp
public Task<ApiResponse<PatientDetailDto>> RestoreAsync(Guid id)
    => _api.RestoreAsync(id);
```

Repeat for all new methods.

- [ ] **Step 5: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 6: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/ \
        src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/
git commit -m "feat(desktop): add restore, batch-enable/disable, validation, permissions API client methods"
```

---

### Task 5: Add Restore Buttons to MasterDetail Views

**Covers:** [S7]

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Controls/PatientMasterDetailControl.xaml`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Controls/HerbMasterDetailControl.xaml`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Controls/FormulaMasterDetailControl.xaml`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Controls/UserMasterDetailControl.xaml`

**Interfaces:**
- Consumes: `RestoreCommand` from ViewModel (to be added)
- Produces: "恢复" context menu button in each MasterDetail view

- [ ] **Step 1: Add RestoreCommand to MasterDetailViewModelBase**

In `MasterDetailViewModelBase.cs`, add after `BatchDisableAsync`:

```csharp
[RelayCommand(CanExecute = nameof(HasSelection))]
protected virtual async Task RestoreAsync()
{
    var item = SelectedItem;
    if (item == null) return;

    var confirmed = await _masterDetailServices.Dialog.ShowConfirmAsync(
        "确认恢复", "确定要恢复选中的记录吗？");
    if (!confirmed) return;

    await RestoreItemAsync(item);
    await RefreshAsync();
}

protected virtual Task RestoreItemAsync(TListItem item)
{
    return Task.CompletedTask;
}
```

- [ ] **Step 2: Add restore context menu to PatientMasterDetailControl**

In `PatientMasterDetailControl.xaml`, add a "恢复" menu item to the DataGrid context menu (after the existing import/export items):

```xml
<MenuItem Header="恢复" Command="{Binding RestoreCommand}"
          Icon="{StaticResource RestoreIcon}"
          Visibility="{Binding SelectedItem.IsDeleted, Converter={StaticResource BoolToVis}}"/>
```

Note: The visibility binding depends on whether the selected item has an `IsDeleted` property. If not, the button should always be visible and the service layer handles the check.

- [ ] **Step 3: Add restore context menu to other MasterDetail views**

Repeat Step 2 for:
- `HerbMasterDetailControl.xaml`
- `FormulaMasterDetailControl.xaml`
- `UserMasterDetailControl.xaml`

- [ ] **Step 4: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 5: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/MasterDetailViewModelBase.cs \
        src/Client/Desktop/Modules/LYBT.Desktop.Patients/Controls/PatientMasterDetailControl.xaml \
        src/Client/Desktop/Modules/LYBT.Desktop.Herbs/Controls/HerbMasterDetailControl.xaml \
        src/Client/Desktop/Modules/LYBT.Desktop.Formula/Controls/FormulaMasterDetailControl.xaml \
        src/Client/Desktop/Modules/LYBT.Desktop.Users/Controls/UserMasterDetailControl.xaml
git commit -m "feat(desktop): add restore button to all MasterDetail views"
```
