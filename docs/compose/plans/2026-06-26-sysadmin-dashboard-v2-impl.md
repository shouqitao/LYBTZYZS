# Sysadmin Dashboard v2 实施计划

> **For agentic workers:** Use compose:subagent or compose:execute to implement this plan.

**Goal:** 完善 Sysadmin Dashboard 设计，移除冗余信息，统一导航到工具栏，提升视觉效果。

**Architecture:** 移除与状态栏重复的卡片，使用 FunctionCardStyle 统一样式，添加状态指示器和图标。

**Tech Stack:** .NET 8, WPF, MaterialDesignInXamlToolkit (MDIX), Prism

---

## File Structure

| 操作 | 文件 | 职责 |
|------|------|------|
| Modify | `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Models/DashboardStatus.cs` | 移除冗余卡片 |
| Modify | `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/ViewModels/SysadminHomeViewModel.cs` | 移除导航命令，新增统计属性 |
| Modify | `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/SysadminHomeView.xaml` | 重新设计布局 |

---

## Task 1: 更新 DashboardStatus 模型

**Covers:** [S3] Dashboard 重新设计

**Files:**
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Models/DashboardStatus.cs`

- [ ] **Step 1: 移除冗余卡片属性**

```csharp
// 移除以下属性（状态栏已有）
// ApiStatus — 状态栏已有 ApiStatusIcon + ApiStatusColor
// ConnectionMode — 状态栏已有 ConnectionModeDisplay

// 保留以下属性
// DbStatus — 数据库状态
// SystemInfo — 系统信息
```

- [ ] **Step 2: 验证编译**

Run: `dotnet build src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/LYBT.Desktop.Sysadmin.csproj --nologo 2>&1 | Select-Object -Last 5`
Expected: 编译失败（ViewModel 引用已删除的属性）

- [ ] **Step 3: 提交**

```bash
git add src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Models/DashboardStatus.cs
git commit -m "refactor: remove redundant status cards from DashboardStatus"
```

---

## Task 2: 更新 ViewModel

**Covers:** [S4] ViewModel 变更

**Files:**
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/ViewModels/SysadminHomeViewModel.cs`

- [ ] **Step 1: 移除导航命令**

```csharp
// 移除以下命令（移到工具栏）
// NavigateToAdminUsersCommand
// NavigateToClinicSettingsCommand
// NavigateToLogLevelCommand
```

- [ ] **Step 2: 新增用户统计属性**

```csharp
[ObservableProperty]
private int _userCount;

[ObservableProperty]
private int _adminCount;

[ObservableProperty]
private int _doctorCount;

[ObservableProperty]
private int _receptionistCount;

[ObservableProperty]
private string _uptimeDisplay = "计算中...";
```

- [ ] **Step 3: 新增加载方法**

```csharp
private async Task LoadUserStatisticsAsync()
{
    try
    {
        var result = await _userApi.GetUsersAsync(page: 1, pageSize: 1000);
        if (result.Success && result.Data != null)
        {
            UserCount = result.Data.TotalCount;
            // 统计各角色数量
            AdminCount = result.Data.Items.Count(u => u.Role == UserRole.Admin);
            DoctorCount = result.Data.Items.Count(u => u.Role == UserRole.Doctor);
            ReceptionistCount = result.Data.Items.Count(u => u.Role == UserRole.Receptionist);
        }
    }
    catch (Exception ex)
    {
        Logger.LogError(ex, "[SYSADMIN] Load user statistics failed");
    }
}

private void CalculateUptime()
{
    var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
    UptimeDisplay = $"运行 {uptime.Days} 天 {uptime.Hours} 小时";
}
```

- [ ] **Step 4: 修改 PollDashboardAsync**

```csharp
// 移除 ApiStatus 和 ConnectionMode 的更新逻辑
// 保留 DbStatus 和 SystemInfo 的更新逻辑
// 添加用户统计和运行时长的更新
```

- [ ] **Step 5: 验证编译**

Run: `dotnet build src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/LYBT.Desktop.Sysadmin.csproj --nologo 2>&1 | Select-Object -Last 5`
Expected: BUILD SUCCESSFUL

- [ ] **Step 6: 提交**

```bash
git add src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/ViewModels/SysadminHomeViewModel.cs
git commit -m "refactor: update ViewModel - remove nav commands, add user statistics"
```

---

## Task 3: 重新设计 XAML

**Covers:** [S5] XAML 变更

**Files:**
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/SysadminHomeView.xaml`

- [ ] **Step 1: 重写 XAML**

```xml
<UserControl x:Class="LYBT.Desktop.Sysadmin.Views.SysadminHomeView"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
             xmlns:converters="clr-namespace:LYBT.Desktop.Controls.Converters;assembly=LYBT.Desktop.Controls"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             prism:ViewModelLocator.AutoWireViewModel="True"
             xmlns:prism="http://prismlibrary.com/">
    <UserControl.Resources>
        <ResourceDictionary>
            <ResourceDictionary.MergedDictionaries>
                <ResourceDictionary Source="/LYBT.Desktop.Sysadmin;component/Themes/SysadminDarkTheme.xaml" />
            </ResourceDictionary.MergedDictionaries>
        </ResourceDictionary>
    </UserControl.Resources>

    <materialDesign:DialogHost>
        <ScrollViewer VerticalScrollBarVisibility="Auto" Background="{DynamicResource MaterialDesignPaper}">
            <StackPanel Margin="24">
                <!-- 标题 -->
                <TextBlock Text="运维控制台" FontSize="28" FontWeight="Bold"
                           Foreground="{DynamicResource MaterialDesignBody}" Margin="0,0,0,8" />
                <TextBlock Text="凌隐宝堂中医诊所 · 系统运维" FontSize="14"
                           Foreground="{DynamicResource MaterialDesignBodyLight}" Margin="0,0,0,32" />

                <!-- 系统状态区域 -->
                <TextBlock Text="系统状态" FontSize="16" FontWeight="SemiBold"
                           Foreground="{DynamicResource MaterialDesignBody}" Margin="0,0,0,16" />
                <UniformGrid Columns="2" Margin="0,0,0,32">
                    <!-- 数据库卡片 -->
                    <Border Style="{StaticResource FunctionCardStyle}" Margin="12">
                        <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
                            <Path Style="{StaticResource CardIconStyle}" Data="{DynamicResource IconDatabase}" />
                            <TextBlock Style="{StaticResource CardTitleStyle}" Text="数据库" />
                            <StackPanel Orientation="Horizontal" HorizontalAlignment="Center">
                                <Ellipse Width="10" Height="10" Fill="{StaticResource OpsSuccessBrush}" 
                                         Margin="0,0,8,0" />
                                <TextBlock Text="{Binding Dashboard.DbStatus.Value}" 
                                           Foreground="{DynamicResource MaterialDesignBody}" />
                            </StackPanel>
                        </StackPanel>
                    </Border>

                    <!-- 系统信息卡片 -->
                    <Border Style="{StaticResource FunctionCardStyle}" Margin="12">
                        <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
                            <Path Style="{StaticResource CardIconStyle}" Data="{DynamicResource IconInfo}" />
                            <TextBlock Style="{StaticResource CardTitleStyle}" Text="系统信息" />
                            <TextBlock Text="{Binding Dashboard.SystemInfo.Value}" 
                                       Foreground="{DynamicResource MaterialDesignBody}" />
                            <TextBlock Text="{Binding Dashboard.SystemInfo.Status}" 
                                       Foreground="{DynamicResource MaterialDesignBodyLight}" 
                                       FontSize="12" Margin="0,4,0,0" />
                        </StackPanel>
                    </Border>
                </UniformGrid>

                <!-- 系统概览区域 -->
                <TextBlock Text="系统概览" FontSize="16" FontWeight="SemiBold"
                           Foreground="{DynamicResource MaterialDesignBody}" Margin="0,0,0,16" />
                <UniformGrid Columns="2">
                    <!-- 用户统计卡片 -->
                    <Border Style="{StaticResource FunctionCardStyle}" Margin="12">
                        <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
                            <Path Style="{StaticResource CardIconStyle}" Data="{DynamicResource IconUsers}" />
                            <TextBlock Style="{StaticResource CardTitleStyle}" Text="用户统计" />
                            <TextBlock Text="{Binding UserCount, StringFormat='总计 {0} 个用户'}" 
                                       Foreground="{DynamicResource MaterialDesignBody}" />
                            <TextBlock Text="{Binding AdminCount, StringFormat='Admin: {0}'}" 
                                       Foreground="{DynamicResource MaterialDesignBodyLight}" 
                                       FontSize="12" Margin="0,4,0,0" />
                        </StackPanel>
                    </Border>

                    <!-- 运行状态卡片 -->
                    <Border Style="{StaticResource FunctionCardStyle}" Margin="12">
                        <StackPanel HorizontalAlignment="Center" VerticalAlignment="Center">
                            <Path Style="{StaticResource CardIconStyle}" Data="{DynamicResource IconClock}" />
                            <TextBlock Style="{StaticResource CardTitleStyle}" Text="运行状态" />
                            <TextBlock Text="{Binding UptimeDisplay}" 
                                       Foreground="{DynamicResource MaterialDesignBody}" />
                        </StackPanel>
                    </Border>
                </UniformGrid>
            </StackPanel>
        </ScrollViewer>
    </materialDesign:DialogHost>
</UserControl>
```

- [ ] **Step 2: 验证编译**

Run: `dotnet build src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/LYBT.Desktop.Sysadmin.csproj --nologo 2>&1 | Select-Object -Last 5`
Expected: BUILD SUCCESSFUL

- [ ] **Step 3: 提交**

```bash
git add src/Client/Desktop/Roles/LYBT.Desktop.Sysadmin/Views/SysadminHomeView.xaml
git commit -m "feat: redesign Sysadmin Dashboard - remove redundancy, use FunctionCardStyle"
```

---

## Task 4: 完整构建验证

**Covers:** 所有任务的集成验证

- [ ] **Step 1: 完整解决方案构建**

Run: `dotnet build LYBTZYZS.sln --nologo 2>&1 | Select-Object -Last 5`
Expected: BUILD SUCCESSFUL (0 错误)

- [ ] **Step 2: 提交最终版本**

```bash
git add -A
git commit -m "feat: complete Sysadmin Dashboard v2 redesign"
```

---

## 变更日志

| 日期 | 版本 | 变更 |
|------|------|------|
| 2026-06-26 | v1.0 | 初始实施计划 |
