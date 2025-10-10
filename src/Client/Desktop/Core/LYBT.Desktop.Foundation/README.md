# LYBT.Desktop.Foundation

> **版本**: 2.0.0
> **关联Issue**: #1114 Phase 1
> **创建日期**: 2025-01-09

---

## 项目概述

Desktop客户端的**技术基础设施层**，提供横向技术能力支持，包括HTTP通信、缓存、配置、安全、性能、诊断等核心服务。

### 设计原则

- **职责单一**：仅包含技术基础设施，不含业务逻辑
- **可复用**：所有模块可共享使用
- **高性能**：优化的缓存、连接池、对象池实现
- **可观测**：完善的日志、诊断、性能监控

---

## 目录结构

```
LYBT.Desktop.Foundation/
├── Caching/                    # 缓存服务
│   ├── Interfaces/
│   ├── MemoryCacheService.cs  # 内存缓存实现
│   └── CacheKeyGenerator.cs   # 缓存键生成器
│
├── Configuration/              # 配置管理
│   ├── Interfaces/
│   ├── AppSettingsManager.cs  # 应用配置管理
│   └── ConfigurationExtensions.cs
│
├── Diagnostics/                # 诊断服务
│   ├── Interfaces/
│   ├── DiagnosticLogger.cs    # 诊断日志
│   └── PerformanceMonitor.cs  # 性能监控
│
├── ErrorHandling/              # 异常处理
│   ├── Interfaces/
│   ├── ExceptionHandler.cs    # 统一异常处理
│   └── RetryPolicy.cs         # 重试策略
│
├── Http/                       # HTTP客户端
│   ├── Interfaces/
│   ├── BaseApiClient.cs       # 基础HTTP客户端
│   ├── HttpClientFactory.cs   # HTTP工厂
│   └── HttpExtensions.cs      # HTTP扩展方法
│
├── Performance/                # 性能优化
│   ├── Interfaces/
│   ├── ObjectPoolProvider.cs  # 对象池
│   └── PerformanceCounter.cs  # 性能计数器
│
├── Security/                   # 安全服务
│   ├── Interfaces/
│   ├── EncryptionService.cs   # 加密服务
│   └── TokenManager.cs        # 令牌管理
│
├── Session/                    # 会话管理
│   ├── Interfaces/
│   ├── SessionManager.cs      # 会话管理器
│   └── SessionStorage.cs      # 会话存储
│
├── Settings/                   # 设置管理
│   ├── Interfaces/
│   ├── UserSettingsManager.cs # 用户设置
│   └── SystemSettingsManager.cs # 系统设置
│
├── HealthCheck/                # 健康检查
│   ├── Interfaces/
│   ├── HealthCheckService.cs  # 健康检查服务
│   └── ConnectivityChecker.cs # 连通性检查
│
├── Modules/                    # 模块注册
│   ├── Interfaces/
│   └── ModuleRegistrar.cs     # 模块注册器
│
├── Handlers/                   # 处理器
│   ├── MessageHandlers/       # 消息处理器
│   └── EventHandlers/         # 事件处理器
│
└── Extensions/                 # 扩展方法
    ├── StringExtensions.cs
    ├── CollectionExtensions.cs
    └── DateTimeExtensions.cs
```

---

## 主要功能

### 1. HTTP通信（Http/）
- ✅ 基于Refit的类型安全HTTP客户端
- ✅ Polly弹性策略（重试、熔断、超时）
- ✅ HTTP连接池管理
- ✅ 请求/响应日志记录

### 2. 缓存服务（Caching/）
- ✅ 内存缓存（IMemoryCache）
- ✅ 分层缓存策略
- ✅ 缓存键自动生成
- ✅ 缓存过期策略

### 3. 配置管理（Configuration/）
- ✅ appsettings.json配置加载
- ✅ 环境变量覆盖
- ✅ 配置热重载
- ✅ 强类型配置绑定

### 4. 安全服务（Security/）
- ✅ 数据加密/解密（DPAPI）
- ✅ JWT令牌管理
- ✅ 敏感数据保护
- ✅ 密码哈希

### 5. 性能优化（Performance/）
- ✅ 对象池（ObjectPool）
- ✅ 性能计数器
- ✅ 内存压力监控
- ✅ GC优化建议

### 6. 诊断监控（Diagnostics/）
- ✅ 结构化日志（ILogger）
- ✅ 性能追踪（DiagnosticSource）
- ✅ 异常遥测
- ✅ 健康检查端点

---

## 依赖项

### NuGet包
- Refit - HTTP客户端
- Polly - 弹性策略
- Microsoft.Extensions.Http - HTTP工厂
- Microsoft.Extensions.Caching.Memory - 内存缓存
- System.Security.Cryptography.ProtectedData - 数据保护
- System.Diagnostics.DiagnosticSource - 诊断
- System.Diagnostics.PerformanceCounter - 性能计数器

### 项目引用
- LYBT.Shared.Models - 共享模型
- LYBT.Shared.Interfaces - 共享接口
- LYBT.Shared.Utilities - 共享工具类

---

## 使用示例

### HTTP客户端
```csharp
public class PatientRepository : IPatientRepository
{
    private readonly HttpClient _httpClient;

    public PatientRepository(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
    {
        var response = await _httpClient.GetAsync($"/api/patients/{id}");
        // ...
    }
}
```

### 缓存服务
```csharp
public class CachedPatientRepository : IPatientRepository
{
    private readonly IMemoryCache _cache;
    private readonly IPatientRepository _inner;

    public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
    {
        var cacheKey = $"patient:{id}";
        if (_cache.TryGetValue(cacheKey, out PatientDto cached))
            return ServiceResult<PatientDto>.Success(cached);

        var result = await _inner.GetByIdAsync(id);
        if (result.IsSuccess)
            _cache.Set(cacheKey, result.Data, TimeSpan.FromMinutes(5));

        return result;
    }
}
```

### 配置管理
```csharp
public class HttpClientConfig
{
    public string BaseUrl { get; set; } = "https://localhost:5001";
    public int TimeoutSeconds { get; set; } = 30;
}

// Startup
services.Configure<HttpClientConfig>(Configuration.GetSection("HttpClient"));
```

---

## 迁移说明

本项目由 `Desktop.Services` 项目中的技术基础设施部分迁移而来（Issue #1114 Phase 1）。

### 迁移来源
- `Desktop.Services/Caching/` → `Desktop.Foundation/Caching/`
- `Desktop.Services/Configuration/` → `Desktop.Foundation/Configuration/`
- `Desktop.Services/Http/` → `Desktop.Foundation/Http/`
- ... (13个目录)

### 迁移影响
- ✅ 命名空间变更：`LYBT.Desktop.Services.*` → `LYBT.Desktop.Foundation.*`
- ✅ 项目引用更新：引用`Desktop.Services`的项目需更新为`Desktop.Foundation`
- ✅ 依赖注入注册：需在Shell中更新DI注册

---

## 架构决策记录

详见：[ADR-005: Desktop端模块化架构重构](../../../../docs/architecture/adr/ADR-005-desktop-modular-architecture.md)

---

## 后续计划

### Phase 1.3 - 迁移技术基础设施
- [ ] 迁移Caching/目录（含接口、实现、单元测试）
- [ ] 迁移Configuration/目录
- [ ] 迁移Diagnostics/目录
- [ ] 迁移ErrorHandling/目录
- [ ] 迁移Http/目录
- [ ] 迁移Performance/目录
- [ ] 迁移Security/目录
- [ ] 迁移Session/目录
- [ ] 迁移Settings/目录
- [ ] 迁移HealthCheck/目录
- [ ] 迁移Modules/目录
- [ ] 迁移Handlers/目录
- [ ] 迁移Extensions/目录

### Phase 1.6 - 验证
- [ ] 编译验证（0错误0警告）
- [ ] 单元测试通过
- [ ] 更新依赖注入配置
- [ ] 更新项目引用

---

🤖 Generated with [Claude Code](https://claude.com/claude-code)
