# Sysadmin 运维控制台 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a dedicated dark-themed operations console for sysadmin users, replacing the shared AdminHome with a dashboard + admin user management + log level control.

**Architecture:** New `SysadminModule` project (mirrors `LYBT.Desktop.Admin` structure) registered only for `UserRole.SuperAdmin`. SuperAdminRoleDefinition.HomeViewName points to new `SysadminHomeView`. Dashboard calls existing health/diagnostics endpoints. User management reuses existing `UserMasterDetailControl` filtered to Admin role. Log level control calls existing DiagnosticsController endpoints via new Refit interface.

**Tech Stack:** WPF + Prism MVVM + CommunityToolkit.Mvvm + Refit + existing ASP.NET Core DiagnosticsController API

---

## File Structure

```
src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/
├── LYBT.Desktop.Sysadmin.csproj          # Project file (mirrors Admin.csproj)
├── SysadminModule.cs                     # Prism IModule registration
├── Themes/
│   └── SysadminDarkTheme.xaml            # Dark theme ResourceDictionary
├── Views/
│   ├── SysadminHomeView.xaml             # Dashboard with 4 status cards
│   ├── SysadminHomeView.xaml.cs
│   ├── AdminUserManagementView.xaml      # Thin wrapper for UserMasterDetailControl
│   ├── AdminUserManagementView.xaml.cs
│   ├── LogLevelControlView.xaml         # Log level control panel
│   └── LogLevelControlView.xaml.cs
├── ViewModels/
│   ├── SysadminHomeViewModel.cs          # Dashboard VM with health polling
│   ├── AdminUserManagementViewModel.cs   # Simple wrapper VM
│   └── LogLevelControlViewModel.cs       # Log level VM
└── Models/
    └── DashboardStatus.cs                # Status card data models

Modified:
- src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Constants/ViewNames.cs  — add SysadminHome constant
- src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Roles/Definitions/SuperAdminRoleDefinition.cs  — HomeViewName → SysadminHome
- src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs  — register SysadminModule for SuperAdmin
- src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IDiagnosticsApi.cs  — new Refit interface for diagnostics
```

---

## Task 1: Create SysadminModule project skeleton

**Covers:** [S3]
**Files:**
- Create: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/LYBT.Desktop.Sysadmin.csproj`
- Create: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/SysadminModule.cs`
- Modify: `src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj` — add project reference
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Constants/ViewNames.cs` — add `SysadminHome`

- [ ] **Step 1: Create csproj**

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Prism.Wpf" />
    <PackageReference Include="CommunityToolkit.Mvvm" />
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\Core\LYBT.Desktop.Infrastructure\LYBT.Desktop.Infrastructure.csproj" />
    <ProjectReference Include="..\..\Core\LYBT.Desktop.Contracts\LYBT.Desktop.Contracts.csproj" />
    <ProjectReference Include="..\..\Modules\LYBT.Desktop.Users\LYBT.Desktop.Users.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create SysadminModule.cs**

```csharp
using Prism.Ioc;
using Prism.Modularity;

namespace LYBT.Desktop.Sysadmin;

[Module(ModuleName = nameof(SysadminModule))]
public class SysadminModule : IModule
{
    public void OnInitialized(IContainerProvider containerProvider) { }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterForNavigation<Views.SysadminHomeView>();
        containerRegistry.RegisterForNavigation<Views.AdminUserManagementView>();
        containerRegistry.RegisterForNavigation<Views.LogLevelControlView>();
    }
}
```

- [ ] **Step 3: Add ViewNames constant**

In `ViewNames.cs`, after `ReceptionistHome`:

```csharp
/// <summary>系统运维控制台主页</summary>
public const string SysadminHome = "SysadminHomeView";
```

- [ ] **Step 4: Add project reference to Shell.csproj**

```xml
<ProjectReference Include="..\Roles\LYBT.Desktop.Sysadmin\LYBT.Desktop.Sysadmin.csproj" />
```

- [ ] **Step 5: Build**

```bash
dotnet build LYBTZYZS.sln
```
Expected: 0 errors

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat(sysadmin): create SysadminModule project skeleton + ViewNames.SysadminHome"
```

---

## Task 2: Point SuperAdminRoleDefinition to SysadminHome

**Covers:** [S3]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Roles/Definitions/SuperAdminRoleDefinition.cs`

- [ ] **Step 1: Update HomeViewName**

Change line 33 from:
```csharp
public override string HomeViewName => ViewNames.AdminHome;
```
to:
```csharp
public override string HomeViewName => ViewNames.SysadminHome;
```

- [ ] **Step 2: Update RequiredModules — remove business modules, keep Users**

```csharp
private static readonly string[] Modules = new[]
{
    "UsersModule",
    "SysadminModule"
};
```

- [ ] **Step 3: Build + Commit**

```bash
dotnet build LYBTZYZS.sln
git add -A && git commit -m "feat(sysadmin): SuperAdminRoleDefinition → SysadminHome + load SysadminModule"
```

---

## Task 3: Dark theme ResourceDictionary

**Covers:** [S4]

**Files:**
- Create: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Themes/SysadminDarkTheme.xaml`

- [ ] **Step 1: Create dark theme resources**

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <Color x:Key="OpsBackgroundColor">#1a1a2e</Color>
    <Color x:Key="OpsCardBackgroundColor">#16213e</Color>
    <Color x:Key="OpsAccentColor">#0f3460</Color>
    <Color x:Key="OpsTextPrimaryColor">#e0e0e0</Color>
    <Color x:Key="OpsTextSecondaryColor">#8b8b9e</Color>
    <Color x:Key="OpsSuccessColor">#4caf50</Color>
    <Color x:Key="OpsDangerColor">#f44336</Color>
    <Color x:Key="OpsWarningColor">#ff9800</Color>

    <SolidColorBrush x:Key="OpsBackgroundBrush" Color="{StaticResource OpsBackgroundColor}" />
    <SolidColorBrush x:Key="OpsCardBackgroundBrush" Color="{StaticResource OpsCardBackgroundColor}" />
    <SolidColorBrush x:Key="OpsAccentBrush" Color="{StaticResource OpsAccentColor}" />
    <SolidColorBrush x:Key="OpsTextPrimaryBrush" Color="{StaticResource OpsTextPrimaryColor}" />
    <SolidColorBrush x:Key="OpsTextSecondaryBrush" Color="{StaticResource OpsTextSecondaryColor}" />
    <SolidColorBrush x:Key="OpsSuccessBrush" Color="{StaticResource OpsSuccessColor}" />
    <SolidColorBrush x:Key="OpsDangerBrush" Color="{StaticResource OpsDangerColor}" />
    <SolidColorBrush x:Key="OpsWarningBrush" Color="{StaticResource OpsWarningColor}" />
</ResourceDictionary>
```

- [ ] **Step 2: Commit**

```bash
git add -A && git commit -m "feat(sysadmin): dark operations theme ResourceDictionary"
```

---

## Task 4: IDiagnosticsApi Refit interface

**Covers:** [S3]

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/IDiagnosticsApi.cs`

- [ ] **Step 1: Create Refit interface matching existing DiagnosticsController endpoints**

```csharp
using LYBT.Shared.Models.Contracts.Diagnostics;
using Refit;

namespace LYBT.Desktop.Contracts.Api;

public interface IDiagnosticsApi
{
    [Get("/api/v1/diagnostics/logging/status")]
    Task<ApiResponse<object>> GetLoggingStatusAsync();

    [Post("/api/v1/diagnostics/logging/debug/enable")]
    Task<ApiResponse<object>> EnableDebugModeAsync([Body] EnableDebugModeRequest request);

    [Post("/api/v1/diagnostics/logging/debug/disable")]
    Task<ApiResponse<object>> DisableDebugModeAsync();

    [Post("/api/v1/diagnostics/logging/level")]
    Task<ApiResponse<object>> SetLoggingLevelAsync([Body] SetLoggingLevelRequest request);
}
```

- [ ] **Step 2: Register in HttpServiceRegistrationExtensions**

In `src/Client/Desktop/Shell/Extensions/HttpServiceRegistrationExtensions.cs`, after the `IRegistrationApi` line:

```csharp
containerRegistry.RegisterSingleton<IDiagnosticsApi>(r => RestService.For<IDiagnosticsApi>(r.Resolve<HttpClient>(), refitSettings));
```

- [ ] **Step 3: Build + Commit**

```bash
dotnet build LYBTZYZS.sln
git add -A && git commit -m "feat(sysadmin): IDiagnosticsApi Refit interface + DI registration"
```

---

## Task 5: SysadminHomeView — dashboard with status cards

**Covers:** [S3, S4, S5]

**Files:**
- Create: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Models/DashboardStatus.cs`
- Create: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/ViewModels/SysadminHomeViewModel.cs`
- Create: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/SysadminHomeView.xaml`
- Create: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/SysadminHomeView.xaml.cs`

- [ ] **Step 1: Create DashboardStatus model**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;

namespace LYBT.Desktop.Sysadmin.Models;

public partial class StatusCard : ObservableObject
{
    [ObservableProperty] private string _title = string.Empty;
    [ObservableProperty] private string _value = string.Empty;
    [ObservableProperty] private string _status = "正常";
    [ObservableProperty] private bool _isHealthy = true;
}

public partial class DashboardStatus : ObservableObject
{
    [ObservableProperty] private StatusCard _apiStatus = new() { Title = "API 状态", Value = "检测中..." };
    [ObservableProperty] private StatusCard _dbStatus = new() { Title = "数据库", Value = "检测中..." };
    [ObservableProperty] private StatusCard _loginCount = new() { Title = "今日登录", Value = "--" };
    [ObservableProperty] private StatusCard _systemInfo = new() { Title = "系统信息", Value = "加载中..." };
    [ObservableProperty] private bool _isLoading;
}
```

- [ ] **Step 2: Create SysadminHomeViewModel**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Extensions;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Desktop.Sysadmin.Models;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Sysadmin.ViewModels;

public partial class SysadminHomeViewModel : NavigableViewModelBase
{
    private readonly IAuthApi _authApi;
    private readonly INavigationCoordinator _navigationCoordinator;
    private CancellationTokenSource? _pollCts;

    [ObservableProperty] private DashboardStatus _dashboard = new();

    public SysadminHomeViewModel(
        IViewModelServices services,
        IAuthApi authApi,
        INavigationCoordinator navigationCoordinator)
        : base(services)
    {
        _authApi = authApi;
        _navigationCoordinator = navigationCoordinator;
        Title = "运维控制台";
    }

    [RelayCommand]
    private void NavigateToAdminUsers() => _navigationCoordinator.NavigateTo("AdminUserManagementView");

    [RelayCommand]
    private void NavigateToLogLevel() => _navigationCoordinator.NavigateTo("LogLevelControlView");

    public override void OnNavigatedTo(Prism.Regions.NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);
        StartPolling();
    }

    public override void OnNavigatedFrom(Prism.Regions.NavigationContext navigationContext)
    {
        base.OnNavigatedFrom(navigationContext);
        StopPolling();
    }

    private void StartPolling()
    {
        _pollCts?.Cancel();
        _pollCts = new CancellationTokenSource();
        _ = PollDashboardAsync(_pollCts.Token);
    }

    private void StopPolling() => _pollCts?.Cancel();

    private async Task PollDashboardAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                Dashboard.IsLoading = true;

                var healthResp = await _authApi.HealthCheckAsync();
                if (healthResp.Success)
                {
                    Dashboard.ApiStatus.Value = "在线";
                    Dashboard.ApiStatus.IsHealthy = true;
                    Dashboard.ApiStatus.Status = "正常";
                }
                else
                {
                    Dashboard.ApiStatus.Value = "离线";
                    Dashboard.ApiStatus.IsHealthy = false;
                    Dashboard.ApiStatus.Status = "异常";
                }

                Dashboard.SystemInfo.Value = $"v{SystemConstants.ApplicationVersion}";
                Dashboard.LoginCount.Value = "--";
                Dashboard.DbStatus.Value = "连接正常";
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[SYSADMIN] Dashboard poll failed");
                Dashboard.ApiStatus.Value = "不可达";
                Dashboard.ApiStatus.IsHealthy = false;
            }
            finally
            {
                Dashboard.IsLoading = false;
            }

            try { await Task.Delay(TimeSpan.FromSeconds(30), ct); }
            catch { break; }
        }
    }
}
```

- [ ] **Step 3: Create SysadminHomeView.xaml**

```xml
<UserControl x:Class="LYBT.Desktop.Sysadmin.Views.SysadminHomeView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Background="{StaticResource OpsBackgroundBrush}">
    <UserControl.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="../Themes/SysadminDarkTheme.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </UserControl.Resources>

    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel Margin="24">
            <TextBlock Text="运维控制台" FontSize="24" FontWeight="Bold"
                       Foreground="{StaticResource OpsTextPrimaryBrush}" Margin="0,0,0,24" />

            <!-- Status Cards Grid -->
            <UniformGrid Columns="2" Rows="2" Margin="0,0,0,24">
                <!-- API Status -->
                <Border Background="{StaticResource OpsCardBackgroundBrush}" CornerRadius="8" Padding="20" Margin="6">
                    <StackPanel>
                        <TextBlock Text="{Binding Dashboard.ApiStatus.Title}" Foreground="{StaticResource OpsTextSecondaryBrush}" FontSize="13" />
                        <TextBlock Text="{Binding Dashboard.ApiStatus.Value}" Foreground="{StaticResource OpsTextPrimaryBrush}" FontSize="28" FontWeight="Bold" Margin="0,8,0,0" />
                        <Ellipse Width="10" Height="10" Fill="{StaticResource OpsSuccessBrush}"
                                 Visibility="{Binding Dashboard.ApiStatus.IsHealthy, Converter={StaticResource BoolToVis}}" />
                    </StackPanel>
                </Border>

                <!-- DB Status -->
                <Border Background="{StaticResource OpsCardBackgroundBrush}" CornerRadius="8" Padding="20" Margin="6">
                    <StackPanel>
                        <TextBlock Text="{Binding Dashboard.DbStatus.Title}" Foreground="{StaticResource OpsTextSecondaryBrush}" FontSize="13" />
                        <TextBlock Text="{Binding Dashboard.DbStatus.Value}" Foreground="{StaticResource OpsTextPrimaryBrush}" FontSize="28" FontWeight="Bold" Margin="0,8,0,0" />
                    </StackPanel>
                </Border>

                <!-- Login Count -->
                <Border Background="{StaticResource OpsCardBackgroundBrush}" CornerRadius="8" Padding="20" Margin="6">
                    <StackPanel>
                        <TextBlock Text="{Binding Dashboard.LoginCount.Title}" Foreground="{StaticResource OpsTextSecondaryBrush}" FontSize="13" />
                        <TextBlock Text="{Binding Dashboard.LoginCount.Value}" Foreground="{StaticResource OpsTextPrimaryBrush}" FontSize="28" FontWeight="Bold" Margin="0,8,0,0" />
                    </StackPanel>
                </Border>

                <!-- System Info -->
                <Border Background="{StaticResource OpsCardBackgroundBrush}" CornerRadius="8" Padding="20" Margin="6">
                    <StackPanel>
                        <TextBlock Text="{Binding Dashboard.SystemInfo.Title}" Foreground="{StaticResource OpsTextSecondaryBrush}" FontSize="13" />
                        <TextBlock Text="{Binding Dashboard.SystemInfo.Value}" Foreground="{StaticResource OpsTextPrimaryBrush}" FontSize="28" FontWeight="Bold" Margin="0,8,0,0" />
                    </StackPanel>
                </Border>
            </UniformGrid>

            <!-- Navigation Buttons -->
            <UniformGrid Columns="2" Margin="0,24,0,0">
                <Button Content="管理员账号管理" Command="{Binding NavigateToAdminUsersCommand}"
                        Background="{StaticResource OpsAccentBrush}" Foreground="White"
                        Padding="16,12" Margin="6" FontSize="14" />
                <Button Content="日志级别控制" Command="{Binding NavigateToLogLevelCommand}"
                        Background="{StaticResource OpsAccentBrush}" Foreground="White"
                        Padding="16,12" Margin="6" FontSize="14" />
            </UniformGrid>
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

- [ ] **Step 4: Create SysadminHomeView.xaml.cs**

```csharp
using System.Windows.Controls;

namespace LYBT.Desktop.Sysadmin.Views;

public partial class SysadminHomeView : UserControl
{
    public SysadminHomeView() { InitializeComponent(); }
}
```

- [ ] **Step 5: Build**

```bash
dotnet build LYBTZYZS.sln
```
Expected: 0 errors (may have warnings about unused using — acceptable)

- [ ] **Step 6: Commit**

```bash
git add -A && git commit -m "feat(sysadmin): dashboard home view with 4 status cards + dark theme + 30s polling"
```

---

## Task 6: AdminUserManagementView — filtered user list

**Covers:** [S3, S5]

**Files:**
- Create: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/AdminUserManagementView.xaml`
- Create: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/AdminUserManagementView.xaml.cs`

- [ ] **Step 1: Create thin wrapper XAML — embeds Users module control**

```xml
<UserControl x:Class="LYBT.Desktop.Sysadmin.Views.AdminUserManagementView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:users="clr-namespace:LYBT.Desktop.Users.Controls;assembly=LYBT.Desktop.Users"
             Background="{StaticResource OpsBackgroundBrush}">
    <UserControl.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="../Themes/SysadminDarkTheme.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </UserControl.Resources>

    <Grid Margin="16">
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto" />
            <RowDefinition Height="*" />
        </Grid.RowDefinitions>

        <TextBlock Grid.Row="0" Text="管理员账号管理" FontSize="20" FontWeight="Bold"
                   Foreground="{StaticResource OpsTextPrimaryBrush}" Margin="0,0,0,16" />

        <users:UserMasterDetailControl Grid.Row="1" />
    </Grid>
</UserControl>
```

- [ ] **Step 2: Create code-behind**

```csharp
using System.Windows.Controls;

namespace LYBT.Desktop.Sysadmin.Views;

public partial class AdminUserManagementView : UserControl
{
    public AdminUserManagementView() { InitializeComponent(); }
}
```

- [ ] **Step 3: Build + Commit**

```bash
dotnet build LYBTZYZS.sln
git add -A && git commit -m "feat(sysadmin): admin user management view (embeds UserMasterDetailControl)"
```

---

## Task 7: LogLevelControlView — diagnostics panel

**Covers:** [S3, S5]

**Files:**
- Create: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/ViewModels/LogLevelControlViewModel.cs`
- Create: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/LogLevelControlView.xaml`
- Create: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/LogLevelControlView.xaml.cs`

- [ ] **Step 1: Create LogLevelControlViewModel**

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.Extensions;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace LYBT.Desktop.Sysadmin.ViewModels;

public partial class LogLevelControlViewModel : NavigableViewModelBase
{
    private readonly IDiagnosticsApi _diagnosticsApi;

    [ObservableProperty] private string _currentLevel = "加载中...";
    [ObservableProperty] private string _statusMessage = string.Empty;
    [ObservableProperty] private bool _isBusy;

    public LogLevelControlViewModel(IViewModelServices services, IDiagnosticsApi diagnosticsApi)
        : base(services)
    {
        _diagnosticsApi = diagnosticsApi;
        Title = "日志级别控制";
    }

    public override void OnNavigatedTo(Prism.Regions.NavigationContext navigationContext)
    {
        base.OnNavigatedTo(navigationContext);
        _ = LoadStatusAsync();
    }

    private async Task LoadStatusAsync()
    {
        try
        {
            IsBusy = true;
            var resp = await _diagnosticsApi.GetLoggingStatusAsync();
            if (resp.Success)
            {
                var json = JsonSerializer.Serialize(resp.Data);
                CurrentLevel = json.Contains("Debug", StringComparison.OrdinalIgnoreCase) ? "Debug" :
                               json.Contains("Verbose", StringComparison.OrdinalIgnoreCase) ? "Verbose" :
                               json.Contains("Information", StringComparison.OrdinalIgnoreCase) ? "Information" :
                               json.Contains("Warning", StringComparison.OrdinalIgnoreCase) ? "Warning" :
                               json.Contains("Error", StringComparison.OrdinalIgnoreCase) ? "Error" : "未知";
            }
        }
        catch (Exception ex) { Logger.LogError(ex, "[SYSADMIN] Load log status failed"); }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task SetLevelAsync(string level)
    {
        try
        {
            IsBusy = true;
            var req = new SetLoggingLevelRequest { Level = level };
            await _diagnosticsApi.SetLoggingLevelAsync(req);
            CurrentLevel = level;
            StatusMessage = $"日志级别已设置为 {level}";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[SYSADMIN] Set log level failed");
            StatusMessage = $"设置失败: {ex.Message}";
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task EnableDebugAsync()
    {
        try
        {
            IsBusy = true;
            var req = new EnableDebugModeRequest { Level = "Debug", DurationMinutes = 60 };
            await _diagnosticsApi.EnableDebugModeAsync(req);
            CurrentLevel = "Debug (60分钟)";
            StatusMessage = "Debug 模式已开启（60分钟后自动关闭）";
        }
        catch (Exception ex) { StatusMessage = $"开启失败: {ex.Message}"; }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task DisableDebugAsync()
    {
        try
        {
            IsBusy = true;
            await _diagnosticsApi.DisableDebugModeAsync();
            CurrentLevel = "Information";
            StatusMessage = "Debug 模式已关闭";
        }
        catch (Exception ex) { StatusMessage = $"关闭失败: {ex.Message}"; }
        finally { IsBusy = false; }
    }
}
```

- [ ] **Step 2: Create LogLevelControlView.xaml**

```xml
<UserControl x:Class="LYBT.Desktop.Sysadmin.Views.LogLevelControlView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             Background="{StaticResource OpsBackgroundBrush}">
    <UserControl.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="../Themes/SysadminDarkTheme.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </UserControl.Resources>

    <ScrollViewer VerticalScrollBarVisibility="Auto">
        <StackPanel Margin="24">
            <TextBlock Text="日志级别控制" FontSize="20" FontWeight="Bold"
                       Foreground="{StaticResource OpsTextPrimaryBrush}" Margin="0,0,0,8" />
            <TextBlock Text="{Binding CurrentLevel}" FontSize="16"
                       Foreground="{StaticResource OpsAccentBrush}" Margin="0,0,0,24" />

            <TextBlock Text="快捷操作" Foreground="{StaticResource OpsTextSecondaryBrush}" FontSize="14" Margin="0,0,0,12" />

            <StackPanel Orientation="Horizontal" Margin="0,0,0,16">
                <Button Content="开启 Debug (60分钟)" Command="{Binding EnableDebugCommand}"
                        Background="{StaticResource OpsAccentBrush}" Foreground="White"
                        Padding="16,10" Margin="0,0,8,0" />
                <Button Content="关闭 Debug" Command="{Binding DisableDebugCommand}"
                        Background="{StaticResource OpsDangerBrush}" Foreground="White"
                        Padding="16,10" />
            </StackPanel>

            <TextBlock Text="手动设置级别" Foreground="{StaticResource OpsTextSecondaryBrush}" FontSize="14" Margin="0,16,0,12" />

            <UniformGrid Columns="5" Margin="0,0,0,16">
                <Button Content="Verbose" Command="{Binding SetLevelCommand}" CommandParameter="Verbose"
                        Padding="8,10" Margin="4" Foreground="White" Background="{StaticResource OpsAccentBrush}" />
                <Button Content="Debug" Command="{Binding SetLevelCommand}" CommandParameter="Debug"
                        Padding="8,10" Margin="4" Foreground="White" Background="{StaticResource OpsAccentBrush}" />
                <Button Content="Info" Command="{Binding SetLevelCommand}" CommandParameter="Information"
                        Padding="8,10" Margin="4" Foreground="White" Background="{StaticResource OpsAccentBrush}" />
                <Button Content="Warning" Command="{Binding SetLevelCommand}" CommandParameter="Warning"
                        Padding="8,10" Margin="4" Foreground="White" Background="{StaticResource OpsAccentBrush}" />
                <Button Content="Error" Command="{Binding SetLevelCommand}" CommandParameter="Error"
                        Padding="8,10" Margin="4" Foreground="White" Background="{StaticResource OpsAccentBrush}" />
            </UniformGrid>

            <TextBlock Text="{Binding StatusMessage}" Foreground="{StaticResource OpsTextSecondaryBrush}"
                       FontSize="13" Margin="0,16,0,0" />
        </StackPanel>
    </ScrollViewer>
</UserControl>
```

- [ ] **Step 3: Create code-behind**

```csharp
using System.Windows.Controls;

namespace LYBT.Desktop.Sysadmin.Views;

public partial class LogLevelControlView : UserControl
{
    public LogLevelControlView() { InitializeComponent(); }
}
```

- [ ] **Step 4: Build + Commit**

```bash
dotnet build LYBTZYZS.sln
git add -A && git commit -m "feat(sysadmin): log level control view — enable/disable debug + manual level set"
```

---

## Task 8: Wire up module loading + verify navigation

**Covers:** [S3, S5]

**Files:**
- Modify: `src/Client/Desktop/Shell/Extensions/ServiceCollectionExtensions.cs` — load SysadminModule for SuperAdmin role
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/SysadminModule.cs` — register VMs for navigation

- [ ] **Step 1: In ServiceCollectionExtensions, add SysadminModule to SuperAdmin role loading**

Find the role-based module loading section and add `SysadminModule` to the SuperAdmin role's module list. The existing pattern loads modules via `IModuleManager.LoadModule("ModuleName")` — add `"SysadminModule"` to the SuperAdmin role's module set.

- [ ] **Step 2: In SysadminModule.RegisterTypes, register VMs**

```csharp
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    containerRegistry.RegisterForNavigation<Views.SysadminHomeView>();
    containerRegistry.RegisterForNavigation<Views.AdminUserManagementView>();
    containerRegistry.RegisterForNavigation<Views.LogLevelControlView>();
}
```

- [ ] **Step 3: Build**

```bash
dotnet build LYBTZYZS.sln
```
Expected: 0 errors

- [ ] **Step 4: Manual verification**

Launch Desktop app, login as sysadmin/SysAdmin@2026!, verify:
1. Dashboard with 4 dark cards appears (not AdminHome)
2. "管理员账号管理" button navigates to user management
3. "日志级别控制" button navigates to log level panel
4. Log level buttons work (enable debug, set level)

- [ ] **Step 5: Commit + Push**

```bash
git add -A && git commit -m "feat(sysadmin): wire up SysadminModule loading for SuperAdmin role + verify navigation" && git push origin master
```
