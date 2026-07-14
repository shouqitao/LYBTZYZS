# View Design & Cleanup Implementation Plan

> [!NOTE]
> This document may not reflect the current implementation.
> See the final report for up-to-date state:
> [Final Report](../reports/view-design-cleanup.md)

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Design a complete view architecture for the Desktop WPF application and remove all unused/dead views.

**Architecture:** Prism DryIoc region-based navigation. Roles (Admin/Clinical/Receptionist/Sysadmin) define workspace layouts. Modules (Patients/Herbs/Formula/MedicalCase/Registration/Users/Reports) provide reusable Controls. Shell provides shared dialogs and global navigation.

**Tech Stack:** .NET 8, WPF, Prism.DryIoc, CommunityToolkit.Mvvm, MaterialDesignInXAML

## View Architecture Overview

### Navigation Flow

```
Login (LoginRegion)
  -> LoginView -> NavigateToRoleHomeAsync
     ├── Admin: AdminHomeView
     ├── Doctor: ClinicalWorkspaceView (NOT ClinicalHomeView)
     ├── Receptionist: ReceptionistHomeView
     └── Sysadmin: SysadminHomeView
```

### View Categories

| Category | Count | Purpose |
|----------|-------|---------|
| Shell | 6 | MainWindow, AccountSettings, 3 dialogs |
| Role Home Dashboards | 4 | Admin/Clinical/Receptionist/Sysadmin home |
| Role Workspaces | 1 | ClinicalWorkspaceView (integrated patient+consultation) |
| Role Management Wrappers | 9 | Thin wrappers embedding module MasterDetailControls |
| Business Module Views | 2 | RegistrationListView, MedicalCaseMasterDetailView |
| Business Module Controls | 16 | MasterDetail, View, Edit controls |
| Dialogs | 7 | FormulaImport, HistoryCopy, UnsavedChanges, etc. |
| Core UI Controls | 20+ | BreadcrumbBar, StatusBadge, LoadingOverlay, etc. |

### Active Views by Role

| Role | Home | Navigation Targets |
|------|------|-------------------|
| Admin | AdminHomeView | Patient/MedicalCase/Herb/Formula/User Management, SystemSettings, ReportsHome |
| Doctor | ClinicalWorkspaceView | PatientSelection -> MedicalCaseWorkspace -> MedicalCaseMasterDetail |
| Receptionist | ReceptionistHomeView | PatientManagement, RegistrationList |
| Sysadmin | SysadminHomeView | LogLevelControl |

---

## Global Constraints

- Prism DryIoc region-based navigation only
- CommunityToolkit.Mvvm `[ObservableProperty]`/`[RelayCommand]` — no Prism BindableBase
- MDIX built-in styles — no custom ControlTemplate for standard controls
- Module Controls embedded in Role Views via Prism region injection
- All Views must have corresponding ViewModels
- Navigation via `INavigationCoordinator.NavigateTo(ViewNames.*)` only

---

### Task 1: Remove Unused Navigation Controls (Phase 2.1 Dead Code)

**Covers:** Cleanup of dead Phase 2.1 navigation infrastructure

**Files:**
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Navigation/BreadcrumbControl.xaml`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Navigation/BreadcrumbControl.xaml.cs`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Navigation/NavigationSuggestionsPanel.xaml`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Navigation/NavigationSuggestionsPanel.xaml.cs`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Navigation/NavigationHistoryPanel.xaml`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Navigation/NavigationHistoryPanel.xaml.cs`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/NavigationSuggestionsPanelStyles.xaml`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/NavigationHistoryPanelStyles.xaml`

**Interfaces:** None — these files have no consumers.

- [ ] **Step 1: Verify no references exist**

Run: `rg "Navigation\.BreadcrumbControl|NavigationSuggestionsPanel|NavigationHistoryPanel" --include "*.cs" --include "*.xaml" src/Client/Desktop/`
Expected: Only self-references in the files being deleted (code-behind comments, style definitions)

- [ ] **Step 2: Delete Navigation/BreadcrumbControl**

Delete files:
```
src/Client/Desktop/Core/LYBT.Desktop.Controls/Navigation/BreadcrumbControl.xaml
src/Client/Desktop/Core/LYBT.Desktop.Controls/Navigation/BreadcrumbControl.xaml.cs
```

Note: The active breadcrumb control is `Controls/BreadcrumbBar.xaml` (used in `BaseDetailContainer.xaml`). This Navigation/ variant is a Phase 2.1 skeleton that was never integrated.

- [ ] **Step 3: Delete NavigationSuggestionsPanel and NavigationHistoryPanel**

Delete files:
```
src/Client/Desktop/Core/LYBT.Desktop.Controls/Navigation/NavigationSuggestionsPanel.xaml
src/Client/Desktop/Core/LYBT.Desktop.Controls/Navigation/NavigationSuggestionsPanel.xaml.cs
src/Client/Desktop/Core/LYBT.Desktop.Controls/Navigation/NavigationHistoryPanel.xaml
src/Client/Desktop/Core/LYBT.Desktop.Controls/Navigation/NavigationHistoryPanel.xaml.cs
```

Note: `MenuManager.cs` line 202 has a TODO comment referencing NavigationHistoryPanel — remove that comment.

- [ ] **Step 4: Delete unused style dictionaries**

Delete files:
```
src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/NavigationSuggestionsPanelStyles.xaml
src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/NavigationHistoryPanelStyles.xaml
```

- [ ] **Step 5: Remove TODO comment from MenuManager**

In `src/Client/Desktop/Shell/Services/MenuManager.cs`, remove line 202:
```csharp
// TODO: Publish event to open/focus NavigationHistoryPanel
```

- [ ] **Step 6: Build and verify**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj`
Expected: BUILD SUCCESSFUL, no new warnings

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "refactor(controls): remove dead Phase 2.1 navigation controls"
```

---

### Task 2: Remove Duplicate BreadcrumbControl

**Covers:** Cleanup of duplicate BreadcrumbControl in Controls/Controls/

**Files:**
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/BreadcrumbControl.xaml`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/BreadcrumbControl.xaml.cs`

**Interfaces:** None — `BreadcrumbBar.xaml` (same directory) is the active implementation used in `BaseDetailContainer.xaml`.

- [ ] **Step 1: Verify BreadcrumbBar is the active implementation**

Run: `rg "BreadcrumbBar" --include "*.xaml" src/Client/Desktop/`
Expected: Found in `BaseDetailContainer.xaml` (line 231)

Run: `rg "Controls\.BreadcrumbControl" --include "*.xaml" src/Client/Desktop/`
Expected: Only self-reference in `Controls/BreadcrumbControl.xaml`

- [ ] **Step 2: Delete duplicate BreadcrumbControl**

Delete files:
```
src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/BreadcrumbControl.xaml
src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/BreadcrumbControl.xaml.cs
```

Note: `BreadcrumbBar.xaml` in the same directory provides the same breadcrumb functionality and is actively used.

- [ ] **Step 3: Build and verify**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj`
Expected: BUILD SUCCESSFUL

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor(controls): remove duplicate BreadcrumbControl (superseded by BreadcrumbBar)"
```

---

### Task 3: Remove Unused CardReaderStatusControl

**Covers:** Cleanup of unused CardReaderStatusControl

**Files:**
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/CardReaderStatusControl.xaml`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/CardReaderStatusControl.xaml.cs`

**Interfaces:** None — this control is not referenced in any XAML or C# file outside its own definition.

- [ ] **Step 1: Verify no references**

Run: `rg "CardReaderStatusControl" --include "*.xaml" --include "*.cs" src/Client/Desktop/`
Expected: Only self-references in the files being deleted

- [ ] **Step 2: Delete CardReaderStatusControl**

Delete files:
```
src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/CardReaderStatusControl.xaml
src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/CardReaderStatusControl.xaml.cs
```

Note: The card reader status is displayed via `CardReaderViewModel` in `PatientSelectionView` and `ClinicalWorkspaceView`, not via this standalone control.

- [ ] **Step 3: Build and verify**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj`
Expected: BUILD SUCCESSFUL

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor(controls): remove unused CardReaderStatusControl"
```

---

### Task 4: Remove Dead PendingQueueView Navigation Registration

**Covers:** Cleanup of PendingQueueView navigation registration (ViewModel is composed, not navigated)

**Files:**
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ClinicalModule.cs` (remove line 40)

**Interfaces:** None — `PendingQueueViewModel` is composed as a child of `PatientSelectionViewModel`, not used as a navigation target. The `PendingQueueView.xaml` and `PendingQueueViewModel.cs` remain (they are used as embedded components).

- [ ] **Step 1: Verify PendingQueueView is never navigated to**

Run: `rg "NavigateTo.*PendingQueue|ViewNames.*PendingQueue" --include "*.cs" src/Client/Desktop/`
Expected: No results (no navigation to PendingQueueView)

- [ ] **Step 2: Remove navigation registration**

In `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ClinicalModule.cs`, remove:
```csharp
// 注册PendingQueueView
containerRegistry.RegisterForNavigation<Views.PendingQueueView>();
```

Note: `PendingQueueViewModel` is still used as `PatientSelectionViewModel.PendingQueue` child property. Only the navigation registration is dead.

- [ ] **Step 3: Build and verify**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj`
Expected: BUILD SUCCESSFUL

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor(clinical): remove dead PendingQueueView navigation registration"
```

---

### Task 5: Verify ClinicalHomeView Usage (Decision Point)

**Covers:** Analysis of ClinicalHomeView — currently registered but navigated to only as fallback

**Files:** No changes — analysis only

**Context:** `ClinicalHomeView` is registered for navigation and has a `ViewNames.ClinicalHome` constant. It's used as:
1. `RoleRegistry.DefaultHomeView` fallback
2. `NavigationCoordinator` line 247 for non-doctor clinical roles

However, the Doctor role home is `ClinicalWorkspaceView` (set in `RoleRegistry`), so `ClinicalHomeView` is only used for clinical staff who are NOT doctors (if any exist). The view provides a dashboard with navigation cards to Patient Management, Registration Queue, Medical Case Query, Herb Library, Formula Library.

- [ ] **Step 1: Confirm current usage**

Run: `rg "ClinicalHome" --include "*.cs" src/Client/Desktop/`
Expected: Found in ViewNames.cs, ClinicalModule.cs, NavigationCoordinator.cs, RoleRegistry.cs

- [ ] **Step 2: Decision**

**Option A (Keep):** If non-doctor clinical staff need a dashboard, keep ClinicalHomeView as-is.
**Option B (Remove):** If all clinical staff are doctors, remove ClinicalHomeView and redirect the fallback to ClinicalWorkspaceView.

Recommended: **Keep** — ClinicalHomeView serves as a useful fallback for clinical roles that aren't doctors (e.g., future clinical assistant roles). It provides quick navigation to all management views.

- [ ] **Step 3: Document decision (no code change needed)**

No action required — this is an analysis step. The view is actively registered and has valid navigation targets.

---

### Task 6: Verify MedicalCaseMasterDetailView Usage

**Covers:** Analysis of MedicalCaseMasterDetailView — navigation wrapper for MedicalCaseMasterDetailControl

**Files:** No changes — analysis only

**Context:** `MedicalCaseMasterDetailView` is a navigation view wrapper registered via `RegisterForNavigation` in `MedicalCaseModule.cs`. It embeds `MedicalCaseMasterDetailControl` and is navigated to from `MedicalCaseWorkspaceViewModel` (lines 569, 573).

- [ ] **Step 1: Confirm navigation path**

Run: `rg "MedicalCaseMasterDetail" --include "*.cs" src/Client/Desktop/`
Expected: Found in ViewNames.cs, MedicalCaseModule.cs, NavigationCoordinator.cs, MedicalCaseWorkspaceViewModel.cs

- [ ] **Step 2: Confirm XAML embedding**

Run: `rg "MedicalCaseMasterDetailView" --include "*.xaml" src/Client/Desktop/`
Expected: Self-reference in MedicalCaseMasterDetailView.xaml

- [ ] **Step 3: Decision**

**Keep** — This is the view-only mode for medical cases (read-only viewing after case completion). It's actively navigated to from the workspace.

---

## Summary of Changes

### Files to Delete (8 files)
1. `src/Client/Desktop/Core/LYBT.Desktop.Controls/Navigation/BreadcrumbControl.xaml` + `.cs`
2. `src/Client/Desktop/Core/LYBT.Desktop.Controls/Navigation/NavigationSuggestionsPanel.xaml` + `.cs`
3. `src/Client/Desktop/Core/LYBT.Desktop.Controls/Navigation/NavigationHistoryPanel.xaml` + `.cs`
4. `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/NavigationSuggestionsPanelStyles.xaml`
5. `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/NavigationHistoryPanelStyles.xaml`
6. `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/BreadcrumbControl.xaml` + `.cs`
7. `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/CardReaderStatusControl.xaml` + `.cs`

### Files to Modify (1 file)
1. `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/ClinicalModule.cs` — remove PendingQueueView navigation registration
2. `src/Client/Desktop/Shell/Services/MenuManager.cs` — remove TODO comment

### Active Views (No Changes)
All remaining views are actively used and serve clear purposes:
- **Shell:** MainWindow, AccountSettings, Confirmation/Message/InputDialogs
- **Role Dashboards:** AdminHome, ClinicalHome (fallback), ReceptionistHome, SysadminHome
- **Clinical Workflow:** ClinicalWorkspace, PatientSelection, MedicalCaseWorkspace
- **Management Wrappers:** Patient/MedicalCase/Herb/Formula/User Management, SystemSettings
- **Business Views:** RegistrationList, MedicalCaseMasterDetail, LogLevelControl
- **Module Controls:** All MasterDetail/View/Edit controls are actively embedded
- **Dialogs:** FormulaImport, HistoryCopy, UnsavedChanges, RegistrationCreate, ServerConfig, FirstRunSetup, UnfinishedCase

## Self-Review

**1. Spec coverage:** All cleanup tasks address confirmed dead code. Active views are documented and verified.

**2. Placeholder scan:** No TBD/TODO placeholders found in implementation steps.

**3. Type consistency:** No type changes involved — only file deletions and a single line removal.
