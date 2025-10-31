# Foundation层架构设计

> **文档层级**: Level 2 - 架构解释（Explanation）
> **目标读者**: 架构师、高级开发者
> **更新日期**: 2025-10-29

---

## 📋 目录

1. [Foundation层定位与职责](#1-foundation层定位与职责)
2. [核心架构设计](#2-核心架构设计)
3. [HTTP客户端三层抽象](#3-http客户端三层抽象)
4. [缓存服务架构](#4-缓存服务架构)
5. [配置管理架构](#5-配置管理架构)
6. [安全服务架构](#6-安全服务架构)
7. [性能优化架构](#7-性能优化架构)
8. [异常处理架构](#8-异常处理架构)
9. [健康检查架构](#9-健康检查架构)
10. [诊断与监控架构](#10-诊断与监控架构)
11. [Foundation vs Infrastructure分离](#11-foundation-vs-infrastructure分离)
12. [依赖注入注册](#12-依赖注入注册)
13. [参考资料](#13-参考资料)

---

## 1. Foundation层定位与职责

### 1.1 层级定位

```
Client端层级结构：
┌─────────────────────────────────────────┐
│  Shell层（应用启动与主窗口）             │
├─────────────────────────────────────────┤
│  Modules层（业务模块 - Auth/Patients等）│
├─────────────────────────────────────────┤
│  Infrastructure层（WPF UI基础组件）      │ ← WPF专用
├─────────────────────────────────────────┤
│  ✨ Foundation层（平台无关技术基础）✨    │ ← 当前层
├─────────────────────────────────────────┤
│  Shared层（跨端共享DTO和组件）           │
└─────────────────────────────────────────┘
```

**Foundation层核心定位**：
- **平台无关性**：不依赖WPF，可跨平台复用（Avalonia/MAUI）
- **技术基础设施**：提供HTTP、缓存、配置、安全、性能、诊断等技术能力
- **服务化设计**：所有能力通过服务接口暴露，支持依赖注入
- **桥接角色**：连接Client端业务模块与Server端WebAPI

### 1.2 核心职责

| 职责类别 | 核心能力 | 代表性服务 |
|---------|---------|-----------|
| **HTTP通信** | RESTful API调用、请求重试、熔断保护 | `IApiService`（15方法）、`BaseApiRepository`（8方法） |
| **缓存管理** | 内存缓存、请求去重、过期策略 | `ICacheService`（7方法） |
| **配置管理** | 应用配置、用户设置、热重载 | `IConfigurationService`（10方法） |
| **安全认证** | JWT令牌管理、DPAPI加密、凭证存储 | `IAuthenticationService`（8方法） |
| **性能优化** | 启动优化、预加载、资源预热 | `IStartupOptimizationService`（7成员） |
| **异常处理** | 全局捕获、友好消息、安全执行 | `IExceptionHandler`（4方法） |
| **健康检查** | API连通性检测、状态监控 | `IApiHealthCheckService`（2成员） |
| **诊断监控** | 结构化日志、性能指标、错误追踪 | `DiagnosticService` |

### 1.3 Foundation vs Infrastructure对比

| 对比维度 | Foundation（当前层） | Infrastructure（WPF UI层） |
|---------|---------------------|---------------------------|
| **平台依赖** | ❌ 无WPF依赖，纯.NET 8 | ✅ 强依赖WPF框架 |
| **核心关注** | 技术基础设施（HTTP/缓存/配置/安全） | UI基础组件（Controls/Converters/Events） |
| **复用范围** | 跨平台（Desktop/Avalonia/MAUI） | 仅WPF Desktop |
| **依赖方向** | 被Infrastructure依赖 | 依赖Foundation |
| **典型组件** | `ApiService`、`CacheService`、`AuthenticationService` | `SessionManager`、`DialogService`、`RegionBehaviors` |

**设计原则**：
- ✅ **Foundation不能依赖Infrastructure**（避免循环依赖）
- ✅ **Infrastructure可以依赖Foundation**（技术能力复用）
- ✅ **Foundation通过接口暴露能力**（松耦合设计）

---

## 2. 核心架构设计

### 2.1 服务接口总览（8个核心服务）

```csharp
// 1. 认证服务（8个方法）
public interface IAuthenticationService
{
    Task<bool> IsLoggedInAsync();                          // 检查登录状态
    Task<ServiceResult<(UserDto User, string Token)>> LoginAsync(string username, string password);
    Task<ServiceResult<bool>> LogoutAsync();              // 注销登录
    Task<UserDto?> GetCurrentUserAsync();                 // 获取当前用户
    string? GetToken();                                    // 获取JWT令牌
    void ClearAuthInfo();                                  // 清除认证信息
    Task<bool> CheckConnectionAsync();                    // 检查服务器连接
    Task<bool> ChangePasswordAsync(string oldPassword, string newPassword);
}

// 2. 缓存服务（7个方法）
public interface ICacheService
{
    T? Get<T>(string key);                                // 获取缓存项
    void Set<T>(string key, T value, TimeSpan? expiration = null); // 设置缓存
    void Remove(string key);                              // 移除缓存
    bool Exists(string key);                              // 检查存在性
    void Clear();                                          // 清空所有缓存
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory,
        TimeSpan? expiration = null);                     // 获取或创建（简化模式）
}

// 3. 配置服务（10个方法）
public interface IConfigurationService
{
    T? GetValue<T>(string key);                           // 获取配置值
    IConfigurationSection GetSection(string key);         // 获取配置节
    IDictionary<string, string> GetDefaultSettings();    // 获取默认设置
    Task SetValueAsync<T>(string key, T value);           // 设置配置值
    Task ReloadAsync();                                   // 热重载配置
    Task LoadUserSettings();                              // 加载用户设置
    Task SaveUserSettingsAsync();                         // 保存用户设置
    void Dispose();                                        // 释放资源
}

// 4. API服务（15个方法）
public interface IApiService
{
    Task<ServiceResult<T>> GetAsync<T>(string endpoint);  // GET请求
    Task<ServiceResult<TResponse>> PostAsync<TRequest, TResponse>(
        string endpoint, TRequest request);               // POST请求
    Task<ServiceResult<TResponse>> PutAsync<TRequest, TResponse>(
        string endpoint, TRequest request);               // PUT请求
    Task<ServiceResult<TResponse>> PatchAsync<TRequest, TResponse>(
        string endpoint, TRequest request);               // PATCH请求
    Task<ServiceResult<bool>> DeleteAsync(string endpoint); // DELETE请求
    Task<ServiceResult<byte[]>> DownloadAsync(string endpoint); // 文件下载
    Task<ServiceResult<TResponse>> UploadAsync<TResponse>(
        string endpoint, Stream fileStream, string fileName); // 文件上传
    // ... 其他7个重载方法
}

// 5. API仓储基类（8个方法）
public abstract class BaseApiRepository<TDto> where TDto : class
{
    public virtual async Task<ServiceResult<List<TDto>>> GetAllAsync();
    public virtual async Task<ServiceResult<TDto>> GetByIdAsync(Guid id);
    public virtual async Task<ServiceResult<TDto>> CreateAsync(TDto dto);
    public virtual async Task<ServiceResult<TDto>> UpdateAsync(Guid id, TDto dto);
    public virtual async Task<ServiceResult<bool>> DeleteAsync(Guid id);
    public virtual async Task<ServiceResult<PagedResult<TDto>>> GetPagedAsync(
        int pageIndex = 1, int pageSize = 10, string? filter = null);
    public virtual async Task<ServiceResult<List<TDto>>> SearchAsync(string keyword);
}

// 6. API健康检查服务（2个成员）
public interface IApiHealthCheckService
{
    Task<ApiHealthStatus> CheckHealthAsync();             // 检查健康状态
    string? LastErrorMessage { get; }                     // 最后错误信息
}

// 7. 启动优化服务（7个成员）
public interface IStartupOptimizationService
{
    Task WarmupAsync();                                   // 预热应用
    Task PreloadCriticalResourcesAsync();                 // 预加载资源
    Task OptimizeStartupAsync();                          // 优化启动
    Task WarmupApplicationAsync();                        // 应用预热
    TimeSpan GetStartupDuration();                        // 获取启动时长
    void ClearStartupCache();                             // 清理启动缓存
    event EventHandler? OptimizationCompleted;            // 优化完成事件
}

// 8. 异常处理服务（4个方法）
public interface IExceptionHandler
{
    void HandleException(Exception exception);            // 处理异常
    ServiceResult<T> HandleException<T>(Exception exception); // 处理并返回结果
    Task<ServiceResult<T>> SafeExecuteAsync<T>(Func<Task<T>> operation); // 安全执行
    Task<ServiceResult<bool>> SafeExecuteAsync(Func<Task> operation);
}
```

### 2.2 服务依赖关系图

```
┌─────────────────────────────────────────────────────────────┐
│                     业务模块层（Modules）                      │
│   PatientModule, ConsultationModule, PrescriptionModule...   │
└─────────────────────────────────────────────────────────────┘
                              ↓ 依赖
┌─────────────────────────────────────────────────────────────┐
│                   Foundation服务层（Services）                │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │ AuthService │  │ CacheService│  │ConfigService│         │
│  └─────────────┘  └─────────────┘  └─────────────┘         │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐         │
│  │  ApiService │  │ExceptionHdr │  │HealthCheck  │         │
│  └─────────────┘  └─────────────┘  └─────────────┘         │
└─────────────────────────────────────────────────────────────┘
                              ↓ 基于
┌─────────────────────────────────────────────────────────────┐
│               .NET 8 Core Libraries（无平台依赖）              │
│  HttpClientFactory, IMemoryCache, IConfiguration, ILogger... │
└─────────────────────────────────────────────────────────────┘
```

### 2.3 目录结构（15个目录，35个文件）

```
LYBT.Desktop.Foundation/
├── Api/Managers/           → IUnifiedApiClientManager（统一API客户端）
├── Caching/                → CacheService, ICacheService
├── Configuration/          → ConfigurationService, IConfigurationService
├── Diagnostics/            → DiagnosticService（诊断服务）
├── Exceptions/             → IExceptionHandler, ExceptionMessageMapper, ExceptionSeverity
├── Extensions/             → FoundationServiceCollectionExtensions, PollyExtensions
├── Handlers/               → ServiceHandlerExtensions（服务处理器扩展）
├── HealthCheck/            → IApiHealthCheckService, ApiHealthCheckService
├── Http/                   → ApiService, AuthorizationMessageHandler, RetryPolicyExtensions
├── Modules/                → IModuleLoadingService, ModuleLoadingService（Prism模块延迟加载）
├── Performance/            → IStartupOptimizationService, StartupOptimizationService
├── Repositories/           → BaseApiRepository<TDto>（泛型CRUD仓储基类）
├── Security/               → IAuthenticationService, SecureCredentialStorage, TokenStorageService
├── Settings/               → SettingsService（设置服务）
└── README.md               → 完整项目文档（1192行）
```

---

## 3. HTTP客户端三层抽象

### 3.1 三层抽象设计

```
┌──────────────────────────────────────────────────────────────┐
│  Level 3: BaseApiRepository<TDto>                             │
│  职责：提供完整CRUD模板（GetAll/GetById/Create/Update/Delete） │
│  复用：业务仓储只需继承即可获得标准CRUD能力                      │
└──────────────────────────────────────────────────────────────┘
                              ↓ 依赖
┌──────────────────────────────────────────────────────────────┐
│  Level 2: ApiService<TDto>                                    │
│  职责：提供强类型API操作（泛型端点调用）                          │
│  特性：自动序列化/反序列化、统一错误处理                          │
└──────────────────────────────────────────────────────────────┘
                              ↓ 依赖
┌──────────────────────────────────────────────────────────────┐
│  Level 1: IApiService                                         │
│  职责：提供基础HTTP操作（GET/POST/PUT/PATCH/DELETE）            │
│  特性：Polly弹性策略、JWT认证、超时控制                          │
└──────────────────────────────────────────────────────────────┘
                              ↓ 基于
┌──────────────────────────────────────────────────────────────┐
│  HttpClientFactory + Polly                                    │
│  职责：管理HttpClient生命周期、应用弹性策略                       │
└──────────────────────────────────────────────────────────────┘
```

### 3.2 Level 1: IApiService（基础HTTP服务）

**核心能力**：
- ✅ **RESTful操作**：GET/POST/PUT/PATCH/DELETE
- ✅ **文件传输**：DownloadAsync（下载）、UploadAsync（上传）
- ✅ **Polly策略**：重试（3次指数退避）、熔断器（5次失败后开路30秒）
- ✅ **JWT认证**：自动注入Authorization头
- ✅ **超时控制**：默认30秒，可配置

**核心方法**：

```csharp
public interface IApiService
{
    // 基础CRUD操作
    Task<ServiceResult<T>> GetAsync<T>(string endpoint);
    Task<ServiceResult<TResponse>> PostAsync<TRequest, TResponse>(
        string endpoint, TRequest request);
    Task<ServiceResult<TResponse>> PutAsync<TRequest, TResponse>(
        string endpoint, TRequest request);
    Task<ServiceResult<TResponse>> PatchAsync<TRequest, TResponse>(
        string endpoint, TRequest request);
    Task<ServiceResult<bool>> DeleteAsync(string endpoint);

    // 文件传输
    Task<ServiceResult<byte[]>> DownloadAsync(string endpoint);
    Task<ServiceResult<TResponse>> UploadAsync<TResponse>(
        string endpoint, Stream fileStream, string fileName);

    // 无请求体的变体方法
    Task<ServiceResult<TResponse>> PostAsync<TResponse>(string endpoint);
    Task<ServiceResult<TResponse>> PutAsync<TResponse>(string endpoint);
    Task<ServiceResult<TResponse>> PatchAsync<TResponse>(string endpoint);

    // 无响应体的变体方法
    Task<ServiceResult<bool>> PostAsync<TRequest>(string endpoint, TRequest request);
    Task<ServiceResult<bool>> PutAsync<TRequest>(string endpoint, TRequest request);
    Task<ServiceResult<bool>> PatchAsync<TRequest>(string endpoint, TRequest request);
}
```

**Polly弹性策略配置**：

```csharp
// 1. 重试策略（指数退避）
private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError() // 处理5xx和408错误
        .WaitAndRetryAsync(3, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))); // 2秒、4秒、8秒
}

// 2. 熔断器策略
private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,  // 5次失败后开路
            durationOfBreak: TimeSpan.FromSeconds(30) // 开路30秒
        );
}

// 3. 超时策略
private static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
{
    return Policy.TimeoutAsync<HttpResponseMessage>(30); // 30秒超时
}
```

**使用示例**：

```csharp
public class PatientService
{
    private readonly IApiService _apiService;

    public async Task<ServiceResult<PatientDto>> GetPatientAsync(Guid id)
    {
        // Level 1使用：直接调用基础API服务
        return await _apiService.GetAsync<PatientDto>($"api/v1/patients/{id}");
    }

    public async Task<ServiceResult<PatientDto>> CreatePatientAsync(CreatePatientDto dto)
    {
        // POST请求自动应用重试和熔断策略
        return await _apiService.PostAsync<CreatePatientDto, PatientDto>(
            "api/v1/patients", dto);
    }
}
```

### 3.3 Level 2: ApiService<TDto>（强类型API服务）

**设计目标**：
- 消除重复的端点字符串拼接
- 提供强类型的API操作
- 统一错误处理和日志记录

**典型实现**：

```csharp
public class PatientApiService
{
    private readonly IApiService _apiService;
    private const string Endpoint = "api/v1/patients";

    public PatientApiService(IApiService apiService)
    {
        _apiService = apiService;
    }

    // 强类型方法，隐藏端点细节
    public Task<ServiceResult<List<PatientDto>>> GetAllAsync()
        => _apiService.GetAsync<List<PatientDto>>(Endpoint);

    public Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
        => _apiService.GetAsync<PatientDto>($"{Endpoint}/{id}");

    public Task<ServiceResult<PatientDto>> CreateAsync(CreatePatientDto dto)
        => _apiService.PostAsync<CreatePatientDto, PatientDto>(Endpoint, dto);

    public Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, UpdatePatientDto dto)
        => _apiService.PutAsync<UpdatePatientDto, PatientDto>($"{Endpoint}/{id}", dto);

    public Task<ServiceResult<bool>> DeleteAsync(Guid id)
        => _apiService.DeleteAsync($"{Endpoint}/{id}");
}
```

### 3.4 Level 3: BaseApiRepository<TDto>（CRUD仓储模板）

**核心价值**：
- ✅ **开箱即用的CRUD**：业务仓储继承即获得8个标准方法
- ✅ **可覆盖扩展**：virtual方法允许自定义实现
- ✅ **统一异常处理**：自动转换为ServiceResult
- ✅ **日志集成**：自动记录请求和错误

**基类实现**（关键方法）：

```csharp
public abstract class BaseApiRepository<TDto> where TDto : class
{
    protected readonly IApiService _apiService;
    protected readonly ILogger _logger;
    protected readonly string _endpoint;

    protected BaseApiRepository(
        IApiService apiService,
        ILogger logger,
        string endpoint)
    {
        _apiService = apiService;
        _logger = logger;
        _endpoint = endpoint;
    }

    // 1. 查询所有
    public virtual async Task<ServiceResult<List<TDto>>> GetAllAsync()
    {
        _logger.LogDebug("Fetching all {EntityType}", typeof(TDto).Name);
        return await _apiService.GetAsync<List<TDto>>(_endpoint);
    }

    // 2. 按ID查询
    public virtual async Task<ServiceResult<TDto>> GetByIdAsync(Guid id)
    {
        _logger.LogDebug("Fetching {EntityType} with ID: {Id}", typeof(TDto).Name, id);
        return await _apiService.GetAsync<TDto>($"{_endpoint}/{id}");
    }

    // 3. 创建
    public virtual async Task<ServiceResult<TDto>> CreateAsync(TDto dto)
    {
        _logger.LogInformation("Creating {EntityType}", typeof(TDto).Name);
        return await _apiService.PostAsync<TDto, TDto>(_endpoint, dto);
    }

    // 4. 更新
    public virtual async Task<ServiceResult<TDto>> UpdateAsync(Guid id, TDto dto)
    {
        _logger.LogInformation("Updating {EntityType} with ID: {Id}", typeof(TDto).Name, id);
        return await _apiService.PutAsync<TDto, TDto>($"{_endpoint}/{id}", dto);
    }

    // 5. 删除
    public virtual async Task<ServiceResult<bool>> DeleteAsync(Guid id)
    {
        _logger.LogWarning("Deleting {EntityType} with ID: {Id}", typeof(TDto).Name, id);
        return await _apiService.DeleteAsync($"{_endpoint}/{id}");
    }

    // 6. 分页查询
    public virtual async Task<ServiceResult<PagedResult<TDto>>> GetPagedAsync(
        int pageIndex = 1,
        int pageSize = 10,
        string? filter = null)
    {
        var query = $"{_endpoint}?pageIndex={pageIndex}&pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(filter))
            query += $"&filter={Uri.EscapeDataString(filter)}";

        _logger.LogDebug("Fetching paged {EntityType}: Page {PageIndex}, Size {PageSize}",
            typeof(TDto).Name, pageIndex, pageSize);

        return await _apiService.GetAsync<PagedResult<TDto>>(query);
    }

    // 7. 搜索
    public virtual async Task<ServiceResult<List<TDto>>> SearchAsync(string keyword)
    {
        var query = $"{_endpoint}/search?keyword={Uri.EscapeDataString(keyword)}";
        _logger.LogDebug("Searching {EntityType} with keyword: {Keyword}",
            typeof(TDto).Name, keyword);

        return await _apiService.GetAsync<List<TDto>>(query);
    }
}
```

**业务仓储实现示例**：

```csharp
// 场景1：零代码继承（完全复用CRUD）
public class PatientRepository : BaseApiRepository<PatientDto>
{
    public PatientRepository(
        IApiService apiService,
        ILogger<PatientRepository> logger)
        : base(apiService, logger, "api/v1/patients")
    {
    }
    // ✅ 无需编写任何代码，已获得8个CRUD方法
}

// 场景2：部分覆盖（自定义扩展）
public class HerbRepository : BaseApiRepository<HerbDto>
{
    private readonly ICacheService _cache;

    public HerbRepository(
        IApiService apiService,
        ILogger<HerbRepository> logger,
        ICacheService cache)
        : base(apiService, logger, "api/v1/herbs")
    {
        _cache = cache;
    }

    // 覆盖GetByIdAsync实现缓存逻辑
    public override async Task<ServiceResult<HerbDto>> GetByIdAsync(Guid id)
    {
        var cacheKey = $"herb:{id}";

        // 先查缓存
        var cached = _cache.Get<HerbDto>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Cache hit for herb: {HerbId}", id);
            return ServiceResult<HerbDto>.Success(cached);
        }

        // 缓存未命中，调用基类实现
        var result = await base.GetByIdAsync(id);
        if (result.IsSuccess)
        {
            // 缓存5分钟
            _cache.Set(cacheKey, result.Data, TimeSpan.FromMinutes(5));
        }

        return result;
    }

    // 新增自定义方法
    public async Task<ServiceResult<List<HerbDto>>> GetByPinyinAsync(string pinyin)
    {
        var endpoint = $"{_endpoint}/pinyin?keyword={Uri.EscapeDataString(pinyin)}";
        return await _apiService.GetAsync<List<HerbDto>>(endpoint);
    }
}
```

### 3.5 三层抽象优势总结

| 层级 | 适用场景 | 代码量 | 灵活性 | 类型安全 |
|-----|---------|-------|-------|---------|
| **Level 1** | 非RESTful API、特殊端点 | 多 | 高 | 中 |
| **Level 2** | RESTful API、需要强类型 | 中 | 中 | 高 |
| **Level 3** | 标准CRUD、快速开发 | 少 | 低 | 高 |

**选择建议**：
- ✅ **优先Level 3**：90%的业务仓储使用`BaseApiRepository`
- ✅ **必要时Level 2**：需要自定义端点逻辑时
- ✅ **特殊时Level 1**：非标准API或需要最大灵活性时

---

## 4. 缓存服务架构

### 4.1 缓存策略设计

```
┌─────────────────────────────────────────────────────────┐
│               ICacheService（缓存抽象层）                  │
│  Get<T> / Set<T> / Remove / Exists / Clear / GetOrCreate │
└─────────────────────────────────────────────────────────┘
                         ↓ 实现
┌─────────────────────────────────────────────────────────┐
│          CacheService（基于IMemoryCache）                  │
│  - 绝对过期时间（Absolute Expiration）                      │
│  - 滑动过期时间（Sliding Expiration）                       │
│  - 请求去重（Request Deduplication）                        │
└─────────────────────────────────────────────────────────┘
                         ↓ 依赖
┌─────────────────────────────────────────────────────────┐
│           .NET IMemoryCache（内存缓存）                     │
│  - 高性能LRU缓存                                            │
│  - 线程安全                                                │
│  - 自动过期清理                                             │
└─────────────────────────────────────────────────────────┘
```

### 4.2 ICacheService接口设计

```csharp
public interface ICacheService
{
    // 基础CRUD操作
    T? Get<T>(string key);                                // 获取缓存项
    void Set<T>(string key, T value, TimeSpan? expiration = null); // 设置缓存
    void Remove(string key);                              // 移除单个缓存
    bool Exists(string key);                              // 检查是否存在
    void Clear();                                          // 清空所有缓存

    // 高级模式
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory,
        TimeSpan? expiration = null);                     // 获取或创建
}
```

### 4.3 核心缓存模式

#### 模式1：Cache-First（缓存优先）

```csharp
public class PatientRepository : BaseApiRepository<PatientDto>
{
    private readonly ICacheService _cache;
    private const string CacheKeyPrefix = "patient:";

    public override async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
    {
        var cacheKey = $"{CacheKeyPrefix}{id}";

        // 1. 先查缓存
        var cached = _cache.Get<PatientDto>(cacheKey);
        if (cached != null)
        {
            _logger.LogDebug("Cache hit for patient: {PatientId}", id);
            return ServiceResult<PatientDto>.Success(cached);
        }

        // 2. 缓存未命中，调用API
        var result = await base.GetByIdAsync(id);
        if (result.IsSuccess)
        {
            // 3. 缓存5分钟
            _cache.Set(cacheKey, result.Data, TimeSpan.FromMinutes(5));
        }

        return result;
    }
}
```

#### 模式2：GetOrCreateAsync（简化模式）

```csharp
public async Task<ServiceResult<List<PatientDto>>> GetRecentPatientsAsync()
{
    var cacheKey = "patients:recent";

    try
    {
        // 自动处理缓存未命中的工厂方法
        var patients = await _cache.GetOrCreateAsync(cacheKey, async () =>
        {
            // 仅在缓存未命中时调用
            var result = await _apiService.GetAsync<List<PatientDto>>(
                $"{_endpoint}/recent");
            return result.IsSuccess ? result.Data : new List<PatientDto>();
        }, TimeSpan.FromMinutes(2)); // 缓存2分钟

        return ServiceResult<List<PatientDto>>.Success(patients);
    }
    catch (Exception ex)
    {
        return ServiceResult<List<PatientDto>>.Failure(ex.Message);
    }
}
```

#### 模式3：Cache Invalidation（缓存失效）

```csharp
public override async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientDto dto)
{
    var result = await base.UpdateAsync(id, dto);
    if (result.IsSuccess)
    {
        // 更新成功后，立即清除缓存
        _cache.Remove($"{CacheKeyPrefix}{id}");
        _cache.Remove("patients:recent"); // 清除列表缓存
    }
    return result;
}

public override async Task<ServiceResult<bool>> DeleteAsync(Guid id)
{
    var result = await base.DeleteAsync(id);
    if (result.IsSuccess)
    {
        // 删除成功后，清除缓存
        _cache.Remove($"{CacheKeyPrefix}{id}");
        _cache.Remove("patients:recent");
    }
    return result;
}
```

### 4.4 缓存策略配置

```csharp
public class CacheService : ICacheService
{
    private readonly IMemoryCache _cache;

    public void Set<T>(string key, T value, TimeSpan? expiration = null)
    {
        var options = new MemoryCacheEntryOptions
        {
            // 绝对过期时间（从现在开始计算）
            AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(5),

            // 滑动过期时间（最后访问后多久过期）
            SlidingExpiration = TimeSpan.FromMinutes(2),

            // 优先级（内存不足时的清理顺序）
            Priority = CacheItemPriority.Normal,

            // 过期回调
            PostEvictionCallbacks =
            {
                new PostEvictionCallbackRegistration
                {
                    EvictionCallback = (key, value, reason, state) =>
                    {
                        _logger.LogDebug("Cache entry {Key} evicted. Reason: {Reason}",
                            key, reason);
                    }
                }
            }
        };

        _cache.Set(key, value, options);
    }
}
```

### 4.5 缓存最佳实践

| 实践项 | 建议 | 原因 |
|-------|-----|-----|
| **Key命名规范** | 使用`entity:operation:id`格式（如`patient:detail:123`） | 避免冲突、便于批量清理 |
| **过期时间** | 频繁变更：1-2分钟；稳定数据：5-10分钟 | 平衡性能与数据新鲜度 |
| **缓存粒度** | 优先缓存单个实体，避免大列表 | 减少内存占用、提高命中率 |
| **失效策略** | 写操作（Create/Update/Delete）必须清除缓存 | 保证数据一致性 |
| **异常处理** | GetOrCreateAsync必须捕获异常 | 避免缓存穿透 |

---

## 5. 配置管理架构

### 5.1 配置层次结构

```
┌──────────────────────────────────────────────────────┐
│          IConfigurationService（统一配置接口）         │
│  GetValue / GetSection / LoadUserSettings / Reload... │
└──────────────────────────────────────────────────────┘
                        ↓ 聚合
┌──────────────────────────────────────────────────────┐
│            ConfigurationService（实现类）               │
│  - appsettings.json（应用配置）                         │
│  - usersettings.json（用户设置）                        │
│  - 环境变量（开发/生产环境）                             │
└──────────────────────────────────────────────────────┘
                        ↓ 基于
┌──────────────────────────────────────────────────────┐
│        .NET IConfiguration（配置抽象）                  │
│  - JSON文件                                            │
│  - 环境变量                                             │
│  - 命令行参数                                           │
│  - 热重载支持                                           │
└──────────────────────────────────────────────────────┘
```

### 5.2 IConfigurationService接口设计

```csharp
public interface IConfigurationService
{
    // 读取配置
    T? GetValue<T>(string key);                          // 获取单个配置值
    IConfigurationSection GetSection(string key);        // 获取配置节
    IDictionary<string, string> GetDefaultSettings();   // 获取默认设置

    // 写入配置
    Task SetValueAsync<T>(string key, T value);          // 设置配置值（持久化）

    // 热重载
    Task ReloadAsync();                                  // 重新加载配置

    // 用户设置
    Task LoadUserSettings();                             // 加载用户个性化设置
    Task SaveUserSettingsAsync();                        // 保存用户设置

    // 资源管理
    void Dispose();                                       // 释放资源
}
```

### 5.3 配置文件结构

#### appsettings.json（应用配置）

```json
{
  "Lybt": {
    "Client": {
      "Api": {
        "BaseUrl": "https://localhost:5001",
        "TimeoutSeconds": 30,
        "RetryCount": 3,
        "CircuitBreakerThreshold": 5,
        "IgnoreSslErrors": false
      }
    }
  },
  "CacheSettings": {
    "DefaultExpiration": "00:05:00",
    "MaxCacheSize": "100MB"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "LYBT": "Debug"
    }
  },
  "Performance": {
    "StartupTimeout": "00:00:05",
    "PreloadEnabled": true
  }
}
```

#### usersettings.json（用户设置）

```json
{
  "UI": {
    "Language": "zh-CN",
    "Theme": "Light",
    "FontSize": 14
  },
  "Data": {
    "PageSize": 20,
    "CacheEnabled": true
  },
  "Print": {
    "DefaultPrinter": "HP LaserJet",
    "PaperSize": "A4"
  }
}
```

### 5.4 配置使用示例

#### 场景1：读取API配置

```csharp
public class ApiService : IApiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfigurationService _configService;

    public ApiService(HttpClient httpClient, IConfigurationService configService)
    {
        _httpClient = httpClient;
        _configService = configService;

        // 从配置读取API基地址（Issue #1726: 使用新配置路径）
        var baseUrl = _configService.GetValue<string>("Lybt:Client:Api:BaseUrl");
        _httpClient.BaseAddress = new Uri(baseUrl ?? "https://localhost:5001");

        // 读取超时设置
        var timeout = _configService.GetValue<int>("Lybt:Client:Api:TimeoutSeconds");
        _httpClient.Timeout = TimeSpan.FromSeconds(timeout > 0 ? timeout : 30);
    }
}
```

#### 场景2：用户设置读写

```csharp
public class SettingsViewModel : ViewModelBase
{
    private readonly IConfigurationService _configService;

    private int _pageSize;
    public int PageSize
    {
        get => _pageSize;
        set
        {
            if (SetProperty(ref _pageSize, value))
            {
                // 保存用户设置
                _ = SaveUserSettingAsync("Data:PageSize", value);
            }
        }
    }

    public async Task LoadUserSettingsAsync()
    {
        await _configService.LoadUserSettings();
        PageSize = _configService.GetValue<int>("Data:PageSize") ?? 20;
    }

    private async Task SaveUserSettingAsync<T>(string key, T value)
    {
        await _configService.SetValueAsync(key, value);
        await _configService.SaveUserSettingsAsync();
    }
}
```

#### 场景3：配置热重载

```csharp
public partial class App : PrismApplication
{
    private readonly IConfigurationService _configService;

    protected override void OnInitialized()
    {
        // 监听配置文件变化
        _configService.ReloadAsync();

        // 订阅配置变更事件
        ChangeToken.OnChange(
            () => _configuration.GetReloadToken(),
            () =>
            {
                _logger.LogInformation("Configuration reloaded");
                // 重新加载依赖配置的服务
                RefreshServices();
            });
    }
}
```

### 5.5 配置最佳实践

| 配置类型 | 存储位置 | 修改方式 | 示例 |
|---------|---------|---------|-----|
| **应用配置** | appsettings.json | 部署时修改，不支持运行时 | API地址、日志级别 |
| **用户设置** | usersettings.json | 运行时修改，支持热重载 | 界面语言、分页大小 |
| **敏感配置** | 环境变量或User Secrets | 开发环境隔离 | 数据库密码、API密钥 |
| **开发配置** | appsettings.Development.json | 覆盖appsettings.json | 开发环境API地址 |

---

## 6. 安全服务架构

### 6.1 安全架构总览

```
┌─────────────────────────────────────────────────────┐
│        IAuthenticationService（认证服务）              │
│  Login / Logout / GetToken / ChangePassword...       │
└─────────────────────────────────────────────────────┘
                      ↓ 依赖
┌─────────────────────────────────────────────────────┐
│      TokenStorageService（令牌存储）                   │
│  - 内存存储：ITokenStorageService                      │
│  - 持久化存储：SecureCredentialStorage（DPAPI加密）    │
└─────────────────────────────────────────────────────┘
                      ↓ 注入
┌─────────────────────────────────────────────────────┐
│  AuthorizationMessageHandler（HTTP拦截器）             │
│  - 自动注入Authorization: Bearer {token}              │
│  - 401响应自动清除令牌                                 │
└─────────────────────────────────────────────────────┘
                      ↓ 应用
┌─────────────────────────────────────────────────────┐
│          所有HTTP请求（通过HttpClient）                 │
└─────────────────────────────────────────────────────┘
```

### 6.2 IAuthenticationService接口设计

```csharp
public interface IAuthenticationService
{
    // 核心认证方法
    Task<bool> IsLoggedInAsync();                       // 检查登录状态
    Task<ServiceResult<(UserDto User, string Token)>> LoginAsync(
        string username, string password);              // 登录
    Task<ServiceResult<bool>> LogoutAsync();           // 注销
    Task<UserDto?> GetCurrentUserAsync();              // 获取当前用户
    string? GetToken();                                 // 获取JWT令牌
    void ClearAuthInfo();                               // 清除认证信息

    // 扩展功能
    Task<bool> CheckConnectionAsync();                 // 检查服务器连接
    Task<bool> ChangePasswordAsync(string oldPassword, string newPassword);
}
```

### 6.3 DPAPI凭证加密

**Windows Data Protection API（DPAPI）特性**：
- ✅ **Windows内置加密**：无需管理密钥
- ✅ **用户级别隔离**：每个Windows用户独立加密
- ✅ **机器绑定**：加密数据无法移植到其他机器
- ✅ **零配置**：开箱即用

**实现示例**：

```csharp
public class SecureCredentialStorage : ISecureCredentialStorage
{
    private const string UsernameKey = "LYBT_Username";
    private const string PasswordKey = "LYBT_Password";

    // 保存凭证（DPAPI加密）
    public void SaveCredentials(string username, string password)
    {
        try
        {
            // 加密用户名
            var usernameBytes = Encoding.UTF8.GetBytes(username);
            var encryptedUsername = ProtectedData.Protect(
                usernameBytes,
                entropy: null,
                scope: DataProtectionScope.CurrentUser); // 用户级别加密

            // 加密密码
            var passwordBytes = Encoding.UTF8.GetBytes(password);
            var encryptedPassword = ProtectedData.Protect(
                passwordBytes,
                entropy: null,
                scope: DataProtectionScope.CurrentUser);

            // 保存到注册表（或文件）
            using var key = Registry.CurrentUser.CreateSubKey(@"Software\LYBT\Credentials");
            key?.SetValue(UsernameKey, Convert.ToBase64String(encryptedUsername));
            key?.SetValue(PasswordKey, Convert.ToBase64String(encryptedPassword));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save credentials");
            throw;
        }
    }

    // 读取凭证（DPAPI解密）
    public (string? Username, string? Password) LoadCredentials()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\LYBT\Credentials");
            if (key == null) return (null, null);

            // 读取并解密用户名
            var encryptedUsernameBase64 = key.GetValue(UsernameKey) as string;
            var username = DecryptString(encryptedUsernameBase64);

            // 读取并解密密码
            var encryptedPasswordBase64 = key.GetValue(PasswordKey) as string;
            var password = DecryptString(encryptedPasswordBase64);

            return (username, password);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load credentials");
            return (null, null);
        }
    }

    private string? DecryptString(string? encryptedBase64)
    {
        if (string.IsNullOrEmpty(encryptedBase64)) return null;

        var encryptedBytes = Convert.FromBase64String(encryptedBase64);
        var decryptedBytes = ProtectedData.Unprotect(
            encryptedBytes,
            entropy: null,
            scope: DataProtectionScope.CurrentUser);

        return Encoding.UTF8.GetString(decryptedBytes);
    }

    // 清除凭证
    public void ClearCredentials()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\LYBT\Credentials", writable: true);
            key?.DeleteValue(UsernameKey, throwOnMissingValue: false);
            key?.DeleteValue(PasswordKey, throwOnMissingValue: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to clear credentials");
        }
    }
}
```

### 6.4 JWT令牌管理

**TokenStorageService实现**：

```csharp
public class TokenStorageService : ITokenStorageService
{
    private string? _currentToken;
    private DateTime? _tokenExpiry;

    // 保存令牌到内存
    public void SaveToken(string token)
    {
        _currentToken = token;

        // 解析JWT过期时间
        var handler = new JwtSecurityTokenHandler();
        if (handler.CanReadToken(token))
        {
            var jwtToken = handler.ReadJwtToken(token);
            _tokenExpiry = jwtToken.ValidTo;
        }
    }

    // 获取当前令牌
    public string? GetToken()
    {
        // 检查令牌是否过期
        if (_tokenExpiry.HasValue && DateTime.UtcNow >= _tokenExpiry.Value)
        {
            _currentToken = null; // 令牌已过期
        }

        return _currentToken;
    }

    // 检查令牌是否有效
    public bool IsTokenValid()
    {
        if (string.IsNullOrEmpty(_currentToken)) return false;
        if (!_tokenExpiry.HasValue) return true;

        return DateTime.UtcNow < _tokenExpiry.Value;
    }

    // 清除令牌
    public void ClearToken()
    {
        _currentToken = null;
        _tokenExpiry = null;
    }
}
```

### 6.5 HTTP请求自动认证

**AuthorizationMessageHandler实现**：

```csharp
public class AuthorizationMessageHandler : DelegatingHandler
{
    private readonly ITokenStorageService _tokenStorage;
    private readonly ILogger<AuthorizationMessageHandler> _logger;

    public AuthorizationMessageHandler(
        ITokenStorageService tokenStorage,
        ILogger<AuthorizationMessageHandler> logger)
    {
        _tokenStorage = tokenStorage;
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // 1. 自动注入Authorization头
        var token = _tokenStorage.GetToken();
        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _logger.LogDebug("Added Authorization header to request: {Method} {Uri}",
                request.Method, request.RequestUri);
        }

        // 2. 发送请求
        var response = await base.SendAsync(request, cancellationToken);

        // 3. 处理401未授权响应
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("Received 401 Unauthorized response. Clearing token.");
            _tokenStorage.ClearToken();

            // 可选：触发重新登录事件
            // EventAggregator.GetEvent<UnauthorizedEvent>().Publish();
        }

        return response;
    }
}

// 注册到HttpClient
services.AddHttpClient<IApiService, ApiService>()
    .AddHttpMessageHandler<AuthorizationMessageHandler>();
```

### 6.6 认证流程示例

```csharp
public class LoginViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authService;
    private readonly ISecureCredentialStorage _credentialStorage;

    // 登录命令
    public ICommand LoginCommand => new DelegateCommand(async () => await LoginAsync());

    private async Task LoginAsync()
    {
        // 1. 调用登录接口
        var result = await _authService.LoginAsync(Username, Password);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.ErrorMessage;
            return;
        }

        // 2. 保存用户信息和令牌
        var (user, token) = result.Data;
        CurrentUser = user;

        // 3. 可选：记住密码（DPAPI加密存储）
        if (RememberPassword)
        {
            _credentialStorage.SaveCredentials(Username, Password);
        }

        // 4. 导航到主页
        _regionManager.RequestNavigate("ContentRegion", "DashboardView");
    }

    // 注销命令
    public ICommand LogoutCommand => new DelegateCommand(async () => await LogoutAsync());

    private async Task LogoutAsync()
    {
        var result = await _authService.LogoutAsync();
        if (result.IsSuccess)
        {
            // 清除凭证
            _credentialStorage.ClearCredentials();

            // 返回登录页
            _regionManager.RequestNavigate("ContentRegion", "LoginView");
        }
    }
}
```

---

## 7. 性能优化架构

### 7.1 启动优化流程

```
┌─────────────────────────────────────────────────────┐
│      IStartupOptimizationService（启动优化服务）       │
└─────────────────────────────────────────────────────┘
                      ↓ 执行
┌─────────────────────────────────────────────────────┐
│             四步启动优化流程                           │
│  Step 1: OptimizeStartupAsync()    → 优化启动          │
│  Step 2: PreloadCriticalResourcesAsync() → 预加载资源  │
│  Step 3: WarmupApplicationAsync()  → 应用预热          │
│  Step 4: GetStartupDuration()      → 测量启动时长      │
└─────────────────────────────────────────────────────┘
                      ↓ 触发
┌─────────────────────────────────────────────────────┐
│          OptimizationCompleted事件                    │
│  - 启动完成后触发                                      │
│  - 可用于显示主窗口、启动后台任务                       │
└─────────────────────────────────────────────────────┘
```

### 7.2 IStartupOptimizationService接口设计

```csharp
public interface IStartupOptimizationService
{
    // 核心优化方法
    Task WarmupAsync();                                  // 预热应用（初始化关键组件）
    Task PreloadCriticalResourcesAsync();               // 预加载资源（字典/配置/权限）
    Task OptimizeStartupAsync();                        // 优化启动（延迟加载非关键服务）
    Task WarmupApplicationAsync();                      // 应用预热（HttpClient/数据库连接池）

    // 性能监控
    TimeSpan GetStartupDuration();                      // 获取启动时长
    void ClearStartupCache();                           // 清理启动缓存

    // 事件
    event EventHandler? OptimizationCompleted;          // 优化完成事件
}
```

### 7.3 启动优化实现

```csharp
public class StartupOptimizationService : IStartupOptimizationService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<StartupOptimizationService> _logger;
    private readonly Stopwatch _stopwatch;

    public event EventHandler? OptimizationCompleted;

    public StartupOptimizationService(
        IServiceProvider serviceProvider,
        ILogger<StartupOptimizationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _stopwatch = Stopwatch.StartNew();
    }

    // Step 1: 优化启动
    public async Task OptimizeStartupAsync()
    {
        _logger.LogInformation("Starting application optimization...");

        // 延迟加载策略
        var tasks = new List<Task>
        {
            // 立即加载：关键服务
            Task.Run(() => InitializeCriticalServices()),

            // 延迟加载：非关键服务（100ms延迟）
            Task.Delay(100).ContinueWith(_ => InitializeNonCriticalServices()),

            // 后台加载：可选服务（500ms延迟）
            Task.Delay(500).ContinueWith(_ => InitializeOptionalServices())
        };

        await Task.WhenAll(tasks);
    }

    // Step 2: 预加载关键资源
    public async Task PreloadCriticalResourcesAsync()
    {
        _logger.LogInformation("Preloading critical resources...");

        var tasks = new List<Task>
        {
            // 预加载用户权限
            Task.Run(async () =>
            {
                var authService = _serviceProvider.GetRequiredService<IAuthenticationService>();
                await authService.IsLoggedInAsync();
            }),

            // 预加载系统配置
            Task.Run(async () =>
            {
                var configService = _serviceProvider.GetRequiredService<IConfigurationService>();
                await configService.LoadUserSettings();
            }),

            // 预加载常用字典数据
            Task.Run(async () =>
            {
                var cacheService = _serviceProvider.GetRequiredService<ICacheService>();
                // 预加载常用枚举/字典
                cacheService.Set("dictionaries:loaded", true);
            })
        };

        await Task.WhenAll(tasks);
        _logger.LogInformation("Critical resources preloaded successfully");
    }

    // Step 3: 应用预热
    public async Task WarmupApplicationAsync()
    {
        _logger.LogInformation("Warming up application...");

        var tasks = new List<Task>
        {
            // 预热HttpClient（建立连接）
            Task.Run(async () =>
            {
                var apiService = _serviceProvider.GetRequiredService<IApiService>();
                var healthCheck = _serviceProvider.GetRequiredService<IApiHealthCheckService>();
                await healthCheck.CheckHealthAsync();
            }),

            // 预热数据库连接池
            Task.Run(async () =>
            {
                // 执行一次简单查询，初始化连接池
                // await _dbContext.Database.ExecuteSqlRawAsync("SELECT 1");
            })
        };

        await Task.WhenAll(tasks);
        _logger.LogInformation("Application warmup completed");
    }

    // Step 4: 测量启动时长
    public TimeSpan GetStartupDuration()
    {
        _stopwatch.Stop();
        var duration = _stopwatch.Elapsed;
        _logger.LogInformation("Application startup duration: {Duration}ms",
            duration.TotalMilliseconds);

        // 触发完成事件
        OptimizationCompleted?.Invoke(this, EventArgs.Empty);

        return duration;
    }

    // 关键服务初始化
    private void InitializeCriticalServices()
    {
        _logger.LogDebug("Initializing critical services...");
        // IAuthenticationService, IConfigurationService, ICacheService
    }

    // 非关键服务初始化
    private void InitializeNonCriticalServices()
    {
        _logger.LogDebug("Initializing non-critical services...");
        // IExceptionHandler, DiagnosticService
    }

    // 可选服务初始化
    private void InitializeOptionalServices()
    {
        _logger.LogDebug("Initializing optional services...");
        // IModuleLoadingService（Prism模块延迟加载）
    }

    public void ClearStartupCache()
    {
        _logger.LogInformation("Clearing startup cache...");
        // 清理临时启动数据
    }
}
```

### 7.4 启动流程集成

```csharp
public partial class App : PrismApplication
{
    protected override async void OnInitialized()
    {
        InitializeComponent();

        var startupService = Container.Resolve<IStartupOptimizationService>();

        // 订阅优化完成事件
        startupService.OptimizationCompleted += OnOptimizationCompleted;

        // 执行四步优化流程
        await startupService.OptimizeStartupAsync();         // Step 1
        await startupService.PreloadCriticalResourcesAsync(); // Step 2
        await startupService.WarmupApplicationAsync();        // Step 3
        var duration = startupService.GetStartupDuration();   // Step 4

        // 记录启动时长
        var logger = Container.Resolve<ILogger<App>>();
        logger.LogInformation("Application startup completed. Duration: {Duration}ms",
            duration.TotalMilliseconds);

        // 导航到主视图
        await NavigateToMainViewAsync();
    }

    private void OnOptimizationCompleted(object? sender, EventArgs e)
    {
        // 显示主窗口
        Application.Current.MainWindow?.Show();

        // 启动后台任务
        _ = Task.Run(() => StartBackgroundServices());
    }

    private async Task NavigateToMainViewAsync()
    {
        // 检查登录状态
        var authService = Container.Resolve<IAuthenticationService>();
        if (await authService.IsLoggedInAsync())
        {
            _regionManager.RequestNavigate("ContentRegion", "DashboardView");
        }
        else
        {
            _regionManager.RequestNavigate("ContentRegion", "LoginView");
        }
    }
}
```

### 7.5 性能目标与监控

| 指标 | 理想值 | 可接受值 | 需优化值 |
|-----|-------|---------|---------|
| **启动时长** | <3秒 | 3-5秒 | >5秒 |
| **登录响应** | <1秒 | 1-2秒 | >2秒 |
| **页面切换** | <500ms | 500ms-1秒 | >1秒 |
| **API调用** | <500ms | 500ms-2秒 | >2秒 |
| **缓存命中率** | >80% | 60-80% | <60% |

**性能监控实现**：

```csharp
public class PerformanceMonitor
{
    private readonly ILogger<PerformanceMonitor> _logger;

    public void MonitorStartupPerformance(TimeSpan duration)
    {
        if (duration.TotalSeconds < 3)
        {
            _logger.LogInformation("✅ Startup performance: Excellent ({Duration}s)",
                duration.TotalSeconds);
        }
        else if (duration.TotalSeconds < 5)
        {
            _logger.LogWarning("⚠️ Startup performance: Acceptable ({Duration}s)",
                duration.TotalSeconds);
        }
        else
        {
            _logger.LogError("❌ Startup performance: Needs optimization ({Duration}s)",
                duration.TotalSeconds);
        }
    }
}
```

---

## 8. 异常处理架构

### 8.1 三层异常处理机制

```
┌─────────────────────────────────────────────────────┐
│       Layer 3: 全局捕获（Global Exception Handler）    │
│  - AppDomain.CurrentDomain.UnhandledException       │
│  - TaskScheduler.UnobservedTaskException            │
│  - Dispatcher.UnhandledException（WPF）              │
└─────────────────────────────────────────────────────┘
                      ↓ 补充
┌─────────────────────────────────────────────────────┐
│     Layer 2: ServiceResult封装（统一返回格式）          │
│  - ServiceResult<T>.Success(data)                   │
│  - ServiceResult<T>.Failure(errorMessage)           │
│  - 避免异常传播到UI层                                 │
└─────────────────────────────────────────────────────┘
                      ↓ 辅助
┌─────────────────────────────────────────────────────┐
│    Layer 1: 友好消息映射（User-Friendly Messages）     │
│  - ExceptionMessageMapper.GetFriendlyMessage()      │
│  - 技术异常 → 用户可理解的中文消息                      │
└─────────────────────────────────────────────────────┘
```

### 8.2 IExceptionHandler接口设计

```csharp
public interface IExceptionHandler
{
    // 直接处理异常
    void HandleException(Exception exception);          // 记录日志、显示消息

    // 处理并返回ServiceResult
    ServiceResult<T> HandleException<T>(Exception exception);

    // 安全执行（自动捕获异常）
    Task<ServiceResult<T>> SafeExecuteAsync<T>(Func<Task<T>> operation);
    Task<ServiceResult<bool>> SafeExecuteAsync(Func<Task> operation);
}
```

### 8.3 异常严重级别

```csharp
public enum ExceptionSeverity
{
    Low,        // 低：可恢复的警告（如缓存失效）
    Medium,     // 中：影响单个操作（如API调用失败）
    High,       // 高：影响功能可用性（如登录失败）
    Critical    // 严重：影响系统稳定性（如数据库连接断开）
}
```

### 8.4 友好消息映射

```csharp
public class ExceptionMessageMapper
{
    public static string GetFriendlyMessage(Exception exception)
    {
        return exception switch
        {
            // HTTP异常
            HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.NotFound
                => "请求的资源不存在，请检查操作是否正确",

            HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.Unauthorized
                => "您的登录已过期，请重新登录",

            HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.Forbidden
                => "您没有权限执行此操作",

            HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.BadRequest
                => "请求参数错误，请检查输入数据",

            HttpRequestException
                => "网络连接失败，请检查网络设置或联系管理员",

            // 超时异常
            TaskCanceledException or TimeoutException
                => "请求超时，请稍后重试或检查网络连接",

            // 权限异常
            UnauthorizedAccessException
                => "您没有权限执行此操作，请联系管理员",

            // 参数异常
            ArgumentNullException argEx
                => $"缺少必要参数：{argEx.ParamName}",

            ArgumentException argEx
                => $"参数错误：{argEx.Message}",

            // 操作异常
            InvalidOperationException
                => "操作无效，请检查操作条件是否满足",

            // JSON异常
            System.Text.Json.JsonException
                => "数据格式错误，请联系技术支持",

            // 默认异常
            _
                => $"系统错误：{exception.Message}，请联系管理员"
        };
    }

    public static ExceptionSeverity GetSeverity(Exception exception)
    {
        return exception switch
        {
            TaskCanceledException or TimeoutException
                => ExceptionSeverity.Low,

            ArgumentException
                => ExceptionSeverity.Medium,

            HttpRequestException httpEx when httpEx.StatusCode == HttpStatusCode.Unauthorized
                => ExceptionSeverity.High,

            UnauthorizedAccessException
                => ExceptionSeverity.High,

            _
                => ExceptionSeverity.Critical
        };
    }
}
```

### 8.5 SafeExecuteAsync模式

```csharp
public class StandardExceptionHandler : IExceptionHandler
{
    private readonly ILogger<StandardExceptionHandler> _logger;
    private readonly IDialogService _dialogService;

    // 安全执行：返回数据
    public async Task<ServiceResult<T>> SafeExecuteAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            var result = await operation();
            return ServiceResult<T>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Safe execution failed: {Message}", ex.Message);

            // 获取友好消息
            var friendlyMessage = ExceptionMessageMapper.GetFriendlyMessage(ex);

            // 根据严重级别决定是否显示对话框
            var severity = ExceptionMessageMapper.GetSeverity(ex);
            if (severity >= ExceptionSeverity.High)
            {
                _dialogService.ShowError(friendlyMessage);
            }

            return ServiceResult<T>.Failure(friendlyMessage);
        }
    }

    // 安全执行：无返回数据
    public async Task<ServiceResult<bool>> SafeExecuteAsync(Func<Task> operation)
    {
        try
        {
            await operation();
            return ServiceResult<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Safe execution failed: {Message}", ex.Message);
            var friendlyMessage = ExceptionMessageMapper.GetFriendlyMessage(ex);
            return ServiceResult<bool>.Failure(friendlyMessage);
        }
    }
}
```

### 8.6 ViewModel异常处理示例

```csharp
public class PatientViewModel : ViewModelBase
{
    private readonly IExceptionHandler _exceptionHandler;
    private readonly IPatientRepository _patientRepository;

    // 加载患者数据（SafeExecuteAsync模式）
    private async Task LoadPatientAsync(Guid id)
    {
        IsBusy = true;

        var result = await _exceptionHandler.SafeExecuteAsync(async () =>
        {
            var patient = await _patientRepository.GetByIdAsync(id);
            return patient.Data;
        });

        IsBusy = false;

        if (result.IsSuccess)
        {
            CurrentPatient = result.Data;
            ErrorMessage = null;
        }
        else
        {
            ErrorMessage = result.ErrorMessage; // 已经是友好消息
        }
    }

    // 保存患者数据
    private async Task SavePatientAsync()
    {
        IsBusy = true;

        var result = await _exceptionHandler.SafeExecuteAsync(async () =>
        {
            await _patientRepository.UpdateAsync(CurrentPatient.Id, CurrentPatient);
        });

        IsBusy = false;

        if (result.IsSuccess)
        {
            _dialogService.ShowSuccess("患者信息保存成功");
        }
        else
        {
            _dialogService.ShowError(result.ErrorMessage);
        }
    }
}
```

### 8.7 全局异常捕获

```csharp
public partial class App : PrismApplication
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // 1. AppDomain未处理异常
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var ex = (Exception)args.ExceptionObject;
            _logger.LogCritical(ex, "Unhandled exception in AppDomain");
            HandleUnhandledException(ex);
        };

        // 2. Task未观察异常
        TaskScheduler.UnobservedTaskException += (sender, args) =>
        {
            _logger.LogCritical(args.Exception, "Unobserved task exception");
            HandleUnhandledException(args.Exception);
            args.SetObserved(); // 阻止应用崩溃
        };

        // 3. WPF Dispatcher未处理异常
        Current.DispatcherUnhandledException += (sender, args) =>
        {
            _logger.LogCritical(args.Exception, "Unhandled exception in Dispatcher");
            HandleUnhandledException(args.Exception);
            args.Handled = true; // 阻止应用崩溃
        };

        base.OnStartup(e);
    }

    private void HandleUnhandledException(Exception ex)
    {
        var message = ExceptionMessageMapper.GetFriendlyMessage(ex);
        MessageBox.Show(
            message + "\n\n应用程序将继续运行，但可能不稳定。建议保存工作并重启。",
            "系统错误",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }
}
```

---

## 9. 健康检查架构

### 9.1 IApiHealthCheckService接口设计

```csharp
public interface IApiHealthCheckService
{
    // 检查API健康状态
    Task<ApiHealthStatus> CheckHealthAsync();

    // 最后错误信息
    string? LastErrorMessage { get; }
}

public enum ApiHealthStatus
{
    Healthy,       // 健康：API可用
    Degraded,      // 降级：API响应慢（>2秒）
    Unhealthy      // 不健康：API不可用
}
```

### 9.2 健康检查实现

```csharp
public class ApiHealthCheckService : IApiHealthCheckService
{
    private readonly IApiService _apiService;
    private readonly ILogger<ApiHealthCheckService> _logger;

    public string? LastErrorMessage { get; private set; }

    public ApiHealthCheckService(
        IApiService apiService,
        ILogger<ApiHealthCheckService> logger)
    {
        _apiService = apiService;
        _logger = logger;
    }

    public async Task<ApiHealthStatus> CheckHealthAsync()
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 调用健康检查端点
            var result = await _apiService.GetAsync<HealthCheckResult>(
                "api/v1/health");

            stopwatch.Stop();
            var responseTime = stopwatch.Elapsed;

            if (!result.IsSuccess)
            {
                LastErrorMessage = result.ErrorMessage;
                _logger.LogWarning("Health check failed: {Error}", result.ErrorMessage);
                return ApiHealthStatus.Unhealthy;
            }

            // 判断响应时间
            if (responseTime.TotalSeconds > 2)
            {
                LastErrorMessage = $"API响应慢（{responseTime.TotalSeconds:F2}秒）";
                _logger.LogWarning("Health check degraded: Response time {ResponseTime}ms",
                    responseTime.TotalMilliseconds);
                return ApiHealthStatus.Degraded;
            }

            LastErrorMessage = null;
            _logger.LogDebug("Health check passed. Response time: {ResponseTime}ms",
                responseTime.TotalMilliseconds);
            return ApiHealthStatus.Healthy;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            LastErrorMessage = ex.Message;
            _logger.LogError(ex, "Health check exception");
            return ApiHealthStatus.Unhealthy;
        }
    }
}
```

### 9.3 定时健康检查

```csharp
public class HealthMonitorService
{
    private readonly IApiHealthCheckService _healthCheck;
    private readonly ILogger<HealthMonitorService> _logger;
    private Timer? _healthCheckTimer;

    public event EventHandler<ApiHealthStatus>? HealthStatusChanged;

    public void StartMonitoring(TimeSpan interval)
    {
        _healthCheckTimer = new Timer(async _ =>
        {
            var status = await _healthCheck.CheckHealthAsync();
            _logger.LogInformation("Health status: {Status}", status);

            // 触发状态变更事件
            HealthStatusChanged?.Invoke(this, status);

        }, null, TimeSpan.Zero, interval); // 立即执行，然后每隔interval执行一次
    }

    public void StopMonitoring()
    {
        _healthCheckTimer?.Dispose();
        _healthCheckTimer = null;
    }
}

// 在App.xaml.cs中启动
public partial class App : PrismApplication
{
    protected override void OnInitialized()
    {
        var healthMonitor = Container.Resolve<HealthMonitorService>();

        // 每30秒检查一次健康状态
        healthMonitor.StartMonitoring(TimeSpan.FromSeconds(30));

        // 订阅状态变更事件
        healthMonitor.HealthStatusChanged += OnHealthStatusChanged;

        base.OnInitialized();
    }

    private void OnHealthStatusChanged(object? sender, ApiHealthStatus status)
    {
        if (status == ApiHealthStatus.Unhealthy)
        {
            // 显示离线提示
            _dialogService.ShowWarning("服务器连接中断，请检查网络连接");
        }
        else if (status == ApiHealthStatus.Degraded)
        {
            // 显示性能警告
            _logger.LogWarning("API响应变慢，请注意性能问题");
        }
    }
}
```

---

## 10. 诊断与监控架构

### 10.1 DiagnosticService职责

- ✅ **结构化日志**：JSON格式、上下文信息、性能指标
- ✅ **错误追踪**：异常链、调用栈、用户操作轨迹
- ✅ **性能监控**：API耗时、缓存命中率、启动时长
- ✅ **健康监控**：服务状态、资源使用、连接状态

### 10.2 结构化日志示例

```csharp
public class DiagnosticService
{
    private readonly ILogger<DiagnosticService> _logger;

    // 记录API调用
    public void LogApiCall(string method, string endpoint, TimeSpan duration, bool success)
    {
        _logger.LogInformation(
            "API call: {Method} {Endpoint} completed in {Duration}ms. Success: {Success}",
            method, endpoint, duration.TotalMilliseconds, success);
    }

    // 记录缓存命中/未命中
    public void LogCacheAccess(string key, bool hit)
    {
        if (hit)
        {
            _logger.LogDebug("Cache hit: {Key}", key);
        }
        else
        {
            _logger.LogDebug("Cache miss: {Key}", key);
        }
    }

    // 记录性能指标
    public void LogPerformanceMetric(string operation, TimeSpan duration)
    {
        if (duration.TotalSeconds > 2)
        {
            _logger.LogWarning(
                "Performance issue: {Operation} took {Duration}ms (threshold: 2000ms)",
                operation, duration.TotalMilliseconds);
        }
        else
        {
            _logger.LogDebug(
                "Performance: {Operation} took {Duration}ms",
                operation, duration.TotalMilliseconds);
        }
    }
}
```

---

## 11. Foundation vs Infrastructure分离

### 11.1 职责边界

| 维度 | Foundation（当前层） | Infrastructure（WPF UI层） |
|-----|---------------------|---------------------------|
| **定位** | 平台无关技术基础设施 | WPF专用UI基础组件 |
| **平台依赖** | ❌ 无WPF依赖，纯.NET 8 | ✅ 强依赖WPF（System.Windows.*） |
| **核心能力** | HTTP/缓存/配置/安全/性能/诊断 | 自定义Controls/Converters/Behaviors |
| **复用范围** | 跨平台（Desktop/Avalonia/MAUI） | 仅WPF Desktop |
| **依赖方向** | 被Infrastructure依赖 | 依赖Foundation |
| **典型组件** | `ApiService`、`CacheService`、`AuthenticationService` | `SessionManager`、`DialogService`、`RegionBehaviors` |

### 11.2 依赖关系图

```
┌────────────────────────────────────────────────┐
│        Client端依赖层次（由上至下）                │
└────────────────────────────────────────────────┘
         │
         │ 业务逻辑
         ↓
┌────────────────────────────────────────────────┐
│     Modules层（Auth/Patients/Consultation...）  │
│     - 业务ViewModel                             │
│     - 业务Repository                            │
│     - 业务Service                               │
└────────────────────────────────────────────────┘
         │
         │ UI基础能力
         ↓
┌────────────────────────────────────────────────┐
│   Infrastructure层（WPF UI基础组件）⭐           │
│   - SessionManager（会话管理）                   │
│   - DialogService（对话框服务）                  │
│   - RegionBehaviors（区域行为）                  │
│   - CustomControls（自定义控件）                 │
│   - Converters（值转换器）                       │
│   ⚠️ 强依赖WPF框架                              │
└────────────────────────────────────────────────┘
         │
         │ 技术基础能力
         ↓
┌────────────────────────────────────────────────┐
│   ✨ Foundation层（平台无关技术基础）✨            │
│   - ApiService（HTTP通信）                       │
│   - CacheService（缓存管理）                     │
│   - AuthenticationService（认证安全）            │
│   - ConfigurationService（配置管理）             │
│   - ExceptionHandler（异常处理）                 │
│   ✅ 无WPF依赖，可跨平台复用                      │
└────────────────────────────────────────────────┘
         │
         │ 跨端共享
         ↓
┌────────────────────────────────────────────────┐
│   Shared层（跨端共享DTO和组件）                   │
│   - DTO模型（UserDto/PatientDto...）             │
│   - 业务组件（HerbCalculator/Validator）         │
└────────────────────────────────────────────────┘
```

### 11.3 为什么需要Foundation/Infrastructure分离？

**问题场景**：
- ❌ **不分离**：`Infrastructure`既包含HTTP/缓存（技术能力），又包含Controls/Converters（UI能力）
- ❌ **后果**：Avalonia端需要HTTP/缓存，但不能引用`Infrastructure`（因为包含WPF依赖）

**解决方案**：
- ✅ **Foundation层**：提取平台无关的技术能力（HTTP/缓存/配置/安全）
- ✅ **Infrastructure层**：保留WPF专用的UI能力（Controls/Converters/Behaviors）
- ✅ **Avalonia端**：只引用Foundation，不引用Infrastructure

**跨平台复用示例**：

```csharp
// Desktop端（WPF）
public class DesktopPatientModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 使用Foundation服务
        containerRegistry.RegisterSingleton<IApiService, ApiService>();
        containerRegistry.RegisterSingleton<ICacheService, CacheService>();

        // 使用Infrastructure服务（WPF专用）
        containerRegistry.RegisterSingleton<IDialogService, DialogService>();
        containerRegistry.RegisterSingleton<ISessionManager, SessionManager>();
    }
}

// Avalonia端（跨平台）
public class AvaloniaPatientModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // ✅ 复用Foundation服务（无需修改）
        containerRegistry.RegisterSingleton<IApiService, ApiService>();
        containerRegistry.RegisterSingleton<ICacheService, CacheService>();

        // ✅ 实现Avalonia专用的UI服务
        containerRegistry.RegisterSingleton<IDialogService, AvaloniaDialogService>();
        containerRegistry.RegisterSingleton<ISessionManager, AvaloniaSessionManager>();
    }
}
```

---

## 12. 依赖注入注册

### 12.1 FoundationServiceCollectionExtensions

```csharp
public static class FoundationServiceCollectionExtensions
{
    public static IServiceCollection AddFoundationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // 1. 核心服务（单例）
        services.AddSingleton<ICacheService, CacheService>();
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddTransient<IAuthenticationService, AuthenticationService>();
        services.AddSingleton<IExceptionHandler, StandardExceptionHandler>();

        // 2. HTTP服务（带Polly策略）
        services.AddHttpClient<IApiService, ApiService>(client =>
        {
            // Issue #1726: 使用新配置路径
            var baseUrl = configuration["Lybt:Client:Api:BaseUrl"] ?? "https://localhost:5001";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(configuration.GetValue<int>("Lybt:Client:Api:TimeoutSeconds", 30));
        })
        .AddHttpMessageHandler<AuthorizationMessageHandler>() // 自动JWT认证
        .AddPolicyHandler(GetRetryPolicy())                    // 重试策略
        .AddPolicyHandler(GetCircuitBreakerPolicy());         // 熔断器策略

        // 3. 安全服务
        services.AddSingleton<ISecureCredentialStorage, SecureCredentialStorage>();
        services.AddSingleton<ITokenStorageService, TokenStorageService>();
        services.AddSingleton<IUsernameStorageService, UsernameStorageService>();
        services.AddTransient<SecurityService>();

        // 4. 性能优化
        services.AddSingleton<IStartupOptimizationService, StartupOptimizationService>();
        services.AddSingleton<IModuleLoadingService, ModuleLoadingService>();

        // 5. 诊断与健康检查
        services.AddSingleton<DiagnosticService>();
        services.AddSingleton<IApiHealthCheckService, ApiHealthCheckService>();

        // 6. 设置服务
        services.AddSingleton<SettingsService>();

        return services;
    }

    // Polly重试策略（指数退避）
    private static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError() // 处理5xx和408错误
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))); // 2秒、4秒、8秒
    }

    // Polly熔断器策略
    private static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
    {
        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 5,  // 5次失败后开路
                durationOfBreak: TimeSpan.FromSeconds(30) // 开路30秒
            );
    }
}
```

### 12.2 Prism应用集成

```csharp
public partial class App : PrismApplication
{
    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册Foundation服务
        var services = new ServiceCollection();
        services.AddFoundationServices(Configuration);

        // 转换为Prism容器
        var serviceProvider = services.BuildServiceProvider();
        containerRegistry.RegisterInstance(serviceProvider);
    }

    protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
    {
        // 业务模块注册（依赖Foundation服务）
        moduleCatalog.AddModule<AuthModule>();
        moduleCatalog.AddModule<PatientModule>();
        moduleCatalog.AddModule<ConsultationModule>();
    }
}
```

---

## 13. 参考资料

### 13.1 内部文档

| 文档类型 | 文档路径 | 说明 |
|---------|---------|-----|
| **项目README** | `src/Client/Desktop/Core/LYBT.Desktop.Foundation/README.md` | Foundation层完整项目文档（1192行） |
| **Client架构总览** | `docs/explanation/architecture/client/README.md` | Client端五层架构设计 |
| **Server架构总览** | `docs/explanation/architecture/server/README.md` | Server端三层架构设计 |
| **Shared架构** | `docs/explanation/architecture/shared/README.md` | 跨端共享架构设计 |
| **WebAPI设计** | `docs/explanation/architecture/server/webapi-design.md` | WebAPI架构设计（与Foundation对应） |
| **Infrastructure设计** | `docs/explanation/architecture/client/infrastructure-design.md` | Infrastructure层架构（Foundation的上层） |
| **Models设计** | `docs/explanation/architecture/client/models-layer-design.md` | Client端Models层设计 |

### 13.2 技术栈参考

| 技术 | 官方文档 | 说明 |
|-----|---------|-----|
| **.NET 8** | https://learn.microsoft.com/dotnet/core/ | 基础框架 |
| **HttpClientFactory** | https://learn.microsoft.com/aspnet/core/fundamentals/http-requests | HTTP客户端管理 |
| **Polly** | https://github.com/App-vNext/Polly | 弹性和瞬态故障处理 |
| **IMemoryCache** | https://learn.microsoft.com/aspnet/core/performance/caching/memory | 内存缓存 |
| **IConfiguration** | https://learn.microsoft.com/dotnet/core/extensions/configuration | 配置系统 |
| **DPAPI** | https://learn.microsoft.com/dotnet/api/system.security.cryptography.protecteddata | Windows数据保护 |
| **Prism** | https://prismlibrary.com/ | MVVM框架 |

### 13.3 设计模式参考

| 模式名称 | 应用场景 | Foundation实现 |
|---------|---------|---------------|
| **Repository模式** | 数据访问抽象 | `BaseApiRepository<TDto>` |
| **策略模式** | 弹性策略配置 | Polly Retry/CircuitBreaker |
| **工厂模式** | HttpClient创建 | `HttpClientFactory` |
| **装饰器模式** | HTTP请求拦截 | `AuthorizationMessageHandler` |
| **单例模式** | 全局服务 | `CacheService`、`ConfigurationService` |
| **观察者模式** | 事件通知 | `OptimizationCompleted事件` |
| **模板方法模式** | CRUD模板 | `BaseApiRepository.GetAllAsync()` |
| **安全执行模式** | 异常处理 | `SafeExecuteAsync<T>()` |

---

**文档维护**：Client端架构团队
**最后更新**：2025-10-29
**相关Epic**：#1718 - Phase 1架构文档补充
