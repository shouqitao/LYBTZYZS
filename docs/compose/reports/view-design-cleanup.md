---
feature: view-design-cleanup
status: delivered
specs: []
plans:
  - docs/compose/plans/2026-07-14-view-design-cleanup.md
branch: master
commits: 8f94bd514..446551212
---

# View Design & Cleanup — Final Report

## What Was Built

A comprehensive audit of the Desktop WPF application's view system, resulting in the removal of 12 dead/unused files and 1 dead navigation registration. The audit mapped all 60+ views, controls, and dialogs across Shell, 4 Role modules, 8 Business modules, and Core infrastructure, confirming which are actively used and which are dead code.

## Architecture

### View System Structure

```
Shell (6 views)
  ├── MainWindow, AccountSettings, Confirmation/Message/InputDialogs
  
Roles (4 modules, 24 views)
  ├── Admin (7): AdminHome, SystemSettings, 5 Management wrappers
  ├── Clinical (10): ClinicalHome, ClinicalWorkspace, PatientSelection,
  │                   MedicalCaseWorkspace, PendingQueue, 4 Management wrappers
  ├── Receptionist (1): ReceptionistHome
  └── Sysadmin (2): SysadminHome, LogLevelControl

Business Modules (8 modules, 16+ controls)
  ├── Auth: Login, ServerConfig, FirstRunSetup dialogs
  ├── Users: MasterDetail/View/Edit controls
  ├── Patients: MasterDetail/View/Edit/Selection controls
  ├── Herbs: MasterDetail/View/Edit controls
  ├── Formula: MasterDetail/Edit controls
  ├── MedicalCase: MasterDetail/View/Edit, WorkflowStepIndicator, 3 dialogs
  ├── Registration: List view, Create dialog
  └── Reports: Home view

Core Controls (20+)
  ├── BreadcrumbBar, MasterDetailLayout, DataGridToolbar
  ├── StatusBadge, LoadingOverlay, EmptyState, SearchBox
  ├── HerbList/HerbItem, FormulaView, Toast, InfoCard
  └── UnifiedPaginationBar, DetailToolbar
```

### Navigation Flow

```
Login -> RoleHome (via RoleRegistry)
  Admin:     AdminHome -> Management wrappers (Patient/MedicalCase/Herb/Formula/User), SystemSettings, Reports
  Doctor:    ClinicalWorkspace -> PatientSelection -> MedicalCaseWorkspace -> MedicalCaseMasterDetail
  Receptionist: ReceptionistHome -> PatientManagement, RegistrationList
  Sysadmin:  SysadminHome -> LogLevelControl
```

### Design Decisions

- **ClinicalHomeView kept as fallback**: Though Doctor role uses ClinicalWorkspaceView as home, ClinicalHomeView serves non-doctor clinical roles and provides quick navigation to all management views.
- **PendingQueueViewModel composed, not navigated**: PendingQueueViewModel is a child of PatientSelectionViewModel (composed via property), not a navigation target. The navigation registration was dead code.
- **BreadcrumbBar over BreadcrumbControl**: BreadcrumbBar (used in BaseDetailContainer) is the active breadcrumb implementation. BreadcrumbControl in Controls/Controls/ and Navigation/ were duplicates/skeletons.

## What Was Removed

| File | Reason |
|------|--------|
| Navigation/BreadcrumbControl.xaml + .cs | Phase 2.1 skeleton, never integrated |
| Navigation/NavigationSuggestionsPanel.xaml + .cs | Phase 2.1 skeleton, never integrated |
| Navigation/NavigationHistoryPanel.xaml + .cs | Phase 2.1 skeleton, never integrated |
| Themes/NavigationSuggestionsPanelStyles.xaml | Associated style dictionary |
| Themes/NavigationHistoryPanelStyles.xaml | Associated style dictionary |
| Controls/BreadcrumbControl.xaml + .cs | Duplicate superseded by BreadcrumbBar |
| Controls/CardReaderStatusControl.xaml + .cs | No consumers found |
| ClinicalModule.cs (PendingQueueView registration) | ViewModel composed, not navigated |
| MenuManager.cs (TODO comment) | Referenced deleted NavigationHistoryPanel |

## Verification

- All 4 commits pass `dotnet build` with 0 warnings, 0 errors
- Grep confirms no remaining references to deleted files
- Active views verified: BreadcrumbBar (BaseDetailContainer), ClinicalHomeView (RoleRegistry fallback), MedicalCaseMasterDetailView (MedicalCaseWorkspaceViewModel navigation)

## Journey Log

- [lesson] Phase 2.1 navigation controls (BreadcrumbControl, NavigationSuggestionsPanel, NavigationHistoryPanel) were scaffolded but never integrated — 6 files totaling ~660 lines of dead code
- [lesson] CardReaderStatusControl was built as a standalone status indicator but never embedded — card reader status is shown via CardReaderViewModel in PatientSelectionView instead
- [lesson] PendingQueueView was registered for navigation but PendingQueueViewModel is composed as a child of PatientSelectionViewModel — the navigation registration was never used

## Source Materials

| File | Role | Notes |
|------|------|-------|
| `docs/compose/plans/2026-07-14-view-design-cleanup.md` | Implementation plan | Complete |
