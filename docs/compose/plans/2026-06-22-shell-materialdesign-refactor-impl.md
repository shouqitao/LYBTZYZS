# Shell Material Design Refactor — Implementation Plan

> [!NOTE]
> This document may not reflect the current implementation.
> See the final report for up-to-date state:
> [Final Report](../reports/shell-materialdesign-refactor.md)

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the post-login Shell with MDIX MaterialDesignWindow + DrawerHost architecture, unifying theme (Brown/Amber), dialogs (MDIX DialogHost), and notifications (SnackbarHost).

**Architecture:** Clean-slate rewrite of MainWindow.xaml using MDIX controls. New ThemeService for Light/Dark toggle. DialogHostService replaces Prism IDialogService. SnackbarService replaces custom ToastService. LoginView stays unchanged.

**Tech Stack:** MaterialDesignInXaml 5.3.2, Prism.Wpf 9.x, CommunityToolkit.Mvvm 8.x, .NET 8

---

## File Map

| Action | File | Purpose |
|--------|------|---------|
| CREATE | `Shell/Services/ThemeService.cs` | MDIX Light/Dark toggle via PaletteHelper |
| CREATE | `Shell/Services/DialogHostService.cs` | MDIX DialogHost wrapper (replaces Prism IDialogService) |
| CREATE | `Shell/Services/SnackbarService.cs` | MDIX SnackbarHost wrapper (replaces ToastService) |
| CREATE | `Shell/Services/ISnackbarService.cs` | Snackbar service interface |
| CREATE | `Shell/Services/IDialogHostService.cs` | Dialog host service interface |
| MODIFY | `Shell/Views/MainWindow.xaml` | Full rewrite: MaterialDesignWindow + DrawerHost |
| MODIFY | `Shell/ViewModels/MainWindowViewModel.cs` | Add IsDrawerOpen, IsDarkMode, ToggleDrawerCommand |
| MODIFY | `Shell/App.xaml` | Update BundledTheme to Brown+Amber |
| MODIFY | `Shell/App.xaml.cs` | Register ThemeService, DialogHostService, SnackbarService |
| DELETE | `Core/Controls/SidebarControl.xaml` + `.cs` | Replaced by DrawerHost.LeftDrawerContent |
| DELETE | `Core/Controls/GlobalStatusBar.xaml` + `.cs` | Replaced by TopBar |

---

## Task 1: Create Service Interfaces

**Covers:** [S5] MDIX Dialog/Snackbar services

**Files:**
- Create: `src/Client/Desktop/Shell/Services/IDialogHostService.cs`
- Create: `src/Client/Desktop/Shell/Services/ISnackbarService.cs`

- [ ] **Step 1: Create IDialogHostService**

```csharp
// src/Client/Desktop/Shell/Services/IDialogHostService.cs
namespace LYBT.Desktop.Shell.Services;

public interface IDialogHostService
{
    Task<bool> ShowConfirmationAsync(string message, string title = "确认");
    Task<T?> ShowCustomDialogAsync<T>(object dialogContent) where T : class;
}
```

- [ ] **Step 2: Create ISnackbarService**

```csharp
// src/Client/Desktop/Shell/Services/ISnackbarService.cs
namespace LYBT.Desktop.Shell.Services;

public interface ISnackbarService
{
    void ShowSuccess(string message, int durationMs = 3000);
    void ShowError(string message, int durationMs = 5000);
    void ShowInfo(string message, int durationMs = 3000);
}
```

- [ ] **Step 3: Verify compilation**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj --no-restore`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Shell/Services/IDialogHostService.cs src/Client/Desktop/Shell/Services/ISnackbarService.cs
git commit -m "feat(shell): add IDialogHostService and ISnackbarService interfaces"
```

---

## Task 2: Create Service Implementations

**Covers:** [S5] MDIX Dialog/Snackbar services

**Files:**
- Create: `src/Client/Desktop/Shell/Services/DialogHostService.cs`
- Create: `src/Client/Desktop/Shell/Services/SnackbarService.cs`

- [ ] **Step 1: Create DialogHostService**

```csharp
// src/Client/Desktop/Shell/Services/DialogHostService.cs
using MaterialDesignThemes.Wpf;

namespace LYBT.Desktop.Shell.Services;

public class DialogHostService : IDialogHostService
{
    private const string RootDialog = "RootDialog";

    public async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
    {
        var view = new Dialogs.Views.ConfirmationDialog
        {
            DataContext = new Dialogs.ViewModels.ConfirmationDialogViewModel(message, title)
        };
        var result = await DialogHost.Show(view, RootDialog);
        return result is true;
    }

    public async Task<T?> ShowCustomDialogAsync<T>(object dialogContent) where T : class
    {
        var result = await DialogHost.Show(dialogContent, RootDialog);
        return result as T;
    }
}
```

- [ ] **Step 2: Create SnackbarService**

```csharp
// src/Client/Desktop/Shell/Services/SnackbarService.cs
using MaterialDesignThemes.Wpf;

namespace LYBT.Desktop.Shell.Services;

public class SnackbarService : ISnackbarService
{
    private readonly ISnackbarMessageQueue _messageQueue;

    public SnackbarService(ISnackbarMessageQueue messageQueue)
    {
        _messageQueue = messageQueue;
    }

    public void ShowSuccess(string message, int durationMs = 3000) =>
        _messageQueue.Enqueue(message, "关闭", () => { },
            true, true, TimeSpan.FromMilliseconds(durationMs));

    public void ShowError(string message, int durationMs = 5000) =>
        _messageQueue.Enqueue(message, "关闭", () => { },
            true, true, TimeSpan.FromMilliseconds(durationMs));

    public void ShowInfo(string message, int durationMs = 3000) =>
        _messageQueue.Enqueue(message, "关闭", () => { },
            true, true, TimeSpan.FromMilliseconds(durationMs));
}
```

- [ ] **Step 3: Verify compilation**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj --no-restore`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Shell/Services/DialogHostService.cs src/Client/Desktop/Shell/Services/SnackbarService.cs
git commit -m "feat(shell): implement DialogHostService and SnackbarService"
```

---

## Task 3: Create ThemeService

**Covers:** [S4] Theme system

**Files:**
- Create: `src/Client/Desktop/Shell/Services/ThemeService.cs`

- [ ] **Step 1: Create ThemeService**

```csharp
// src/Client/Desktop/Shell/Services/ThemeService.cs
using CommunityToolkit.Mvvm.ComponentModel;
using MaterialDesignThemes.Wpf;

namespace LYBT.Desktop.Shell.Services;

public partial class ThemeService : ObservableObject
{
    private readonly PaletteHelper _paletteHelper = new();

    [ObservableProperty]
    private bool _isDarkMode;

    public void ToggleTheme()
    {
        IsDarkMode = !IsDarkMode;
        var theme = _paletteHelper.GetTheme();
        theme.SetBaseTheme(IsDarkMode ? BaseTheme.Dark : BaseTheme.Light);
        _paletteHelper.SetTheme(theme);
    }

    public void InitializeThemeSync()
    {
        if (_paletteHelper.GetThemeManager() is { } themeManager)
        {
            themeManager.ThemeChanged += (_, e) =>
            {
                IsDarkMode = e.NewTheme?.GetBaseTheme() == BaseTheme.Dark;
            };
        }
    }
}
```

- [ ] **Step 2: Verify compilation**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj --no-restore`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add src/Client/Desktop/Shell/Services/ThemeService.cs
git commit -m "feat(shell): add ThemeService for Light/Dark toggle"
```

---

## Task 4: Update App.xaml Color Scheme

**Covers:** [S4] Theme system — Brown+Amber

**Files:**
- Modify: `src/Client/Desktop/Shell/App.xaml`

- [ ] **Step 1: Update BundledTheme**

Replace the existing `BundledTheme` in `App.xaml`:

```xml
<materialDesign:BundledTheme
    BaseTheme="Light"
    PrimaryColor="Brown"
    SecondaryColor="Amber" />
```

- [ ] **Step 2: Verify XAML parses**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj --no-restore`
Expected: PASS

- [ ] **Step 3: Commit**

```bash
git add src/Client/Desktop/Shell/App.xaml
git commit -m "feat(shell): update theme to Brown+Amber color scheme"
```

---

## Task 5: Register Services in App.xaml.cs

**Covers:** [S6] Service registration

**Files:**
- Modify: `src/Client/Desktop/Shell/App.xaml.cs`

- [ ] **Step 1: Add using directives**

```csharp
using LYBT.Desktop.Shell.Services;
using MaterialDesignThemes.Wpf;
```

- [ ] **Step 2: Register new services in RegisterTypes**

Add after existing registrations:

```csharp
// MDIX services
containerRegistry.RegisterSingleton<PaletteHelper>();
containerRegistry.RegisterSingleton<ThemeService>();
containerRegistry.RegisterSingleton<ISnackbarService, SnackbarService>();
containerRegistry.RegisterSingleton<IDialogHostService, DialogHostService>();
```

- [ ] **Step 3: Verify compilation**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj --no-restore`
Expected: PASS

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Shell/App.xaml.cs
git commit -m "feat(shell): register ThemeService, DialogHostService, SnackbarService"
```

---

## Task 6: Add All New ViewModel Properties and Commands

**Covers:** [S3] Architecture — ViewModel layering

**Files:**
- Modify: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`

- [ ] **Step 1: Add using directive**

```csharp
using MaterialDesignThemes.Wpf;
```

- [ ] **Step 2: Add new observable properties**

Add after existing `[ObservableProperty]` fields:

```csharp
/// <summary>
/// 左侧抽屉是否展开 (MDIX DrawerHost)
/// </summary>
[ObservableProperty]
private bool _isDrawerOpen = true;

/// <summary>
/// 是否深色模式
/// </summary>
[ObservableProperty]
private bool _isDarkMode;
```

- [ ] **Step 3: Add ThemeService dependency**

Add to constructor parameters and field:

```csharp
private readonly ThemeService _themeService;

// In constructor:
_themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
```

- [ ] **Step 4: Add computed properties for XAML binding**

```csharp
/// <summary>
/// 用户名首字母（用于头像显示）
/// </summary>
public string CurrentUserInitial =>
    CurrentUser != null && !string.IsNullOrEmpty(CurrentUser.UserName)
        ? CurrentUser.UserName[..1].ToUpper()
        : "?";

/// <summary>
/// 用户角色显示文本
/// </summary>
public string CurrentUserRoleDisplay =>
    CurrentUser?.Role switch
    {
        UserRole.SuperAdmin => "超级管理员",
        UserRole.Admin => "管理员",
        UserRole.Doctor => "医生",
        UserRole.Receptionist => "前台",
        _ => string.Empty
    };

/// <summary>
/// API 状态图标
/// </summary>
public PackIconKind ApiStatusIcon => ApiStatus switch
{
    ApiHealthStatus.Healthy => PackIconKind.Wifi,
    ApiHealthStatus.Unhealthy => PackIconKind.WifiOff,
    _ => PackIconKind.WifiStrengthAlertOutline
};

/// <summary>
/// API 状态颜色
/// </summary>
public Brush ApiStatusColor => ApiStatus switch
{
    ApiHealthStatus.Healthy => Brushes.Green,
    ApiHealthStatus.Unhealthy => Brushes.Orange,
    _ => Brushes.Gray
};
```

- [ ] **Step 5: Add NotifyPropertyChangedFor attributes**

Update existing properties:

```csharp
[ObservableProperty]
[NotifyPropertyChangedFor(nameof(CurrentUserInitial))]
[NotifyPropertyChangedFor(nameof(CurrentUserRoleDisplay))]
[NotifyPropertyChangedFor(nameof(CurrentUserDisplayName))]
private UserDetailDto? _currentUser;

[ObservableProperty]
[NotifyPropertyChangedFor(nameof(ApiStatusIcon))]
[NotifyPropertyChangedFor(nameof(ApiStatusColor))]
private ApiHealthStatus _apiStatus = ApiHealthStatus.Checking;
```

- [ ] **Step 6: Add ToggleDrawer command**

Replace existing `ToggleSidebar` with:

```csharp
/// <summary>
/// 切换左侧抽屉展开/收起 (Ctrl+M)
/// </summary>
[RelayCommand]
private void ToggleDrawer() => IsDrawerOpen = !IsDrawerOpen;
```

- [ ] **Step 7: Add theme toggle handler**

```csharp
partial void OnIsDarkModeChanged(bool value) => _themeService.ToggleTheme();
```

- [ ] **Step 8: Update OnSelectedNavItemChanged to close drawer**

```csharp
partial void OnSelectedNavItemChanged(NavigationItem? value)
{
    if (value?.ViewName is string viewName && !string.IsNullOrEmpty(viewName))
    {
        _navigationCoordinator.NavigateTo(viewName);
        IsDrawerOpen = false; // Close drawer after navigation
    }
}
```

- [ ] **Step 9: Verify compilation**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj --no-restore`
Expected: PASS

- [ ] **Step 10: Commit**

```bash
git add src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs
git commit -m "feat(shell): add MDIX properties, computed bindings, and ToggleDrawer to MainWindowViewModel"
```

---

## Task 7: Rewrite MainWindow.xaml

**Covers:** [S1] Shell layout, [S3] Architecture, [S5] MDIX controls

**Files:**
- Modify: `src/Client/Desktop/Shell/Views/MainWindow.xaml`

- [ ] **Step 1: Replace entire MainWindow.xaml**

Replace the full content with:

```xml
<Window
    x:Class="LYBT.Desktop.Shell.Views.MainWindow"
    xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
    xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
    xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
    xmlns:prism="http://prismlibrary.com/"
    xmlns:converters="clr-namespace:LYBT.Desktop.Controls.Converters;assembly=LYBT.Desktop.Controls"
    Style="{StaticResource MaterialDesignWindow}"
    Title="{Binding Title}"
    MinWidth="1024" MinHeight="768"
    WindowState="Maximized"
    prism:ViewModelLocator.AutoWireViewModel="True">

    <Window.Resources>
        <converters:Cvt x:Key="Cvt" />
    </Window.Resources>

    <Window.InputBindings>
        <KeyBinding Key="N" Command="{Binding QuickAddPatientCommand}" Modifiers="Ctrl" />
        <KeyBinding Key="C" Command="{Binding QuickStartMedicalCaseCommand}" Modifiers="Ctrl+Shift" />
        <KeyBinding Key="F1" Command="{Binding ShowHelpCommand}" />
        <KeyBinding Key="OemComma" Command="{Binding ShowSettingsCommand}" Modifiers="Ctrl" />
        <KeyBinding Key="M" Command="{Binding ToggleDrawerCommand}" Modifiers="Ctrl" />
        <KeyBinding Key="Left" Command="{Binding NavigateBackCommand}" Modifiers="Alt" />
        <KeyBinding Key="Right" Command="{Binding NavigateForwardCommand}" Modifiers="Alt" />
        <KeyBinding Key="Home" Command="{Binding NavigateToHomeCommand}" Modifiers="Alt" />
    </Window.InputBindings>

    <!-- Login (unchanged) -->
    <Grid Visibility="{Binding IsNotLoggedIn, Converter={x:Static converters:Cvt.BoolToVis}}">
        <ContentControl prism:RegionManager.RegionName="LoginRegion" />
    </Grid>

    <!-- Post-login Shell (MDIX native) -->
    <Grid Visibility="{Binding IsLoggedIn, Converter={x:Static converters:Cvt.BoolToVis}}">

        <materialDesign:DialogHost
            DialogTheme="Inherit"
            Identifier="RootDialog"
            SnackbarMessageQueue="{Binding ElementName=MainSnackbar, Path=MessageQueue}">

            <materialDesign:SnackbarHost Name="MainSnackbar">

                <materialDesign:DrawerHost
                    IsLeftDrawerOpen="{Binding IsDrawerOpen, Mode=TwoWay}">

                    <!-- Left drawer -->
                    <materialDesign:DrawerHost.LeftDrawerContent>
                        <DockPanel MinWidth="280"
                                   Background="{DynamicResource MaterialDesign.Brush.Surface}">

                            <!-- Brand row -->
                            <DockPanel DockPanel.Dock="Top" LastChildFill="False">
                                <ToggleButton
                                    DockPanel.Dock="Right"
                                    IsChecked="{Binding IsDrawerOpen, Mode=TwoWay}"
                                    Style="{StaticResource MaterialDesignHamburgerToggleButton}" />
                                <StackPanel Orientation="Horizontal" Margin="16,12">
                                    <materialDesign:PackIcon Kind="Leaf" Width="24" Height="24"
                                                             Foreground="{DynamicResource MaterialDesign.Brush.Primary}" />
                                    <TextBlock Margin="12,0,0,0" FontSize="16" FontWeight="Bold"
                                               Text="凌隐宝堂" />
                                </StackPanel>
                            </DockPanel>

                            <!-- Navigation list (flat, no grouping) -->
                            <ListBox
                                ItemsSource="{Binding NavigationItems}"
                                SelectedItem="{Binding SelectedNavItem, Mode=TwoWay}"
                                Style="{StaticResource MaterialDesignNavigationPrimaryListBox}"
                                Margin="0,8">
                                <ListBox.ItemTemplate>
                                    <DataTemplate>
                                        <StackPanel Orientation="Horizontal" Margin="16,10">
                                            <materialDesign:PackIcon Kind="{Binding IconKind}"
                                                                     Width="22" Height="22"
                                                                     VerticalAlignment="Center" />
                                            <TextBlock Margin="20,0,0,0" FontSize="14"
                                                       VerticalAlignment="Center"
                                                       Text="{Binding Title}" />
                                        </StackPanel>
                                    </DataTemplate>
                                </ListBox.ItemTemplate>
                            </ListBox>

                            <!-- Separator -->
                            <Separator DockPanel.Dock="Bottom" Margin="16,0"
                                       Background="{DynamicResource MaterialDesign.Brush.Outline}" />

                            <!-- User card -->
                            <StackPanel DockPanel.Dock="Bottom" Margin="16,12">
                                <StackPanel Orientation="Horizontal" Margin="0,0,0,8">
                                    <Border Width="36" Height="36" CornerRadius="18"
                                            Background="{StaticResource SidebarAvatarBrush}">
                                        <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center"
                                                   FontSize="14" FontWeight="Bold" Foreground="White"
                                                   Text="{Binding CurrentUserInitial}" />
                                    </Border>
                                    <StackPanel Margin="12,0,0,0" VerticalAlignment="Center">
                                        <TextBlock FontSize="13" FontWeight="SemiBold"
                                                   Text="{Binding CurrentUserDisplayName}" />
                                        <TextBlock FontSize="11" Opacity="0.6"
                                                   Text="{Binding CurrentUserRoleDisplay}" />
                                    </StackPanel>
                                </StackPanel>
                                <Button Command="{Binding LogoutCommand}"
                                        Style="{StaticResource MaterialDesignFlatButton}"
                                        HorizontalAlignment="Stretch">
                                    <StackPanel Orientation="Horizontal">
                                        <materialDesign:PackIcon Kind="Logout" Width="18" Height="18" />
                                        <TextBlock Margin="12,0,0,0" Text="退出登录" />
                                    </StackPanel>
                                </Button>
                            </StackPanel>

                        </DockPanel>
                    </materialDesign:DrawerHost.LeftDrawerContent>

                    <!-- Main content -->
                    <DockPanel>

                        <!-- Top bar -->
                        <DockPanel DockPanel.Dock="Top" Height="48"
                                   Background="{DynamicResource MaterialDesign.Brush.Surface}"
                                   BorderBrush="{DynamicResource MaterialDesign.Brush.Outline}"
                                   BorderThickness="0,0,0,1">

                            <TextBlock DockPanel.Dock="Left" Margin="16,0"
                                       VerticalAlignment="Center"
                                       Style="{StaticResource MaterialDesignHeadline6TextBlock}"
                                       Text="{Binding CurrentPageTitle}" />

                            <StackPanel DockPanel.Dock="Right" Orientation="Horizontal"
                                        VerticalAlignment="Center" Margin="0,0,12,0">

                                <ToggleButton IsChecked="{Binding IsDarkMode}"
                                              Style="{StaticResource MaterialDesignSwitchToggleButton}"
                                              ToolTip="切换深色模式" />

                                <materialDesign:PackIcon
                                    Kind="{Binding ApiStatusIcon}"
                                    Foreground="{Binding ApiStatusColor}"
                                    Width="20" Height="20" Margin="12,0"
                                    ToolTip="{Binding ConnectionModeDisplay}" />

                                <Button Style="{StaticResource MaterialDesignFlatButton}" Padding="4">
                                    <StackPanel Orientation="Horizontal">
                                        <Border Width="32" Height="32" CornerRadius="16"
                                                Background="{StaticResource SidebarAvatarBrush}">
                                            <TextBlock HorizontalAlignment="Center" VerticalAlignment="Center"
                                                       FontSize="13" Foreground="White"
                                                       Text="{Binding CurrentUserInitial}" />
                                        </Border>
                                        <TextBlock Margin="8,0,0,0" VerticalAlignment="Center"
                                                   FontSize="13"
                                                   Text="{Binding CurrentUserDisplayName}" />
                                        <materialDesign:PackIcon Kind="ChevronDown" Margin="4,0,0,0"
                                                                 VerticalAlignment="Center" />
                                    </StackPanel>
                                </Button>
                            </StackPanel>
                        </DockPanel>

                        <!-- Breadcrumb bar -->
                        <Border DockPanel.Dock="Top" Padding="16,8"
                                BorderBrush="{DynamicResource MaterialDesign.Brush.Outline}"
                                BorderThickness="0,0,0,1">
                            <ItemsControl ItemsSource="{Binding Breadcrumbs}">
                                <ItemsControl.ItemsPanel>
                                    <ItemsPanelTemplate>
                                        <StackPanel Orientation="Horizontal" />
                                    </ItemsPanelTemplate>
                                </ItemsControl.ItemsPanel>
                                <ItemsControl.ItemTemplate>
                                    <DataTemplate>
                                        <Button Content="{Binding Title}"
                                                Command="{Binding DataContext.NavigateToBreadcrumbCommand,
                                                    RelativeSource={RelativeSource AncestorType=Window}}"
                                                CommandParameter="{Binding}"
                                                Style="{StaticResource MaterialDesignFlatButton}" />
                                    </DataTemplate>
                                </ItemsControl.ItemTemplate>
                            </ItemsControl>
                        </Border>

                        <!-- Content region (Prism) -->
                        <Border Margin="12" CornerRadius="8"
                                Background="{DynamicResource MaterialDesign.Brush.Surface}">
                            <ContentControl prism:RegionManager.RegionName="ContentRegion" />
                        </Border>

                    </DockPanel>

                </materialDesign:DrawerHost>
            </materialDesign:SnackbarHost>
        </materialDesign:DialogHost>
    </Grid>

</Window>
```

- [ ] **Step 2: Verify compilation**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj --no-restore`
Expected: PASS

- [ ] **Step 3: Manual smoke test**

Launch the Desktop app. Verify:
1. Login screen appears unchanged
2. After login, Shell shows MDIX MaterialDesignWindow style
3. Left drawer opens/closes with hamburger button
4. Navigation items display correctly
5. Dark mode toggle works
6. TopBar shows user info and connection status

- [ ] **Step 4: Commit**

```bash
git add src/Client/Desktop/Shell/Views/MainWindow.xaml
git commit -m "feat(shell): rewrite MainWindow.xaml with MDIX DrawerHost architecture"
```

---

## Task 8: Delete SidebarControl and GlobalStatusBar

**Covers:** [S1] Shell layout — cleanup

**Files:**
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml.cs`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/GlobalStatusBar.xaml`
- Delete: `src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/GlobalStatusBar.xaml.cs`

- [ ] **Step 1: Check for remaining references**

Run: `Get-ChildItem -Path "src" -Recurse -Include "*.cs","*.xaml" | Select-String -Pattern "SidebarControl|GlobalStatusBar" | Select-Object -First 20`
Expected: Only references in the files being deleted (and possibly in obj/ debug artifacts)

- [ ] **Step 2: Delete files**

```bash
git rm src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml
git rm src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/SidebarControl.xaml.cs
git rm src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/GlobalStatusBar.xaml
git rm src/Client/Desktop/Core/LYBT.Desktop.Controls/Controls/GlobalStatusBar.xaml.cs
```

- [ ] **Step 3: Verify compilation**

Run: `dotnet build src/Client/Desktop/Shell/LYBT.Desktop.Shell.csproj --no-restore`
Expected: PASS (if any references remain, fix them before committing)

- [ ] **Step 4: Commit**

```bash
git commit -m "feat(shell): remove SidebarControl and GlobalStatusBar (replaced by MDIX DrawerHost)"
```

---

## Task 9: Final Integration Test

**Covers:** All sections — end-to-end verification

**Files:** None (verification only)

- [ ] **Step 1: Full solution build**

Run: `dotnet build LYBTZYZS.sln`
Expected: PASS

- [ ] **Step 2: Run Desktop tests**

Run: `dotnet test tests/LYBT.Tests.Desktop/`
Expected: PASS

- [ ] **Step 3: Manual smoke test**

Launch Desktop app and verify:
1. Login screen renders correctly (unchanged)
2. Login succeeds and Shell appears with MDIX style
3. Left drawer opens/closes via hamburger button
4. Navigation items display correctly per role
5. Dark mode toggle switches theme
6. Breadcrumb navigation works
7. User avatar and name display in TopBar
8. Connection status icon displays correctly
9. Logout returns to login screen
10. Ctrl+M toggles drawer

- [ ] **Step 4: Final commit**

```bash
git add -A
git commit -m "feat(shell): complete MaterialDesignInXaml Shell refactor"
```
