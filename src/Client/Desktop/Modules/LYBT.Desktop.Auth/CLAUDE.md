# LYBT.Desktop.Auth CLAUDE.md

## 架构决策

- AuthenticationModule 是基础模块，无模块依赖 (其他模块如 PatientsModule 依赖它)
- 登录流程通过 ILoginCoordinator 编排，ViewModel 不直接调用 AuthApi
- 密码双向绑定通过 code-behind 手动同步 PasswordBox (WPF 安全限制，PasswordBox.Password 不支持数据绑定)
- 记住账号 (IUsernameStorageService) 和记住密码 (ICredentialVault/DPAPI) 为可选依赖
- LoginWindow 已弃用，当前使用单窗口模式 (LoginView 作为 UserControl 嵌入主窗口)
- 连接模式选择 (Remote/Local) 已预留 UI 入口，Local 模式尚未实现

## 代码文件结构

### 模块注册

| 文件 | 类 | 说明 |
|------|-----|------|
| AuthenticationModule.cs | `AuthenticationModule : IModule` | Prism 模块注册，注册 LoginViewModel 和 LoginView (导航)。Services 由 Core 层统一注册 |

### ViewModels/

| 文件 | 类 | 说明 |
|------|-----|------|
| LoginViewModel.cs | `LoginViewModel : NavigableViewModelBase` | 登录视图模型。属性: Username, Password, RememberUsername, RememberPassword, SelectedConnectionMode, IsRemoteMode, IsLocalMode, HasMessage, ApiStatus (ApiHealthStatus), ApiStatusMessage, IsApiUnhealthy。命令: LoginCommand (DelegateCommand, CanExecute 检查用户名/密码非空且非加载中), CloseApplicationCommand (确认后退出), RetryApiCheckCommand (重试 API 连接)。核心方法: ExecuteLoginAsync (调用 LoginCoordinator，成功后保存用户名/密码), LoadSavedCredentialsAsync (启动时加载已存凭证), LoadApiStatusFromStateServiceAsync, OnApiStatusChanged (事件驱动 API 状态更新), ClearSavedUsernameAsync, ClearSavedPasswordAsync |

### Views/

| 文件 | 类 | 说明 |
|------|-----|------|
| LoginView.xaml.cs | `LoginView : UserControl` | 登录视图 code-behind。PasswordBox 双向绑定: DataContextChanged/PropertyChanged 事件同步 ViewModel.Password <-> PasswordBox.Password (避免循环更新)。响应式布局: SizeChanged 事件处理宽度 <800px 时隐藏左侧品牌区 (适配 1080P) |
| LoginWindow.xaml.cs | `LoginWindow : Window` | 已弃用的登录窗口，保留仅为向后兼容，当前使用单窗口模式 LoginView |

## 死代码与废弃标记

| 类型 | 位置 | 状态 | 说明 |
|------|------|------|------|
| LoginWindow | Views/LoginWindow.xaml.cs | 已弃用 | 代码注释明确标记"已弃用，现在使用单窗口模式"。仅被自身 XAML 和 README 引用，无运行时消费者 |

## 已知陷阱

- PasswordBox.Password 不支持 WPF 数据绑定 (安全设计)，必须通过 code-behind 的 PasswordChanged 事件手动同步，且需防止循环更新
- LoginView 构造函数中 Prism 可能在 InitializeComponent 时就设置 DataContext (Issue #1246)，此时 DataContextChanged 不会触发，需在构造函数中手动处理
- RememberPassword 勾选时自动联动勾选 RememberUsername (取消 RememberPassword 不取消 RememberUsername)
- 切换用户名时，如果之前有已保存的密码，会自动清空 Password 字段
- ConnectionMode.Local 选择会被拦截并提示"功能开发中"，不会实际切换

## OpenSpec 追踪

| OpenSpec ID | 涉及文件 | 状态 |
|-------------|----------|------|
| simplify-login-options | LoginViewModel.cs | 记住账号+记住密码已实现，自动登录已移除 |
| refactor-startup-connection-resilience | LoginViewModel.cs | 事件驱动 API 状态更新，ConnectionMode 预留 |
| remove-secure-credential-storage | LoginViewModel.cs | 已移除废弃的 SecureCredentialStorage 依赖 |
| redesign-login-remember-password | LoginViewModel.cs | DPAPI CredentialVault 保存/加载密码已实现 |
| enhance-viewmodel-architecture | LoginViewModel.cs | 使用 IViewModelServices 聚合服务 |
| remove-titlebar-add-close-button | LoginViewModel.cs | CloseApplicationCommand 已实现 |
| remove-statusbar-relocate-status | LoginViewModel.cs | RetryApiCheckCommand 已实现 |

---
最后更新: 2026-03-01
