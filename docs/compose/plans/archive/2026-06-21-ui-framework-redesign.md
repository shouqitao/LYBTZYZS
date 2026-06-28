# UI/UX 基础框架重设计 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Redesign Desktop shell UI/UX — sidebar becomes role-filtered navigation hub, eliminate info duplication, unify design tokens, standardize card styles with vector icons.

**Architecture:** Modify existing SidebarControl/GlobalStatusBar/MainWindow XAML + code-behind. Add NavigationItemsSource DependencyProperty to SidebarControl. Merge token files. Replace emoji with Path icons.

**Tech Stack:** WPF + Prism + CommunityToolkit.Mvvm + XAML ResourceDictionary

---

## File Structure

```
Modified:
  src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml     ← 重写：加导航菜单
  src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml.cs  ← 加 NavigationItemsSource DP
  src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/GlobalStatusBar.xaml     ← 精简：删除时钟/用户名/API
  src/Client/Desktop/Shell/Views/MainWindow.xaml                                  ← 删除内联连接徽章
  src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs                       ← 绑定导航项
  src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/DesignSystem.xaml           ← 合并令牌
  src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/DesignTokens/Spacing.xaml   ← 删除（合并到 DesignSystem）
  src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/HomePageStyles.xaml         ← 加矢量图标 Path data
  src/Client/Desktop/Roles/LYBT.Desktop.Admin/Views/AdminHomeView.xaml             ← 换矢量图标
  src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/ClinicalHomeView.xaml       ← 换矢量图标 + 删重复样式
  src/Client/Desktop/Roles/LYBT.Desktop.Receptionist/Views/ReceptionistHomeView.xaml ← 换矢量图标 + 删重复样式
  src/Client/Desktop/Shell/App.xaml                                                ← 删除字体覆盖

Deleted:
  src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/BreadcrumbBar.xaml(.cs)   ← 死代码

Created:
  src/Client/Desktop/Core/LYBT.Desktop.Controls/Models/NavigationItem.cs           ← 导航项数据模型
  src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/Icons.xaml                  ← 矢量图标 Path data
```

---

## Task 1: 创建 NavigationItem 模型 + Icons.xaml

**Covers:** [S4, S8]

**Files:**
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Models/NavigationItem.cs`
- Create: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/Icons.xaml`

- [ ] **Step 1: Create NavigationItem model**

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
    public string IconKey { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public ICommand? Command { get; set; }
}
```

- [ ] **Step 2: Create Icons.xaml with vector Path data for all navigation + card icons**

```xml
<ResourceDictionary xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
    <!-- Navigation icons (24x24) -->
    <x:String x:Key="IconHome">M12,3L2,12H5V20H10V14H14V20H19V12H22L12,3Z</x:String>
    <x:String x:Key="IconPatients">M16,11C18,11 20,9 20,7C20,5 18,3 16,3C14,3 12,5 12,7C12,9 14,11 16,11M6,11C8,11 10,9 10,7C10,5 8,3 6,3C4,3 2,5 2,7C2,9 4,11 6,11M6,13C4,13 2,13 2,15V17C2,18 3,19 4,19H8C9,19 10,18 10,17V15C10,13 8,13 6,13M16,13C14,13 12,13 12,15V17C12,18 13,19 14,19H18C19,19 20,18 20,17V15C20,13 18,13 16,13</x:String>
    <x:String x:Key="IconHerbs">M12,2C8,2 5,5 5,9C5,13 8,16 12,16C16,16 19,13 19,9C19,5 16,2 12,2M12,14C9,14 7,12 7,9C7,6 9,4 12,4C15,4 17,6 17,9C17,12 15,14 12,14M11,17H13V22H11V17Z</x:String>
    <x:String x:Key="IconFormula">M14,2H6C5,2 4,3 4,4V20C4,21 5,22 6,22H18C19,22 20,21 20,20V8L14,2M16,18H8V16H16V18M16,14H8V12H16V14M13,9V3.5L18.5,9H13Z</x:String>
    <x:String x:Key="IconMedicalCase">M19,3H5C4,3 3,4 3,5V19C3,20 4,21 5,21H19C20,21 21,20 21,19V5C21,4 20,3 19,3M12,17L7,12L9,10L11,12V7H13V12L15,10L17,12L12,17Z</x:String>
    <x:String x:Key="IconUsers">M12,4A4,4 0 0,1 16,8A4,4 0 0,1 12,12A4,4 0 0,1 8,8A4,4 0 0,1 12,4M12,14C16,14 20,16 20,18V20H4V18C4,16 8,14 12,14Z</x:String>
    <x:String x:Key="IconReports">M3,3H21V5H3V3M3,7H21V9H3V7M3,11H21V13H3V11M3,15H15V17H3V15M3,19H15V21H3V19Z</x:String>
    <x:String x:Key="IconSettings">M12,8A4,4 0 0,1 16,12A4,4 0 0,1 12,16A4,4 0 0,1 8,12A4,4 0 0,1 12,8M12,10A2,2 0 0,0 10,12A2,2 0 0,0 12,14A2,2 0 0,0 14,12A2,2 0 0,0 12,10M19,12C19,12.4 19,12.8 18.9,13.2L20.9,14.8L18.9,18.3L16.5,17.3C15.9,17.8 15.2,18.2 14.5,18.5L14.1,21H10L9.6,18.5C8.9,18.2 8.2,17.8 7.6,17.3L5.2,18.3L3.2,14.8L5.2,13.2C5.1,12.8 5,12.4 5,12C5,11.6 5.1,11.2 5.2,10.8L3.2,9.2L5.2,5.7L7.6,6.7C8.2,6.2 8.9,5.8 9.6,5.5L10,3H14L14.4,5.5C15.1,5.8 15.8,6.2 16.4,6.7L18.8,5.7L20.8,9.2L18.8,10.8C18.9,11.2 19,11.6 19,12Z</x:String>
    <x:String x:Key="IconLogout">M16,17V14H9V10H16V7L21,12L16,17M14,2H4C3,2 2,3 2,4V20C2,21 3,22 4,22H14C15,22 16,21 16,20V18H14V20H4V4H14V6H16V4C16,3 15,2 14,2Z</x:String>
    <!-- Status icons -->
    <x:String x:Key="IconApiHealthy">M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M11,16.5L18,9.5L16.6,8.1L11,13.7L7.4,10.1L6,11.5L11,16.5Z</x:String>
    <x:String x:Key="IconApiUnhealthy">M12,2A10,10 0 0,1 22,12A10,10 0 0,1 12,22A10,10 0 0,1 2,12A10,10 0 0,1 12,2M15,9L9,15M15,15L9,9</x:String>
</ResourceDictionary>
```

- [ ] **Step 3: Add Icons.xaml to App.xaml MergedDictionaries**

In `src/Client/Desktop/Shell/App.xaml`, add after DesignSystem.xaml:

```xml
<ResourceDictionary Source="/LYBT.Desktop.Controls;component/Themes/Icons.xaml" />
```

- [ ] **Step 4: Build + Commit**

```bash
dotnet build LYBTZYZS.sln
git add -A && git commit -m "feat(ui): create NavigationItem model + Icons.xaml vector path data"
```

---

## Task 2: 重写 SidebarControl — 角色导航菜单

**Covers:** [S4, S5]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml.cs` — add NavigationItemsSource DP
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml` — rewrite layout

- [ ] **Step 1: Add NavigationItemsSource + ConnectionDisplay DependencyProperty to SidebarControl.xaml.cs**

Add after existing DPs:

```csharp
using System.Collections.ObjectModel;
using LYBT.Desktop.Controls.Models;

// In the class:
#region NavigationItemsSource - 导航菜单项

public ObservableCollection<NavigationItem> NavigationItemsSource
{
    get => (ObservableCollection<NavigationItem>)GetValue(NavigationItemsSourceProperty);
    set => SetValue(NavigationItemsSourceProperty, value);
}

public static readonly DependencyProperty NavigationItemsSourceProperty =
    DependencyProperty.Register(nameof(NavigationItemsSource), typeof(ObservableCollection<NavigationItem>),
        typeof(SidebarControl), new PropertyMetadata(null));

#endregion

#region ConnectionDisplay - 连接模式显示文字

public string ConnectionDisplay
{
    get => (string)GetValue(ConnectionDisplayProperty);
    set => SetValue(ConnectionDisplayProperty, value);
}

public static readonly DependencyProperty ConnectionDisplayProperty =
    DependencyProperty.Register(nameof(ConnectionDisplay), typeof(string), typeof(SidebarControl),
        new PropertyMetadata(string.Empty));

#endregion

#region IsRemoteMode - 是否远程模式

public bool IsRemoteMode
{
    get => (bool)GetValue(IsRemoteModeProperty);
    set => SetValue(IsRemoteModeProperty, value);
}

public static readonly DependencyProperty IsRemoteModeProperty =
    DependencyProperty.Register(nameof(IsRemoteMode), typeof(bool), typeof(SidebarControl),
        new PropertyMetadata(false));

#endregion
```

- [ ] **Step 2: Rewrite SidebarControl.xaml — 3-zone layout (brand+user / navigation / status+logout)**

The new XAML structure:
```
Grid (6 rows)
  Row 0: Hamburger + Logo text
  Row 1: User avatar + name + role
  Row 2: Navigation ItemsControl (bound to NavigationItemsSource)
  Row 3: Spacer (*)
  Row 4: Clock + API status + connection mode
  Row 5: Logout button
```

Each navigation item renders as: `<Button>` with `<Path Data="{Binding IconKey}" />` + `<TextBlock Text="{Binding Title}" />`.
The button's `Command` binds to `NavigationItem.Command`.

The clock shows `CurrentTime` in `HH:mm` format (collapsed mode) or `yyyy-MM-dd HH:mm` (expanded mode).
API status shows a Path icon (green check or red X) + text.
Connection mode shows "远程" or "本地" with color coding.

- [ ] **Step 3: Build + Commit**

```bash
dotnet build LYBTZYZS.sln
git add -A && git commit -m "feat(ui): rewrite SidebarControl with role-filtered navigation menu"
```

---

## Task 3: MainWindowViewModel — 绑定导航项到侧边栏

**Covers:** [S4]

**Files:**
- Modify: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`

- [ ] **Step 1: Build NavigationItems from role definition**

In `MainWindowViewModel`, add a method that creates `NavigationItem` list from `IRoleRegistry.GetDefinition(role)`:

```csharp
private ObservableCollection<NavigationItem> BuildNavigationItems(UserRole role)
{
    var definition = _roleRegistry.GetDefinition(role);
    var items = new ObservableCollection<NavigationItem>();

    // Always add Home
    items.Add(new NavigationItem
    {
        Title = "主页",
        ViewName = definition.HomeViewName,
        IconKey = (string)Application.Current.FindResource("IconHome"),
        Command = new DelegateCommand(() => _navigationCoordinator.NavigateToHome())
    });

    // Add module navigation items based on RequiredModules
    if (definition.RequiredModules.Contains("PatientsModule"))
        items.Add(CreateNavItem("患者管理", ViewNames.PatientManagement, "IconPatients"));
    if (definition.RequiredModules.Contains("HerbsModule"))
        items.Add(CreateNavItem("药材管理", ViewNames.HerbManagement, "IconHerbs"));
    if (definition.RequiredModules.Contains("FormulaModule"))
        items.Add(CreateNavItem("验方管理", ViewNames.FormulaManagement, "IconFormula"));
    if (definition.RequiredModules.Contains("MedicalCaseModule"))
        items.Add(CreateNavItem("医案管理", ViewNames.MedicalCaseManagement, "IconMedicalCase"));
    if (definition.RequiredModules.Contains("UsersModule") && role is UserRole.Admin or UserRole.SuperAdmin)
        items.Add(CreateNavItem("用户管理", ViewNames.UserManagement, "IconUsers"));
    if (definition.RequiredModules.Contains("ReportsModule") || definition.GetAllModules().Contains("ReportsModule"))
        items.Add(CreateNavItem("统计报表", ViewNames.ReportsHome, "IconReports"));

    return items;
}

private NavigationItem CreateNavItem(string title, string viewName, string iconKey) =>
    new()
    {
        Title = title,
        ViewName = viewName,
        IconKey = (string)Application.Current.FindResource(iconKey),
        Command = new DelegateCommand(() => _navigationCoordinator.NavigateTo(viewName))
    };
```

- [ ] **Step 2: Call BuildNavigationItems in OnLoginCoordinatorSuccess**

After login success, build navigation items and assign to SidebarControl via binding.

- [ ] **Step 3: Bind in MainWindow.xaml**

Update the `<controls:SidebarControl>` element to include:
```xml
NavigationItemsSource="{Binding NavigationItems}"
ConnectionDisplay="{Binding ConnectionModeDisplay}"
IsRemoteMode="{Binding IsRemoteMode}"
```

- [ ] **Step 4: Build + Commit**

```bash
dotnet build LYBTZYZS.sln
git add -A && git commit -m "feat(ui): bind role-filtered navigation items to SidebarControl"
```

---

## Task 4: 精简 GlobalStatusBar + 删除 MainWindow 内联徽章

**Covers:** [S5, S6, S9]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/GlobalStatusBar.xaml` — remove clock/user/api columns
- Modify: `src/Client/Desktop/Shell/Views/MainWindow.xaml` — remove connection badge Border

- [ ] **Step 1: Simplify GlobalStatusBar to 2 columns**

Remove columns for: loading spinner (keep but simplify), connection URL, status message (keep), progress, username, clock.

New layout: `Grid Columns="*,Auto"`:
- Left: loading indicator + status message
- Right: version number

- [ ] **Step 2: Remove connection badge from MainWindow.xaml**

Delete the entire `<Border DockPanel.Dock="Right" ...>` connection mode badge block (~35 lines of inline Style+DataTrigger).

- [ ] **Step 3: Build + Commit**

```bash
dotnet build LYBTZYZS.sln
git add -A && git commit -m "refactor(ui): simplify GlobalStatusBar to 2 columns + remove MainWindow inline connection badge"
```

---

## Task 5: 统一设计令牌

**Covers:** [S7]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/DesignSystem.xaml` — merge spacing/radius tokens
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/DesignTokens/Spacing.xaml` — merged into DesignSystem
- Modify: `src/Client/Desktop/Shell/App.xaml` — remove font override, update MergedDictionaries
- Fix: All XAML files referencing `SpacingSM/MD/LG` from Spacing.xaml → use DesignSystem equivalents

- [ ] **Step 1: Add unified spacing + radius tokens to DesignSystem.xaml**

Add these tokens to DesignSystem.xaml (keeping existing colors/fonts):

```xml
<!-- Unified Spacing Tokens (merges Spacing.xaml) -->
<Thickness x:Key="SpacingXS">4</Thickness>
<Thickness x:Key="SpacingSM">8</Thickness>
<Thickness x:Key="SpacingMD">12</Thickness>
<Thickness x:Key="SpacingLG">16</Thickness>
<Thickness x:Key="SpacingXL">24</Thickness>
<Thickness x:Key="SpacingXXL">32</Thickness>

<!-- Unified Radius Tokens -->
<CornerRadius x:Key="RadiusSM">4</CornerRadius>
<CornerRadius x:Key="RadiusMD">8</CornerRadius>
<CornerRadius x:Key="RadiusLG">12</CornerRadius>
```

- [ ] **Step 2: Delete Spacing.xaml, update App.xaml**

Remove `<ResourceDictionary Source=".../DesignTokens/Spacing.xaml" />` from App.xaml.

- [ ] **Step 3: Remove font override from App.xaml**

Delete the three inline `<Style>` blocks that override TextBlock/TextBox/Button FontFamily to "Microsoft YaHei".

- [ ] **Step 4: Find and fix all references to old Spacing.xaml tokens**

```bash
rg "SpacingSM|SpacingMD|SpacingLG|SpacingXL|SpacingXXL|RadiusSM|RadiusMD|RadiusLG|SpaceSM|SpaceMD|SpaceLG" src/Client/Desktop --include *.xaml -l
```

Update each to use the DesignSystem token names. Most will already match since we kept the same naming.

- [ ] **Step 5: Build + Commit**

```bash
dotnet build LYBTZYZS.sln
git add -A && git commit -m "refactor(ui): unify design tokens — merge Spacing.xaml into DesignSystem + remove font override"
```

---

## Task 6: 统一卡片样式 + 替换 emoji 为矢量图标

**Covers:** [S8, S10]

**Files:**
- Modify: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Themes/HomePageStyles.xaml` — update FunctionCardStyle with Path icon
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Admin/Views/AdminHomeView.xaml` — use Button + Path
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Clinical/Views/ClinicalHomeView.xaml` — use Button + Path, delete local TransparentButtonStyle
- Modify: `src/Client/Desktop/Roles/LYBT.Desktop.Receptionist/Views/ReceptionistHomeView.xaml` — use Button + Path, delete local TransparentButtonStyle

- [ ] **Step 1: Update FunctionCardStyle to support vector Path icons**

Add an `IconData` attached property or convention: each card Button sets a Path in its Content with `Data="{DynamicResource IconXxx}"`.

- [ ] **Step 2: Replace emoji with Path icons in AdminHomeView**

Replace each card's emoji (`👤 🌿 🏥 📋 📁 ⚙ 📊`) with:
```xml
<Path Width="32" Height="32" Data="{DynamicResource IconUsers}" Fill="{DynamicResource PrimaryBrush}" Stretch="Uniform" />
```

Also change `<Border.InputBindings><MouseBinding>` to proper `<Button>` pattern.

- [ ] **Step 3: Replace emoji + delete local styles in ClinicalHomeView and ReceptionistHomeView**

Same icon replacement. Delete local `TransparentButtonStyle` definitions (use the one from HomePageStyles).

- [ ] **Step 4: Build + Commit**

```bash
dotnet build LYBTZYZS.sln
git add -A && git commit -m "feat(ui): replace emoji with vector Path icons + unify card click pattern to Button"
```

---

## Task 7: 删除死代码 + 最终验证

**Covers:** [S9, S10]

**Files:**
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/BreadcrumbBar.xaml`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/BreadcrumbBar.xaml.cs`

- [ ] **Step 1: Verify BreadcrumbBar is dead code**

```bash
rg "BreadcrumbBar" src/Client/Desktop -g "*.cs" -g "*.xaml" -l
```

If only referenced by itself, delete both files.

- [ ] **Step 2: Delete files**

- [ ] **Step 3: Full build + test**

```bash
dotnet build LYBTZYZS.sln
dotnet test tests/LYBT.Tests.Architecture/ --filter "CustomControl"
```

- [ ] **Step 4: Manual verification**

Launch Desktop app, login as admin, verify:
1. Sidebar shows navigation items (主页/患者/药材/验方/医案/用户/报表)
2. Clicking a nav item navigates to the correct view
3. Status bar only shows loading + version
4. Clock only in sidebar
5. API status only in sidebar
6. Cards use vector icons not emoji

- [ ] **Step 5: Commit + Push**

```bash
git add -A && git commit -m "refactor(ui): delete dead BreadcrumbBar + final verification" && git push origin master
```
