# Shell

> Desktop WPF 宿主层，承载所有业务模块、认证流程、导航框架与本地 WebAPI 内嵌服务。

## 项目定位

| 属性 | 值 |
|------|-----|
| 层级 | Client/Desktop/Shell |
| 职责 | 应用启动编排、DI 容器组装、模块加载、认证/会话管理、导航路由、状态栏、主题切换、内嵌 LocalWebAPI |
| 状态 | Active |

## 目录结构

```
Shell/
├── App.xaml.cs                          # PrismApplication 入口
├── NativeMethods.cs                     # P/Invoke 单实例窗口管理
├── Assets/                              # 字体、图标、背景图
├── Controls/
│   └── AccountSettingsControl.xaml(.cs) # 账户设置控件
├── Dialogs/
│   ├── ViewModels/
│   │   ├── ConfirmationDialogViewModel.cs
│   │   ├── InputDialogViewModel.cs
│   │   └── MessageDialogViewModel.cs
│   └── Views/
│       ├── ConfirmationDialog.xaml(.cs)
│       ├── InputDialog.xaml(.cs)
│       └── MessageDialog.xaml(.cs)
├── Extensions/
│   ├── DataSourceRegistrationExtensions.cs
│   ├── HttpServiceRegistrationExtensions.cs
│   ├── LoggingRegistrationExtensions.cs
│   ├── PrismConfigurationExtensions.cs
│   ├── ServiceCollectionExtensions.cs
│   └── UnifiedApiClientExtensions.cs
├── Resources/Strings/                   # 本地化资源
├── Services/
│   ├── AppStartupOrchestrator.cs
│   ├── DialogHostService.cs
│   ├── EmbeddedLocalWebApiService.cs
│   ├── MenuManager.cs
│   ├── NavigationManager.cs
│   ├── ShellEventCoordinator.cs
│   ├── SnackbarService.cs
│   ├── StatusBarManager.cs
│   ├── ThemeService.cs
│   ├── Bootstrap/
│   │   ├── IApplicationBootstrapper.cs
│   │   └── ApplicationBootstrapper.cs
│   ├── HealthCheck/
│   │   └── ApiHealthMonitor.cs
│   ├── Login/
│   │   ├── ILoginStateManager.cs
│   │   ├── LoginStateManager.cs
│   │   └── LoginCoordinator.cs
│   ├── Session/
│   │   ├── ISessionLifecycleManager.cs
│   │   ├── SessionLifecycleManager.cs
│   │   └── SessionBasedCurrentUserProvider.cs
│   └── Startup/
│       ├── StartupPipeline.cs
│       └── Steps/
│           ├── ErrorHandlingStartupStep.cs
│           ├── ModuleCoordinatorStartupStep.cs
│           ├── LocalWebApiStartupStep.cs
│           ├── ApiHealthCheckStartupStep.cs
│           └── WarmupStartupStep.cs
├── ViewModels/
│   ├── AccountSettingsViewModel.cs
│   └── MainWindowViewModel.cs
└── Views/
    ├── AccountSettingsView.xaml(.cs)
    └── MainWindow.xaml(.cs)
```

## 核心组件

| 类 | 设计依据 | 职责 |
|----|---------|------|
| **App** | PrismApplication 单实例模式 | 单实例 Mutex 防重复启动；Serilog 日志初始化；`ConfigureModuleCatalog` 加载 Core 模块 WhenAvailable + 业务模块 OnDemand；`RegisterTypes` 注册全部服务/对话框/主题；`OnInitialized` 显示主窗口并执行 `RunStartupAsync` |
| **MainWindowViewModel** | CoreViewModelBase，组合优于继承 | 委托 LoginStateManager/MenuManager/NavigationManager/StatusBarManager 四大管理器；`LogoutAsync` 含活跃医案检查防误退出；`ToggleSidebar` 侧边栏折叠；17 个 ICommand 均为委托转发 |
| **NavigationManager** | ObservableObject，基于角色动态构建 | `BuildNavigationItems` 按用户角色 + RoleRegistry 生成导航项，按「主页/业务/管理」分组 |
| **MenuManager** | DelegateCommand 快捷键绑定 | QuickAdd(`Ctrl+N`)、QuickStartMedicalCase(`Ctrl+Shift+C`)、Help(`F1`)、Settings(`Ctrl+,`)、主题切换、面包屑导航 |
| **StatusBarManager** | 响应式状态聚合 | 暴露 ApiStatus/ConnectionUrl/IsLocal/CurrentTime/ApiStatusIcon/ApiStatusColor；订阅 HealthMonitor + Connection 事件自动刷新 |
| **AppStartupOrchestrator** | 管道模式，有序步骤注册 | 注册 5 个启动步骤：ErrorHandling → ModuleCoordinator → LocalWebApi → ApiHealthCheck → Warmup |
| **ShellEventCoordinator** | 事件总线协调器 | 订阅 Auth/Session/Token/Password/Profile 事件，协调 LoginStateManager 状态同步 |
| **EmbeddedLocalWebApiService** | IEmbeddedLocalWebApiService | 在 WPF 进程内启动 Kestrel，为 Local 模式提供内嵌 WebAPI 服务 |
| **LoginStateManager** | ILoginStateManager，状态机 | 管理用户状态/Token/会话/Profile，是登录状态的单一事实来源 |
| **LoginCoordinator** | ILoginCoordinator，流程编排 | 完整登录流程：认证 → 会话创建 → 模块加载 → 导航跳转；SemaphoreSlim 防并发登录 |
| **SessionLifecycleManager** | 4 状态机 | Unauthenticated → Authenticated → Expired → Refreshing；协调 TokenManager + ActivityTracker |
| **SessionBasedCurrentUserProvider** | 会话感知的用户信息提供 | 从当前会话中提取用户信息，供 DI 注入使用 |
| **ApiHealthMonitor** | IApiHealthMonitor，断路器模式 | 定时健康检查：10s 间隔 / 5s 超时 / 3 次失败触发断路 / 30s 恢复窗口 |
| **ApplicationBootstrapper** | IApplicationBootstrapper | 注册 IRoleRegistry → IModuleManager、IPerformanceMonitor，完成框架级初始化 |
| **StartupPipeline** | Order 排序 + 并行分组 | 步骤按 Order 排序执行，相同 ParallelGroup 的步骤并行；Required 步骤失败则终止启动 |
| **ErrorHandlingStartupStep** | Order=10, Required | 全局异常处理初始化，启动管道第一步 |
| **ModuleCoordinatorStartupStep** | Order=20, ParallelGroup=CoreInit | 模块协调器初始化，与 CoreInit 组内其他步骤并行 |
| **LocalWebApiStartupStep** | Order=250 | 内嵌 LocalWebAPI 启动，依赖前置步骤完成 |
| **ApiHealthCheckStartupStep** | Order=40 | API 健康检查，验证远程/本地 API 可达性 |
| **WarmupStartupStep** | Order=50 | 预热缓存/资源，提升首次使用体验 |
| **ServiceCollectionExtensions** | 链式注册，单一入口 | `RegisterAllServices` 链：Config → Logging → Cache → Repos → HTTP → UnifiedApi → Foundation → Presentation → Infrastructure → Commands → Application → ViewModelServices |
| **HttpServiceRegistrationExtensions** | Handler 链 + Refit 客户端 | 配置 HTTP 消息处理链（Auth/Retry/Logging）+ 注册 Refit 接口客户端 |
| **UnifiedApiClientExtensions** | SwitchingApiClient 单例 | 注册切换式 API 客户端，localhost:5300 → 内嵌 LocalWebAPI，否则 → Refit 远程 |
| **PrismConfigurationExtensions** | 9 项 Prism 配置选项 | 统一 Prism 框架行为配置（区域管理、模块发现等） |
| **DataSourceRegistrationExtensions** | 数据源感知注册 | 根据连接模式注册 Repos + ConnectionMode + EmbeddedLocalWebApi |
| **ConfirmationDialogViewModel** | 删除确认场景 | 支持软删除/硬删除两种选项 |
| **MessageDialogViewModel** | 五行配色 | Success/Error/Warning/Info 四种类型，配色：木青/火赤/土黄/水黑 |
| **InputDialogViewModel** | 输入验证 | IsRequired 必填校验 |
| **ThemeService** | 持久化偏好 | 主题持久化到 theme-preference.json，基于 MaterialDesign PaletteHelper |
| **SnackbarService** | 轻量提示 | 全局 Snackbar 消息推送 |
| **DialogHostService** | 模态对话框 | 封装 MaterialDesign DialogHost 弹窗服务 |
| **AccountSettingsViewModel** | INavigationAware | 个人资料 + 密码修改，验证规则：8+ 位、一致性检查、不允许重复旧密码 |
| **NativeMethods** | P/Invoke | FindWindow/SetForegroundWindow/ShowWindow/ActivateExistingWindow（单实例窗口激活）、GetConsoleWindow/SetConsoleOutputCP（控制台编码） |

## 依赖关系

```
Shell
├── LYBT.Desktop.Core          (基础设施：DI 基类、接口契约、导航契约)
├── LYBT.Desktop.Infrastructure (数据访问、API 客户端、缓存)
├── LYBT.Module.Users          (用户/角色/认证模块)
├── LYBT.Module.MedicalCase    (医案模块)
├── LYBT.Module.Prescription    (处方模块)
├── LYBT.Module.Settings        (系统设置模块)
├── LYBT.Shared.Models          (DTO/Contracts)
├── Prism.DryIoc                (MVVM + DI + 模块化)
├── MaterialDesignThemes        (UI 主题)
├── Serilog                     (日志)
├── Refit                       (类型安全 HTTP 客户端)
└── Microsoft.AspNetCore        (内嵌 Kestrel)
```

## 设计决策

1. **PrismApplication 而非手动 DI** — 模块化架构天然需要 Prism 的 ModuleCatalog + RegionManager，避免自行实现模块发现和区域注册
2. **四大管理器委托模式** — MainWindowViewModel 不直接持有状态，而是委托 LoginStateManager/MenuManager/NavigationManager/StatusBarManager，单一职责且便于单元测试
3. **StartupPipeline 有序管道** — 启动步骤用 Order + ParallelGroup 控制执行顺序和并发，Required 标记区分可选/必选步骤，失败策略清晰
4. **SwitchingApiClient 双模路由** — localhost:5300 走内嵌 Kestrel（Local 模式），其他地址走 Refit 远程（Remote 模式），对业务层透明
5. **LoginCoordinator SemaphoreSlim** — 防止并发登录请求，认证 → 会话 → 模块 → 导航是原子流程
6. **ApiHealthMonitor 断路器** — 3 次失败触发断路、30s 恢复窗口，避免无效请求风暴
7. **五行配色对话框** — MessageDialog 用木青/火赤/土黄/水黑映射 Success/Error/Warning/Info，兼顾中医学主题一致性
8. **单实例 Mutex + P/Invoke 窗口激活** — 防重复启动的同时，将已有实例窗口带到前台
9. **Serilog 而非 ILogger 泛型** — 结构化日志 + 文件滚动，桌面场景比控制台 ILogger 更实用

## 已知陷阱

1. **RoleManager/UserManager 是 SCOPED** — 禁从 root provider resolve，必须在 scope 内使用
2. **BCrypt(PasswordHelper) vs PBKDF2(Identity) 不兼容** — 全部走 UserManager 统一密码哈希
3. **AddIdentity() 必在 JWT AddAuthentication() 前** — 否则认证管道顺序错误
4. **EmbeddedLocalWebApiService 路由前缀** — 必须 `api/v1/[controller]`，与远程 API 一致
5. **ApiResponse\<T\> 信封** — 所有响应必须包装，裸 `Ok()` 会导致 Refit 反序列化静默失败
6. **FindAsync 套全局过滤** — EF Core 全局过滤器 (IsDeleted) 会影响 FindAsync，恢复数据需 `IgnoreQueryFilters()`
7. **theme-preference.json 路径** — 使用 `AppContext.BaseDirectory`，非工作目录
