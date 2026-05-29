<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# LYBT.Desktop.Auth

## Purpose

Authentication module for the TCM clinic desktop client. Handles user login/logout, credential storage (remember username/password via DPAPI), JWT token management, and API health status display on the login screen. This is a foundational module with no module dependencies -- all other modules depend on successful authentication before they initialize.

## Key Files

| File | Description |
|------|-------------|
| `AuthenticationModule.cs` | Prism IModule entry point; registers LoginViewModel and LoginView for navigation. No service registrations (services registered centrally in Shell). |
| `ViewModels/LoginViewModel.cs` | Main login ViewModel (extends NavigableViewModelBase). Orchestrates login via ILoginCoordinator, manages remember-username/password via IUsernameStorageService and ICredentialVault, monitors API health via IApplicationStateService. |
| `Views/LoginView.xaml` | Login form UI (username, password, remember checkboxes, API status indicator). |
| `Views/LoginWindow.xaml` | Standalone login window (used before Shell initializes). |
| `LYBT.Desktop.Auth.csproj` | Project file; targets net8.0-windows with WPF, references Foundation/Infrastructure/Models/Contracts. |

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| `ViewModels/` | Contains LoginViewModel -- the sole ViewModel for this module. |
| `Views/` | Contains LoginView (navigation target) and LoginWindow (standalone). |

## For AI Agents

### Working In This Directory

- LoginViewModel delegates actual authentication to `ILoginCoordinator.LoginAsync()` -- do not add auth logic directly in the ViewModel.
- Credential persistence uses two services: `IUsernameStorageService` (plaintext username) and `ICredentialVault` (DPAPI-encrypted password). Both are optional injections.
- The "Remember Password" checkbox auto-enables "Remember Username" (T5-P2-07 behavior).
- API health is monitored via `IApplicationStateService.StatusChanged` event -- ViewModel subscribes in constructor and unsubscribes in `OnDisposing()`.
- The ViewModel uses `NavigableViewModelBase` (not `UnifiedViewModelBase`) per OpenSpec refactor-viewmodel-base-classes.
- Commands use Prism `DelegateCommand` (not CommunityToolkit `[RelayCommand]`) for async commands with CanExecute.

### Testing Requirements

- Login flow is tested via `LYBT.Tests.Desktop` project.
- Test the CanExecute guards: LoginCommand requires non-empty Username, Password, and !IsLoading.
- Verify credential save/clear behavior when RememberUsername/RememberPassword checkboxes change.
- Verify API status event handler updates UI properties correctly.

### Common Patterns

- **Async fire-and-forget**: Uses `SafeFireAndForget()` extension for background initialization.
- **UI thread dispatch**: All property updates from async/event callbacks go through `Services.UiThreadDispatcher.InvokeAsync()`.
- **Error mapping**: Login failures use `ClientErrorMessageMapper.GetSafeOperationFailureMessage()` for user-friendly messages.
- **Disposal**: `OnDisposing()` unsubscribes events and cancels CancellationTokenSource.

## Dependencies

### Internal

| Dependency | Purpose |
|------------|---------|
| `LYBT.Desktop.Contracts` | ILoginCoordinator, IApplicationStateService, IUsernameStorageService, ICredentialVault interfaces |
| `LYBT.Desktop.Foundation` | HealthCheck.ApiHealthStatus, Security.ICredentialVault, Application.IApplicationStateService |
| `LYBT.Desktop.Infrastructure` | Extensions (SafeFireAndForget), UI thread utilities |
| `LYBT.Desktop.Models` | NavigableViewModelBase base class |
| `LYBT.Shared.ExceptionHandling` | ClientErrorMessageMapper for safe error messages |
| `LYBT.Shared.Models` | Shared DTOs |

### External

| Package | Purpose |
|---------|---------|
| `Prism.Core` / `Prism.DryIoc` / `Prism.Wpf` | MVVM framework, DI, navigation |
| `Microsoft.Extensions.Logging.Abstractions` | ILogger injection |

<!-- MANUAL: -->
