# 侧边栏全面重设计 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 全面重设计 SidebarControl — 双面板布局 + 信息架构重建 + 8 个 Root Cause bug 修复。

**Architecture:** 保留现有 DynamicResource 主题令牌（不大动主题），重写 SidebarControl.xaml 为「上半卡片（品牌+导航分组）+ 下半卡片（用户+状态条）」双面板结构。新增 `ProfileUpdatedEvent` 跨 VM 同步用户信息。删除代码后置 `OnUserAvatarClick`，所有交互走 XAML `Command` 绑定。

**Tech Stack:** WPF (.NET 8), Prism.DryIoc, CommunityToolkit.Mvvm ([ObservableProperty]/[RelayCommand]), Prism PubSubEvent.

**Spec:** `docs/compose/specs/2026-06-22-sidebar-full-redesign-design.md`

---

## 文件结构

| 文件 | 责任 | 操作 |
|------|------|------|
| `src/Client/Desktop/Core/LYBT.Desktop.Controls/Models/NavigationItem.cs` | 导航项数据模型 | 新增 `Group` 字段 |
| `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/AuthEvents.cs` | 跨 VM 事件契约 | 新增 `ProfileUpdatedEvent` |
| `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml` | 侧边栏 UI 主体 | 重写 ~80% |
| `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml.cs` | 侧边栏 code-behind | 删除死代码 + 修 DP 默认 |
| `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs` | 主窗口 VM | 暴露 3 个分组属性 + 订阅事件 |
| `src/Client/Desktop/Shell/ViewModels/AccountSettingsViewModel.cs` | 账户设置 VM | 保存成功后发布事件 |
| `src/Client/Desktop/Shell/Views/MainWindow.xaml` | 主窗口视图 | 删除死绑定 |

---

## Task 1: NavigationItem 添加 Group 字段

**Covers:** [S5]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Models/NavigationItem.cs`
- Test: `tests/LYBT.Tests.Desktop/Models/NavigationItemTests.cs` (new)

- [ ] **Step 1: 写失败测试**

创建测试文件：

```csharp
// tests/LYBT.Tests.Desktop/Models/NavigationItemTests.cs
using LYBT.Desktop.Controls.Models;

namespace LYBT.Tests.Desktop.Models;

public class NavigationItemTests
{
    [Fact]
    public void New_Instance_DefaultGroup_IsBusiness()
    {
        var item = new NavigationItem();
        Assert.Equal("业务", item.Group);
    }

    [Theory]
    [InlineData("主页")]
    [InlineData("业务")]
    [InlineData("管理")]
    public void Group_CanBeSet_ToKnownValues(string group)
    {
        var item = new NavigationItem { Group = group };
        Assert.Equal(group, item.Group);
    }
}
```

- [ ] **Step 2: 运行测试验证失败**

Run: `dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~NavigationItemTests"`
Expected: FAIL with "Property 'Group' does not exist"（编译错误）

- [ ] **Step 3: 实现字段**

修改 `NavigationItem.cs`：

```csharp
using System.Windows.Input;
using System.Windows.Media;

namespace LYBT.Desktop.Controls.Models;

/// <summary>
/// 侧边栏导航项数据模型
/// </summary>
public class NavigationItem
{
    public string Title { get; set; } = string.Empty;
    public string ViewName { get; set; } = string.Empty;
    public Geometry? IconData { get; set; }
    public bool IsVisible { get; set; } = true;
    public ICommand? Command { get; set; }

    /// <summary>
    /// 导航项所属分组：可选 "主页" / "业务" / "管理"。默认 "业务"。
    /// </summary>
    public string Group { get; set; } = "业务";
}
```

- [ ] **Step 4: 运行测试验证通过**

Run: `dotnet test tests/LYBT.Tests.Desktop/ --filter "FullyQualifiedName~NavigationItemTests"`
Expected: PASS (2 tests)

- [ ] **Step 5: 提交**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Controls/Models/NavigationItem.cs tests/LYBT.Tests.Desktop/Models/NavigationItemTests.cs
git commit -m "feat(desktop): add Group field to NavigationItem for sidebar grouping"
```

---

## Task 2: 新增 ProfileUpdatedEvent 跨 VM 同步事件

**Covers:** [S4] (R8 修复 - 信息同步机制)

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/AuthEvents.cs`

- [ ] **Step 1: 在 AuthEvents.cs 末尾追加事件定义**

在文件末尾的 `#endregion` 之前（即最后一个 record 定义 `SessionExpiredReason` 之后），追加：

```csharp
    #region 资料更新事件

    /// <summary>
    /// 用户资料更新事件
    /// 当用户在 AccountSettings 修改资料成功后触发，用于跨 VM 同步 CurrentUser
    /// </summary>
    public class ProfileUpdatedEvent : PubSubEvent<ProfileUpdatedPayload> { }

    #endregion
```

然后在文件末尾（最后一个 record `PasswordChangedPayload` 之后），追加载荷：

```csharp
/// <summary>
/// 用户资料更新载荷
/// </summary>
public record ProfileUpdatedPayload
{
    /// <summary>更新后的用户完整信息</summary>
    public required UserDetailDto UpdatedUser { get; init; }

    /// <summary>时间戳</summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}
```

- [ ] **Step 2: 验证编译**

Run: `dotnet build src/Client/Desktop/Core/LYBT.Desktop.Foundation/LYBT.Desktop.Foundation.csproj`
Expected: Build succeeded, 0 errors

- [ ] **Step 3: 提交**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Foundation/Security/AuthEvents.cs
git commit -m "feat(desktop): add ProfileUpdatedEvent for cross-VM user sync"
```

---

## Task 3: MainWindowViewModel 暴露 3 个分组属性 + 订阅 ProfileUpdatedEvent

**Covers:** [S4] [S5] (R4, R7, R8 部分)

**Files:**
- Modify: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`

- [ ] **Step 1: 给 BuildNavigationItems 调用赋 Group 值**

定位 `BuildNavigationItems(UserRole role)` 方法（约 line 660），修改每个 `NavigationItem` 创建处：

```csharp
private ObservableCollection<NavigationItem> BuildNavigationItems(UserRole role)
{
    var definition = Services.RoleRegistry.GetDefinition(role);
    var items = new ObservableCollection<NavigationItem>();

    if (definition == null)
    {
        Logger.LogWarning("无法为角色 {Role} 找到定义，导航项为空", role);
        return items;
    }

    var modules = definition.RequiredModules;

    // 主页组
    items.Add(new NavigationItem
    {
        Title = "主页",
        ViewName = definition.HomeViewName,
        IconData = (Geometry)Application.Current.FindResource("IconHome"),
        Command = new RelayCommand(() => _navigationCoordinator.NavigateTo(definition.HomeViewName)),
        Group = "主页"
    });

    // 业务组
    if (modules.Contains("PatientsModule"))
        items.Add(CreateNavItem("患者管理", ViewNames.PatientManagement, "IconPatients", "业务"));
    if (modules.Contains("HerbsModule"))
        items.Add(CreateNavItem("药材管理", ViewNames.HerbManagement, "IconHerbs", "业务"));
    if (modules.Contains("FormulaModule"))
        items.Add(CreateNavItem("验方管理", ViewNames.FormulaManagement, "IconFormula", "业务"));
    if (modules.Contains("MedicalCaseModule"))
        items.Add(CreateNavItem("医案管理", ViewNames.MedicalCaseManagement, "IconMedicalCase", "业务"));
    if (modules.Contains("RegistrationModule"))
        items.Add(CreateNavItem("挂号管理", ViewNames.RegistrationList, "IconRegistration", "业务"));

    // 管理组
    if (modules.Contains("UsersModule") && role is UserRole.Admin or UserRole.SuperAdmin)
        items.Add(CreateNavItem("用户管理", ViewNames.UserManagement, "IconUsers", "管理"));
    if (definition.GetAllModules().Contains("ReportsModule"))
        items.Add(CreateNavItem("统计报表", ViewNames.ReportsHome, "IconReports", "管理"));

    Logger.LogInformation("已为角色 {Role} 构建 {Count} 个导航项", role, items.Count);
    return items;
}

private NavigationItem CreateNavItem(string title, string viewName, string iconKey, string group = "业务") =>
    new()
    {
        Title = title,
        ViewName = viewName,
        IconData = (Geometry)Application.Current.FindResource(iconKey),
        Command = new RelayCommand(() => _navigationCoordinator.NavigateTo(viewName)),
        Group = group
    };
```

- [ ] **Step 2: 新增 3 个分组属性**

在 `NavigationItems` 属性附近（约 line 145 之后的 properties 区域），添加：

```csharp
/// <summary>
/// 主页组导航项（侧边栏分组显示用）
/// </summary>
public ObservableCollection<NavigationItem> HomeNavItems =>
    new(NavigationItems.Where(i => i.Group == "主页"));

/// <summary>
/// 业务组导航项
/// </summary>
public ObservableCollection<NavigationItem> BusinessNavItems =>
    new(NavigationItems.Where(i => i.Group == "业务"));

/// <summary>
/// 管理组导航项
/// </summary>
public ObservableCollection<NavigationItem> AdminNavItems =>
    new(NavigationItems.Where(i => i.Group == "管理"));
```

并在 `BuildNavigationItems` 调用后（line 631 之后），加上属性变更通知：

```csharp
NavigationItems = BuildNavigationItems(user.Role);
OnPropertyChanged(nameof(HomeNavItems));
OnPropertyChanged(nameof(BusinessNavItems));
OnPropertyChanged(nameof(AdminNavItems));
```

同样在 `NavigationItems.Clear()` (line 736) 之后也加上：

```csharp
NavigationItems.Clear();
OnPropertyChanged(nameof(HomeNavItems));
OnPropertyChanged(nameof(BusinessNavItems));
OnPropertyChanged(nameof(AdminNavItems));
```

- [ ] **Step 3: 订阅 ProfileUpdatedEvent**

先确认 MainWindowViewModel 中 `EventAggregator` 的访问方式（参考 line 741 现有用法 `EventAggregator.GetEvent<AuthEvents.LogoutCompletedEvent>().Publish(...)`，是直接字段而非 Services. 前缀）。

在 MainWindowViewModel 构造函数末尾追加（如果已有 `_eventAggregator` 字段就用它，否则用现有 `EventAggregator` 属性）：

```csharp
// 订阅用户资料更新事件（来自 AccountSettingsViewModel）
EventAggregator.GetEvent<AuthEvents.ProfileUpdatedEvent>()
    .Subscribe(OnProfileUpdated);
```

然后添加事件处理方法（放在 `OnPasswordChanged` 方法附近，参考其 `Services.UiThreadDispatcher.InvokeAsync` 模式）：

```csharp
/// <summary>
/// 用户资料更新事件处理 - 同步 CurrentUser
/// </summary>
private void OnProfileUpdated(ProfileUpdatedPayload payload)
{
    Services.UiThreadDispatcher.InvokeAsync(() =>
    {
        CurrentUser = payload.UpdatedUser;
        Logger.LogInformation("已同步用户资料更新 [用户: {UserName}]", payload.UpdatedUser.UserName);
    });
}
```

确保文件顶部有 `using LYBT.Desktop.Foundation.Security;` 引用（如果没有则添加）。

- [ ] **Step 4: 验证编译**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell/LYBT.Desktop.Shell.csproj`
Expected: Build succeeded, 0 errors

- [ ] **Step 5: 提交**

```bash
git add src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs
git commit -m "feat(desktop): split NavigationItems by Group + subscribe ProfileUpdatedEvent"
```

---

## Task 4: AccountSettingsViewModel 发布 ProfileUpdatedEvent

**Covers:** [S4] (R8 修复 - 触发同步)

**Files:**
- Modify: `src/Client/Desktop/Shell/ViewModels/AccountSettingsViewModel.cs`

- [ ] **Step 1: 在 SaveProfileAsync 成功分支发布事件**

定位 `SaveProfileAsync` 方法（约 line 82），在 `Services.ToastService.ShowSuccess("个人资料已保存")` 之后（line 113 后），添加事件发布：

```csharp
if (resp.Success)
{
    if (resp.Data != null)
    {
        CurrentUser = resp.Data;
        // 发布事件通知 MainWindowViewModel 同步
        Services.EventAggregator.GetEvent<AuthEvents.ProfileUpdatedEvent>()
            .Publish(new ProfileUpdatedPayload { UpdatedUser = resp.Data });
    }
    Services.ToastService.ShowSuccess("个人资料已保存");
    Logger.LogInformation("用户资料更新成功: {UserName}", CurrentUser.UserName);
}
```

确保文件顶部有 `using LYBT.Desktop.Foundation.Security;` 引用。

- [ ] **Step 2: 验证编译**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell/LYBT.Desktop.Shell.csproj`
Expected: Build succeeded

- [ ] **Step 3: 提交**

```bash
git add src/Client/Desktop/Shell/ViewModels/AccountSettingsViewModel.cs
git commit -m "feat(desktop): publish ProfileUpdatedEvent on profile save"
```

---

## Task 5: 重写 SidebarControl.xaml（核心任务）

**Covers:** [S3] [S4] [S5] [S6] [S7] [S8]（R1, R2, R5, R6, R7）

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml`

> **重要**：这是最大改动。建议分两次提交：第一次提交骨架（双面板 + 状态条），第二次提交用户卡 Popup + 动画。

- [ ] **Step 1: 重写 SidebarControl.xaml 完整内容**

将整个文件替换为：

```xaml
<!--
    SidebarControl - 侧边栏控件 (Full Redesign 2026-06-22)

    双面板布局：
    - 上半卡片 (SidebarBrush): 品牌行 + 导航分组 (Home/Business/Admin)
    - 下半卡片 (SidebarHoverBrush): 用户卡 + 状态条

    绑定：
    - HomeNavItems / BusinessNavItems / AdminNavItems (新增分组源)
    - CurrentUser / ApiStatus / CurrentTime / IsRemoteMode / ConnectionDisplay (保留)
    - EditProfileCommand / LogoutCommand / ToggleCommand / NavigateToHomeCommand (保留)
-->
<UserControl
    x:Class="LYBT.Desktop.Controls.Controls.SidebarControl"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:converters="clr-namespace:LYBT.Desktop.Controls.Converters"
    xmlns:models="clr-namespace:LYBT.Desktop.Controls.Models"
    x:Name="Root">

    <UserControl.Resources>
        <ResourceDictionary>
            <CubicEase x:Key="CubicEase" />

            <!-- 导航项样式 -->
            <Style x:Key="NavMenuItemStyle" TargetType="Button">
                <Setter Property="Background" Value="Transparent" />
                <Setter Property="Foreground" Value="{DynamicResource SidebarTextBrush}" />
                <Setter Property="BorderThickness" Value="0" />
                <Setter Property="Height" Value="40" />
                <Setter Property="Padding" Value="12,0" />
                <Setter Property="Cursor" Value="Hand" />
                <Setter Property="HorizontalContentAlignment" Value="Left" />
                <Setter Property="Template">
                    <Setter.Value>
                        <ControlTemplate TargetType="Button">
                            <Border
                                x:Name="ItemBorder"
                                Padding="{TemplateBinding Padding}"
                                Background="{TemplateBinding Background}">
                                <ContentPresenter HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}" VerticalAlignment="Center" />
                            </Border>
                            <ControlTemplate.Triggers>
                                <Trigger Property="IsMouseOver" Value="True">
                                    <Setter TargetName="ItemBorder" Property="Background" Value="{DynamicResource SidebarHoverBrush}" />
                                </Trigger>
                            </ControlTemplate.Triggers>
                        </ControlTemplate>
                    </Setter.Value>
                </Setter>
                <Style.Triggers>
                    <DataTrigger Binding="{Binding IsExpanded, ElementName=Root}" Value="False">
                        <Setter Property="HorizontalContentAlignment" Value="Center" />
                        <Setter Property="Padding" Value="0,0" />
                    </DataTrigger>
                </Style.Triggers>
            </Style>

            <!-- 分组小标题样式 -->
            <Style x:Key="NavGroupHeaderStyle" TargetType="TextBlock">
                <Setter Property="FontSize" Value="11" />
                <Setter Property="Foreground" Value="{DynamicResource SidebarSecondaryTextBrush}" />
                <Setter Property="Padding" Value="12,8,0,4" />
            </Style>

            <!-- 导航项数据模板 -->
            <DataTemplate DataType="{x:Type models:NavigationItem}">
                <Button
                    Command="{Binding Command}"
                    Style="{StaticResource NavMenuItemStyle}"
                    ToolTip="{Binding Title}">
                    <StackPanel Orientation="Horizontal">
                        <Path
                            Width="20"
                            Height="20"
                            Data="{Binding IconData}"
                            Fill="{DynamicResource SidebarTextBrush}"
                            Stretch="Uniform" />
                        <TextBlock
                            Margin="12,0,0,0"
                            VerticalAlignment="Center"
                            FontSize="13"
                            Foreground="{DynamicResource SidebarTextBrush}"
                            Text="{Binding Title}"
                            Visibility="{Binding IsExpanded, ElementName=Root, Converter={x:Static converters:Cvt.BoolToVis}}" />
                    </StackPanel>
                </Button>
            </DataTemplate>
        </ResourceDictionary>
    </UserControl.Resources>

    <!-- 根 Border：宽度由 IsExpanded 控制，200ms CubicEase 动画 -->
    <Border x:Name="RootBorder" Background="{DynamicResource SidebarBrush}">
        <Border.Style>
            <Style TargetType="Border">
                <Setter Property="Width" Value="56" />
                <Style.Triggers>
                    <DataTrigger Binding="{Binding IsExpanded, ElementName=Root}" Value="True">
                        <DataTrigger.EnterActions>
                            <BeginStoryboard>
                                <Storyboard>
                                    <DoubleAnimation
                                        Storyboard.TargetProperty="Width"
                                        To="220"
                                        Duration="0:0:0.2"
                                        EasingFunction="{StaticResource CubicEase}" />
                                </Storyboard>
                            </BeginStoryboard>
                        </DataTrigger.EnterActions>
                        <DataTrigger.ExitActions>
                            <BeginStoryboard>
                                <Storyboard>
                                    <DoubleAnimation
                                        Storyboard.TargetProperty="Width"
                                        To="56"
                                        Duration="0:0:0.2"
                                        EasingFunction="{StaticResource CubicEase}" />
                                </Storyboard>
                            </BeginStoryboard>
                        </DataTrigger.ExitActions>
                    </DataTrigger>
                </Style.Triggers>
            </Style>
        </Border.Style>

        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height="*" />
                <!-- 上半卡片 -->
                <RowDefinition Height="8" />
                <!-- 间隙 -->
                <RowDefinition Height="Auto" />
                <!-- 下半卡片 -->
            </Grid.RowDefinitions>

            <!-- ============ 上半卡片：品牌 + 导航 ============ -->
            <DockPanel Grid.Row="0" Background="{DynamicResource SidebarBrush}">

                <!-- 品牌行（顶部固定，点击回主页）-->
                <Button
                    DockPanel.Dock="Top"
                    Height="56"
                    Command="{Binding NavigateToHomeCommand, ElementName=Root}"
                    Style="{StaticResource NavMenuItemStyle}"
                    ToolTip="返回主页">
                    <StackPanel Orientation="Horizontal">
                        <Path
                            Width="24"
                            Height="24"
                            Data="M3 18h18v-2H3v2zm0-5h18v-2H3v2zm0-7v2h18V6H3z"
                            Fill="{DynamicResource SidebarTextBrush}"
                            Stretch="Uniform" />
                        <TextBlock
                            Margin="12,0,0,0"
                            VerticalAlignment="Center"
                            FontSize="16"
                            FontWeight="Bold"
                            Foreground="{DynamicResource SidebarTextBrush}"
                            Text="凌隐宝堂"
                            Visibility="{Binding IsExpanded, ElementName=Root, Converter={x:Static converters:Cvt.BoolToVis}}" />
                    </StackPanel>
                </Button>

                <!-- 汉堡按钮（右上角小图标，用于折叠/展开） -->
                <Button
                    DockPanel.Dock="Top"
                    Height="32"
                    Margin="0,-44,0,12"
                    HorizontalAlignment="Right"
                    Background="Transparent"
                    BorderThickness="0"
                    Command="{Binding ToggleCommand, ElementName=Root}"
                    Cursor="Hand"
                    ToolTip="展开/收缩 (Ctrl+M)"
                    Visibility="{Binding IsExpanded, ElementName=Root, Converter={x:Static converters:Cvt.BoolToVis}}">
                    <Path
                        Width="14"
                        Height="14"
                        Data="M3 18h18v-2H3v2zm0-5h18v-2H3v2zm0-7v2h18V6H3z"
                        Fill="{DynamicResource SidebarSecondaryTextBrush}"
                        Stretch="Uniform" />
                </Button>

                <!-- 导航区（可滚动） -->
                <ScrollViewer
                    HorizontalScrollBarVisibility="Disabled"
                    VerticalScrollBarVisibility="Auto">
                    <StackPanel Margin="0,8,0,8">

                        <!-- 主页组（无标题） -->
                        <ItemsControl ItemsSource="{Binding HomeNavItems, ElementName=Root}" />

                        <!-- 业务组 -->
                        <StackPanel Visibility="{Binding IsExpanded, ElementName=Root, Converter={x:Static converters:Cvt.BoolToVis}}">
                            <TextBlock Style="{StaticResource NavGroupHeaderStyle}" Text="业务" />
                        </StackPanel>
                        <ItemsControl ItemsSource="{Binding BusinessNavItems, ElementName=Root}" />

                        <!-- 管理组 -->
                        <StackPanel>
                            <TextBlock
                                Style="{StaticResource NavGroupHeaderStyle}"
                                Text="管理"
                                Visibility="{Binding IsExpanded, ElementName=Root, Converter={x:Static converters:Cvt.BoolToVis}}" />
                        </StackPanel>
                        <ItemsControl ItemsSource="{Binding AdminNavItems, ElementName=Root}" />

                    </StackPanel>
                </ScrollViewer>
            </DockPanel>

            <!-- ============ 间隙（透明） ============ -->
            <Border Grid.Row="1" Background="Transparent" />

            <!-- ============ 下半卡片：用户卡 + 状态条 ============ -->
            <Border
                Grid.Row="2"
                Background="{DynamicResource SidebarHoverBrush}"
                Padding="0,8,0,0">

                <StackPanel>

                    <!-- 用户卡（Grid 分主体 Button + ⋯ Button） -->
                    <Grid Height="64" Margin="8,0">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width="*" />
                            <ColumnDefinition Width="Auto" />
                        </Grid.ColumnDefinitions>

                        <!-- 主体 Button：点击跳账户设置（展开态） -->
                        <Button
                            x:Name="UserCardButton"
                            Grid.Column="0"
                            Command="{Binding EditProfileCommand, ElementName=Root}"
                            Cursor="Hand"
                            ToolTip="点击修改个人资料"
                            Visibility="{Binding IsExpanded, ElementName=Root, Converter={x:Static converters:Cvt.BoolToVis}}">
                            <Button.Style>
                                <Style TargetType="Button">
                                    <Setter Property="Background" Value="Transparent" />
                                    <Setter Property="BorderThickness" Value="0" />
                                    <Setter Property="HorizontalContentAlignment" Value="Stretch" />
                                    <Setter Property="Template">
                                        <Setter.Value>
                                            <ControlTemplate TargetType="Button">
                                                <Border x:Name="UserBorder" Padding="4,0" Background="Transparent" CornerRadius="8">
                                                    <ContentPresenter HorizontalAlignment="{TemplateBinding HorizontalContentAlignment}" VerticalAlignment="Center" />
                                                </Border>
                                                <ControlTemplate.Triggers>
                                                    <Trigger Property="IsMouseOver" Value="True">
                                                        <Setter TargetName="UserBorder" Property="Background" Value="{DynamicResource SidebarDividerBrush}" />
                                                    </Trigger>
                                                </ControlTemplate.Triggers>
                                            </ControlTemplate>
                                        </Setter.Value>
                                    </Setter>
                                </Style>
                            </Button.Style>
                            <StackPanel Orientation="Horizontal">
                                <Border
                                    Width="32"
                                    Height="32"
                                    Background="{DynamicResource SidebarAvatarBrush}"
                                    CornerRadius="16">
                                    <TextBlock
                                        HorizontalAlignment="Center"
                                        VerticalAlignment="Center"
                                        FontSize="14"
                                        FontWeight="SemiBold"
                                        Foreground="{DynamicResource SidebarTextBrush}"
                                        Text="{Binding CurrentUser.RealName, ElementName=Root, Converter={x:Static converters:Cvt.FirstChar}}" />
                                </Border>
                                <StackPanel Margin="10,0,0,0" VerticalAlignment="Center">
                                    <TextBlock
                                        FontSize="13"
                                        FontWeight="SemiBold"
                                        Foreground="{DynamicResource SidebarTextBrush}"
                                        Text="{Binding CurrentUser.RealName, ElementName=Root}" />
                                    <TextBlock
                                        FontSize="11"
                                        Foreground="{DynamicResource SidebarSecondaryTextBrush}"
                                        Text="{Binding CurrentUser.Role, ElementName=Root, Converter={x:Static converters:Cvt.EnumDesc}}" />
                                </StackPanel>
                            </StackPanel>
                        </Button>

                        <!-- 折叠态 Avatar Button：点击弹 Popup -->
                        <Button
                            x:Name="UserCardCollapsedButton"
                            Grid.Column="0"
                            Click="OnCollapsedAvatarClick"
                            Cursor="Hand"
                            ToolTip="{Binding CurrentUser.RealName, ElementName=Root}"
                            Visibility="{Binding IsExpanded, ElementName=Root, Converter={x:Static converters:Cvt.InverseBoolToVis}}">
                            <Button.Style>
                                <Style TargetType="Button">
                                    <Setter Property="Background" Value="Transparent" />
                                    <Setter Property="BorderThickness" Value="0" />
                                    <Setter Property="Template">
                                        <Setter.Value>
                                            <ControlTemplate TargetType="Button">
                                                <Border x:Name="ColBorder" Padding="0" Background="Transparent" CornerRadius="8">
                                                    <ContentPresenter HorizontalAlignment="Center" VerticalAlignment="Center" />
                                                </Border>
                                                <ControlTemplate.Triggers>
                                                    <Trigger Property="IsMouseOver" Value="True">
                                                        <Setter TargetName="ColBorder" Property="Background" Value="{DynamicResource SidebarDividerBrush}" />
                                                    </Trigger>
                                                </ControlTemplate.Triggers>
                                            </ControlTemplate>
                                        </Setter.Value>
                                    </Setter>
                                </Style>
                            </Button.Style>
                            <Border
                                Width="28"
                                Height="28"
                                Background="{DynamicResource SidebarAvatarBrush}"
                                CornerRadius="14">
                                <TextBlock
                                    HorizontalAlignment="Center"
                                    VerticalAlignment="Center"
                                    FontSize="12"
                                    FontWeight="SemiBold"
                                    Foreground="{DynamicResource SidebarTextBrush}"
                                    Text="{Binding CurrentUser.RealName, ElementName=Root, Converter={x:Static converters:Cvt.FirstChar}}" />
                            </Border>
                        </Button>

                        <!-- ⋯ 更多按钮（展开态显示） -->
                        <Button
                            x:Name="MoreButton"
                            Grid.Column="1"
                            Width="32"
                            Height="32"
                            Margin="4,0,8,0"
                            Background="Transparent"
                            BorderThickness="0"
                            Click="OnMoreButtonClick"
                            Cursor="Hand"
                            ToolTip="更多"
                            Visibility="{Binding IsExpanded, ElementName=Root, Converter={x:Static converters:Cvt.BoolToVis}}">
                            <Path
                                Width="16"
                                Height="16"
                                Data="M12 8c1.1 0 2-.9 2-2s-.9-2-2-2-2 .9-2 2 .9 2 2 2zm0 2c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2zm0 6c-1.1 0-2 .9-2 2s.9 2 2 2 2-.9 2-2-.9-2-2-2z"
                                Fill="{DynamicResource SidebarSecondaryTextBrush}"
                                Stretch="Uniform" />
                        </Button>

                        <!-- 共享 Popup（⋯ 按钮和折叠态 avatar 都用这个） -->
                        <Popup
                            x:Name="UserMenuPopup"
                            AllowsTransparency="True"
                            Placement="Bottom"
                            PlacementTarget="{Binding ElementName=MoreButton}"
                            StaysOpen="False">
                            <Border
                                MinWidth="160"
                                Background="{DynamicResource SidebarBrush}"
                                BorderBrush="{DynamicResource SidebarDividerBrush}"
                                BorderThickness="1"
                                CornerRadius="6">
                                <StackPanel>
                                    <Button
                                        Padding="16,10"
                                        HorizontalContentAlignment="Left"
                                        Background="Transparent"
                                        BorderThickness="0"
                                        Command="{Binding EditProfileCommand, ElementName=Root}"
                                        Content="个人资料"
                                        Cursor="Hand"
                                        FontSize="13"
                                        Foreground="{DynamicResource SidebarTextBrush}" />
                                    <Border Height="1" Background="{DynamicResource SidebarDividerBrush}" />
                                    <Button
                                        Padding="16,10"
                                        HorizontalContentAlignment="Left"
                                        Background="Transparent"
                                        BorderThickness="0"
                                        Command="{Binding LogoutCommand, ElementName=Root}"
                                        Content="退出登录"
                                        Cursor="Hand"
                                        FontSize="13"
                                        Foreground="{DynamicResource DangerBrush}" />
                                </StackPanel>
                            </Border>
                        </Popup>
                    </Grid>

                    <!-- 分隔线 -->
                    <Border
                        Height="1"
                        Margin="12,8"
                        Background="{DynamicResource SidebarDividerBrush}" />

                    <!-- 状态条 -->
                    <Border Padding="12,8">
                        <StackPanel>

                            <!-- 展开态：完整状态（API + 模式 + 时钟） -->
                            <StackPanel Visibility="{Binding IsExpanded, ElementName=Root, Converter={x:Static converters:Cvt.BoolToVis}}">

                                <!-- API + 模式（一行） -->
                                <StackPanel Margin="0,0,0,4" Orientation="Horizontal">
                                    <Ellipse
                                        Width="12"
                                        Height="12"
                                        VerticalAlignment="Center"
                                        Fill="{Binding ApiStatus, ElementName=Root, Converter={x:Static converters:Cvt.ApiStatusToColor}}" />
                                    <TextBlock
                                        Margin="8,0,0,0"
                                        VerticalAlignment="Center"
                                        FontSize="11"
                                        Foreground="{DynamicResource SidebarSecondaryTextBrush}"
                                        Text="{Binding ApiStatus, ElementName=Root, Converter={x:Static converters:Cvt.ApiStatusToText}}" />
                                    <TextBlock
                                        Margin="6,0,0,0"
                                        VerticalAlignment="Center"
                                        FontSize="11"
                                        FontWeight="Medium"
                                        Foreground="{Binding IsRemoteMode, ElementName=Root, Converter={x:Static converters:Cvt.BoolToColor}, ConverterParameter=Green|OrangeRed}"
                                        Text="{Binding ConnectionDisplay, ElementName=Root}" />
                                </StackPanel>

                                <!-- 时钟（一行） -->
                                <StackPanel Orientation="Horizontal">
                                    <Path
                                        Width="14"
                                        Height="14"
                                        VerticalAlignment="Center"
                                        Data="M12 2C6.5 2 2 6.5 2 12s4.5 10 10 10 10-4.5 10-10S17.5 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm.5-13H11v6l5.25 3.15.75-1.23-4.5-2.67V7z"
                                        Fill="{DynamicResource SidebarSecondaryTextBrush}"
                                        Stretch="Uniform" />
                                    <StackPanel Margin="8,0,0,0">
                                        <TextBlock
                                            FontSize="12"
                                            FontWeight="SemiBold"
                                            Foreground="{DynamicResource SidebarTextBrush}"
                                            Text="{Binding CurrentTime, ElementName=Root, StringFormat=HH:mm:ss}" />
                                        <TextBlock
                                            FontSize="10"
                                            Foreground="{DynamicResource SidebarSecondaryTextBrush}"
                                            Text="{Binding CurrentTime, ElementName=Root, StringFormat=yyyy-MM-dd}" />
                                    </StackPanel>
                                </StackPanel>

                            </StackPanel>

                            <!-- 折叠态：3 个图标水平排列 -->
                            <StackPanel
                                HorizontalAlignment="Center"
                                Orientation="Horizontal"
                                Visibility="{Binding IsExpanded, ElementName=Root, Converter={x:Static converters:Cvt.InverseBoolToVis}}">

                                <Ellipse
                                    Width="12"
                                    Height="12"
                                    Margin="0,0,8,0"
                                    VerticalAlignment="Center"
                                    Fill="{Binding ApiStatus, ElementName=Root, Converter={x:Static converters:Cvt.ApiStatusToColor}}"
                                    ToolTip="{Binding ApiStatus, ElementName=Root, Converter={x:Static converters:Cvt.ApiStatusToText}}" />

                                <Path
                                    Width="12"
                                    Height="12"
                                    Margin="0,0,8,0"
                                    VerticalAlignment="Center"
                                    Data="M12,2A10,10 0 0,0 2,12A10,10 0 0,0 12,22A10,10 0 0,0 22,12A10,10 0 0,0 12,2M12,17L7,12L8.41,10.59L11,13.17V7H13V13.17L15.59,10.59L17,12L12,17Z"
                                    Fill="{Binding IsRemoteMode, ElementName=Root, Converter={x:Static converters:Cvt.BoolToColor}, ConverterParameter=Green|OrangeRed}"
                                    Stretch="Uniform"
                                    ToolTip="{Binding ConnectionDisplay, ElementName=Root}" />

                                <Path
                                    Width="14"
                                    Height="14"
                                    VerticalAlignment="Center"
                                    Data="M12 2C6.5 2 2 6.5 2 12s4.5 10 10 10 10-4.5 10-10S17.5 2 12 2zm0 18c-4.41 0-8-3.59-8-8s3.59-8 8-8 8 3.59 8 8-3.59 8-8 8zm.5-13H11v6l5.25 3.15.75-1.23-4.5-2.67V7z"
                                    Fill="{DynamicResource SidebarSecondaryTextBrush}"
                                    Stretch="Uniform"
                                    ToolTip="{Binding CurrentTime, ElementName=Root, StringFormat='yyyy-MM-dd HH:mm'}" />

                            </StackPanel>

                        </StackPanel>
                    </Border>

                </StackPanel>
            </Border>
        </Grid>
    </Border>
</UserControl>
```

- [ ] **Step 2: 验证编译**

Run: `dotnet build src/Client/Desktop/Core/LYBT.Desktop.Controls/LYBT.Desktop.Controls.csproj`
Expected: Build succeeded（可能 warning 关于未使用字段，先忽略）

- [ ] **Step 3: 提交（核心改动单独提交）**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml
git commit -m "feat(desktop): rewrite SidebarControl.xaml with dual-pane layout and animations"
```

---

## Task 6: 清理 SidebarControl.xaml.cs（删死代码 + 新增 Popup 处理）

**Covers:** [S4] (R1, R3, R4)

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml.cs`

- [ ] **Step 1: 精确编辑 SidebarControl.xaml.cs（不重写）**

打开 `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml.cs`，做以下 4 处精确改动：

**改动 1：删除整个 `OnUserAvatarClick` 方法（约 line 15-32）**

完整删除这个方法块，包括方法签名和大括号内的所有代码。

**改动 2：删除整个 `#region NavigateToSystemSettingsCommand` 区域（约 line 134-146）**

完整删除从 `#region NavigateToSystemSettingsCommand - 系统设置命令` 到对应 `#endregion` 的整个块，包括 `NavigateToSystemSettingsCommand` 属性、`NavigateToSystemSettingsCommandProperty` 字段。

**改动 3：将 `IsExpandedProperty` 默认值改为 `true`**

定位 `IsExpandedProperty` 定义，将 `new PropertyMetadata(false)` 改为 `new PropertyMetadata(true)`：

```csharp
public static readonly DependencyProperty IsExpandedProperty =
    DependencyProperty.Register(nameof(IsExpanded), typeof(bool), typeof(SidebarControl),
        new PropertyMetadata(true));  // ← 改为 true
```

**改动 4：在类内部 `SidebarControl()` 构造函数之后、第一个 `#region` 之前，追加 2 个新方法：**

```csharp
private void OnMoreButtonClick(object sender, RoutedEventArgs e)
{
    UserMenuPopup.PlacementTarget = sender as UIElement;
    UserMenuPopup.IsOpen = true;
}

private void OnCollapsedAvatarClick(object sender, RoutedEventArgs e)
{
    UserMenuPopup.PlacementTarget = sender as UIElement;
    UserMenuPopup.IsOpen = true;
}
```

> **保留所有其他 DP 不变**：`CurrentUser`、`ApiStatus`、`CurrentTime`、`ToggleCommand`、`NavigateToHomeCommand`、`EditProfileCommand`、`LogoutCommand`、`NavigationItemsSource`、`ConnectionDisplay`、`IsRemoteMode` 等区域原样保留。**不要删除** `NavigationItemsSource`（Task 7 中讨论为何保留）。

- [ ] **Step 2: 验证编译**

Run: `dotnet build src/Client/Desktop/Core/LYBT.Desktop.Controls/LYBT.Desktop.Controls.csproj`
Expected: Build succeeded, 0 errors

- [ ] **Step 3: 提交**

```bash
git add src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml.cs
git commit -m "refactor(desktop): remove dead code in SidebarControl, fix IsExpanded default to true"
```

---

## Task 7: 更新 MainWindow.xaml 绑定 + 添加新绑定

**Covers:** [S4] [S9] (R3 + 新增分组属性绑定)

**Files:**
- Modify: `src/Client/Desktop/Shell/Views/MainWindow.xaml`

- [ ] **Step 1: 更新 SidebarControl 绑定**

定位 `controls:SidebarControl` 元素（约 line 79-93），修改为：

```xaml
<controls:SidebarControl
    Grid.Row="0"
    Grid.RowSpan="2"
    Grid.Column="0"
    ApiStatus="{Binding ApiStatus}"
    ConnectionDisplay="{Binding ConnectionModeDisplay}"
    CurrentTime="{Binding CurrentTime}"
    CurrentUser="{Binding CurrentUser}"
    EditProfileCommand="{Binding EditProfileCommand}"
    HomeNavItems="{Binding HomeNavItems}"
    BusinessNavItems="{Binding BusinessNavItems}"
    AdminNavItems="{Binding AdminNavItems}"
    IsExpanded="{Binding IsDrawerOpen}"
    IsRemoteMode="{Binding IsRemoteMode}"
    LogoutCommand="{Binding LogoutCommand}"
    NavigateToHomeCommand="{Binding NavigateToHomeCommand}"
    ToggleCommand="{Binding ToggleDrawerCommand}" />
```

变更点：
- 删除 `NavigationItemsSource="{Binding NavigationItems}"` 绑定（旧的单一源）
- 新增 3 个分组绑定 `HomeNavItems` / `BusinessNavItems` / `AdminNavItems`
- 删除 `NavigateToSystemSettingsCommand` 绑定（已在 Task 6 删除 DP）

- [ ] **Step 2: SidebarControl.xaml.cs 添加 3 个新 DP**

在 `SidebarControl.xaml.cs` 的 `#region NavigationItemsSource` 之前，追加 3 个新 DP：

```csharp
#region HomeNavItems - 主页组导航项

public ObservableCollection<NavigationItem> HomeNavItems
{
    get => (ObservableCollection<NavigationItem>)GetValue(HomeNavItemsProperty);
    set => SetValue(HomeNavItemsProperty, value);
}

public static readonly DependencyProperty HomeNavItemsProperty =
    DependencyProperty.Register(nameof(HomeNavItems), typeof(ObservableCollection<NavigationItem>),
        typeof(SidebarControl), new PropertyMetadata(null));

#endregion

#region BusinessNavItems - 业务组导航项

public ObservableCollection<NavigationItem> BusinessNavItems
{
    get => (ObservableCollection<NavigationItem>)GetValue(BusinessNavItemsProperty);
    set => SetValue(BusinessNavItemsProperty, value);
}

public static readonly DependencyProperty BusinessNavItemsProperty =
    DependencyProperty.Register(nameof(BusinessNavItems), typeof(ObservableCollection<NavigationItem>),
        typeof(SidebarControl), new PropertyMetadata(null));

#endregion

#region AdminNavItems - 管理组导航项

public ObservableCollection<NavigationItem> AdminNavItems
{
    get => (ObservableCollection<NavigationItem>)GetValue(AdminNavItemsProperty);
    set => SetValue(AdminNavItemsProperty, value);
}

public static readonly DependencyProperty AdminNavItemsProperty =
    DependencyProperty.Register(nameof(AdminNavItems), typeof(ObservableCollection<NavigationItem>),
        typeof(SidebarControl), new PropertyMetadata(null));

#endregion
```

> **可选清理**：如果旧的 `NavigationItemsSource` DP 在新设计下完全无用（XAML 已不引用），可以一并删除。但保留也不影响功能。建议保留以避免意外破坏其他引用方。本步骤不强制删除。

- [ ] **Step 3: 验证编译**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeded, 0 errors

- [ ] **Step 4: 提交**

```bash
git add src/Client/Desktop/Shell/Views/MainWindow.xaml src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml.cs
git commit -m "feat(desktop): bind 3 nav groups in MainWindow, remove dead NavigateToSystemSettingsCommand binding"
```

---

## Task 8: 全量构建 + 手工验证

**Covers:** [S10] [S11] (全部 bug 修复验证)

**Files:** (无代码改动，仅验证)

- [ ] **Step 1: 全量构建**

Run: `dotnet build LYBTZYZS.sln`
Expected: Build succeeded, 0 errors, 0 warnings (或仅有已知的非相关 warning)

- [ ] **Step 2: 运行 Desktop 测试**

Run: `dotnet test tests/LYBT.Tests.Desktop/`
Expected: All tests PASS（包含 Task 1 的 2 个新测试）

- [ ] **Step 3: 手工 UI 验证清单**

启动 Desktop 应用，按以下清单逐项验证：

| # | 验证点 | 操作 | 预期 |
|---|--------|------|------|
| 1 | 启动默认展开 | 登录后看侧边栏 | 默认 220px 展开 |
| 2 | 用户卡显示 | 看用户卡区域 | avatar + 真实姓名 + 角色描述 |
| 3 | 主页组渲染 | 看顶部 | "🏠 主页" 项存在，无分组标题 |
| 4 | 业务组渲染 | 看中部 | "业务" 小标题 + 该角色业务项 |
| 5 | 管理组渲染 | Admin 登录看底部 | "管理" 小标题 + 用户管理/报表 |
| 6 | 管理组角色过滤 | 医生登录 | "管理" 组不显示 |
| 7 | 展开动画 | Ctrl+M 切换 | 200ms 平滑动画 |
| 8 | 折叠态信息 | 折叠后看用户卡 | 仅 avatar，hover 显示 tooltip |
| 9 | 折叠态状态条 | 折叠后看底部 | 3 个图标（API/模式/时钟）水平排列 |
| 10 | 折叠态 Popup | 折叠态点 avatar | Popup 弹出在 avatar 下方 |
| 11 | 用户卡主体点击 | 展开态点用户卡主体 | 跳转 AccountSettingsView |
| 12 | ⋯ 按钮点击 | 展开态点 ⋯ | Popup 弹出 |
| 13 | Popup 个人资料 | 点 Popup "个人资料" | 跳转 AccountSettingsView |
| 14 | Popup 退出登录 | 点 Popup "退出登录" | 触发登出流程 |
| 15 | 品牌行点击 | 点 "☰ 凌隐宝堂" | 跳转主页 |
| 16 | 编辑后同步 | 改 RealName 保存 | 侧边栏用户卡姓名立即刷新 |
| 17 | 时钟刷新 | 等几秒 | 时间每秒更新 |

- [ ] **Step 4: 4 角色回归测试**

依次用 `admin`/`sysadmin`/`doctor`/`receptionist` 登录，验证：
- 每个角色看到正确的导航项
- 分组渲染正确（业务/管理角色差异）
- 侧边栏外壳完全一致（仅按钮列表不同）

- [ ] **Step 5: 提交验证记录（可选）**

如果有任何修复，单独提交。否则跳到下一步。

---

## Task 9: 子代理 diff 审查

**Covers:** 全局质量门

- [ ] **Step 1: 触发 compose:review 子代理**

使用 `compose:review` 子代理审查本次改动的 diff：
- 重点：XAML 绑定路径正确性、DP 注册完整性、动画 Storyboard 语法
- 关注：是否还有死代码、未引用的 DP、未处理的边界情况

- [ ] **Step 2: 修复审查发现的问题**

按子代理反馈修复，每修一处单独提交。

---

## Spec 覆盖检查

| Spec Section | 覆盖任务 |
|--------------|---------|
| [S1] 问题诊断 | Task 5/6/7（修复对应 Root Cause） |
| [S2] 设计目标与约束 | 全部任务（约束遵守） |
| [S3] 整体布局 | Task 5 |
| [S4] 用户卡 + 账户菜单交互 | Task 3, 4, 5, 6 |
| [S5] 导航分组 | Task 1, 3, 5 |
| [S6] 状态条 | Task 5 |
| [S7] 折叠态信息保留矩阵 | Task 5 |
| [S8] 宽度动画 | Task 5 |
| [S9] 文件改动清单 | 全部任务（按文件清单执行） |
| [S10] Bug 修复映射表 | Task 5/6/7（R1-R7） + Task 3/4（R8） |
| [S11] 测试覆盖 | Task 1（单元）+ Task 8（UI） |
| [S12] 实现顺序 | Task 1-9 顺序一致 |

所有 spec section 均被至少一个任务覆盖。✓
