# Post Two-Page Separation: Comprehensive TODO Plan

> **Created**: 2026-04-11
> **Status**: Ready for Execution
> **Prerequisite**: Two-Page Separation Refactoring (Wave 1-5) COMPLETED — 713 pass, 1 expected fail

---

## Table of Contents

1. [Executive Summary](#1-executive-summary)
2. [P0 — Critical Fixes](#2-p0--critical-fixes)
3. [P1 — Important Improvements](#3-p1--important-improvements)
4. [P2 — Nice-to-Have Features](#4-p2--nice-to-have-features)
5. [Dependency Graph](#5-dependency-graph)
6. [Execution Waves](#6-execution-waves)
7. [Risk Assessment](#7-risk-assessment)
8. [Atomic Commit Strategy](#8-atomic-commit-strategy)
9. [Verification Plan](#9-verification-plan)

---

## 1. Executive Summary

After the Two-Page Separation Refactoring (PatientSelectionView + MedicalCaseWorkspaceView), the following work items remain. They are organized by priority and designed for parallel ultrawork execution.

| Priority | Count | Est. Total Effort |
|----------|-------|-------------------|
| P0 Critical | 3 items | ~3 hours |
| P1 Important | 5 items | ~8 hours |
| P2 Nice-to-Have | 6 items | ~12 hours |

---

## 2. P0 — Critical Fixes

These must be resolved before any P1/P2 work. They represent broken functionality or data loss.

### P0-1: Fix Failing PendingQueue Test

**Problem**: `SelectPendingCaseAsync_WithNoActiveMedicalCaseId_SkipsSuspend_NavigatesDirectly` expects `NavigateTo` to be called, but `HandleSuspendedCaseAsync` returns early when `CommonDialogService` is null (not mocked in the test).

**Root Cause**: The test arranges a `Suspended` target case but doesn't mock `IWorkspaceHost.CommonDialogService`. In `HandleSuspendedCaseAsync` (line 180-222), when `dialogService == null`, the method returns without navigating.

**Fix**: Mock `CommonDialogService` on `IWorkspaceHost` to return `true` from `ShowConfirmAsync`, so the suspended-case dialog flow completes and `NavigateToExistingMedicalCaseAsync` calls `NavigateTo`.

| Attribute | Value |
|-----------|-------|
| Effort | 15 min |
| Files | `tests/LYBT.Tests.Desktop/PureLogic/Clinical/PendingQueueViewModelTests.cs` |
| Dependencies | None |
| Risk | Low — test-only change |
| TDD | RED→GREEN: Fix the mock setup, verify test passes |

**Test Fix**:
```csharp
// In constructor, add:
var dialogService = Substitute.For<ICommonDialogService>();
dialogService.ShowConfirmAsync(Arg.Any<string>(), Arg.Any<string>())
    .Returns(Task.FromResult(true));
_host.CommonDialogService.Returns(dialogService);
```

**Verification**:
- `dotnet test tests/LYBT.Tests.Desktop/ --filter "PendingQueueViewModelTests"` → all 5 pass
- Full suite: `dotnet test tests/LYBT.Tests.Desktop/` → 713+ pass, 0 fail

---

### P0-2: Fix LoginView HasMessage Bug

**Problem**: `LoginViewModel.HasMessage` computed property never fires `PropertyChanged`. Users cannot see login error/status messages.

**Root Cause**: `CoreViewModelBase._statusMessage` field has NO `[NotifyPropertyChangedFor(nameof(HasMessage))]` attribute. `_errorMessage` only notifies `HasError`, not `HasMessage`.

**Fix**: Make `LoginViewModel` a partial class and add `On_StatusMessageChanged` / `On_ErrorMessageChanged` partial methods that call `OnPropertyChanged(nameof(HasMessage))`. OR add `[NotifyPropertyChangedFor]` to the base class fields.

| Attribute | Value |
|-----------|-------|
| Effort | 30 min |
| Files | `src/Client/Desktop/Core/LYBT.Desktop.Foundation/ViewModels/CoreViewModelBase.cs` (or `LoginViewModel.cs`) |
| Dependencies | None |
| Risk | Medium — base class change could affect all VMs; prefer LoginViewModel-level fix |
| TDD | Write test: set StatusMessage → assert HasMessage PropertyChanged fires |

**Verification**:
- New unit test: `LoginViewModel_StatusMessage_Fires_HasMessage_PropertyChanged`
- Manual: Login with invalid credentials → error message visible

---

### P0-3: Fix HerbInputDto Missing Properties Field

**Problem**: `HerbInputDto` is missing the `Properties` field. `HerbMapper` has `[MapperIgnoreSource]` on it, causing API-layer data loss when creating/updating herbs.

**Root Cause**: Mapper ignores Properties field that should be mapped.

**Fix**: 
1. Add `Properties` field to `HerbInputDto`
2. Remove `[MapperIgnoreSource(nameof(Properties))]` from `HerbMapper`

| Attribute | Value |
|-----------|-------|
| Effort | 30 min |
| Files | `src/Shared/LYBT.Shared.Models/DTOs/Herbs/HerbInputDto.cs`, `src/Server/Services/LYBT.WebAPI/Mappers/HerbMapper.cs` |
| Dependencies | None |
| Risk | Medium — API contract change, need to verify both Desktop and Server |
| TDD | Write test: Create herb with Properties → GET herb → Properties preserved |

**Verification**:
- `dotnet test tests/LYBT.Tests.Server/ --filter "Herb"` → all pass
- `dotnet test tests/LYBT.Tests.Desktop/ --filter "Herb"` → all pass
- Manual: Create herb with properties → verify properties saved and returned

---

## 3. P1 — Important Improvements

These improve clinical workflow reliability and data integrity.

### P1-1: Fix IsEnabled Scope Bug in MedicalCaseWorkspace

**Problem**: `IsEnabled` binding on MedicalCaseEditControl may disable the entire control instead of individual fields, preventing user interaction in unexpected ways.

**Root Cause**: Need to investigate the exact scope of `IsEnabled` binding in `MedicalCaseWorkspaceView.xaml` vs `MedicalCaseEditControl.xaml`.

| Attribute | Value |
|-----------|-------|
| Effort | 1 hour |
| Files | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/MedicalCaseWorkspaceView.xaml`, `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml` |
| Dependencies | None |
| Risk | Medium — UI behavior change |
| TDD | Write test verifying individual field enable/disable vs whole-control disable |

**Verification**:
- Manual: In workspace, verify individual fields can be toggled while others remain active
- Architecture test: Verify no whole-control IsEnabled binding

---

### P1-2: Fix EnterEditMode Binding Error

**Problem**: `EnterEditMode` command binding may throw or silently fail in certain navigation states.

**Root Cause**: Need to investigate binding path from XAML to ViewModel command.

| Attribute | Value |
|-----------|-------|
| Effort | 1 hour |
| Files | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/MedicalCaseWorkspaceView.xaml`, `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/MedicalCaseWorkspaceViewModel.cs` |
| Dependencies | None |
| Risk | Low — binding fix |
| TDD | Write test: Navigate to workspace in ReadOnly → trigger EnterEditMode → verify state changes to Editing |

**Verification**:
- `dotnet test tests/LYBT.Tests.Desktop/ --filter "MedicalCaseWorkspace"` → all pass
- Manual: Open case in ReadOnly → click Edit → transitions to Editing mode

---

### P1-3: Unify Remark Data Source

**Problem**: Remark/notes field may have inconsistent data sources between Consultation and MedicalCase levels.

**Root Cause**: Need to audit where Remark is bound — ConsultationItem vs MedicalCase aggregate.

| Attribute | Value |
|-----------|-------|
| Effort | 2 hours |
| Files | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml`, `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Models/ConsultationItem.cs`, relevant ViewModels |
| Dependencies | P1-1 (IsEnabled scope must be correct first) |
| Risk | Medium — data binding change |
| TDD | Write test: Set Remark on Consultation → verify displayed in UI model |

**Verification**:
- Unit test: Remark data flows correctly from DTO → Item → Display
- Manual: Edit remark, save, reload → remark preserved

---

### P1-4: Add Validation Error Display

**Problem**: Validation errors from ViewModel/Model validation are not displayed to the user in the MedicalCaseWorkspace.

**Root Cause**: No `Validation.ErrorTemplate` or `INotifyDataErrorInfo` integration in MedicalCaseEditControl.

| Attribute | Value |
|-----------|-------|
| Effort | 2 hours |
| Files | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml`, `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Styles/` (shared error styles) |
| Dependencies | P1-1 (IsEnabled scope must work correctly) |
| Risk | Low — additive change |
| TDD | Write test: Set invalid data → verify validation error collection is populated |

**Verification**:
- Unit test: Model validation returns errors for invalid input
- Manual: Leave required field empty → red border + error message visible

---

### P1-5: Add UserEditControl Missing Remark Field

**Problem**: `UserEditControl` is missing the Remark/Notes field in the UI, even though the DTO/model supports it.

**Root Cause**: Missing DependencyProperty + XAML binding in the UserEditControl.

| Attribute | Value |
|-----------|-------|
| Effort | 1 hour |
| Files | `src/Client/Desktop/Modules/LYBT.Desktop.Users/Controls/UserEditControl.xaml`, `src/Client/Desktop/Modules/LYBT.Desktop.Users/Controls/UserEditControl.xaml.cs` |
| Dependencies | None |
| Risk | Low — additive UI change |
| TDD | Write test: Set Remark on UserEditControl → verify DependencyProperty and binding |

**Verification**:
- Manual: Open user edit → Remark field visible and editable
- Save remark → reload → remark preserved

---

## 4. P2 — Nice-to-Have Features

These improve clinical workflow efficiency but are not blocking.

### P2-1: Diagnosis Area Grouping (望闻问切)

**Description**: Group diagnostic fields by the four TCM examination methods (望-inspection, 闻-auscultation, 问-inquiry, 切-palpation) with visual section headers.

| Attribute | Value |
|-----------|-------|
| Effort | 2 hours |
| Files | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml` |
| Dependencies | P1-1, P1-3 (layout and data must be stable first) |

---

### P2-2: Prescription Decision Guidance

**Description**: Add visual cues to help doctors decide on prescription actions (e.g., "add formula", "copy from history", "start from scratch") with contextual tooltips.

| Attribute | Value |
|-----------|-------|
| Effort | 2 hours |
| Files | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/PrescriptionEditorPanel.xaml` |
| Dependencies | None |

---

### P2-3: Bottom Action Bar

**Description**: Add a persistent bottom action bar to MedicalCaseWorkspaceView with primary actions (Save, Complete, Print, Suspend) always visible.

| Attribute | Value |
|-----------|-------|
| Effort | 2 hours |
| Files | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/MedicalCaseWorkspaceView.xaml` |
| Dependencies | P1-2 (command bindings must work first) |

---

### P2-4: Real-time Price Calculation

**Description**: Show running total price of prescription herbs as they are added/modified.

| Attribute | Value |
|-----------|-------|
| Effort | 2 hours |
| Files | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/PrescriptionEditorViewModel.cs`, related XAML |
| Dependencies | None |

---

### P2-5: Completeness Check Indicator

**Description**: Visual indicator showing which required fields are filled vs empty, helping doctors complete cases faster.

| Attribute | Value |
|-----------|-------|
| Effort | 2 hours |
| Files | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml`, related ViewModel |
| Dependencies | P1-4 (validation infrastructure must exist) |

---

### P2-6: Common Term Quick Selection

**Description**: Dropdown/autocomplete for common TCM diagnostic terms (tongue coating descriptions, pulse types, etc.) to speed up data entry.

| Attribute | Value |
|-----------|-------|
| Effort | 2 hours |
| Files | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml`, new data source files |
| Dependencies | P2-1 (diagnostic grouping should be done first) |

---

## 5. Dependency Graph

```
P0-1 (Test Fix)         ──→ independent
P0-2 (HasMessage)       ──→ independent
P0-3 (HerbDto)          ──→ independent

P1-1 (IsEnabled)        ──→ independent
P1-2 (EnterEditMode)    ──→ independent
P1-3 (Remark Unify)     ──→ depends on P1-1
P1-4 (Validation)       ──→ depends on P1-1
P1-5 (User Remark)      ──→ independent

P2-1 (望闻问切)          ──→ depends on P1-1, P1-3
P2-2 (Rx Guidance)      ──→ independent
P2-3 (Action Bar)       ──→ depends on P1-2
P2-4 (Price Calc)       ──→ independent
P2-5 (Completeness)     ──→ depends on P1-4
P2-6 (Quick Terms)      ──→ depends on P2-1
```

### Parallelization Groups

| Group | Items | Can Run in Parallel |
|-------|-------|---------------------|
| A | P0-1, P0-2, P0-3 | Yes — all independent |
| B | P1-1, P1-2, P1-5 | Yes — all independent |
| C | P1-3, P1-4 | Yes with each other, but after P1-1 |
| D | P2-2, P2-4 | Yes — independent of everything except P0 |
| E | P2-1 | After P1-1 + P1-3 |
| F | P2-3 | After P1-2 |
| G | P2-5 | After P1-4 |
| H | P2-6 | After P2-1 |

---

## 6. Execution Waves

### Wave 1: P0 Critical Fixes (Parallel — ~1 hour)

| Agent | Task | Files |
|-------|------|-------|
| Agent A | P0-1: Fix PendingQueue test mock | `PendingQueueViewModelTests.cs` |
| Agent B | P0-2: Fix LoginView HasMessage | `CoreViewModelBase.cs` or `LoginViewModel.cs` |
| Agent C | P0-3: Fix HerbInputDto Properties | `HerbInputDto.cs`, `HerbMapper.cs` |

**Gate**: All tests pass (`dotnet test tests/LYBT.Tests.Desktop/` + `dotnet test tests/LYBT.Tests.Server/`)

### Wave 2: P1 Independent Fixes (Parallel — ~2 hours)

| Agent | Task | Files |
|-------|------|-------|
| Agent D | P1-1: Fix IsEnabled scope | `MedicalCaseWorkspaceView.xaml`, `MedicalCaseEditControl.xaml` |
| Agent E | P1-2: Fix EnterEditMode binding | `MedicalCaseWorkspaceView.xaml`, `MedicalCaseWorkspaceViewModel.cs` |
| Agent F | P1-5: Add User Remark field | `UserEditControl.xaml`, `UserEditControl.xaml.cs` |

**Gate**: `dotnet test tests/LYBT.Tests.Desktop/` → all pass + manual UI verification

### Wave 3: P1 Dependent Fixes (Parallel — ~2 hours)

| Agent | Task | Files |
|-------|------|-------|
| Agent G | P1-3: Unify Remark data source | `MedicalCaseEditControl.xaml`, `ConsultationItem.cs` |
| Agent H | P1-4: Add validation error display | `MedicalCaseEditControl.xaml`, shared styles |

**Gate**: `dotnet test tests/LYBT.Tests.Desktop/` → all pass + manual UI verification

### Wave 4: P2 Features (Parallel — ~4 hours)

| Agent | Task |
|-------|------|
| Agent I | P2-1: 望闻问切 grouping |
| Agent J | P2-2: Prescription guidance |
| Agent K | P2-3: Bottom action bar |
| Agent L | P2-4: Real-time price calculation |

### Wave 5: P2 Dependent Features (~4 hours)

| Agent | Task |
|-------|------|
| Agent M | P2-5: Completeness indicator |
| Agent N | P2-6: Common term quick selection |

---

## 7. Risk Assessment

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|------------|
| CoreViewModelBase change breaks other VMs | High | Medium | Prefer LoginViewModel-level fix; run full test suite |
| HerbInputDto API change breaks clients | High | Low | DTO is additive (new field); existing clients unaffected |
| IsEnabled scope change breaks existing UI | Medium | Medium | Test in both Clinical and Management modes |
| MedicalCaseEditControl XAML changes conflict | Medium | High | Serialize Wave 3 and Wave 4 XAML changes on same file |
| Validation error styles don't match app theme | Low | Medium | Reuse existing error styles from Infrastructure |

### File Conflict Matrix

Files modified by multiple tasks (must be serialized or carefully merged):

| File | Tasks | Resolution |
|------|-------|------------|
| `MedicalCaseEditControl.xaml` | P1-1, P1-3, P1-4, P2-1, P2-5, P2-6 | Serialize: P1-1 → P1-3 → P1-4 → P2-1 → P2-5 → P2-6 |
| `MedicalCaseWorkspaceView.xaml` | P1-1, P1-2, P2-3 | Serialize: P1-1 → P1-2 → P2-3 |

---

## 8. Atomic Commit Strategy

Each task = one atomic commit. Commit message format:

```
fix(module): description — closes #issue
test(module): description
feat(module): description
```

| Task | Commit Message |
|------|---------------|
| P0-1 | `fix(test): mock CommonDialogService in PendingQueueViewModelTests` |
| P0-2 | `fix(auth): fire HasMessage PropertyChanged when StatusMessage changes` |
| P0-3 | `fix(herbs): add Properties field to HerbInputDto, remove MapperIgnoreSource` |
| P1-1 | `fix(medical-case): scope IsEnabled to individual fields, not whole control` |
| P1-2 | `fix(clinical): fix EnterEditMode command binding in workspace view` |
| P1-3 | `refactor(medical-case): unify Remark data source to ConsultationItem` |
| P1-4 | `feat(medical-case): add validation error display with INotifyDataErrorInfo` |
| P1-5 | `feat(users): add Remark field to UserEditControl` |
| P2-1 | `feat(medical-case): group diagnosis fields by 望闻问切 examination methods` |
| P2-2 | `feat(medical-case): add prescription decision guidance tooltips` |
| P2-3 | `feat(clinical): add persistent bottom action bar to workspace` |
| P2-4 | `feat(medical-case): show real-time prescription price total` |
| P2-5 | `feat(medical-case): add completeness check indicator` |
| P2-6 | `feat(medical-case): add common TCM term quick selection dropdowns` |

---

## 9. Verification Plan

### Per-Task Verification

| Task | Automated | Manual |
|------|-----------|--------|
| P0-1 | `dotnet test --filter "PendingQueueViewModelTests"` → 5/5 pass | N/A |
| P0-2 | New test: `HasMessage_PropertyChanged` | Login with bad credentials → error shows |
| P0-3 | `dotnet test --filter "Herb"` (server + desktop) | Create herb with properties → verify round-trip |
| P1-1 | Existing workspace tests pass | Individual field enable/disable works |
| P1-2 | Existing workspace tests pass | ReadOnly → click Edit → Editing mode |
| P1-3 | New test: Remark data flow | Edit remark → save → reload → preserved |
| P1-4 | New test: Validation error collection | Empty required field → red border visible |
| P1-5 | Existing user tests + new DP test | User edit → Remark field visible |
| P2-* | Existing tests still pass | Visual inspection of new UI elements |

### Full Regression Gates

After each wave:

```powershell
# Desktop tests (760+ tests)
dotnet test tests/LYBT.Tests.Desktop/

# Architecture guards (76 tests)
dotnet test tests/LYBT.Tests.Architecture/

# Server tests (if API changes in P0-3)
dotnet test tests/LYBT.Tests.Server/ --filter "Herb"
```

### Final Acceptance

- [ ] All 2021+ tests pass (0 failures)
- [ ] `dotnet build LYBTZYZS.sln` succeeds with 0 errors
- [ ] Manual smoke test: Full clinical workflow (login → select patient → create case → diagnose → prescribe → complete)
- [ ] No new compiler warnings in modified files

---

## Appendix: Key File Locations

| Category | File | Lines |
|----------|------|-------|
| Test | `tests/LYBT.Tests.Desktop/PureLogic/Clinical/PendingQueueViewModelTests.cs` | 113 |
| ViewModel | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/MedicalCaseWorkspaceViewModel.cs` | ~650 |
| ViewModel | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/PatientSelectionViewModel.cs` | ~484 |
| ViewModel | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/Workspace/PendingQueueViewModel.cs` | ~341 |
| ViewModel | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/Workspace/CardReaderViewModel.cs` | ~457 |
| View | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/MedicalCaseWorkspaceView.xaml` | ~203 |
| View | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/PatientSelectionView.xaml` | ~189 |
| Control | `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Controls/MedicalCaseEditControl.xaml` | Large |
| DI | `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ClinicalModule.cs` | 45 |
| Base VM | `src/Client/Desktop/Core/LYBT.Desktop.Foundation/ViewModels/CoreViewModelBase.cs` | Medium |
| DTO | `src/Shared/LYBT.Shared.Models/DTOs/Herbs/HerbInputDto.cs` | Small |
| Mapper | `src/Server/Services/LYBT.WebAPI/Mappers/HerbMapper.cs` | Small |
