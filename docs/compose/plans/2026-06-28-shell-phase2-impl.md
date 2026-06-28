# Phase② Shell 整层实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 修复 Shell 层 bug（C1/C2）、补齐缺口（G3-G10）、统一对话框体系

**Architecture:** 以 RoleRegistry 为单一真相源，删除 LoginCoordinator 旁路；全迁 MDIX DialogHost；Toast 通知统一错误 UX

**Tech Stack:** WPF/Prism/DryIoc, MaterialDesignThemes.XAML, CommunityToolkit.Mvvm, Serilog

## Global Constraints

- 代码风格：中文业务注释，英文标识符
- MVVM：CommunityToolkit.Mvvm `[ObservableProperty]`/`[RelayCommand]`
- UI：MDIX 内置样式 + Spacing Token，不自定义 ControlTemplate
- 包版本：统一在 `Directory.Packages.props`
- 模块间禁止直接引用
- `dotnet build LYBTZYZS.sln` 必须通过

---

### Task 1: C1 修复 — 删除 LoginCoordinator 旁路

**Covers:** [S2]

**Files:**
- Modify: `src/Client/Desktop/Shell/Services/Login/LoginCoordinator.cs`
- Modify: `src/Client/Desktop/Shell/Services/Bootstrap/ApplicationBootstrapper.cs`

**Interfaces:**
- Consumes: `IRoleRegistry.GetModulesForRole(role)`, `IModuleManager`
- Produces: `ApplicationBootstrapper.LoadModulesForRoleAsync(role)` 被 LoginCoordinator 调用

- [ ] **Step 1: 读取 LoginCoordinator.cs 硬编码旁路**

```bash
rg "LoadModulesForUserAsync|PatientsModule" src/Client/Desktop/Shell/Services/Login/LoginCoordinator.cs
```

- [ ] **Step 2: 修改 LoginCoordinator，删除硬编码旁路**

在 `LoginCoordinator.cs` 中找到 `LoadModulesForUserAsync` 方法，将其改为调用 `ApplicationBootstrapper.LoadModulesForRoleAsync`：

```csharp
// 删除硬编码的 PatientsModule 添加逻辑
// 改为统一走 RoleRegistry
private async Task LoadModulesForUserAsync(UserRole role)
{
    var bootstrapper = _moduleLoadingService as IApplicationBootstrapper;
    if (bootstrapper != null)
    {
        await bootstrapper.LoadModulesForRoleAsync(role);
    }
}
```

- [ ] **Step 3: 验证 ApplicationBootstrapper 使用 RoleRegistry**

```bash
rg "RoleRegistry|GetModulesForRole" src/Client/Desktop/Shell/Services/Bootstrap/ApplicationBootstrapper.cs
```

- [ ] **Step 4: 编译验证**

```bash
dotnet build LYBTZYZS.sln --no-restore
```

- [ ] **Step 5: 提交**

```bash
git add src/Client/Desktop/Shell/Services/Login/LoginCoordinator.cs
git commit -m "fix(Shell): remove LoginCoordinator hardcoded module bypass, unify via RoleRegistry"
```

---

### Task 2: Splash 进度修复

**Covers:** [S10]

**Files:**
- Modify: `src/Client/Desktop/Shell/Services/Bootstrap/AppStartupOrchestrator.cs`

**Interfaces:**
- Consumes: `IProgress<string>`
- Produces: 进度回调供 Splash Screen 绑定

- [ ] **Step 1: 读取 AppStartupOrchestrator.cs**

```bash
rg "RunStartupAsync|IProgress" src/Client/Desktop/Shell/Services/Bootstrap/AppStartupOrchestrator.cs
```

- [ ] **Step 2: 修改 RunStartupAsync 接入 IProgress**

```csharp
public async Task RunStartupAsync(IProgress<string>? progress = null)
{
    var steps = BuildExecutionUnits();
    foreach (var step in steps)
    {
        progress?.Report(step.Name);
        await step.ExecuteAsync();
    }
}
```

- [ ] **Step 3: 修改 App.xaml.cs 传入进度回调**

```csharp
// App.xaml.cs OnInitialized
var progress = new Progress<string>(stepName =>
{
    // 更新 Splash Screen 进度
    SplashProgressText = stepName;
});
_ = Container.Resolve<AppStartupOrchestrator>().RunStartupAsync(progress);
```

- [ ] **Step 4: 编译验证**

```bash
dotnet build LYBTZYZS.sln --no-restore
```

- [ ] **Step 5: 提交**

```bash
git add src/Client/Desktop/Shell/Services/Bootstrap/AppStartupOrchestrator.cs
git commit -m "fix(Shell): wire IProgress into startup pipeline for Splash progress display"
```

---

### Task 3: C2 修复 — 快捷键统一

**Covers:** [S3]

**Files:**
- Modify: `src/Client/Desktop/Shell/Views/MainWindow.xaml` (InputBindings)
- Modify: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs` (Commands)

**Interfaces:**
- Consumes: `NavigationCoordinator.NavigateTo`, `INavigationCoordinator`
- Produces: `SaveCommand`, `RefreshCommand`, `PrintCommand`

- [ ] **Step 1: 读取当前 InputBindings**

```bash
rg "InputBinding|KeyBinding" src/Client/Desktop/Shell/Views/MainWindow.xaml
```

- [ ] **Step 2: 更新 MainWindow.xaml 快捷键**

```xml
<Window.InputBindings>
    <KeyBinding Key="N" Modifiers="Control" Command="{Binding NewCommand}" />
    <KeyBinding Key="S" Modifiers="Control" Command="{Binding SaveCommand}" />
    <KeyBinding Key="F5" Command="{Binding RefreshCommand}" />
    <KeyBinding Key="P" Modifiers="Control" Command="{Binding PrintCommand}" />
    <KeyBinding Key="F1" Command="{Binding HelpCommand}" />
    <KeyBinding Key="OemComma" Modifiers="Control" Command="{Binding SettingsCommand}" />
    <KeyBinding Key="M" Modifiers="Control" Command="{Binding ToggleSidebarCommand}" />
    <KeyBinding Key="H" Modifiers="Control+Shift" Command="{Binding HomeCommand}" />
    <KeyBinding Key="F6" Command="{Binding FocusSwitchCommand}" />
    <KeyBinding Key="Left" Modifiers="Alt" Command="{Binding NavigateBackCommand}" />
    <KeyBinding Key="Right" Modifiers="Alt" Command="{Binding NavigateForwardCommand}" />
    <KeyBinding Key="Home" Modifiers="Alt" Command="{Binding NavigateHomeCommand}" />
</Window.InputBindings>
```

- [ ] **Step 3: 在 MainWindowViewModel 中实现新命令**

```csharp
[RelayCommand]
private void Save()
{
    // 发送保存事件到当前活动视图
    _eventAggregator.GetEvent<SaveRequestedEvent>().Publish();
}

[RelayCommand]
private void Refresh()
{
    _eventAggregator.GetEvent<RefreshRequestedEvent>().Publish();
}

[RelayCommand]
private void Print()
{
    _eventAggregator.GetEvent<PrintRequestedEvent>().Publish();
}
```

- [ ] **Step 4: 编译验证**

```bash
dotnet build LYBTZYZS.sln --no-restore
```

- [ ] **Step 5: 提交**

```bash
git add src/Client/Desktop/Shell/Views/MainWindow.xaml src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs
git commit -m "fix(Shell): unify keyboard shortcuts per C2 decision, add Ctrl+S/F5/Ctrl+P"
```

---

### Task 4: 状态栏重构（G3）

**Covers:** [S5]

**Files:**
- Create/Modify: `src/Client/Desktop/Shell/ViewModels/StatusBarViewModel.cs`
- Modify: `src/Client/Desktop/Shell/Views/MainWindow.xaml` (StatusBar 区域)

**Interfaces:**
- Consumes: `IConnectionModeProvider`, `ISessionManager`, `IThemeService`
- Produces: 绑定属性 `ConnectionMode`, `CurrentUser`, `CurrentTime`, `HealthStatus`

- [ ] **Step 1: 创建/重构 StatusBarViewModel**

```csharp
public partial class StatusBarViewModel : ObservableObject
{
    private readonly IConnectionModeProvider _connectionMode;
    private readonly ISessionManager _sessionManager;

    [ObservableProperty]
    private string _connectionModeText = "远程";

    [ObservableProperty]
    private string _connectionModeColor = "Green";

    [ObservableProperty]
    private string _currentUser = "";

    [ObservableProperty]
    private string _currentRole = "";

    [ObservableProperty]
    private string _currentTime = "";

    [ObservableProperty]
    private string _healthStatus = "正常";

    [ObservableProperty]
    private string _healthColor = "Green";
}
```

- [ ] **Step 2: 注册 StatusBarViewModel**

```csharp
// App.xaml.cs RegisterTypes
containerRegistry.Register<StatusBarViewModel>();
```

- [ ] **Step 3: 更新 MainWindow.xaml 状态栏区域**

状态栏位于底部（Grid.Row="1", Height="32"），使用 Border + DockPanel 实现：

```xml
<!-- Status bar (底部 Row=1) -->
<Border Grid.Column="1" Grid.Row="1"
        Background="Transparent"
        BorderBrush="{DynamicResource MaterialDesign.Brush.Outline}"
        BorderThickness="0,1,0,0">
    <DockPanel Margin="12,0" LastChildFill="False">
        <!-- Right: 连接模式 + 用户 + 时间 -->
        <StackPanel DockPanel.Dock="Right" Orientation="Horizontal"
                    VerticalAlignment="Center">
            <Ellipse Width="8" Height="8" Fill="{Binding ConnectionModeColor}" />
            <TextBlock Margin="4,0,12,0" FontSize="12"
                       Text="{Binding ConnectionModeText}" />
            <materialDesign:PackIcon Kind="AccountCircle" Width="14" Height="14" />
            <TextBlock Margin="4,0,4,0" FontSize="12"
                       Text="{Binding CurrentUserDisplay}" />
            <TextBlock FontSize="12"
                       Text="{Binding CurrentTimeDisplay}" />
        </StackPanel>
        <!-- Left: 健康状态 -->
        <StackPanel DockPanel.Dock="Left" Orientation="Horizontal"
                    VerticalAlignment="Center">
            <Ellipse Width="8" Height="8" Fill="{Binding HealthColor}" />
            <TextBlock Margin="4,0,0,0" FontSize="12"
                       Text="{Binding HealthStatus}" />
        </StackPanel>
    </DockPanel>
</Border>
```

- [ ] **Step 4: 编译验证**

```bash
dotnet build LYBTZYZS.sln --no-restore
```

- [ ] **Step 5: 提交**

```bash
git add src/Client/Desktop/Shell/ViewModels/StatusBarViewModel.cs src/Client/Desktop/Shell/Views/MainWindow.xaml
git commit -m "feat(Shell): implement status bar with all required items per G3"
```

---

### Task 5: 断网 UX（G5）

**Covers:** [S5]

**Files:**
- Modify: `src/Client/Desktop/Shell/ViewModels/StatusBarViewModel.cs`
- Modify: `src/Client/Desktop/Shell/Views/MainWindow.xaml` (添加横幅)

**Interfaces:**
- Consumes: `IConnectionModeProvider` 状态变更事件
- Produces: 断网横幅显示/隐藏

- [ ] **Step 1: 在 StatusBarViewModel 中添加断网状态**

```csharp
[ObservableProperty]
private bool _isDisconnected;

[ObservableProperty]
private string _disconnectMessage = "";
```

- [ ] **Step 2: 订阅连接状态变更**

```csharp
_connectionMode.ConnectionChanged += OnConnectionChanged;

private void OnConnectionChanged(object? sender, ConnectionChangedEventArgs e)
{
    IsDisconnected = !e.IsConnected;
    ConnectionModeText = e.IsConnected ? "远程" : "本地";
    ConnectionModeColor = e.IsConnected ? "Green" : "Orange";
    DisconnectMessage = e.IsConnected ? "" : "网络连接已断开，当前为本地模式";
}
```

- [ ] **Step 3: 在 MainWindow.xaml 添加断网横幅**

```xml
<!-- ContentRegion 上方 -->
<Border Background="Orange" Visibility="{Binding IsDisconnected, Converter={StaticResource BoolToVis}}"
        Padding="8,4">
    <TextBlock Text="{Binding DisconnectMessage}" HorizontalAlignment="Center" />
</Border>
```

- [ ] **Step 4: 编译验证**

```bash
dotnet build LYBTZYZS.sln --no-restore
```

- [ ] **Step 5: 提交**

```bash
git add src/Client/Desktop/Shell/ViewModels/StatusBarViewModel.cs src/Client/Desktop/Shell/Views/MainWindow.xaml
git commit -m "feat(Shell): add disconnection UX with status bar color change and banner per G5"
```

---

### Task 6: 主题系统（G7）

**Covers:** [S6]

**Files:**
- Verify: `src/Client/Desktop/Shell/Services/ThemeService.cs`
- Verify: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs` (IsDarkMode)

**Interfaces:**
- Consumes: `IThemeService.ApplyTheme(isDark)`
- Produces: 双主题切换

- [ ] **Step 1: 验证 ThemeService 已实现**

```bash
rg "ApplyTheme|IsDarkMode" src/Client/Desktop/Shell/Services/ThemeService.cs
```

- [ ] **Step 2: 验证 MainWindowViewModel 绑定**

```bash
rg "IsDarkMode|OnIsDarkModeChanged" src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs
```

- [ ] **Step 3: 确认 Dark 主题资源存在**

```bash
rg "DarkTheme|Dark" src/Client/Desktop/Shell/Themes/
```

- [ ] **Step 4: 编译验证（无需代码变更）**

```bash
dotnet build LYBTZYZS.sln --no-restore
```

- [ ] **Step 5: 提交（仅文档/验证）**

```bash
git commit --allow-empty -m "docs(Shell): verify dual theme system per G7 - already implemented"
```

---

### Task 7: 导航项配置（G10）

**Covers:** [S4]

**Files:**
- Modify: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs` (BuildNavigationItems)

**Interfaces:**
- Consumes: `IRoleRegistry`, `ISessionManager`
- Produces: 按角色过滤的导航项列表

- [ ] **Step 1: 读取当前 BuildNavigationItems**

```bash
rg "BuildNavigationItems|NavigationItem" src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs
```

- [ ] **Step 2: 修改为按角色可见性矩阵过滤**

```csharp
private List<NavigationItem> BuildNavigationItems(UserRole role)
{
    var allItems = new List<NavigationItem>
    {
        new("首页", "AdminHome", PackIconKind.Home),
        new("患者管理", "Patients", PackIconKind.AccountGroup),
        new("药材管理", "Herbs", PackIconKind.Pill),
        new("验方管理", "Formula", PackIconKind.FileDocument),
        new("医案管理", "MedicalCase", PackIconKind.MedicalBag),
        new("挂号管理", "Registration", PackIconKind.CalendarCheck),
        new("用户管理", "Users", PackIconKind.AccountCog),
        new("系统设置", "Settings", PackIconKind.Cog),
    };

    return allItems.Where(item => IsVisibleForRole(item, role)).ToList();
}

private bool IsVisibleForRole(NavigationItem item, UserRole role) => item.ViewName switch
{
    "Settings" => role == UserRole.SuperAdmin,
    "Users" => role >= UserRole.Admin,
    "Herbs" or "Formula" or "MedicalCase" => role >= UserRole.Doctor,
    _ => true // 首页、患者、挂号对所有角色可见
};
```

- [ ] **Step 3: 编译验证**

```bash
dotnet build LYBTZYZS.sln --no-restore
```

- [ ] **Step 4: 提交**

```bash
git add src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs
git commit -m "feat(Shell): implement role-based navigation visibility matrix per G10"
```

---

### Task 8: 对话框全迁 DialogHost（G9）

**Covers:** [S7]

**Files:**
- Modify: `src/Client/Desktop/Shell/Services/DialogHostService.cs`
- Modify: 各 ViewModel 中使用 `IDialogService` 的地方

**Interfaces:**
- Consumes: `MaterialDesignThemes.Wpf.DialogHost`
- Produces: `IDialogHostService.ShowDialogAsync<T>()`

- [ ] **Step 1: 识别所有 IDialogService 使用**

```bash
rg "IDialogService|ShowDialogAsync" src/Client/Desktop/ --include "*.cs"
```

- [ ] **Step 2: 更新 DialogHostService 封装**

```csharp
public class DialogHostService : IDialogHostService
{
    public async Task<T?> ShowDialogAsync<T>(string dialogName, object? parameter = null) where T : class
    {
        var result = await DialogHost.Show dialogName, parameter);
        return result as T;
    }
}
```

- [ ] **Step 3: 逐个替换 IDialogService 调用**

将各 ViewModel 中的 `IDialogService.ShowDialogAsync` 改为 `IDialogHostService.ShowDialogAsync`。

- [ ] **Step 4: 编译验证**

```bash
dotnet build LYBTZYZS.sln --no-restore
```

- [ ] **Step 5: 提交**

```bash
git add src/Client/Desktop/Shell/Services/DialogHostService.cs
git commit -m "refactor(Shell): migrate all dialogs to MDIX DialogHost per G9"
```

---

### Task 9: 错误处理 Toast（G8）

**Covers:** [S8]

**Files:**
- Modify: `src/Client/Desktop/Shell/Services/SnackbarService.cs`
- Modify: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`

**Interfaces:**
- Consumes: `MaterialDesignThemes.Wpf.Snackbar`
- Produces: `ISnackbarService.ShowToast(message, level)`

- [ ] **Step 1: 验证 SnackbarService 已实现**

```bash
rg "ShowToast|SnackbarMessageQueue" src/Client/Desktop/Shell/Services/SnackbarService.cs
```

- [ ] **Step 2: 确保全局异常处理器使用 Toast**

```bash
rg "UnhandledException|DispatcherUnhandledException" src/Client/Desktop/Shell/App.xaml.cs
```

- [ ] **Step 3: 编译验证（如已实现则无需变更）**

```bash
dotnet build LYBTZYZS.sln --no-restore
```

- [ ] **Step 4: 提交（如已实现则仅文档验证）**

```bash
git commit --allow-empty -m "docs(Shell): verify Toast error UX per G8 - already implemented"
```

---

### Task 10: 登录区行为（G6）

**Covers:** [S9]

**Files:**
- Modify: `src/Client/Desktop/Shell/Services/Login/LoginCoordinator.cs`
- Modify: `src/Client/Desktop/Shell/ViewModels/MainWindowViewModel.cs`

**Interfaces:**
- Consumes: `LoginResponse.MustChangePassword`, `IConfiguration`
- Produces: 强制改密对话框、Sysadmin 本地自动登录

- [ ] **Step 1: 在 LoginCoordinator 中添加首次改密检查**

```csharp
if (loginResult.MustChangePassword)
{
    // 弹出改密对话框
    var changePasswordResult = await _dialogHostService.ShowDialogAsync<ChangePasswordDialog>();
    if (changePasswordResult == null)
    {
        // 用户取消，登出
        await LogoutAsync();
        return;
    }
}
```

- [ ] **Step 2: 实现 Sysadmin 本地自动登录**

```csharp
// 在 LoginCoordinator 中
if (IsLocalMode && IsSysadminAutoLoginEnabled())
{
    var autoLoginRequest = new LoginRequest
    {
        UserName = _configuration["AutoLogin:UserName"] ?? "sysadmin",
        Password = _configuration["AutoLogin:Password"] ?? ""
    };
    return await LoginAsync(autoLoginRequest);
}
```

- [ ] **Step 3: 编译验证**

```bash
dotnet build LYBTZYZS.sln --no-restore
```

- [ ] **Step 4: 提交**

```bash
git add src/Client/Desktop/Shell/Services/Login/LoginCoordinator.cs
git commit -m "feat(Shell): add forced password change and sysadmin auto-login per G6"
```

---

## Task 依赖关系

```
Task 1 (C1) ──┐
Task 2 (Splash) ─┤
Task 3 (C2) ────┤
Task 4 (状态栏) ─┼── 独立，可并行
Task 5 (断网) ──┤ 依赖 Task 4
Task 6 (主题) ──┤ 无代码变更（验证）
Task 7 (导航项) ─┤ 依赖 Task 1
Task 8 (对话框) ─┤ 独立
Task 9 (Toast) ──┤ 无代码变更（验证）
Task 10 (登录) ──┘ 依赖 Task 1
```

**推荐执行顺序**：
1. Task 1 → Task 7 → Task 10（C1 修复链）
2. Task 4 → Task 5（状态栏链）
3. Task 3（快捷键）
4. Task 8（对话框迁移）
5. Task 2/6/9（验证类，可并行）
