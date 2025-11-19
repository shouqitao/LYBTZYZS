# LYBT.Desktop.Auth - 认证授权模块

## 📦 项目定位

- **层级**:Client端
- **类型**:业务模块(认证授权)
- **职责**:提供用户登录界面和身份认证功能，负责JWT令牌的获取与管理，为整个WPF桌面应用提供会话管理和权限验证的基础。采用MVVM架构，通过Prism实现模块化和依赖注入，确保认证流程的安全性和用户体验。

## 📂 代码结构

```
LYBT.Desktop.Auth/
├── ViewModels/                         # MVVM视图模型层(1个)
│   └── LoginViewModel.cs               # 登录视图模型(9属性+7方法)
│       ├── 属性(9):Username, Password, RememberMe, RememberPassword,
│       │          HasSavedPassword, ApiStatus, ApiStatusMessage,
│       │          HasMessage, LoginCommand
│       └── 方法(7):构造函数, CheckApiHealthAsyncSafe,
│                  LoadSavedCredentialsAsync, CheckApiHealthAsync,
│                  CanExecuteLogin, ExecuteLoginAsync, NavigateBasedOnRole
├── Views/                              # WPF视图层(4个)
│   ├── LoginView.xaml                  # 登录视图(作为UserControl嵌入)
│   ├── LoginView.xaml.cs               # LoginView代码后置
│   ├── LoginWindow.xaml                # 登录窗口(独立Window)
│   └── LoginWindow.xaml.cs             # LoginWindow代码后置
├── AuthenticationModule.cs             # Prism模块定义(2个方法)
│   ├── OnInitialized()                 # 模块初始化
│   └── RegisterTypes()                 # 类型注册(Views + ViewModels)
├── LYBT.Desktop.Auth.csproj            # 项目配置文件
└── README.md                           # 本文档
```

**说明**:
- **LoginViewModel**:完整的登录逻辑，包含API健康检查、凭证存储、自动登录、记住密码、角色导航等9属性+7方法
- **Views**:提供UserControl(LoginView)和独立Window(LoginWindow)两种使用方式
- **AuthenticationModule**:Prism模块注册，将Views和ViewModels注册到DI容器

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Desktop.Foundation** - 平台无关基础服务(IAuthenticationService, ICacheService, IConfigurationService, ISecureCredentialStorage)
2. **LYBT.Desktop.Infrastructure** - WPF基础设施(CoreViewModel基类, Converters, PasswordBoxHelper)
3. **LYBT.Desktop.Contracts** - 共享契约(LoginRequest, LoginResponse, UserDto)
4. **LYBT.Shared.Models** - 跨端共享模型(用户模型、角色枚举)
5. **LYBT.Shared.Interfaces** - 跨端共享接口(IUserService)

### 被依赖项目
1. **LYBT.Desktop.Shell** - Shell加载此模块并导航到登录窗口
2. **其他业务模块** - 依赖认证模块提供的会话状态和用户信息

### NuGet包
- **Prism.DryIoc** (8.x) - MVVM框架和依赖注入容器
- **Microsoft.Extensions.Logging** (8.0.x) - 日志记录
- **MaterialDesignThemes** (5.1.x) - Material Design UI组件

## 🛠 技术栈

- **.NET 8**: 基础框架
- **WPF**: Windows Presentation Foundation UI框架
- **Prism.DryIoc 8.x**: MVVM框架、模块化、依赖注入、区域导航
- **MaterialDesignThemes 5.1.x**: Material Design风格UI组件库
- **System.Security.Cryptography**: DPAPI加密凭证存储
- **Microsoft.Extensions.Logging**: 结构化日志记录
- **异步编程**: async/await提升UI响应性

##  快速开始

此项目是一个Prism模块库，由 `LYBT.Desktop.Shell` 在启动时加载。无法独立运行。

```bash
# 构建此项目
dotnet build src/Client/Desktop/Modules/LYBT.Desktop.Auth/LYBT.Desktop.Auth.csproj
```

**集成说明**:

### 1. 在Shell中加载Auth模块
```csharp
// App.xaml.cs (Shell项目)
protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
{
    // 注册Auth模块(优先级最高，应用启动时加载)
    moduleCatalog.AddModule<AuthenticationModule>(InitializationMode.WhenAvailable);
}

protected override Window CreateShell()
{
    // 如果未登录，显示登录窗口
    var authService = Container.Resolve<IAuthenticationService>();
    if (!authService.IsLoggedInAsync().Result)
    {
        return Container.Resolve<LoginWindow>();
    }

    // 已登录，显示主窗口
    return Container.Resolve<MainWindow>();
}
```

### 2. LoginViewModel核心属性与方法

**完整接口表**（9属性+7方法）:

| 成员类型 | 名称 | 功能描述 |
|---------|------|---------|
| **绑定属性** (6) | | |
| | Username | 用户名（双向绑定） |
| | Password | 密码（双向绑定，安全字段） |
| | RememberMe | 记住用户名（CheckBox绑定） |
| | RememberPassword | 记住密码（CheckBox绑定） |
| | ApiStatus | API连接状态（Healthy/Degraded/Unhealthy） |
| | ApiStatusMessage | API状态消息（显示连接提示） |
| **只读属性** (2) | | |
| | HasSavedPassword | 是否存在保存的密码（控制UI显示） |
| | HasMessage | 是否有错误消息（控制MessageBar可见性） |
| **命令** (1) | | |
| | LoginCommand | 登录命令（绑定到登录按钮） |
| **方法** (7) | | |
| | 构造函数 | 初始化依赖服务、注册命令、触发健康检查和凭证加载 |
| | CheckApiHealthAsyncSafe | 安全的API健康检查（防止异常阻塞UI） |
| | LoadSavedCredentialsAsync | 加载保存的凭证（自动填充用户名/密码） |
| | CheckApiHealthAsync | 完整的API健康检查（更新状态和消息） |
| | CanExecuteLogin | 登录命令可执行条件（用户名和密码非空） |
| | ExecuteLoginAsync | 执行登录逻辑（调用AuthService、保存凭证、导航） |
| | NavigateBasedOnRole | 基于用户角色导航到对应模块（Admin/Doctor） |

### 3. 完整登录流程示例

```csharp
// LoginViewModel.cs
public class LoginViewModel : CoreViewModel
{
    private readonly IAuthenticationService _authService;
    private readonly ITokenStorageService _tokenStorage;
    private readonly IApiHealthCheckService _apiHealthCheck;
    private readonly ISecureCredentialStorage _credentialStorage;
    private readonly IUsernameStorage _usernameStorage;

    public LoginViewModel(
        IAuthenticationService authService,
        ITokenStorageService tokenStorage,
        IApiHealthCheckService apiHealthCheck,
        ISecureCredentialStorage credentialStorage,
        IUsernameStorage usernameStorage,
        IEventAggregator eventAggregator,
        IRegionManager regionManager)
        : base(eventAggregator, regionManager)
    {
        _authService = authService;
        _tokenStorage = tokenStorage;
        _apiHealthCheck = apiHealthCheck;
        _credentialStorage = credentialStorage;
        _usernameStorage = usernameStorage;

        // 注册登录命令（带CanExecute检查）
        LoginCommand = new DelegateCommand(
            async () => await ExecuteLoginAsync(),
            CanExecuteLogin
        ).ObservesProperty(() => Username)
         .ObservesProperty(() => Password);

        // 启动时执行：健康检查 + 加载保存的凭证
        _ = CheckApiHealthAsyncSafe();
        _ = LoadSavedCredentialsAsync();
    }

    private async Task ExecuteLoginAsync()
    {
        try
        {
            IsBusy = true;
            ClearMessage();

            // Step 1: 验证输入
            if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
            {
                SetErrorMessage("用户名和密码不能为空");
                return;
            }

            // Step 2: 调用认证服务登录
            var result = await _authService.LoginAsync(new LoginRequest
            {
                Username = Username.Trim(),
                Password = Password
            });

            if (!result.IsSuccess)
            {
                SetErrorMessage(result.ErrorMessage ?? "登录失败，请检查用户名和密码");
                return;
            }

            // Step 3: 保存JWT令牌
            await _tokenStorage.SaveTokenAsync(result.Data.Token);

            // Step 4: 保存凭证（如果用户勾选）
            if (RememberMe)
            {
                _usernameStorage.SaveUsername(Username);
            }
            else
            {
                _usernameStorage.ClearUsername();
            }

            if (RememberPassword)
            {
                _credentialStorage.SavePassword(Username, Password);
            }
            else
            {
                _credentialStorage.DeletePassword(Username);
            }

            // Step 5: 发布用户登录事件（通知其他模块）
            EventAggregator.GetEvent<UserLoggedInEvent>().Publish(result.Data.User);

            // Step 6: 记录登录成功日志
            _logger.LogInformation("用户 {Username} 登录成功，角色: {Role}",
                Username, result.Data.User.Role);

            // Step 7: 基于用户角色导航到对应模块
            NavigateBasedOnRole(result.Data.User.Role);

            // Step 8: 关闭登录窗口（如果是独立窗口）
            if (Application.Current.MainWindow is LoginWindow loginWindow)
            {
                loginWindow.Close();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登录过程中发生异常");
            SetErrorMessage("登录失败: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanExecuteLogin()
    {
        return !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(Password);
    }

    private void NavigateBasedOnRole(UserRole role)
    {
        switch (role)
        {
            case UserRole.Admin:
                RegionManager.RequestNavigate("ContentRegion", "UsersView");
                break;
            case UserRole.Doctor:
                RegionManager.RequestNavigate("ContentRegion", "PatientsView");
                break;
            default:
                _logger.LogWarning("未知的用户角色: {Role}", role);
                SetErrorMessage("用户角色配置错误，请联系管理员");
                break;
        }
    }
}
```

### 4. API健康检查示例

```csharp
// LoginViewModel.cs
private async Task CheckApiHealthAsync()
{
    try
    {
        _logger.LogInformation("开始检查API健康状态...");

        var result = await _apiHealthCheck.CheckHealthAsync();

        if (result.IsSuccess)
        {
            var healthStatus = result.Data;
            ApiStatus = healthStatus.Status;

            switch (healthStatus.Status)
            {
                case ApiHealthStatus.Healthy:
                    ApiStatusMessage = " API连接正常";
                    _logger.LogInformation("API健康检查成功");
                    break;
                case ApiHealthStatus.Degraded:
                    ApiStatusMessage = "⚠️ API连接不稳定";
                    _logger.LogWarning("API处于降级状态");
                    break;
                case ApiHealthStatus.Unhealthy:
                    ApiStatusMessage = "❌ API连接失败";
                    _logger.LogError("API健康检查失败");
                    break;
            }
        }
        else
        {
            ApiStatus = ApiHealthStatus.Unhealthy;
            ApiStatusMessage = $"❌ 无法连接到服务器: {result.ErrorMessage}";
            _logger.LogError("API健康检查失败: {Error}", result.ErrorMessage);
        }
    }
    catch (Exception ex)
    {
        ApiStatus = ApiHealthStatus.Unhealthy;
        ApiStatusMessage = $"❌ 健康检查异常: {ex.Message}";
        _logger.LogError(ex, "API健康检查过程中发生异常");
    }
}

// 启动时安全的健康检查（不阻塞UI）
private async Task CheckApiHealthAsyncSafe()
{
    try
    {
        await CheckApiHealthAsync();
    }
    catch
    {
        // 吞掉异常，避免阻塞UI初始化
    }
}
```

### 5. 记住密码功能示例

```csharp
// LoginViewModel.cs
private async Task LoadSavedCredentialsAsync()
{
    try
    {
        // Step 1: 加载用户名
        var savedUsername = _usernameStorage.GetSavedUsername();
        if (!string.IsNullOrWhiteSpace(savedUsername))
        {
            Username = savedUsername;
            RememberMe = true;
            _logger.LogInformation("加载保存的用户名: {Username}", savedUsername);
        }

        // Step 2: 加载密码
        if (!string.IsNullOrWhiteSpace(Username))
        {
            var savedPassword = _credentialStorage.GetPassword(Username);
            if (!string.IsNullOrWhiteSpace(savedPassword))
            {
                Password = savedPassword;
                RememberPassword = true;
                HasSavedPassword = true;
                _logger.LogInformation("加载保存的密码（用户: {Username}）", Username);
            }
            else
            {
                HasSavedPassword = false;
            }
        }
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "加载保存的凭证失败");
        HasSavedPassword = false;
    }
}

// RememberPassword属性变化时保存/删除密码
public bool RememberPassword
{
    get => _rememberPassword;
    set
    {
        if (SetProperty(ref _rememberPassword, value))
        {
            if (value && !string.IsNullOrWhiteSpace(Username) && !string.IsNullOrWhiteSpace(Password))
            {
                // 保存密码
                _credentialStorage.SavePassword(Username, Password);
                _logger.LogInformation("密码已保存（用户: {Username}）", Username);
            }
            else if (!value && !string.IsNullOrWhiteSpace(Username))
            {
                // 删除密码
                _credentialStorage.DeletePassword(Username);
                _logger.LogInformation("密码已删除（用户: {Username}）", Username);
            }
        }
    }
}
```

### 6. LoginView XAML绑定示例

```xml
<!-- LoginView.xaml -->
<UserControl x:Class="LYBT.Desktop.Auth.Views.LoginView"
             xmlns:materialDesign="http://materialdesigninxaml.net/winfx/xaml/themes"
             prism:ViewModelLocator.AutoWireViewModel="True">

    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/> <!-- API状态栏 -->
            <RowDefinition Height="*"/>    <!-- 登录表单 -->
        </Grid.RowDefinitions>

        <!-- API连接状态栏 -->
        <Border Grid.Row="0" Background="{Binding ApiStatus, Converter={StaticResource ApiStatusToBrushConverter}}"
                Padding="10" Visibility="{Binding HasMessage, Converter={StaticResource BoolToVisibilityConverter}}">
            <TextBlock Text="{Binding ApiStatusMessage}" Foreground="White" FontWeight="Medium"/>
        </Border>

        <!-- 登录表单 -->
        <materialDesign:Card Grid.Row="1" Padding="32" Margin="16">
            <StackPanel>
                <!-- Logo和标题 -->
                <TextBlock Text="凌隐宝堂中医诊所" Style="{StaticResource MaterialDesignHeadline4TextBlock}"
                           HorizontalAlignment="Center" Margin="0,0,0,24"/>

                <!-- 用户名输入框 -->
                <TextBox materialDesign:HintAssist.Hint="用户名"
                         Text="{Binding Username, UpdateSourceTrigger=PropertyChanged}"
                         Margin="0,8"/>

                <!-- 密码输入框（使用PasswordBoxHelper绑定） -->
                <PasswordBox materialDesign:HintAssist.Hint="密码"
                             helpers:PasswordBoxHelper.BoundPassword="{Binding Password, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"
                             Margin="0,8"/>

                <!-- 记住选项 -->
                <StackPanel Orientation="Horizontal" Margin="0,16,0,0">
                    <CheckBox Content="记住用户名" IsChecked="{Binding RememberMe}" Margin="0,0,16,0"/>
                    <CheckBox Content="记住密码" IsChecked="{Binding RememberPassword}"/>
                </StackPanel>

                <!-- 登录按钮 -->
                <Button Content="登录" Command="{Binding LoginCommand}"
                        Style="{StaticResource MaterialDesignRaisedButton}"
                        Margin="0,24,0,0" Height="40"/>

                <!-- 错误消息 -->
                <TextBlock Text="{Binding ErrorMessage}" Foreground="Red"
                           Visibility="{Binding HasMessage, Converter={StaticResource BoolToVisibilityConverter}}"
                           Margin="0,16,0,0" TextWrapping="Wrap"/>
            </StackPanel>
        </materialDesign:Card>
    </Grid>
</UserControl>
```

### 7. AuthenticationModule注册

```csharp
// AuthenticationModule.cs
public class AuthenticationModule : IModule
{
    private readonly IRegionManager _regionManager;

    public AuthenticationModule(IRegionManager regionManager)
    {
        _regionManager = regionManager;
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化时的逻辑（如需要）
        _regionManager.RegisterViewWithRegion("LoginRegion", typeof(LoginView));
    }

    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册Views（用于导航）
        containerRegistry.RegisterForNavigation<LoginView>();
        containerRegistry.RegisterForNavigation<LoginWindow>();

        // 注册ViewModels（自动绑定到Views）
        containerRegistry.Register<LoginViewModel>();
    }
}
```

## 🎨 模块架构图

```
┌─────────────────────────────────────────────────────────────┐
│                     LYBT.Desktop.Auth                       │
│                    (认证授权模块)                            │
├─────────────────────────────────────────────────────────────┤
│                                                             │
│  ┌─────────────────────────────────────────────────────┐  │
│  │            Views (WPF视图层)                         │  │
│  │  ┌─────────────────┐    ┌─────────────────┐        │  │
│  │  │  LoginView      │    │  LoginWindow    │        │  │
│  │  │  (UserControl)  │    │  (Window)       │        │  │
│  │  └────────┬────────┘    └────────┬────────┘        │  │
│  │           │ DataContext          │ DataContext     │  │
│  └───────────┼──────────────────────┼─────────────────┘  │
│              │                      │                     │
│  ┌───────────┴──────────────────────┴─────────────────┐  │
│  │         ViewModels (业务逻辑层)                      │  │
│  │  ┌───────────────────────────────────────────────┐ │  │
│  │  │          LoginViewModel                       │ │  │
│  │  │  ─────────────────────────────────────────   │ │  │
│  │  │  属性: Username, Password, RememberMe        │ │  │
│  │  │        ApiStatus, LoginCommand               │ │  │
│  │  │  ─────────────────────────────────────────   │ │  │
│  │  │  方法: ExecuteLoginAsync()                   │ │  │
│  │  │        CheckApiHealthAsync()                 │ │  │
│  │  │        LoadSavedCredentialsAsync()           │ │  │
│  │  │        NavigateBasedOnRole()                 │ │  │
│  │  └────────────┬───────────────┬─────────────────┘ │  │
│  └───────────────┼───────────────┼───────────────────┘  │
│                  │               │                       │
└──────────────────┼───────────────┼───────────────────────┘
                   │               │
                   ▼               ▼
      ┌────────────────────────────────────────┐
      │   LYBT.Desktop.Foundation (依赖服务)   │
      ├────────────────────────────────────────┤
      │  • IAuthenticationService             │
      │  • ITokenStorageService               │
      │  • ISecureCredentialStorage           │
      │  • IApiHealthCheckService             │
      └────────────────────────────────────────┘
                   │
                   ▼
      ┌────────────────────────────────────────┐
      │    LYBT.WebAPI (后端认证服务)          │
      ├────────────────────────────────────────┤
      │  POST /api/v1/auth/login              │
      │  POST /api/v1/auth/logout             │
      │  GET  /api/v1/health                  │
      └────────────────────────────────────────┘
```

## 🎯 设计原则

### 1. MVVM架构严格遵循

**原则**：视图与业务逻辑完全分离，所有UI状态和操作通过ViewModel暴露。

**实现**：
- **View层**：LoginView.xaml仅包含XAML标记和数据绑定，无业务逻辑代码
- **ViewModel层**：LoginViewModel包含所有登录逻辑、状态管理、命令处理
- **数据绑定**：Username/Password/RememberMe等属性双向绑定
- **命令模式**：LoginCommand绑定到Button，使用CanExecute自动控制可用性
- **PasswordBox绑定**：通过PasswordBoxHelper实现密码字段的MVVM绑定（解决WPF原生限制）

**反面案例（禁止）**：
```csharp
// ❌ 错误：在View代码后置中处理登录逻辑
private void LoginButton_Click(object sender, RoutedEventArgs e)
{
    var authService = ServiceLocator.Current.GetInstance<IAuthService>();
    authService.LoginAsync(Username, Password);
}
```

### 2. 安全性优先

**密码存储安全**：
- 使用Windows DPAPI加密存储密码（ISecureCredentialStorage）
- 密码仅在用户明确勾选"记住密码"时保存
- 登录失败不暴露敏感信息（统一返回"用户名或密码错误"）

**JWT令牌管理**：
- 令牌存储在内存（ITokenStorageService）
- 应用关闭时自动清除令牌（除非用户勾选"记住我"）
- 所有API请求自动附加Bearer Token

**日志记录**：
- 记录登录成功/失败事件（不记录密码）
- 记录用户角色和会话信息
- 记录API健康检查结果

### 3. 用户体验优化

**启动时体验**：
- 自动执行API健康检查，显示连接状态（ 正常 / ⚠️ 不稳定 / ❌ 失败）
- 自动加载保存的用户名和密码（如果用户曾勾选记住）
- 健康检查异常不阻塞UI显示（CheckApiHealthAsyncSafe）

**登录过程体验**：
- IsBusy状态显示Loading动画（防止重复点击）
- 输入验证实时反馈（用户名/密码为空时禁用登录按钮）
- 友好的错误提示（"用户名和密码不能为空" 而非 "Validation Failed"）
- 登录成功后自动导航到对应模块（Admin→用户管理，Doctor→患者管理）

**记住功能**：
- "记住用户名"：下次启动自动填充用户名
- "记住密码"：下次启动自动填充密码（DPAPI加密存储）
- HasSavedPassword属性控制UI提示（"已保存密码"）

### 4. 事件驱动通信

**Prism EventAggregator模式**：
- 登录成功后发布`UserLoggedInEvent`，通知其他模块更新状态
- 其他模块订阅事件，无需直接依赖Auth模块
- 解耦模块间通信，提升可维护性

```csharp
// 发布登录事件
EventAggregator.GetEvent<UserLoggedInEvent>().Publish(currentUser);

// 其他模块订阅事件（如Users模块）
EventAggregator.GetEvent<UserLoggedInEvent>().Subscribe(user =>
{
    CurrentUser = user;
    LoadUserPermissions(user.Role);
});
```

### 5. 异步优先与防阻塞

**所有I/O操作异步化**：
- `ExecuteLoginAsync`：登录API调用
- `CheckApiHealthAsync`：健康检查API调用
- `LoadSavedCredentialsAsync`：凭证加载（可能涉及磁盘I/O）

**防阻塞策略**：
- 构造函数中使用`_ = CheckApiHealthAsyncSafe()`异步触发（不阻塞构造完成）
- 健康检查异常不影响UI初始化（try-catch吞掉异常）
- IsBusy状态控制登录按钮可用性（防止重复点击）

### 6. 依赖注入与可测试性

**构造函数注入**：
- 所有依赖服务通过构造函数注入（IAuthenticationService, ITokenStorageService等）
- 避免ServiceLocator反模式
- 便于单元测试（Mock依赖服务）

**单元测试友好**：
```csharp
// 单元测试示例
[Fact]
public async Task ExecuteLoginAsync_ValidCredentials_ShouldNavigateToCorrectView()
{
    // Arrange
    var mockAuthService = new Mock<IAuthenticationService>();
    mockAuthService.Setup(s => s.LoginAsync(It.IsAny<LoginRequest>()))
        .ReturnsAsync(ServiceResult<LoginResponse>.Success(new LoginResponse
        {
            User = new UserDto { Role = UserRole.Doctor },
            Token = "fake-jwt-token"
        }));

    var viewModel = new LoginViewModel(
        mockAuthService.Object,
        Mock.Of<ITokenStorageService>(),
        Mock.Of<IApiHealthCheckService>(),
        Mock.Of<ISecureCredentialStorage>(),
        Mock.Of<IUsernameStorage>(),
        Mock.Of<IEventAggregator>(),
        Mock.Of<IRegionManager>()
    );

    viewModel.Username = "doctor1";
    viewModel.Password = "password";

    // Act
    await viewModel.ExecuteLoginAsync();

    // Assert
    mockAuthService.Verify(s => s.LoginAsync(It.IsAny<LoginRequest>()), Times.Once);
    // 验证导航到PatientsView
}
```

## 📚 详细文档

- **完整模块文档**:[docs/reference/modules/auth/](../../../../docs/reference/modules/auth/) *(待创建)*
- **架构设计**:[docs/explanation/architecture/client/auth-design.md](../../../../docs/explanation/architecture/client/auth-design.md) *(待创建)*
- **开发指南**:[docs/how-to-guides/client/auth-development.md](../../../../docs/how-to-guides/client/auth-development.md) *(待创建)*

---

**最后更新**:2025-10-29
**维护负责**:Client端开发组
