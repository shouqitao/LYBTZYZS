# Large File Refactor Design Spec

> **Goal:** Refactor 5 large files (3500+ total lines) without changing functionality. Pure structural improvement.

## [S1] HttpClientApiClient (777行 → ~400行)

### Current State
- 96 explicit interface methods across 8 sub-interfaces
- 11 private helper methods (GetAndWrapAsync, PostAndWrapAsync, etc.)
- Inconsistent pagination: 5 methods use client-side PagedResult, 4 use server-side
- URL construction duplicated 9+ times with identical boilerplate

### Extractable
1. **QueryStringBuilder** — conditional parameter URL construction (~45 lines saved)
2. **GetPagedAndWrapAsync<T>** — unified client-side pagination (~25 lines saved)
3. **SendAsync** — unified HTTP method execution (~50 lines saved)
4. **ExportWithKeywordAsync** — export with optional keyword (~15 lines saved)

### Constraints
- Keep explicit interface implementation pattern (correct ISP)
- Keep SwitchingApiClient routing unchanged
- No new DI registrations needed (all private helpers)

## [S2] MasterDetailViewModelBase (774行 → ~200行)

### Current State
- 5 distinct responsibilities: property facade, event sync, list commands, CRUD commands, batch operations
- 8 event subscription pairs (subscribe/unsubscribe)
- 30 virtual members, 6 abstract members
- 5 concrete subclasses (Patient, Herb, User, Formula, MedicalCase)

### Extractable
1. **MasterDetailPropertySync<T,D>** — delegated properties + event forwarding (~290 lines)
2. **PaginationCommandGroup** — 4 pagination commands + CanExecute (~80 lines)
3. **CrudCommandCoordinator<T,D>** — CRUD + batch + restore commands (~240 lines)
4. **Base (slim)** — constructor + abstract methods + lifecycle (~160 lines)

### Constraints
- 5 subclasses MUST NOT change (zero modification)
- Public API surface must remain identical
- `IMasterDetailServices<T,D>` composition pattern preserved

## [S3] PrescriptionPrintService (768行 → ~400行)

### Current State
- 128-line CreateSettingsPanel (programmatic WPF, anti-pattern)
- 36-line ShowPreviewWindow (programmatic Window)
- CloneModelWithItems: 50-line manual property copy
- CreateFixedPage/Continuation: near-identical code duplicated

### Extractable
1. **CloneModelWithItems** → expression tree or record copy (~50 lines saved)
2. **CreatePageFromTemplate** helper (~50 lines saved)
3. **PrintSettingsPanel.xaml** UserControl (~128 lines → XAML)
4. **PreviewWindow.xaml** (~36 lines → XAML)
5. **LocalPrintServer** field caching (~15 lines saved)

### Constraints
- Keep IPrintService<PrescriptionPrintModel> interface unchanged
- Keep QuestPDF export path unchanged
- New XAML files go in LYBT.Desktop.Printing/Templates/

## [S4] MedicalCaseWorkspaceVM (784行 → ~400行)

### Current State
- 5 responsibilities: state machine, navigation, child VM orchestration, patient display, data loading
- Dual state machines: WorkspaceState record + IEditModeStateMachine
- 3 child VMs created directly (not DI)
- Complex back/leave navigation with triple-choice dialog

### Extractable
1. **WorkspaceStateManager** — dual FSM + 5-step workflow (~150 lines)
2. **WorkspaceNavigationHandler** — back/leave + management save (~160 lines)
3. **Base** — child VM orchestration + data loading (~470 lines)

### Constraints
- Keep IWorkspaceHost interface unchanged
- Keep child VM interaction patterns unchanged
- Keep Prism navigation parameter extraction

## [S5] MedicalCaseCommandsVM (555行 → ~350行)

### Current State
- 11 Func<> delegate properties for parent data access
- 9 DelegateCommands for medical case lifecycle
- Import/copy/clear operations

### Refactoring
1. **IMedicalCaseDataProvider** interface — 11 methods
2. CommandsVM constructor takes interface (not Func<> properties)
3. Parent implements interface (1:1 mapping to existing properties)

### Constraints
- Keep all 9 command behaviors unchanged
- Keep ImportFormula/CopyHistory/ClearHerbs dialog flows
- New interface goes in LYBT.Desktop.MedicalCase.Interfaces/
