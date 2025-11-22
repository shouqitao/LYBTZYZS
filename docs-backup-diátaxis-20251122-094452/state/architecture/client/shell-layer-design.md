# Shell层架构设计

**版本**：v1.0
**创建时间**：2025-10-20
**对应代码层**：LYBT.Desktop.Shell
**关联决策**：ADR-003 Workstation架构重构

## 📋 概述

Shell层是WPF应用程序的**外壳容器层**，负责应用程序启动、窗口管理、全局导航控制，但**不包含任何业务逻辑或业务模块主页**。

## 🎯 核心职责（明确边界）

### ✅ Shell层应该做什么

1. **应用程序生命周期管理**
   - WPF应用程序启动和关闭
   - 依赖注入容器初始化（Prism + DryIoc）
   - 模块目录配置和模块加载管理

2. **主窗口容器管理**
   - MainWindow主窗口定义和Region容器
   - 窗口状态管理（最小化、最大化、关闭）
   - 启动画面（SplashScreen）

3. **全局导航控制**
   - 基于角色的导航决策（医生→ClinicalHomeView，管理员→AdminHomeView）
   - 事件驱动导航（LoginSuccessEvent → 导航）
   - Region导航管理（ContentRegion、LoginRegion）

4. **全局基础设施**
   - 全局样式和主题（CommonStyles.xaml）
   - 全局对话框服务（ErrorDetailsDialog、ConfirmationDialog、InformationDialog）
   - 全局值转换器（ApiHealthStatusToColorConverter等）
   - 启动性能监控（StartupPerformanceMonitor）

5. **认证入口视图**
   - LoginView（登录界面，来自Auth模块但注册到Shell的LoginRegion）

### ❌ Shell层不应该做什么（重要）

1. **❌ 不包含业务模块主页**
   - 医生主页（ClinicalHomeView）→ 属于MedicalCase模块
   - 前台主页（ReceptionHomeView）→ 属于Reception模块（未来）
   - 药房主页（PharmacyHomeView）→ 属于Pharmacy模块（未来）
   - 管理员主页（AdminHomeView）→ 属于Admin模块（未来）

2. **❌ 不包含业务逻辑**
   - 患者管理、医案管理、处方管理等业务逻辑属于对应业务模块
   - Shell层只负责导航到业务模块，不执行业务操作

3. **❌ 不包含业务数据访问**
   - Shell层不直接调用业务Repository或Service
   - Shell层只通过事件与业务模块通信

4. **❌ 不承担认证职责**
   - 认证逻辑由Auth模块的LoginViewModel负责
   - Shell层只负责根据认证成功事件执行导航

## 🏗️ Shell层组件结构

### 目录结构

```
src/Client/Desktop/Shell/
├── App.xaml                          # WPF应用程序入口
├── App.xaml.cs                       # 应用程序启动逻辑、DI容器配置
├── appsettings.json                  # 应用程序配置
├── GlobalAssemblyInfo.cs             # 全局程序集信息
├── LYBT.Desktop.Shell.csproj         # 项目文件
│
├── Views/                            # 视图组件
│   ├── MainWindow.xaml               # 主窗口（ContentRegion + 工具栏 + 状态栏）
│   ├── MainWindow.xaml.cs            # 主窗口Code-behind
│   ├── SplashScreenWindow.xaml       # 启动画面
│   ├── SplashScreenWindow.xaml.cs
│   └── PlaceholderViews.cs           # 占位符视图（开发中使用）
│
├── ViewModels/                       # 视图模型
│   └── MainWindowViewModel.cs        # ⭐ 全局导航控制器
│
├── Services/                         # Shell层服务
│   ├── ApplicationInitializationService.cs  # 应用程序初始化服务
│   ├── Bootstrap/
│   │   ├── IApplicationBootstrapper.cs     # 启动引导接口
│   │   └── ApplicationBootstrapper.cs      # 启动引导实现
│   ├── INavigationService.cs         # 导航服务接口
│   ├── ThemeService.cs               # 主题服务
│   └── StartupPerformanceMonitor.cs  # 启动性能监控
│
├── Dialogs/                          # 全局对话框
│   ├── Views/
│   │   ├── ErrorDetailsDialog.xaml   # 错误详情对话框
│   │   ├── ConfirmationDialog.xaml   # 确认对话框
│   │   └── InformationDialog.xaml    # 信息对话框
│   └── ViewModels/
│       ├── ErrorDetailsDialogViewModel.cs
│       ├── ConfirmationDialogViewModel.cs
│       └── InformationDialogViewModel.cs
│
├── Converters/                       # 全局值转换器
│   ├── ApiHealthStatusToColorConverter.cs
│   └── ApiHealthStatusToTextConverter.cs
│
├── Styles/                           # 全局样式
│   └── CommonStyles.xaml             # 通用样式定义
│
├── Assets/                           # 资源文件
│   ├── Icons/
│   │   └── App/
│   │       └── app.ico               # 应用程序图标
│   └── RESOURCE_MANAGEMENT.md        # 资源管理说明
│
├── Extensions/                       # 扩展方法
│   ├── ErrorHandlingServiceExtensions.cs
│   └── ServiceCollectionExtensions.cs
│
└── Models/                           # Shell层数据模型
    └── TodayPatientItem.cs           # 今日患者项（用于状态栏）
```

## ⭐ 核心组件详解

### 1. App.xaml.cs - 应用程序入口

**职责**：
- Prism应用程序初始化
- DI容器配置（DryIoc）
- 模块目录注册
- 全局异常处理
- 启动画面管理

**关键代码**（简化）：
```csharp
public partial class App : PrismApplication
{
    protected override Window CreateShell()
    {
        return Container.Resolve<MainWindow>();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册全局服务
        containerRegistry.RegisterSingleton<IApplicationBootstrapper, ApplicationBootstrapper>();
        containerRegistry.RegisterSingleton<StartupPerformanceMonitor>();

        // 注册对话框
        containerRegistry.RegisterDialog<ErrorDetailsDialog, ErrorDetailsDialogViewModel>();
        containerRegistry.RegisterDialog<ConfirmationDialog, ConfirmationDialogViewModel>();
        containerRegistry.RegisterDialog<InformationDialog, InformationDialogViewModel>();

        // 注册MainWindow和ViewModel
        containerRegistry.Register<MainWindowViewModel>();  // Transient
    }

    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        // 核心模块（WhenAvailable）
        moduleCatalog.AddModule<AuthModule>(InitializationMode.WhenAvailable);
        moduleCatalog.AddModule<PatientsModule>(InitializationMode.WhenAvailable);

        // 业务模块（OnDemand，按需加载）
        moduleCatalog.AddModule<MedicalCaseModule>(InitializationMode.OnDemand);
        moduleCatalog.AddModule<ConsultationModule>(InitializationMode.OnDemand);
        moduleCatalog.AddModule<PrescriptionsModule>(InitializationMode.OnDemand);
        moduleCatalog.AddModule<HerbsModule>(InitializationMode.OnDemand);
        moduleCatalog.AddModule<FormulaModule>(InitializationMode.OnDemand);

        // 工作台模块（OnDemand）
        moduleCatalog.AddModule<ClinicalWorkstationModule>(InitializationMode.OnDemand);
        moduleCatalog.AddModule<AdminWorkstationModule>(InitializationMode.OnDemand);
    }

    protected override void OnInitialized()
    {
        // 应用程序启动后初始化
        var bootstrapper = Container.Resolve<IApplicationBootstrapper>();
        bootstrapper.Initialize();

        base.OnInitialized();
    }
}
```

**架构原则**：
- ✅ Shell层**不注册**业务模块的HomeView（ClinicalHomeView归MedicalCase模块注册）
- ✅ 模块加载模式：核心模块WhenAvailable，业务模块OnDemand
- ✅ 启用全局异常处理，避免程序崩溃

---

### 2. MainWindow.xaml - 主窗口容器

**职责**：
- 定义应用程序主窗口布局
- 定义Region容器（LoginRegion、ContentRegion）
- 提供顶部工具栏和底部状态栏

**XAML结构**（简化）：
```xml
<Window x:Class="LYBT.Desktop.Shell.Views.MainWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        xmlns:prism="http://prismlibrary.com/"
        Title="{Binding Title}" Height="900" Width="1440">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>  <!-- 工具栏 -->
            <RowDefinition Height="*"/>     <!-- 主内容区 -->
            <RowDefinition Height="Auto"/>  <!-- 状态栏 -->
        </Grid.RowDefinitions>

        <!-- 顶部工具栏 -->
        <Menu Grid.Row="0">
            <MenuItem Header="文件">
                <MenuItem Header="退出" Command="{Binding ExitCommand}"/>
            </MenuItem>
            <MenuItem Header="视图">
                <MenuItem Header="刷新" Command="{Binding RefreshCommand}"/>
            </MenuItem>
        </Menu>

        <!-- 主内容区（Region容器） -->
        <Grid Grid.Row="1">
            <!-- LoginRegion（登录视图，登录后隐藏） -->
            <ContentControl prism:RegionManager.RegionName="LoginRegion"
                           Visibility="{Binding IsLoggedIn, Converter={StaticResource BoolToVisibilityConverter}}"/>

            <!-- ContentRegion（业务视图，登录后显示） -->
            <ContentControl prism:RegionManager.RegionName="ContentRegion"
                           Visibility="{Binding IsLoggedIn, Converter={StaticResource InverseBoolToVisibilityConverter}}"/>
        </Grid>

        <!-- 底部状态栏 -->
        <StatusBar Grid.Row="2">
            <StatusBarItem>
                <TextBlock Text="{Binding StatusMessage}"/>
            </StatusBarItem>
            <StatusBarItem HorizontalAlignment="Right">
                <TextBlock Text="{Binding CurrentUser}"/>
            </StatusBarItem>
        </StatusBar>
    </Grid>
</Window>
```

**Region设计**：
| Region名称 | 用途 | 显示时机 | 注册视图 |
|-----------|------|---------|---------|
| **LoginRegion** | 登录视图 | 未登录时显示 | LoginView（Auth模块） |
| **ContentRegion** | 业务视图 | 登录后显示 | ClinicalHomeView、MedicalCaseFlowView等 |

**架构原则**：
- ✅ 简化Region设计：只有LoginRegion和ContentRegion，无嵌套子Region
- ✅ 所有业务视图注册到ContentRegion，导航路径统一
- ❌ 不在MainWindow中定义侧边栏导航（避免过度抽象）

---

### 3. MainWindowViewModel.cs - ⭐ 全局导航控制器

**职责**（单一职责原则）：
- 订阅LoginSuccessEvent，根据用户角色执行导航
- 加载角色对应的业务模块
- 管理应用程序全局状态（IsLoggedIn、CurrentUser等）
- 提供全局命令（ExitCommand、RefreshCommand等）

**架构设计**：
```csharp
public class MainWindowViewModel : UnifiedViewModelBase
{
    private readonly IRegionManager _regionManager;
    private readonly IEventAggregator _eventAggregator;
    private readonly IModuleManager _moduleManager;
    private readonly IModuleLoadingService _moduleLoadingService;

    public MainWindowViewModel(
        IRegionManager regionManager,
        IEventAggregator eventAggregator,
        IModuleManager moduleManager,
        IModuleLoadingService moduleLoadingService,
        ILoggerFactory loggerFactory)
        : base(eventAggregator, loggerFactory, regionManager)
    {
        _regionManager = regionManager;
        _eventAggregator = eventAggregator;
        _moduleManager = moduleManager;
        _moduleLoadingService = moduleLoadingService;

        // 订阅登录成功事件
        _eventAggregator.GetEvent<LoginSuccessEvent>().Subscribe(OnLoginSuccess);
    }

    private async void OnLoginSuccess(UserDto user)
    {
        Logger.LogInformation("收到登录成功事件，用户：{UserName}，角色：{Role}", user.UserName, user.Role);

        IsLoggedIn = true;
        CurrentUser = user;

        // 根据角色加载模块和导航
        await LoadModulesAndNavigateAsync(user.Role);
    }

    private async Task LoadModulesAndNavigateAsync(UserRole role)
    {
        string targetView = string.Empty;

        switch (role)
        {
            case UserRole.Doctor:
                // 加载医生相关模块
                await LoadClinicalWorkstationAsync();
                targetView = "ClinicalHomeView";  // 导航到MedicalCase模块的医生主页
                break;

            case UserRole.Reception:
                // 加载前台相关模块（未来实现）
                await LoadReceptionWorkstationAsync();
                targetView = "ReceptionHomeView";
                break;

            case UserRole.Pharmacy:
                // 加载药房相关模块（未来实现）
                await LoadPharmacyWorkstationAsync();
                targetView = "PharmacyHomeView";
                break;

            case UserRole.Admin:
                // 加载管理员相关模块
                await LoadAdminWorkstationAsync();
                targetView = "AdminHomeView";
                break;

            default:
                Logger.LogWarning("未知角色: {Role}", role);
                return;
        }

        // 导航到目标视图
        Logger.LogInformation("导航到: {TargetView}", targetView);
        _regionManager.RequestNavigate("ContentRegion", targetView);
    }

    private async Task LoadClinicalWorkstationAsync()
    {
        // Issue #1514 Phase 1: 医生主页已迁移到MedicalCaseModule，需要加载该模块
        await _moduleLoadingService.LoadModulesAsync(new[]
        {
            "MedicalCaseModule",        // 医案管理模块（包含ClinicalHomeView医生主页）
            "ConsultationModule",       // 诊断模块
            "PrescriptionsModule",      // 处方模块
            "HerbsModule",              // 药材模块
            "FormulaModule",            // 方剂模块
            "ClinicalWorkstationModule" // 诊疗工作台模块（保留以兼容旧架构）
        });
        Logger.LogDebug("诊疗工作台及相关模块加载完成");
    }

    // ... 其他方法
}
```

**架构原则（ADR-003）**：
- ✅ **单一职责**：MainWindowViewModel只负责导航，不负责认证
- ✅ **事件驱动**：通过LoginSuccessEvent与Auth模块解耦
- ✅ **角色驱动导航**：根据用户角色动态加载模块和导航
- ✅ **导航目标**：所有主页视图来自业务模块，Shell层不包含

---

## 🔄 Shell层与业务模块的交互模式

### 1. 事件驱动导航（推荐）

```
Auth模块（LoginViewModel）
  → 认证成功
    → 发布 LoginSuccessEvent
      → Shell层（MainWindowViewModel）订阅事件
        → 根据角色加载模块
          → 导航到业务模块主页
```

**代码示例**：
```csharp
// Auth模块：LoginViewModel.cs
private void NavigateBasedOnRole(UserRole role, UserDto user, string token)
{
    Logger.LogInformation($"用户 {user.UserName} 认证成功，角色：{role}");

    // 发布登录成功事件，让 MainWindowViewModel 处理后续的模块加载和导航
    Logger.LogInformation("📢 发布 LoginSuccessEvent，触发导航流程");
    EventAggregator.GetEvent<LoginSuccessEvent>().Publish(user);

    Logger.LogInformation("✅ 登录流程完成，等待导航");
}

// Shell层：MainWindowViewModel.cs
public MainWindowViewModel(...)
{
    // 订阅登录成功事件
    _eventAggregator.GetEvent<LoginSuccessEvent>().Subscribe(OnLoginSuccess);
}

private async void OnLoginSuccess(UserDto user)
{
    // 处理导航逻辑
    await LoadModulesAndNavigateAsync(user.Role);
}
```

**优势**：
- ✅ **解耦**：Auth模块不需要知道Shell层的导航逻辑
- ✅ **可测试**：事件发布和订阅可以独立测试
- ✅ **灵活**：可以有多个订阅者响应同一事件

---

### 2. Region导航（统一导航路径）

**导航示例**：
```csharp
// 所有业务视图统一注册到ContentRegion
_regionManager.RequestNavigate("ContentRegion", "ClinicalHomeView");
_regionManager.RequestNavigate("ContentRegion", "MedicalCaseFlowView");
_regionManager.RequestNavigate("ContentRegion", "PatientManagementView");
```

**架构原则**：
- ✅ 所有业务视图注册到ContentRegion
- ✅ 无嵌套子Region（ClinicalContentRegion已废弃）
- ✅ 导航路径统一，易于维护

---

## 📋 架构决策记录（基于ADR-003）

### 决策1：取消Workstation作为UI容器 ❌

**背景**：
- ClinicalWorkstationView和AdminWorkstationView只是侧边栏布局容器
- 引入了额外的Region（ClinicalContentRegion），增加复杂度
- 与HomeView功能重叠

**决策**：
- ❌ Workstation ≠ 侧边栏容器
- ✅ Workstation = 角色业务模块（如Clinical、Reception、Admin）
- ✅ MVP阶段用Shell/HomeView过渡 → **Phase 1完成后**，HomeView已迁移到MedicalCase模块

---

### 决策2：简化Region设计 ✅

**变更前**：
```
Shell:
  ├─ LoginRegion（登录视图）
  └─ ContentRegion（主内容）
      └─ ClinicalWorkstation:
           └─ ClinicalContentRegion（医生业务内容）
```

**变更后**：
```
Shell:
  ├─ LoginRegion（登录视图）
  └─ ContentRegion（所有角色的业务视图）
```

**收益**：
- ✅ 导航路径统一：`_regionManager.RequestNavigate("ContentRegion", viewName)`
- ✅ 无需管理多个嵌套Region
- ✅ Prism导航更简单

---

### 决策3：Shell层只负责导航，不负责认证 ✅

**架构分层**：
```
认证层（Auth模块）
  └─ LoginViewModel
       └─ 认证逻辑 + 发布LoginSuccessEvent

事件层
  └─ LoginSuccessEvent（UserDto）

导航层（Shell层）
  └─ MainWindowViewModel
       └─ 订阅事件 + 角色判断 + 模块加载 + 导航
```

**单一职责原则**：
- ✅ LoginViewModel：只负责认证（验证用户名密码、获取Token、发布事件）
- ✅ MainWindowViewModel：只负责导航（根据角色加载模块、导航到主页）

---

## 🚫 禁止模式（Anti-Patterns）

### ❌ Anti-Pattern 1：Shell层包含业务主页

**错误示例**：
```
src/Client/Desktop/Shell/
├── Views/
│   ├── MainWindow.xaml
│   ├── HomeView.xaml          ❌ 错误：业务主页不应在Shell层
│   └── LoginView.xaml         ✅ 正确：登录视图可以在Shell层
```

**正确架构**：
```
src/Client/Desktop/Shell/
├── Views/
│   └── MainWindow.xaml        ✅ 只包含容器，不包含业务视图

src/Client/Desktop/Modules/LYBT.Desktop.MedicalCase/
├── Views/
│   └── ClinicalHomeView.xaml  ✅ 医生主页属于MedicalCase模块
```

---

### ❌ Anti-Pattern 2：LoginViewModel执行导航

**错误示例**（Phase 1修复前）：
```csharp
// LoginViewModel.cs
private void NavigateBasedOnRole(UserRole role, UserDto user, string token)
{
    string targetView = role switch
    {
        UserRole.Doctor => "ClinicalWorkstationView",  ❌ 错误：认证层不应处理导航
        _ => "ClinicalWorkstationView"
    };

    EventAggregator.GetEvent<LoginSuccessEvent>().Publish(user);

    _ = Task.Delay(100).ContinueWith(_ =>
    {
        RegionManager.RequestNavigate("ContentRegion", targetView, ...);  ❌ 双重导航冲突
    });
}
```

**正确架构**：
```csharp
// LoginViewModel.cs
private void NavigateBasedOnRole(UserRole role, UserDto user, string token)
{
    Logger.LogInformation($"用户 {user.UserName} 认证成功，角色：{role}");

    // ✅ 只发布事件，不执行导航
    EventAggregator.GetEvent<LoginSuccessEvent>().Publish(user);
}

// MainWindowViewModel.cs
private async void OnLoginSuccess(UserDto user)
{
    // ✅ 导航层统一处理导航逻辑
    await LoadModulesAndNavigateAsync(user.Role);
}
```

---

### ❌ Anti-Pattern 3：嵌套过多Region

**错误示例**：
```xml
<!-- MainWindow.xaml -->
<ContentControl prism:RegionManager.RegionName="ContentRegion"/>

<!-- ClinicalWorkstationView.xaml -->
<ContentControl prism:RegionManager.RegionName="ClinicalContentRegion"/>  ❌ 不必要的嵌套

<!-- MedicalCaseFlowView.xaml -->
<ContentControl prism:RegionManager.RegionName="MedicalCaseStepRegion"/>  ❌ 过度抽象
```

**正确架构**：
```xml
<!-- MainWindow.xaml -->
<ContentControl prism:RegionManager.RegionName="ContentRegion"/>  ✅ 扁平化Region设计

<!-- 所有业务视图直接注册到ContentRegion -->
_regionManager.RequestNavigate("ContentRegion", "ClinicalHomeView");
_regionManager.RequestNavigate("ContentRegion", "MedicalCaseFlowView");
```

---

## 🔄 未来扩展（Phase 2-3）

### Phase 2：创建Reception模块（前台业务）

**新增组件**：
```
src/Client/Desktop/Modules/LYBT.Desktop.Reception/
├── Views/
│   └── ReceptionHomeView.xaml         # 前台主页
├── ViewModels/
│   └── ReceptionHomeViewModel.cs
└── ReceptionModule.cs
```

**Shell层变更**：
```csharp
// MainWindowViewModel.cs
private async Task LoadModulesAndNavigateAsync(UserRole role)
{
    switch (role)
    {
        case UserRole.Reception:  // 新增前台角色
            await LoadReceptionWorkstationAsync();
            targetView = "ReceptionHomeView";
            break;
    }
}

private async Task LoadReceptionWorkstationAsync()
{
    await _moduleLoadingService.LoadModulesAsync(new[]
    {
        "ReceptionModule",      // 前台模块
        "PatientsModule",       // 患者模块
        "BillingModule"         // 收费模块（新增）
    });
}
```

**架构原则**：
- ✅ Shell层只添加导航逻辑，不添加业务视图
- ✅ ReceptionHomeView归Reception模块注册

---

### Phase 3：移除或重构Workstation模块

**目标**：
- 评估ClinicalWorkstationModule和AdminWorkstationModule是否还有存在价值
- 如果只是空容器，考虑删除
- 如果有业务价值（如侧边栏快捷导航），重构为组件而非独立模块

---

## 📚 相关文档

- **[ADR-003 Workstation架构重构](../../architecture/decisions/adr-003-workstation-refactoring.md)** - 架构决策记录
- **[Client端架构指南](README.md)** - WPF架构总览
- **[MedicalCase模块](../reference/modules/medicalcase/README.md)** - 医生主页所在模块

---

## ✅ Shell层架构检查清单

在修改Shell层代码前，请确认：

- [ ] 新增视图是否属于Shell层职责？（如果是业务视图，应放在对应模块）
- [ ] 导航逻辑是否集中在MainWindowViewModel？（避免分散到多处）
- [ ] 是否遵循事件驱动模式？（避免模块间直接耦合）
- [ ] Region设计是否扁平化？（避免过度嵌套）
- [ ] 模块加载是否按需加载？（OnDemand模式优化启动性能）

---

**文档维护**：架构组 | **最后更新**：2025-10-20
**适用版本**：v1.0 Shell层设计标准 | **审核状态**：待审核
