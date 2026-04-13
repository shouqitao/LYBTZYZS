# Two-Page Separation Refactoring Plan

**Date**: 2026-04-10
**Status**: Draft - Awaiting Approval
**Scope**: Refactor Clinical Workstation from single-page to two-page layout

---

## 1. Current State Analysis

### MedicalCaseWorkspaceView (254 lines)
```
+----------------------------------+
| Left 25% (280-350px)  | Right 75%                    |
| - CardReaderStatus     | BaseDetailContainer          |
| - PatientInfoCard      |   ViewContent (readonly)     |
| - PendingQueue         |   EditContent (edit mode)    |
|                        |   FooterContent (actions)    |
+----------------------------------+
```
- **Problem**: Left sidebar wastes 280-350px of precious screen width that should be used for diagnosis/prescription editing. CardReader and PendingQueue belong in the patient selection phase, not the active case editing phase.

### MedicalCaseWorkspaceViewModel (689 lines)
- Composite VM with **5 child VMs**: ConsultationEditor, PrescriptionEditor, Commands, PendingQueue, CardReader
- Constructor receives **9 parameters** (8 services + optional dialog)
- Implements `IMedicalCaseWorkspaceContext` + `IWorkspaceHost`
- PendingQueue + CardReader are manually created (not DI-resolved), tightly coupled to parent lifecycle

### PatientSelectionView (87 lines)
```
+----------------------------------+
| Header: "患者选择"                |
| PatientSelectionControl (full)   |
| Footer: Status + "开始接诊" btn  |
+----------------------------------+
```
- Simple 3-row layout, no CardReader, no PendingQueue

### PatientSelectionViewModel (429 lines)
- Dependencies: IPatientApi, IMedicalCaseApi, IMedicalCaseService, INavigationCoordinator
- **No CardReader or PendingQueue child VMs**
- Navigates to MedicalCaseWorkspace with: MedicalCaseId, CurrentPatient, WorkspaceMode, EditState

### Key Child VMs (to be moved)

| VM | Lines | Dependencies | Coupled Interfaces |
|----|-------|-------------|-------------------|
| CardReaderViewModel | 456 | ICardReaderService, IPatientCardReaderIntegration, IMedicalCaseService, INavigationCoordinator | IMedicalCaseWorkspaceContext, IWorkspaceHost |
| PendingQueueViewModel | 333 | IMedicalCaseService, IPendingQueueManager, INavigationCoordinator | IMedicalCaseWorkspaceContext, IWorkspaceHost |

### Existing Assets
- **PendingQueueView.xaml** (112 lines) — Already exists as standalone view, registered in ClinicalModule
- **IWorkspaceHost** — Located in `LYBT.Desktop.Contracts/Services/IWorkspaceHost.cs`, provides SetBusy/ShowError/ShowSuccess/ShowConfirm/NotifyStateChanged
- **IMedicalCaseWorkspaceContext** — Located in `LYBT.Desktop.MedicalCase/Interfaces/`, provides State/MedicalCaseId/CurrentPatient/SessionManager

### Test Inventory
| Test File | Tests | Impact |
|-----------|-------|--------|
| `PureLogic/Clinical/MedicalCaseWorkspaceViewModelTests.cs` | ~10 | Constructor signature changes |
| `PureLogic/MedicalCase/MedicalCaseWorkspaceViewModelTests.cs` | ~10 | Constructor signature changes |
| `PureLogic/Clinical/CardReaderPureTests.cs` | ~5 | Interface dependency changes |
| PatientSelectionViewModel tests | **0 (none exist)** | Need to create |

---

## 2. Target State

### Page 1: PatientSelectionView (Enhanced)
```
+----------------------------------------------+
| Header: "患者选择"                             |
| +----------+---------------------+-----------+|
| | CardReader|  PatientSelection   | Patient   ||
| | Status   |  Control (search +  | Detail    ||
| |          |  list)              | Preview   ||
| +----------+                     |           ||
| | Pending  |                     |           ||
| | Queue    |                     |           ||
| +----------+---------------------+-----------+|
| Footer: Status + "开始接诊" btn                |
+----------------------------------------------+
```

### Page 2: MedicalCaseWorkspaceView (Simplified)
```
+----------------------------------------------+
| BaseDetailContainer (100% width)              |
|   Header: Title + Patient Info                |
|   ViewContent (readonly mode)                 |
|   EditContent (edit mode)                     |
|   FooterContent (remark + actions)            |
+----------------------------------------------+
```

---

## 3. The Coupling Problem & Solution

### Problem
CardReaderViewModel and PendingQueueViewModel depend on:
- `IMedicalCaseWorkspaceContext` — provides MedicalCaseId, CurrentPatient, State
- `IWorkspaceHost` — provides SetBusy, ShowError, ShowConfirm, NotifyStateChanged

In PatientSelectionView, there's **no active medical case** — so these interfaces seem incompatible.

### Solution: Introduce IChildViewModelHost (generalized host interface)

**Option A (Recommended): Make PatientSelectionViewModel implement IWorkspaceHost + provide a "no-case" context**

The existing interfaces already support this:
- `IWorkspaceHost.SetBusy/ShowError/ShowSuccess/ShowConfirm` are pure UI utilities — any parent VM can provide these
- `IMedicalCaseWorkspaceContext.MedicalCaseId = Guid.Empty` and `CurrentPatient = null` when no case is active
- PendingQueue/CardReader already handle the "no active case" flow (they create new cases and navigate)

**Changes needed**:
1. PatientSelectionViewModel implements `IWorkspaceHost`
2. Create `PatientSelectionWorkspaceContext` (simple adapter implementing `IMedicalCaseWorkspaceContext` with empty state)
3. CardReader/PendingQueue child VMs check for `context.MedicalCaseId == Guid.Empty` to skip "suspend current case" logic

**Risk**: Low — the child VMs already have navigation flows for creating new cases. The "suspend current case" path is the only logic that assumes an active case exists, and it can be gated with a simple null/empty check.

---

## 4. File Modification List

### Files to Modify

| # | File | Change | Est. LOC |
|---|------|--------|----------|
| M1 | `ViewModels/Workspace/PendingQueueViewModel.cs` | Add null-guard for MedicalCaseId==Guid.Empty in SelectPendingCaseAsync (skip suspend logic) | ~5 |
| M2 | `ViewModels/Workspace/CardReaderViewModel.cs` | Add null-guard for context in NavigateToMedicalCaseForPatientAsync (skip suspend logic) | ~5 |
| M3 | `Views/MedicalCaseWorkspaceView.xaml` | Remove 2-column grid, remove left sidebar (CardReader/PatientInfoCard/PendingQueue), make BaseDetailContainer 100% width | -30 |
| M4 | `ViewModels/MedicalCaseWorkspaceViewModel.cs` | Remove PendingQueue + CardReader properties, remove constructor params (IPendingQueueManager, ICardReaderService, IPatientCardReaderIntegration), remove related wiring | -40 |
| M5 | `ViewModels/PatientSelectionViewModel.cs` | Add IWorkspaceHost implementation, add CardReader + PendingQueue child VMs, add constructor params | +80 |
| M6 | `Views/PatientSelectionView.xaml` | Redesign layout: add CardReader status panel + PendingQueue panel alongside existing PatientSelectionControl | +80 |
| M7 | `ClinicalModule.cs` | No changes needed (registrations already exist) | 0 |

### Files to Create

| # | File | Purpose | Est. LOC |
|---|------|---------|----------|
| C1 | `ViewModels/PatientSelectionWorkspaceContext.cs` | Adapter implementing IMedicalCaseWorkspaceContext with empty/null state for PatientSelectionViewModel | ~30 |

### Test Files to Modify

| # | File | Change |
|---|------|--------|
| T1 | `tests/.../Clinical/MedicalCaseWorkspaceViewModelTests.cs` | Remove CardReader/PendingQueue-related constructor params from CreateSut() |
| T2 | `tests/.../MedicalCase/MedicalCaseWorkspaceViewModelTests.cs` | Same as T1 |
| T3 | `tests/.../Clinical/CardReaderPureTests.cs` | Verify tests still pass (CardReaderVM interface unchanged) |

### Test Files to Create

| # | File | Purpose | Est. Tests |
|---|------|---------|-----------|
| T4 | `tests/.../Clinical/PatientSelectionViewModelTests.cs` | Test PatientSelectionVM with CardReader/PendingQueue child VMs | ~10 |
| T5 | `tests/.../Clinical/PendingQueueViewModelTests.cs` | Test PendingQueue in "no active case" mode | ~5 |
| T6 | `tests/.../Clinical/PatientSelectionWorkspaceContextTests.cs` | Test adapter returns empty/null state | ~3 |

---

## 5. Dependency Graph

```
                    M1 (PendingQueue null-guard)
                   /                              \
C1 (Context adapter)                               M4 (Remove from WorkspaceVM)
                   \                              /         |
                    M2 (CardReader null-guard)              M3 (Remove sidebar XAML)
                         |
                    M5 (Add to PatientSelectionVM)
                         |
                    M6 (Add UI to PatientSelectionView)
```

**Parallel tracks**:
- Track A: M1 + M2 (null-guards in child VMs) — can run in parallel
- Track B: C1 (context adapter) — independent
- Track C: M3 + M4 (remove from workspace) — M3 depends on M4 being planned but can execute together
- Track D: M5 + M6 (add to patient selection) — depends on M1, M2, C1 being complete

---

## 6. Atomic Commit Strategy

### Commit 1: `test: add PatientSelectionViewModel tests with CardReader/PendingQueue expectations`
**Files**: T4, T5, T6
**Rationale**: TDD — write failing tests first that define the expected behavior
**Verification**: `dotnet test tests/LYBT.Tests.Desktop/ --filter "PatientSelection"` — should FAIL (expected)

### Commit 2: `feat: add PatientSelectionWorkspaceContext adapter for child VM hosting`
**Files**: C1
**Rationale**: New class, no existing code changes, safe to commit independently
**Verification**: `dotnet build LYBTZYZS.sln` — should pass

### Commit 3: `refactor: add null-guards to PendingQueue/CardReader for no-active-case mode`
**Files**: M1, M2
**Rationale**: Make child VMs work in both contexts (with and without active case)
**Verification**: `dotnet test tests/LYBT.Tests.Desktop/` — ALL 760 tests should pass (backward compatible)

### Commit 4: `refactor: remove CardReader and PendingQueue from MedicalCaseWorkspaceViewModel`
**Files**: M3, M4, T1, T2
**Rationale**: Remove the sidebar from workspace view — this is the "breaking" change for the old layout
**Verification**: `dotnet test tests/LYBT.Tests.Desktop/` — all tests pass, `dotnet build LYBTZYZS.sln` — builds clean

### Commit 5: `feat: add CardReader and PendingQueue to PatientSelectionViewModel`
**Files**: M5
**Rationale**: Wire up the child VMs in the new parent
**Verification**: `dotnet test tests/LYBT.Tests.Desktop/ --filter "PatientSelection"` — T4/T5/T6 tests should now PASS

### Commit 6: `feat: redesign PatientSelectionView with CardReader and PendingQueue panels`
**Files**: M6
**Rationale**: UI layout change, separated from logic changes for easy rollback
**Verification**: Visual inspection + `dotnet build LYBTZYZS.sln` — builds clean

### Commit 7: `test: verify full test suite passes after two-page separation`
**Files**: None (verification only)
**Verification**: `dotnet test tests/LYBT.Tests.Desktop/` — all 760+ tests pass, `dotnet test tests/LYBT.Tests.Architecture/` — all 76 tests pass

---

## 7. TDD Test Plan

### Phase 1: Write Failing Tests (Commit 1)

#### T4: PatientSelectionViewModelTests.cs (~10 tests)

```
Test_Constructor_Creates_CardReader_ChildVM
Test_Constructor_Creates_PendingQueue_ChildVM
Test_CardReader_Property_IsNotNull_After_Construction
Test_PendingQueue_Property_IsNotNull_After_Construction
Test_Implements_IWorkspaceHost_SetBusy
Test_Implements_IWorkspaceHost_ShowErrorAsync
Test_Implements_IWorkspaceHost_ShowConfirmAsync
Test_CardReader_CanInitialize_WithoutActiveMedicalCase
Test_PendingQueue_CanRefresh_WithoutActiveMedicalCase
Test_Existing_StartMedicalCaseAsync_StillWorks
```

#### T5: PendingQueueViewModelTests.cs (~5 tests)

```
Test_SelectPendingCase_WithNoActiveCaseId_SkipsSuspend_NavigatesDirectly
Test_RefreshQueue_WorksWithEmptyContext
Test_HasNoPendingCases_ReturnsTrue_WhenQueueEmpty
Test_Queue_IsObservableCollection
Test_Constructor_DoesNotThrow_WithNullContext
```

#### T6: PatientSelectionWorkspaceContextTests.cs (~3 tests)

```
Test_MedicalCaseId_ReturnsGuidEmpty
Test_CurrentPatient_ReturnsNull
Test_State_ReturnsDefaultWorkspaceState
```

### Phase 2: Implement to Make Tests Pass (Commits 2-5)

### Phase 3: Full Regression (Commit 7)
- Run ALL desktop tests: `dotnet test tests/LYBT.Tests.Desktop/`
- Run ALL architecture tests: `dotnet test tests/LYBT.Tests.Architecture/`
- Manual smoke test: launch app, navigate Clinical Home → Patient Selection → start case → workspace

---

## 8. Risk Assessment

| # | Risk | Likelihood | Impact | Mitigation |
|---|------|-----------|--------|-----------|
| R1 | CardReader auto-read triggers during PatientSelection with no active case | Medium | Low | Null-guard in M1/M2 handles this; auto-read would find patient and auto-select |
| R2 | PendingQueue "suspend current case" crashes with Guid.Empty | High | High | M1 adds explicit guard: `if (context.MedicalCaseId == Guid.Empty) skip suspend` |
| R3 | Existing MedicalCaseWorkspaceViewModel tests break due to constructor signature change | Certain | Medium | T1/T2 update test constructors in same commit as M4 |
| R4 | PatientInfoCardControl bindings break (CurrentPatientDisplayModel removed) | Certain | Low | M3 removes the control from XAML, so no dangling bindings |
| R5 | PendingQueueView (standalone) conflicts with embedded PendingQueueControl | Low | Low | PendingQueueView is for navigation target (ClinicalHome card "挂号队列"); PendingQueueControl is the embedded version in PatientSelectionView — different usage |
| R6 | Navigation parameters change when going from PatientSelection → Workspace | Low | Medium | NavigateToMedicalCase method stays in PatientSelectionVM unchanged |
| R7 | Architecture tests fail (unexpected dependency direction) | Low | Medium | Verify with `dotnet test tests/LYBT.Tests.Architecture/` after each commit |

---

## 9. Implementation Sequence (Ultrawork Execution)

### Wave 1 (Parallel — 2 agents)
| Agent | Task | Commit |
|-------|------|--------|
| Agent A | Write T4 + T5 + T6 tests (PatientSelection, PendingQueue no-case, Context adapter) | Commit 1 |
| Agent B | Create C1 (PatientSelectionWorkspaceContext adapter) | Commit 2 |

### Wave 2 (Parallel — 2 agents)
| Agent | Task | Commit |
|-------|------|--------|
| Agent A | M1 + M2: Add null-guards to PendingQueue + CardReader child VMs | Commit 3 |
| Agent B | M3 + M4 + T1 + T2: Remove sidebar from workspace (XAML + VM + test updates) | Commit 4 |

### Wave 3 (Sequential — 1 agent)
| Agent | Task | Commit |
|-------|------|--------|
| Agent A | M5: Add CardReader + PendingQueue to PatientSelectionViewModel | Commit 5 |

### Wave 4 (Sequential — 1 agent)
| Agent | Task | Commit |
|-------|------|--------|
| Agent A | M6: Redesign PatientSelectionView.xaml layout | Commit 6 |

### Wave 5 (Verification — 1 agent)
| Agent | Task | Commit |
|-------|------|--------|
| Agent A | Full regression: `dotnet test` all projects | Commit 7 |

---

## 10. PatientSelectionView Target Layout (Detailed)

```xaml
<!-- Target Layout: 3-column with sidebar -->
<Grid>
    <Grid.RowDefinitions>
        <RowDefinition Height="60"/>     <!-- Header -->
        <RowDefinition Height="*"/>      <!-- Main Content -->
        <RowDefinition Height="80"/>     <!-- Footer -->
    </Grid.RowDefinitions>

    <!-- Header: unchanged -->

    <!-- Main Content: 3 columns -->
    <Grid Grid.Row="1">
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width="280"/>   <!-- Left: CardReader + PendingQueue -->
            <ColumnDefinition Width="*"/>     <!-- Center: PatientSelectionControl -->
            <ColumnDefinition Width="300"/>   <!-- Right: Patient Detail Preview -->
        </Grid.ColumnDefinitions>

        <!-- Left Panel -->
        <StackPanel Grid.Column="0" Margin="12">
            <!-- CardReader Status -->
            <infraControls:CardReaderStatusControl ... />
            <!-- PendingQueue -->
            <infraControls:PendingQueueControl ... />
        </StackPanel>

        <!-- Center: Existing PatientSelectionControl -->
        <patientControls:PatientSelectionControl Grid.Column="1" ... />

        <!-- Right: Patient Detail Preview -->
        <infraControls:PatientInfoCardControl Grid.Column="2" ... />
    </Grid>

    <!-- Footer: unchanged -->
</Grid>
```

---

## 11. Rollback Strategy

Each commit is independently revertable:
- **Commit 6** (XAML layout): `git revert` restores old PatientSelectionView layout
- **Commit 5** (VM wiring): `git revert` removes child VMs from PatientSelectionVM
- **Commit 4** (workspace removal): `git revert` restores sidebar to workspace
- **Commit 3** (null-guards): `git revert` removes null-guards (safe, they're additive)
- **Commit 2** (adapter): `git revert` removes adapter class
- **Commit 1** (tests): `git revert` removes new tests

For a complete rollback: `git revert HEAD~6..HEAD` (revert all 6 implementation commits)

---

## Appendix A: Constructor Signature Changes

### MedicalCaseWorkspaceViewModel — BEFORE
```csharp
public MedicalCaseWorkspaceViewModel(
    IViewModelServices services,
    IMedicalCaseService medicalCaseService,
    INavigationCoordinator navigationCoordinator,
    IActiveConsultationService activeConsultationService,
    IPendingQueueManager pendingQueueManager,           // REMOVE
    PrescriptionPrintHandler printHandler,
    ICardReaderService cardReaderService,                // REMOVE
    IPatientCardReaderIntegration patientIntegration,    // REMOVE
    IDialogService? dialogService = null)
```

### MedicalCaseWorkspaceViewModel — AFTER
```csharp
public MedicalCaseWorkspaceViewModel(
    IViewModelServices services,
    IMedicalCaseService medicalCaseService,
    INavigationCoordinator navigationCoordinator,
    IActiveConsultationService activeConsultationService,
    PrescriptionPrintHandler printHandler,
    IDialogService? dialogService = null)
```

### PatientSelectionViewModel — BEFORE
```csharp
public PatientSelectionViewModel(
    IViewModelServices services,
    IPatientApi patientApi,
    IMedicalCaseApi medicalCaseApi,
    IMedicalCaseService medicalCaseService,
    INavigationCoordinator navigationCoordinator)
```

### PatientSelectionViewModel — AFTER
```csharp
public PatientSelectionViewModel(
    IViewModelServices services,
    IPatientApi patientApi,
    IMedicalCaseApi medicalCaseApi,
    IMedicalCaseService medicalCaseService,
    INavigationCoordinator navigationCoordinator,
    ICardReaderService cardReaderService,                // ADD
    IPatientCardReaderIntegration patientIntegration,    // ADD
    IPendingQueueManager pendingQueueManager)            // ADD
```

---

## Appendix B: Files Reference

### Source Files
```
src/Client/Desktop/Roles/LYBT.Desktop.Clinical/
├── ClinicalModule.cs                                          (no change)
├── ViewModels/
│   ├── ClinicalHomeViewModel.cs                               (no change)
│   ├── MedicalCaseWorkspaceViewModel.cs                       (M4: remove child VMs)
│   ├── PatientSelectionViewModel.cs                           (M5: add child VMs)
│   ├── PatientSelectionWorkspaceContext.cs                     (C1: NEW)
│   └── Workspace/
│       ├── CardReaderViewModel.cs                             (M2: add null-guard)
│       ├── ConsultationEditorViewModel.cs                     (no change)
│       ├── MedicalCaseCommandsViewModel.cs                    (no change)
│       ├── PendingQueueViewModel.cs                           (M1: add null-guard)
│       └── PrescriptionEditorViewModel.cs                     (no change)
├── Views/
│   ├── ClinicalHomeView.xaml                                  (no change)
│   ├── MedicalCaseWorkspaceView.xaml                          (M3: remove sidebar)
│   ├── PatientSelectionView.xaml                              (M6: redesign layout)
│   └── PendingQueueView.xaml                                  (no change)
```

### Test Files
```
tests/LYBT.Tests.Desktop/PureLogic/Clinical/
├── MedicalCaseWorkspaceViewModelTests.cs                      (T1: update constructor)
├── CardReaderPureTests.cs                                     (T3: verify unchanged)
├── PatientSelectionViewModelTests.cs                          (T4: NEW)
├── PendingQueueViewModelTests.cs                              (T5: NEW)
└── PatientSelectionWorkspaceContextTests.cs                   (T6: NEW)

tests/LYBT.Tests.Desktop/PureLogic/MedicalCase/
└── MedicalCaseWorkspaceViewModelTests.cs                      (T2: update constructor)
```
