<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# LYBT.Desktop.Models

## Purpose
Client-side model layer providing ViewModel base classes, validation infrastructure, and shared UI models for the WPF desktop application. Contains the ViewModel inheritance hierarchy (`CoreViewModelBase` -> `NavigableViewModelBase` / `DialogViewModelBase` / `ValidatableModelBase`) built on CommunityToolkit.Mvvm source generators and Prism event aggregation. These base classes provide standardized busy/error state management, async execution wrappers with unified exception handling, UI thread dispatch, disposable lifecycle management, and event subscription tracking. All business module ViewModels inherit from these bases.

## Key Files
| File | Description |
|------|-------------|
| `ViewModels/Base/CoreViewModelBase.cs` | Root base class: ObservableObject + IsBusy/StatusMessage/ErrorMessage + ExecuteWithErrorHandlingAsync + UI thread dispatch + CompositeDisposable |
| `ViewModels/Base/NavigableViewModelBase.cs` | Base for ViewModels hosted in Prism regions (navigation lifecycle) |
| `ViewModels/Base/DialogViewModelBase.cs` | Base for ViewModels shown in dialog windows |
| `ViewModels/Base/ValidatableModelBase.cs` | Base for models requiring validation |
| `ViewModels/Base/ValidationAccessors.cs` | Validation helper extensions |

## Subdirectories
| Directory | Purpose |
|-----------|---------|
| `ViewModels/Base/` | ViewModel base class hierarchy |

## For AI Agents

### Working In This Directory
- This is a foundational layer -- nearly every business module ViewModel depends on these base classes
- `CoreViewModelBase` uses CommunityToolkit.Mvvm `[ObservableProperty]` source generators -- do NOT manually implement `INotifyPropertyChanged` for decorated fields
- Service injection uses `IViewModelServices` aggregate (from Contracts) to reduce constructor parameter count
- Event subscriptions via `Events` property use `EventSubscriptionManager` for automatic cleanup on Dispose
- `ExecuteWithErrorHandlingAsync` is the standard pattern for all async ViewModel operations -- it handles busy state, error state, and logging
- This project references `LYBT.Desktop.Infrastructure` (for Events, UI thread dispatcher) and `LYBT.Desktop.Contracts`

### Testing Requirements
- Base class tests should verify: IsBusy toggling, error state management, disposal cleanup, event subscription disposal
- Use `Mock<IViewModelServices>` for testing derived ViewModels
- Test `ExecuteWithErrorHandlingAsync` for: success path, OperationCanceledException, general Exception, finally-block busy reset

### Common Patterns
- `[ObservableProperty]` + `[NotifyPropertyChangedFor]` for computed property chains
- `SetBusy(bool, string?)` / `SetError(string)` / `ClearError()` for state management
- `RunOnUIThread(Action)` / `RunOnUIThreadAsync(Func<Task>)` for cross-thread UI updates
- `AddDisposable(IDisposable)` for registering cleanup targets
- `Events.Subscribe<TEvent>(Action<TEvent>)` for Prism event aggregation with auto-cleanup

## Dependencies

### Internal
- `LYBT.Desktop.Contracts` -- Interface definitions (IViewModelServices, IUiThreadDispatcher)
- `LYBT.Desktop.Infrastructure` -- EventSubscriptionManager, UI thread dispatcher, infrastructure events
- `LYBT.Shared.Components` -- Shared UI components
- `LYBT.Shared.Models` -- DTOs and contracts
- `LYBT.Shared.Primitives` -- Base types and constants
- `LYBT.Shared.Utilities` -- Utility classes

### External
- `CommunityToolkit.Mvvm` -- Source generator MVVM ([ObservableProperty], [RelayCommand])
- `Prism.Core` / `Prism.Wpf` -- Prism MVVM framework (IEventAggregator)
- `System.Reactive` -- Reactive extensions (CompositeDisposable)
- `System.ComponentModel.Annotations` -- Data annotation attributes
- `Microsoft.Extensions.Logging` -- Logging
- `System.Text.Json` -- JSON serialization

<!-- MANUAL: -->
