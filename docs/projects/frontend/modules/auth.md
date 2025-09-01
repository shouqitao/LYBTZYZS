# LYBT.Desktop.Auth - 身份认证模块

## 📋 项目概览

**项目名称**: LYBT.Desktop.Auth  
**项目类型**: WPF 模块化业务组件  
**技术栈**: .NET 8.0, WPF, Prism.DryIoc 9.0.537  
**架构模式**: MVVM + Prism 模块化架构  
**业务职责**: 用户身份认证、登录管理、会话控制、凭证管理

### 核心功能

1. **用户登录认证** - JWT Bearer Token认证机制
2. **会话管理** - Token验证、自动续期、会话状态跟踪  
3. **凭证管理** - 用户名密码安全存储、记住我功能
4. **API连接监控** - 实时监控API服务可用性
5. **认证状态广播** - 通过事件聚合器通知系统认证状态变更

### 依赖关系

- **Desktop.Core** - 基础控件和设计系统
- **Desktop.Infrastructure** - 认证服务接口和基础设施
- **Desktop.Services** - API客户端和缓存服务
- **Shared.Models** - 认证相关DTO模型
- **第三方依赖**: Prism.DryIoc 9.0.537, AutoMapper, BCrypt.Net

## 🏗️ 项目架构

### 目录结构

```
LYBT.Desktop.Auth/
├── Api/                     # API接口定义 (空目录，使用Infrastructure层接口)
├── Mappings/               # AutoMapper映射配置
│   └── MappingProfile.cs   # DTO映射配置
├── Services/               # 业务服务层
│   └── AuthModule.cs       # 核心认证模块服务
├── ViewModels/             # MVVM视图模型
│   └── LoginViewModel.cs   # 登录界面视图模型
├── Views/                  # WPF视图界面
│   ├── LoginView.xaml      # 登录用户控件
│   ├── LoginView.xaml.cs   # 登录控件代码隐藏
│   ├── LoginWindow.xaml    # 独立登录窗口
│   └── LoginWindow.xaml.cs # 登录窗口代码隐藏
├── AuthenticationModule.cs # Prism模块注册
└── LYBT.Desktop.Auth.csproj
```

### 架构模式

#### 1. Prism模块化架构
```csharp
// AuthenticationModule.cs - 模块注册
public class AuthenticationModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // UltraThink模块化架构：注册模块业务服务
        containerRegistry.RegisterSingleton<AuthModule>();
        
        // 注册视图模型
        containerRegistry.Register<LoginViewModel>();

        // 注册视图用于导航
        containerRegistry.RegisterForNavigation<LoginView>();
    }

    public void OnInitialized(IContainerProvider containerProvider)
    {
        // 模块初始化完成
        var logger = containerProvider.Resolve<ILogger<AuthenticationModule>>();
        logger?.LogInformation("Auth模块初始化完成 - UltraThink架构");
    }
}
```

#### 2. MVVM + 模块服务架构
```csharp
// LoginViewModel.cs - 视图模型核心实现
public class LoginViewModel : ModernViewModelBase
{
    private readonly AuthModule _authModule;
    private readonly IMapper _mapper;
    
    // UltraThink四层架构：使用模块化服务执行登录
    private async Task ExecuteLoginAsync()
    {
        var result = await _authModule.LoginAsync(LoginRequest);
        
        if (result.IsSuccess && result.Data != null)
        {
            // 通过事件总线通知登录成功
            EventAggregator.GetEvent<LoginSuccessEvent>().Publish();
        }
        else
        {
            SetError(result.ErrorMessage ?? "登录失败，请检查用户名和密码");
        }
    }
}
```

#### 3. 综合业务模块服务
```csharp
// AuthModule.cs - 核心认证服务
public class AuthModule : IAuthenticationService, IDisposable
{
    private readonly IAuthApi _authApi;
    private readonly ITokenManager _tokenManager;
    private readonly SecureCredentialService _credentialService;
    
    // 认证状态事件
    public event EventHandler<(bool IsLoggedIn, string? Username, string? Message)>? AuthStatusChanged;
    public event EventHandler<(bool IsConnected, string Message)>? ApiConnectionChanged;
    
    // 核心登录方法
    public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest loginRequest)
    {
        // UltraThink统一架构: 直接调用API服务，移除中间服务层
        var apiResponse = await _authApi.LoginAsync(loginRequest);
        
        if (apiResponse.Success && apiResponse.Data != null)
        {
            var loginResponse = apiResponse.Data;
            
            // 更新认证状态和缓存
            _isAuthenticated = true;
            _currentUser = loginResponse.User;
            _tokenManager.SetToken(loginResponse.Token);
            
            // 触发事件
            OnAuthStatusChanged(true, loginResponse.User.Username, "登录成功");
            
            return ServiceResult<LoginResponse>.Success(loginResponse);
        }
        
        return ServiceResult<LoginResponse>.Failure(apiResponse.Message ?? "登录失败");
    }
}
```

## 🔧 核心组件

### 1. AuthModule (核心业务服务)

#### 主要功能
- **身份认证**: JWT登录、登出、Token管理
- **会话管理**: Token验证、会话状态跟踪  
- **凭证管理**: 安全存储用户凭证、记住我功能
- **连接监控**: 实时监控API服务可用性
- **事件通知**: 认证状态变更广播

#### 关键方法
```csharp
// 用户登录认证
Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest loginRequest)

// 用户登出
Task<ServiceResult> LogoutAsync()

// 验证Token有效性
Task<ServiceResult<bool>> ValidateTokenAsync()

// 获取当前用户信息
Task<UserDto?> GetCurrentUserAsync()

// API连接状态检查
Task<ServiceResult<bool>> CheckApiConnectionAsync()

// 凭证管理
ServiceResult SaveCredentials(string username, string password, bool rememberMe)
ServiceResult<LoginRequest?> LoadSavedCredentials()
```

#### 事件系统
```csharp
// 认证状态变更事件
public event EventHandler<(bool IsLoggedIn, string? Username, string? Message)>? AuthStatusChanged;

// API连接状态变更事件  
public event EventHandler<(bool IsConnected, string Message)>? ApiConnectionChanged;
```

### 2. LoginViewModel (登录视图模型)

#### 核心属性
```csharp
public class LoginViewModel : ModernViewModelBase
{
    // 登录请求模型
    public LoginRequest LoginRequest { get; set; }
    
    // 数据绑定属性
    public string Username { get; set; }
    public string Password { get; set; }
    public bool RememberMe { get; set; }
    
    // 状态属性
    public bool IsApiOnline { get; set; }
    public string ApiStatus { get; set; }
    public bool HasSavedPassword { get; set; }
    
    // 命令
    public DelegateCommand LoginCommand { get; }
    public DelegateCommand<PasswordBox> PasswordChangedCommand { get; }
}
```

#### 数据绑定和命令处理
```csharp
// 登录命令执行
private async Task ExecuteLoginAsync()
{
    var success = await ExecuteAsync(async () =>
    {
        // UltraThink四层架构：使用模块化服务执行登录
        var result = await _authModule.LoginAsync(LoginRequest);

        if (result.IsSuccess && result.Data != null)
        {
            // 设置状态消息
            SetStatus("登录成功，正在跳转...");
            
            // 等待一下让用户看到成功消息
            await Task.Delay(1000);

            // 通过事件总线通知登录成功
            EventAggregator.GetEvent<LoginSuccessEvent>().Publish();
        }
        else
        {
            SetError(result.ErrorMessage ?? "登录失败，请检查用户名和密码");
        }
    }, "登录");
}

// 命令可执行性检查
private bool CanExecuteLogin()
{
    return !IsLoading && IsApiOnline && 
           !string.IsNullOrWhiteSpace(LoginRequest.Username) && 
           !string.IsNullOrWhiteSpace(LoginRequest.Password);
}
```

### 3. LoginView (登录界面)

#### XAML布局特点
```xml
<UserControl x:Class="LYBT.Desktop.Auth.Views.LoginView"
             xmlns:prism="http://prismlibrary.com/"
             prism:ViewModelLocator.AutoWireViewModel="True">
             
    <!-- 登录内容，移除外层边框，因为主窗口已经有了 -->
    <Grid Margin="25">
        <!-- 标题Logo -->
        <StackPanel Grid.Row="0" HorizontalAlignment="Center">
            <TextBlock Text="凌隐宝堂中医诊所" 
                       FontSize="20" 
                       FontWeight="Bold" 
                       Foreground="#2E86AB"/>
            <TextBlock Text="管理系统" 
                       FontSize="14" 
                       Foreground="#666"/>
        </StackPanel>

        <!-- 用户名输入 -->
        <TextBox Text="{Binding Username, UpdateSourceTrigger=PropertyChanged}" 
                 Height="40" 
                 FontSize="13" 
                 Padding="12,6"
                 BorderBrush="#DDD"
                 BorderThickness="1"/>
        
        <!-- 密码输入 -->
        <PasswordBox Height="40"
                     FontSize="13" 
                     Padding="12,6"
                     BorderBrush="#DDD">
            <i:Interaction.Triggers>
                <i:EventTrigger EventName="PasswordChanged">
                    <prism:InvokeCommandAction Command="{Binding PasswordChangedCommand}" 
                                               CommandParameter="{Binding RelativeSource={RelativeSource AncestorType=PasswordBox}}"/>
                </i:EventTrigger>
            </i:Interaction.Triggers>
        </PasswordBox>

        <!-- 记住我选项 -->
        <CheckBox IsChecked="{Binding RememberMe}" 
                  Content="记住我" 
                  FontSize="12"/>

        <!-- 登录按钮 -->
        <Button Command="{Binding LoginCommand}"
                Content="登录"
                Height="40"
                FontSize="14"
                Background="#2E86AB"
                Foreground="White"/>
                
        <!-- API状态显示 -->
        <TextBlock Text="{Binding ApiStatus}" 
                   FontSize="11" 
                   Foreground="#666"
                   HorizontalAlignment="Center"/>
    </Grid>
</UserControl>
```

### 4. MappingProfile (映射配置)

#### AutoMapper配置
```csharp
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // 登录相关映射 - 目前使用共享DTO，映射相对简单
        CreateMap<LoginRequest, LoginRequest>()
            .ReverseMap();
            
        CreateMap<LoginResponse, LoginResponse>()
            .ReverseMap();
            
        CreateMap<UserDto, UserDto>()
            .ReverseMap();
    }
}
```

## 🔐 安全特性

### 1. 凭证安全存储
```csharp
// 使用SecureCredentialService安全存储
public ServiceResult SaveCredentials(string username, string password, bool rememberMe)
{
    try
    {
        _credentialService.SaveCredentials(username, password, rememberMe);
        _logger.LogInformation("保存用户凭证成功: {Username}", username);
        return ServiceResult.Success();
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "保存用户凭证异常: {Username}", username);
        return ServiceResult.Failure($"保存凭证失败: {ex.Message}");
    }
}
```

### 2. 输入验证
```csharp
// DTO验证方法
public ServiceResult ValidateLoginRequest(LoginRequest loginRequest)
{
    if (loginRequest == null)
        return ServiceResult.Failure("登录信息不能为空");

    if (string.IsNullOrWhiteSpace(loginRequest.Username))
        return ServiceResult.Failure("用户名不能为空");

    if (loginRequest.Username.Length < 3 || loginRequest.Username.Length > 32)
        return ServiceResult.Failure("用户名长度必须在3到32个字符之间");

    if (string.IsNullOrWhiteSpace(loginRequest.Password))
        return ServiceResult.Failure("密码不能为空");

    if (loginRequest.Password.Length < 6)
        return ServiceResult.Failure("密码长度不能少于6个字符");

    return ServiceResult.Success();
}
```

### 3. JWT Token管理
```csharp
// Token生命周期管理
public async Task<ServiceResult<bool>> ValidateTokenAsync()
{
    try
    {
        // 检查Token是否存在
        var token = GetToken();
        if (string.IsNullOrEmpty(token))
        {
            return ServiceResult<bool>.Success(false);
        }

        // 尝试获取当前用户来验证Token有效性
        var user = await GetCurrentUserAsync();
        return ServiceResult<bool>.Success(user != null);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "验证Token异常");
        return ServiceResult<bool>.Failure($"验证Token失败: {ex.Message}");
    }
}
```

## 📡 事件系统

### 1. 认证事件
```csharp
// 登录成功事件
EventAggregator.GetEvent<LoginSuccessEvent>().Publish();

// 登出事件监听
EventAggregator.GetEvent<LogoutEvent>().Subscribe(OnLogout, ThreadOption.UIThread);

// 模块服务事件订阅
_authModule.AuthStatusChanged += OnAuthStatusChanged;
_authModule.ApiConnectionChanged += OnApiConnectionChanged;
```

### 2. 状态变更广播
```csharp
// 认证状态变更事件处理
private void OnAuthStatusChanged(object? sender, (bool IsLoggedIn, string? Username, string? Message) e)
{
    try
    {
        // 在UI线程上更新状态
        if (Application.Current?.Dispatcher != null)
        {
            if (Application.Current.Dispatcher.CheckAccess())
            {
                UpdateAuthStatus(e);
            }
            else
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() => UpdateAuthStatus(e)));
            }
        }
    }
    catch (Exception ex)
    {
        _ = HandleErrorAsync("认证状态更新", ex, false);
    }
}
```

## 🔄 生命周期管理

### 1. 模块初始化
```csharp
public void OnInitialized(IContainerProvider containerProvider)
{
    // 模块初始化完成
    var logger = containerProvider.Resolve<ILogger<AuthenticationModule>>();
    logger?.LogInformation("Auth模块初始化完成 - UltraThink架构");
}
```

### 2. 服务启动
```csharp
public AuthModule(/* 依赖注入参数 */)
{
    // 依赖注入初始化
    _authApi = authApi ?? throw new ArgumentNullException(nameof(authApi));
    _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));
    
    // 初始化API状态
    _currentApiStatus = new
    {
        IsOnline = false,
        StatusMessage = "正在检测API连接...",
        LastCheckTime = DateTime.Now,
        ResponseTime = (TimeSpan?)null
    };

    // 启动API连接监控
    StartApiConnectionMonitoring();
}
```

### 3. 资源清理
```csharp
// LoginViewModel资源清理
protected override void OnDisposing()
{
    // 取消事件订阅
    if (_authModule != null)
    {
        _authModule.AuthStatusChanged -= OnAuthStatusChanged;
        _authModule.ApiConnectionChanged -= OnApiConnectionChanged;
    }

    base.OnDisposing();
}

// AuthModule资源清理
public void Dispose()
{
    if (_disposed) return;

    StopApiConnectionMonitoring();
    _disposed = true;
}
```

## 🔧 依赖注入配置

### 1. 模块注册
```csharp
// AuthenticationModule.cs
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // UltraThink模块化架构：注册模块业务服务
    containerRegistry.RegisterSingleton<AuthModule>();
    
    // 注册视图模型
    containerRegistry.Register<LoginViewModel>();

    // 注册视图用于导航
    containerRegistry.RegisterForNavigation<LoginView>();
}
```

### 2. 服务依赖
```csharp
// AuthModule构造函数依赖
public AuthModule(
    IAuthApi authApi,                           // API客户端 (来自Desktop.Services)
    ITokenManager tokenManager,                 // Token管理 (来自Desktop.Infrastructure)  
    SecureCredentialService credentialService,  // 凭证服务 (来自Desktop.Infrastructure)
    IMapper mapper,                            // 对象映射 (AutoMapper)
    ILogger<AuthModule> logger)                // 日志服务 (Microsoft.Extensions.Logging)
```

### 3. ViewModel依赖
```csharp
// LoginViewModel构造函数依赖
public LoginViewModel(
    IEventAggregator eventAggregator,      // 事件聚合器 (Prism)
    AuthModule authModule,                 // 认证模块服务
    IMapper mapper,                        // 对象映射
    IErrorHandlingService? errorHandlingService = null) // 错误处理服务 (可选)
```

## 📊 性能特性

### 1. API连接监控
```csharp
public void StartApiConnectionMonitoring()
{
    if (_isMonitoring) return;

    lock (_lockObject)
    {
        if (_isMonitoring) return;

        _isMonitoring = true;
        
        // 立即执行一次检测
        _ = Task.Run(async () => await CheckApiConnectionAsync());

        // 设置定时器，每5秒检测一次
        _apiCheckTimer = new Timer(
            async _ => await CheckApiConnectionAsync(),
            null,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromSeconds(5));

        _logger.LogInformation("API连接监控已启动");
    }
}
```

### 2. 异步操作
```csharp
// 异步登录处理
private async Task ExecuteLoginAsync()
{
    var success = await ExecuteAsync(async () =>
    {
        // 异步调用认证服务
        var result = await _authModule.LoginAsync(LoginRequest);
        
        if (result.IsSuccess && result.Data != null)
        {
            SetStatus("登录成功，正在跳转...");
            
            // 非阻塞等待，提升用户体验
            await Task.Delay(1000);
            
            EventAggregator.GetEvent<LoginSuccessEvent>().Publish();
        }
        else
        {
            SetError(result.ErrorMessage ?? "登录失败，请检查用户名和密码");
        }
    }, "登录");
}
```

### 3. 缓存机制
```csharp
// 用户状态缓存
private LoginResponse? _currentLoginResponse;
private UserDto? _currentUser;
private bool _isAuthenticated;

// 快速状态检查，避免重复API调用
public bool IsLoggedIn => _isAuthenticated && _currentUser != null;
```

## 🧪 测试支持

### 1. 单元测试结构
```csharp
// 推荐测试结构
[TestClass]
public class AuthModuleTests
{
    private Mock<IAuthApi> _mockAuthApi;
    private Mock<ITokenManager> _mockTokenManager;
    private Mock<SecureCredentialService> _mockCredentialService;
    private AuthModule _authModule;

    [TestInitialize]
    public void Setup()
    {
        _mockAuthApi = new Mock<IAuthApi>();
        _mockTokenManager = new Mock<ITokenManager>();
        _mockCredentialService = new Mock<SecureCredentialService>();
        
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<MappingProfile>()).CreateMapper();
        var logger = new Mock<ILogger<AuthModule>>().Object;
        
        _authModule = new AuthModule(
            _mockAuthApi.Object,
            _mockTokenManager.Object,
            _mockCredentialService.Object,
            mapper,
            logger);
    }

    [TestMethod]
    public async Task LoginAsync_ValidCredentials_ReturnsSuccess()
    {
        // Arrange
        var loginRequest = new LoginRequest 
        { 
            Username = "testuser", 
            Password = "password123" 
        };
        
        var expectedResponse = new ApiResponse<LoginResponse>
        {
            Success = true,
            Data = new LoginResponse
            {
                Token = "jwt-token",
                User = new UserDto { Username = "testuser" }
            }
        };
        
        _mockAuthApi.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
                   .ReturnsAsync(expectedResponse);

        // Act
        var result = await _authModule.LoginAsync(loginRequest);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.IsNotNull(result.Data);
        Assert.AreEqual("testuser", result.Data.User.Username);
    }
}
```

### 2. UI测试支持
```csharp
// LoginViewModel测试示例
[TestClass]
public class LoginViewModelTests
{
    private Mock<IEventAggregator> _mockEventAggregator;
    private Mock<AuthModule> _mockAuthModule;
    private LoginViewModel _viewModel;

    [TestMethod]
    public void CanExecuteLogin_WithValidInput_ReturnsTrue()
    {
        // Arrange
        _viewModel.Username = "testuser";
        _viewModel.Password = "password123";
        _viewModel.IsApiOnline = true;

        // Act
        var canExecute = _viewModel.LoginCommand.CanExecute();

        // Assert
        Assert.IsTrue(canExecute);
    }
}
```

## 📝 使用示例

### 1. 基本登录流程
```csharp
// 1. 用户输入凭证
LoginRequest loginRequest = new LoginRequest
{
    Username = "sysadmin",
    Password = "Admin@123456",
    RememberMe = true,
    UserAgent = "LYBT.WPF.Client",
    LoginType = "Password"
};

// 2. 执行登录
var result = await authModule.LoginAsync(loginRequest);

if (result.IsSuccess)
{
    // 3. 登录成功，获取用户信息
    var currentUser = await authModule.GetCurrentUserAsync();
    Console.WriteLine($"欢迎，{currentUser?.Username}！");
}
else
{
    // 4. 登录失败，显示错误信息
    Console.WriteLine($"登录失败：{result.ErrorMessage}");
}
```

### 2. 凭证管理
```csharp
// 保存凭证（记住我功能）
authModule.SaveCredentials("sysadmin", "Admin@123456", true);

// 加载保存的凭证
var savedCredentials = authModule.LoadSavedCredentials();
if (savedCredentials.IsSuccess && savedCredentials.Data != null)
{
    var loginRequest = savedCredentials.Data;
    // 使用保存的凭证自动填充登录界面
}

// 清除保存的凭证
authModule.ClearSavedCredentials();
```

### 3. 事件处理
```csharp
// 订阅认证状态变更事件
authModule.AuthStatusChanged += (sender, e) =>
{
    if (e.IsLoggedIn)
    {
        Console.WriteLine($"用户 {e.Username} 登录成功：{e.Message}");
    }
    else
    {
        Console.WriteLine($"登录失败：{e.Message}");
    }
};

// 订阅API连接状态变更事件
authModule.ApiConnectionChanged += (sender, e) =>
{
    Console.WriteLine($"API连接状态：{(e.IsConnected ? "在线" : "离线")} - {e.Message}");
};
```

## 🔄 版本历史

- **v1.0.0** - 初始版本，基础认证功能
- **v1.1.0** - 添加记住我功能和凭证管理
- **v2.0.0** - UltraThink架构重构，移除过度设计组件
- **v2.1.0** - 添加API连接监控和状态广播
- **v2.2.0** - 完善事件系统和异常处理

## 📚 相关文档

- [项目文档标准](../../PROJECT_DOCUMENTATION_STANDARDS.md)
- [Desktop.Infrastructure文档](../core/desktop-infrastructure.md) 
- [Desktop.Services文档](../core/desktop-services.md)
- [Shared.Models文档](../../shared/models.md)
- [后端Auth模块文档](../../backend/modules/auth.md)