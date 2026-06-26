# Sysadmin Dashboard 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 完善 Sysadmin 模块的 Dashboard 和用户管理功能，包括版本号统一、状态卡片修复、诊所信息配置页面、用户管理过滤。

**Architecture:** 
- Phase 0: 版本号统一为单一来源（Directory.Build.props）
- Phase 1: Dashboard 4 卡片使用真实数据源
- Phase 2: 诊所信息配置独立页面
- Phase 3: 用户管理增加角色过滤和 sysadmin 保护

**Tech Stack:** .NET 8, WPF/Prism, CommunityToolkit.Mvvm, IClinicSettingsService, IConnectionModeService

---

## File Structure

| 操作 | 文件 | 职责 |
|------|------|------|
| Modify | `Directory.Build.props` | 版本号单一来源 |
| Modify | `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Constants/SystemConstants.cs` | 运行时读取版本 |
| Modify | `src/Client/Desktop/LocalWebAPI/Controllers/HealthController.cs` | 移除硬编码版本 |
| Modify | `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Models/DashboardStatus.cs` | 重命名卡片 |
| Modify | `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/ViewModels/SysadminHomeViewModel.cs` | 注入新服务 + 导航 |
| Modify | `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/SysadminHomeView.xaml` | MDIX 主题 + 绑定 |
| Modify | `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/LogLevelControlView.xaml` | MDIX 主题 |
| Modify | `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Themes/SysadminDarkTheme.xaml` | MDIX 资源映射 |
| Reuse | `src/Client/Desktop/Roles/LYBT.Desktop.Admin/Views/SystemSettingsView.xaml` | 诊所配置（已有） |

---

## Task 1: 版本号统一

**Covers:** [S1] 问题陈述 - 版本号混乱

**Files:**
- Modify: `Directory.Build.props:54`
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Constants/SystemConstants.cs:21`
- Modify: `src/Client/Desktop/LocalWebAPI/Controllers/HealthController.cs:107`

- [ ] **Step 1: 修改 Directory.Build.props 版本号**

```xml
<!-- 修改前 -->
<VersionPrefix>2.1.0</VersionPrefix>

<!-- 修改后 -->
<VersionPrefix>1.0.0</VersionPrefix>
```

- [ ] **Step 2: 修改 SystemConstants 使用运行时版本**

```csharp
// 修改前
public const string ApplicationVersion = "2.0.0";

// 修改后 - 使用程序集版本
public static string ApplicationVersion => 
    System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
```

- [ ] **Step 3: 修改 HealthController 移除硬编码版本**

```csharp
// 修改前
version = "1.0.0-local",

// 修改后 - 使用 SystemConstants
version = SystemConstants.ApplicationVersion,
```

需要添加 using: `using LYBT.Infrastructure.Constants;`

- [ ] **Step 4: 验证版本号一致**

Run: `dotnet build LYBTZYZS.sln`
Expected: BUILD SUCCESSFUL

- [ ] **Step 5: 提交**

```bash
git add Directory.Build.props src/Client/Desktop/Core/LYBT.Desktop.Infrastructure/Constants/SystemConstants.cs src/Client/Desktop/LocalWebAPI/Controllers/HealthController.cs
git commit -m "refactor: unify version to single source (1.0.0)"
```

---

## Task 2: Dashboard 卡片重命名

**Covers:** [S3] Dashboard 详细设计 - 卡片重新定义

**Files:**
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Models/DashboardStatus.cs`

- [ ] **Step 1: 修改 DashboardStatus 模型**

```csharp
// 修改前
[ObservableProperty]
private StatusCard _loginCount = new() { Title = "今日登录", Value = "--" };

// 修改后
[ObservableProperty]
private StatusCard _connectionMode = new() { Title = "连接模式", Value = "检测中..." };
```

- [ ] **Step 2: 验证编译**

Run: `dotnet build src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/LYBT.Desktop.Sysadmin.csproj`
Expected: BUILD FAILED (因为 ViewModel 和 View 尚未修改，这是预期行为)

- [ ] **Step 3: 提交**

```bash
git add src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Models/DashboardStatus.cs
git commit -m "refactor: rename LoginCount card to ConnectionMode"
```

---

## Task 3: Dashboard ViewModel 注入新服务

**Covers:** [S3] Dashboard 详细设计 - 数据源映射

**Files:**
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/ViewModels/SysadminHomeViewModel.cs`

- [ ] **Step 1: 添加新的依赖注入**

```csharp
// 添加 using
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Interfaces;
using Prism.Regions;

// 添加字段
private readonly IConnectionModeService _connectionModeService;
private readonly IConnectionSettingsService _connectionSettings;
private readonly IClinicSettingsService _clinicSettings;

// 修改构造函数
public SysadminHomeViewModel(
    IViewModelServices services,
    IAuthApi authApi,
    INavigationCoordinator navigationCoordinator,
    IConnectionModeService connectionModeService,
    IConnectionSettingsService connectionSettings,
    IClinicSettingsService clinicSettings)
    : base(services)
{
    _authApi = authApi;
    _navigationCoordinator = navigationCoordinator;
    _connectionModeService = connectionModeService;
    _connectionSettings = connectionSettings;
    _clinicSettings = clinicSettings;
    PageTitle = "运维控制台";
}
```

- [ ] **Step 2: 修改 PollDashboardAsync 方法**

```csharp
private async Task PollDashboardAsync(CancellationToken ct)
{
    var isFirstLoad = true;
    while (!ct.IsCancellationRequested)
    {
        try
        {
            if (isFirstLoad) Dashboard.IsLoading = true;

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

            // 连接模式卡片
            Dashboard.ConnectionMode.Value = _connectionModeService.CurrentModeDisplay;
            Dashboard.ConnectionMode.IsHealthy = true;
            Dashboard.ConnectionMode.Status = _connectionSettings.CurrentUrl;

            // 数据库状态卡片（通过 API 间接判断）
            if (healthResp.Success)
            {
                Dashboard.DbStatus.Value = "连接正常";
                Dashboard.DbStatus.IsHealthy = true;
                Dashboard.DbStatus.Status = "正常";
            }
            else
            {
                Dashboard.DbStatus.Value = "连接异常";
                Dashboard.DbStatus.IsHealthy = false;
                Dashboard.DbStatus.Status = "异常";
            }

            // 系统信息卡片
            Dashboard.SystemInfo.Value = $"v{SystemConstants.ApplicationVersion}";
            Dashboard.SystemInfo.Status = _clinicSettings.ClinicName;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "[SYSADMIN] Dashboard poll failed");
            Dashboard.ApiStatus.Value = "不可达";
            Dashboard.ApiStatus.IsHealthy = false;
        }
        finally
        {
            if (isFirstLoad) { Dashboard.IsLoading = false; isFirstLoad = false; }
        }

        try { await Task.Delay(TimeSpan.FromSeconds(30), ct); }
        catch { break; }
    }
}
```

- [ ] **Step 3: 添加诊所配置导航命令**

```csharp
[RelayCommand]
private void NavigateToClinicSettings() => _navigationCoordinator.NavigateTo("SystemSettingsView");
```

- [ ] **Step 4: 验证编译**

Run: `dotnet build src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/LYBT.Desktop.Sysadmin.csproj`
Expected: BUILD SUCCESSFUL

- [ ] **Step 5: 提交**

```bash
git add src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/ViewModels/SysadminHomeViewModel.cs
git commit -m "feat: inject ConnectionModeService and ClinicSettingsService into dashboard"
```

---

## Task 4: Dashboard View 更新绑定

**Covers:** [S3] Dashboard 详细设计 - SysadminHomeView.xaml

**Files:**
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/SysadminHomeView.xaml`

- [ ] **Step 1: 更新卡片绑定路径**

```xml
<!-- 卡片 2: 连接模式（替换今日登录） -->
<Border Background="{StaticResource OpsCardBackgroundBrush}" CornerRadius="8" Padding="20" Margin="6">
    <StackPanel>
        <TextBlock Text="{Binding Dashboard.ConnectionMode.Title}" Foreground="{StaticResource OpsTextSecondaryBrush}" FontSize="13" />
        <TextBlock Text="{Binding Dashboard.ConnectionMode.Value}" Foreground="{StaticResource OpsTextPrimaryBrush}" FontSize="28" FontWeight="Bold" Margin="0,8,0,0" />
        <TextBlock Text="{Binding Dashboard.ConnectionMode.Status}" Foreground="{StaticResource OpsTextSecondaryBrush}" FontSize="11" Margin="0,4,0,0" TextTrimming="CharacterEllipsis" />
    </StackPanel>
</Border>

<!-- 卡片 3: 数据库状态 -->
<Border Background="{StaticResource OpsCardBackgroundBrush}" CornerRadius="8" Padding="20" Margin="6">
    <StackPanel>
        <TextBlock Text="{Binding Dashboard.DbStatus.Title}" Foreground="{StaticResource OpsTextSecondaryBrush}" FontSize="13" />
        <TextBlock Text="{Binding Dashboard.DbStatus.Value}" Foreground="{StaticResource OpsTextPrimaryBrush}" FontSize="28" FontWeight="Bold" Margin="0,8,0,0" />
        <Ellipse Width="10" Height="10" Fill="{StaticResource OpsSuccessBrush}" Margin="0,8,0,0"
                 Visibility="{Binding Dashboard.DbStatus.IsHealthy, Converter={x:Static converters:Cvt.BoolToVis}}" />
    </StackPanel>
</Border>

<!-- 卡片 4: 系统信息 -->
<Border Background="{StaticResource OpsCardBackgroundBrush}" CornerRadius="8" Padding="20" Margin="6">
    <StackPanel>
        <TextBlock Text="{Binding Dashboard.SystemInfo.Title}" Foreground="{StaticResource OpsTextSecondaryBrush}" FontSize="13" />
        <TextBlock Text="{Binding Dashboard.SystemInfo.Value}" Foreground="{StaticResource OpsTextPrimaryBrush}" FontSize="28" FontWeight="Bold" Margin="0,8,0,0" />
        <TextBlock Text="{Binding Dashboard.SystemInfo.Status}" Foreground="{StaticResource OpsTextSecondaryBrush}" FontSize="11" Margin="0,4,0,0" TextTrimming="CharacterEllipsis" />
    </StackPanel>
</Border>
```

- [ ] **Step 2: 添加诊所配置导航按钮**

```xml
<!-- 导航按钮区域 -->
<UniformGrid Columns="2" Margin="0,24,0,0">
    <Button Content="管理员账号管理" Command="{Binding NavigateToAdminUsersCommand}"
            Background="{StaticResource OpsAccentBrush}" Foreground="White"
            Padding="16,12" Margin="6" FontSize="14" />
    <Button Content="诊所信息配置" Command="{Binding NavigateToClinicSettingsCommand}"
            Background="{StaticResource OpsAccentBrush}" Foreground="White"
            Padding="16,12" Margin="6" FontSize="14" />
    <Button Content="日志级别控制" Command="{Binding NavigateToLogLevelCommand}"
            Background="{StaticResource OpsAccentBrush}" Foreground="White"
            Padding="16,12" Margin="6" FontSize="14" />
</UniformGrid>
```

- [ ] **Step 3: 验证编译**

Run: `dotnet build src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/LYBT.Desktop.Sysadmin.csproj`
Expected: BUILD SUCCESSFUL

- [ ] **Step 4: 提交**

```bash
git add src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/SysadminHomeView.xaml
git commit -m "feat: update dashboard cards and add clinic settings navigation"
```

---

## Task 5: 用户管理角色过滤

**Covers:** [S4] 用户管理详细设计 - 角色过滤

**Files:**
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/AdminUserManagementView.xaml.cs`
- Modify: `src/Client/Desktop/Modules/LYBT.Desktop.Users/Controls/UserMasterDetailControl.xaml.cs`

- [ ] **Step 1: UserMasterDetailControl 添加设置默认过滤方法**

```csharp
// 在 UserMasterDetailControl.xaml.cs 中添加
public void SetDefaultRoleFilter(UserRole role)
{
    if (DataContext is UserMasterDetailViewModel vm)
    {
        vm.SelectedRoleFilter = role;
    }
}
```

需要添加 using: `using LYBT.Shared.Models.Enums;`

- [ ] **Step 2: AdminUserManagementView 接收导航参数**

首先修改 XAML 添加 x:Name：

```xml
<!-- 在 AdminUserManagementView.xaml 中 -->
<users:UserMasterDetailControl x:Name="UserMasterDetailControl" Grid.Row="1" />
```

然后修改代码隐藏：

```csharp
// 修改 AdminUserManagementView.xaml.cs
using System.Windows.Controls;
using LYBT.Shared.Models.Enums;
using Prism.Regions;

namespace LYBT.Desktop.Sysadmin.Views;

public partial class AdminUserManagementView : UserControl, INavigationAware
{
    public AdminUserManagementView()
    {
        InitializeComponent();
    }

    public void OnNavigatedTo(NavigationContext navigationContext)
    {
        if (navigationContext.Parameters.ContainsKey("DefaultRoleFilter"))
        {
            var role = (UserRole)navigationContext.Parameters["DefaultRoleFilter"];
            UserMasterDetailControl.SetDefaultRoleFilter(role);
        }
    }

    public bool IsNavigationTarget(NavigationContext navigationContext) => true;

    public void OnNavigatedFrom(NavigationContext navigationContext) { }
}
```

- [ ] **Step 3: SysadminHomeViewModel 导航时传递参数**

```csharp
// 修改 NavigateToAdminUsers 方法
[RelayCommand]
private void NavigateToAdminUsers()
{
    var parameters = new Prism.Regions.NavigationParameters
    {
        { "DefaultRoleFilter", UserRole.Admin }
    };
    _navigationCoordinator.NavigateTo("AdminUserManagementView", parameters);
}
```

需要添加 using: `using LYBT.Shared.Models.Enums;`

- [ ] **Step 4: 验证编译**

Run: `dotnet build LYBTZYZS.sln`
Expected: BUILD SUCCESSFUL

- [ ] **Step 5: 提交**

```bash
git add src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/AdminUserManagementView.xaml.cs src/Client/Desktop/Modules/LYBT.Desktop.Users/Controls/UserMasterDetailControl.xaml.cs src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/ViewModels/SysadminHomeViewModel.cs
git commit -m "feat: add default Admin role filter for sysadmin user management"
```

---

## Task 7: MDIX 主题迁移

**Covers:** [S3.0] 主题一致性 - 使用 MDIX 暗色主题

**Files:**
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Themes/SysadminDarkTheme.xaml`
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/SysadminHomeView.xaml`
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/LogLevelControlView.xaml`

**根因:** SysadminDarkTheme.xaml 定义了自定义颜色，与 MDIX 主题系统脱节。MDIX 已内置 Dark 主题支持，应通过 `PaletteHelper.SetTheme()` 切换，而非自定义颜色。

- [ ] **Step 1: 更新 SysadminDarkTheme.xaml 使用 MDIX 资源**

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                    xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes">

    <!-- 运维控制台暗色主题 - 使用 MDIX Dark 主题资源 -->
    <SolidColorBrush x:Key="OpsBackgroundBrush" Color="{DynamicResource MaterialDesignPaper}" />
    <SolidColorBrush x:Key="OpsCardBackgroundBrush" Color="{DynamicResource MaterialDesignBody}" />
    <SolidColorBrush x:Key="OpsAccentBrush" Color="{DynamicResource MaterialDesignFlatButton.ClickThrough}" />
    <SolidColorBrush x:Key="OpsTextPrimaryBrush" Color="{DynamicResource MaterialDesignBody}" />
    <SolidColorBrush x:Key="OpsTextSecondaryBrush" Color="{DynamicResource MaterialDesignBodyLight}" />
    <SolidColorBrush x:Key="OpsSuccessBrush" Color="{DynamicResource MaterialDesignBrush.Success}" />
    <SolidColorBrush x:Key="OpsDangerBrush" Color="{DynamicResource MaterialDesignBrush.Error}" />
    <SolidColorBrush x:Key="OpsWarningBrush" Color="{DynamicResource MaterialDesignBrush.Warning}" />

</ResourceDictionary>
```

- [ ] **Step 2: 更新 SysadminHomeView.xaml 使用 MDIX 样式**

关键变更：
1. 添加 `materialDesign:DialogHost` 包装
2. 背景改为 `DynamicResource MaterialDesignPaper`
3. 文字改为 `DynamicResource MaterialDesignBody` / `MaterialDesignBodyLight`
4. 按钮添加 `Style="{StaticResource MaterialDesignFlatMidBgButton}"`

- [ ] **Step 3: 更新 LogLevelControlView.xaml 使用 MDIX 样式**

同样变更：
1. 添加 `materialDesign:DialogHost` 包装
2. 按钮使用 MDIX 样式
3. 文字使用 MDIX 资源

- [ ] **Step 4: 验证编译**

Run: `dotnet build src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/LYBT.Desktop.Sysadmin.csproj`
Expected: BUILD SUCCESSFUL

- [ ] **Step 5: 提交**

```bash
git add src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/
git commit -m "feat: migrate sysadmin views to MDIX dark theme resources"
```

---

## Task 8: 完整构建验证

**Covers:** 所有任务的集成验证

- [ ] **Step 1: 完整解决方案构建**

Run: `dotnet build LYBTZYZS.sln`
Expected: BUILD SUCCESSFUL

- [ ] **Step 2: 运行架构测试**

Run: `dotnet test tests/LYBT.Tests.Architecture/`
Expected: ALL TESTS PASS

- [ ] **Step 3: 提交最终版本**

```bash
git add -A
git commit -m "feat: complete sysadmin dashboard and user management improvements"
```

---

## 变更日志

| 日期 | 版本 | 变更 |
|------|------|------|
| 2026-06-24 | v1.0 | 初始实施计划 |
