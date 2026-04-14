# Desktop Anti-Pattern Refactoring Plan — Parallelizable Work Breakdown

**Created**: 2026-04-06  
**Goal**: Fix identified anti-patterns in LYBTZYZS Desktop layer (MVVM/Prism/WPF)  
**Estimated Total Effort**: ~4.5 hours (with 2-3 parallel workers)  
**TDD Approach**: Write failing test → Apply fix → Verify pass → Run full suite

---

## Executive Summary

This plan addresses 5 categories of anti-patterns across 20+ files in the Desktop layer:

| Category | Priority | Files Affected | Effort |
|----------|----------|----------------|--------|
| async void methods | CRITICAL | 16+ methods | High |
| Missing CancellationToken | HIGH | 9 service methods | Medium |
| Concrete DI injection | HIGH | 4 locations | Low |
| Event subscription leaks | HIGH | 3 ViewModels | Medium |
| Direct mapper instantiation | MEDIUM | 2 ViewModels | Low |

---

## Execution Strategy

```
Phase 0 (Base) ──→ Phase 1 (Services, parallel) ──→ Phase 2 (VMs, parallel groups) ──→ Phase 3 (Tests)
     │                    │                              │
  Group A            Group B                          Groups C-H
  (sequential)       (parallel pairs)                 (parallel, no conflicts)
```

---

## PHASE 0 — Base Class Foundation (MUST BE FIRST)

### Group A: MasterDetailViewModelBase.cs
**File**: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/MasterDetailViewModelBase.cs`  
**Complexity**: Medium (30 min)  
**Dependencies**: None  
**Blocks**: All VM groups (C-H)

#### Changes Required
```csharp
// BEFORE (current):
public virtual async void OnNavigatedTo(NavigationContext navigationContext)
{
    // async void — exceptions lost
}

// AFTER (target):
public virtual void OnNavigatedTo(NavigationContext navigationContext)
{
    _ = OnNavigatedToAsync(navigationContext)
        .SafeFireAndForget(ex => MasterDetailServices.ErrorHandler.Handle(ex));
}

protected virtual Task OnNavigatedToAsync(NavigationContext navigationContext)
    => Task.CompletedTask;
```

#### TDD Verification
- [ ] RED: Write test that verifies exception in OnNavigatedTo doesn't crash
- [ ] GREEN: Apply base class pattern, test passes
- [ ] VERIFY: All existing MasterDetail VMs inherit correctly

#### Atomic Commit
```
refactor(desktop): Group A — Add safe-fire OnNavigatedToAsync base pattern
```

---

## PHASE 1 — Service Interfaces (PARALLEL EXECUTION)

All 3 pairs are independent — fire simultaneously.

### Group B1: IMedicalCaseService + MedicalCaseService

**Files**:
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Interfaces/IMedicalCaseService.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Services/MedicalCaseService.cs`

**Complexity**: Low (20 min)  
**Dependencies**: None (runs in parallel with B2, B3)

**Methods Requiring CancellationToken**:
| Method | Current Signature | Target Signature |
|--------|-------------------|------------------|
| InitializeAsync | `Task<T>` | `Task<T> CancellationToken ct = default` |
| SaveAsync | `Task<T>` | `Task<T> CancellationToken ct = default` |
| AggregateSaveAsync | `Task<T>` | `Task<T> CancellationToken ct = default` |
| CreateMedicalCaseAsync | `Task<T>` | `Task<T> CancellationToken ct = default` |
| SetPrescriptionFlagAsync | `Task<T>` | `Task<T> CancellationToken ct = default` |
| CloseCaseAsync | `Task<T>` | `Task<T> CancellationToken ct = default` |
| UpdateStatusAsync | `Task<T>` | `Task<T> CancellationToken ct = default` |
| SuspendViaApiAsync | `Task<T>` | `Task<T> CancellationToken ct = default` |
| CancelMedicalCaseViaApiAsync | `Task<T>` | `Task<T> CancellationToken ct = default` |

**Pattern**:
```csharp
// Interface: Add optional CT parameter
Task<CommandResult<T>> SaveAsync(..., CancellationToken ct = default);

// Implementation: Propagate CT to repository/API calls
public async Task<CommandResult<T>> SaveAsync(..., CancellationToken ct = default)
{
    await _repository.SaveAsync(entity, ct); // propagate
}
```

#### Atomic Commit
```
refactor(desktop): Group B1 — Add CancellationToken to IMedicalCaseService
```

### Group B2: IPatientService + PatientService

**Files**:
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Interfaces/IPatientService.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Services/PatientService.cs`

**Complexity**: Low (15 min)  
**Dependencies**: None

**Method**: `CreatePatientAsync(PatientInputDto)` — add `CancellationToken ct = default`

#### Atomic Commit
```
refactor(desktop): Group B2 — Add CancellationToken to IPatientService
```

### Group B3: IFormulaService + FormulaService

**Files**:
- `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Interfaces/IFormulaService.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Formula/Services/FormulaService.cs`

**Complexity**: Low (15 min)  
**Dependencies**: None

**Method**: `SaveFormulaAsync(...)` — add `CancellationToken ct = default`

#### Atomic Commit
```
refactor(desktop): Group B3 — Add CancellationToken to IFormulaService
```

---

## PHASE 2 — ViewModel Fixes (PARALLEL GROUPS)

Groups C-H are independent — fire simultaneously after Phase 1 completes.

### Group C: MedicalCase Module VMs

**Files**:
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/MedicalCaseMasterDetailViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Workspace/MedicalCaseCommandsViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Dialogs/FormulaImportDialogViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Dialogs/HistoryCopyDialogViewModel.cs`

**Complexity**: Medium (40 min)  
**Dependencies**: Group A (base pattern), Group B1 (service CT)

#### Changes Per File

**MedicalCaseMasterDetailViewModel.cs**:
```csharp
// 1. Override async pattern
protected override async Task OnNavigatedToAsync(NavigationContext ctx)
{
    await LoadDataAsync(); // uses new base pattern
}

// 2. Replace direct mapper instantiation
// BEFORE: private readonly MedicalCaseDetailModelMapper _mapper = new();
// AFTER: inject via constructor
public MedicalCaseMasterDetailViewModel(
    ...,
    IMedicalCaseDetailModelMapper mapper)
```

**MedicalCaseCommandsViewModel.cs**:
```csharp
// 6 async void command handlers → AsyncDelegateCommand
// BEFORE:
SaveCommand = new DelegateCommand(ExecuteSave);
private async void ExecuteSave() { ... }

// AFTER:
SaveCommand = new AsyncDelegateCommand(ExecuteSaveAsync);
private async Task ExecuteSaveAsync() { ... }
```

**FormulaImportDialogViewModel.cs**:
```csharp
// 2 async void methods → Task with safe-fire
private async Task LoadFormulasAsync() { ... }
private async Task LoadFormulaPreviewAsync() { ... }
// Call sites use: _ = LoadFormulasAsync().SafeFireAndForget(...)
```

**HistoryCopyDialogViewModel.cs**:
```csharp
// 3 async void methods → Task with safe-fire
private async Task LoadCasesAsync() { ... }
private async Task LoadAllPatientsAsync() { ... }
private async Task LoadCaseDetailAsync() { ... }
```

#### TDD Verification
- [ ] Write test for each command handler verifying exception propagation
- [ ] Verify navigation calls base pattern correctly

#### Atomic Commit
```
refactor(desktop): Group C — Fix async void in MedicalCase module VMs
```

### Group D: Formula Module

**File**: `src/Client/Desktop/Modules/LYBT.Desktop.Formula/ViewModels/FormulaMasterDetailViewModel.cs`

**Complexity**: Low (15 min)  
**Dependencies**: Group A, Group B3

**Changes**:
1. Override `OnNavigatedToAsync` instead of `OnNavigatedTo`
2. Replace `new FormulaDetailModelMapper()` with constructor injection
3. Fix anonymous PropertyChanged handler → proper named method + unsubscription

#### Atomic Commit
```
refactor(desktop): Group D — Fix FormulaMasterDetailViewModel DI and disposal
```

### Group E: Clinical/Workspace VMs

**Files**:
- `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/MedicalCaseWorkspaceViewModel.cs`
- `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/Workspace/CardReaderViewModel.cs`
- `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/ClinicalHomeViewModel.cs`

**Complexity**: Medium (35 min)  
**Dependencies**: Group A, Group B1

**Changes**:

**MedicalCaseWorkspaceViewModel.cs**:
- Override `OnNavigatedToAsync` instead of `OnNavigatedTo`
- Fix `ExecuteSaveChanges` async void → Task
- Verify/unsubscribe CaseEvents.ConsultationCompletedEvent and PrescriptionCompletedEvent

**CardReaderViewModel.cs**:
- Already has IDisposable ✓
- Fix `OnCardReadCompleted` async void → Task with safe-fire

**ClinicalHomeViewModel.cs**:
- Fix `LoadCurrentUser` async void (called from constructor)
- Move to `OnNavigatedToAsync` override

#### Atomic Commit
```
refactor(desktop): Group E — Fix async void in Clinical workspace VMs
```

### Group F: Auth/Admin VMs

**Files**:
- `src/Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/LoginViewModel.cs`
- `src/Client/Desktop/Roles/LYBT.Desktop.Admin/ViewModels/AdminHomeViewModel.cs`

**Complexity**: Medium (30 min)  
**Dependencies**: Group A

**Changes**:

**LoginViewModel.cs**:
```csharp
// BEFORE: Task.Run fire-and-forget in constructor
Task.Run(async () => {
    await Task.Delay(100);
    await LoadSavedCredentialsAsync();
    await LoadApiStatusFromStateServiceAsync();
});

// AFTER: Move to OnNavigatedToAsync
protected override async Task OnNavigatedToAsync(NavigationContext ctx)
{
    await Task.Delay(100); // if still needed
    await LoadSavedCredentialsAsync();
    await LoadApiStatusFromStateServiceAsync();
}

// BEFORE: Application.Current.Shutdown()
Application.Current.Shutdown();

// AFTER: Inject IApplicationLifetime
_appLifetime.CloseApplication();
```

**AdminHomeViewModel.cs**:
- Fix `LoadCurrentUser` async void → move to `OnNavigatedToAsync`

#### Atomic Commit
```
refactor(desktop): Group F — Fix LoginViewModel Task.Run and Application.Current
```

### Group G: Shell (MainWindowViewModel)

**File**: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`

**Complexity**: High (45 min)  
**Dependencies**: Group A

**Changes**:
1. Audit all event subscriptions → add unsubscriptions in Dispose
2. Replace `Application.Current?.Dispatcher.BeginInvoke` → use injected `IUiThreadDispatcher`
3. Fix `OnSessionExpired` and `OnTokenLifecycleStateChanged` async void → Task with safe-fire

**Event subscriptions to audit**:
- PasswordChangedEvent
- TokenLifecycleStateChanged
- SyncStatusChanged

#### TDD Verification
- [ ] Test: Construct VM, dispose, verify no event handler leaks

#### Atomic Commit
```
refactor(desktop): Group G — Fix MainWindowViewModel event leaks and dispatcher coupling
```

### Group H: Other Modules

**Files**:
- `src/Client/Desktop/Modules/LYBT.Desktop.Users/ViewModels/UserMasterDetailViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Sync/ViewModels/SyncViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/ViewModels/PatientMasterDetailViewModel.cs`
- `src/Client/Desktop/Modules/LYBT.Desktop.Patients/Services/PatientImportExecutor.cs`

**Complexity**: Medium (40 min)  
**Dependencies**: Group A, Group B2

**Changes**:

**UserMasterDetailViewModel.cs**:
- Fix anonymous PropertyChanged handler → named method
- Implement proper disposal with unsubscription

**SyncViewModel.cs**:
- Verify callback registration/unregistration lifecycle
- Remove Task.Run + RunOnUIThread pattern for dialogs

**PatientMasterDetailViewModel.cs**:
- Change constructor from `PatientService` to `IPatientService`

**PatientImportExecutor.cs**:
- Replace BackgroundWorker + async void DoWork with Task + CancellationToken + IProgress

#### Atomic Commit
```
refactor(desktop): Group H — Fix DI injection and PatientImportExecutor async pattern
```

---

## PHASE 3 — Verification & Testing

### Full Test Suite Run
```bash
dotnet test tests/LYBT.Tests.Desktop/ --logger "console;verbosity=detailed"
```

### Manual QA Checklist
- [ ] Login flow works (credential save/load)
- [ ] MedicalCase save/complete/suspend operations succeed
- [ ] Formula import dialog loads and previews correctly
- [ ] Patient import works with progress reporting
- [ ] MainWindow event subscriptions clean up on close
- [ ] Navigation between modules doesn't leak memory

### Regression Check
- [ ] All existing 760 Desktop tests pass
- [ ] No new warnings in build output
- [ ] lsp_diagnostics clean on all modified files

---

## Commit Strategy (Atomic Units)

| Order | Group | Files | Commit Message |
|-------|-------|-------|----------------|
| 1 | A | MasterDetailViewModelBase.cs | `refactor(desktop): Group A — Add safe-fire OnNavigatedToAsync base pattern` |
| 2 | B1 | IMedicalCaseService + MedicalCaseService | `refactor(desktop): Group B1 — Add CancellationToken to IMedicalCaseService` |
| 3 | B2 | IPatientService + PatientService | `refactor(desktop): Group B2 — Add CancellationToken to IPatientService` |
| 4 | B3 | IFormulaService + FormulaService | `refactor(desktop): Group B3 — Add CancellationToken to IFormulaService` |
| 5 | C | 4 MedicalCase VMs | `refactor(desktop): Group C — Fix async void in MedicalCase module VMs` |
| 6 | D | FormulaMasterDetailViewModel | `refactor(desktop): Group D — Fix FormulaMasterDetailViewModel DI and disposal` |
| 7 | E | 3 Clinical VMs | `refactor(desktop): Group E — Fix async void in Clinical workspace VMs` |
| 8 | F | LoginViewModel + AdminHomeViewModel | `refactor(desktop): Group F — Fix LoginViewModel Task.Run and Application.Current` |
| 9 | G | MainWindowViewModel | `refactor(desktop): Group G — Fix MainWindowViewModel event leaks and dispatcher coupling` |
| 10 | H | 4 Other module files | `refactor(desktop): Group H — Fix DI injection and PatientImportExecutor async pattern` |

---

## Template Patterns (Reference)

### Pattern 1: async void → Safe-Fire
```csharp
// BEFORE
private async void ExecuteSave()
{
    await SaveAsync();
}

// AFTER
private Task ExecuteSaveAsync() => SaveAsync();

// Command registration
SaveCommand = new AsyncDelegateCommand(ExecuteSaveAsync, CanSave);
// OR with existing DelegateCommand infrastructure:
SaveCommand = new DelegateCommand(
    () => _ = ExecuteSaveAsync().SafeFireAndForget(ex => _errorHandler.Handle(ex)),
    CanSave);
```

### Pattern 2: OnNavigatedTo Override
```csharp
// BEFORE
public override async void OnNavigatedTo(NavigationContext ctx)
{
    await LoadDataAsync();
}

// AFTER
protected override async Task OnNavigatedToAsync(NavigationContext ctx)
{
    await LoadDataAsync();
}
// Base class handles safe-fire automatically
```

### Pattern 3: Event Subscription Lifecycle
```csharp
// Subscription
_eventAggregator.GetEvent<MyEvent>().Subscribe(OnMyEvent);

// Disposal
protected override void Dispose(bool disposing)
{
    if (disposing)
    {
        _eventAggregator.GetEvent<MyEvent>().Unsubscribe(OnMyEvent);
    }
    base.Dispose(disposing);
}
```

### Pattern 4: Mapper DI Extraction
```csharp
// BEFORE
private readonly FormulaDetailModelMapper _mapper = new FormulaDetailModelMapper();

// AFTER
private readonly IFormulaDetailModelMapper _mapper;

public FormulaMasterDetailViewModel(
    ...,
    IFormulaDetailModelMapper mapper)
{
    _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
}
```

### Pattern 5: BackgroundWorker → Task + IProgress
```csharp
// BEFORE
var worker = new BackgroundWorker();
worker.DoWork += async (s, e) => { await ProcessRowAsync(row); };

// AFTER
public async Task<ImportResult> ExecuteImportAsync(
    DataTable data,
    IProgress<ImportProgressInfo> progress,
    CancellationToken ct)
{
    for (int i = 0; i < data.Rows.Count; i++)
    {
        ct.ThrowIfCancellationRequested();
        await ProcessRowAsync(data.Rows[i]);
        progress?.Report(new ImportProgressInfo(i + 1, data.Rows.Count));
    }
    return ImportResult.Success();
}
```

---

## Risk Assessment

| Risk | Mitigation |
|------|------------|
| Breaking existing command bindings | Use AsyncDelegateCommand wrapper, keep same command names |
| Navigation timing changes | Base pattern preserves synchronous OnNavigatedTo signature |
| Test failures from DI changes | Update test constructors to inject interfaces |
| Event handler memory leaks | Audit all subscriptions, add disposal tests |

---

## Success Criteria

- [ ] Zero `async void` methods (except event handlers with safe-fire wrapper)
- [ ] All service async methods accept optional CancellationToken
- [ ] All ViewModels inject interfaces, not concrete types
- [ ] All event subscriptions have matching unsubscriptions in Dispose
- [ ] All 760 existing Desktop tests pass
- [ ] lsp_diagnostics shows no errors/warnings in modified files

---

*Plan ready for ultrawork execution. Groups A-H can be distributed across parallel workers.*
