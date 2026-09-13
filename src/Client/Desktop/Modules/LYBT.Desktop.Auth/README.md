# LYBT.Desktop.Auth

> 登录认证模块 — 首次运行向导 / 凭据持久化 / 连接模式切换 / API 健康检查

## 项目定位

- **层级**: Client / Desktop / Modules
- **职责**: 用户登录界面与认证流程编排，管理凭据持久化（DPAPI）、连接模式（Remote/Local）检测与切换、首次运行配置向导
- **状态**: Active

## 目录结构

```
LYBT.Desktop.Auth/
├── AuthenticationModule.cs                # Prism 模块注册 (IModule, 无 ModuleDependency)
├── ViewModels/
│   ├── LoginViewModel.cs                  # 登录主 VM（组合 Credentials + ConnectionStatus 两个子 VM）
│   ├── LoginCredentialsViewModel.cs       # 凭证输入子 VM（用户名/密码/记住密码）
│   ├── ConnectionStatusViewModel.cs       # 连接状态子 VM（API 健康 + 连接模式切换）
│   ├── ConnectionTestViewModelBase.cs     # 连接测试对话框抽象基类（A-31-C5-4 收敛）
│   ├── ServerConfigViewModel.cs           # 服务器配置对话框 VM（继承 ConnectionTestViewModelBase）
│   └── FirstRunSetupViewModel.cs          # 首次运行向导 VM（继承 ConnectionTestViewModelBase）
├── Views/
│   ├── LoginView.xaml / .xaml.cs          # 登录视图（导航注册，PasswordBox code-behind 同步）
│   ├── ServerConfigView.xaml / .xaml.cs   # 服务器配置对话框（RegisterDialog）
│   └── FirstRunSetupView.xaml / .xaml.cs  # 首次运行向导对话框（RegisterDialog）
├── Models/
│   └── ConnectionTestStatus.cs            # 连接测试状态枚举（Idle/Testing/Success/Failed）
└── README.md
```

## 视图 / ViewModel 清单

**计数口径**（与桌面全局清单一致）：View = 页面/导航级 XAML（`*/Views/*.xaml`）；ViewModel 按「每文件 1 个 VM 类型」计，抽象基类不计入。

| 类别 | 数量 | 明细 |
|------|------|------|
| View | 3 | `LoginView`（`RegisterForNavigation`）、`ServerConfigView`、`FirstRunSetupView` |
| Dialog | 0（按路径口径） | `ServerConfigView` / `FirstRunSetupView` 物理位于 `Views/`，但经 `RegisterDialog` 作为对话框注册 |
| ViewModel | 5 | `LoginViewModel`、`LoginCredentialsViewModel`、`ConnectionStatusViewModel`、`ServerConfigViewModel`、`FirstRunSetupViewModel`（另有抽象基类 `ConnectionTestViewModelBase`） |

> 全桌面口径：View 30 / Control 33 / Dialog 7 / ViewModel 55（代码实际：`src/Client/Desktop`）。

## 核心组件

### AuthenticationModule — 模块入口

**设计依据**: 基础模块，无 `[ModuleDependency]`；Services 由 Core 层统一注册，此处仅注册 VM/View/Dialog

| 注册项 | 方式 | 说明 |
|--------|------|------|
| `LoginViewModel` | `Register<T>()` | 登录主 ViewModel |
| `LoginCredentialsViewModel` | `Register<T>()` | 凭证输入子 VM（D3：装配统一，由 DI 注入 `LoginViewModel`） |
| `ConnectionStatusViewModel` | `Register<T>()` | 连接状态子 VM（同上；`LoginViewModel` 内对二者 `Dispose`） |
| `LoginView` | `RegisterForNavigation<T>()` | 导航视图 |
| `ServerConfigView` + `ServerConfigViewModel` | `RegisterDialog<TView, TViewModel>()` | 服务器配置对话框 |
| `FirstRunSetupView` + `FirstRunSetupViewModel` | `RegisterDialog<TView, TViewModel>()` | 首次运行向导 |

### LoginViewModel — 登录主逻辑

**设计依据**: 继承 `NavigableViewModelBase`；**组合模式**——`Credentials`（`LoginCredentialsViewModel`）+ `ConnectionStatus`（`ConnectionStatusViewModel`）两个子 VM 由 DI 注入，主 VM 通过代理属性（`Username`/`Password`/`ApiStatus` 等）保持 XAML 绑定兼容；登录流程委托 `ILoginCoordinator`，VM 自身不直接调用 AuthApi

| 命令 | 类型 | CanExecute | 说明 |
|------|------|------------|------|
| `LoginCommand` | `AsyncRelayCommand` | Username/Password 非空且非 Loading | 调用 `ILoginCoordinator.LoginAsync`，成功后保存凭据；`allowConcurrentExecutions:false` 防双击重入 |
| `CloseApplicationCommand` | `AsyncRelayCommand` | 始终可用 | 确认对话框后 `Application.Current.Shutdown()` |
| `RetryApiCheckCommand` | `ICommand` | 转发自 `ConnectionStatus` | 触发 `IApplicationStateService.CheckApiHealthAsync` |
| `OpenSettingsCommand` | `ICommand` | 始终可用 | 打开 `ServerConfigView` 对话框 |
| `SwitchToLocalCommand` | `ICommand` | 转发自 `ConnectionStatus` | `IConnectionModeService.SetMode(Local)` |
| `SwitchToRemoteCommand` | `ICommand` | 转发自 `ConnectionStatus`（`IsRemoteAvailable`） | `IConnectionModeService.SetMode(Remote)` |
| `ForgotPasswordCommand` | `ICommand` | 始终可用 | 设计稿占位：找回流程待业务确认，当前为空实现 |

**BackgroundInitAsync 序列**（`SafeFireAndForget` 启动，首次 `Task.Delay(100)` 让出 UI）:
1. `MaybeShowFirstRunSetupAsync` — 检测 `%LOCALAPPDATA%\LYBT\Desktop\first_run_done.flag`，不存在则弹出向导，关闭后写标记
2. `Credentials.LoadSavedCredentialsAsync` — 从 `IUsernameStorageService` + `ICredentialVault`（DPAPI）加载用户名/密码
3. `ConnectionStatus.LoadApiStatusAsync` — 从 `IApplicationStateService` 读取 API 健康状态
4. `ConnectionStatus.DetectConnectionModeAsync` — 检测最佳连接模式（远程优先，本地回退）

### LoginCredentialsViewModel — 凭证输入子 VM

**设计依据**: 从 `LoginViewModel` 提取（SRP）；`[ObservableProperty]` 管理 `Username`/`Password`/`RememberUsername`/`RememberPassword`/`HasSavedPassword`/`SavedUsername`；承载保存/清除凭据的联动规则（见「已知陷阱」）

### ConnectionStatusViewModel — 连接状态子 VM

**设计依据**: 从 `LoginViewModel` 提取（SRP）；管理 `ApiStatus`/`ApiStatusMessage`/`IsApiUnhealthy`/`CurrentModeDisplay`/`IsRemoteMode`/`IsRemoteAvailable`，并持有 `RetryApiCheckCommand`/`SwitchToLocalCommand`/`SwitchToRemoteCommand`

### ConnectionTestViewModelBase — 连接测试对话框基类

**设计依据**: 继承 `DialogViewModelBase`；`ServerConfigViewModel` 与 `FirstRunSetupViewModel` 的 `TestConnectionAsync` 状态机约 95% 同构（A-31-C5-4 收敛），统一 `RemoteUrl`/`TestStatus`/`TestStatusMessage`/`IsNotTesting` + `TestConnectionCommand`；子类通过 `OnRemoteUrlChangedCore`/`OnTestStatusChangedCore`/`OnTestCompleted` 三个虚方法扩展差异

### ServerConfigViewModel — 服务器配置对话框

**设计依据**: 继承 `ConnectionTestViewModelBase`；两种保存模式：「保存并启用」(Confirm) vs 「仅保存」(SaveOnly)

| 命令 | 说明 |
|------|------|
| `TestConnectionCommand` | 调用 `IConnectionModeService.TestRemoteConnectionAsync`，更新 `TestStatus`/`TestStatusMessage` |
| `Confirm` (override) | 校验 URL + 测试通过后，`SetUrlAsync` + `SetMode(Remote)` + 关闭对话框 |
| `SaveOnlyAsync` | 仅 `SaveRemoteUrlAsync`，不切换当前模式 |

| 属性 | 类型 | 说明 |
|------|------|------|
| `RemoteUrl` | `string` | `[ObservableProperty]`（定义于基类），双向绑定 |
| `TestStatus` | `ConnectionTestStatus` | Idle/Testing/Success/Failed |
| `TestStatusMessage` | `string` | 状态描述文本 |
| `IsNotTesting` | `bool` | 测试中时禁用按钮 |

### FirstRunSetupViewModel — 首次运行向导

**设计依据**: 继承 `ConnectionTestViewModelBase`；单屏欢迎对话框，引导用户配置远程服务器或回退到本地模式

| 命令 | 说明 |
|------|------|
| `TestConnectionCommand` | 与 ServerConfig 共用基类连接测试逻辑 |
| `Confirm` (override) | 保存远程 URL 并切换到远程模式 |
| `UseLocalMode` | 直接 `SetMode(Local)` 并关闭对话框 |

| 属性 | 类型 | 说明 |
|------|------|------|
| `IsRemoteAvailable` | `bool` | 最近一次测试是否成功（`OnTestCompleted` 联动） |
| `ShouldShowFallbackHint` | `bool` | 测试失败时显示「将使用本地模式」提示 |

## 依赖关系

### 依赖

| 项目 | 用途 |
|------|------|
| LYBT.Desktop.Foundation | `IApplicationStateService`、`ICredentialVault`、`IUsernameStorageService`、`SafeFireAndForget` |
| LYBT.Desktop.Infrastructure | `NavigableViewModelBase`、`DialogViewModelBase`、`IViewModelServices`、`Interfaces.IClinicSettingsService`（品牌标题） |
| LYBT.Desktop.Contracts | `Services.ILoginCoordinator`、`IConnectionModeService`、`IConnectionSettingsService` |
| LYBT.Shared.Models | DTOs、Enums |

NuGet: `Prism.Core`, `Prism.DryIoc`, `Prism.Wpf`, `Microsoft.Extensions.Logging.Abstractions`

### 被依赖

| 消费方 | 方式 | 说明 |
|--------|------|------|
| Shell | `moduleCatalog.AddModule<AuthenticationModule>(WhenAvailable)` | 唯一立即加载的认证模块 |
| `UsersModule` | `[ModuleDependency("AuthenticationModule")]` | 用户管理需登录态 |
| `PatientsModule` | `[ModuleDependency("AuthenticationModule")]`（另依赖 `UsersModule`） | 患者管理需登录态 |
| `CatalogModule` | `[ModuleDependency("AuthenticationModule")]` | 药房目录模块只依赖认证 |
| `RegistrationModule` | `[ModuleDependency("AuthenticationModule")]`（另依赖 `PatientsModule`/`UsersModule`） | 挂号队列需登录态 |

> 角色台 `ClinicalModule` / `AdminModule` 不直接声明对认证模块的依赖，经上述业务模块传递。

## 设计决策

| 决策 | 原因 |
|------|------|
| 认证作为独立 Prism 模块 | 遵循模块化原则，便于独立测试和替换认证方案 |
| `ILoginCoordinator` 委托模式 | VM 不直接调用 AuthApi，解耦登录流程编排与 UI 逻辑 |
| 登录 VM 拆分为三个 VM（主 + 凭证 + 连接状态） | 单类职责过重；子 VM 各自单一职责（凭证输入 / 连接状态），主 VM 以代理属性保持 XAML 绑定兼容 |
| `ConnectionTestViewModelBase` 抽象基类 | ServerConfig 与 FirstRunSetup 的测试状态机 ~95% 同构（A-31-C5-4），收敛到基类 + 三个虚扩展点 |
| PasswordBox code-behind 同步 | WPF 安全限制：`PasswordBox.Password` 不支持数据绑定，需 `PasswordChanged` 事件手动同步并防循环 |
| DPAPI 凭据持久化 | `ICredentialVault` 使用 Windows DPAPI 加密保存密码，非明文存储 |
| 首次运行标记文件 | `%LOCALAPPDATA%\LYBT\Desktop\first_run_done.flag`，标记失败不阻塞使用 |
| `SafeFireAndForget` 异步模式 | BackgroundInitAsync 和 SaveAndEnableAsync 使用此模式避免未观察异常 |

## 已知陷阱

- `PasswordBox.Password` 不支持 WPF 数据绑定，必须通过 code-behind 的 `PasswordChanged` 事件手动同步，且需防止循环更新
- `LoginView` 构造函数中 Prism 可能在 `InitializeComponent` 时就设置 `DataContext`，此时 `DataContextChanged` 不会触发，需在构造函数中手动处理
- 子 VM 属性经主 VM 代理属性转发：`LoginViewModel` 订阅 `Credentials`/`ConnectionStatus` 的 `PropertyChanged` 再 `OnPropertyChanged(e.PropertyName)` 透传；`Username`/`Password` 变化还需额外 `LoginCommand.NotifyCanExecuteChanged()`，否则登录按钮会保持灰色
- `RememberPassword` 勾选时自动联动勾选 `RememberUsername`（取消时不反向取消）
- 切换用户名时，如果之前有已保存的密码，会自动清空 `Password` 字段
- 登录按钮防重入依赖 `LoginCommand` 的 `!IsLoading` 谓词 + `AsyncRelayCommand` 默认拒绝并发执行；`ExecuteLoginAsync` 的 `finally` 必须复位 `IsLoading` 并 `NotifyCanExecuteChanged`，否则登录后返回登录页会残留遮罩
- 单窗口模式：`LoginView` 为 `UserControl` 嵌入主窗口；早期 `LoginWindow.xaml` 已删除，不要再按独立登录窗口的假设改动
- 首次运行标记文件写入失败时，下次启动仍会弹出向导（设计为可接受的降级）
- `ForgotPasswordCommand` 是空实现占位（设计稿对齐），未接业务链路

---

2026-09-13 docs 复盘：与代码对齐（View/VM 清单、目录树、依赖）
