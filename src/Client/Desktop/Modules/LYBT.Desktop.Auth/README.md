# LYBT.Desktop.Auth

> 登录认证模块 — 首次运行向导 / 凭据持久化 / 连接模式切换 / API 健康检查

## 项目定位

- **层级**: Client / Desktop / Modules
- **职责**: 用户登录界面与认证流程编排，管理凭据持久化（DPAPI）、连接模式（Remote/Local）检测与切换、首次运行配置向导
- **状态**: Active

## 目录结构

```
LYBT.Desktop.Auth/
├── AuthenticationModule.cs                # Prism 模块注册 (IModule, 无依赖)
├── ViewModels/
│   ├── LoginViewModel.cs                  # 登录主 ViewModel (~705 行)
│   ├── ServerConfigViewModel.cs           # 服务器配置对话框 ViewModel
│   └── FirstRunSetupViewModel.cs          # 首次运行配置向导 ViewModel
├── Views/
│   ├── LoginView.xaml / .xaml.cs          # 登录视图 (UserControl, PasswordBox code-behind 同步)
│   ├── ServerConfigView.xaml / .xaml.cs   # 服务器配置对话框
│   └── FirstRunSetupView.xaml / .xaml.cs  # 首次运行向导对话框
└── README.md
```

## 核心组件

### AuthenticationModule — 模块入口

**设计依据**: 基础模块，无 `[ModuleDependency]`；Services 由 Core 层统一注册，此处仅注册 VM/View/Dialog

| 注册项 | 方式 | 说明 |
|--------|------|------|
| `LoginViewModel` | `Register<T>()` | 登录 ViewModel |
| `LoginView` | `RegisterForNavigation<T>()` | 导航视图 |
| `ServerConfigView` + `ServerConfigViewModel` | `RegisterDialog<TView, TViewModel>()` | 服务器配置对话框 |
| `FirstRunSetupView` + `FirstRunSetupViewModel` | `RegisterDialog<TView, TViewModel>()` | 首次运行向导 |

### LoginViewModel — 登录主逻辑

**设计依据**: 继承 `NavigableViewModelBase`；通过 `ILoginCoordinator` 委托登录流程，VM 自身不直接调用 AuthApi；凭据通过 `IUsernameStorageService` + `ICredentialVault`（DPAPI）持久化

| 命令 | 类型 | CanExecute | 说明 |
|------|------|------------|------|
| `LoginCommand` | `DelegateCommand` | Username/Password 非空且非 Loading | 调用 `ILoginCoordinator.LoginAsync`，成功后保存凭据 |
| `CloseApplicationCommand` | `DelegateCommand` | 始终可用 | 确认对话框后 `Application.Current.Shutdown()` |
| `RetryApiCheckCommand` | `DelegateCommand` | `ApiStatus == Unhealthy` | 触发 `IApplicationStateService.CheckApiHealthAsync` |
| `OpenSettingsCommand` | `DelegateCommand` | 始终可用 | 打开 `ServerConfigView` 对话框 |
| `SwitchToLocalCommand` | `DelegateCommand` | 始终可用 | `IConnectionModeService.SetMode(Local)` |
| `SwitchToRemoteCommand` | `DelegateCommand` | `IsRemoteAvailable` | `IConnectionModeService.SetMode(Remote)` |

**BackgroundInitAsync 序列**:
1. `MaybeShowFirstRunSetupAsync` — 检测 `%LOCALAPPDATA%\LYBT\Desktop\first_run_done.flag`，不存在则弹出向导
2. `LoadSavedCredentialsAsync` — 从 `IUsernameStorageService` + `ICredentialVault` 加载用户名/密码
3. `LoadApiStatusFromStateServiceAsync` — 从 `IApplicationStateService` 读取 API 健康状态
4. `DetectConnectionModeAsync` — 检测最佳连接模式（远程优先，本地回退）

### ServerConfigViewModel — 服务器配置对话框

**设计依据**: 继承 `DialogViewModelBase`；两种保存模式：「保存并启用」(Confirm) vs 「仅保存」(SaveOnly)

| 命令 | 说明 |
|------|------|
| `TestConnectionAsync` | 调用 `IConnectionModeService.TestRemoteConnectionAsync`，更新 `ConnectionTestStatus` |
| `Confirm` (override) | 校验 URL + 测试通过后，`SetUrlAsync` + `SetMode(Remote)` + 关闭对话框 |
| `SaveOnlyAsync` | 仅 `SaveRemoteUrlAsync`，不切换当前模式 |

| 属性 | 类型 | 说明 |
|------|------|------|
| `RemoteUrl` | `string` | `[ObservableProperty]`，双向绑定 |
| `TestStatus` | `ConnectionTestStatus` | Idle/Testing/Success/Failed |
| `TestStatusMessage` | `string` | 状态描述文本 |
| `IsNotTesting` | `bool` | 测试中时禁用按钮 |

### FirstRunSetupViewModel — 首次运行向导

**设计依据**: 继承 `DialogViewModelBase`；单屏欢迎对话框，引导用户配置远程服务器或回退到本地模式

| 命令 | 说明 |
|------|------|
| `TestConnectionAsync` | 与 ServerConfig 相同的连接测试逻辑 |
| `Confirm` (override) | 保存远程 URL 并切换到远程模式 |
| `UseLocalMode` | 直接 `SetMode(Local)` 并关闭对话框 |

| 属性 | 类型 | 说明 |
|------|------|------|
| `IsRemoteAvailable` | `bool` | 最近一次测试是否成功 |
| `ShouldShowFallbackHint` | `bool` | 测试失败时显示「将使用本地模式」提示 |

## 依赖关系

```
LYBT.Desktop.Auth
├── LYBT.Desktop.Foundation      (IApplicationStateService, IConnectionModeService, ICredentialVault, IUsernameStorageService, ILoginCoordinator)
├── LYBT.Desktop.Infrastructure   (NavigableViewModelBase, DialogViewModelBase, IViewModelServices)
├── LYBT.Desktop.Contracts        (IConnectionSettingsService)
└── LYBT.Shared.Models            (DTOs, Enums)
```

NuGet: `Prism.Core`, `Prism.DryIoc`, `Prism.Wpf`, `Microsoft.Extensions.Logging.Abstractions`

**被依赖**: Shell 启动时加载；其他模块（如 Patients、Users）通过 `[ModuleDependency("AuthenticationModule")]` 声明依赖

## 设计决策

| 决策 | 原因 |
|------|------|
| 认证作为独立 Prism 模块 | 遵循模块化原则，便于独立测试和替换认证方案 |
| `ILoginCoordinator` 委托模式 | VM 不直接调用 AuthApi，解耦登录流程编排与 UI 逻辑 |
| PasswordBox code-behind 同步 | WPF 安全限制：`PasswordBox.Password` 不支持数据绑定，需 `PasswordChanged` 事件手动同步并防循环 |
| DPAPI 凭据持久化 | `ICredentialVault` 使用 Windows DPAPI 加密保存密码，非明文存储 |
| 首次运行标记文件 | `%LOCALAPPDATA%\LYBT\Desktop\first_run_done.flag`，标记失败不阻塞使用 |
| `SafeFireAndForget` 异步模式 | BackgroundInitAsync 和 SaveAndEnableAsync 使用此模式避免未观察异常 |

## 已知陷阱

- `PasswordBox.Password` 不支持 WPF 数据绑定，必须通过 code-behind 的 `PasswordChanged` 事件手动同步，且需防止循环更新
- `LoginView` 构造函数中 Prism 可能在 `InitializeComponent` 时就设置 `DataContext`，此时 `DataContextChanged` 不会触发，需在构造函数中手动处理
- `RememberPassword` 勾选时自动联动勾选 `RememberUsername`（取消时不反向取消）
- 切换用户名时，如果之前有已保存的密码，会自动清空 `Password` 字段
- `LoginWindow.xaml` 已弃用（代码注释明确标记），当前使用单窗口模式 `LoginView` 作为 `UserControl` 嵌入主窗口
- 首次运行标记文件写入失败时，下次启动仍会弹出向导（设计为可接受的降级）
