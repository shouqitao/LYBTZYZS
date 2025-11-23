# LYBT.Desktop.Foundation - 桌面端技术基础层

## 📦 项目定位

- **层级**: Client端 → Desktop端 → 核心层（Core）
- **类型**: 技术基础设施层（Foundation）
- **职责**: 提供平台无关的技术基础能力，包括HTTP通信、缓存、配置、安全、性能优化、诊断监控等横向技术服务。与Desktop.Infrastructure不同，Foundation专注于技术基础设施而非WPF UI组件，确保业务模块能够跨平台复用（如未来支持Avalonia/MAUI）。采用分层架构设计，确保技术能力集中管理、高性能、可观测、可复用。

##  代码结构

```
LYBT.Desktop.Foundation/
├── Api/                                 # API管理
│   └── Managers/
│       └── IUnifiedApiClientManager.cs  # 统一API客户端管理器（1个接口）
│
├── Caching/                             # 缓存服务
│   └── CacheService.cs                  # 内存缓存服务实现（ICacheService接口 + 实现，7个方法）
│
├── Configuration/                       # 配置管理
│   └── ConfigurationService.cs          # 应用配置服务（IConfigurationService接口 + 实现，10个方法）
│
├── Diagnostics/                         # 诊断服务
│   └── DiagnosticService.cs             # 诊断日志服务（结构化日志、性能追踪）
│
├── Exceptions/                          # 异常处理
│   ├── IExceptionHandler.cs             # 异常处理器接口（4个方法）
│   ├── StandardExceptionHandler.cs      # 标准异常处理器实现
│   ├── ExceptionMessageMapper.cs        # 异常消息映射器（友好化错误消息）
│   ├── ExceptionSeverity.cs             # 异常严重程度枚举
│   └── README.md                        # 异常处理子模块文档
│
├── Extensions/                          # 扩展方法
│   ├── FoundationServiceCollectionExtensions.cs # DI注册扩展（AddFoundationServices）
│   ├── PollyExtensions.cs               # Polly弹性策略扩展（重试、熔断、超时）
│   └── ServiceExceptionExtensions.cs    # ServiceResult异常扩展
│
├── Handlers/                            # 处理器
│   └── ServiceHandlerExtensions.cs      # 服务处理器扩展方法
│
├── HealthCheck/                         # 健康检查
│   ├── IApiHealthCheckService.cs        # API健康检查接口（2个成员）
│   └── ApiHealthCheckService.cs         # API健康检查实现（连通性检测）
│
├── Http/                                # HTTP客户端
│   ├── ApiService.cs                    # 通用API服务（IApiService接口 + 2个实现，15+11个方法）
│   ├── AuthorizationMessageHandler.cs   # JWT认证消息处理器（自动添加Authorization头）
│   └── RetryPolicyExtensions.cs         # HTTP重试策略扩展
│
├── Modules/                             # 模块管理
│   ├── IModuleLoadingService.cs         # 模块加载服务接口（2个成员）
│   └── ModuleLoadingService.cs          # 模块加载服务实现（Prism模块延迟加载）
│
├── Performance/                         # 性能优化
│   ├── IStartupOptimizationService.cs   # 启动优化接口（7个成员）
│   └── StartupOptimizationService.cs    # 启动优化实现（预加载、预热、缓存）
│
├── Repositories/                        # 数据仓储
│   └── BaseApiRepository.cs             # API仓储基类（8个方法：CRUD + 分页 + 搜索）
│
├── Security/                            # 安全服务
│   ├── IAuthenticationService.cs        # 认证服务接口（8个方法）
│   ├── AuthenticationService.cs         # 认证服务实现（登录、登出、令牌管理）
│   ├── ISecureCredentialStorage.cs      # 安全凭证存储接口
│   ├── SecureCredentialStorage.cs       # 安全凭证存储实现（DPAPI加密）
│   ├── ITokenStorageService.cs          # 令牌存储接口
│   ├── TokenStorageService.cs           # 令牌存储实现（JWT令牌管理）
│   ├── IUsernameStorageService.cs       # 用户名存储接口
│   ├── UsernameStorageService.cs        # 用户名存储实现
│   └── SecurityService.cs               # 安全服务（加密、解密、哈希）
│
├── Settings/                            # 设置管理
│   └── SettingsService.cs               # 用户设置服务（本地设置持久化）
│
├── LYBT.Desktop.Foundation.csproj       # 项目文件
└── README.md                            # 项目文档
```

**说明**:
- **15个目录，35个文件**：覆盖HTTP通信、缓存、配置、安全、性能、诊断等完整技术栈
- **8个核心服务接口**：IAuthenticationService（8方法）、ICacheService（7方法）、IConfigurationService（10方法）、IApiService（15方法）、BaseApiRepository（8方法）、IApiHealthCheckService（2成员）、IStartupOptimizationService（7成员）、IExceptionHandler（4方法）
- **平台无关设计**：无WPF依赖，纯.NET 8技术栈，支持跨平台复用
- **Foundation vs Infrastructure**：Foundation提供技术基础（HTTP/缓存/配置），Infrastructure提供WPF UI组件（Controls/Converters/Events）

## 🔗 依赖关系

### 依赖的项目
1. **LYBT.Shared.Models** - 共享DTO模型（UserDto、ServiceResult等）
2. **LYBT.Shared.Interfaces** - 共享接口定义（业务契约）
3. **LYBT.Shared.Utilities** - 共享工具类（通用扩展方法）

### 被依赖项目
1. **LYBT.Desktop.Infrastructure** - WPF基础设施（依赖Foundation的HTTP/缓存/配置服务）
2. **LYBT.Desktop.Shell** - 桌面端Shell（DI注册Foundation服务）
3. **Client端业务模块**:
   - LYBT.Desktop.Auth（认证模块）
   - LYBT.Desktop.Users（用户管理）
   - LYBT.Desktop.Patients（患者管理）
   - LYBT.Desktop.MedicalCase（病案管理）
   - LYBT.Desktop.Consultation（诊断管理）
   - LYBT.Desktop.Prescriptions（处方管理）
   - LYBT.Desktop.Herbs（药材管理）
   - LYBT.Desktop.Formula（验方管理）

### NuGet包
- **Microsoft.Extensions.Http** (8.0.x) - HttpClient工厂和生命周期管理
- **Microsoft.Extensions.Caching.Memory** (8.0.x) - 内存缓存框架
- **Microsoft.Extensions.Configuration** (8.0.x) - 配置管理框架
- **Microsoft.Extensions.Logging** (8.0.x) - 结构化日志框架
- **Polly** (8.x) - 弹性策略库（重试、熔断、超时）
- **Refit** (7.x) - 声明式HTTP客户端（可选）
- **System.Security.Cryptography.ProtectedData** (8.0.x) - DPAPI数据保护

## 🛠 技术栈

- **.NET 8**: 基础框架
- **Microsoft.Extensions.*** : 配置、日志、DI、缓存核心库
- **Polly 8.x**: 弹性策略（重试、熔断、超时、回退）
- **System.Text.Json**: JSON序列化/反序列化（高性能）
- **System.Security.Cryptography**: 加密、解密、哈希算法
- **System.Diagnostics**: 诊断追踪、性能监控
- **异步编程**: 全异步API（async/await），提升响应性

##  快速开始

此项目是一个类库，作为Desktop端技术基础设施被其他模块引用。无法独立运行。

```bash
# 构建此项目
dotnet build src/Client/Desktop/Core/LYBT.Desktop.Foundation/LYBT.Desktop.Foundation.csproj
```

**集成说明**：

### 1. 注册Foundation服务（在Shell的App.xaml.cs中）
```csharp
using LYBT.Desktop.Foundation.Extensions;

protected override void RegisterTypes(IContainerRegistry containerRegistry)
{
    // 注册Foundation核心服务（HTTP + 缓存 + 配置 + 安全 + 性能 + 诊断）
    containerRegistry.AddFoundationServices(Configuration);

    // 配置HTTP客户端（BaseUrl + Polly策略）
    containerRegistry.RegisterHttpClient<IApiService, ApiService>(options =>
    {
        options.BaseAddress = new Uri(Configuration["Lybt:Client:Api:BaseUrl"] ?? "https://localhost:5001");
        options.Timeout = TimeSpan.FromSeconds(30);
    });

    // 配置Polly弹性策略（重试 + 熔断 + 超时）
    containerRegistry.ConfigurePollyPolicies(options =>
    {
        options.RetryCount = 3;
        options.CircuitBreakerThreshold = 5;
        options.TimeoutSeconds = 10;
    });
}
```

### 2. 核心服务接口

#### 2.1 IAuthenticationService（认证服务 - 8个方法）

```csharp
public interface IAuthenticationService
{
    // 核心方法（6个）
    Task<bool> IsLoggedInAsync();                                           // 检查登录状态
    Task<ServiceResult<(UserDto User, string Token)>> LoginAsync(           // 登录
        string username, string password);
    Task<ServiceResult<bool>> LogoutAsync();                                // 登出
    Task<UserDto?> GetCurrentUserAsync();                                   // 获取当前用户
    string? GetToken();                                                     // 获取令牌
    void ClearAuthInfo();                                                   // 清除认证信息

    // 扩展功能（2个）
    Task<bool> CheckConnectionAsync();                                      // 检查连接
    Task<bool> ChangePasswordAsync(string oldPassword, string newPassword); // 修改密码
}
```

#### 2.2 ICacheService（缓存服务 - 7个方法）

```csharp
public interface ICacheService
{
    // CRUD操作（4个）
    T? Get<T>(string key);                                              // 获取缓存
    void Set<T>(string key, T value, TimeSpan? expiration = null);      // 设置缓存
    void Remove(string key);                                            // 删除缓存
    bool Exists(string key);                                            // 检查缓存是否存在

    // 高级功能（3个）
    void Clear();                                                       // 清空缓存
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory,     // 获取或创建缓存
        TimeSpan? expiration = null);                                   // （不存在则调用factory）
}
```

#### 2.3 IConfigurationService（配置服务 - 10个方法）

```csharp
public interface IConfigurationService
{
    // 配置读取（3个）
    T GetValue<T>(string key, T defaultValue = default);               // 获取配置值
    IConfigurationSection GetSection(string key);                       // 获取配置节
    Dictionary<string, string> GetDefaultSettings();                    // 获取默认设置

    // 配置写入（2个）
    Task SetValueAsync(string key, object value);                       // 设置配置值
    Task ReloadAsync();                                                 // 重新加载配置

    // 用户设置（2个）
    Dictionary<string, string> LoadUserSettings();                      // 加载用户设置
    Task SaveUserSettingsAsync(Dictionary<string, string> settings);    // 保存用户设置

    // 生命周期（1个）
    void Dispose();                                                     // 释放资源
}
```

#### 2.4 IApiService（通用HTTP服务 - 15个方法）

```csharp
public interface IApiService
{
    // RESTful CRUD（5个）
    Task<ServiceResult<T>> GetAsync<T>(string endpoint,                 // GET请求
        Dictionary<string, string>? queryParams = null);
    Task<ServiceResult<TResponse>> PostAsync<TRequest, TResponse>(      // POST请求
        string endpoint, TRequest data);
    Task<ServiceResult<TResponse>> PutAsync<TRequest, TResponse>(       // PUT请求
        string endpoint, TRequest data);
    Task<ServiceResult<TResponse>> PatchAsync<TRequest, TResponse>(     // PATCH请求
        string endpoint, TRequest data);
    Task<ServiceResult<bool>> DeleteAsync(string endpoint);             // DELETE请求

    // 文件操作（2个）
    Task<byte[]> DownloadAsync(string endpoint);                        // 下载文件
    Task<ServiceResult<TResponse>> UploadAsync<TResponse>(              // 上传文件
        string endpoint, IFormFile file);
}
```

#### 2.5 BaseApiRepository<TDto>（API仓储基类 - 8个方法）

```csharp
public abstract class BaseApiRepository<TDto> where TDto : class
{
    // 基础CRUD（5个）
    public virtual async Task<ServiceResult<List<TDto>>> GetAllAsync(); // 查询全部
    public virtual async Task<ServiceResult<TDto>> GetByIdAsync(Guid id); // 按ID查询
    public virtual async Task<ServiceResult<TDto>> CreateAsync(TDto dto); // 创建
    public virtual async Task<ServiceResult<TDto>> UpdateAsync(Guid id,  // 更新
        TDto dto);
    public virtual async Task<ServiceResult<bool>> DeleteAsync(Guid id); // 删除

    // 高级查询（2个）
    public virtual async Task<ServiceResult<PagedResult<TDto>>>         // 分页查询
        GetPagedAsync(int pageIndex = 1, int pageSize = 10,
            string? filter = null);
    public virtual async Task<ServiceResult<List<TDto>>> SearchAsync(   // 搜索
        string keyword);
}
```

#### 2.6 IApiHealthCheckService（健康检查 - 2个成员）

```csharp
public interface IApiHealthCheckService
{
    // 核心方法
    Task<ApiHealthStatus> CheckHealthAsync();                           // 检查API健康状态

    // 属性
    string? LastErrorMessage { get; }                                   // 最后错误消息
}

public enum ApiHealthStatus
{
    Healthy = 0,        // 健康
    Degraded = 1,       // 降级
    Unhealthy = 2       // 不健康
}
```

#### 2.7 IStartupOptimizationService（启动优化 - 7个成员）

```csharp
public interface IStartupOptimizationService
{
    // 核心优化（4个方法）
    Task WarmupAsync();                                                 // 预热应用
    Task PreloadCriticalResourcesAsync();                               // 预加载关键资源
    Task OptimizeStartupAsync();                                        // 优化启动流程
    Task WarmupApplicationAsync();                                      // 预热应用（完整版）

    // 诊断（2个方法）
    TimeSpan GetStartupDuration();                                      // 获取启动耗时
    void ClearStartupCache();                                           // 清理启动缓存

    // 事件（1个）
    event EventHandler? OptimizationCompleted;                          // 优化完成事件
}
```

#### 2.8 IExceptionHandler（异常处理 - 4个方法）

```csharp
public interface IExceptionHandler
{
    // 同步异常处理（2个）
    void HandleException(Exception exception);                          // 处理异常（无返回值）
    ServiceResult<T> HandleException<T>(Exception exception);           // 处理异常（返回ServiceResult）

    // 异步异常安全执行（2个）
    Task<ServiceResult<T>> SafeExecuteAsync<T>(Func<Task<T>> action);  // 安全执行（带返回值）
    Task<ServiceResult> SafeExecuteAsync(Func<Task> action);            // 安全执行（无返回值）
}
```

### 3. 核心服务使用示例

#### 示例1：认证服务（IAuthenticationService）

**场景**：用户登录与令牌管理

```csharp
// ViewModel中注入认证服务
public class LoginViewModel : BindableBase
{
    private readonly IAuthenticationService _authService;
    private readonly IRegionManager _regionManager;
    private readonly ISessionManager _sessionManager;

    public LoginViewModel(
        IAuthenticationService authService,
        IRegionManager regionManager,
        ISessionManager sessionManager)
    {
        _authService = authService;
        _regionManager = regionManager;
        _sessionManager = sessionManager;

        LoginCommand = new DelegateCommand(async () => await LoginAsync());
    }

    private async Task LoginAsync()
    {
        try
        {
            // 1. 检查连接
            if (!await _authService.CheckConnectionAsync())
            {
                ErrorMessage = "无法连接到服务器，请检查网络";
                return;
            }

            // 2. 执行登录
            var result = await _authService.LoginAsync(Username, Password);
            if (result.IsSuccess)
            {
                // 3. 保存会话信息
                _sessionManager.SetSession(result.Data.User, result.Data.Token);

                // 4. 导航到主界面
                _regionManager.RequestNavigate("ContentRegion", "MainView");
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"登录失败: {ex.Message}";
        }
    }
}
```

#### 示例2：缓存服务（ICacheService）

**场景**：缓存患者列表，减少HTTP请求

```csharp
public class PatientRepository : BaseApiRepository<PatientDto>
{
    private readonly ICacheService _cache;
    private const string CacheKeyPrefix = "patient:";

    public PatientRepository(
        IApiService apiService,
        ICacheService cache,
        ILogger<PatientRepository> logger)
        : base(apiService, "patients", logger)
    {
        _cache = cache;
    }

    // 带缓存的查询（优先从缓存读取）
    public override async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
    {
        var cacheKey = $"{CacheKeyPrefix}{id}";

        // 尝试从缓存获取
        var cached = _cache.Get<PatientDto>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("从缓存命中患者: {PatientId}", id);
            return ServiceResult<PatientDto>.Success(cached);
        }

        // 缓存未命中，调用API
        var result = await base.GetByIdAsync(id);
        if (result.IsSuccess)
        {
            // 缓存5分钟
            _cache.Set(cacheKey, result.Data, TimeSpan.FromMinutes(5));
        }

        return result;
    }

    // GetOrCreateAsync模式（简化缓存逻辑）
    public async Task<ServiceResult<List<PatientDto>>> GetRecentPatientsAsync()
    {
        var cacheKey = "patients:recent";

        try
        {
            var patients = await _cache.GetOrCreateAsync(cacheKey, async () =>
            {
                // 缓存未命中时调用API
                var result = await _apiService.GetAsync<List<PatientDto>>(
                    $"{_endpoint}/recent");
                return result.IsSuccess ? result.Data : new List<PatientDto>();
            }, TimeSpan.FromMinutes(2));

            return ServiceResult<List<PatientDto>>.Success(patients);
        }
        catch (Exception ex)
        {
            return ServiceResult<List<PatientDto>>.Failure(ex.Message);
        }
    }

    // 清除缓存（数据更新后）
    public override async Task<ServiceResult<PatientDto>> UpdateAsync(
        Guid id, PatientDto dto)
    {
        var result = await base.UpdateAsync(id, dto);
        if (result.IsSuccess)
        {
            // 清除缓存，强制下次重新加载
            _cache.Remove($"{CacheKeyPrefix}{id}");
            _cache.Remove("patients:recent");
        }
        return result;
    }
}
```

#### 示例3：配置服务（IConfigurationService）

**场景**：读取API配置与用户设置

```csharp
public class AppConfigurationService
{
    private readonly IConfigurationService _config;
    private readonly ILogger<AppConfigurationService> _logger;

    public AppConfigurationService(
        IConfigurationService config,
        ILogger<AppConfigurationService> logger)
    {
        _config = config;
        _logger = logger;
    }

    // 读取API配置
    public ApiConfiguration GetApiConfiguration()
    {
        return new ApiConfiguration
        {
            BaseUrl = _config.GetValue("Lybt:Client:Api:BaseUrl", "https://localhost:5001"),
            Timeout = _config.GetValue("Lybt:Client:Api:TimeoutSeconds", 30),
            RetryCount = _config.GetValue("Lybt:Client:Api:RetryCount", 3),
            EnableLogging = _config.GetValue("Lybt:Client:Api:EnableLogging", true)
        };
    }

    // 读取用户界面设置
    public UserInterfaceSettings GetUserInterfaceSettings()
    {
        var userSettings = _config.LoadUserSettings();

        return new UserInterfaceSettings
        {
            Theme = userSettings.GetValueOrDefault("UI:Theme", "Light"),
            Language = userSettings.GetValueOrDefault("UI:Language", "zh-CN"),
            PageSize = int.Parse(userSettings.GetValueOrDefault("UI:PageSize", "20"))
        };
    }

    // 保存用户设置
    public async Task SaveUserSettingsAsync(UserInterfaceSettings settings)
    {
        try
        {
            await _config.SetValueAsync("UI:Theme", settings.Theme);
            await _config.SetValueAsync("UI:Language", settings.Language);
            await _config.SetValueAsync("UI:PageSize", settings.PageSize.ToString());

            _logger.LogInformation("用户设置已保存");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "保存用户设置失败");
            throw;
        }
    }

    // 热重载配置（检测appsettings.json变化）
    public async Task ReloadConfigurationAsync()
    {
        await _config.ReloadAsync();
        _logger.LogInformation("配置已重新加载");
    }
}
```

#### 示例4：HTTP客户端（IApiService + BaseApiRepository）

**场景**：实现患者Repository，封装HTTP API调用

```csharp
// 1. 定义Repository接口
public interface IPatientRepository
{
    Task<ServiceResult<List<PatientDto>>> GetAllAsync();
    Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(
        int pageIndex, int pageSize, string? searchTerm);
    Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id);
    Task<ServiceResult<PatientDto>> CreateAsync(PatientDto dto);
    Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientDto dto);
    Task<ServiceResult<bool>> DeleteAsync(Guid id);
    Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword);
}

// 2. 继承BaseApiRepository实现
public class PatientRepository : BaseApiRepository<PatientDto>, IPatientRepository
{
    public PatientRepository(
        IApiService apiService,
        ILogger<PatientRepository> logger)
        : base(apiService, "patients", logger) // 指定端点 "/api/v1/patients"
    {
    }

    // BaseApiRepository已提供默认实现（GetAllAsync, GetByIdAsync, CreateAsync等）
    // 仅需重写需要自定义逻辑的方法

    // 重写分页查询（添加自定义过滤）
    public override async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(
        int pageIndex = 1, int pageSize = 10, string? filter = null)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["pageIndex"] = pageIndex.ToString(),
            ["pageSize"] = pageSize.ToString()
        };

        if (!string.IsNullOrEmpty(filter))
        {
            queryParams["searchTerm"] = filter; // 自定义过滤参数名
        }

        return await _apiService.GetAsync<PagedResult<PatientDto>>(
            $"{_endpoint}/paged", queryParams);
    }

    // 新增自定义方法（不在BaseApiRepository中）
    public async Task<ServiceResult<List<PatientDto>>> GetRecentPatientsAsync(int days = 7)
    {
        var queryParams = new Dictionary<string, string>
        {
            ["days"] = days.ToString()
        };

        return await _apiService.GetAsync<List<PatientDto>>(
            $"{_endpoint}/recent", queryParams);
    }
}

// 3. ViewModel中使用Repository
public class PatientListViewModel : BindableBase
{
    private readonly IPatientRepository _patientRepository;
    private ObservableCollection<PatientDto> _patients;

    public PatientListViewModel(IPatientRepository patientRepository)
    {
        _patientRepository = patientRepository;
        LoadPatientsCommand = new DelegateCommand(async () => await LoadPatientsAsync());
    }

    private async Task LoadPatientsAsync()
    {
        IsBusy = true;

        // 调用Repository（自动处理HTTP请求、错误处理、日志记录）
        var result = await _patientRepository.GetPagedAsync(
            CurrentPageIndex, PageSize, SearchTerm);

        if (result.IsSuccess)
        {
            Patients = new ObservableCollection<PatientDto>(result.Data.Items);
            TotalCount = result.Data.TotalCount;
        }
        else
        {
            // 错误处理
            ErrorMessage = result.ErrorMessage;
        }

        IsBusy = false;
    }
}
```

#### 示例5：健康检查（IApiHealthCheckService）

**场景**：应用启动时检查API连通性

```csharp
public class StartupHealthCheckService
{
    private readonly IApiHealthCheckService _healthCheck;
    private readonly ILogger<StartupHealthCheckService> _logger;

    public StartupHealthCheckService(
        IApiHealthCheckService healthCheck,
        ILogger<StartupHealthCheckService> logger)
    {
        _healthCheck = healthCheck;
        _logger = logger;
    }

    // 启动时检查API健康状态
    public async Task<bool> CheckApiAvailabilityAsync()
    {
        try
        {
            var status = await _healthCheck.CheckHealthAsync();

            switch (status)
            {
                case ApiHealthStatus.Healthy:
                    _logger.LogInformation("API服务健康");
                    return true;

                case ApiHealthStatus.Degraded:
                    _logger.LogWarning("API服务降级: {Message}",
                        _healthCheck.LastErrorMessage);
                    return true; // 仍可使用，但性能下降

                case ApiHealthStatus.Unhealthy:
                    _logger.LogError("API服务不可用: {Message}",
                        _healthCheck.LastErrorMessage);
                    return false;

                default:
                    return false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "健康检查失败");
            return false;
        }
    }

    // 定期健康检查（后台任务）
    public async Task StartPeriodicHealthCheckAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var isHealthy = await CheckApiAvailabilityAsync();

            if (!isHealthy)
            {
                // 触发断线重连机制
                await TriggerReconnectionAsync();
            }

            // 每30秒检查一次
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
        }
    }

    private async Task TriggerReconnectionAsync()
    {
        // 实现重连逻辑（如刷新Token、重新登录等）
        _logger.LogInformation("尝试重新连接API...");
        await Task.Delay(TimeSpan.FromSeconds(5));
    }
}
```

#### 示例6：启动优化（IStartupOptimizationService）

**场景**：应用启动时预加载关键资源，提升用户体验

```csharp
// 在Shell的OnInitialized中使用
public partial class App : PrismApplication
{
    private readonly IStartupOptimizationService _startupOptimization;

    protected override async void OnInitialized()
    {
        InitializeComponent();

        // 1. 启动优化流程
        var startupService = Container.Resolve<IStartupOptimizationService>();
        startupService.OptimizationCompleted += OnOptimizationCompleted;

        await startupService.OptimizeStartupAsync();

        // 2. 预加载关键资源（字典数据、用户权限、系统配置）
        await startupService.PreloadCriticalResourcesAsync();

        // 3. 预热应用（初始化HttpClient、数据库连接池）
        await startupService.WarmupApplicationAsync();

        // 4. 记录启动耗时
        var duration = startupService.GetStartupDuration();
        var logger = Container.Resolve<ILogger<App>>();
        logger.LogInformation("应用启动完成，耗时: {Duration}ms", duration.TotalMilliseconds);

        // 5. 导航到主界面
        await NavigateToMainViewAsync();
    }

    private void OnOptimizationCompleted(object? sender, EventArgs e)
    {
        // 优化完成后的逻辑（如显示主界面、隐藏启动画面）
        Dispatcher.Invoke(() =>
        {
            MainWindow.Show();
            // SplashScreen.Close();
        });
    }
}

// 自定义启动优化逻辑（继承StartupOptimizationService）
public class CustomStartupOptimizationService : StartupOptimizationService
{
    private readonly ICacheService _cache;
    private readonly IConfigurationService _config;
    private readonly IApiService _apiService;

    public CustomStartupOptimizationService(
        ICacheService cache,
        IConfigurationService config,
        IApiService apiService,
        ILogger<CustomStartupOptimizationService> logger)
        : base(logger)
    {
        _cache = cache;
        _config = config;
        _apiService = apiService;
    }

    // 重写预加载逻辑
    public override async Task PreloadCriticalResourcesAsync()
    {
        await base.PreloadCriticalResourcesAsync();

        // 1. 预加载字典数据（药材分类、处方模板）
        await PreloadDictionariesAsync();

        // 2. 预加载用户权限
        await PreloadUserPermissionsAsync();

        // 3. 预热缓存
        await WarmupCacheAsync();
    }

    private async Task PreloadDictionariesAsync()
    {
        // 后台加载字典数据到缓存
        _ = Task.Run(async () =>
        {
            var herbCategories = await _apiService.GetAsync<List<string>>(
                "/api/v1/herbs/categories");
            if (herbCategories.IsSuccess)
            {
                _cache.Set("dict:herb_categories", herbCategories.Data,
                    TimeSpan.FromHours(24));
            }
        });
    }

    private async Task PreloadUserPermissionsAsync()
    {
        // 预加载当前用户权限（避免首次进入模块时加载）
        _ = Task.Run(async () =>
        {
            var permissions = await _apiService.GetAsync<List<string>>(
                "/api/v1/auth/permissions");
            if (permissions.IsSuccess)
            {
                _cache.Set("user:permissions", permissions.Data,
                    TimeSpan.FromMinutes(30));
            }
        });
    }

    private async Task WarmupCacheAsync()
    {
        // 预热缓存（触发IMemoryCache初始化）
        _cache.Set("warmup", DateTime.Now, TimeSpan.FromSeconds(1));
        await Task.CompletedTask;
    }
}
```

#### 示例7：异常处理（IExceptionHandler）

**场景**：全局异常处理与友好错误提示

```csharp
public class GlobalExceptionHandler
{
    private readonly IExceptionHandler _exceptionHandler;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(
        IExceptionHandler exceptionHandler,
        ILogger<GlobalExceptionHandler> logger)
    {
        _exceptionHandler = exceptionHandler;
        _logger = logger;
    }

    // 注册全局异常处理器（在App.xaml.cs中调用）
    public void RegisterGlobalExceptionHandlers()
    {
        // WPF UI线程异常
        Application.Current.DispatcherUnhandledException += (s, e) =>
        {
            _exceptionHandler.HandleException(e.Exception);
            e.Handled = true; // 阻止应用崩溃
        };

        // 任务异常
        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            _exceptionHandler.HandleException(e.Exception);
            e.SetObserved(); // 标记已处理
        };

        // AppDomain异常
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception exception)
            {
                _exceptionHandler.HandleException(exception);
            }
        };
    }

    // 安全执行API调用（自动异常处理）
    public async Task<ServiceResult<T>> SafeApiCallAsync<T>(Func<Task<T>> apiCall)
    {
        return await _exceptionHandler.SafeExecuteAsync(async () =>
        {
            return await apiCall();
        });
    }
}

// ViewModel中使用安全执行
public class PatientViewModel : BindableBase
{
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IPatientRepository _patientRepository;

    private async Task LoadPatientAsync(Guid id)
    {
        // 方式1：使用SafeExecuteAsync（自动捕获异常并转换为ServiceResult）
        var result = await _exceptionHandler.SafeExecuteAsync(async () =>
        {
            var patient = await _patientRepository.GetByIdAsync(id);
            return patient.Data;
        });

        if (result.IsSuccess)
        {
            CurrentPatient = result.Data;
        }
        else
        {
            ErrorMessage = result.ErrorMessage; // 友好的错误消息
        }
    }

    private async Task DeletePatientAsync(Guid id)
    {
        // 方式2：使用SafeExecuteAsync（无返回值）
        var result = await _exceptionHandler.SafeExecuteAsync(async () =>
        {
            await _patientRepository.DeleteAsync(id);
        });

        if (result.IsSuccess)
        {
            // 删除成功，刷新列表
            await LoadPatientsAsync();
        }
        else
        {
            ErrorMessage = result.ErrorMessage;
        }
    }
}

// 异常消息映射器（将技术异常转换为友好提示）
public class ExceptionMessageMapper
{
    public static string GetFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            HttpRequestException => "网络连接失败，请检查网络设置",
            TaskCanceledException => "请求超时，请稍后重试",
            UnauthorizedAccessException => "您没有权限执行此操作",
            ArgumentException => "输入数据不符合要求",
            InvalidOperationException => "操作无效，请检查操作条件",
            _ => $"系统错误: {exception.Message}，请联系管理员"
        };
    }
}
```

### 4. 依赖注入注册（FoundationServiceCollectionExtensions）

```csharp
using LYBT.Desktop.Foundation.Extensions;
using Microsoft.Extensions.DependencyInjection;

public static class FoundationServiceCollectionExtensions
{
    public static IServiceCollection AddFoundationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. 核心服务注册
        services.AddSingleton<ICacheService, CacheService>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddTransient<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IExceptionHandler, StandardExceptionHandler>();

        // 2. HTTP服务注册
        services.AddHttpClient<IApiService, ApiService>(client =>
        {
            client.BaseAddress = new Uri(configuration["Lybt:Client:Api:BaseUrl"]
                ?? "https://localhost:5001");
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddPolicyHandler(GetRetryPolicy())      // 重试策略
        .AddPolicyHandler(GetCircuitBreakerPolicy()); // 熔断策略

        // 3. 安全服务注册
        services.AddSingleton<ISecureCredentialStorage, SecureCredentialStorage>();
        services.AddSingleton<ITokenStorageService, TokenStorageService>();
        services.AddSingleton<IUsernameStorageService, UsernameStorageService>();
        services.AddTransient<SecurityService>();

        // 4. 性能优化服务
        services.AddSingleton<IStartupOptimizationService, StartupOptimizationService>();
        services.AddSingleton<IModuleLoadingService, ModuleLoadingService>();

        // 5. 诊断服务
        services.AddSingleton<DiagnosticService>();
        services.AddSingleton<IApiHealthCheckService, ApiHealthCheckService>();

        // 6. 设置服务
        services.AddSingleton<SettingsService>();

        return services;
    }

    // Polly重试策略（3次重试，指数退避）
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
    }

    // Polly熔断策略（5次失败后熔断30秒）
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));
    }
}
```

### 5. 服务架构图

```
┌─────────────────────────────────────────────────────────────┐
│                 LYBT.Desktop.Foundation                     │
│              (技术基础设施层 - Platform-Agnostic)              │
└─────────────────────────────────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
        ▼                   ▼                   ▼
┌──────────────┐   ┌──────────────┐   ┌──────────────┐
│ HTTP通信     │   │ 缓存管理     │   │ 配置管理     │
│              │   │              │   │              │
│ IApiService  │   │ ICacheService│   │ IConfiguration│
│ BaseApiRepo  │   │ Memory Cache │   │ Service      │
│ ApiHealth    │   │ GetOrCreate  │   │ Hot Reload   │
│ Polly策略    │   │ 过期策略     │   │ 用户设置     │
└──────────────┘   └──────────────┘   └──────────────┘
        │                   │                   │
        └───────────────────┼───────────────────┘
                            ▼
        ┌───────────────────────────────────────┐
        │           核心技术能力层               │
        │                                       │
        │  ┌──────────┐     ┌──────────┐      │
        │  │ 安全服务  │     │ 性能优化  │      │
        │  │          │     │          │      │
        │  │ Auth     │     │ Startup  │      │
        │  │ Token    │     │ Optimize │      │
        │  │ DPAPI    │     │ Warmup   │      │
        │  └──────────┘     └──────────┘      │
        │                                       │
        │  ┌──────────┐     ┌──────────┐      │
        │  │ 异常处理  │     │ 诊断监控  │      │
        │  │          │     │          │      │
        │  │ Exception│     │ Logger   │      │
        │  │ Handler  │     │ Diagnostic│     │
        │  │ SafeExec │     │ HealthCheck│    │
        │  └──────────┘     └──────────┘      │
        └───────────────────────────────────────┘
                            │
        ┌───────────────────┼───────────────────┐
        │                   │                   │
        ▼                   ▼                   ▼
┌──────────────┐   ┌──────────────┐   ┌──────────────┐
│ Infrastructure│   │ 业务模块      │   │ Shell        │
│              │   │              │   │              │
│ WPF UI组件   │   │ Auth/Users   │   │ DI容器注册   │
│ Controls     │   │ Patients     │   │ 模块初始化   │
│ Converters   │   │ MedicalCase  │   │ 全局配置     │
│ Events       │   │ ...          │   │              │
└──────────────┘   └──────────────┘   └──────────────┘
```

### 6. 设计原则

#### 6.1 Foundation vs Infrastructure职责划分

**Foundation（技术基础层）**:
- **定位**: 平台无关的技术基础设施
- **依赖**: 仅依赖.NET 8核心库（Microsoft.Extensions.*）
- **职责**: HTTP通信、缓存、配置、安全、性能、诊断
- **特点**: 无WPF依赖，可跨平台复用（Avalonia/MAUI）

**Infrastructure（WPF基础设施层）**:
- **定位**: WPF专属的UI基础组件
- **依赖**: WPF、Prism、依赖Foundation
- **职责**: 自定义Controls、Converters、Events、XAML扩展
- **特点**: 强WPF依赖，仅适用于WPF Desktop

**职责边界**:
```
Foundation → 技术能力（HTTP/缓存/配置/安全/性能/诊断）
Infrastructure → UI能力（Controls/Converters/Events/XAML）
```

#### 6.2 HTTP客户端分层设计

**三层HTTP抽象**:
```
Level 3: BaseApiRepository<TDto> → 业务仓储（CRUD + 分页 + 搜索）
Level 2: ApiService<TDto>        → 强类型API服务（泛型端点操作）
Level 1: IApiService             → 通用HTTP服务（GET/POST/PUT/DELETE）
```

**优势**:
- **Level 1**: 提供基础HTTP能力，适用于非RESTful API
- **Level 2**: 提供强类型操作，减少重复代码
- **Level 3**: 提供完整CRUD模板，业务仓储仅需继承

#### 6.3 缓存策略设计

**两级缓存机制**:
1. **内存缓存（IMemoryCache）**: 高速缓存，适用于热点数据（字典、用户权限）
2. **请求去重（RequestDeduplicator）**: 避免短时间内重复请求相同API

**缓存失效策略**:
- **时间失效**: 绝对过期（AbsoluteExpiration）+ 滑动过期（SlidingExpiration）
- **主动失效**: 数据更新后立即清除缓存
- **全局清除**: ClearStartupCache、Clear方法

**最佳实践**:
```csharp
// 短生命周期数据（30秒-5分钟）
_cache.Set("data:recent", data, TimeSpan.FromMinutes(2));

// 长生命周期数据（1小时-24小时）
_cache.Set("dict:categories", categories, TimeSpan.FromHours(24));

// GetOrCreateAsync模式（简化缓存逻辑）
var data = await _cache.GetOrCreateAsync("key", async () =>
    await FetchDataFromApiAsync(), TimeSpan.FromMinutes(5));
```

#### 6.4 异常处理与友好化

**三层异常处理机制**:
1. **全局捕获**: AppDomain.UnhandledException + TaskScheduler.UnobservedTaskException + Dispatcher.UnhandledException
2. **ServiceResult封装**: 统一返回格式，避免异常穿透到UI层
3. **友好消息映射**: ExceptionMessageMapper将技术异常转换为用户可理解的提示

**异常严重程度分级**:
```csharp
public enum ExceptionSeverity
{
    Low = 0,        // 低：可恢复的警告（如缓存未命中）
    Medium = 1,     // 中：业务错误（如验证失败）
    High = 2,       // 高：系统错误（如网络异常）
    Critical = 3    // 严重：致命错误（如数据库不可用）
}
```

**SafeExecuteAsync模式**:
```csharp
// 自动捕获异常 + 转换为ServiceResult + 记录日志
var result = await _exceptionHandler.SafeExecuteAsync(async () =>
{
    return await _apiService.GetAsync<DataDto>("/api/data");
});
```

#### 6.5 启动性能优化策略

**四步启动优化流程**:
1. **OptimizeStartupAsync**: 优化启动流程（延迟加载、按需初始化）
2. **PreloadCriticalResourcesAsync**: 预加载关键资源（字典数据、用户权限）
3. **WarmupApplicationAsync**: 预热应用（HttpClient、数据库连接池）
4. **GetStartupDuration**: 记录启动耗时（诊断慢启动问题）

**延迟加载原则**:
- **立即加载**: 登录界面、全局配置、认证服务
- **延迟加载**: 业务模块（Prism模块）、字典数据、历史记录
- **后台加载**: 非关键资源（帮助文档、更新检查）

**启动耗时优化目标**:
- **理想**: <3秒（首次启动）
- **可接受**: <5秒（首次启动）
- **需优化**: >5秒（需ProfileStartup定位瓶颈）

## 📚 详细文档

- **完整模块文档**: [docs/reference/modules/foundation/](../../../../docs/reference/modules/foundation/) *(待创建)*
- **架构设计**: [docs/explanation/architecture/client/foundation-design.md](../../../../docs/explanation/architecture/client/foundation-design.md) *(待创建)*
- **开发指南**: [docs/how-to-guides/client/foundation-development.md](../../../../docs/how-to-guides/client/foundation-development.md) *(待创建)*

---

**最后更新**: 2025-01-29
**维护负责**: Client端开发组

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
