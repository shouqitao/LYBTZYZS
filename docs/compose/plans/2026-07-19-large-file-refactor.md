# Large File Refactor Implementation Plan

> **For agentic workers:** Use compose:subagent or compose:execute to implement.

**Goal:** Refactor 5 large files (3500+ lines) without changing functionality.

## Execution Order

**Parallel Batch 1** (independent):
- Task 1: HttpClientApiClient helper extraction
- Task 2: MasterDetailViewModelBase decomposition
- Task 3: PrescriptionPrintService cleanup

**Sequential Batch 2** (depends on Task 2):
- Task 4: MedicalCaseCommandsVM — IMedicalCaseDataProvider interface
- Task 5: MedicalCaseWorkspaceVM — state/navigation extraction

## Task 1: HttpClientApiClient Helpers

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/HttpClientApiClient.cs`

**Steps:**
1. Extract `QueryStringBuilder` static helper (conditional params → URL)
2. Add `GetPagedAndWrapAsync<T>` for client-side pagination
3. Add `SendAndWrapAsync` unified HTTP method execution
4. Replace all 5 client-side pagination methods with `GetPagedAndWrapAsync`
5. Replace Post/Put boilerplate with `SendAndWrapAsync`
6. Build and verify: 0 errors

## Task 2: MasterDetailViewModelBase Decomposition

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/MasterDetailPropertySync.cs`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Composition/PaginationCommandGroup.cs`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/Composition/CrudCommandCoordinator.cs`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/ViewModels/MasterDetailViewModelBase.cs`

**Steps:**
1. Create `MasterDetailPropertySync<T,D>` — extract delegated properties (B-F) + event handlers (I)
2. Create `PaginationCommandGroup` — extract pagination commands (J subset)
3. Create `CrudCommandCoordinator<T,D>` — extract CRUD/batch/restore commands (K+L)
4. Slim down `MasterDetailViewModelBase` to constructor + abstract methods + lifecycle
5. Verify 5 subclasses compile without changes
6. Build and verify: 0 errors

## Task 3: PrescriptionPrintService Cleanup

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Printing/Services/PrescriptionPrintService.cs`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Printing/Templates/PrintSettingsPanel.xaml`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Printing/Templates/PrintSettingsPanel.xaml.cs`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Printing/Templates/PreviewWindow.xaml`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Printing/Templates/PreviewWindow.xaml.cs`

**Steps:**
1. Extract `CreatePageFromTemplate` helper (dedup page creation)
2. Replace `CloneModelWithItems` with expression-based copy
3. Create `PrintSettingsPanel.xaml` UserControl (replace 128-line code-behind)
4. Create `PreviewWindow.xaml` (replace 36-line programmatic Window)
5. Cache `LocalPrintServer` as field
6. Build and verify: 0 errors

## Task 4: MedicalCaseCommandsVM Interface Extraction

**Files:**
- Create: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/Interfaces/IMedicalCaseDataProvider.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/ViewModels/Workspace/MedicalCaseCommandsViewModel.cs`
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/MedicalCaseWorkspaceViewModel.cs`

**Steps:**
1. Define `IMedicalCaseDataProvider` interface (11 methods)
2. Refactor CommandsVM constructor to accept interface
3. Implement interface on MedicalCaseWorkspaceVM
4. Remove 11 Func<> delegate properties
5. Build and verify: 0 errors

## Task 5: MedicalCaseWorkspaceVM State/Navigation Extraction

**Files:**
- Create: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/Workspace/WorkspaceStateManager.cs`
- Create: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/Workspace/WorkspaceNavigationHandler.cs`
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ViewModels/MedicalCaseWorkspaceViewModel.cs`

**Steps:**
1. Create `WorkspaceStateManager` — extract dual FSM + 5-step workflow
2. Create `WorkspaceNavigationHandler` — extract back/leave + management save
3. Slim down MedicalCaseWorkspaceVM
4. Build and verify: 0 errors
