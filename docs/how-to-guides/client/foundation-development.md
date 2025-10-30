# Client端 Foundation 层开发指南

> **目标读者**：Client端（Desktop/Avalonia）开发者
> **前置知识**：熟悉 .NET 8、WPF、Prism、依赖注入、异步编程
> **关联文档**：[Foundation架构设计](../../explanation/architecture/client/foundation-design.md)

---

## 📋 文档概览

本指南提供 **Foundation 层**（平台无关技术基础设施层）的实战开发指导，覆盖HTTP客户端、缓存、认证、配置、异常处理、启动优化等核心服务的开发实践。

**关键特性**：
- 🌐 **平台无关**：无WPF依赖，可跨平台复用（Desktop/Avalonia/MAUI）
- 🔄 **Polly弹性策略**：自动重试、熔断器、超时控制
- 🔐 **JWT自动认证**：HTTP拦截器自动注入Bearer token
- 💾 **智能缓存**：内存缓存 + 自动失效策略
- 🚀 **启动优化**：延迟加载、预加载、预热机制
- 🛡️ **三层异常处理**：友好消息映射 + 全局捕获

---

## 第1章：开发流程总览

### 1.1 Foundation层定位

```
┌─────────────────────────────────────────┐
│         Presentation Layer              │  ← WPF Views/ViewModels
│      (LYBT.Desktop.{Module})            │
├─────────────────────────────────────────┤
│      Infrastructure Layer (WPF专用)     │  ← DialogService, PrintService
│      (LYBT.Desktop.Infrastructure)      │
├─────────────────────────────────────────┤
│      Foundation Layer (平台无关) ⭐     │  ← HTTP客户端、缓存、认证
│      (LYBT.Desktop.Foundation)          │
├─────────────────────────────────────────┤
│         Models Layer                    │  ← ViewModels, DTOs
│      (LYBT.Desktop.Models)              │
└─────────────────────────────────────────┘
```

**职责边界**：
- ✅ **Foundation负责**：HTTP通信、缓存管理、认证授权、配置管理、异常处理、启动优化
- ✅ **Infrastructure负责**：WPF对话框、打印、主题、数据绑定转换器（依赖System.Windows.*）
- ❌ **Foundation禁止**：引用WPF类型、引用Presentation层、直接操作UI

### 1.2 典型开发流程（8步）

```
Step 1: 创建 Repository 继承 BaseApiRepository<TDto>
  ↓
Step 2: 注入 IApiService + ILogger + endpoint
  ↓
Step 3: 实现 Service 层（调用 Repository）
  ↓
Step 4: 在 ViewModel 中注入 Service
  ↓
Step 5: 使用 SafeExecuteAsync 包装异常
  ↓
Step 6: 配置依赖注入（注册到容器）
  ↓
Step 7: 验证 Polly 重试策略生效
  ↓
Step 8: 验证缓存命中率和认证流程
```

### 1.3 快速实践示例

**场景**：为"患者管理"模块创建Repository和Service

```csharp
// Step 1: 创建 PatientRepository.cs
public class PatientRepository : BaseApiRepository<PatientDto>
{
    public PatientRepository(
        IApiService apiService,
        ILogger<PatientRepository> logger)
        : base(apiService, logger, "/api/v1/patients")
    {
    }

    // 自动继承7个CRUD方法：
    // - GetAllAsync()
    // - GetPagedAsync(page, pageSize, keyword)
    // - GetByIdAsync(id)
    // - CreateAsync(entity)
    // - UpdateAsync(id, entity)
    // - DeleteAsync(id)
    // - SearchAsync(keyword)
}

// Step 2: 创建 PatientService.cs
public class PatientService : IPatientService
{
    private readonly PatientRepository _repository;
    private readonly ICacheService _cache;
    private readonly IExceptionHandler _exceptionHandler;

    public PatientService(
        PatientRepository repository,
        ICacheService cache,
        IExceptionHandler exceptionHandler)
    {
        _repository = repository;
        _cache = cache;
        _exceptionHandler = exceptionHandler;
    }

    public async Task<ServiceResult<List<PatientDto>>> GetAllPatientsAsync()
    {
        return await _exceptionHandler.SafeExecuteAsync(async () =>
        {
            // Cache-First 模式
            return await _cache.GetOrCreateAsync(
                "patients:all",
                () => _repository.GetAllAsync(),
                expiry: TimeSpan.FromMinutes(5));
        });
    }
}

// Step 3: 注册到容器（在 PatientsModule.cs）
public class PatientsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        containerRegistry.RegisterSingleton<PatientRepository>();
        containerRegistry.RegisterScoped<IPatientService, PatientService>();
    }
}
```

---

## 第2章：环境准备

### 2.1 必需NuGet包

```xml
<!-- LYBT.Desktop.Foundation.csproj -->
<ItemGroup>
  <!-- HTTP客户端 -->
  <PackageReference Include="Microsoft.Extensions.Http" Version="8.0.0" />
  <PackageReference Include="Microsoft.Extensions.Http.Polly" Version="8.0.0" />

  <!-- Polly弹性策略 -->
  <PackageReference Include="Polly" Version="8.2.0" />
  <PackageReference Include="Polly.Extensions.Http" Version="3.0.0" />

  <!-- 缓存 -->
  <PackageReference Include="Microsoft.Extensions.Caching.Memory" Version="8.0.0" />

  <!-- 配置 -->
  <PackageReference Include="Microsoft.Extensions.Configuration" Version="8.0.0" />
  <PackageReference Include="Microsoft.Extensions.Configuration.Json" Version="8.0.0" />

  <!-- 日志 -->
  <PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />

  <!-- 依赖注入 -->
  <PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="8.0.0" />
</ItemGroup>

<!-- 项目引用 -->
<ItemGroup>
  <ProjectReference Include="..\..\Shared\LYBT.Shared.Models\LYBT.Shared.Models.csproj" />
  <ProjectReference Include="..\..\Shared\LYBT.Shared.Components\LYBT.Shared.Components.csproj" />
  <ProjectReference Include="..\Models\LYBT.Desktop.Models\LYBT.Desktop.Models.csproj" />
</ItemGroup>
```

### 2.2 appsettings.json 配置

```json
{
  "ApiSettings": {
    "BaseUrl": "https://localhost:5001",
    "Timeout": 30,
    "RetryCount": 3,
    "RetryDelaySeconds": 2
  },
  "CacheSettings": {
    "DefaultExpirationMinutes": 5,
    "MaxCacheSize": 100,
    "EnableCaching": true
  },
  "PollySettings": {
    "RetryCount": 3,
    "BaseDelaySeconds": 2,
    "TimeoutSeconds": 30,
    "CircuitBreakerThreshold": 5,
    "CircuitBreakerDurationSeconds": 30
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "LYBT.Desktop.Foundation": "Debug",
      "System.Net.Http": "Warning"
    }
  }
}
```

### 2.3 验证环境准备

**编译测试**：
```bash
# 清理并构建 Foundation 项目
dotnet clean src/Client/Desktop/Core/LYBT.Desktop.Foundation/LYBT.Desktop.Foundation.csproj
dotnet build src/Client/Desktop/Core/LYBT.Desktop.Foundation/LYBT.Desktop.Foundation.csproj -c Release
# 预期: 0 errors, 0 warnings
```

**依赖验证**：
```bash
# 检查 Foundation 不依赖 WPF
dotnet list src/Client/Desktop/Core/LYBT.Desktop.Foundation/LYBT.Desktop.Foundation.csproj reference
# 预期: 无 System.Windows.* 引用
```

---

## 第3章：HTTP客户端三层抽象使用

### 3.1 三层抽象架构

```
Level 1: IApiService
  ↓
  7个HTTP基础方法
  ├── GetAsync<TResponse>(endpoint, parameters)
  ├── PostAsync<TRequest, TResponse>(endpoint, request)
  ├── PutAsync<TRequest, TResponse>(endpoint, request)
  ├── PatchAsync<TRequest, TResponse>(endpoint, request)
  ├── DeleteAsync(endpoint)
  ├── DownloadAsync(endpoint)
  └── UploadAsync<TResponse>(endpoint, file, fileName, metadata)

Level 2: ApiService<TEntity>
  ↓
  5个CRUD快捷方法
  ├── GetAllAsync()
  ├── GetByIdAsync(id)
  ├── CreateAsync(entity)
  ├── UpdateAsync(id, entity)
  └── DeleteAsync(id)

Level 3: BaseApiRepository<TDto>
  ↓
  7个仓储方法（业务模块继承）
  ├── GetAllAsync()
  ├── GetPagedAsync(page, pageSize, keyword)
  ├── GetByIdAsync(id)
  ├── CreateAsync(entity)
  ├── UpdateAsync(id, entity)
  ├── DeleteAsync(id)
  └── SearchAsync(keyword)
```

### 3.2 Level 1：IApiService 直接使用

**适用场景**：
- ✅ 非标准CRUD操作（如批量删除、导入导出）
- ✅ 自定义端点格式
- ✅ 需要完全控制HTTP请求

**示例1：批量删除患者**
```csharp
public class PatientRepository : BaseApiRepository<PatientDto>
{
    public async Task<bool> BatchDeleteAsync(List<Guid> patientIds)
    {
        // 使用继承的 _apiService 访问 IApiService
        var result = await _apiService.PostAsync<List<Guid>, bool>(
            "/api/v1/patients/batch-delete",
            patientIds);
        return result ?? false;
    }
}
```

**示例2：下载患者Excel导出**
```csharp
public async Task<Stream> ExportPatientsAsync()
{
    // 直接使用 DownloadAsync
    var stream = await _apiService.DownloadAsync("/api/v1/patients/export");
    return stream;
}
```

**示例3：上传患者头像**
```csharp
public async Task<PatientDto?> UploadAvatarAsync(Guid patientId, Stream avatarStream, string fileName)
{
    var metadata = new Dictionary<string, string>
    {
        { "patientId", patientId.ToString() }
    };

    return await _apiService.UploadAsync<PatientDto>(
        "/api/v1/patients/upload-avatar",
        avatarStream,
        fileName,
        metadata);
}
```

### 3.3 Level 2：ApiService<TEntity> 快捷使用

**适用场景**：
- ✅ 标准CRUD操作
- ✅ 不需要扩展Repository
- ✅ 快速原型开发

**示例：直接在ViewModel中使用**
```csharp
public class QuickPatientViewModel : BindableBase
{
    private readonly ApiService<PatientDto> _patientApi;

    public QuickPatientViewModel(IApiService baseApiService)
    {
        // 创建泛型包装器
        _patientApi = new ApiService<PatientDto>(baseApiService, "/api/v1/patients");
    }

    public async Task LoadPatientsAsync()
    {
        // 使用快捷方法
        var patients = await _patientApi.GetAllAsync();
        if (patients != null)
        {
            Patients = new ObservableCollection<PatientDto>(patients);
        }
    }

    public async Task CreatePatientAsync(PatientDto patient)
    {
        var created = await _patientApi.CreateAsync(patient);
        if (created != null)
        {
            Patients.Add(created);
        }
    }
}
```

### 3.4 Level 3：BaseApiRepository<TDto> 继承扩展（⭐推荐）

**适用场景**：
- ✅ 标准业务模块（90%场景）
- ✅ 需要日志记录
- ✅ 需要扩展自定义方法

**完整示例：PatientRepository**
```csharp
public class PatientRepository : BaseApiRepository<PatientDto>
{
    public PatientRepository(
        IApiService apiService,
        ILogger<PatientRepository> logger)
        : base(apiService, logger, "/api/v1/patients")
    {
    }

    // 1. 直接使用继承的7个方法（无需实现）
    // - GetAllAsync()
    // - GetPagedAsync(page, pageSize, keyword)
    // - GetByIdAsync(id)
    // - CreateAsync(entity)
    // - UpdateAsync(id, entity)
    // - DeleteAsync(id)
    // - SearchAsync(keyword)

    // 2. 扩展自定义方法
    public async Task<List<PatientDto>> GetPatientsByAgeRangeAsync(int minAge, int maxAge)
    {
        var parameters = new { minAge, maxAge };
        var result = await _apiService.GetAsync<List<PatientDto>>(
            $"{_endpoint}/by-age-range",
            parameters);
        return result ?? new List<PatientDto>();
    }

    // 3. 重写基类方法以添加自定义逻辑
    public override async Task<PatientDto> CreateAsync(PatientDto entity)
    {
        _logger.LogInformation("创建患者: {Name}", entity.Name);

        // 调用基类实现
        var created = await base.CreateAsync(entity);

        _logger.LogInformation("患者创建成功: ID={Id}", created?.Id);
        return created!;
    }
}
```

**BaseApiRepository 提供的7个方法详解**：

| 方法 | 签名 | 说明 | HTTP请求 |
|------|------|------|----------|
| GetAllAsync | `Task<List<T>> GetAllAsync()` | 查询所有实体 | `GET /api/v1/{endpoint}` |
| GetPagedAsync | `Task<PagedResult<T>> GetPagedAsync(int page, int pageSize, string? keyword)` | 分页查询 | `GET /api/v1/{endpoint}?page=1&pageSize=20&keyword=xxx` |
| GetByIdAsync | `Task<T> GetByIdAsync(Guid id)` | 按ID查询 | `GET /api/v1/{endpoint}/{id}` |
| CreateAsync | `Task<T> CreateAsync(T entity)` | 创建实体 | `POST /api/v1/{endpoint}` (Body: entity) |
| UpdateAsync | `Task<T> UpdateAsync(Guid id, T entity)` | 更新实体 | `PUT /api/v1/{endpoint}/{id}` (Body: entity) |
| DeleteAsync | `Task<bool> DeleteAsync(Guid id)` | 删除实体 | `DELETE /api/v1/{endpoint}/{id}` |
| SearchAsync | `Task<List<T>> SearchAsync(string keyword)` | 搜索实体 | `GET /api/v1/{endpoint}/search?keyword=xxx` |

---

## 第4章：依赖注入与构造函数

### 4.1 Foundation层依赖注入注册

**FoundationServiceCollectionExtensions.cs**（完整注册示例）：
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
            var baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:5001";
            client.BaseAddress = new Uri(baseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        })
        .AddHttpMessageHandler<AuthorizationMessageHandler>() // 自动JWT认证
        .AddPolicyHandler(GetRetryPolicy())                    // 重试策略
        .AddPolicyHandler(GetCircuitBreakerPolicy());         // 熔断器策略

        // 3. 内存缓存（带大小限制）
        services.AddMemoryCache(options =>
        {
            options.SizeLimit = 100; // 最多100个缓存条目
        });

        // 4. 安全服务
        services.AddSingleton<ISecureCredentialStorage, SecureCredentialStorage>();
        services.AddSingleton<ITokenStorageService, TokenStorageService>();
        services.AddTransient<AuthorizationMessageHandler>();

        // 5. 性能优化
        services.AddSingleton<IStartupOptimizationService, StartupOptimizationService>();

        // 6. 健康检查
        services.AddSingleton<IApiHealthCheckService, ApiHealthCheckService>();

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

### 4.2 业务模块依赖注入（PatientsModule示例）

```csharp
public class PatientsModule : IModule
{
    public void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 1. Repository（单例，可复用）
        containerRegistry.RegisterSingleton<PatientRepository>();

        // 2. Service（作用域，按需创建）
        containerRegistry.RegisterScoped<IPatientService, PatientService>();

        // 3. ViewModel（瞬态，每次导航创建）
        containerRegistry.RegisterForNavigation<PatientListView, PatientListViewModel>();
        containerRegistry.RegisterForNavigation<PatientDetailView, PatientDetailViewModel>();

        // 4. Dialog（瞬态）
        containerRegistry.RegisterDialog<PatientEditDialog, PatientEditDialogViewModel>();
    }
}
```

### 4.3 构造函数注入最佳实践

**✅ 推荐：显式依赖**
```csharp
public class PatientService : IPatientService
{
    private readonly PatientRepository _repository;
    private readonly ICacheService _cache;
    private readonly IExceptionHandler _exceptionHandler;
    private readonly ILogger<PatientService> _logger;

    // 明确声明所有依赖
    public PatientService(
        PatientRepository repository,
        ICacheService cache,
        IExceptionHandler exceptionHandler,
        ILogger<PatientService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _exceptionHandler = exceptionHandler ?? throw new ArgumentNullException(nameof(exceptionHandler));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }
}
```

**❌ 禁止：ServiceLocator反模式**
```csharp
// ❌ 反模式1：直接解析容器
public class BadPatientService
{
    private readonly IContainerProvider _container;

    public BadPatientService(IContainerProvider container)
    {
        _container = container;
    }

    public async Task DoSomething()
    {
        // ❌ 隐藏依赖，难以测试
        var repository = _container.Resolve<PatientRepository>();
    }
}

// ❌ 反模式2：静态Container.Resolve
public class WorsePatientService
{
    public async Task DoSomething()
    {
        // ❌ 完全违反DI原则
        var repository = Container.Resolve<PatientRepository>();
    }
}
```

### 4.4 生命周期选择指南

| 生命周期 | 适用场景 | Foundation示例 |
|---------|---------|---------------|
| **Singleton** | 无状态服务，全局共享 | `ICacheService`, `IConfigurationService`, `IApiHealthCheckService` |
| **Scoped** | 每个业务范围一个实例 | `IPatientService`, `IMedicalCaseService`（需要Session级别隔离） |
| **Transient** | 轻量级对象，每次创建 | `IAuthenticationService`, `ExceptionHandler`, `AuthorizationMessageHandler` |

**判断原则**：
- ✅ **无状态 + 高频使用** → Singleton
- ✅ **有状态 + 需要隔离** → Scoped
- ✅ **轻量级 + 短生命周期** → Transient

---

## 第5章：IApiService 使用模式

### 5.1 GET 请求模式

**模式1：无参数查询**
```csharp
public async Task<List<PatientDto>> GetAllPatientsAsync()
{
    var result = await _apiService.GetAsync<List<PatientDto>>("/api/v1/patients");
    return result ?? new List<PatientDto>();
}
```

**模式2：查询参数对象**
```csharp
public async Task<PagedResult<PatientDto>> GetPagedPatientsAsync(int page, int pageSize, string? keyword)
{
    var parameters = new
    {
        page,
        pageSize,
        keyword
    };

    // 自动构建查询字符串: /api/v1/patients?page=1&pageSize=20&keyword=xxx
    var result = await _apiService.GetAsync<PagedResult<PatientDto>>(
        "/api/v1/patients",
        parameters);

    return result ?? new PagedResult<PatientDto>();
}
```

**模式3：手动构建查询字符串**
```csharp
public async Task<List<PatientDto>> SearchPatientsByNameAsync(string name)
{
    var endpoint = $"/api/v1/patients/search?name={Uri.EscapeDataString(name)}";
    var result = await _apiService.GetAsync<List<PatientDto>>(endpoint);
    return result ?? new List<PatientDto>();
}
```

### 5.2 POST 请求模式

**模式1：标准创建**
```csharp
public async Task<PatientDto?> CreatePatientAsync(CreatePatientDto request)
{
    return await _apiService.PostAsync<CreatePatientDto, PatientDto>(
        "/api/v1/patients",
        request);
}
```

**模式2：批量操作**
```csharp
public async Task<BatchResult> BatchCreatePatientsAsync(List<CreatePatientDto> patients)
{
    return await _apiService.PostAsync<List<CreatePatientDto>, BatchResult>(
        "/api/v1/patients/batch",
        patients);
}
```

**模式3：自定义操作**
```csharp
public async Task<bool> ArchivePatientAsync(Guid patientId)
{
    var request = new { patientId };
    var result = await _apiService.PostAsync<object, bool>(
        "/api/v1/patients/archive",
        request);
    return result ?? false;
}
```

### 5.3 PUT/PATCH 请求模式

**PUT（完整更新）**
```csharp
public async Task<PatientDto?> UpdatePatientAsync(Guid id, UpdatePatientDto request)
{
    return await _apiService.PutAsync<UpdatePatientDto, PatientDto>(
        $"/api/v1/patients/{id}",
        request);
}
```

**PATCH（部分更新）**
```csharp
public async Task<PatientDto?> UpdatePatientPhoneAsync(Guid id, string newPhone)
{
    var request = new { phone = newPhone };
    return await _apiService.PatchAsync<object, PatientDto>(
        $"/api/v1/patients/{id}",
        request);
}
```

### 5.4 DELETE 请求模式

```csharp
public async Task<bool> DeletePatientAsync(Guid id)
{
    return await _apiService.DeleteAsync($"/api/v1/patients/{id}");
}

public async Task<bool> BatchDeleteAsync(List<Guid> ids)
{
    var result = await _apiService.PostAsync<List<Guid>, bool>(
        "/api/v1/patients/batch-delete",
        ids);
    return result ?? false;
}
```

### 5.5 文件上传/下载模式

**上传文件**
```csharp
public async Task<PatientDto?> UploadAvatarAsync(Guid patientId, Stream fileStream, string fileName)
{
    var metadata = new Dictionary<string, string>
    {
        { "patientId", patientId.ToString() },
        { "uploadTime", DateTime.Now.ToString("o") }
    };

    return await _apiService.UploadAsync<PatientDto>(
        "/api/v1/patients/upload-avatar",
        fileStream,
        fileName,
        metadata);
}
```

**下载文件**
```csharp
public async Task<Stream> DownloadPatientReportAsync(Guid patientId)
{
    return await _apiService.DownloadAsync($"/api/v1/patients/{patientId}/report");
}

// 使用示例
public async Task SaveReportToFileAsync(Guid patientId, string savePath)
{
    using var stream = await DownloadPatientReportAsync(patientId);
    using var fileStream = File.Create(savePath);
    await stream.CopyToAsync(fileStream);
}
```

### 5.6 自动缓存机制（ApiService 内置）

**GET请求自动缓存**：
```csharp
// ApiService 内部实现（开发者无感）
public async Task<TResponse?> GetAsync<TResponse>(
    string endpoint,
    object? parameters = null,
    CancellationToken cancellationToken = default)
    where TResponse : class
{
    var url = BuildUrl(endpoint, parameters);

    // 1. 尝试从缓存获取
    if (_cache != null)
    {
        var cacheKey = $"GET:{url}";
        var cached = _cache.Get<TResponse>(cacheKey);
        if (cached != null)
        {
            _logger?.LogDebug($"缓存命中: {url}");
            return cached;
        }
    }

    // 2. 去重处理（避免并发重复请求）
    return await _deduplicator.ExecuteAsync(url, async () =>
    {
        // 3. 执行HTTP请求（带Polly重试）
        using var response = await _retryPolicy.ExecuteAsync(async () =>
            await _httpClient.GetAsync(url, cancellationToken));
        var result = await HandleResponseAsync<TResponse>(response);

        // 4. 缓存成功的响应（5分钟）
        if (_cache != null && response.IsSuccessStatusCode && result != null)
        {
            var cacheKey = $"GET:{url}";
            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));
        }

        return result;
    });
}
```

**缓存失效策略**：
- ✅ **自动失效**：5分钟绝对过期
- ✅ **写操作失效**：POST/PUT/DELETE后自动清除相关缓存
- ✅ **手动失效**：通过`ICacheService.Remove(key)`

---

## 第6章：BaseApiRepository 继承与扩展

### 6.1 基本继承模式

```csharp
public class PatientRepository : BaseApiRepository<PatientDto>
{
    public PatientRepository(
        IApiService apiService,
        ILogger<PatientRepository> logger)
        : base(apiService, logger, "/api/v1/patients")
    {
    }

    // 自动获得7个方法：
    // - GetAllAsync()
    // - GetPagedAsync(page, pageSize, keyword)
    // - GetByIdAsync(id)
    // - CreateAsync(entity)
    // - UpdateAsync(id, entity)
    // - DeleteAsync(id)
    // - SearchAsync(keyword)
}
```

### 6.2 扩展自定义方法

**示例1：按条件查询**
```csharp
public class PatientRepository : BaseApiRepository<PatientDto>
{
    public async Task<List<PatientDto>> GetPatientsByGenderAsync(Gender gender)
    {
        var parameters = new { gender };
        var result = await _apiService.GetAsync<List<PatientDto>>(
            $"{_endpoint}/by-gender",
            parameters);
        return result ?? new List<PatientDto>();
    }

    public async Task<List<PatientDto>> GetRecentPatientsAsync(int days)
    {
        var result = await _apiService.GetAsync<List<PatientDto>>(
            $"{_endpoint}/recent?days={days}");
        return result ?? new List<PatientDto>();
    }
}
```

**示例2：批量操作**
```csharp
public async Task<BatchResult> BatchUpdatePatientsAsync(List<Guid> ids, UpdatePatientDto update)
{
    var request = new { ids, update };
    var result = await _apiService.PostAsync<object, BatchResult>(
        $"{_endpoint}/batch-update",
        request);
    return result ?? new BatchResult();
}
```

**示例3：统计查询**
```csharp
public async Task<PatientStatistics> GetStatisticsAsync()
{
    var result = await _apiService.GetAsync<PatientStatistics>($"{_endpoint}/statistics");
    return result ?? new PatientStatistics();
}
```

### 6.3 重写基类方法以添加自定义逻辑

**示例1：添加日志**
```csharp
public override async Task<PatientDto> CreateAsync(PatientDto entity)
{
    _logger.LogInformation("开始创建患者: {Name}, {IdNumber}", entity.Name, entity.IdNumber);

    var created = await base.CreateAsync(entity);

    if (created != null)
    {
        _logger.LogInformation("患者创建成功: ID={Id}", created.Id);
    }

    return created!;
}
```

**示例2：添加验证**
```csharp
public override async Task<PatientDto> UpdateAsync(Guid id, PatientDto entity)
{
    // 前置验证
    if (string.IsNullOrWhiteSpace(entity.Name))
    {
        throw new ArgumentException("患者姓名不能为空");
    }

    // 调用基类实现
    var updated = await base.UpdateAsync(id, entity);

    // 后置处理（如清除缓存）
    _cache?.Remove($"patient:{id}");

    return updated!;
}
```

**示例3：添加缓存**
```csharp
public class PatientRepository : BaseApiRepository<PatientDto>
{
    private readonly ICacheService _cache;

    public PatientRepository(
        IApiService apiService,
        ILogger<PatientRepository> logger,
        ICacheService cache)
        : base(apiService, logger, "/api/v1/patients")
    {
        _cache = cache;
    }

    public override async Task<PatientDto> GetByIdAsync(Guid id)
    {
        // Cache-First 模式
        return await _cache.GetOrCreateAsync(
            $"patient:{id}",
            () => base.GetByIdAsync(id),
            expiry: TimeSpan.FromMinutes(10));
    }

    public override async Task<PatientDto> UpdateAsync(Guid id, PatientDto entity)
    {
        var updated = await base.UpdateAsync(id, entity);

        // 清除缓存
        _cache.Remove($"patient:{id}");

        return updated!;
    }
}
```

### 6.4 Repository 与 Service 分层

**Repository职责**（数据访问层）：
- ✅ 封装HTTP API调用
- ✅ 处理数据转换
- ✅ 实现缓存策略
- ❌ 禁止业务逻辑

**Service职责**（业务逻辑层）：
- ✅ 编排多个Repository调用
- ✅ 实现业务规则
- ✅ 异常处理和转换
- ❌ 禁止直接调用HTTP

**示例：PatientService 编排**
```csharp
public class PatientService : IPatientService
{
    private readonly PatientRepository _patientRepo;
    private readonly MedicalCaseRepository _caseRepo;
    private readonly IExceptionHandler _exceptionHandler;

    public async Task<ServiceResult<PatientWithCasesDto>> GetPatientWithCasesAsync(Guid patientId)
    {
        return await _exceptionHandler.SafeExecuteAsync(async () =>
        {
            // 1. 获取患者信息
            var patient = await _patientRepo.GetByIdAsync(patientId);

            // 2. 获取病案列表
            var cases = await _caseRepo.GetByPatientIdAsync(patientId);

            // 3. 业务规则：筛选未删除的病案
            var activeCases = cases.Where(c => !c.IsDeleted).ToList();

            // 4. 组装DTO
            return new PatientWithCasesDto
            {
                Patient = patient,
                MedicalCases = activeCases,
                TotalCases = activeCases.Count
            };
        });
    }
}
```

---

## 第7章：缓存服务使用

### 7.1 ICacheService 接口定义

```csharp
public interface ICacheService
{
    T? Get<T>(string key);
    void Set<T>(string key, T value, TimeSpan? expiry = null);
    void Remove(string key);
    bool Exists(string key);
    void Clear();
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null);
}
```

### 7.2 缓存使用模式

**模式1：Cache-First（优先缓存）**
```csharp
public class PatientRepository : BaseApiRepository<PatientDto>
{
    private readonly ICacheService _cache;

    public async Task<PatientDto> GetByIdAsync(Guid id)
    {
        return await _cache.GetOrCreateAsync(
            $"patient:{id}",
            () => base.GetByIdAsync(id),
            expiry: TimeSpan.FromMinutes(10));
    }
}
```

**模式2：Cache-Aside（手动缓存）**
```csharp
public async Task<List<PatientDto>> GetAllPatientsAsync()
{
    // 1. 尝试从缓存获取
    var cached = _cache.Get<List<PatientDto>>("patients:all");
    if (cached != null)
    {
        return cached;
    }

    // 2. 从API获取
    var patients = await base.GetAllAsync();

    // 3. 写入缓存
    _cache.Set("patients:all", patients, TimeSpan.FromMinutes(5));

    return patients;
}
```

**模式3：Cache Invalidation（缓存失效）**
```csharp
public async Task<PatientDto> CreateAsync(PatientDto entity)
{
    var created = await base.CreateAsync(entity);

    // 创建后清除相关缓存
    _cache.Remove("patients:all");
    _cache.Remove($"patients:search");

    return created!;
}

public async Task<PatientDto> UpdateAsync(Guid id, PatientDto entity)
{
    var updated = await base.UpdateAsync(id, entity);

    // 更新后清除特定缓存
    _cache.Remove($"patient:{id}");
    _cache.Remove("patients:all");

    return updated!;
}

public async Task<bool> DeleteAsync(Guid id)
{
    var deleted = await base.DeleteAsync(id);

    if (deleted)
    {
        _cache.Remove($"patient:{id}");
        _cache.Remove("patients:all");
    }

    return deleted;
}
```

### 7.3 缓存键命名规范

**推荐格式**：`{module}:{entity}:{operation}:{id/filter}`

**示例**：
```csharp
// 单个实体
"patient:123e4567-e89b-12d3-a456-426614174000"

// 列表查询
"patients:all"
"patients:active"
"patients:recent:7days"

// 搜索结果
"patients:search:张三"
"patients:by-gender:male"

// 分页数据
"patients:paged:1:20"  // page 1, size 20

// 统计数据
"patients:statistics:2024-01"
"patients:count:active"
```

### 7.4 缓存过期策略

**策略1：固定时间过期**
```csharp
// 短期缓存（1分钟）
_cache.Set("patients:count", count, TimeSpan.FromMinutes(1));

// 中期缓存（5分钟）
_cache.Set("patients:all", patients, TimeSpan.FromMinutes(5));

// 长期缓存（1小时）
_cache.Set("patients:statistics", stats, TimeSpan.FromHours(1));
```

**策略2：条件过期**
```csharp
public async Task<List<PatientDto>> GetActivePatientsAsync()
{
    return await _cache.GetOrCreateAsync(
        "patients:active",
        async () =>
        {
            var all = await base.GetAllAsync();
            return all.Where(p => p.IsActive).ToList();
        },
        expiry: TimeSpan.FromMinutes(5));
}
```

**策略3：滑动过期（访问时续期）**
```csharp
public void SetWithSlidingExpiration<T>(string key, T value, TimeSpan slidingExpiration)
{
    var options = new MemoryCacheEntryOptions
    {
        SlidingExpiration = slidingExpiration,  // 滑动过期（访问时续期）
        Size = 1
    };

    _cache.Set(key, value, options);
}
```

### 7.5 缓存监控与调试

**启用缓存日志**（appsettings.json）：
```json
{
  "Logging": {
    "LogLevel": {
      "LYBT.Desktop.Foundation.Caching": "Debug"
    }
  }
}
```

**CacheService 日志输出**：
```
[Debug] 缓存命中: patient:123e4567
[Debug] 缓存未命中，创建新值: patients:all
[Debug] 设置缓存: patients:all
[Debug] 移除缓存: patient:123e4567
[Information] 清空缓存完成
```

---

## 第8章：认证服务集成

### 8.1 JWT认证流程

```
┌──────────┐   LoginAsync    ┌──────────┐
│ViewModel │ ─────────────→  │  Service │
└──────────┘                  └──────────┘
                                    │
                                    ↓
                          ┌─────────────────────┐
                          │ AuthenticationService│
                          └─────────────────────┘
                                    │
                                    ↓
                          ┌─────────────────────┐
                          │  IAuthApi (HTTP)     │ ← POST /api/v1/auth/login
                          └─────────────────────┘
                                    │
                                    ↓
                          ┌─────────────────────┐
                          │  Server Response     │
                          │  { Token, RefreshToken, User }
                          └─────────────────────┘
                                    │
                                    ↓
                          ┌─────────────────────┐
                          │ TokenStorageService  │ ← 保存Token
                          └─────────────────────┘
                                    │
                                    ↓
                          ┌─────────────────────┐
                          │ AuthorizationMessageHandler │ ← 自动注入 Bearer Token
                          └─────────────────────┘
```

### 8.2 IAuthenticationService 接口

```csharp
public interface IAuthenticationService
{
    Task<bool> IsLoggedInAsync();
    Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request);
    Task<ServiceResult> LogoutAsync();
    Task<UserDto?> GetCurrentUserAsync();
    string? GetToken();
    void ClearAuthInfo();
    Task<bool> CheckConnectionAsync();
    Task<bool> ChangePasswordAsync(string currentPassword, string newPassword);
}
```

### 8.3 登录实现（ViewModel）

```csharp
public class LoginViewModel : BindableBase
{
    private readonly IAuthenticationService _authService;
    private readonly IRegionManager _regionManager;
    private readonly ILogger<LoginViewModel> _logger;

    private string _username = string.Empty;
    private string _password = string.Empty;
    private bool _isLoading;
    private string _errorMessage = string.Empty;

    public DelegateCommand LoginCommand { get; }

    public LoginViewModel(
        IAuthenticationService authService,
        IRegionManager regionManager,
        ILogger<LoginViewModel> logger)
    {
        _authService = authService;
        _regionManager = regionManager;
        _logger = logger;

        LoginCommand = new DelegateCommand(ExecuteLogin, CanLogin)
            .ObservesProperty(() => Username)
            .ObservesProperty(() => Password);
    }

    private bool CanLogin()
    {
        return !string.IsNullOrWhiteSpace(Username) &&
               !string.IsNullOrWhiteSpace(Password) &&
               !IsLoading;
    }

    private async void ExecuteLogin()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var request = new LoginRequest
            {
                Username = Username,
                Password = Password
            };

            var result = await _authService.LoginAsync(request);

            if (result.IsSuccess)
            {
                _logger.LogInformation("用户登录成功: {Username}", Username);

                // 导航到主界面
                _regionManager.RequestNavigate("MainRegion", "MainView");
            }
            else
            {
                ErrorMessage = result.Message ?? "登录失败";
                _logger.LogWarning("用户登录失败: {Username}, 原因: {Message}", Username, ErrorMessage);
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = "登录失败: " + ex.Message;
            _logger.LogError(ex, "登录异常");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
```

### 8.4 AuthenticationService 实现

```csharp
public class AuthenticationService : IAuthenticationService
{
    private readonly IAuthApi _authApi;
    private readonly ITokenStorageService _tokenStorage;
    private readonly ILogger<AuthenticationService> _logger;

    public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
    {
        try
        {
            // 1. 调用HTTP API登录
            var apiResponse = await _authApi.LoginAsync(request);

            if (apiResponse.Success && apiResponse.Data != null)
            {
                // 2. 保存Token到内存
                await _tokenStorage.SaveTokenAsync(apiResponse.Data.Token);
                await _tokenStorage.SaveLoginResponseAsync(apiResponse.Data);

                return ServiceResult<LoginResponse>.Success(apiResponse.Data, "登录成功");
            }
            else
            {
                return ServiceResult<LoginResponse>.Failure(apiResponse.Message ?? "登录失败");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登录失败");
            return ServiceResult<LoginResponse>.Failure($"登录失败: {ex.Message}");
        }
    }

    public async Task<ServiceResult> LogoutAsync()
    {
        try
        {
            // 1. 获取当前用户信息
            var loginResponse = await _tokenStorage.GetLoginResponseAsync();
            var username = loginResponse?.User.UserName ?? "unknown";

            // 2. 调用HTTP API登出
            var logoutRequest = new LogoutRequest
            {
                Username = username,
                RefreshToken = loginResponse?.RefreshToken
            };

            var apiResponse = await _authApi.LogoutAsync(logoutRequest);

            // 3. 清除本地Token（无论API调用成功与否）
            await _tokenStorage.ClearAuthenticationAsync();

            return ServiceResult.Success("登出成功");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "登出失败");
            // 即使异常，也清除本地Token
            await _tokenStorage.ClearAuthenticationAsync();
            return ServiceResult.Success("本地登出成功");
        }
    }

    public async Task<UserDto?> GetCurrentUserAsync()
    {
        var loginResponse = await _tokenStorage.GetLoginResponseAsync();
        return loginResponse?.User;
    }

    public string? GetToken()
    {
        return _tokenStorage.GetTokenAsync().GetAwaiter().GetResult();
    }

    public async Task<bool> CheckConnectionAsync()
    {
        try
        {
            // 1. 检查Token是否过期
            var isExpired = await _tokenStorage.IsTokenExpiredAsync();
            if (isExpired)
            {
                return false;
            }

            // 2. 调用健康检查API
            var healthResponse = await _authApi.HealthCheckAsync();
            return healthResponse != null && healthResponse.Status == "Healthy";
        }
        catch
        {
            return false;
        }
    }
}
```

### 8.5 AuthorizationMessageHandler（自动注入JWT）

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
        // 1. 获取Token
        var token = await _tokenStorage.GetTokenAsync();

        if (!string.IsNullOrEmpty(token))
        {
            // 2. 自动注入 Authorization 头
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            _logger.LogDebug("已注入 Bearer Token 到请求头");
        }

        // 3. 执行HTTP请求
        var response = await base.SendAsync(request, cancellationToken);

        // 4. 处理401响应（Token过期）
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            _logger.LogWarning("收到401响应，Token可能已过期");
            await _tokenStorage.ClearAuthenticationAsync();
        }

        return response;
    }
}
```

### 8.6 Token存储服务

```csharp
public class TokenStorageService : ITokenStorageService
{
    private LoginResponse? _loginResponse;
    private string? _token;

    public Task SaveTokenAsync(string token)
    {
        _token = token;
        return Task.CompletedTask;
    }

    public Task<string?> GetTokenAsync()
    {
        return Task.FromResult(_token);
    }

    public Task SaveLoginResponseAsync(LoginResponse response)
    {
        _loginResponse = response;
        _token = response.Token;
        return Task.CompletedTask;
    }

    public Task<LoginResponse?> GetLoginResponseAsync()
    {
        return Task.FromResult(_loginResponse);
    }

    public Task ClearAuthenticationAsync()
    {
        _token = null;
        _loginResponse = null;
        return Task.CompletedTask;
    }

    public Task<bool> IsTokenExpiredAsync()
    {
        if (_loginResponse == null || string.IsNullOrEmpty(_token))
        {
            return Task.FromResult(true);
        }

        // 检查Token过期时间
        if (_loginResponse.ExpiresAt < DateTime.UtcNow)
        {
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    }
}
```

### 8.7 启动时检查登录状态

```csharp
public class App : PrismApplication
{
    protected override async void OnInitialized()
    {
        InitializeComponent();

        var authService = Container.Resolve<IAuthenticationService>();
        var isLoggedIn = await authService.IsLoggedInAsync();

        if (isLoggedIn)
        {
            // 已登录，导航到主界面
            var regionManager = Container.Resolve<IRegionManager>();
            regionManager.RequestNavigate("MainRegion", "MainView");
        }
        else
        {
            // 未登录，显示登录界面
            var regionManager = Container.Resolve<IRegionManager>();
            regionManager.RequestNavigate("MainRegion", "LoginView");
        }
    }
}
```

---

## 第9章：异常处理与SafeExecuteAsync

### 9.1 IExceptionHandler 接口

```csharp
public interface IExceptionHandler
{
    void HandleException(Exception exception);
    ServiceResult<T> HandleException<T>(Exception exception);
    Task<ServiceResult<T>> SafeExecuteAsync<T>(Func<Task<T>> operation);
    Task<ServiceResult<bool>> SafeExecuteAsync(Func<Task> operation);
}
```

### 9.2 SafeExecuteAsync 使用模式

**模式1：返回数据的操作**
```csharp
public class PatientService : IPatientService
{
    private readonly PatientRepository _repository;
    private readonly IExceptionHandler _exceptionHandler;

    public async Task<ServiceResult<PatientDto>> GetPatientAsync(Guid id)
    {
        return await _exceptionHandler.SafeExecuteAsync(async () =>
        {
            // 业务逻辑（可能抛出异常）
            var patient = await _repository.GetByIdAsync(id);

            if (patient == null)
            {
                throw new NotFoundException($"未找到患者: {id}");
            }

            return patient;
        });
    }
}
```

**模式2：无返回值的操作**
```csharp
public async Task<ServiceResult<bool>> DeletePatientAsync(Guid id)
{
    return await _exceptionHandler.SafeExecuteAsync(async () =>
    {
        var deleted = await _repository.DeleteAsync(id);

        if (!deleted)
        {
            throw new InvalidOperationException("删除失败");
        }
    });
}
```

**模式3：复杂业务逻辑**
```csharp
public async Task<ServiceResult<MedicalCaseDto>> CreateMedicalCaseAsync(CreateMedicalCaseDto request)
{
    return await _exceptionHandler.SafeExecuteAsync(async () =>
    {
        // Step 1: 验证患者存在
        var patient = await _patientRepository.GetByIdAsync(request.PatientId);
        if (patient == null)
        {
            throw new NotFoundException("患者不存在");
        }

        // Step 2: 验证医生权限
        var currentUser = await _authService.GetCurrentUserAsync();
        if (!currentUser.HasPermission("CreateMedicalCase"))
        {
            throw new UnauthorizedException("无权限创建病案");
        }

        // Step 3: 创建病案
        var medicalCase = new MedicalCaseDto
        {
            PatientId = request.PatientId,
            DoctorId = currentUser.Id,
            CreatedAt = DateTime.Now
        };

        var created = await _medicalCaseRepository.CreateAsync(medicalCase);

        // Step 4: 清除缓存
        _cache.Remove($"patient:{request.PatientId}:cases");

        return created;
    });
}
```

### 9.3 StandardExceptionHandler 实现

```csharp
public class StandardExceptionHandler : IExceptionHandler
{
    private readonly ILogger<StandardExceptionHandler> _logger;
    private readonly ExceptionMessageMapper _messageMapper;

    public async Task<ServiceResult<T>> SafeExecuteAsync<T>(Func<Task<T>> operation)
    {
        try
        {
            var result = await operation();
            return ServiceResult<T>.Success(result);
        }
        catch (Exception ex)
        {
            return HandleException<T>(ex);
        }
    }

    public ServiceResult<T> HandleException<T>(Exception exception)
    {
        // 1. 记录日志
        var severity = DetermineExceptionSeverity(exception);
        LogException(exception, severity);

        // 2. 映射友好消息
        var friendlyMessage = _messageMapper.Map(exception);

        // 3. 返回ServiceResult
        return ServiceResult<T>.Failure(friendlyMessage);
    }

    private ExceptionSeverity DetermineExceptionSeverity(Exception exception)
    {
        return exception switch
        {
            NotFoundException => ExceptionSeverity.Low,
            ValidationException => ExceptionSeverity.Low,
            UnauthorizedException => ExceptionSeverity.Medium,
            ApiException { StatusCode: System.Net.HttpStatusCode.InternalServerError } => ExceptionSeverity.High,
            _ => ExceptionSeverity.Medium
        };
    }

    private void LogException(Exception exception, ExceptionSeverity severity)
    {
        switch (severity)
        {
            case ExceptionSeverity.Low:
                _logger.LogWarning(exception, "业务异常");
                break;
            case ExceptionSeverity.Medium:
                _logger.LogError(exception, "运行时异常");
                break;
            case ExceptionSeverity.High:
            case ExceptionSeverity.Critical:
                _logger.LogCritical(exception, "严重异常");
                break;
        }
    }
}
```

### 9.4 ExceptionMessageMapper（友好消息映射）

```csharp
public class ExceptionMessageMapper
{
    private readonly Dictionary<Type, string> _messageMap = new()
    {
        { typeof(NotFoundException), "未找到相关数据" },
        { typeof(ValidationException), "数据验证失败" },
        { typeof(UnauthorizedException), "您无权执行此操作" },
        { typeof(DuplicateException), "数据已存在，无法重复添加" },
        { typeof(InvalidOperationException), "操作失败，请检查数据状态" },
        { typeof(TimeoutException), "操作超时，请稍后重试" },
        { typeof(HttpRequestException), "网络连接失败，请检查网络" },
        { typeof(ApiException), "服务器错误，请联系管理员" }
    };

    public string Map(Exception exception)
    {
        // 1. 精确匹配异常类型
        if (_messageMap.TryGetValue(exception.GetType(), out var message))
        {
            return message;
        }

        // 2. 匹配基类
        foreach (var (exceptionType, msg) in _messageMap)
        {
            if (exceptionType.IsAssignableFrom(exception.GetType()))
            {
                return msg;
            }
        }

        // 3. 默认消息
        return "操作失败: " + exception.Message;
    }
}
```

### 9.5 ViewModel中使用SafeExecuteAsync

**示例1：加载数据**
```csharp
public class PatientListViewModel : BindableBase
{
    private ObservableCollection<PatientDto> _patients = new();
    private bool _isLoading;
    private string _errorMessage = string.Empty;

    private readonly IPatientService _patientService;

    public DelegateCommand LoadCommand { get; }

    public PatientListViewModel(IPatientService patientService)
    {
        _patientService = patientService;
        LoadCommand = new DelegateCommand(ExecuteLoad);
    }

    private async void ExecuteLoad()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;

        // SafeExecuteAsync已在Service层处理
        var result = await _patientService.GetAllPatientsAsync();

        if (result.IsSuccess)
        {
            Patients = new ObservableCollection<PatientDto>(result.Data!);
        }
        else
        {
            ErrorMessage = result.Message ?? "加载失败";
        }

        IsLoading = false;
    }
}
```

**示例2：保存数据**
```csharp
public async void ExecuteSave()
{
    IsSaving = true;
    ErrorMessage = string.Empty;

    var result = await _patientService.UpdatePatientAsync(PatientId, Patient);

    if (result.IsSuccess)
    {
        // 保存成功，关闭对话框
        _dialogService.CloseDialog("Success");
    }
    else
    {
        // 显示错误消息
        ErrorMessage = result.Message ?? "保存失败";
    }

    IsSaving = false;
}
```

### 9.6 全局异常捕获（App.xaml.cs）

```csharp
public partial class App : PrismApplication
{
    private ILogger<App>? _logger;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 注册全局异常处理器
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        _logger?.LogCritical(exception, "AppDomain未处理异常");

        MessageBox.Show(
            "应用程序发生严重错误，即将退出。\\n错误信息: " + exception?.Message,
            "严重错误",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, "Task未观察到的异常");
        e.SetObserved(); // 标记为已处理，避免应用崩溃
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _logger?.LogError(e.Exception, "Dispatcher未处理异常");

        MessageBox.Show(
            "操作失败: " + e.Exception.Message,
            "错误",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

        e.Handled = true; // 标记为已处理
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        // 注册异常处理器
        containerRegistry.RegisterSingleton<IExceptionHandler, StandardExceptionHandler>();
        containerRegistry.RegisterSingleton<ExceptionMessageMapper>();

        // 获取Logger
        _logger = Container.Resolve<ILogger<App>>();
    }
}
```

---

## 第10章：启动优化集成

### 10.1 IStartupOptimizationService 接口

```csharp
public interface IStartupOptimizationService
{
    Task WarmupAsync();
    Task PreloadCriticalResourcesAsync();
    Task OptimizeStartupAsync();
    Task WarmupApplicationAsync();
    TimeSpan GetStartupDuration();
    void ClearStartupCache();
    event EventHandler? OptimizationCompleted;
}
```

### 10.2 启动优化四步流程

```
Step 1: OptimizeStartupAsync()
  ↓ 延迟加载非关键服务
Step 2: PreloadCriticalResourcesAsync()
  ↓ 预加载权限/配置/字典
Step 3: WarmupApplicationAsync()
  ↓ HttpClient预热、连接池初始化
Step 4: GetStartupDuration()
  ↓ 测量启动时长、触发OptimizationCompleted
```

### 10.3 在App.xaml.cs中集成

```csharp
public partial class App : PrismApplication
{
    private readonly Stopwatch _startupTimer = Stopwatch.StartNew();

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 显示启动界面
        var splashScreen = new SplashScreenWindow();
        splashScreen.Show();

        try
        {
            // Step 1: 延迟加载策略
            var optimizationService = Container.Resolve<IStartupOptimizationService>();
            await optimizationService.OptimizeStartupAsync();
            splashScreen.UpdateProgress(25, "正在加载配置...");

            // Step 2: 预加载关键资源
            await optimizationService.PreloadCriticalResourcesAsync();
            splashScreen.UpdateProgress(50, "正在加载权限...");

            // Step 3: 应用预热
            await optimizationService.WarmupApplicationAsync();
            splashScreen.UpdateProgress(75, "正在预热连接...");

            // Step 4: 检查登录状态
            var authService = Container.Resolve<IAuthenticationService>();
            var isLoggedIn = await authService.IsLoggedInAsync();

            // 导航到主界面或登录界面
            var regionManager = Container.Resolve<IRegionManager>();
            if (isLoggedIn)
            {
                regionManager.RequestNavigate("MainRegion", "MainView");
            }
            else
            {
                regionManager.RequestNavigate("MainRegion", "LoginView");
            }

            splashScreen.UpdateProgress(100, "启动完成");
        }
        catch (Exception ex)
        {
            var logger = Container.Resolve<ILogger<App>>();
            logger.LogError(ex, "启动失败");
            MessageBox.Show("应用启动失败: " + ex.Message);
        }
        finally
        {
            // 关闭启动界面
            splashScreen.Close();

            _startupTimer.Stop();
            var logger = Container.Resolve<ILogger<App>>();
            logger.LogInformation("应用启动完成，耗时: {Duration}ms", _startupTimer.ElapsedMilliseconds);
        }
    }
}
```

### 10.4 StartupOptimizationService 实现

```csharp
public class StartupOptimizationService : IStartupOptimizationService
{
    private readonly ILogger<StartupOptimizationService> _logger;
    private readonly IApiService _apiService;
    private readonly IConfigurationService _configService;
    private readonly Stopwatch _timer = new();

    public event EventHandler? OptimizationCompleted;

    public async Task OptimizeStartupAsync()
    {
        _timer.Start();
        _logger.LogInformation("开始启动优化...");

        // 延迟加载非关键服务
        await Task.Delay(100); // 模拟延迟加载

        _logger.LogInformation("启动优化完成");
    }

    public async Task PreloadCriticalResourcesAsync()
    {
        _logger.LogInformation("开始预加载关键资源...");

        // 预加载配置
        await _configService.LoadUserSettings();

        // 预加载权限（如果已登录）
        // var permissions = await _authService.GetCurrentUserPermissionsAsync();

        _logger.LogInformation("关键资源预加载完成");
    }

    public async Task WarmupApplicationAsync()
    {
        _logger.LogInformation("开始应用预热...");

        // HttpClient预热（发送健康检查请求）
        try
        {
            await _apiService.GetAsync<object>("/api/v1/health");
        }
        catch
        {
            _logger.LogWarning("健康检查失败，跳过预热");
        }

        _logger.LogInformation("应用预热完成");

        _timer.Stop();
        OptimizationCompleted?.Invoke(this, EventArgs.Empty);
    }

    public TimeSpan GetStartupDuration()
    {
        return _timer.Elapsed;
    }

    public void ClearStartupCache()
    {
        _logger.LogInformation("清除启动缓存");
        // 清理临时资源
    }
}
```

### 10.5 SplashScreenWindow（启动界面）

```xaml
<Window x:Class="LYBT.Desktop.Shell.Views.SplashScreenWindow"
        xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
        xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
        Title="LYBTZYZS - 启动中"
        Height="300" Width="500"
        WindowStyle="None"
        AllowsTransparency="True"
        Background="Transparent"
        WindowStartupLocation="CenterScreen">
    <Grid>
        <Border Background="#2c3e50" CornerRadius="10" Padding="30">
            <StackPanel>
                <TextBlock Text="中医诊所管理系统"
                           FontSize="24"
                           FontWeight="Bold"
                           Foreground="White"
                           HorizontalAlignment="Center"/>

                <TextBlock x:Name="StatusText"
                           Text="正在启动..."
                           Foreground="#ecf0f1"
                           FontSize="14"
                           Margin="0,20,0,0"
                           HorizontalAlignment="Center"/>

                <ProgressBar x:Name="ProgressBar"
                             Height="20"
                             Minimum="0"
                             Maximum="100"
                             Value="0"
                             Margin="0,20,0,0"/>

                <TextBlock x:Name="ProgressText"
                           Text="0%"
                           Foreground="#bdc3c7"
                           FontSize="12"
                           HorizontalAlignment="Center"
                           Margin="0,10,0,0"/>
            </StackPanel>
        </Border>
    </Grid>
</Window>
```

```csharp
public partial class SplashScreenWindow : Window
{
    public SplashScreenWindow()
    {
        InitializeComponent();
    }

    public void UpdateProgress(int value, string status)
    {
        Dispatcher.Invoke(() =>
        {
            ProgressBar.Value = value;
            ProgressText.Text = $"{value}%";
            StatusText.Text = status;
        });
    }
}
```

---

## 第11章：健康检查实现

### 11.1 IApiHealthCheckService 接口

```csharp
public interface IApiHealthCheckService
{
    Task<ApiHealthStatus> CheckHealthAsync();
    string? LastErrorMessage { get; }
}

public enum ApiHealthStatus
{
    Healthy,    // 健康（响应时间<2秒）
    Degraded,   // 降级（响应时间≥2秒）
    Unhealthy   // 不健康（连接失败/错误）
}
```

### 11.2 ApiHealthCheckService 实现

```csharp
public class ApiHealthCheckService : IApiHealthCheckService
{
    private readonly IApiService _apiService;
    private readonly ILogger<ApiHealthCheckService> _logger;
    private string? _lastErrorMessage;

    public string? LastErrorMessage => _lastErrorMessage;

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
            var response = await _apiService.GetAsync<HealthCheckResponse>("/api/v1/health");

            stopwatch.Stop();
            var elapsed = stopwatch.Elapsed.TotalSeconds;

            if (response == null)
            {
                _lastErrorMessage = "健康检查响应为空";
                return ApiHealthStatus.Unhealthy;
            }

            if (response.Status == "Healthy")
            {
                _lastErrorMessage = null;

                // 根据响应时间判断
                return elapsed < 2.0
                    ? ApiHealthStatus.Healthy
                    : ApiHealthStatus.Degraded;
            }
            else
            {
                _lastErrorMessage = response.Message ?? "健康检查失败";
                return ApiHealthStatus.Unhealthy;
            }
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _lastErrorMessage = ex.Message;
            _logger.LogError(ex, "健康检查失败");
            return ApiHealthStatus.Unhealthy;
        }
    }
}
```

### 11.3 在ViewModel中使用健康检查

**示例：StatusBar健康指示器**
```csharp
public class StatusBarViewModel : BindableBase
{
    private readonly IApiHealthCheckService _healthCheck;
    private readonly Timer _timer;
    private ApiHealthStatus _healthStatus;
    private string _statusMessage = string.Empty;

    public ApiHealthStatus HealthStatus
    {
        get => _healthStatus;
        set => SetProperty(ref _healthStatus, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public StatusBarViewModel(IApiHealthCheckService healthCheck)
    {
        _healthCheck = healthCheck;

        // 每30秒检查一次健康状态
        _timer = new Timer(30000);
        _timer.Elapsed += async (s, e) => await CheckHealthAsync();
        _timer.Start();

        // 初始检查
        Task.Run(async () => await CheckHealthAsync());
    }

    private async Task CheckHealthAsync()
    {
        var status = await _healthCheck.CheckHealthAsync();

        await Application.Current.Dispatcher.InvokeAsync(() =>
        {
            HealthStatus = status;

            StatusMessage = status switch
            {
                ApiHealthStatus.Healthy => "服务正常",
                ApiHealthStatus.Degraded => "服务降级（响应缓慢）",
                ApiHealthStatus.Unhealthy => $"服务异常: {_healthCheck.LastErrorMessage}",
                _ => "未知状态"
            };
        });
    }
}
```

**StatusBar.xaml（健康指示器UI）**：
```xaml
<StatusBar DockPanel.Dock="Bottom">
    <StatusBarItem>
        <StackPanel Orientation="Horizontal">
            <!-- 健康状态图标 -->
            <Ellipse Width="10" Height="10" Margin="5,0">
                <Ellipse.Style>
                    <Style TargetType="Ellipse">
                        <Style.Triggers>
                            <DataTrigger Binding="{Binding HealthStatus}" Value="Healthy">
                                <Setter Property="Fill" Value="Green"/>
                            </DataTrigger>
                            <DataTrigger Binding="{Binding HealthStatus}" Value="Degraded">
                                <Setter Property="Fill" Value="Orange"/>
                            </DataTrigger>
                            <DataTrigger Binding="{Binding HealthStatus}" Value="Unhealthy">
                                <Setter Property="Fill" Value="Red"/>
                            </DataTrigger>
                        </Style.Triggers>
                    </Style>
                </Ellipse.Style>
            </Ellipse>

            <!-- 状态消息 -->
            <TextBlock Text="{Binding StatusMessage}" Margin="5,0"/>
        </StackPanel>
    </StatusBarItem>
</StatusBar>
```

---

## 第12章：常见问题与陷阱

### 12.1 ❌ 反模式1：在Repository中实现业务逻辑

**错误示例**：
```csharp
// ❌ 错误：在Repository中实现业务规则
public class PatientRepository : BaseApiRepository<PatientDto>
{
    public async Task<PatientDto> CreatePatientWithValidation(CreatePatientDto request)
    {
        // ❌ 业务验证不应该在Repository
        if (request.Age < 0 || request.Age > 150)
        {
            throw new ValidationException("年龄无效");
        }

        // ❌ 业务规则不应该在Repository
        if (await IsDuplicateIdNumber(request.IdNumber))
        {
            throw new DuplicateException("身份证号已存在");
        }

        return await CreateAsync(request);
    }
}
```

**✅ 正确示例**：
```csharp
// ✅ Repository只负责数据访问
public class PatientRepository : BaseApiRepository<PatientDto>
{
    public async Task<bool> ExistsByIdNumberAsync(string idNumber)
    {
        var result = await _apiService.GetAsync<bool>(
            $"{_endpoint}/exists-by-idnumber?idNumber={Uri.EscapeDataString(idNumber)}");
        return result ?? false;
    }
}

// ✅ Service层实现业务逻辑
public class PatientService : IPatientService
{
    public async Task<ServiceResult<PatientDto>> CreatePatientAsync(CreatePatientDto request)
    {
        return await _exceptionHandler.SafeExecuteAsync(async () =>
        {
            // 业务验证在Service层
            if (request.Age < 0 || request.Age > 150)
            {
                throw new ValidationException("年龄必须在0-150之间");
            }

            // 业务规则在Service层
            if (await _repository.ExistsByIdNumberAsync(request.IdNumber))
            {
                throw new DuplicateException("身份证号已存在");
            }

            return await _repository.CreateAsync(request);
        });
    }
}
```

### 12.2 ❌ 反模式2：忽略缓存失效

**错误示例**：
```csharp
// ❌ 错误：写操作后未清除缓存
public async Task<PatientDto> UpdatePatientAsync(Guid id, PatientDto entity)
{
    return await _repository.UpdateAsync(id, entity);
    // ❌ 缓存未清除，导致脏读
}
```

**✅ 正确示例**：
```csharp
// ✅ 正确：写操作后清除相关缓存
public async Task<ServiceResult<PatientDto>> UpdatePatientAsync(Guid id, UpdatePatientDto request)
{
    return await _exceptionHandler.SafeExecuteAsync(async () =>
    {
        var updated = await _repository.UpdateAsync(id, request);

        // 清除缓存
        _cache.Remove($"patient:{id}");
        _cache.Remove("patients:all");
        _cache.Remove($"patients:search");

        return updated;
    });
}
```

### 12.3 ❌ 反模式3：直接在ViewModel中调用IApiService

**错误示例**：
```csharp
// ❌ 错误：ViewModel直接调用IApiService
public class PatientListViewModel : BindableBase
{
    private readonly IApiService _apiService;

    public PatientListViewModel(IApiService apiService)
    {
        _apiService = apiService;
    }

    private async void ExecuteLoad()
    {
        // ❌ 跳过Repository和Service层，难以测试和维护
        var patients = await _apiService.GetAsync<List<PatientDto>>("/api/v1/patients");
        Patients = new ObservableCollection<PatientDto>(patients ?? new List<PatientDto>());
    }
}
```

**✅ 正确示例**：
```csharp
// ✅ 正确：ViewModel通过Service调用
public class PatientListViewModel : BindableBase
{
    private readonly IPatientService _patientService;

    public PatientListViewModel(IPatientService patientService)
    {
        _patientService = patientService;
    }

    private async void ExecuteLoad()
    {
        var result = await _patientService.GetAllPatientsAsync();

        if (result.IsSuccess)
        {
            Patients = new ObservableCollection<PatientDto>(result.Data!);
        }
        else
        {
            ErrorMessage = result.Message ?? "加载失败";
        }
    }
}
```

### 12.4 ❌ 反模式4：忽略异常处理

**错误示例**：
```csharp
// ❌ 错误：Service层未使用SafeExecuteAsync
public class PatientService : IPatientService
{
    public async Task<PatientDto> GetPatientAsync(Guid id)
    {
        // ❌ 异常直接传播到UI层，用户看到技术异常消息
        return await _repository.GetByIdAsync(id);
    }
}
```

**✅ 正确示例**：
```csharp
// ✅ 正确：使用SafeExecuteAsync统一异常处理
public class PatientService : IPatientService
{
    public async Task<ServiceResult<PatientDto>> GetPatientAsync(Guid id)
    {
        return await _exceptionHandler.SafeExecuteAsync(async () =>
        {
            var patient = await _repository.GetByIdAsync(id);

            if (patient == null)
            {
                throw new NotFoundException($"未找到患者: {id}");
            }

            return patient;
        });
    }
}
```

### 12.5 ❌ 反模式5：Foundation层引用WPF类型

**错误示例**：
```csharp
// ❌ 错误：Foundation层引用System.Windows.*
using System.Windows;

public class PatientService : IPatientService
{
    public async Task<ServiceResult<bool>> DeletePatientAsync(Guid id)
    {
        // ❌ Foundation层不应该使用WPF对话框
        var result = MessageBox.Show(
            "确认删除患者?",
            "确认",
            MessageBoxButton.YesNo);

        if (result == MessageBoxResult.Yes)
        {
            return await _repository.DeleteAsync(id);
        }

        return ServiceResult<bool>.Failure("用户取消");
    }
}
```

**✅ 正确示例**：
```csharp
// ✅ 正确：Foundation层保持平台无关
public class PatientService : IPatientService
{
    public async Task<ServiceResult<bool>> DeletePatientAsync(Guid id)
    {
        return await _exceptionHandler.SafeExecuteAsync(async () =>
        {
            return await _repository.DeleteAsync(id);
        });
    }
}

// ✅ 对话框确认在Presentation层（ViewModel）
public class PatientListViewModel : BindableBase
{
    private readonly IDialogService _dialogService; // Infrastructure层服务

    private async void ExecuteDelete(PatientDto patient)
    {
        // 使用Prism DialogService
        var result = await _dialogService.ShowConfirmationAsync(
            "确认删除",
            $"确认删除患者 {patient.Name}?");

        if (result == ButtonResult.OK)
        {
            var deleteResult = await _patientService.DeletePatientAsync(patient.Id);

            if (deleteResult.IsSuccess)
            {
                Patients.Remove(patient);
            }
            else
            {
                await _dialogService.ShowErrorAsync("删除失败", deleteResult.Message);
            }
        }
    }
}
```

### 12.6 ❌ 反模式6：滥用缓存导致内存泄漏

**错误示例**：
```csharp
// ❌ 错误：无限期缓存大对象
public async Task<List<MedicalCaseDto>> GetAllCasesAsync()
{
    return await _cache.GetOrCreateAsync(
        "cases:all",
        () => _repository.GetAllAsync(),
        expiry: null); // ❌ 永不过期
}
```

**✅ 正确示例**：
```csharp
// ✅ 正确：合理设置过期时间
public async Task<ServiceResult<List<MedicalCaseDto>>> GetAllCasesAsync()
{
    return await _exceptionHandler.SafeExecuteAsync(async () =>
    {
        return await _cache.GetOrCreateAsync(
            "cases:all",
            () => _repository.GetAllAsync(),
            expiry: TimeSpan.FromMinutes(5)); // 5分钟过期
    });
}
```

---

## 第13章：检查清单

### 13.1 代码审查清单

**架构分层**：
- [ ] Repository仅负责数据访问，无业务逻辑
- [ ] Service层实现业务规则和编排
- [ ] ViewModel通过Service访问数据，不直接调用Repository或IApiService
- [ ] Foundation层无WPF依赖（无System.Windows.*引用）

**依赖注入**：
- [ ] 所有服务通过构造函数注入
- [ ] 无Container.Resolve或ServiceLocator使用
- [ ] 生命周期配置正确（Singleton/Scoped/Transient）
- [ ] 所有接口已注册到容器

**HTTP客户端**：
- [ ] 使用BaseApiRepository<TDto>继承（推荐）
- [ ] HTTP请求带Polly重试策略
- [ ] AuthorizationMessageHandler已注册（自动JWT认证）
- [ ] GET请求自动缓存5分钟

**异常处理**：
- [ ] Service层所有公开方法使用SafeExecuteAsync
- [ ] 返回类型为ServiceResult<T>
- [ ] 友好消息映射已配置
- [ ] 全局异常处理器已注册（App.xaml.cs）

**缓存策略**：
- [ ] 读操作使用GetOrCreateAsync模式
- [ ] 写操作后清除相关缓存
- [ ] 缓存键遵循命名规范（module:entity:operation:id）
- [ ] 缓存过期时间合理（5-60分钟）

**认证授权**：
- [ ] 登录后Token存储到TokenStorageService
- [ ] 所有HTTP请求自动注入Bearer token
- [ ] 401响应自动清除Token
- [ ] 启动时检查登录状态

**启动优化**：
- [ ] 延迟加载非关键服务
- [ ] 预加载关键资源（权限/配置）
- [ ] HttpClient预热
- [ ] 启动时长测量

### 13.2 性能检查清单

**HTTP性能**：
- [ ] 避免N+1查询（使用批量API）
- [ ] 分页查询代替GetAllAsync（大数据集）
- [ ] GET请求利用缓存（5分钟）
- [ ] Polly重试策略已启用（3次指数退避）

**缓存性能**：
- [ ] 高频查询已缓存
- [ ] 缓存命中率监控（日志）
- [ ] 缓存大小限制（100条）
- [ ] 避免缓存大对象（>1MB）

**内存性能**：
- [ ] IDisposable对象已Dispose（Stream, HttpClient）
- [ ] 避免内存泄漏（取消订阅事件）
- [ ] 大对象使用WeakReference
- [ ] 定期清理缓存（Clear方法）

### 13.3 安全检查清单

**认证授权**：
- [ ] JWT Token安全存储（内存，不持久化）
- [ ] Token过期检查
- [ ] 敏感操作需要权限验证
- [ ] 密码使用DPAPI加密存储

**数据验证**：
- [ ] 所有用户输入已验证
- [ ] SQL注入防护（使用参数化查询）
- [ ] XSS防护（服务器端验证）
- [ ] CSRF防护（Token验证）

**错误处理**：
- [ ] 敏感信息不泄露到UI（友好消息映射）
- [ ] 异常日志不包含密码
- [ ] API错误信息不暴露内部实现
- [ ] 关键操作有审计日志

---

## 第14章：参考资料

### 14.1 架构文档

- [Foundation架构设计](../../explanation/architecture/client/foundation-design.md) - 完整架构规范
- [三层对齐架构总览](../../explanation/architecture/client/README.md) - Client端架构概览
- [跨端共享原则](../../explanation/architecture/shared/README.md) - Foundation与Shared层关系

### 14.2 相关开发指南

- [Models层使用指南](./models-usage.md) - DTO与ViewModel开发
- [Infrastructure层使用指南](./infrastructure-usage.md) - WPF专用服务
- [DTO开发指南](../../how-to-guides/shared/dto-development.md) - 共享DTO规范
- [WebAPI开发指南](../../how-to-guides/server/webapi-development.md) - Server端API契约

### 14.3 快速参考

- [代码模式参考](../../quick-reference/code-patterns.md) - 常用代码模式速查
- [API参考](../../quick-reference/api-reference.md) - 核心API速查
- [问题解决指南](../../quick-reference/troubleshooting.md) - 常见问题快速解决

### 14.4 外部资源

**官方文档**：
- [.NET 8文档](https://learn.microsoft.com/zh-cn/dotnet/core/whats-new/dotnet-8)
- [Polly文档](https://www.pollydocs.org/)
- [Prism文档](https://prismlibrary.com/docs/)
- [WPF文档](https://learn.microsoft.com/zh-cn/dotnet/desktop/wpf/)

**最佳实践**：
- [ASP.NET Core最佳实践](https://learn.microsoft.com/zh-cn/aspnet/core/fundamentals/best-practices)
- [依赖注入最佳实践](https://learn.microsoft.com/zh-cn/dotnet/core/extensions/dependency-injection-guidelines)
- [异步编程最佳实践](https://learn.microsoft.com/zh-cn/dotnet/csharp/asynchronous-programming/async-scenarios)

---

**最后更新**：2025-10-30
**文档版本**：v1.0
**维护负责**：Client端开发组
