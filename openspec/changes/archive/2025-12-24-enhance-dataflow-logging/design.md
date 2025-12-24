# 技术设计: enhance-dataflow-logging

**Change ID**: enhance-dataflow-logging
**Created**: 2025-12-24

---

## 1. 架构概览

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           Desktop Client                                 │
├─────────────────────────────────────────────────────────────────────────┤
│  ViewModel                                                               │
│     │                                                                    │
│     ▼                                                                    │
│  CommandHandler ─────► [LOG: Command执行]                                │
│     │                                                                    │
│     ▼                                                                    │
│  Refit API Client                                                        │
│     │                                                                    │
│     ▼                                                                    │
│  LoggingHttpHandler ─► [LOG: HTTP Request/Response]                     │
│     │                  ─► [CorrelationId传递]                            │
└─────│───────────────────────────────────────────────────────────────────┘
      │
      │ HTTP (traceparent header)
      ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                           WebAPI Server                                  │
├─────────────────────────────────────────────────────────────────────────┤
│  CorrelationIdMiddleware ─► [提取/生成CorrelationId]                     │
│     │                                                                    │
│     ▼                                                                    │
│  Controller                                                              │
│     │                                                                    │
│     ▼                                                                    │
│  ApiLoggingFilter ───────► [LOG: Action执行]                             │
│     │                                                                    │
│     ▼                                                                    │
│  Service Layer                                                           │
│     │                                                                    │
│     ▼                                                                    │
│  Repository ─────────────► [LOG: 数据操作]                               │
│     │                                                                    │
│     ▼                                                                    │
│  EF Core DbContext ──────► [LOG: SQL查询(可选)]                          │
└─────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Desktop端设计

### 2.1 LoggingHttpHandler

```csharp
namespace LYBT.Desktop.Infrastructure.Http;

/// <summary>
/// HTTP请求/响应日志处理器
/// 记录所有API调用的请求和响应信息
/// </summary>
public class LoggingHttpHandler : DelegatingHandler
{
    private readonly ILogger<LoggingHttpHandler> _logger;
    private readonly ISensitiveDataMasker _sensitiveDataMasker;

    public LoggingHttpHandler(
        ILogger<LoggingHttpHandler> logger,
        ISensitiveDataMasker sensitiveDataMasker)
    {
        _logger = logger;
        _sensitiveDataMasker = sensitiveDataMasker;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, 
        CancellationToken cancellationToken)
    {
        // 获取或创建CorrelationId
        var activity = Activity.Current;
        var correlationId = activity?.Id ?? Guid.NewGuid().ToString("N");
        
        // 添加traceparent header用于分布式追踪
        if (activity != null && !request.Headers.Contains("traceparent"))
        {
            request.Headers.Add("traceparent", activity.Id);
        }

        var sw = Stopwatch.StartNew();
        
        // 记录请求
        _logger.LogInformation(
            "[HTTP] >>> {Method} {Uri} CorrelationId={CorrelationId}",
            request.Method,
            _sensitiveDataMasker.MaskUri(request.RequestUri?.ToString() ?? ""),
            correlationId);

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();

            // 记录响应
            var logLevel = response.IsSuccessStatusCode ? LogLevel.Information : LogLevel.Warning;
            _logger.Log(logLevel,
                "[HTTP] <<< {StatusCode} {Uri} Duration={Duration}ms CorrelationId={CorrelationId}",
                (int)response.StatusCode,
                _sensitiveDataMasker.MaskUri(request.RequestUri?.ToString() ?? ""),
                sw.ElapsedMilliseconds,
                correlationId);

            // 非成功响应记录Body
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogWarning(
                    "[HTTP] Error Response Body: {Body} CorrelationId={CorrelationId}",
                    _sensitiveDataMasker.Mask(body),
                    correlationId);
            }

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex,
                "[HTTP] !!! {Method} {Uri} failed after {Duration}ms CorrelationId={CorrelationId}",
                request.Method,
                request.RequestUri,
                sw.ElapsedMilliseconds,
                correlationId);
            throw;
        }
    }
}
```

### 2.2 注册方式

```csharp
// ServiceCollectionExtensions.cs
public static IServiceCollection AddApiClients(this IServiceCollection services, string baseUrl)
{
    // 注册LoggingHttpHandler
    services.AddTransient<LoggingHttpHandler>();
    
    // 配置HttpClient
    services.AddRefitClient<IUserApi>()
        .ConfigureHttpClient(c => c.BaseAddress = new Uri(baseUrl))
        .AddHttpMessageHandler<LoggingHttpHandler>();  // 添加日志Handler
    
    // 其他API...
    
    return services;
}
```

---

## 3. Server端设计

### 3.1 CorrelationIdMiddleware

```csharp
namespace LYBT.Server.Core.Middleware;

/// <summary>
/// CorrelationId中间件
/// 从请求头提取或生成CorrelationId，并设置到日志上下文
/// </summary>
public class CorrelationIdMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CorrelationIdMiddleware> _logger;

    public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // 尝试从traceparent header提取
        var correlationId = context.Request.Headers["traceparent"].FirstOrDefault();
        
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString("N");
        }

        // 设置到HttpContext
        context.TraceIdentifier = correlationId;
        
        // 设置到Activity
        var activity = Activity.Current ?? new Activity("Request");
        if (Activity.Current == null)
        {
            activity.Start();
        }

        // 添加到日志上下文
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            // 添加响应头
            context.Response.Headers["X-Correlation-Id"] = correlationId;
            
            await _next(context);
        }
    }
}
```

### 3.2 ApiLoggingFilter

```csharp
namespace LYBT.Server.Core.Filters;

/// <summary>
/// API日志过滤器
/// 记录所有Controller Action的执行情况
/// </summary>
public class ApiLoggingFilter : IAsyncActionFilter
{
    private readonly ILogger<ApiLoggingFilter> _logger;

    public ApiLoggingFilter(ILogger<ApiLoggingFilter> logger)
    {
        _logger = logger;
    }

    public async Task OnActionExecutionAsync(
        ActionExecutingContext context,
        ActionExecutionDelegate next)
    {
        var actionName = context.ActionDescriptor.DisplayName;
        var correlationId = context.HttpContext.TraceIdentifier;
        var sw = Stopwatch.StartNew();

        // 记录Action开始
        _logger.LogInformation(
            "[API] >>> {Action} started. CorrelationId={CorrelationId}",
            actionName,
            correlationId);

        // 记录参数（脱敏）
        if (context.ActionArguments.Any())
        {
            _logger.LogDebug(
                "[API] Parameters: {Parameters} CorrelationId={CorrelationId}",
                SanitizeParameters(context.ActionArguments),
                correlationId);
        }

        var executedContext = await next();
        sw.Stop();

        // 记录Action结束
        if (executedContext.Exception != null)
        {
            _logger.LogError(executedContext.Exception,
                "[API] !!! {Action} failed after {Duration}ms. CorrelationId={CorrelationId}",
                actionName,
                sw.ElapsedMilliseconds,
                correlationId);
        }
        else
        {
            _logger.LogInformation(
                "[API] <<< {Action} completed in {Duration}ms. CorrelationId={CorrelationId}",
                actionName,
                sw.ElapsedMilliseconds,
                correlationId);
        }
    }

    private static string SanitizeParameters(IDictionary<string, object?> parameters)
    {
        var sanitized = parameters
            .Where(p => p.Value != null)
            .Select(p => $"{p.Key}={SanitizeValue(p.Value)}");
        return string.Join(", ", sanitized);
    }

    private static string SanitizeValue(object? value)
    {
        if (value == null) return "null";
        
        var type = value.GetType();
        if (type.IsPrimitive || type == typeof(string) || type == typeof(Guid))
        {
            return value.ToString() ?? "null";
        }
        
        return $"[{type.Name}]";
    }
}
```

### 3.3 Repository日志增强

```csharp
namespace LYBT.Server.Core.Repositories;

/// <summary>
/// Repository基类 - 带日志支持
/// </summary>
public abstract class RepositoryBase<TEntity, TKey> : IRepository<TEntity, TKey>
    where TEntity : class
{
    protected readonly DbContext DbContext;
    protected readonly DbSet<TEntity> DbSet;
    protected readonly ILogger Logger;
    private readonly string _entityName;

    protected RepositoryBase(DbContext dbContext, ILogger logger)
    {
        DbContext = dbContext;
        DbSet = dbContext.Set<TEntity>();
        Logger = logger;
        _entityName = typeof(TEntity).Name;
    }

    public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("[Repository] {Entity}.GetById({Id})", _entityName, id);
        
        var entity = await DbSet.FindAsync(new object[] { id! }, cancellationToken);
        
        Logger.LogDebug("[Repository] {Entity}.GetById result: {Found}", _entityName, entity != null);
        return entity;
    }

    public virtual async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("[Repository] {Entity}.Add", _entityName);
        
        var entry = await DbSet.AddAsync(entity, cancellationToken);
        
        Logger.LogDebug("[Repository] {Entity}.Add completed", _entityName);
        return entry.Entity;
    }

    public virtual Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("[Repository] {Entity}.Update", _entityName);
        
        DbSet.Update(entity);
        
        Logger.LogDebug("[Repository] {Entity}.Update completed", _entityName);
        return Task.CompletedTask;
    }

    public virtual Task DeleteAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Logger.LogDebug("[Repository] {Entity}.Delete", _entityName);
        
        DbSet.Remove(entity);
        
        Logger.LogDebug("[Repository] {Entity}.Delete completed", _entityName);
        return Task.CompletedTask;
    }
}
```

---

## 4. 日志级别规范

| 组件 | 日志级别 | 记录内容 |
|------|----------|----------|
| HTTP Handler | Information | 请求/响应概要 |
| HTTP Handler | Warning | 非2xx响应 |
| HTTP Handler | Debug | 请求/响应Body |
| API Filter | Information | Action开始/结束 |
| API Filter | Debug | Action参数 |
| API Filter | Error | Action异常 |
| Repository | Debug | 所有CRUD操作 |
| EF Core | Warning | 慢查询(>100ms) |

---

## 5. 敏感数据处理

### 需要脱敏的字段

- Password / PasswordHash
- Token / AccessToken / RefreshToken
- IdNumber (身份证号)
- PhoneNumber (手机号)
- Email (邮箱)

### 脱敏规则

```csharp
public class SensitiveDataMasker : ISensitiveDataMasker
{
    private static readonly string[] SensitiveFields = 
    {
        "password", "token", "secret", "key", "credential",
        "idnumber", "phonenumber", "email", "address"
    };

    public string Mask(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        
        // JSON中的敏感字段替换为***
        foreach (var field in SensitiveFields)
        {
            var pattern = $@"""{field}""\s*:\s*""[^""]*""";
            input = Regex.Replace(input, pattern, 
                $@"""{field}"":""***""", 
                RegexOptions.IgnoreCase);
        }
        
        return input;
    }

    public string MaskUri(string uri)
    {
        // 替换URL中的敏感参数
        return Regex.Replace(uri, 
            @"(password|token|key)=([^&]*)", 
            "$1=***", 
            RegexOptions.IgnoreCase);
    }
}
```

---

## 6. 性能考虑

1. **异步日志**: Serilog默认异步写入
2. **结构化日志**: 避免字符串拼接
3. **条件日志**: Debug级别日志使用LoggerMessage
4. **Buffer写入**: 配置适当的flush间隔

### LoggerMessage优化示例

```csharp
public static partial class LogMessages
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Debug,
        Message = "[Repository] {EntityName}.GetById({Id})")]
    public static partial void LogRepositoryGetById(
        this ILogger logger, string entityName, object id);
}
```

---

## 7. 配置示例

### Desktop appsettings.json

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "LYBT.Desktop.Infrastructure.Http": "Information",
        "System.Net.Http": "Warning",
        "Microsoft": "Warning"
      }
    }
  }
}
```

### Server appsettings.json

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "LYBT.Server.Core.Filters": "Information",
        "LYBT.Server.Core.Repositories": "Debug",
        "Microsoft.EntityFrameworkCore": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Warning"
      }
    }
  }
}
```

---

## 8. 验证方法

### 测试场景: 用户创建

1. Desktop发起创建用户请求
2. 检查Desktop日志:
   ```
   [HTTP] >>> POST /api/users CorrelationId=abc123
   [HTTP] <<< 201 /api/users Duration=150ms CorrelationId=abc123
   ```
3. 检查Server日志:
   ```
   [API] >>> UsersController.Create started. CorrelationId=abc123
   [Repository] User.Add
   [Repository] User.Add completed
   [API] <<< UsersController.Create completed in 120ms. CorrelationId=abc123
   ```
4. 确认CorrelationId一致，可追踪完整链路
