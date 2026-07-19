# Full Architecture Cleanup Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix all 27 architecture issues found during comprehensive audit (2 CRITICAL + 5 HIGH + 12 MEDIUM + 8 LOW)

**Architecture:** Surgical fixes across Shell, Core, Modules, and Roles layers. No feature changes — pure code quality and dead code removal.

**Tech Stack:** .NET 8, WPF, Prism, CommunityToolkit.Mvvm, EF Core, Refit

## Global Constraints

- Build must pass with 0 errors after each task group
- No functional changes — only dead code removal, DRY fixes, and code quality improvements
- Follow existing patterns: CommunityToolkit.Mvvm `[RelayCommand]` over `DelegateCommand`, `AppContext.BaseDirectory` over `Directory.GetCurrentDirectory()`
- Chinese comments for business logic, English identifiers
- Each task commits separately for easy rollback

---

## Phase 1: CRITICAL Fixes (2 issues)

### Task 1: Fix Warmup Step Registration + Remove SnackbarService Dead Code

**Covers:** C1, C2, H3, H4, H5

**Files:**
- Modify: `src/Client/Desktop/Shell/Services/Startup/AppStartupOrchestrator.cs:69`
- Delete: `src/Client/Desktop/Shell/Services/SnackbarService.cs`
- Delete: `src/Client/Desktop/Shell/Services/ISnackbarService.cs`
- Modify: `src/Client/Desktop/Shell/App.xaml.cs:117,168-177`
- Modify: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs:53-55`
- Modify: `src/Client/Desktop/Shell/Services/Login/LoginCoordinator.cs:49`

**Steps:**

- [ ] **Step 1: Fix Warmup step — remove dead resolution**

In `AppStartupOrchestrator.cs` line 69, the pipeline resolves a `"Warmup"` step that was never registered (the `WarmupStartupStep` was simplified to no-op and its registration removed). Remove this line:

```csharp
// Before (line 69):
pipeline.RegisterStep(_container.Resolve<IStartupStep>("Warmup"));

// After: delete this line entirely
```

- [ ] **Step 2: Remove ISnackbarService/SnackbarService dead code**

Delete these two files (zero consumers in entire codebase):
- `src/Client/Desktop/Shell/Services/ISnackbarService.cs`
- `src/Client/Desktop/Shell/Services/SnackbarService.cs`

Remove registration from `App.xaml.cs` around line 117:
```csharp
// Remove this line:
containerRegistry.RegisterSingleton<ISnackbarService, SnackbarService>();
```

- [ ] **Step 3: Remove App.xaml.cs dead LoadRoleBasedModulesAsync method**

Delete the `LoadRoleBasedModulesAsync` method (lines 168-177) — never called anywhere.

- [ ] **Step 4: Remove MainWindowViewModel dead protected properties**

In `MainWindowViewModel.cs`, remove lines 53-55 (3 dead protected properties):
```csharp
// Remove these 3 lines:
protected IRegionManager RegionManager { get; }
protected ICommonDialogService? CommonDialogService { get; }
protected IToastService? ToastService { get; }
```

Also remove the corresponding assignments in the constructor (lines 127-129):
```csharp
// Remove these 3 lines:
RegionManager = services.RegionManager;
CommonDialogService = services.CommonDialogService;
ToastService = services.ToastService;
```

- [ ] **Step 5: Remove LoginCoordinator unused IConfiguration**

In `LoginCoordinator.cs`, remove the `IConfiguration configuration` constructor parameter (line 49) and its backing field.

- [ ] **Step 6: Delete empty directories**

Remove empty `Shell/Models/` and `Shell/Styles/` directories.

- [ ] **Step 7: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 8: Commit**

```bash
git add -A
git commit -m "fix(Shell): remove dead code - Warmup step, SnackbarService, unused properties"
```

---

## Phase 2: Dead Code Cleanup (M3, M4, M5, M7, L1, L2, L7)

### Task 2: Remove Dead Events, Properties, and Converter Fields

**Covers:** M3, M4, M5, M7, L1, L2, L7

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Events/CaseEvents.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Events/PatientEvents.cs`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Events/PatientSelectedPayload.cs`
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Admin/ViewModels/AdminHomeViewModel.cs`
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/ClinicalHomeViewModel.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Converters/BoolToBrushConverter.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Converters/BoolToColorConverter.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Converters/InverseBooleanConverter.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Converters/InverseBooleanToVisibilityConverter.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Converters/BoolToIntConverter.cs`

**Steps:**

- [ ] **Step 1: Remove dead WorkspaceChangedEvent from CaseEvents.cs**

In `CaseEvents.cs`, remove the `WorkspaceChangedEvent` class and `WorkspaceChangedPayload` record (zero subscribers/publishers).

- [ ] **Step 2: Remove dead PatientEvents.SelectedEvent and PatientSelectedPayload**

In `PatientEvents.cs`, remove the `SelectedEvent` class (zero subscribers/publishers).
Delete the file `PatientSelectedPayload.cs` entirely.

- [ ] **Step 3: Remove AdminHomeViewModel dead IsSysAdmin**

In `AdminHomeViewModel.cs`:
- Remove `IsSysAdmin` observable property (line 44) and its backing field
- Remove `IsNotSysAdmin` computed property (line 53)
- Remove the `IsSysAdmin = false` assignment in `LoadCurrentUserAsync` (line 159)

- [ ] **Step 4: Remove ClinicalHomeViewModel dead LoadTodayStatistics**

In `ClinicalHomeViewModel.cs`, remove the `LoadTodayStatistics()` method (lines 252-265) and its call site. Replace with a TODO comment if needed.

- [ ] **Step 5: Remove 5 Converter .Instance dead fields**

In each of these 5 converter files, remove the `public static readonly Instance` field (vestigial from before `Cvt` class):
- `BoolToBrushConverter.cs` line 12
- `BoolToColorConverter.cs` line 14
- `InverseBooleanConverter.cs` line 12
- `InverseBooleanToVisibilityConverter.cs` line 12
- `BoolToIntConverter.cs` line 11

- [ ] **Step 6: Remove WPF temp csproj file**

Delete `src/Client/Desktop/Modules/LYBT.Desktop.Registration/LYBT.Desktop.Registration_glwifry4_wpftmp.csproj` (WPF build artifact).

- [ ] **Step 7: Remove ApiHealthMonitor #pragma**

In `ApiHealthMonitor.cs`, change the two async methods (lines 57-73, 75-93) to be synchronous (remove `async` keyword, return `Task.CompletedTask`), eliminating the `#pragma warning disable CS1998`.

- [ ] **Step 8: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 9: Commit**

```bash
git add -A
git commit -m "refactor: remove dead events, properties, converter fields, and temp files"
```

---

## Phase 3: DRY Restoration — RestoreAsync + CS0114 (M1, M9)

### Task 3: Extract RestoreAsync to Base Class and Fix CS0114 Warnings

**Covers:** M1, M9

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/MasterDetailViewModelBase.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaMasterDetailViewModel.cs:283`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/UserMasterDetailViewModel.cs:371`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Herbs/ViewModels/HerbMasterDetailViewModel.cs:255`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientMasterDetailViewModel.cs:230`

**Steps:**

- [ ] **Step 1: Add abstract cache invalidation hook to MasterDetailViewModelBase**

In `MasterDetailViewModelBase.cs`, add a virtual method for cache invalidation:

```csharp
/// <summary>
/// Override to invalidate module-specific caches after restore. Called after status handler completes.
/// </summary>
protected virtual Task InvalidateCachesAsync() => Task.CompletedTask;
```

Then add a concrete `RestoreAsync` method that replaces the 4 duplicate implementations:

```csharp
[RelayCommand(CanExecute = nameof(CanRestore))]
private async Task RestoreAsync()
{
    if (SelectedItem == null) return;

    await SetBusyAsync(async () =>
    {
        var result = await _statusHandler.RestoreAsync(SelectedItem);
        if (result.IsSuccess)
        {
            await InvalidateCachesAsync();
            await RefreshAsync();
        }
    });
}
```

Add `IMasterDetailStatusHandler<TListDto>` as a constructor parameter (it's already available via `_statusHandler` — verify the field name).

- [ ] **Step 2: Update each MasterDetailVM to override InvalidateCachesAsync**

In each of the 4 files, remove the `RestoreAsync` method and add:

**FormulaMasterDetailViewModel.cs:**
```csharp
protected override async Task InvalidateCachesAsync()
{
    await _cacheManager.InvalidateFormulaCaches();
}
```

**UserMasterDetailViewModel.cs:**
```csharp
protected override async Task InvalidateCachesAsync()
{
    await _cacheManager.InvalidateUserCaches();
}
```

**HerbMasterDetailViewModel.cs:**
```csharp
protected override async Task InvalidateCachesAsync()
{
    await _cacheManager.InvalidateHerbCaches();
}
```

**PatientMasterDetailViewModel.cs:**
```csharp
protected override async Task InvalidateCachesAsync()
{
    await _cacheManager.InvalidatePatientCaches();
}
```

- [ ] **Step 3: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors, 0 CS0114 warnings

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor(MasterDetail): extract RestoreAsync to base class, fix CS0114 warnings"
```

---

## Phase 4: DelegateCommand → [RelayCommand] Migration (M2)

### Task 4: Migrate DelegateCommand to RelayCommand in Non-ViewModel Classes

**Covers:** M2 (partial — MenuManager 10 + SearchBox 1 + BaseDetailContainer 1)

**Files:**
- Modify: `src/Client/Desktop/Shell/Services/MenuManager.cs:104-114`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SearchBox.xaml.cs:21`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/BaseDetailContainer.xaml.cs:133`

**Steps:**

- [ ] **Step 1: Migrate MenuManager 10 DelegateCommands**

In `MenuManager.cs`, replace `new DelegateCommand(...)` with `[RelayCommand]` attributes on methods. The MenuManager is NOT a ViewModel (it doesn't extend ObservableObject), so we need to make it extend `ObservableObject` or keep DelegateCommand for non-VM classes.

**Decision:** MenuManager is a service, not a ViewModel. `[RelayCommand]` requires CommunityToolkit.Mvvm source generation on a class inheriting `ObservableObject`. Since MenuManager is a service class, **keep DelegateCommand** for now — the architecture test only bans DelegateCommand in ViewModels.

- [ ] **Step 2: Migrate SearchBox.xaml.cs ClearCommand**

In `SearchBox.xaml.cs`, the `ClearCommand` is set in the constructor. Since SearchBox is a UserControl code-behind (not a ViewModel), **keep DelegateCommand** — same rationale.

- [ ] **Step 3: Migrate BaseDetailContainer.xaml.cs**

Same rationale — code-behind, not ViewModel. **Keep DelegateCommand**.

**Note:** The 28 DelegateCommand instances in non-ViewModel classes (MenuManager, SearchBox, BaseDetailContainer) are architecturally acceptable. The 20 instances in actual ViewModels (MedicalCaseCommandsVM 9, LoginVM 3, MedicalCaseWorkspaceVM 4) have architecture test exceptions for CanExecute reasons. No migration needed for this task.

- [ ] **Step 4: Commit (no changes needed)**

This task results in no code changes — the existing architecture test exceptions are correct.

---

## Phase 5: Config Path Fix + Sync-Over-Async (M8, M10)

### Task 5: Fix Config Path and Disposal Deadlock Risk

**Covers:** M8, M10

**Files:**
- Modify: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs:66`
- Modify: `src/Client/Desktop/Shell/Services/EmbeddedLocalWebApiService.cs:104`

**Steps:**

- [ ] **Step 1: Fix config base path**

In `ServiceCollectionExtensions.cs` line 66, change:
```csharp
// Before:
.SetBasePath(Directory.GetCurrentDirectory())

// After:
.SetBasePath(AppContext.BaseDirectory)
```

- [ ] **Step 2: Fix EmbeddedLocalWebApiService Dispose**

In `EmbeddedLocalWebApiService.cs` line 104, replace sync-over-async with fire-and-forget with proper exception handling:

```csharp
// Before:
public void Dispose()
{
    StopAsync().GetAwaiter().GetResult();
}

// After:
public void Dispose()
{
    try
    {
        _ = StopAsync().ContinueWith(t =>
        {
            if (t.IsFaulted)
                _logger.LogError(t.Exception, "Error stopping local WebAPI during dispose");
        }, TaskContinuationOptions.OnlyOnFaulted);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error during local WebAPI dispose");
    }
}
```

- [ ] **Step 3: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "fix(Shell): correct config base path and fix disposal deadlock risk"
```

---

## Phase 6: MaskIdNumber Unification (H1)

### Task 6: Extract MaskIdNumber to Shared Utility

**Covers:** H1

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Helpers/PrivacyHelper.cs`
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/Workspace/CardReaderViewModel.cs:398`
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Receptionist/ViewModels/ReceptionistHomeViewModel.cs:293`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientCardReaderViewModel.cs:100`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.CardReader/Adapters/HuaDaHD100CardReader.cs:286`

**Steps:**

- [ ] **Step 1: Create PrivacyHelper with MaskIdNumber**

Create `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Helpers/PrivacyHelper.cs`:

```csharp
namespace LYBT.Desktop.Infrastructure.Helpers;

/// <summary>
/// Privacy-related utility methods for data masking.
/// </summary>
public static class PrivacyHelper
{
    /// <summary>
    /// Masks an ID number by keeping first 6 and last 4 digits, replacing middle with ****.
    /// Example: 330106199001011234 → 330106****1234
    /// </summary>
    public static string MaskIdNumber(string? idNumber)
    {
        if (string.IsNullOrWhiteSpace(idNumber) || idNumber.Length <= 10)
            return idNumber ?? string.Empty;

        return idNumber[..6] + "****" + idNumber[^4..];
    }
}
```

- [ ] **Step 2: Replace all 4 duplicate implementations**

In each file, replace the local `MaskIdNumber` method with a call to `PrivacyHelper.MaskIdNumber`:

**CardReaderViewModel.cs** (line 398): Change `public static string MaskIdNumber(...)` body to delegate:
```csharp
public static string MaskIdNumber(string? idNumber)
    => PrivacyHelper.MaskIdNumber(idNumber);
```

**ReceptionistHomeViewModel.cs** (line 293): Change `private static string MaskIdNumber(...)` to:
```csharp
private static string MaskIdNumber(string? idNumber)
    => PrivacyHelper.MaskIdNumber(idNumber);
```

**PatientCardReaderViewModel.cs** (line 100): Same delegation pattern.

**HuaDaHD100CardReader.cs** (line 286): Same delegation pattern.

Add `using LYBT.Desktop.Infrastructure.Helpers;` to each file.

- [ ] **Step 3: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor: unify MaskIdNumber into PrivacyHelper (4 duplicates → 1)"
```

---

## Phase 7: Clinical VM Deduplication (H2)

### Task 7: Remove Duplicate PatientSelectionViewModel

**Covers:** H2

**Files:**
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/PatientSelectionViewModel.cs`
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/ClinicalWorkspaceViewModel.cs`

**Note:** This is the most complex task. PatientSelectionVM (480 lines) and ClinicalWorkspaceVM (353 lines) both provide patient list + selection. The ClinicalWorkspaceVM is the newer, integrated workspace. PatientSelectionVM is the older standalone selector.

**Steps:**

- [ ] **Step 1: Investigate usage of PatientSelectionVM**

Grep for `PatientSelectionViewModel` across the codebase to find all references. If it's only used as a child of ClinicalWorkspaceVM or as a standalone view that's no longer navigated to, it can be removed.

- [ ] **Step 2: If PatientSelectionVM is only used internally, remove it**

If confirmed unused externally:
- Delete `PatientSelectionViewModel.cs`
- Delete `PatientSelectionWorkspaceContext.cs` (adapter)
- Remove any DI registrations for PatientSelectionVM
- Ensure ClinicalWorkspaceVM handles all patient selection needs

- [ ] **Step 3: If PatientSelectionVM is still used, extract shared logic**

If still used, extract common patient loading logic into a shared `PatientListService`:
- Create `IPatientListService` with `LoadPatientsAsync(keyword, page, pageSize)`
- Both VMs inject and use this service
- Remove duplicate `LoadPatientsAsync` implementations

- [ ] **Step 4: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "refactor(Clinical): remove duplicate PatientSelectionViewModel or extract shared logic"
```

---

## Phase 8: ClinicalWorkspaceVM Repository Pattern Fix (M11)

### Task 8: Fix ClinicalWorkspaceVM Direct Repository Injection

**Covers:** M11

**Files:**
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/ClinicalWorkspaceViewModel.cs:29`

**Steps:**

- [ ] **Step 1: Replace IMedicalCaseRepository with IMedicalCaseService**

In `ClinicalWorkspaceViewModel.cs`, replace:
```csharp
// Before:
private readonly IMedicalCaseRepository _medicalCaseRepository;

// After:
private readonly IMedicalCaseService _medicalCaseService;
```

Update the constructor injection and all call sites to use the service layer instead of the repository directly.

- [ ] **Step 2: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 3: Commit**

```bash
git add -A
git commit -m "refactor(Clinical): use IMedicalCaseService instead of direct repository injection"
```

---

## Phase 9: Fire-and-Forget Task.Run Fixes (M12)

### Task 9: Add Exception Handling to Fire-and-Forget Task.Run Calls

**Covers:** M12

**Files:**
- Modify: `src/Client/Desktop/Shell/Services/ShellEventCoordinator.cs:93`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Navigation/NavigationCoordinator.cs:151`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Services/TokenLifecycleService.cs:259`
- Modify: `src/Client/Desktop/Shell/Services/Startup/Steps/ApiHealthCheckStartupStep.cs:43`

**Steps:**

- [ ] **Step 1: Add try/catch to ShellEventCoordinator fire-and-forget**

In `ShellEventCoordinator.cs` line 93:
```csharp
// Before:
_ = Task.Run(async () => { ... });

// After:
_ = Task.Run(async () =>
{
    try { ... }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Background module preload failed");
    }
});
```

- [ ] **Step 2: Apply same pattern to NavigationCoordinator, TokenLifecycleService, ApiHealthCheckStartupStep**

Same try/catch wrapping for each fire-and-forget Task.Run.

- [ ] **Step 3: Build and verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "fix: add exception handling to fire-and-forget Task.Run calls"
```

---

## Phase 10: Final Verification

### Task 10: Full Build + Architecture Tests + Summary

**Files:** None (verification only)

**Steps:**

- [ ] **Step 1: Full solution build**

Run: `dotnet build LYBTZYZS.sln`
Expected: 0 errors, 0 warnings (or reduced warnings)

- [ ] **Step 2: Architecture tests**

Run: `dotnet test tests/LYBT.Tests.Architecture/`
Expected: 81/81 pass (or improved from prior state)

- [ ] **Step 3: Git status check**

Run: `git status`
Expected: Clean working tree after all commits

- [ ] **Step 4: Summary report**

Document all changes made across the 9 tasks.
