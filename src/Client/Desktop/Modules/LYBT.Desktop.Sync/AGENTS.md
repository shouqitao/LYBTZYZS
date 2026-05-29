<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# LYBT.Desktop.Sync

## Purpose

Data synchronization module for the TCM clinic desktop client. Provides bidirectional sync of base data (Herbs, Patients, Formulas) between local and server databases. Implements a phase-based workflow (Idle -> Check Differences -> Review -> Execute Sync -> Completed/Failed) with conflict detection and resolution. Users can select which items to upload (local-only), download (server-only), or resolve conflicts (choose local vs server version). Includes error classification for retry decisions and delete-rejection handling for referential integrity.

## Key Files

| File | Description |
|------|-------------|
| `SyncModule.cs` | Prism IModule entry point. Depends on AuthenticationModule. Registers SyncViewModel, SyncConflictDialogViewModel, SyncView, SyncConflictDialog, and SyncItemViewModelFactory (singleton). |
| `ViewModels/SyncViewModel.cs` | Main sync ViewModel. Phase-based workflow using SyncPhase enum. Commands: CheckDifferences, ExecuteSync, Retry, Reset, SelectAll/Download/Upload, Refresh. Validates API health before operations. Publishes SyncEvents.StatusChangedEvent. |
| `ViewModels/SyncPhase.cs` | Enum defining workflow phases: Idle, CheckingDifferences, ReviewingDifferences, ExecutingSync, Completed, Failed. Also defines SyncErrorCategory enum: TransientNetwork, AuthExpired, BusinessReject, ConflictChanged, Unknown. |
| `ViewModels/SyncConflictDialogViewModel.cs` | Dialog ViewModel for conflict resolution. Navigate conflicts one-by-one or bulk "Use All Local/Server". Tracks ResolvedCount/TotalCount. |
| `ViewModels/SyncResultSummary.cs` | Record type for per-entity-type sync result cards (uploaded/downloaded/deleted/skipped/failed counts + rejection reasons). |
| `Services/SyncErrorClassifier.cs` | Static classifier mapping exceptions to SyncErrorCategory (HttpRequestException -> TransientNetwork, 401 -> AuthExpired, 409 -> ConflictChanged, 4xx -> BusinessReject). Determines retryability. |
| `Services/SyncItemViewModelFactory.cs` | Factory creating SyncItemViewModel from SyncDiffDto. Wires PropertyChanged for selection-changed callbacks. Registered as singleton. |
| `Services/SyncResolutionBuilder.cs` | Static builder converting user selections (ObservableCollection of SyncItemViewModel) into SyncResolution DTO for the sync service. |
| `Views/SyncView.xaml` | Main sync UI with entity type selector, difference lists, and action buttons. |
| `Views/SyncConflictDialog.xaml` | Conflict resolution dialog UI. |

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| `ViewModels/` | SyncViewModel (main workflow), SyncConflictDialogViewModel (conflict resolution), SyncPhase/SyncErrorCategory enums, SyncResultSummary record. |
| `Views/` | SyncView and SyncConflictDialog XAML files. |
| `Services/` | SyncErrorClassifier (error classification), SyncItemViewModelFactory (ViewModel creation), SyncResolutionBuilder (resolution DTO assembly). |

## For AI Agents

### Working In This Directory

- **Phase-based workflow**: The ViewModel state machine is driven by `SyncPhase` enum. Command CanExecute guards depend on the current phase (e.g., ExecuteSync only enabled during ReviewingDifferences, CheckDifferences only during Idle/Completed/Failed).
- **Pre-condition validation**: Both CheckDifferences and ExecuteSync validate authentication (`SessionManager.IsAuthenticated`) and API health (`IApiHealthCheckService.CheckHealthAsync`) before proceeding.
- **Conflict resolution flow**: If unresolved conflicts exist when ExecuteSync is triggered, the SyncConflictDialog is shown first. Only after the user resolves all conflicts does the actual sync execute.
- **Retry mechanism**: On failure, `SyncErrorClassifier` categorizes the error. Retryable categories (TransientNetwork, ConflictChanged, AuthExpired) enable the Retry command. The last operation descriptor is stored for retry replay.
- **Selection callbacks**: SyncItemViewModelFactory is a singleton that wires PropertyChanged on each item's IsSelected to a callback that updates computed counts on the parent ViewModel.
- **Event publishing**: Phase transitions publish `SyncEvents.StatusChangedEvent` with `SyncStatusPayload` (IsSyncing, LastSyncTime, StatusMessage) for other modules to observe.

### Testing Requirements

- Test via `LYBT.Tests.Desktop` project.
- Verify phase transitions: Idle -> CheckingDifferences -> ReviewingDifferences -> ExecutingSync -> Completed (happy path).
- Verify error classification: each exception type maps to correct SyncErrorCategory.
- Verify conflict dialog: UseLocal/UseServer/Skip set correct ResolutionDecision values.
- Verify SyncResolutionBuilder: correctly maps selected items to upload/download/conflict-resolution lists.
- Verify pre-condition validation rejects unauthenticated or unhealthy API states.

### Common Patterns

- **CommunityToolkit MVVM**: Extensively uses `[ObservableProperty]`, `[RelayCommand]`, `[NotifyCanExecuteChangedFor]`, `[NotifyPropertyChangedFor]` attributes.
- **Phase-driven CanExecute**: Commands check `CurrentPhase` in their CanExecute predicates rather than boolean flags.
- **Static service classes**: SyncErrorClassifier and SyncResolutionBuilder are stateless static classes (not injected).
- **Singleton factory**: SyncItemViewModelFactory is registered as singleton because it maintains a selection-changed callback reference.
- **Cancellation**: Uses a single CancellationTokenSource for dialog operations, disposed in `OnDisposing()`.
- **Computed properties**: `IsSyncing`, `HasDataToSync`, counts are all computed from collections and phase state, notified via `OnPropertyChanged`.

## Dependencies

### Internal

| Dependency | Purpose |
|------------|---------|
| `LYBT.Desktop.Contracts` | ISyncService, IDialogService, ISessionManager, IViewModelServices, SyncEvents, SyncResolution |
| `LYBT.Desktop.Foundation` | IApiHealthCheckService, ApiHealthStatus |
| `LYBT.Desktop.Infrastructure` | UI utilities |
| `LYBT.Desktop.Models` | NavigableViewModelBase, DialogViewModelBase base classes |
| `LYBT.Shared.Models` | SyncDiffDto, SyncExecutionResult, SyncDiffType, Shared DTOs |
| `LYBT.Shared.Primitives` | Shared constants |

### External

| Package | Purpose |
|---------|---------|
| `Prism.Core` / `Prism.DryIoc` / `Prism.Wpf` | MVVM framework, DI, navigation, dialog service |
| `CommunityToolkit.Mvvm` | [ObservableProperty], [RelayCommand] source generators |

<!-- MANUAL: -->
