# LYBT.Desktop.Shell

> WPF应用程序入口 | Prism.DryIoc模块化容器 | 启动编排与导航中心

## 项目定位

- **层级**: Client端 (Desktop桌面应用)
- **职责**: 整个WPF客户端的统一入口点和容器编排中心。负责应用启动、Prism模块加载、DI容器管理、主界面框架、Region导航和全局异常处理
- **状态**: Active

## 目录结构

```
LYBT.Desktop.Shell/
├── App.xaml / App.xaml.cs         # PrismApplication启动逻辑
├── appsettings.json               # 应用配置 (API地址/UI/功能开关/缓存)
├── GlobalAssemblyInfo.cs          # 程序集版本信息
├── NativeMethods.cs               # Win32互操作
├── Views/                         # 主窗口、闪屏、占位符视图
├── ViewModels/                    # MainWindowViewModel、AccountSettings
├── Controls/                      # AccountSettingsControl
├── Dialogs/                       # Prism对话框 (Views + ViewModels)
├── Extensions/                    # DI注册/错误处理/Prism配置扩展方法
├── Services/                      # 启动管道/Bootstrap/HealthCheck/Session/Login
├── Models/                        # TodayPatientItem等Shell层模型
├── Styles/                        # CommonStyles/Controls/Dialog/Typography
├── Assets/                        # Icons + Images
└── Resources/                     # XAML资源字典
```

## 核心组件

| 名称 | 说明 |
|------|------|
| App.xaml.cs | PrismApplication实现，模块目录配置、DI注册、异常处理 |
| MainWindow | 主窗口容器 (标题栏+菜单栏+ContentRegion+状态栏) |
| MainWindowViewModel | 导航命令、用户会话、状态栏、EventAggregator订阅 |
| StartupPipeline | 启动管道，统一管理启动步骤的唯一入口 |
| ApplicationBootstrapper | 角色驱动的模块加载服务 |
| HealthCheckCoordinator | API健康检查协调器 (定时检查、状态变更事件) |
| NavigationCoordinator | Region导航协调服务 |
| MenuManager | 菜单权限管理 |
| SplashScreenWindow | 启动闪屏窗口 |
| ConfirmationDialog | Prism IDialogAware确认对话框 |

## 设计依据

Shell采用Prism模块化架构，通过ConfigureModuleCatalog集中注册所有业务模块和工作台模块，而非目录自动发现方式，确保加载顺序可控。模块间通过EventAggregator解耦通信，避免直接依赖。启动流程通过StartupPipeline统一编排，替代分散的初始化逻辑。

## 依赖关系

### 依赖

- LYBT.Desktop.Contracts (Refit API接口定义)
- LYBT.Desktop.Presentation (ViewModelBase/DialogService/通用UI组件)
- LYBT.Desktop.Infrastructure (HttpClient配置/Token管理/缓存)
- LYBT.Desktop.Foundation (Result/异常定义/扩展方法)
- LYBT.Shared.Models (跨端DTO)
- 业务模块: Auth, Users, Patients, MedicalCase, Consultation, Prescriptions, Herbs, Formula
- 工作台: WorkstationCore, Admin, Clinical

### 被依赖

- 无 (顶层Shell，不被其他项目引用)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 精简README，详细代码知识迁移至CLAUDE.md |
| 2025-12-07 | 初始版本 |

## 开发笔记

# LYBT.Desktop.Shell 代码知识

## 架构决策

| 决策 | 原因 | 日期 |
|------|------|------|
| 使用Prism.DryIoc 8.1.97 | 成熟的WPF MVVM+模块化框架，DryIoc容器性能优秀 | 2025-10 |
| ConfigureModuleCatalog集中注册 | 目录自动发现(DirectoryModuleCatalog)不可控，集中注册确保加载顺序 | 2025-10 |
| StartupPipeline统一启动 | 替代分散的初始化逻辑，单一入口便于维护和调试 | 2025-12 |
| EventAggregator跨模块通信 | 模块间松耦合，避免直接依赖导致循环引用 | 2025-10 |
| ApplicationBootstrapper角色驱动 | 不同角色加载不同模块集合，减少非必要模块初始化 | 2025-12 |
| HealthCheckCoordinator与ViewModel解耦 | 健康检查逻辑独立，通过事件通知UI层 | 2025-12 |

## 启动流程

```
Program.Main()
  -> new App() (PrismApplication)
  -> App.OnStartup()
       1. ConfigureGlobalExceptionHandling()
          - DispatcherUnhandledException (UI线程)
          - AppDomain.UnhandledException (非UI线程)
          - TaskScheduler.UnobservedTaskException (Task)
       2. InitializeConfiguration()
          - appsettings.json (必需)
          - appsettings.{Environment}.json (可选)
          - 环境变量
       3. InitializeLogging()
       4. base.OnStartup() -> Prism初始化
          -> RegisterTypes() (DI注册)
          -> ConfigureModuleCatalog() (模块注册)
          -> CreateShell() -> MainWindow
       5. StartupPipeline执行各启动步骤
       6. MainWindow.Show()
```

## 模块注册

App.xaml.cs中ConfigureModuleCatalog注册顺序:

**业务模块 (8个)**:
1. AuthModule - 认证模块 (优先级最高，必须最先加载)
2. UsersModule - 用户管理
3. PatientsModule - 患者管理
4. MedicalCaseModule - 医案管理
5. ConsultationModule - 诊疗模块
6. PrescriptionsModule - 处方管理
7. HerbsModule - 药材管理
8. FormulaModule - 验方管理

**工作台模块 (3个)**:
9. WorkstationCoreModule - 核心工作台 (通用布局)
10. AdminWorkstationModule - 管理员工作台
11. ClinicalWorkstationModule - 诊疗工作台

**DI注册** (RegisterTypes):
```csharp
// Prism核心服务
containerRegistry.RegisterSingleton<IDialogService, DialogService>();
containerRegistry.RegisterSingleton<IEventAggregator, EventAggregator>();
containerRegistry.RegisterSingleton<IRegionManager, RegionManager>();

// 应用服务 (通过扩展方法)
containerRegistry.RegisterServices();

// 对话框
containerRegistry.RegisterDialog<ConfirmationDialog, ConfirmationDialogViewModel>();

// 导航视图
containerRegistry.RegisterForNavigation<HomeView, HomeViewModel>();
```

## 资源配置

### appsettings.json 结构

```json
{
  "ApiBaseUrl": "http://localhost:5001",
  "ConnectionTimeout": 30,
  "RetryCount": 3,
  "RetryDelay": 1000,
  "Logging": { "LogLevel": { "Default": "Information" } },
  "UI": { "Theme": "Light", "Language": "zh-CN", "FontSize": 14 },
  "Features": {
    "EnableDeveloperMode": false,
    "EnableUIShowcase": false,
    "EnableAdvancedLogging": false
  },
  "Cache": { "EnableMemoryCache": true, "DefaultExpirationMinutes": 30 },
  "Business": { "MaxPatientsPerPage": 50, "MaxMedicalCasesPerPage": 20 }
}
```

### 环境区分

- `appsettings.Development.json` - 开发环境 (DEBUG编译条件)
- `appsettings.Production.json` - 生产环境 (RELEASE编译条件)
- 通过 `#if DEBUG` 判断环境名称

### XAML样式资源

| 文件 | 内容 |
|------|------|
| Styles/CommonStyles.xaml | 按钮、文本框、标题等通用样式 |
| Styles/Controls.xaml | 控件特定样式 |
| Styles/DialogStyles.xaml | 对话框样式 |
| Styles/Typography.xaml | 字体排版样式 |

### 主窗口布局 (MainWindow.xaml)

```
Grid (4行)
├── Row 0: 标题栏 (Logo + 系统名称 + 用户信息 + 退出按钮)
├── Row 1: 菜单栏 (患者管理/诊疗诊断/处方管理/验方管理/中药材/系统管理)
├── Row 2: ContentRegion (Prism Region，模块视图切换区域)
└── Row 3: 状态栏 (状态消息 + 当前模块 + 实时时间)
```

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| 模块加载顺序敏感 | AuthModule提供的会话服务被其他模块依赖 | 确保AuthModule在ConfigureModuleCatalog中最先注册 |
| Container.Resolve直接调用 | 服务定位器反模式，破坏可测试性 | 使用构造函数注入，禁止直接Resolve |
| EventAggregator内存泄漏 | 订阅后未取消订阅 | 在Dispose中调用Unsubscribe |
| Task异常被吞掉 | TaskScheduler.UnobservedTaskException默认不崩溃 | 全局异常处理中调用SetObserved()并记录日志 |
| UI线程阻塞 | 同步调用API导致界面卡死 | 所有API调用使用async/await |
| appsettings.json不存在 | optional: false时启动直接崩溃 | 确保发布时包含配置文件，或提供默认值 |

## 代码文件结构

### App.xaml / App.xaml.cs (应用入口)

**App : PrismApplication** - WPF应用程序核心启动器

字段:
- `_bootstrapper: IApplicationBootstrapper?` - 角色驱动模块加载器
- `_startupPipeline: IStartupPipeline?` - 启动管道
- `_performanceMonitor: StartupPerformanceMonitor?` - 启动性能监控
- `_splashScreen: SplashScreenWindow?` - 启动画面
- `_instanceMutex: Mutex?` (static) - 单实例互斥锁
- `MutexName` = `"Global\\LYBTZYZS_Shell_Instance"` - 互斥锁名称

生命周期方法:
- `OnStartup()` - 单实例检查 -> UTF-8编码设置 -> Serilog初始化 -> 闪屏显示 -> base.OnStartup
- `OnExit()` - 按顺序释放: TickService -> TokenLifecycle -> UserActivityTracker -> MemoryCache -> Mutex -> Serilog
- `CreateShell()` - 返回 MainWindow
- `InitializeShell()` - 隐藏主窗口 (等启动管道完成后再显示)
- `OnInitialized()` - 创建性能监控器，启动异步初始化

DI注册 (`RegisterTypes`):
- `IApplicationBootstrapper` -> `ApplicationBootstrapper` (Singleton)
- `IApplicationInitializationService` -> `ApplicationInitializationService` (Singleton)
- `RegisterAllServices()` - 委托给 ServiceCollectionExtensions (见下)
- `MainWindowViewModel` (Transient)
- `ConfirmationDialog` / `ConfirmationDialogViewModel` (Dialog)
- `MessageDialog` / `MessageDialogViewModel` (Dialog)
- `InputDialog` / `InputDialogViewModel` (Dialog)
- `UnfinishedCaseDialog` / `UnfinishedCaseDialogViewModel` (Dialog, 来自Infrastructure)
- `AccountSettingsViewModel` (Transient)
- `AccountSettingsView` (ForNavigation)

模块注册 (`ConfigureModuleCatalog`):
- 核心: AuthenticationModule, UsersModule, ClinicalModule, AdminModule
- 业务: PatientsModule, HerbsModule, FormulaModule, MedicalCaseModule
- 扩展: CardReaderModule, SyncModule
- 全部使用 InitializationMode.WhenAvailable

其他方法:
- `LoadRoleBasedModulesAsync(string userRole)` - 登录后按角色加载模块
- `TryAcquireSingleInstance()` - Mutex 单实例检查
- `SetConsoleEncoding()` - 设置 UTF-8 控制台编码 (P/Invoke)
- `SafeDispose(Action, string)` - 安全释放资源包装器
- `RegisterStartupSteps()` - 注册5个启动步骤到管道
- `SubscribeToPipelineEvents()` - 订阅管道步骤完成事件
- `ShowMainWindowAfterInitializationAsync()` - 关闭闪屏，显示主窗口
- `HandleInitializationFailureAsync(Exception)` - 初始化失败处理 (弹框 + 日志定位)

App.xaml 资源合并顺序:
1. HandyControl SkinDefault + Theme
2. TCM.Theme.xaml (五行配色覆盖)
3. Theme.Light.xaml (设计Token)
4. UnifiedComponents.xaml (UI统一化)
5. HomePageStyles.xaml (首页共享)
6. Typography.xaml + Controls.xaml (Shell样式)
7. DialogStyles.xaml (对话框样式)
8. Converters.xaml (统一转换器)

### NativeMethods.cs

**NativeMethods** (internal static) - Windows API P/Invoke 封装

方法:
- `FindWindow(string?, string)` - user32.dll 按标题查找窗口
- `SetForegroundWindow(IntPtr)` - user32.dll 前台激活窗口
- `ShowWindow(IntPtr, int)` - user32.dll 控制显示状态
- `IsIconic(IntPtr)` - user32.dll 检查是否最小化
- `ActivateExistingWindow(string)` - 组合方法: 查找 -> 恢复 -> 前台激活

### GlobalAssemblyInfo.cs

程序集元数据: 版本 2.1.0.0, 公司"凌隐宝堂中医诊所"

---

### Extensions/ (DI注册扩展 - 组合根)

#### ServiceCollectionExtensions.cs (核心注册入口)

**ServiceCollectionExtensions** (static) - 服务注册总入口

`RegisterAllServices()` 调用链:
1. `RegisterConfiguration()` - IConfiguration (appsettings.json) + AddLybtClientConfiguration (强类型配置)
2. `RegisterLogging()` - ILoggerFactory (Serilog) + 约50个具名Logger注册
3. `RegisterCacheServices()` - IMemoryCache (SizeLimit=1000) + IDesktopCacheManager
4. `RegisterDataSources()` - 委托给 DataSourceRegistrationExtensions
5. `RegisterDataSourceLoggers()` - 注册DataSource的Logger
6. `RegisterHttpServices()` - HttpClient链 + 6个Refit API客户端 + ISyncApi
7. `RegisterFoundationServices()` - 认证、Token、会话、API等基础服务
8. `RegisterPresentationServices()` - 通知、异常处理、菜单、导航协调器
9. `RegisterInfrastructureServices()` - SessionManager、ActiveConsultation、Tick、角色注册表等
10. `RegisterCommandServices()` - IApplicationCommands + IModuleLoadingService
11. `RegisterApplicationServices()` - 初始化服务、状态服务、启动管道 + 5个StartupStep
12. `AddViewModelServices()` - ViewModel聚合服务

Logger注册分组:
- Infrastructure层: MainWindowServicesFacade, ActiveConsultationService, ApplicationTickService, UserActivityTracker
- Foundation层: ApiService, AuthorizationMessageHandler, TokenRefreshHandler, AuthenticationService, TokenStorageService, TokenManager, CredentialVault, AuthenticationStateMachine, LogoutService, UsernameStorageService, LocalTokenValidator, ModuleLoadingService, StartupOptimizationService, TokenLifecycleService
- Presentation/Shell层: NotificationService, DesktopExceptionHandler, App, ApplicationInitializationService, ApplicationBootstrapper, ApplicationStateService, MenuManager, NavigationCoordinator, SessionLifecycleManager, LoginCoordinator, HealthCheckCoordinator
- 模块层: AuthenticationModule, UsersModule, PatientsModule, MedicalCaseModule, HerbsModule, FormulaModule, ClinicalModule, AdminModule
- Repository层: UserRepository, PatientRepository, HerbRepository, FormulaRepository, MedicalCaseRepository
- Service层: SystemSettingsService, PrescriptionPrintService
- Component层: UserService, FormulaService, PatientService, MedicalCaseService, PatientValidator, LoggingHttpHandler

HTTP Handler链: HttpClientHandler -> TokenRefreshHandler -> AuthorizationMessageHandler -> LoggingHttpHandler -> HttpClient

Refit API客户端: IAuthApi, IPatientApi, IUserApi, IHerbApi, IFormulaApi, IMedicalCaseApi, ISyncApi

Foundation服务注册:
- IAuthenticationService -> AuthenticationService (Singleton)
- ITokenStorageService -> TokenStorageService (Singleton)
- ITokenManager -> TokenManager (Singleton)
- ICredentialVault -> CredentialVault (Singleton)
- IAuthenticationStateMachine -> AuthenticationStateMachine (Singleton)
- ILogoutService -> LogoutService (Singleton)
- ITokenValidator -> LocalTokenValidator (Singleton)
- IUsernameStorageService -> UsernameStorageService (Singleton)
- ISystemSettingsService -> SystemSettingsService (Singleton)
- IApiHealthCheckService -> ApiHealthCheckService (Singleton)
- IApiService -> ApiService (Singleton)
- IStartupOptimizationService -> StartupOptimizationService (Singleton)
- ITokenLifecycleService -> TokenLifecycleService (Singleton)

Presentation服务注册:
- INotificationService -> NotificationService (Singleton)
- IDesktopExceptionHandler -> DesktopExceptionHandler (Singleton)
- MenuManager (Singleton, 具体类注册)
- INavigationCoordinator -> NavigationCoordinator (Singleton)

Infrastructure服务注册:
- ISessionManager -> SessionManager (Singleton)
- IActiveConsultationService -> ActiveConsultationService (Singleton)
- IApplicationTickService -> ApplicationTickService (Singleton)
- UserActivityTracker (Singleton, 工厂方法, 使用ClientSessionOptions配置超时)
- IUserActivityTracker / IUserActivityState -> 指向同一UserActivityTracker实例
- IUserNotificationService -> UserNotificationService (Singleton)
- IMainWindowServicesFacade -> MainWindowServicesFacade (Singleton)
- IPrescriptionSettingsService -> PrescriptionSettingsService (Singleton)
- IClinicSettingsService -> ClinicSettingsService (Singleton)
- ICommonDialogService -> CommonDialogService (Singleton)
- IRoleRegistry -> RoleRegistry (Singleton, 注册4个角色: SuperAdmin, Admin, Doctor, Receptionist)

Application服务注册:
- IApplicationInitializationService -> ApplicationInitializationService (Singleton)
- IApplicationStateService -> ApplicationStateService (Singleton)
- ISessionLifecycleManager -> SessionLifecycleManager (Singleton)
- ILoginCoordinator -> LoginCoordinator (Singleton)
- IStartupPipeline -> StartupPipeline (Singleton)
- IStartupStep "ErrorHandling" -> ErrorHandlingStartupStep (Transient)
- IStartupStep "ModuleCoordinator" -> ModuleCoordinatorStartupStep (Transient)
- IStartupStep "CoreServices" -> CoreServicesStartupStep (Transient)
- IStartupStep "ApiHealthCheck" -> ApiHealthCheckStartupStep (Transient)
- IStartupStep "Warmup" -> WarmupStartupStep (Transient)
- IHealthCheckCoordinator -> HealthCheckCoordinator (Singleton)

#### PrismConfigurationExtensions.cs

**PrismConfigurationExtensions** (static) - 强类型配置注册

`AddLybtClientConfiguration(IContainerRegistry, IConfiguration)`:
- JwtOptions -> IOptions\<JwtOptions\> + JwtOptions (Singleton)
- ApiClientOptions -> IOptions\<ApiClientOptions\> + ApiClientOptions (Singleton)
- ClientSessionOptions -> IOptions\<ClientSessionOptions\> + ClientSessionOptions (Singleton)
- FeatureToggleOptions -> IOptions\<FeatureToggleOptions\> + FeatureToggleOptions (Singleton)
- ClinicSettingsOptions -> IOptions\<ClinicSettingsOptions\> + ClinicSettingsOptions (Singleton)
- PrescriptionOptions -> IOptions\<PrescriptionOptions\> + PrescriptionOptions (Singleton)

#### DataSourceRegistrationExtensions.cs

**DataSourceRegistrationExtensions** (static) - DataSource注册

`RegisterDataSources(IContainerRegistry)`:
- ICurrentUserProvider -> SessionBasedCurrentUserProvider (Singleton)
- 5个 Remote{Entity}DataSource (Transient)

#### ErrorHandlingServiceExtensions.cs [疑似死代码]

**ErrorHandlingServiceExtensions** (static) - 错误处理注册扩展

`RegisterErrorHandlingAndLogging()` 方法存在但未被任何代码调用。日志服务注册已由 ServiceCollectionExtensions.RegisterLogging() 承担。

---

### ViewModels/

#### MainWindowViewModel.cs (~712行)

**MainWindowViewModel : CoreViewModelBase** - 主窗口视图模型

构造函数注入 (10个依赖):
- IViewModelServices (聚合: RegionManager, CommonDialogService, SessionManager等)
- IUserNotificationService
- IHealthCheckCoordinator
- INavigationCoordinator
- MenuManager
- IActiveConsultationService
- IApplicationTickService
- IUserActivityTracker
- ITokenLifecycleService
- ITokenStorageService
- ILoginCoordinator

可观察属性 (CommunityToolkit.Mvvm):
- `Title: string` - 窗口标题
- `CurrentUser: UserDetailDto?` - 当前登录用户
- `IsLoggedIn: bool` - 登录状态 (NotifyPropertyChangedFor -> IsNotLoggedIn)
- `CurrentTime: DateTime` - 系统时间 (Tick驱动更新)
- `ApiStatus: ApiHealthStatus` - API健康状态
- `IsDrawerOpen: bool` - Drawer是否打开

计算属性:
- `IsNotLoggedIn` - 绑定用，取反IsLoggedIn
- `IsUserManagementVisible` - 委托给MenuManager
- `IsSyncVisible` - 委托给MenuManager
- `IsSystemSettingsVisible` - 委托给MenuManager
- `IsPasswordChangeVisible` - 委托给MenuManager

委托命令 (15个，全部委托给MenuManager):
- ShowControlExamplesCommand, QuickAddPatientCommand, QuickStartMedicalCaseCommand
- ShowHelpCommand, ShowSettingsCommand, ToggleThemeCommand
- SaveAllCommand, RefreshAllCommand, PrintCommand, ExportCommand
- UndoCommand, RedoCommand, EditProfileCommand
- NavigateToHomeCommand, NavigateToSystemSettingsCommand

RelayCommand (4个):
- `LogoutAsync()` - 检查活跃医案 -> 确认 -> 执行登出
- `RetryHealthCheckAsync()` - 手动触发API健康检查
- `ToggleDrawer()` - 切换Drawer (Ctrl+M)
- `CloseDrawer()` - 关闭Drawer (Escape)

事件订阅:
- `_tickService.Tick` -> OnTick (更新时钟)
- `_userActivityTracker.SessionExpired` -> OnSessionExpired (自动登出)
- `_healthCheckCoordinator.StatusChanged` -> OnHealthStatusChanged (更新状态栏)
- `_loginCoordinator.LoginSucceeded` -> OnLoginCoordinatorSuccess (更新UI)
- `AuthEvents.PasswordChangedEvent` -> OnPasswordChanged (跳转登录)
- `TokenLifecycleStateChangedEvent` -> OnTokenLifecycleStateChanged (Token过期处理)

公共方法:
- `OnWindowLoadedAsync()` - 窗口加载完成回调 (延迟500ms后检查登录)
- `RequestCloseApplicationAsync()` - 请求关闭应用 (确认框)

Dispose资源清理:
- 取消Tick/SessionExpired/HealthCheck/LoginSucceeded事件订阅
- 取消Region集合监控
- 释放TokenLifecycleService

#### AccountSettingsViewModel.cs (~378行)

**AccountSettingsViewModel : CoreViewModelBase, INavigationAware** - 账户设置

构造函数注入:
- IViewModelServices, IAuthenticationService, IUserRepository, IUserNotificationService?

可观察属性:
- 个人资料: UserName, RealName, PhoneNumber, Role
- 修改密码: OldPassword, NewPassword, ConfirmPassword
- 验证: ValidationError, HasValidationError
- Tab: IsProfileSelected, IsPasswordSelected

RelayCommand (3个):
- `SaveProfileAsync()` (CanExecute: CanSaveProfile) - 调用 IUserRepository.ChangeProfileAsync
- `ChangePasswordAsync()` (CanExecute: CanChangePassword) - 调用 IUserRepository.ChangePasswordAsync
- `GoBack()` - 导航日志回退

INavigationAware:
- `OnNavigatedTo()` - 加载用户资料，处理Tab参数 ("Password")
- `OnNavigatedFrom()` - 清空密码字段 (安全)

---

### Services/

#### NavigationCoordinator.cs

**NavigationCoordinator : INavigationCoordinator** - 统一导航入口

构造函数注入: IRegionManager, ISessionManager, IRoleRegistry, ILogger, IUserNotificationService?

方法:
- `NavigateTo(string viewName, IDictionary?)` - 带参数导航到指定视图
- `NavigateToHome()` / `NavigateToHome(UserRole)` - 导航到角色首页
- `NavigateBack()` - Journal回退
- `ShowLoginDialog()` - LoginRegion显示登录视图
- `ClearLoginRegion()` / `ClearContentRegion()` - 清除Region内容
- `SubscribeToRegionCollection()` / `UnsubscribeFromRegionCollection()` - Region导航监控
- `ClearHistory()` - 清除导航历史

属性: CurrentView, CanNavigateBack, NavigationHistory (最多20条)
事件: NavigationChanged

#### MenuManager.cs

**MenuManager** - 菜单命令管理器

构造函数注入: INavigationCoordinator, ISessionManager, ILogger, IUserNotificationService, IApplicationCommands

菜单可见性属性 (角色控制):
- `IsUserManagementVisible` - Admin/SuperAdmin
- `IsSyncVisible` - 始终可见
- `IsSystemSettingsVisible` - Admin/SuperAdmin
- `IsPasswordChangeVisible` - 始终可见
- `IsAccountSettingsVisible` - 始终可见

DelegateCommand (9个):
- ShowControlExamplesCommand -> NavigateTo(ControlExamples)
- QuickAddPatientCommand (Ctrl+N) -> NavigateTo(PatientManagement, Action=AddNew)
- QuickStartMedicalCaseCommand (Ctrl+Shift+C) -> NavigateTo(MedicalCaseWorkspace)
- ShowHelpCommand (F1) -> 显示快捷键说明
- ShowSettingsCommand (Ctrl+,) -> 显示占位提示
- ToggleThemeCommand -> 切换亮/暗主题
- EditProfileCommand -> NavigateTo(AccountSettings)
- NavigateToHomeCommand -> NavigateToHome()
- NavigateToSystemSettingsCommand -> NavigateTo(SystemSettings)

委托命令 (来自IApplicationCommands): SaveAllCommand, RefreshAllCommand, PrintCommand, ExportCommand, UndoCommand, RedoCommand

#### ApplicationInitializationService.cs

**IApplicationInitializationService** (接口) + **ApplicationInitializationService** (实现)

接口方法:
- `InitializeCoreServicesAsync()` - 集中初始化入口
- `InitializeErrorHandling()` - 注册全局异常处理器
- `WarmupApplicationAsync()` - 预热应用
- `InitializeModuleCoordinator()` - 初始化模块协调器 (日志记录已注册模块)

#### StartupPerformanceMonitor.cs

**StartupPerformanceMonitor** - 启动性能监控 (非DI注册，App.xaml.cs直接创建)

方法: StartMonitoring(), StartStage(name), EndStage(), Finish(), GetElapsedMilliseconds(), GetStageTime(name)

性能评估阈值: < 2s 优秀, 2-5s 一般, > 5s 较慢

#### Services/Bootstrap/

**IApplicationBootstrapper** (接口) - `LoadModulesForRoleAsync(UserRole)`

**ApplicationBootstrapper** (实现) - 使用 IRoleRegistry 获取角色对应的模块列表，通过 IModuleManager.LoadModule 加载

#### Services/HealthCheck/

**IHealthCheckCoordinator** (接口) + **HealthStatusChangedEventArgs**

属性: CurrentStatus, CheckIntervalSeconds
方法: Start(), Stop(), CheckNowAsync()
事件: StatusChanged

**HealthCheckCoordinator** (实现) - 定时API健康检查协调器

- 默认检查间隔: 10秒
- 健康检查超时: 5000ms
- 通过 IApplicationTickService.Tick 驱动定时检查
- 结果同步到 IApplicationStateService

#### Services/Session/

**SessionState** (枚举) - Unauthenticated(0), Authenticated(1), Expired(2), Refreshing(3)

**ISessionLifecycleManager** (接口) - 会话生命周期管理
- 属性: CurrentState, IsAuthenticated, CurrentUserName, CurrentUserRole, TokenRemainingTime
- 方法: StartSessionAsync, EndSessionAsync, RefreshTokenAsync, UpdateTokenExpiration, RecordUserActivity, GetDiagnostics
- 事件: StateChanged, SessionExpired

**SessionLifecycleManager : ISessionLifecycleManager, IDisposable** (实现)
- 线程安全 (stateLock)
- 订阅 TokenLifecycleStateChangedEvent + UserActivityTracker.SessionExpired
- Token Warning状态: 静默等待自动刷新或过期
- Token Expired: 转换到Expired状态并触发SessionExpired事件

**SessionStateChangedEventArgs** / **SessionDiagnostics** (record) - 事件参数和诊断信息

**SessionBasedCurrentUserProvider : ICurrentUserProvider** - 从 ISessionManager 获取当前用户ID，供 LocalDbContext 审计字段使用

#### Services/Login/

**LoginCoordinator : ILoginCoordinator** - 登录流程编排器

构造函数注入 (11个依赖): Logger, AuthenticationService, TokenStorageService, SessionLifecycleManager, ModuleLoadingService, NavigationCoordinator, AuthenticationStateMachine, Configuration, CredentialVault?, UsernameStorage?, LocalAuthService?

登录流程 (`LoginAsync`):
1. IAuthenticationService.LoginAsync -> 保存Token -> CompleteLoginFlow
2. CompleteLoginFlow: StartSession -> LoadModules -> NavigateToHome -> 触发LoginSucceeded

登出流程 (`LogoutAsync`): EndSession -> AuthService.Logout -> 清理状态 -> 触发LogoutCompleted

诊断: `GetDiagnostics()` -> LoginFlowDiagnostics (record)

#### Services/Startup/

**StartupPipeline : IStartupPipeline** - 启动管道实现
- 状态机: NotStarted -> Running -> Completed/Failed/Cancelled
- 按Order排序执行步骤，必需步骤失败终止管道，可选步骤失败继续
- 事件: StateChanged, StepCompleted
- 方法: RegisterStep, ExecuteAsync, Reset, GetDiagnostics

**StartupPipelineState** (枚举): NotStarted, Running, Completed, Failed, Cancelled

#### Services/Startup/Steps/

| 步骤类 | Name | Order | IsRequired | 职责 |
|--------|------|-------|------------|------|
| ErrorHandlingStartupStep | "错误处理初始化" | 10 | true | 注册全局异常处理器 |
| ModuleCoordinatorStartupStep | "模块协调器初始化" | 20 | false | 订阅模块加载事件 |
| CoreServicesStartupStep | "核心服务初始化" | 30 | true | 委托给ApplicationInitializationService |
| ApiHealthCheckStartupStep | "API健康检查" | 40 | false | 检查后端API可用性 (10s超时) |
| WarmupStartupStep | "应用预热" | 50 | false | 预加载常用资源 |

#### Services/Lifecycle/

**ApplicationState** (枚举) - NotStarted(0), Initializing(1), Authenticating(2), Ready(3), Running(4), ShuttingDown(5)

---

### Dialogs/

#### Dialogs/ViewModels/ConfirmationDialogViewModel.cs

**ConfirmationDialogViewModel : DialogViewModelBase** - 确认对话框

属性: Message, IconSource, ConfirmButtonText, CancelButtonText, ShowDeleteOptions, IsSoftDelete
计算属性: IsHardDelete, IsSoftDeleteSelected
命令: Confirm() (返回IsSoftDelete参数), Cancel()
参数: Title, Message, IconSource, ConfirmButtonText, CancelButtonText, ShowDeleteOptions

#### Dialogs/ViewModels/MessageDialogViewModel.cs

**MessageDialogViewModel : DialogViewModelBase** - 统一消息对话框

**MessageType** (枚举): Success, Error, Warning, Info

属性: Message, MessageType, OkButtonText
计算属性: IconSource (按类型映射图标), IconColor (五行配色: 木青/火赤/土黄/水黑)
命令: Confirm() (关闭)
参数: message, title, type (字符串, 解析为MessageType)

#### Dialogs/ViewModels/InputDialogViewModel.cs

**InputDialogViewModel : DialogViewModelBase** - 输入对话框

属性: Message, InputValue, Placeholder, OkButtonText, CancelButtonText, IsRequired
命令: Confirm() (返回input参数, CanExecute检查IsRequired), Cancel()
变更通知: InputValue变更时刷新ConfirmCommand.CanExecute

#### Dialogs/Views/

- `ConfirmationDialog.xaml/.cs` - UserControl 薄包装
- `MessageDialog.xaml/.cs` - UserControl 薄包装
- `InputDialog.xaml/.cs` - UserControl 薄包装

---

### Views/

- `MainWindow.xaml/.cs` - 主窗口，Loaded事件触发ViewModel.OnWindowLoadedAsync，拦截Alt+F4 (仅登录界面可用)
- `SplashScreenWindow.xaml/.cs` - 启动画面，UpdateStatus(string)更新文本, UpdateProgress(double)更新进度条
- `AccountSettingsView.xaml/.cs` - 账户设置视图，薄包装嵌入AccountSettingsControl

### Controls/

- `AccountSettingsControl.xaml/.cs` - 账户设置控件 (合并个人资料+修改密码)，内部通过ContainerLocator.Container.Resolve解析AccountSettingsViewModel

### Styles/

- `Typography.xaml` - 字体排版样式 (App.xaml引用)
- `Controls.xaml` - 控件样式 (App.xaml引用)
- `DialogStyles.xaml` - 对话框窗口样式 (App.xaml引用)
- `CommonStyles.xaml` - [未使用] 通用样式，不在App.xaml MergedDictionaries中引用

---

### 死代码清单

| 文件 | 类型 | 原因 |
|------|------|------|
| Extensions/ErrorHandlingServiceExtensions.cs | 整个文件未使用 | `RegisterErrorHandlingAndLogging()` 方法从未被调用，日志注册已由 ServiceCollectionExtensions.RegisterLogging() 承担 |
| Styles/CommonStyles.xaml | 样式文件未引用 | 不在 App.xaml MergedDictionaries 中，也未被任何 XAML 引用 |

### 设计注意点

- **ContainerLocator反模式**: AccountSettingsControl.xaml.cs 使用 `ContainerLocator.Container.Resolve<>()` 直接解析ViewModel，属于服务定位器反模式。这是Control模式下的妥协设计，View级别的AccountSettingsView通过Prism自动注入避免了此问题。
- **App.xaml.cs行数**: 约413行，包含P/Invoke声明，接近文件大小上限但因其职责为应用入口，暂可接受。
- **ServiceCollectionExtensions.cs行数**: 约470行，包含全部DI注册，是整个Desktop层的组合根。按职责已拆分为多个private方法，但文件较大。

## 模块演进记录

| 日期 | 变更 | 影响 |
|------|------|------|
| 2025-12 | 新增StartupPipeline统一启动管道 | 替代分散的初始化逻辑 |
| 2025-12 | 新增ApplicationBootstrapper角色驱动加载 | 按用户角色动态加载模块 |
| 2025-12 | 新增HealthCheckCoordinator | API状态检查与MainWindowViewModel解耦 |
| 2025-12 | 新增SplashScreenWindow | 启动闪屏改善用户体验 |
| 2025-12 | 新增NavigationCoordinator | 导航逻辑从ViewModel抽离 |
| 2026-01 | Prescriptions模块移除 | 功能迁移到MedicalCase模块 |
| 2026-03 | README精简，代码知识迁移至CLAUDE.md | 保持README简洁，详细知识在CLAUDE.md |

## Prism最佳实践速查

### DI注册

```csharp
// 单例 - 全局唯一 (DialogService, EventAggregator等)
containerRegistry.RegisterSingleton<IService, Service>();

// 瞬态 - 每次解析新实例 (业务Service)
containerRegistry.Register<IService, Service>();

// 对话框注册
containerRegistry.RegisterDialog<DialogView, DialogViewModel>();

// 导航视图注册
containerRegistry.RegisterForNavigation<View, ViewModel>();
```

### Region导航

```csharp
// 基本导航
_regionManager.RequestNavigate("ContentRegion", "ViewName");

// 带参数导航
var parameters = new NavigationParameters { { "PatientId", id } };
_regionManager.RequestNavigate("ContentRegion", "PatientDetail", parameters);

// 带回调导航 (推荐)
_regionManager.RequestNavigate("ContentRegion", "ViewName", result =>
{
    if (!result.Result)
        _logger.LogError($"导航失败: {result.Error?.Message}");
});
```

### EventAggregator

```csharp
// 定义事件
public class UserLoggedInEvent : PubSubEvent<UserDto> { }

// 发布
_eventAggregator.GetEvent<UserLoggedInEvent>().Publish(user);

// 订阅
_eventAggregator.GetEvent<UserLoggedInEvent>().Subscribe(OnUserLoggedIn);

// 取消订阅 (Dispose中)
_eventAggregator.GetEvent<UserLoggedInEvent>().Unsubscribe(OnUserLoggedIn);
```

### 对话框调用

```csharp
var parameters = new DialogParameters
{
    { "message", "确定要删除吗?" },
    { "title", "删除确认" }
};
_dialogService.ShowDialog("ConfirmationDialog", parameters, result =>
{
    if (result.Result == ButtonResult.OK) { /* 用户确认 */ }
});
```

## NuGet依赖

| 包 | 版本 | 用途 |
|----|------|------|
| Prism.DryIoc | 8.1.97 | MVVM + 模块化 + DI容器 |
| Microsoft.Extensions.Configuration | 9.0.x | 配置管理 |
| Microsoft.Extensions.Configuration.Json | 9.0.x | JSON配置支持 |
| Microsoft.Extensions.Logging | 9.0.x | 日志框架 |
| Microsoft.Extensions.Logging.Debug | 9.0.x | 调试日志 |
| AutoMapper | 15.0.1 | 对象映射 (DTO <-> ViewModel) |
