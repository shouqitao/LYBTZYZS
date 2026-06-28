# Shell 现状审查报告（重构前置基线）

> **用途**：clean-slate 重写前的代码事实基线。所有结论均经 Serena/CodeGraph 符号级核验或逐字源码确认，标注 `file:line` 证据。
> **方法**：3 个并行探查 subagent 全量深读 + Serena 符号 overview + CodeGraph 关系图 + 逐字源码核验三处关键论断。
> **日期**：2026-06-28

## [S1] 审查范围与方法

覆盖 `src/Client/Desktop/Shell/` 全部代码 + 关联的 `Core/LYBT.Desktop.Controls` 资源 + `Core/LYBT.Desktop.Infrastructure` 中的 Shell 依赖服务。总量 ~4500 行。

核验工具：
- **CodeGraph**（仓库已索引）— 关系图、调用链、blast radius
- **Serena** — 符号 overview、诊断、引用查找
- **逐字源码** — 对三处「双轨/三套」论断逐一调取实现体验证

## [S2] 技术栈（已锁定，全部沿用）

| 层 | 技术 | 证据 |
|----|------|------|
| 运行时 | .NET 8 `net8.0-windows`，WPF | `LYBT.Desktop.Shell.csproj:4-6` |
| MVVM | Prism (`Core`/`DryIoc`/`Wpf`) + CommunityToolkit.Mvvm | `csproj:46-49`；VM 用 `[ObservableProperty]`/`[RelayCommand]` |
| UI 库 | MaterialDesignThemes 5.3.2，MD2 风格 | `App.xaml:13-17` |
| 主题 | Light/Brown/Amber，运行时切 Dark | `App.xaml:14-16`；`ThemeService.cs` |
| 日志 | Serilog 两阶段 | `App.xaml.cs:50`；`DesktopSerilogConfiguration` |
| 配置 | `Microsoft.Extensions.Configuration`(JSON) | `csproj:54-57` |
| 持久化 | EF Core SqlServer（为内嵌 LocalWebAPI） | `csproj:64` |
| 对话框 | 决策迁移到 MDIX `DialogHost`（**未完成**，见 S7） | memory `shell-refactor/decisions` |

## [S3] Shell 组合（实际 vs 决策记录）

**实际 `MainWindow.xaml`**：
```
Window(MaterialDesignWindow, WindowStyle=None, Maximized)
 └─ Grid
    ├─ LoginRegion (Visibility=IsNotLoggedIn)
    └─ Post-login Grid (Visibility=IsLoggedIn)
       ├─ DialogHost(Identifier="RootDialog")
       │   └─ Grid[Column=Sidebar | Column=Content + Row=StatusBar]
       └─ Snackbar(MainSnackbar)
```

**memory 记录**：`Window → DialogHost → SnackbarHost → DrawerHost → DockPanel(TopBar + ContentRegion)`

**差异**：实际无 `DrawerHost`、无 `TopBar`；memory 记录的结构是未落地的设计。本次重写以实际代码为准。

## [S4] 核心问题 1：MainWindowViewModel 上帝类（严重度 🔴 致命）

**证据**：`ViewModels/MainWindowViewModel.cs`（1025 行），CodeGraph 逐字核验。

- **13 个构造注入**（L285-299）：`IViewModelServices`, `IUserNotificationService`, `IApiHealthMonitor`, `IApiRouter`, `IConnectionSettingsService`, `INavigationCoordinator`, `MenuManager`(具体类，非接口), `IActiveConsultationService`, `IApplicationTickService`, `IUserActivityTracker`, `ITokenLifecycleService`, `ILoginCoordinator`, `IConnectionModeService`, `IThemeService`
- **8 大职责**聚集：登录状态 / 侧边栏 / 状态栏 / 快捷键委托 / 健康检查 / Token 生命周期 / 主题 / 对话框
- **16 个 ICommand** 仅转调 `MenuManager`（VM 当绑定跳板）
- **10 个事件订阅**（Tick/StatusChanged/UrlChanged/ModeChanged/NavigationChanged/SessionExpired/LoginSucceeded/PasswordChangedEvent/ProfileUpdatedEvent/TokenLifecycleStateChangedEvent），`OnDisposing` 拆解
- **硬编码导航映射**：`BuildNavigationItems` 用 `modules.Contains("PatientsModule")` 字符串魔法值拼装（~70 行），与 `MenuManager` 职责重叠
- **硬编码颜色**：`Brushes.Green/Orange/Gray`（L269-276 TODO 自承认未跟主题）
- **死代码**：`GroupedNavItems` getter 仍构造 `ICollectionView`，但 XAML ListBox 未启用分组
- **过时字段**：`ConnectionUrl` 默认 `http://127.0.0.1:5300`，与 AGENTS.md 约定（5000/5100）矛盾
- **诊断**：4 个 IDE0005 未用 using 警告，无编译错误

## [S5] 核心问题 2：启动管线双轨初始化（严重度 🔴 高，已逐字核验）

**证据链**（CodeGraph 逐字源码）：

`ApplicationInitializationService.InitializeCoreServicesAsync()` (`ApplicationInitializationService.cs:64-86`) 顺序调用：
```
InitializeErrorHandling()        // L71 → _exceptionHandler.RegisterGlobalExceptionHandlers() (L97)
WarmupApplicationAsync()         // L74 → _startupOptimizationService.WarmupApplicationAsync() (L121)
InitializeModuleCoordinator()    // L77 → 反射取 LoadedModules 私有属性 (L148-152，脆弱)
```

同时 `AppStartupOrchestrator` 注册 6 个 step（`StartupPipeline.ExecuteAsync` 按 Order 排序，相邻同 ParallelGroup 合并）：

| Order | Step | Required | ParallelGroup | 核验到的重复 |
|-------|------|----------|---------------|-------------|
| 10 | ErrorHandling | ✅ | — | `RegisterGlobalExceptionHandlers` 与 `InitializeErrorHandling` **同一调用** → **注册两次** |
| 20 | ModuleCoordinator | ❌ | CoreInit | 订阅 `LoadModuleCompleted` 事件 |
| 30 | CoreServices | ✅ | CoreInit | **委托 `InitializeCoreServicesAsync`** → 内部又跑一遍 ErrorHandling+Warmup+ModuleCoordinator |
| 40 | ApiHealthCheck | ❌ | — | Task.Run 后台检查（见 S6） |
| 50 | Warmup | ❌ | — | 与 CoreServices 内部 `WarmupApplicationAsync` **重复** |
| 250 | LocalWebApi | ❌ | — | 启动内嵌 Kestrel |

**确认后果**：`RegisterGlobalExceptionHandlers` 被注册两次、`WarmupApplicationAsync` 跑两次、模块事件被订阅路径重叠。

**附加问题**：
- `AppStartupOrchestrator.RunStartupAsync` 调 `pipeline.ExecuteAsync()` **未传 `IProgress<string>`** → step 内 `progress?.Report(...)` 全部 no-op，splash 拿不到进度
- `LocalWebApiStartupStep.Order=250` 与「顺序」语义不符（命名误导）
- `InitializeModuleCoordinator` 用反射取 `LoadedModules` 私有属性（L148-152），脆弱

## [S6] 核心问题 3：三套健康检查并存（严重度 🔴 高，已逐字核验）

三者均最终调用底层 `IApiHealthCheckService.CheckHealthAsync`：

| 实现 | 文件 | 驱动 | 断路器 | 状态同步 |
|------|------|------|--------|---------|
| `HealthCheckCoordinator` | `HealthCheck/HealthCheckCoordinator.cs`(214行) | `IApplicationTickService.Tick`(10s) | ❌ | 写 `IApplicationStateService` 属性 (L172-199) |
| `ApiHealthMonitor` | `HealthCheck/ApiHealthMonitor.cs`(276行) | `Timer`(10s) | ✅(3次/30s) | `StatusChanged` 事件 |
| `ApiHealthCheckStartupStep` | `Startup/Steps/ApiHealthCheckStartupStep.cs`(68行) | `Task.Run` 一次性 | ❌ | 写 `IApplicationStateService` (L48) |

**确认后果**：
- 同一职责三套代码、三个定时器并行跑
- `HealthCheckCoordinator` 与 `ApiHealthCheckStartupStep` 都通过赋值 `IApplicationStateService` 属性同步状态 → **竞态风险**
- `MainWindowViewModel` 注入的是 `IApiHealthMonitor`（L288），而 `HealthCheckCoordinator` 另走 `IApplicationTickService`，两者状态可能不一致
- `ApiHealthMonitor` 有 `#pragma warning disable CS1998`（async 缺 await，L57/75）

## [S7] 核心问题 4：模块加载双轨 + 登录路径硬编码（严重度 🔴 高，已逐字核验）

**两条独立加载路径并存**：

| 路径 | 入口 | 实现 | 数据源 |
|------|------|------|--------|
| 正规 | `ApplicationBootstrapper.LoadModulesForRoleAsync` | `ApplicationBootstrapper.cs:36-88` | `IRoleRegistry.GetModulesForRole`（数据驱动，`RoleRegistry.cs:81-90`） |
| 旁路 | `LoginCoordinator.LoadModulesForUserAsync` | `LoginCoordinator.cs:261-281` | **硬编码**（见下） |

**LoginCoordinator 硬编码（逐字，`LoginCoordinator.cs:261-281`）**：
```csharp
private async Task LoadModulesForUserAsync(UserDetailDto user)
{
    bool isAdmin = user.UserName?.Equals(SystemConstants.SuperAdminUsername, ...) == true
                   || user.Role == UserRole.Admin;

    await _moduleLoadingService.LoadModulesAsync(new[] { "PatientsModule" });   // 所有人只加这个

    if (isAdmin)
        await _moduleLoadingService.LoadModulesAsync(new[]
            { "UsersModule", "HerbsModule", "FormulaModule", "MedicalCaseModule" });
}
```

**`IModuleLoadingService`/`ModuleLoadingService` 本身干净**（`Foundation/Modules/ModuleLoadingService.cs`）——通用加载器，无硬编码；硬编码在 LoginCoordinator 调用方。

**确认后果（这是 bug，不止是 smell）**：
- 登录路径**完全绕过 `RoleRegistry`**
- **Doctor / Receptionist / Clinical / Sysadmin 经此路径只加载 `PatientsModule`**，拿不到角色专属模块
- 仅 Admin/SuperAdmin 额外获得 Users/Herbs/Formula/MedicalCase
- 与 `App.LoadRoleBasedModulesAsync`(走 RoleRegistry) 形成两套可能冲突的加载路径
- `RoleRegistry.GetModulesForRole` 定义的角色→模块映射在登录时被无视

## [S8] 核心问题 5：反模式蔓延（严重度 🟠 中，AGENTS.md 明文禁止）

| 位置 | 反模式 | 证据 |
|------|--------|------|
| `DialogHostService.cs` | `ContainerLocator.Container.Resolve` | 注释自承认 |
| `AppStartupOrchestrator.cs` | `_container.Resolve<IStartupStep>("name")` 命名解析 | Service Locator |
| `MainWindow.xaml.cs:35` | `ContainerLocator.Current` | code-behind 持容器 |
| `EmbeddedLocalWebApiService.cs` | 硬编码 URL/连接串/密码 | **已逐字核验**：`LocalUrl="http://localhost:5300"`(L17)、`LocalConnectionString="(localdb)\MSSQLLocalDB;LYBTDesktop;..."`(L18-19)、3 套密码在 `StartAsync` L50-52（`SysAdmin@2026!`/`Admin@123456`/`User@123456`）。违背 AGENTS.md「密码从 appsettings.json 读」意图 |
| `MenuManager.cs`(328行) | 无接口 + 主题切换用 ResourceDictionary 硬编码颜色（与 ThemeService 重复且与 MDIX 体系冲突） | 与 `ThemeService` 职责重叠 |

## [S9] 核心问题 6：文档与资源大面积失真（严重度 🟡 低-中）

| 项 | 实际 |
|----|------|
| `Shell/Styles/` 目录 | **不存在**（README 列的 Typography/Controls/DialogStyles/CommonStyles 全是幻觉） |
| csproj `..\Themes\Design\*.xaml` / `..\Themes\Controls\*.xaml` | **失效 glob**（`src/Client/Desktop/Themes` 不存在，解析为空集） |
| `LoggingRegistrationExtensions.RegisterDataSourceLoggers` | 空方法体 |
| `README.md` | App.xaml 资源链（HandyControl/TCM.Theme/UnifiedComponents/...）与实际**完全不符** |
| Dialogs 引用 `ButtonPrimary`/`TextBoxExtend`/`StringToVisibilityConverter` | App.xaml **未定义**，运行期资源解析风险 |
| 3 个对话框（Confirmation/Message/Input） | 仍是 Prism `IDialogService` 风格（`App.xaml.cs:100-102` `RegisterDialog`），**未迁移**到已决策的 MDIX `DialogHost` |
| `AccountSettingsControl.xaml.cs` | 密码同步双机制（PasswordBoxHelper + 手动 code-behind）冗余 |
| `StartupPerformanceMonitor.cs`(123行) | 与 `Foundation.IPerformanceMonitor` 不是同一套（重复造轮子） |

## [S10] 次要发现

- `App.xaml.cs:131-158` `ConfigureModuleCatalog` 硬编码 13 个模块（无 directory scan），注释与代码并存矛盾
- `App.xaml.cs:197-204` P/Invoke `kernel32` 内联在 App 类
- `SessionLifecycleManager.cs`(328行) 全 lock 保护，订阅 `TokenLifecycleStateChangedEvent` + `IUserActivityTracker.SessionExpired`
- `Lifecycle/ApplicationState.cs`(38行) 纯枚举，**无代码引用其作为强类型状态机**（死枚举）
- `Models/TodayPatientItem.cs`(96行) 唯一模型，是否仍被工作台引用需重写前验证
- `App.xaml` 内联 `CustomDialogWindowStyle` 背景硬编码 `White`

## [S11] 区域复杂度评分

| 区域 | 行数 | 复杂度 | 最突出问题 |
|------|------|--------|-----------|
| UI/VM（MainWindowViewModel + Views） | ~1400 | **8/10** | 1025 行上帝类 |
| Services（含启动管线） | ~2400 | **7/10** | 双轨初始化 + 三套健康检查 |
| 辅助层（Controls/Dialogs/Extensions/Models） | ~1700 | **6/10** | README 失真 + 失效引用 |

## [S12] 核验状态

| # | 项 | 状态 |
|---|----|----|
| 1 | `IModuleLoadingService` 实现体是否硬编码 | ✅ **已核验** — 加载器干净，硬编码在 `LoginCoordinator.LoadModulesForUserAsync`（S7） |
| 2 | `EmbeddedLocalWebApiService` 硬编码 URL/连接串/密码 | ✅ **已核验**（S8，L17-19/L50-52） |
| 3 | `MenuManager` 14 个命令哪些是 TODO 占位 | ⬜ 待核验（subagent 称 ShowHistory/CycleRegions 未实现） |
| 4 | `MainWindowViewModel.BuildNavigationItems` 完整体 + 与 RoleRegistry 关系 | ⬜ 待核验 |
| 5 | Dialogs 三对话框资源解析是否真失败 | ⬜ 待核验（运行期行为） |
| 6 | 测试覆盖 | ✅ **已核验** — CodeGraph 标注 `MainWindowViewModel`/`ApplicationInitializationService`/`HealthCheckCoordinator`/`ApiHealthMonitor`/`LoadModulesForRoleAsync`/`LoginCoordinator` 均「no covering tests found」，重写缺乏回归网 |

**剩余待核验（3 项）**：MenuManager TODO 占位、BuildNavigationItems 完整体、Dialogs 运行期资源解析。这三项属「设计某子系统时再深挖」的局部细节，不阻塞整体审查结论。

## [S13] 本文档边界

本文档**只记录审查事实，不含重构方案**。重构方案将在后续多轮对话中，基于本基线逐个子系统设计，每个子系统独立走 spec → plan → implement 循环。

可识别的独立重构子系统（待用户选择本轮范围）：
1. VM 拆分 + 导航数据驱动
2. 启动管线收敛（单一真相源 + splash 进度）
3. 健康检查统一（三套合一）
4. MainWindow XAML 重写（含 MDIX DialogHost 对话框迁移）
5. 对话框体系迁移
6. 文档与死代码清理（低风险预热）

## [S14] 框架规范 vs Shell 现状 交叉对比

> 来源：context7 官方文档（Prism `/prismlibrary/prism-documentation`、MDIX `/keboo/materialdesigninxaml.examples`、CTM `learn.microsoft.com/.../communitytoolkit/mvvm`）。仅记录"框架打算让你怎么做" vs "我们怎么做"。

### 对比 1：模块加载（对应 S7）— Prism

| 维度 | Prism 规范 | Shell 现状 | 偏差 |
|------|-----------|-----------|------|
| 模块清单 | `ConfigureModuleCatalog` + `ModuleInfo`(WhenAvailable/OnDemand)，或 XAML 声明式 | `App.xaml.cs:131-158` 代码清单 13 模块 | ✅ 符合规范（代码式合法） |
| 按需加载 API | `IModuleManager.LoadModule(name)` 注入到消费方 | `ApplicationBootstrapper` + `LoginCoordinator` + `ModuleLoadingService` **三套入口**都最终调 `LoadModule` | ⚠️ 三套包装，入口分散 |
| 模块名真相源 | **单一**（清单/配置/注册表） | `RoleRegistry`(正规) **vs** `LoginCoordinator` 硬编码(旁路) | 🔴 **偏离**——Prism 期望单一真相源，Shell 有两个且登录旁路绕过 Registry |

**结论**：规范允许服务包装 `LoadModule`，但**模块名必须单一真相源**。Shell 的 S7 bug 本质就是违背此原则。

### 对比 2：应用启动（对应 S5）— Prism

| 维度 | Prism 规范 | Shell 现状 | 偏差 |
|------|-----------|-----------|------|
| 启动钩子 | `RegisterTypes` / `ConfigureModuleCatalog` / `OnInitialized` / `CreateShell` | Shell 用了全部 + 自造 `IStartupPipeline`(6 step) + `ApplicationInitializationService` | ⚠️ Prism 已提供启动扩展点，Shell 在其上又叠了一套 |
| 初始化逻辑归属 | 模块初始化在 `IModule.OnInitialized`，服务在 `RegisterTypes` | 双轨：step 一次 + `InitializeCoreServicesAsync` 再一次 | 🔴 **偏离**——重复执行 |

**结论**：Shell 自造的 `IStartupPipeline` 本身不是反模式（Prism 允许扩展），但**没有与 Prism 原生扩展点明确分工**，导致同一初始化跑两遍。规范做法：每个初始化动作只挂在一个扩展点上。

### 对比 3：对话框（对应 S9）— MDIX

| 维度 | MDIX 规范 | Shell 现状 | 偏差 |
|------|-----------|-----------|------|
| 对话框宿主 | `DialogHost Identifier="RootDialog"`，`DialogHost.Show(content, "RootDialog")` | XAML 有 `DialogHost`(`MainWindow.xaml:42-44`)，但 `DialogHostService` 用 `ContainerLocator` 解析 View | 🔴 ServiceLocator 多余——`DialogHost.Show` 是静态式调用，无需容器 |
| 关闭命令 | `DialogHost.CloseDialogCommand` | (未核验用法) | 待核验 |
| 对话框风格 | MDIX 风格按钮(`MaterialDesignRaisedButton`/`FlatButton`) | 3 个旧对话框仍 Prism `IDialogService` 风格(`App.xaml.cs:100-102`) | 🔴 **未迁移**——memory 已决策全迁 DialogHost |

**结论**：MDIX `DialogHost` 设计上不需要 ServiceLocator 包装；Shell 的 `DialogHostService` 滥用容器。规范可直接 `DialogHost.Show`，或用轻量工厂注入 View（非容器 Resolve）。

### 对比 4：Shell 布局（对应 S3）— MDIX

| 维度 | MDIX 规范/示例 | Shell 现状 | 偏差 |
|------|-----------|-----------|------|
| 侧边导航 | 示例用 `DrawerHost`+`LeftDrawerContent`+`HamburgerToggleButton` 或固定侧栏 | 固定 `Border` 侧栏 + 宽度切换(60/140) | ⚠️ 设计选择（固定侧栏合法），memory 记的 DrawerHost 未落地 |
| 顶栏 | 示例 `ColorZone Mode="PrimaryMid" + ShadowAssist.Depth2` | 无顶栏，标题栏 `WindowStyle=None` 自绘 | ⚠️ 无 ColorZone 包裹 |
| Snackbar | `Snackbar MessageQueue` | `MainSnackbar`(`MainWindow.xaml:218`) | ✅ 符合规范 |

**结论**：布局偏差是设计取舍，非硬错误。但 memory `shell-refactor/decisions` 记的 DrawerHost/TopBar 与实际不符——重写需明确**保留固定侧栏**还是**改回 DrawerHost**。

### 对比 5：VM 解耦（对应 S4）— CommunityToolkit.Mvvm

| 维度 | CTM 规范 | Shell 现状 | 偏差 |
|------|-----------|-----------|------|
| VM 间通信 | `Messenger` + `ObservableRecipient` + `IRecipient<TMessage>`，`OnActivated` 自动注册 | `MainWindowViewModel` 直接订阅 10 个服务事件，`OnDisposing` 手动拆解 | 🔴 **偏离**——未用 Messenger，VM 与服务强耦合 |
| VM 体积 | 小而专，职责拆到多 VM/服务 | 1025 行上帝 VM，8 职责 | 🔴 **偏离** |
| 属性/命令 | `[ObservableProperty]` / `[RelayCommand]` | 已用 | ✅ 符合规范 |
| DI | `IServiceProvider` 解析（App.Current.Services 模式） | `ContainerLocator` 在多处 service 里 | 🔴 ServiceLocator 反模式 |

**结论**：CTM 的 Messenger 是为"拆上帝 VM"准备的核心工具——Shell 完全没用。这是 S4 重构的关键杠杆：状态广播（登录/连接模式/主题/健康）改走 Messenger，订阅方 VM 各自 `IRecipient`，主 VM 不再当事件总线。

### 对比 6：DI / ServiceLocator（对应 S8）— Prism + CTM

| 维度 | 规范 | Shell 现状 | 偏差 |
|------|-----------|-----------|------|
| 服务解析 | 构造注入 | 多处 `ContainerLocator.Container.Resolve`(`DialogHostService`/`AppStartupOrchestrator` 命名 Resolve/`MainWindow.xaml.cs`) | 🔴 AGENTS.md 明文禁止 |
| 命名 Resolve | 不鼓励（编译期不可验） | `Resolve<IStartupStep>("ErrorHandling")` | 🔴 脆弱 |

**结论**：规范统一构造注入；命名 Resolve 和 ContainerLocator 都应消除。

## [S15] 交叉对比后的重构杠杆（按收益排序）

1. **引入 CTM Messenger 拆上帝 VM**（对比 5）——最高杠杆。登录状态/连接模式/主题/健康/Token 改为广播消息，`MainWindowViewModel` 只留导航协调，其他职责分散到 `LoginStatusViewModel`/`StatusBarViewModel`/`SidebarViewModel` 等。
2. **单一模块名真相源**（对比 1）——删除 `LoginCoordinator` 硬编码旁路，所有加载走 `RoleRegistry`；保留 `ModuleLoadingService` 作为唯一 `LoadModule` 包装。
3. **初始化动作单一挂载点**（对比 2）——每个初始化只在一个扩展点（要么 Prism 钩子，要么 step，二选一），消灭双轨。
4. **对话框直连 DialogHost.Show**（对比 3）——删 `DialogHostService` 的 ContainerLocator，3 个旧对话框迁 MDIX。
5. **消除全部 ContainerLocator/命名 Resolve**（对比 6）——改构造注入。
6. **明确布局取舍**（对比 4）——固定侧栏 vs DrawerHost 二选一并写入决策 memory。

## [S16] 文档更新

本次审查发现 memory `shell-refactor/decisions` 记录的 Shell 嵌套顺序（DrawerHost/TopBar）与实际代码不符。重写启动前应：
- 更新该 memory，标注实际结构为准
- 或在重写时明确选择并落笔
