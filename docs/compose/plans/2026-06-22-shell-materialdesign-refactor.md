# Shell Material Design Refactor Plan

**Date**: 2026-06-22
**Scope**: Clean-slate refactor of post-login Shell using MaterialDesignInXaml (MDIX) + Prism + CommunityToolkit.Mvvm
**Constraint**: LoginView remains unchanged

---

## 1. Executive Summary

LYBTZYZS has been using MDIX 5.3.2 with `MaterialDesign2.Defaults.xaml` and `BundledTheme`. This plan is a **clean-slate refactor** — not incremental migration — of the post-login Shell. The goal is to replace the current `WindowStyle=None + custom Grid layout` with MDIX native `MaterialDesignWindow` style DrawerHost architecture, while unifying the theme system, integrating Prism Region + NavigationCoordinator, and supporting role-based workspaces.

Key decisions:
- **Lock MDIX 5.x** (currently 5.3.2), use `MaterialDesign2.Defaults.xaml` for MD2 visual style
- **Fully migrate to MDIX DialogHost** — deprecate Prism `IDialogService`
- **Global theme with Brown + Amber** — all roles share Light/Dark toggle
- **Flat ungrouped navigation** — `MaterialDesignNavigationPrimaryListBox` without `GroupStyle`
- **LoginView stays unchanged** — Visibility binding with `IsLoggedIn` toggle

---

## 2. Technology Stack

| Technology | Role | Version |
|------------|------|---------|
| MaterialDesignInXaml Toolkit | UI controls, theme, MD2 style | 5.3.2 |
| Prism.Wpf | Module loading, Region navigation, DI (DryIoc) | 9.x |
| CommunityToolkit.Mvvm | MVVM binding ([ObservableProperty]/[RelayCommand]) | 8.x |
| .NET | Runtime | 8.0 |

---

## 3. Architecture

### 3.1 Shell Layout

```
Window (Style="{StaticResource MaterialDesignWindow}")
  └─ DialogHost (Identifier="RootDialog", DialogTheme="Inherit")
       └─ SnackbarHost (Name="MainSnackbar")
            └─ DrawerHost (IsLeftDrawerOpen="{Binding IsDrawerOpen}")
                 ├─ LeftDrawerContent: DockPanel
                 │    ├─ Brand row + hamburger toggle
                 │    ├─ ListBox (MaterialDesignNavigationPrimaryListBox, flat)
                 │    ├─ Separator
                 │    └─ User card + logout button
                 └─ Content: DockPanel
                      ├─ Top: TopBar (48px)
                      │    ├─ Left: Page title
                      │    └─ Right: DarkMode toggle + connection status + user avatar menu
                      ├─ Top: Breadcrumb bar
                      └─ Fill: ContentControl [prism:RegionManager.RegionName="ContentRegion"]
```

### 3.2 Key Differences from Current Implementation

| Dimension | Current | Target |
|-----------|---------|--------|
| Window style | `WindowStyle=None` + manual layout | `MaterialDesignWindow` native |
| Navigation drawer | Custom `SidebarControl` (manual width animation) | `DrawerHost.LeftDrawerContent` + MDIX ListBox |
| Dialogs | Prism `IDialogService` standalone | `DialogHost.Identifier` + `DialogHost.Show()` |
| Notifications | Custom `ToastService` | `SnackbarHost` + `SnackbarMessageQueue` |
| Login/Main switch | `Visibility` binding on dual Grid | Unchanged (LoginView stays) |

### 3.3 NavigationCoordinator + Prism IRegionManager

The current `NavigationCoordinator` already correctly wraps `_regionManager.RequestNavigate()`. Refactor keeps this architecture, adding:

```csharp
// Incremental enhancements (non-breaking)
public event EventHandler<bool>? DrawerStateChanged;

public void NavigateTo(string viewName, IDictionary<string, object>? parameters = null)
{
    // existing logic...
    DrawerStateChanged?.Invoke(this, false); // close drawer on navigation
}
```

### 3.4 ViewModel Layering

```
ShellViewModel (MainWindowViewModel)
  ├── Theme management: ToggleThemeCommand → ThemeService
  ├── Navigation state: NavigationItems, SelectedNavItem, Breadcrumbs
  ├── User info: CurrentUser, CurrentUserDisplayName
  ├── Connection status: ApiStatus, IsRemoteMode, ConnectionModeDisplay
  └── Drawer control: IsDrawerOpen, ToggleDrawerCommand

RoleWorkspaceViewModel (Admin/Clinical/Receptionist/Sysadmin)
  └── Role-specific nav items driven by RoleDefinition.RequiredModules

FeatureViewModel (business module pages)
  └── Registered to ContentRegion via Prism Region
```

### 3.5 Role-Based Workspace Loading

| Role | Nav Items | Modules |
|------|-----------|---------|
| SuperAdmin | Home, User Management, Statistics | SysadminModule, UsersModule |
| Admin | Home, Patient/Herb/Formula/MedicalCase/User Mgmt | PatientsModule, HerbsModule, FormulaModule, MedicalCaseModule, UsersModule |
| Doctor | Home, Patient/Herb/Formula/MedicalCase/Registration | PatientsModule, HerbsModule, FormulaModule, MedicalCaseModule, RegistrationModule |
| Receptionist | Home, Patient/Registration | PatientsModule, RegistrationModule |

---

## 4. Theme System

### 4.1 Color Scheme

```xml
<materialDesign:BundledTheme
    BaseTheme="Light"
    PrimaryColor="Brown"
    SecondaryColor="Amber" />
<ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign2.Defaults.xaml" />
```

| Token | Value | Usage |
|-------|-------|-------|
| PrimaryColor | `#5D4037` | Primary (deep brown) |
| SecondaryColor | `#FFC107` | Accent (amber) |
| DarkPrimaryColor | `#4E342E` | Dark variant |
| LightPrimaryColor | `#D7CCC8` | Light variant |
| AccentColor | `#8D6E63` | Secondary accent (brown-grey) |
| SuccessColor | `#2E8B57` | Success state |
| DangerColor | `#C75050` | Error/danger state |

### 4.2 Light/Dark Toggle

```csharp
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

### 4.3 Resource Dictionary Merge Order

```xml
<ResourceDictionary.MergedDictionaries>
    <!-- 1. MDIX theme (MUST be first) -->
    <materialDesign:BundledTheme BaseTheme="Light" PrimaryColor="Brown" SecondaryColor="Amber" />
    <ResourceDictionary Source="pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesign2.Defaults.xaml" />

    <!-- 2. Project design tokens -->
    <ResourceDictionary Source="/LYBT.Desktop.Controls;component/Themes/DesignSystem.xaml" />

    <!-- 3. Global styles -->
    <ResourceDictionary Source="/LYBT.Desktop.Controls;component/Themes/Styles.xaml" />
</ResourceDictionary.MergedDictionaries>
```

### 4.4 Typography

- **Primary**: `Segoe UI Variable, Segoe UI` (Win11 native, Win10 fallback)
- **Chinese**: `Microsoft YaHei UI` (UI variant, not Microsoft YaHei)
- **MDIX Typography**: Auto-provided by `MaterialDesign2.Defaults.xaml`

---

## 5. MDIX Control Selection

### 5.1 Window Container Nesting

Based on MDIX official MainDemo (Fact [3]):

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

### 5.2 Left Navigation

Uses `ListBox` + `MaterialDesignNavigationPrimaryListBox` (not NavigationDrawer control) — matches MDIX official MainDemo pattern (Fact [4]).

### 5.3 Global Dialogs

```csharp
public class DialogHostService : IDialogHostService
{
    private const string RootDialog = "RootDialog";

    public async Task<bool> ShowConfirmationAsync(string message, string title = "确认")
    {
        var view = new ConfirmationDialog { Message = message, Title = title };
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

### 5.4 Global Notifications

```csharp
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

---

## 6. Key Integration Points

### 6.1 MainWindowViewModel

The refactored VM retains all 11 existing injected services. Only additions are `ThemeService` and the `IsDrawerOpen`/`IsDarkMode` properties. Commands remain delegated to `MenuManager`.

```csharp
public partial class MainWindowViewModel : CoreViewModelBase
{
    // === Existing services (kept as-is) ===
    private readonly IApiHealthMonitor _apiHealthMonitor;
    private readonly IApiRouter _apiRouter;
    private readonly IConnectionSettingsService _connectionSettings;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly MenuManager _menuManager;
    private readonly IActiveConsultationService _activeConsultationService;
    private readonly IApplicationTickService _tickService;
    private readonly IUserActivityTracker _userActivityTracker;
    private readonly ITokenLifecycleService _tokenLifecycleService;
    private readonly ILoginCoordinator _loginCoordinator;
    private readonly IConnectionModeService _connectionModeService;

    // === NEW: MDIX theme service ===
    private readonly ThemeService _themeService;

    // === Existing observable properties (kept) ===
    [ObservableProperty] private string _title = SystemConstants.SystemTitle;
    [ObservableProperty] private UserDetailDto? _currentUser;
    [ObservableProperty] private bool _isLoggedIn;
    [ObservableProperty] private DateTime _currentTime = DateTime.Now;
    [ObservableProperty] private ApiHealthStatus _apiStatus = ApiHealthStatus.Checking;
    [ObservableProperty] private string _connectionUrl = "http://127.0.0.1:5100";
    [ObservableProperty] private bool _isLocal;
    [ObservableProperty] private string _connectionModeDisplay = string.Empty;
    [ObservableProperty] private bool _isRemoteMode;
    [ObservableProperty] private ObservableCollection<NavigationItem> _navigationItems = new();
    [ObservableProperty] private NavigationItem? _selectedNavItem;
    [ObservableProperty] private IReadOnlyList<BreadcrumbItem> _breadcrumbs = Array.Empty<BreadcrumbItem>();
    [ObservableProperty] private bool _canNavigateBack;
    [ObservableProperty] private bool _canNavigateForward;

    // === NEW: MDIX drawer/theme properties ===
    [ObservableProperty] private bool _isDrawerOpen = true;
    [ObservableProperty] private bool _isDarkMode;

    // === Existing delegated commands (kept via MenuManager) ===
    public ICommand QuickAddPatientCommand => _menuManager.QuickAddPatientCommand;
    public ICommand QuickStartMedicalCaseCommand => _menuManager.QuickStartMedicalCaseCommand;
    public ICommand ShowHelpCommand => _menuManager.ShowHelpCommand;
    public ICommand ShowSettingsCommand => _menuManager.ShowSettingsCommand;
    public ICommand NavigateToHomeCommand => _menuManager.NavigateToHomeCommand;
    public ICommand NavigateBackCommand => _menuManager.NavigateBackCommand;
    public ICommand NavigateForwardCommand => _menuManager.NavigateForwardCommand;
    public ICommand NavigateToBreadcrumbCommand => _menuManager.NavigateToBreadcrumbCommand;
    public ICommand EditProfileCommand => _menuManager.EditProfileCommand;
    public ICommand ToggleThemeCommand => _menuManager.ToggleThemeCommand;

    // === NEW: Drawer toggle (replaces ToggleSidebarCommand) ===
    [RelayCommand]
    private void ToggleDrawer() => IsDrawerOpen = !IsDrawerOpen;

    // === Theme toggle ===
    partial void OnIsDarkModeChanged(bool value) => _themeService.ToggleTheme();

    // === Navigation item selection ===
    partial void OnSelectedNavItemChanged(NavigationItem? value)
    {
        if (value?.ViewName is string viewName && !string.IsNullOrEmpty(viewName))
        {
            _navigationCoordinator.NavigateTo(viewName);
            IsDrawerOpen = false;
        }
    }

    // === Existing login handler (enhanced with drawer close) ===
    private void OnLoginCoordinatorSuccess(object? sender, LoginSuccessEventArgs args)
    {
        var user = args.User;
        Services.UiThreadDispatcher.InvokeAsync(() =>
        {
            IsLoggedIn = true;
            IsDrawerOpen = false; // NEW: close drawer after login
            CurrentUser = user;
            // ... rest of existing logic unchanged ...
        });
    }

    // === Existing logout (enhanced with drawer clear) ===
    [RelayCommand]
    private async Task LogoutAsync()
    {
        // ... existing active consultation check + confirmation ...
        await PerformLogoutAsync();
    }

    private async Task PerformLogoutAsync()
    {
        // ... existing logic ...
        IsDrawerOpen = false; // NEW: ensure drawer closed
        NavigationItems.Clear();
        // ... rest unchanged ...
    }
}
```

### 6.1a MenuManager Role

`MenuManager` stays as-is. It owns all delegated commands (QuickAddPatient, ShowHelp, NavigateBack, etc.). The refactored Shell only:
- Renames `ToggleSidebarCommand` → `ToggleDrawerCommand` (or keeps both aliases)
- Adds `ThemeService` integration for `ToggleThemeCommand`
- No other MenuManager changes needed

### 6.1b GlobalStatusBar → TopBar Migration

Current `GlobalStatusBar.xaml` provides: time display, user info, API status, connection URL. The refactored TopBar absorbs ALL of this into the MDIX toolbar area. `GlobalStatusBar` gets deleted after migration.

### 6.1c SidebarControl Fate

`SidebarControl` gets **deleted** after refactor. Its functionality is fully replaced by:
- `DrawerHost.LeftDrawerContent` (navigation + user card)
- TopBar (API status, connection, theme toggle)
- `ThemeService` + `SnackbarHost` (notifications)

### 6.2 Service Registration (App.xaml.cs)

```csharp
protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    // === Existing registrations (kept as-is) ===
    // MenuManager, IApiHealthMonitor, IConnectionSettingsService,
    // INavigationCoordinator, ILoginCoordinator, IConnectionModeService,
    // ITokenLifecycleService, IUserActivityTracker, etc.
    // ...

    // === NEW: MDIX services ===
    containerRegistry.RegisterSingleton<PaletteHelper>();
    containerRegistry.RegisterSingleton<ThemeService>();
    containerRegistry.RegisterSingleton<ISnackbarService, SnackbarService>();
    containerRegistry.RegisterSingleton<IDialogHostService, DialogHostService>();
}
```

### 6.3 LoginView → Shell Transition

**Current implementation is correct**: Login success triggers `LoginCoordinator.LoginSucceeded` event → `MainWindowViewModel.OnLoginCoordinatorSuccess()` sets `IsLoggedIn = true` → Visibility binding switches UI. **No change needed**.

---

## 7. Gotchas & Best Practices

### 7.1 MDIX + Prism Region Coexistence

| Pitfall | Solution | Source |
|---------|----------|--------|
| DialogHost must be declared in XAML | Cannot be dynamically created and attached; must be part of control tree | MDIX Wiki Dialogs |
| SnackbarMessageQueue position | Bind to `DialogHost.SnackbarMessageQueue` to avoid occlusion during dialogs | MDIX MainDemo |
| Resource dictionary double-loading | `BundledTheme` + `MaterialDesign2.Defaults.xaml` only in `App.xaml`; don't re-merge in modules | MDIX Wiki Getting Started |
| DialogHost.Identifier required | Multi-window/multi-region scenes must set `Identifier` for correct dialog routing | MDIX Wiki Dialogs |

### 7.2 MDIX 5.x vs 4.x

**Decision: Lock MDIX 5.x (current 5.3.2)**

| Change | 5.x Behavior | Impact |
|--------|-------------|--------|
| `ITheme` → `Theme` | Interface removed, use concrete class | Code needs `Theme` not `ITheme` |
| `IBaseTheme` removed | Use `ThemeExtensions.SetBaseTheme()` | Theme toggle API changed |
| `MaterialDesignWindow` style | `Style="{StaticResource MaterialDesignWindow}"` | Simpler than 4.x manual properties |
| MD2/MD3 choice | Load `MaterialDesign2.Defaults.xaml` or `MaterialDesign3.Defaults.xaml` | XAML resource choice, not version lock |
| `Accent` → `Secondary` | Property renamed | Handled in 5.x |

### 7.3 Performance

| Optimization | Approach | Source |
|-------------|----------|--------|
| Startup perf | Reduce `MergedDictionaries` count, merge small dictionaries | MDIX Wiki Performance |
| BitmapCache | Set `CacheMode="BitmapCache"` on high-frequency render controls | MDIX Wiki Performance |
| Disable transitions | `TransitionAssist.DisableTransitions="True"` at Window level (verify coverage for Snackbar/Transitioner/DrawerHost in 5.3.2) | MDIX Wiki Performance |

### 7.4 CommunityToolkit.Mvvm Compatibility

**Already correct**: Project uses `[ObservableProperty]` / `[RelayCommand]`. No migration to Prism `BindableBase` / `DelegateCommand` needed. MDIX controls are fully compatible with CommunityToolkit.Mvvm.

### 7.5 Windows 11 vs 10 Rendering

| Issue | Approach |
|-------|----------|
| Font rendering | `Segoe UI Variable` native on Win11, falls back to `Segoe UI` on Win10 |
| Rounded corners | Win11 native support; `MaterialDesignWindow` style handles Win10 |
| Font fallback | `FontFamily="Segoe UI Variable, Segoe UI, Microsoft YaHei UI"` configured correctly |

---

## 8. Sources

| Source | Version/Date | Type |
|--------|-------------|------|
| MDIX Wiki (Getting Started) | v5.0.0+, 2024-05 edit | Official |
| MDIX GitHub Issues #2255 | v5.0.0 Breaking Changes | Official |
| MDIX MainDemo MainWindow.xaml | master branch, 2026-06-22 verified | Official |
| MDIX Wiki (Dialogs) | 2019 edit, API stable to 5.x | Official |
| MDIX Wiki (Performance) | 2019 edit | Official |
| Prism Library Wiki | DryIoc + Region navigation | Official |

---

## 9. Decisions (Resolved)

| # | Question | Decision |
|---|----------|----------|
| 1 | Sysadmin dark theme scope | Global BundledTheme switch (affects all roles) |
| 2 | NavigationPrimaryListBox grouping | Flat ungrouped (`MaterialDesignNavigationPrimaryListBox` without `GroupStyle`) |
| 3 | InputBindings scope | Define all 8 shortcut commands in ViewModel |
| 4 | MenuManager role | Stays as-is, commands remain delegated |
| 5 | SidebarControl fate | Deleted, replaced by DrawerHost.LeftDrawerContent |
| 6 | GlobalStatusBar fate | Deleted, absorbed into TopBar |
| 7 | Dialog framework | Fully migrate to MDIX DialogHost, deprecate Prism IDialogService |

---

## 10. Research Statistics

- Sources read: 22
- Facts found: 106
- Facts checked (cross-validated): 19
- Facts upheld: 7
- Facts dropped: 12
- Agent runs: 87
