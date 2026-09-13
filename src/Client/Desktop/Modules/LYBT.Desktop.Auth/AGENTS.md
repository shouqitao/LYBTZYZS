<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# LYBT.Desktop.Auth

## Purpose

Authentication module for the TCM clinic desktop client. Handles user login/logout, credential storage (remember username/password via DPAPI), connection mode (Remote/Local) detection & switching, and first-run setup. This is a foundational module with no module dependencies -- all other modules depend on successful authentication before they initialize.

## Key Files

| File | Description |
|------|-------------|
| `AuthenticationModule.cs` | Prism IModule entry point; registers `LoginViewModel` + `LoginCredentialsViewModel` + `ConnectionStatusViewModel`, `LoginView` for navigation, and the `ServerConfigView` / `FirstRunSetupView` dialogs. No service registrations (services registered centrally in Shell). |
| `ViewModels/LoginViewModel.cs` | Main login ViewModel (`NavigableViewModelBase`). Composes `Credentials` + `ConnectionStatus` child VMs; orchestrates login via `ILoginCoordinator`; proxies child properties for XAML compatibility; runs `BackgroundInitAsync`. |
| `ViewModels/LoginCredentialsViewModel.cs` | Credential input child VM (username/password/remember flags) via `IUsernameStorageService` + `ICredentialVault`. |
| `ViewModels/ConnectionStatusViewModel.cs` | Connection state child VM (API health, mode display, mode switch commands). |
| `ViewModels/ConnectionTestViewModelBase.cs` | Abstract base for connection-test dialogs (RemoteUrl/TestStatus/IsNotTesting + `TestConnectionCommand`). |
| `ViewModels/ServerConfigViewModel.cs` | Server config dialog VM (save-and-enable vs save-only). |
| `ViewModels/FirstRunSetupViewModel.cs` | First-run wizard VM (remote setup or local fallback). |
| `Views/LoginView.xaml` | Login form UI (username, password, remember checkboxes, API status indicator). |
| `Models/ConnectionTestStatus.cs` | Idle/Testing/Success/Failed enum. |
| `LYBT.Desktop.Auth.csproj` | Project file; targets net8.0-windows with WPF, references Foundation/Infrastructure/Contracts/Shared.Models. |

> 早期 `Views/LoginWindow.xaml` 已删除——当前为单窗口模式，`LoginView` 作为 `UserControl` 嵌入主窗口。

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| `ViewModels/` | LoginViewModel + 2 child VMs + abstract `ConnectionTestViewModelBase` + 2 dialog VMs. |
| `Views/` | LoginView (navigation target), ServerConfigView & FirstRunSetupView (registered as dialogs). |
| `Models/` | `ConnectionTestStatus` enum. |

## For AI Agents

### Working In This Directory

- LoginViewModel delegates actual authentication to `ILoginCoordinator.LoginAsync()` -- do not add auth logic directly in the ViewModel.
- Credential persistence uses two services: `IUsernameStorageService` (plaintext username) and `ICredentialVault` (DPAPI-encrypted password). Both are optional injections, surfaced through `LoginCredentialsViewModel`.
- The "Remember Password" checkbox auto-enables "Remember Username" (T5-P2-07 behavior); unchecking username clears the saved password.
- API health is monitored via `IApplicationStateService.StatusChanged` event -- `ConnectionStatusViewModel` subscribes in constructor and unsubscribes in `OnDisposing()` (which `LoginViewModel.OnDisposing()` calls).
- `LoginViewModel` proxies child-VM `PropertyChanged` into its own `OnPropertyChanged(e.PropertyName)`; `Username`/`Password` changes additionally call `LoginCommand.NotifyCanExecuteChanged()`.
- ViewModels use `NavigableViewModelBase` / `DialogViewModelBase` (not `UnifiedViewModelBase`).
- Commands are manually instantiated in the constructor (not `[RelayCommand]` attribute) because CanExecute depends on multiple properties; `LoginCommand` relies on `AsyncRelayCommand`'s default `allowConcurrentExecutions: false` for double-click protection.

### Testing Requirements

- Login flow is tested via `LYBT.Tests.Desktop` project.
- Test the CanExecute guards: LoginCommand requires non-empty Username, Password, and !IsLoading.
- Verify credential save/clear behavior when RememberUsername/RememberPassword checkboxes change.
- Verify API status is propagated from `ConnectionStatusViewModel` through the main VM's proxy properties.

### Common Patterns

- **Async fire-and-forget**: Uses `SafeFireAndForget()` extension for `BackgroundInitAsync`.
- **UI thread dispatch**: All property updates from async/event callbacks go through `Services.UiThreadDispatcher.InvokeAsync()`.
- **Error mapping**: Login failures use `ClientErrorMessageMapper.GetSafeOperationFailureMessage()` for user-friendly messages.
- **Disposal**: `OnDisposing()` unsubscribes child-VM events, disposes child VMs, and cancels the `CancellationTokenSource`.

## Dependencies

### Internal

| Dependency | Purpose |
|------------|---------|
| `LYBT.Desktop.Contracts` | Services.ILoginCoordinator, IConnectionModeService, IConnectionSettingsService |
| `LYBT.Desktop.Foundation` | HealthCheck.ApiHealthStatus, Security.ICredentialVault/IUsernameStorageService, Application.IApplicationStateService, ExceptionHandling |
| `LYBT.Desktop.Infrastructure` | `NavigableViewModelBase`, `DialogViewModelBase`, Extensions (SafeFireAndForget), UI thread utilities, Interfaces.IClinicSettingsService |
| `LYBT.Shared.Models` | Shared DTOs |

### External

| Package | Purpose |
|---------|---------|
| `Prism.Core` / `Prism.DryIoc` / `Prism.Wpf` | MVVM framework, DI, navigation, dialog service |
| `Microsoft.Extensions.Logging.Abstractions` | ILogger injection |

<!-- MANUAL: -->
