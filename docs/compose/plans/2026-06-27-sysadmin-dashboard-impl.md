# Sysadmin Dashboard 实施计划

> **For agentic workers:** Use compose:subagent or compose:execute to implement this plan.

**Goal:** 用 MDIX 原生组件重新设计 Sysadmin Dashboard，修复绑定错误，导航统一到侧边栏。

**Architecture:** 重写 XAML（MDIX Card/PackIcon/TextBlock），清理 ViewModel（移除导航命令和冗余依赖），更新侧边栏导航项。

**Tech Stack:** .NET 8, WPF, MaterialDesignInXamlToolkit (MDIX), Prism

## Global Constraints

- MDIX 原生优先：用 Card/PackIcon/MaterialDesign*TextBlock，不用旧自定义样式
- 颜色用 DynamicResource（MaterialDesign.Brush.*），确保 Dark 主题适配
- 不修改工具栏和状态栏
- **执行 Plan 必须经用户确认**

---

## Task 1: 重写 SysadminHomeView.xaml

**Covers:** [S3.1] Dashboard 内容区重新设计

**Files:**
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/SysadminHomeView.xaml`

- [ ] **Step 1: 用 MDIX 原生组件重写 XAML**

完整替换为：

```xml
<UserControl x:Class="LYBT.Desktop.Sysadmin.Views.SysadminHomeView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             prism:ViewModelLocator.AutoWireViewModel="True"
             xmlns:prism="http://prismlibrary.com/">

    <materialDesign:DialogHost>
        <ScrollViewer VerticalScrollBarVisibility="Auto"
                      Background="{DynamicResource MaterialDesign.Brush.Background}">
            <StackPanel Margin="32">

                <!-- 标题区 -->
                <TextBlock Style="{StaticResource MaterialDesignHeadline5TextBlock}"
                           Text="运维控制台" Margin="0,0,0,4" />
                <TextBlock Style="{StaticResource MaterialDesignBody2TextBlock}"
                           Text="凌隐宝堂中医诊所 · 系统运维"
                           Opacity="0.6" Margin="0,0,0,32" />

                <!-- 状态卡片 -->
                <UniformGrid Columns="2">

                    <!-- 数据库状态卡片 -->
                    <materialDesign:Card Margin="8" Padding="24"
                                         UniformCornerRadius="16"
                                         materialDesign:ElevationAssist.Elevation="Dp2">
                        <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
                            <materialDesign:PackIcon Kind="Database"
                                                      Width="48" Height="48"
                                                      HorizontalAlignment="Center"
                                                      Margin="0,0,0,16"
                                                      Foreground="{DynamicResource MaterialDesign.Brush.Primary}" />
                            <TextBlock Style="{StaticResource MaterialDesignHeadline6TextBlock}"
                                       Text="数据库"
                                       HorizontalAlignment="Center" />
                            <StackPanel Orientation="Horizontal" HorizontalAlignment="Center" Margin="0,8,0,0">
                                <materialDesign:PackIcon Kind="CheckCircle"
                                                          Width="16" Height="16"
                                                          Margin="0,0,4,0"
                                                          Foreground="{DynamicResource MaterialDesign.Brush.Primary}" />
                                <TextBlock Style="{StaticResource MaterialDesignBody2TextBlock}"
                                           Text="{Binding Dashboard.DbStatus.Value}" />
                            </StackPanel>
                        </StackPanel>
                    </materialDesign:Card>

                    <!-- 系统信息卡片 -->
                    <materialDesign:Card Margin="8" Padding="24"
                                         UniformCornerRadius="16"
                                         materialDesign:ElevationAssist.Elevation="Dp2">
                        <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
                            <materialDesign:PackIcon Kind="InformationOutline"
                                                      Width="48" Height="48"
                                                      HorizontalAlignment="Center"
                                                      Margin="0,0,0,16"
                                                      Foreground="{DynamicResource MaterialDesign.Brush.Primary}" />
                            <TextBlock Style="{StaticResource MaterialDesignHeadline6TextBlock}"
                                       Text="系统信息"
                                       HorizontalAlignment="Center" />
                            <TextBlock Style="{StaticResource MaterialDesignBody2TextBlock}"
                                       Text="{Binding Dashboard.SystemInfo.Value}"
                                       HorizontalAlignment="Center" Margin="0,8,0,0" />
                            <TextBlock Style="{StaticResource MaterialDesignBody2TextBlock}"
                                       Text="{Binding Dashboard.SystemInfo.Status}"
                                       HorizontalAlignment="Center" Margin="0,4,0,0"
                                       Opacity="0.6" TextTrimming="CharacterEllipsis" />
                        </StackPanel>
                    </materialDesign:Card>

                </UniformGrid>
            </StackPanel>
        </ScrollViewer>
    </materialDesign:DialogHost>
</UserControl>
```

关键变更：
- 移除 `UserControl.Resources` 中的 SysadminDarkTheme.xaml 合并（不再需要 Ops* 画刷）
- 移除 `converters` xmlns（不再需要 BoolToVis）
- 移除 API 状态卡片、连接模式卡片、导航按钮
- 全部使用 MDIX 原生组件和样式

- [ ] **Step 2: 验证编译**

Run: `dotnet build src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/LYBT.Desktop.Sysadmin.csproj --nologo 2>&1 | Select-Object -Last 5`
Expected: 编译失败（ViewModel 仍有导航命令引用）

- [ ] **Step 3: 提交**

```bash
git add src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/SysadminHomeView.xaml
git commit -m "feat: rewrite SysadminHomeView with MDIX native components"
```

---

## Task 2: 清理 SysadminHomeViewModel

**Covers:** [S3.3] ViewModel 清理

**Files:**
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/ViewModels/SysadminHomeViewModel.cs`

- [ ] **Step 1: 重写 ViewModel**

完整替换为：

```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using LYBT.Desktop.Sysadmin.Models;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Sysadmin.ViewModels;

/// <summary>
/// 系统运维控制台主页视图模型
/// </summary>
public partial class SysadminHomeViewModel : NavigableViewModelBase
{
    private readonly IAuthApi _authApi;
    private readonly IClinicSettingsService _clinicSettings;
    private CancellationTokenSource? _pollCts;

    [ObservableProperty]
    private DashboardStatus _dashboard = new();

    public SysadminHomeViewModel(
        IViewModelServices services,
        IAuthApi authApi,
        IClinicSettingsService clinicSettings)
        : base(services)
    {
        _authApi = authApi;
        _clinicSettings = clinicSettings;
        PageTitle = "运维控制台";
    }

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
        _pollCts?.Dispose();
        _pollCts = new CancellationTokenSource();
        _ = PollDashboardAsync(_pollCts.Token);
    }

    private void StopPolling() => _pollCts?.Cancel();

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

                Dashboard.SystemInfo.Value = $"v{SystemConstants.ApplicationVersion}";
                Dashboard.SystemInfo.Status = _clinicSettings.ClinicName;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "[SYSADMIN] Dashboard poll failed");
            }
            finally
            {
                if (isFirstLoad) { Dashboard.IsLoading = false; isFirstLoad = false; }
            }

            try { await Task.Delay(TimeSpan.FromSeconds(30), ct); }
            catch { break; }
        }
    }
}
```

关键变更：
- 移除 `INavigationCoordinator`、`IConnectionModeService`、`IConnectionSettingsService` 依赖
- 移除 `NavigateToAdminUsers`、`NavigateToClinicSettings`、`NavigateToLogLevel` 命令
- 移除 `using LYBT.Shared.Models.Enums`（不再需要 UserRole）
- 移除 `using LYBT.Desktop.Contracts.Services`（不再需要 INavigationCoordinator）
- 移除 `using Prism.Regions`（不再需要 NavigationParameters）
- 保留 `IAuthApi`（健康检查）和 `IClinicSettingsService`（诊所名称）

- [ ] **Step 2: 验证编译**

Run: `dotnet build src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/LYBT.Desktop.Sysadmin.csproj --nologo 2>&1 | Select-Object -Last 5`
Expected: BUILD SUCCESSFUL

- [ ] **Step 3: 提交**

```bash
git add src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/ViewModels/SysadminHomeViewModel.cs
git commit -m "refactor: clean up SysadminHomeViewModel - remove nav commands and unused dependencies"
```

---

## Task 3: 更新侧边栏导航项

**Covers:** [S3.2] 侧边栏导航项更新

**Files:**
- Modify: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs` (BuildNavigationItems 方法，约第 734 行)

- [ ] **Step 1: 在 BuildNavigationItems 中添加 SuperAdmin 导航项**

在现有的"管理组"部分之后，添加 SuperAdmin 专属导航项：

```csharp
// Sysadmin 专属组
if (role == UserRole.SuperAdmin)
{
    items.Add(CreateNavItem("管理员账号", "AdminUserManagementView", "AccountTie", "管理"));
    items.Add(CreateNavItem("诊所信息", ViewNames.SystemSettings, "Domain", "管理"));
    items.Add(CreateNavItem("日志控制", "LogLevelControlView", "Tune", "管理"));
}
```

插入位置：在 `if (definition.GetAllModules().Contains("ReportsModule"))` 之后、`Logger.LogInformation` 之前。

- [ ] **Step 2: 确认 ViewNames 中有 SystemSettings 常量**

检查 `ViewNames` 类是否包含 `SystemSettings` 常量。如果不存在，用字符串 `"SystemSettingsView"`。

- [ ] **Step 3: 验证编译**

Run: `dotnet build LYBTZYZS.sln --nologo 2>&1 | Select-Object -Last 5`
Expected: BUILD SUCCESSFUL

- [ ] **Step 4: 提交**

```bash
git add src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs
git commit -m "feat: add sysadmin navigation items to sidebar (admin accounts, clinic settings, log control)"
```

---

## Task 4: 清理 SysadminDarkTheme 和旧引用

**Covers:** [S3.1] 移除不再需要的资源

**Files:**
- Delete or Simplify: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Themes/SysadminDarkTheme.xaml`
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/LogLevelControlView.xaml` (移除 Ops* 引用)

- [ ] **Step 1: 检查 SysadminDarkTheme.xaml 是否仍被引用**

搜索是否还有 XAML 合并 SysadminDarkTheme.xaml。如果 SysadminHomeView.xaml 已不再引用（Task 1 已移除），检查 LogLevelControlView.xaml 和 AdminUserManagementView.xaml。

- [ ] **Step 2: 如果仍被引用，更新 LogLevelControlView.xaml**

将 LogLevelControlView.xaml 中的 Ops* 画刷引用替换为 MDIX 原生资源：
- `OpsBackgroundBrush` → `{DynamicResource MaterialDesign.Brush.Background}`
- `OpsTextPrimaryBrush` → `{DynamicResource MaterialDesign.Brush.Foreground}`
- `OpsTextSecondaryBrush` → `Opacity="0.6"` + `MaterialDesign.Brush.Foreground`
- `OpsAccentBrush` → `{DynamicResource MaterialDesign.Brush.Primary}`
- `OpsSuccessBrush` → `{DynamicResource MaterialDesign.Brush.Primary}`
- `OpsDangerBrush` → `{DynamicResource MaterialDesign.Brush.Error}`
- `OpsWarningBrush` → `{DynamicResource MaterialDesign.Brush.Warning}`

移除 SysadminDarkTheme.xaml 的 MergedDictionaries 引用。

- [ ] **Step 3: 同样处理 AdminUserManagementView.xaml**

如果 AdminUserManagementView.xaml 引用了 SysadminDarkTheme.xaml，同样替换。

- [ ] **Step 4: 删除 SysadminDarkTheme.xaml**（如果不再被任何文件引用）

- [ ] **Step 5: 验证编译**

Run: `dotnet build LYBTZYZS.sln --nologo 2>&1 | Select-Object -Last 5`
Expected: BUILD SUCCESSFUL

- [ ] **Step 6: 提交**

```bash
git add -A
git commit -m "refactor: replace Ops* brushes with MDIX native resources, remove SysadminDarkTheme"
```

---

## Task 5: 完整构建验证

**Covers:** 所有任务的集成验证

- [ ] **Step 1: 完整解决方案构建**

Run: `dotnet build LYBTZYZS.sln --nologo 2>&1 | Select-Object -Last 5`
Expected: BUILD SUCCESSFUL (0 错误)

- [ ] **Step 2: 验证无残留旧引用**

```bash
rg -n "OpsBackgroundBrush\|OpsCardBackgroundBrush\|OpsAccentBrush\|OpsTextPrimaryBrush\|OpsTextSecondaryBrush" src/ -g "*.xaml"
```
Expected: 0 结果

- [ ] **Step 3: 提交最终版本**

```bash
git add -A
git commit -m "feat: complete Sysadmin Dashboard redesign with MDIX native components"
```
