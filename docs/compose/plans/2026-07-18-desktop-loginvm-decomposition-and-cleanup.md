# LoginViewModel 拆分 + 命令统一 + 清理 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Decompose LoginViewModel into focused sub-VMs, unify DelegateCommand usage, add test coverage, and clean dead code.

**Architecture:** LoginViewModel (705行) mixes 3 concerns: credentials UI, connection status, and login orchestration. We extract `LoginCredentialsViewModel` and `ConnectionStatusViewModel` as child VMs. The parent retains login orchestration and proxies child properties for XAML backward compatibility. Dead code cleanup and test additions follow.

**Tech Stack:** .NET 8, WPF, Prism (DryIoc), CommunityToolkit.Mvvm, NSubstitute (tests)

## Global Constraints

- **Pure refactoring** — no functional changes, no behavior changes
- **XAML backward compatibility** — LoginView.xaml binding paths must continue to work (proxy properties on parent)
- **Incremental commits** — one commit per logical step
- **Build must pass** — `dotnet build LYBTZYZS.sln` after each step

---

### Task 1: Create LoginCredentialsViewModel

**Covers:** [S2]

**Files:**
- Create: `src/Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/LoginCredentialsViewModel.cs`

**Interfaces:**
- Consumes: `IUsernameStorageService`, `ICredentialVault`
- Produces: `LoginCredentialsViewModel` with Username, Password, RememberUsername, RememberPassword, HasSavedPassword properties

- [ ] **Step 1: Create LoginCredentialsViewModel**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Security;
using LYBT.Desktop.Infrastructure.ViewModels.Base;

namespace LYBT.Desktop.Auth.ViewModels;

/// <summary>
/// 登录凭证 UI 状态 — 用户名/密码输入、记住密码
/// 从 LoginViewModel 提取，单一职责：凭证输入状态管理
/// </summary>
public partial class LoginCredentialsViewModel : CoreViewModelBase
{
    private readonly IUsernameStorageService? _usernameStorage;
    private readonly ICredentialVault? _credentialVault;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _rememberUsername;

    [ObservableProperty]
    private bool _rememberPassword;

    [ObservableProperty]
    private bool _hasSavedPassword;

    [ObservableProperty]
    private string? _savedUsername;

    public LoginCredentialsViewModel(
        IViewModelServices services,
        IUsernameStorageService? usernameStorage,
        ICredentialVault? credentialVault)
        : base(services)
    {
        _usernameStorage = usernameStorage;
        _credentialVault = credentialVault;
    }

    /// <summary>
    /// 加载已保存的凭证
    /// </summary>
    public async Task LoadSavedCredentialsAsync()
    {
        try
        {
            if (_usernameStorage != null)
            {
                SavedUsername = await _usernameStorage.GetUsernameAsync();
                if (!string.IsNullOrEmpty(SavedUsername))
                {
                    Username = SavedUsername;
                    RememberUsername = true;
                }
            }

            if (_credentialVault != null)
            {
                var hasSaved = await _credentialVault.HasSavedCredentialsAsync();
                HasSavedPassword = hasSaved;
                if (hasSaved)
                {
                    var credentials = await _credentialVault.LoadCredentialsAsync();
                    if (credentials != null)
                    {
                        Password = credentials.Password;
                        RememberPassword = true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "加载已保存凭证失败");
        }
    }

    /// <summary>
    /// 保存凭证（登录成功后调用）
    /// </summary>
    public async Task SaveCredentialsAsync()
    {
        try
        {
            if (RememberUsername && _usernameStorage != null)
                await _usernameStorage.SaveUsernameAsync(Username);
            else if (_usernameStorage != null)
                await _usernameStorage.ClearUsernameAsync();

            if (RememberPassword && _credentialVault != null)
                await _credentialVault.SaveCredentialsAsync(Username, Password);
            else if (_credentialVault != null)
                await _credentialVault.ClearCredentialsAsync();
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "保存凭证失败");
        }
    }

    /// <summary>
    /// 清除已保存的用户名
    /// </summary>
    [RelayCommand]
    private async Task ClearSavedUsernameAsync()
    {
        if (_usernameStorage != null)
            await _usernameStorage.ClearUsernameAsync();
        SavedUsername = null;
        RememberUsername = false;
    }

    /// <summary>
    /// 清除已保存的密码
    /// </summary>
    [RelayCommand]
    private async Task ClearSavedPasswordAsync()
    {
        if (_credentialVault != null)
            await _credentialVault.ClearCredentialsAsync();
        HasSavedPassword = false;
        RememberPassword = false;
    }
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add src/Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/LoginCredentialsViewModel.cs
git commit -m "refactor(auth): extract LoginCredentialsViewModel from LoginViewModel"
```

---

### Task 2: Create ConnectionStatusViewModel

**Covers:** [S2]

**Files:**
- Create: `src/Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/ConnectionStatusViewModel.cs`

**Interfaces:**
- Consumes: `IApplicationStateService`, `IConnectionModeService`, `IConnectionSettingsService`
- Produces: `ConnectionStatusViewModel` with ApiStatus, IsRemoteMode, IsRemoteAvailable, switch commands

- [ ] **Step 1: Create ConnectionStatusViewModel**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Infrastructure.ViewModels.Base;

namespace LYBT.Desktop.Auth.ViewModels;

/// <summary>
/// 连接状态 UI — API 健康状态、连接模式显示、模式切换
/// 从 LoginViewModel 提取，单一职责：连接状态管理
/// </summary>
public partial class ConnectionStatusViewModel : CoreViewModelBase
{
    private readonly IApplicationStateService _applicationStateService;
    private readonly IConnectionModeService? _connectionModeService;
    private readonly IConnectionSettingsService? _connectionSettingsService;

    [ObservableProperty]
    private ApiHealthStatus _apiStatus = ApiHealthStatus.Unknown;

    [ObservableProperty]
    private string _apiStatusMessage = string.Empty;

    [ObservableProperty]
    private string _currentModeDisplay = string.Empty;

    [ObservableProperty]
    private bool _isRemoteMode;

    [ObservableProperty]
    private bool _isRemoteAvailable;

    public bool IsApiUnhealthy => ApiStatus == ApiHealthStatus.Unhealthy;

    public ConnectionStatusViewModel(
        IViewModelServices services,
        IApplicationStateService applicationStateService,
        IConnectionModeService? connectionModeService,
        IConnectionSettingsService? connectionSettingsService)
        : base(services)
    {
        _applicationStateService = applicationStateService;
        _connectionModeService = connectionModeService;
        _connectionSettingsService = connectionSettingsService;

        _applicationStateService.ApiStatusChanged += OnApiStatusChanged;
    }

    /// <summary>
    /// 检测连接模式
    /// </summary>
    public async Task DetectConnectionModeAsync()
    {
        if (_connectionModeService == null) return;

        try
        {
            IsRemoteMode = await _connectionModeService.IsRemoteModeAsync();
            IsRemoteAvailable = await _connectionModeService.IsRemoteAvailableAsync();
            CurrentModeDisplay = IsRemoteMode ? "远程" : "本地";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "检测连接模式失败");
            IsRemoteMode = false;
            CurrentModeDisplay = "本地";
        }
    }

    /// <summary>
    /// 加载 API 状态
    /// </summary>
    public void LoadApiStatus()
    {
        var status = _applicationStateService.CurrentStatus;
        UpdateApiStatus(status);
    }

    /// <summary>
    /// 切换到本地模式
    /// </summary>
    [RelayCommand]
    private async Task SwitchToLocalAsync()
    {
        if (_connectionModeService == null) return;
        await _connectionModeService.SwitchToLocalAsync();
        IsRemoteMode = false;
        CurrentModeDisplay = "本地";
    }

    /// <summary>
    /// 切换到远程模式
    /// </summary>
    [RelayCommand(CanExecute = nameof(IsRemoteAvailable))]
    private async Task SwitchToRemoteAsync()
    {
        if (_connectionModeService == null) return;
        await _connectionModeService.SwitchToRemoteAsync();
        IsRemoteMode = true;
        CurrentModeDisplay = "远程";
    }

    /// <summary>
    /// 重试 API 健康检查
    /// </summary>
    [RelayCommand]
    private async Task RetryApiCheckAsync()
    {
        await _applicationStateService.CheckHealthAsync();
    }

    private void OnApiStatusChanged(object? sender, ApiStatusChangedEventArgs e)
    {
        UpdateApiStatus(e.Status);
    }

    private void UpdateApiStatus(ApiHealthStatus status)
    {
        ApiStatus = status;
        ApiStatusMessage = status switch
        {
            ApiHealthStatus.Healthy => "API 连接正常",
            ApiHealthStatus.Unhealthy => "API 连接异常",
            _ => "检测中..."
        };
        OnPropertyChanged(nameof(IsApiUnhealthy));
    }

    protected override void OnDisposing()
    {
        _applicationStateService.ApiStatusChanged -= OnApiStatusChanged;
        base.OnDisposing();
    }
}
```

- [ ] **Step 2: Build to verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds

- [ ] **Step 3: Commit**

```bash
git add src/Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/ConnectionStatusViewModel.cs
git commit -m "refactor(auth): extract ConnectionStatusViewModel from LoginViewModel"
```

---

### Task 3: Refactor LoginViewModel to compose child VMs

**Covers:** [S2]

**Files:**
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/LoginViewModel.cs`

**Interfaces:**
- Consumes: `LoginCredentialsViewModel`, `ConnectionStatusViewModel` (from Tasks 1-2)
- Produces: Simplified LoginViewModel (~250行) with proxy properties for XAML backward compatibility

- [ ] **Step 1: Read current LoginViewModel to understand full structure**

Read `src/Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/LoginViewModel.cs` completely.

- [ ] **Step 2: Refactor LoginViewModel**

Key changes:
1. Add `Credentials` and `ConnectionStatus` as public properties
2. Remove migrated fields (Username, Password, RememberUsername, etc.)
3. Remove migrated methods (LoadSavedCredentialsAsync, SaveCredentialsAsync, etc.)
4. Add proxy properties for XAML backward compatibility:
   - `Username` → `Credentials.Username`
   - `Password` → `Credentials.Password`
   - `ApiStatus` → `ConnectionStatus.ApiStatus`
   - etc.
5. Remove DelegateCommand fields, replace with [RelayCommand] where possible
6. Keep LoginCommand, CloseApplicationCommand, OpenSettingsCommand in LoginViewModel

- [ ] **Step 3: Build to verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds (may have warnings about obsolete DelegateCommand usage)

- [ ] **Step 4: Update LoginView.xaml binding paths if needed**

Check if any binding paths changed. If proxy properties are in place, no XAML changes needed.

- [ ] **Step 5: Build and verify full solution**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds

- [ ] **Step 6: Commit**

```bash
git add src/Client/Desktop/Modules/LYBT.Desktop.Auth/
git commit -m "refactor(auth): refactor LoginViewModel to compose child VMs

- LoginViewModel now composes LoginCredentialsViewModel + ConnectionStatusViewModel
- Proxy properties maintain XAML backward compatibility
- Reduced from ~705 lines to ~300 lines"
```

---

### Task 4: Verify and fix PrescriptionPrintHandler DI registration

**Covers:** [S4]

**Files:**
- Check: `src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/MedicalCaseModule.cs`
- Modify (if needed): same file

**Interfaces:**
- Consumes: none
- Produces: PrescriptionPrintHandler registered in DI if missing

- [ ] **Step 1: Check if PrescriptionPrintHandler is registered**

Search `MedicalCaseModule.cs` for `PrescriptionPrintHandler`. Also search `ServiceCollectionExtensions.cs`.

- [ ] **Step 2: If not registered, add registration**

In `MedicalCaseModule.cs` `RegisterTypes` method, add:
```csharp
containerRegistry.Register<PrescriptionPrintHandler>();
```

- [ ] **Step 3: Build to verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds

- [ ] **Step 4: Commit (only if registration was added)**

```bash
git add src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/MedicalCaseModule.cs
git commit -m "fix(desktop): register PrescriptionPrintHandler in DI container"
```

---

### Task 5: Clean dead code — ISettingsService + SettingsService

**Covers:** [S4]

**Files:**
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Settings/ISettingsService.cs` (if exists)
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Settings/SettingsService.cs` (if exists)

**Interfaces:**
- Consumes: none
- Produces: cleaner codebase

- [ ] **Step 1: Verify no consumers**

Search for `ISettingsService` and `SettingsService` across the codebase. Confirm zero registrations in DI and zero injections.

- [ ] **Step 2: Delete the files**

- [ ] **Step 3: Build to verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Foundation/Settings/
git commit -m "refactor(desktop): remove dead ISettingsService + SettingsService"
```

---

### Task 6: Clean dead code — Foundation LocalWebApiHttpClientFactory

**Covers:** [S4]

**Files:**
- Check: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/HttpClientApiClientExtensions.cs`

**Interfaces:**
- Consumes: none
- Produces: removed duplicate class

- [ ] **Step 1: Read HttpClientApiClientExtensions.cs**

Check if `LocalWebApiHttpClientFactory` is defined here and if it's used anywhere.

- [ ] **Step 2: If unused, remove the class**

Keep the file if it has other content. Only remove the `LocalWebApiHttpClientFactory` inner class.

- [ ] **Step 3: Build to verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Foundation/Http/HttpClientApiClientExtensions.cs
git commit -m "refactor(desktop): remove duplicate LocalWebApiHttpClientFactory from Foundation"
```

---

### Task 7: Delete HttpServiceRegistrationExtensions empty shell

**Covers:** [S4]

**Files:**
- Delete: `src/Client/Desktop/Shell/Extensions/HttpServiceRegistrationExtensions.cs`

**Interfaces:**
- Consumes: none
- Produces: removed obsolete file

- [ ] **Step 1: Verify no references**

Search for `HttpServiceRegistrationExtensions` across the codebase. Confirm zero references.

- [ ] **Step 2: Delete the file**

- [ ] **Step 3: Build to verify**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Shell/Extensions/HttpServiceRegistrationExtensions.cs
git commit -m "refactor(shell): delete obsolete HttpServiceRegistrationExtensions"
```

---

### Task 8: Add tests for ReceptionistHomeViewModel

**Covers:** [S4]

**Files:**
- Create: `tests/LYBT.Tests.Desktop/Unit/Receptionist/ReceptionistHomeViewModelTests.cs`

**Interfaces:**
- Consumes: `ReceptionistHomeViewModel`
- Produces: Unit tests covering key scenarios

- [ ] **Step 1: Create test file with basic structure**

```csharp
using FluentAssertions;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Receptionist.ViewModels;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop;

public class ReceptionistHomeViewModelTests
{
    // TODO: Add tests based on ReceptionistHomeViewModel's public API
    // Focus on: initialization, command execution, state transitions
}
```

- [ ] **Step 2: Read ReceptionistHomeViewModel to identify testable behavior**

Read the VM file to understand its public API and key scenarios.

- [ ] **Step 3: Implement tests based on findings**

- [ ] **Step 4: Run tests**

Run: `dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~ReceptionistHomeViewModelTests"`
Expected: All tests pass

- [ ] **Step 5: Commit**

```bash
git add tests/LYBT.Tests.Desktop/Unit/Receptionist/
git commit -m "test(desktop): add unit tests for ReceptionistHomeViewModel"
```

---

### Task 9: Final verification

**Covers:** [S6]

- [ ] **Step 1: Full build**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeds with zero errors

- [ ] **Step 2: Run all Desktop tests**

Run: `dotnet test tests/LYBT.Tests.Desktop/ --no-build`
Expected: All tests pass

- [ ] **Step 3: Verify LoginViewModel line count reduced**

Run: `wc -l src/Client/Desktop/Modules/LYBT.Desktop.Auth/ViewModels/LoginViewModel.cs`
Expected: ~300 lines (down from 705)
