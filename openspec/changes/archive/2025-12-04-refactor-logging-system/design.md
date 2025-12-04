# Technical Design: refactor-logging-system

## Architecture Overview

### 日志与错误处理统一架构

```
┌─────────────────────────────────────────────────────────────────────────┐
│                        Application Layer                                 │
│  ┌─────────────────────┐              ┌─────────────────────┐           │
│  │     WPF Client      │              │    ASP.NET API      │           │
│  │   (Desktop App)     │              │    (Web Server)     │           │
│  │                     │              │                     │           │
│  │ ┌─────────────────┐ │              │ ┌─────────────────┐ │           │
│  │ │StandardException│ │              │ │IExceptionHandler│ │           │
│  │ │   Handler       │ │              │ │  (多个处理器)    │ │           │
│  │ └─────────────────┘ │              │ └─────────────────┘ │           │
│  └──────────┬──────────┘              └──────────┬──────────┘           │
│             │                                    │                       │
│             │ X-Correlation-ID                   │ Problem Details       │
│             │ Problem Details Response           │ (RFC 7807)            │
│             ▼                                    ▼                       │
├─────────────────────────────────────────────────────────────────────────┤
│                    Error Handling Infrastructure                         │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │                    Unified Exception Hierarchy                     │  │
│  │  AppException ──┬── BusinessException (业务规则违反)               │  │
│  │                 ├── ValidationException (输入验证失败)             │  │
│  │                 ├── NotFoundException (资源不存在)                 │  │
│  │                 ├── ConflictException (并发冲突) [新增]            │  │
│  │                 └── UnauthorizedException (权限不足) [新增]        │  │
│  └───────────────────────────────────────────────────────────────────┘  │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │                    ErrorCode Enumeration                          │  │
│  │  Common (1xxx) │ Auth (2xxx) │ Patient (3xxx) │ MedicalCase (4xxx)│  │
│  └───────────────────────────────────────────────────────────────────┘  │
├─────────────────────────────────────────────────────────────────────────┤
│                    Logging Infrastructure                                │
│  ┌───────────────────────────────────────────────────────────────────┐  │
│  │              Serilog (Unified Logging Framework)                  │  │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐            │  │
│  │  │   Enrichers  │  │  Converters  │  │    Sinks     │            │  │
│  │  │ -Correlation │  │ -Sensitive   │  │ -File        │            │  │
│  │  │ -Machine     │  │  DataMasking │  │ -Console     │            │  │
│  │  │ -Thread      │  │              │  │              │            │  │
│  │  │ -User        │  │              │  │              │            │  │
│  │  └──────────────┘  └──────────────┘  └──────────────┘            │  │
│  └───────────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────────┘
```

---

# Part 1: Logging System Design

## Design Decisions

### 1. Serilog两阶段初始化 (Server端)

**问题**: 应用启动配置阶段的异常无法被记录

**方案**: 使用Bootstrap Logger + Final Logger模式

```csharp
// Phase 1: Bootstrap Logger (Program.cs顶部)
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/bootstrap-.log", rollingInterval: RollingInterval.Day)
    .CreateBootstrapLogger();

// Phase 2: Final Logger (after configuration loaded)
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationId()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WithSensitiveDataMasking());
```

### 2. CorrelationId端到端追踪

**问题**: 无法关联客户端请求与服务端日志

**方案**: 使用HTTP Header传递CorrelationId

#### Server端中间件

```csharp
public class CorrelationIdMiddleware
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");
        
        context.Items["CorrelationId"] = correlationId;
        context.Response.Headers[CorrelationIdHeader] = correlationId;
        
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            await next(context);
        }
    }
}
```

#### Client端HttpClient配置

```csharp
public class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private const string CorrelationIdHeader = "X-Correlation-ID";
    
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var correlationId = CorrelationIdContext.Current ?? Guid.NewGuid().ToString("N");
        request.Headers.Add(CorrelationIdHeader, correlationId);
        
        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            return await base.SendAsync(request, cancellationToken);
        }
    }
}
```

### 3. 敏感数据统一脱敏

**问题**: LogSanitizer和SensitiveDataMasker功能重叠

**方案**: 整合为统一的SensitiveDataMasker,作为Serilog的Destructuring Policy

```csharp
// 统一的敏感数据处理配置
builder.Host.UseSerilog((context, services, configuration) => configuration
    .Destructure.With<SensitiveDataDestructuringPolicy>()
    .Enrich.With<SensitiveDataEnricher>());
```

**保留LogSanitizer**: 用于非结构化文本日志的正则脱敏(如SQL连接字符串)

### 4. Client端Serilog集成

**问题**: 当前仅使用Debug provider,无文件日志

**方案**: 引入Serilog.Extensions.Logging,统一日志输出

```csharp
// Client端日志配置
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .Enrich.WithProperty("Application", "LYBT.Desktop")
    .WriteTo.File(
        path: Path.Combine(AppDataPath, "logs", "lybt-desktop-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

// DI注册
services.AddLogging(builder => builder.AddSerilog(dispose: true));
```

### 5. 日志输出格式统一

**格式模板**:
```
{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] [{SourceContext}] {Message:lj}{NewLine}{Exception}
```

**示例输出**:
```
2025-12-04 16:00:00.123 +08:00 [INF] [a1b2c3d4] [LYBT.WebAPI.Controllers.PatientController] 查询患者列表成功 PatientCount=25
2025-12-04 16:00:00.456 +08:00 [ERR] [a1b2c3d4] [LYBT.Module.MedicalCase.Services.MedicalCaseService] 保存医案失败 MedicalCaseId=123
System.InvalidOperationException: 并发冲突
   at ...
```

### 6. 日志分级存储 (Tiered Storage)

**问题**: 重要日志仅存文件,无法长期保存和高效查询;生产环境调试困难

**方案**: 多级存储策略 + 动态调试开关

#### 存储策略

| 级别 | 文件存储 | 数据库存储 | 保留策略 |
|------|---------|-----------|---------|
| Debug | 可选 | 否 | 7天(开启时) |
| Information | 是 | 否 | 30天 |
| Warning | 是 | 是 | 文件30天/数据库90天 |
| Error/Fatal | 是 | 是 | 文件30天/数据库永久 |

#### 数据库表设计 (SystemLogs)

```sql
-- 系统日志表设计
CREATE TABLE [dbo].[SystemLogs] (
    [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
    [Timestamp] DATETIMEOFFSET NOT NULL,
    [Level] NVARCHAR(16) NOT NULL,           -- Warning, Error, Fatal
    [Message] NVARCHAR(MAX) NULL,
    [MessageTemplate] NVARCHAR(MAX) NULL,
    [Exception] NVARCHAR(MAX) NULL,
    [CorrelationId] NVARCHAR(64) NULL,
    [UserId] INT NULL,
    [RequestPath] NVARCHAR(512) NULL,
    [RequestMethod] NVARCHAR(16) NULL,
    [MachineName] NVARCHAR(128) NULL,
    [SourceContext] NVARCHAR(512) NULL,
    [Properties] NVARCHAR(MAX) NULL,         -- JSON格式扩展属性

    INDEX IX_SystemLogs_Timestamp (Timestamp),
    INDEX IX_SystemLogs_Level (Level),
    INDEX IX_SystemLogs_CorrelationId (CorrelationId),
    INDEX IX_SystemLogs_UserId (UserId)
);

-- 日志清理作业 (可选: SQL Agent Job)
-- 保留策略: Warning 90天, Error/Fatal 永久
```

#### Serilog.Sinks.MSSqlServer配置

```csharp
// Program.cs - 添加数据库Sink
builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .Enrich.WithCorrelationId()
    .Enrich.WithMachineName()
    .Enrich.WithThreadId()
    .WriteTo.MSSqlServer(
        connectionString: context.Configuration.GetConnectionString("DefaultConnection"),
        sinkOptions: new MSSqlServerSinkOptions
        {
            TableName = "SystemLogs",
            AutoCreateSqlTable = false,  // 使用Migration创建表
            SchemaName = "dbo"
        },
        restrictedToMinimumLevel: LogEventLevel.Warning,
        columnOptions: GetColumnOptions())
    .WithSensitiveDataMasking());

private static ColumnOptions GetColumnOptions()
{
    var columnOptions = new ColumnOptions();

    // 移除不需要的标准列
    columnOptions.Store.Remove(StandardColumn.Properties);

    // 添加自定义列映射
    columnOptions.AdditionalColumns = new Collection<SqlColumn>
    {
        new SqlColumn { ColumnName = "CorrelationId", DataType = SqlDbType.NVarChar, DataLength = 64 },
        new SqlColumn { ColumnName = "UserId", DataType = SqlDbType.Int, AllowNull = true },
        new SqlColumn { ColumnName = "RequestPath", DataType = SqlDbType.NVarChar, DataLength = 512 },
        new SqlColumn { ColumnName = "RequestMethod", DataType = SqlDbType.NVarChar, DataLength = 16 },
        new SqlColumn { ColumnName = "SourceContext", DataType = SqlDbType.NVarChar, DataLength = 512 },
        new SqlColumn { ColumnName = "Properties", DataType = SqlDbType.NVarChar, DataLength = -1 }  // NVARCHAR(MAX)
    };

    return columnOptions;
}
```

#### LoggingLevelSwitch动态调试

```csharp
// 在Program.cs中注册LoggingLevelSwitch
var levelSwitch = new LoggingLevelSwitch(LogEventLevel.Information);
builder.Services.AddSingleton(levelSwitch);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .MinimumLevel.ControlledBy(levelSwitch)
    // ... 其他配置
);

// Admin API端点 - 动态切换日志级别
[ApiController]
[Route("api/admin/logging")]
[Authorize(Roles = "Admin")]
public class LoggingAdminController : ControllerBase
{
    private readonly LoggingLevelSwitch _levelSwitch;
    private readonly ILogger<LoggingAdminController> _logger;

    [HttpGet("level")]
    public IActionResult GetLevel()
    {
        return Ok(new { Level = _levelSwitch.MinimumLevel.ToString() });
    }

    [HttpPost("level")]
    public IActionResult SetLevel([FromBody] SetLogLevelRequest request)
    {
        if (!Enum.TryParse<LogEventLevel>(request.Level, true, out var level))
            return BadRequest("Invalid log level");

        var previousLevel = _levelSwitch.MinimumLevel;
        _levelSwitch.MinimumLevel = level;

        _logger.LogWarning(
            "日志级别已更改: {PreviousLevel} -> {NewLevel}, 操作者: {User}",
            previousLevel, level, User.Identity?.Name);

        return Ok(new {
            PreviousLevel = previousLevel.ToString(),
            NewLevel = level.ToString()
        });
    }
}

public record SetLogLevelRequest(string Level);
```

#### 配置文件支持

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": { /* ... */ }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/lybt-api-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30
        }
      },
      {
        "Name": "MSSqlServer",
        "Args": {
          "connectionString": "Name=DefaultConnection",
          "tableName": "SystemLogs",
          "restrictedToMinimumLevel": "Warning",
          "autoCreateSqlTable": false
        }
      }
    ]
  },
  "Logging": {
    "EnableDebugInProduction": false,
    "DatabaseRetentionDays": {
      "Warning": 90,
      "Error": -1
    }
  }
}
```

---

# Part 2: Error Handling Design

## Design Decisions

### 7. RFC 7807 Problem Details (Server端)

**问题**: API错误响应格式不统一,Client端难以解析

**方案**: 使用ASP.NET Core 8原生的Problem Details服务

```csharp
// Program.cs
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
    {
        // 添加CorrelationId到每个Problem Details响应
        var correlationId = context.HttpContext.Items["CorrelationId"]?.ToString();
        context.ProblemDetails.Extensions["correlationId"] = correlationId;
        
        // 添加时间戳
        context.ProblemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;
        
        // 添加实例标识(请求路径)
        context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
    };
});

app.UseExceptionHandler();
app.UseStatusCodePages();
```

**标准Problem Details响应格式**:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "业务规则验证失败",
  "status": 400,
  "detail": "患者姓名不能为空",
  "instance": "/api/patients",
  "correlationId": "a1b2c3d4e5f6",
  "timestamp": "2025-12-04T08:00:00.000Z",
  "errorCode": "PATIENT_3001",
  "errors": {
    "Name": ["患者姓名不能为空"]
  }
}
```

### 8. IExceptionHandler多处理器链 (Server端)

**问题**: 当前GlobalExceptionHandler处理所有异常,职责过重

**方案**: 使用ASP.NET Core 8的IExceptionHandler接口,实现责任链模式

```csharp
// 业务异常处理器 (优先级最高)
public class BusinessExceptionHandler : IExceptionHandler
{
    private readonly ILogger<BusinessExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        if (exception is not AppException appException)
            return false; // 不处理,交给下一个处理器

        var statusCode = appException switch
        {
            ValidationException => StatusCodes.Status400BadRequest,
            NotFoundException => StatusCodes.Status404NotFound,
            ConflictException => StatusCodes.Status409Conflict,
            UnauthorizedException => StatusCodes.Status403Forbidden,
            BusinessException => StatusCodes.Status422UnprocessableEntity,
            _ => StatusCodes.Status400BadRequest
        };

        _logger.LogWarning(exception, 
            "业务异常: {ErrorCode} - {Message}", 
            appException.ErrorCode, appException.Message);

        httpContext.Response.StatusCode = statusCode;
        
        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Title = appException.UserMessage ?? "业务处理失败",
                Detail = appException.Message,
                Status = statusCode,
                Extensions = 
                {
                    ["errorCode"] = appException.ErrorCode
                }
            },
            Exception = exception
        });
    }
}

// 系统异常处理器 (兜底)
public class SystemExceptionHandler : IExceptionHandler
{
    private readonly ILogger<SystemExceptionHandler> _logger;
    private readonly IProblemDetailsService _problemDetailsService;
    private readonly IHostEnvironment _environment;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, 
        Exception exception, 
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "系统异常: {Message}", exception.Message);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        var problemDetails = new ProblemDetails
        {
            Title = "服务器内部错误",
            Status = StatusCodes.Status500InternalServerError,
            Detail = _environment.IsDevelopment() 
                ? exception.Message 
                : "请联系系统管理员"
        };

        return await _problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }
}

// 注册处理器链 (按优先级顺序)
builder.Services.AddExceptionHandler<BusinessExceptionHandler>();
builder.Services.AddExceptionHandler<SystemExceptionHandler>();
```

### 9. ErrorCode分层枚举体系

**问题**: 缺乏标准化的错误码,不利于错误追踪和统计

**方案**: 建立分层错误码体系

```csharp
// LYBT.Shared.Models/Errors/ErrorCode.cs

/// <summary>
/// 统一错误码枚举
/// 格式: 模块前缀(1位) + 类别(1位) + 序号(3位)
/// </summary>
public enum ErrorCode
{
    // ===== 通用错误 (0xxxx) =====
    Unknown = 00000,
    ValidationFailed = 00001,
    NotFound = 00002,
    Conflict = 00003,
    Unauthorized = 00004,
    Forbidden = 00005,
    
    // ===== 认证模块 (1xxxx) =====
    Auth_InvalidCredentials = 10001,
    Auth_TokenExpired = 10002,
    Auth_TokenInvalid = 10003,
    Auth_UserLocked = 10004,
    Auth_PasswordExpired = 10005,
    
    // ===== 用户模块 (2xxxx) =====
    User_NotFound = 20001,
    User_DuplicateUsername = 20002,
    User_InvalidRole = 20003,
    
    // ===== 患者模块 (3xxxx) =====
    Patient_NotFound = 30001,
    Patient_DuplicateIdNumber = 30002,
    Patient_InvalidPhoneNumber = 30003,
    
    // ===== 医案模块 (4xxxx) =====
    MedicalCase_NotFound = 40001,
    MedicalCase_InvalidStatus = 40002,
    MedicalCase_ConcurrencyConflict = 40003,
    MedicalCase_CannotEdit = 40004,
    MedicalCase_CannotDelete = 40005,
    MedicalCase_CannotSubmit = 40006,
    
    // ===== 处方模块 (5xxxx) =====
    Prescription_NotFound = 50001,
    Prescription_InvalidDosage = 50002,
    
    // ===== 药材模块 (6xxxx) =====
    Herb_NotFound = 60001,
    Herb_InsufficientStock = 60002,
    
    // ===== 方剂模块 (7xxxx) =====
    Formula_NotFound = 70001,
    Formula_InvalidComposition = 70002
}

/// <summary>
/// ErrorCode扩展方法
/// </summary>
public static class ErrorCodeExtensions
{
    public static string ToCode(this ErrorCode errorCode) 
        => $"ERR_{(int)errorCode:D5}";
    
    public static string GetDefaultMessage(this ErrorCode errorCode) => errorCode switch
    {
        ErrorCode.ValidationFailed => "输入数据验证失败",
        ErrorCode.NotFound => "请求的资源不存在",
        ErrorCode.Conflict => "数据冲突,请刷新后重试",
        ErrorCode.Unauthorized => "您没有权限执行此操作",
        // ... 其他映射
        _ => "发生未知错误"
    };
}
```

### 10. 异常类层次结构扩展

**问题**: 现有异常类型不够细分

**方案**: 扩展AppException层次结构

```csharp
// 基类增强
public class AppException : Exception
{
    public ErrorCode ErrorCode { get; }
    public string? UserMessage { get; }
    public bool ShowDetailToUser { get; }
    public IDictionary<string, object>? Extensions { get; }
    
    public AppException(
        ErrorCode errorCode, 
        string message, 
        string? userMessage = null,
        Exception? innerException = null,
        IDictionary<string, object>? extensions = null) 
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        UserMessage = userMessage ?? errorCode.GetDefaultMessage();
        Extensions = extensions;
    }
}

// 新增: 并发冲突异常
public class ConflictException : AppException
{
    public string? ResourceType { get; }
    public object? ResourceId { get; }
    public byte[]? ExpectedVersion { get; }
    public byte[]? ActualVersion { get; }
    
    public ConflictException(
        string resourceType, 
        object resourceId,
        byte[]? expectedVersion = null,
        byte[]? actualVersion = null)
        : base(
            ErrorCode.Conflict, 
            $"资源 {resourceType}:{resourceId} 已被其他用户修改",
            "数据已被修改,请刷新后重试")
    {
        ResourceType = resourceType;
        ResourceId = resourceId;
        ExpectedVersion = expectedVersion;
        ActualVersion = actualVersion;
    }
}

// 新增: 权限不足异常
public class UnauthorizedException : AppException
{
    public string? RequiredPermission { get; }
    public string? Resource { get; }
    
    public UnauthorizedException(
        string? requiredPermission = null,
        string? resource = null)
        : base(
            ErrorCode.Unauthorized, 
            $"缺少权限: {requiredPermission} 访问资源: {resource}",
            "您没有权限执行此操作")
    {
        RequiredPermission = requiredPermission;
        Resource = resource;
    }
}
```

### 11. Client端错误处理重构

**问题**: StandardExceptionHandler与Server端处理模式不一致

**方案**: 重构为Problem Details感知的错误处理

```csharp
// Problem Details响应模型
public class ProblemDetailsResponse
{
    public string? Type { get; set; }
    public string? Title { get; set; }
    public int? Status { get; set; }
    public string? Detail { get; set; }
    public string? Instance { get; set; }
    public string? CorrelationId { get; set; }
    public string? ErrorCode { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    public Dictionary<string, string[]>? Errors { get; set; }
}

// API响应解析服务
public class ApiErrorParser
{
    public async Task<ProblemDetailsResponse?> ParseErrorResponseAsync(
        HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode)
            return null;
            
        var content = await response.Content.ReadAsStringAsync();
        
        try
        {
            return JsonSerializer.Deserialize<ProblemDetailsResponse>(content);
        }
        catch
        {
            // 非Problem Details格式,构造默认响应
            return new ProblemDetailsResponse
            {
                Status = (int)response.StatusCode,
                Title = response.ReasonPhrase,
                Detail = content
            };
        }
    }
}

// 重构后的StandardExceptionHandler
public class StandardExceptionHandler
{
    private readonly ILogger<StandardExceptionHandler> _logger;
    private readonly IErrorMessageMapper _messageMapper;
    
    public ServiceResult<T> Handle<T>(Exception exception, string? correlationId = null)
    {
        using (LogContext.PushProperty("CorrelationId", correlationId ?? "N/A"))
        {
            _logger.LogError(exception, 
                "操作失败 {@ErrorDetails}", 
                new { 
                    ExceptionType = exception.GetType().Name,
                    exception.Message 
                });
        }
        
        var userMessage = _messageMapper.GetUserMessage(exception);
        var errorCode = GetErrorCode(exception);
        
        return ServiceResult<T>.Failure(userMessage, errorCode);
    }
    
    public async Task<ServiceResult<T>> HandleApiErrorAsync<T>(
        HttpResponseMessage response, 
        string? correlationId = null)
    {
        var problemDetails = await _apiErrorParser.ParseErrorResponseAsync(response);
        
        using (LogContext.PushProperty("CorrelationId", 
            problemDetails?.CorrelationId ?? correlationId ?? "N/A"))
        {
            _logger.LogWarning(
                "API请求失败 {@ProblemDetails}", 
                problemDetails);
        }
        
        var userMessage = problemDetails?.Title ?? "请求失败";
        return ServiceResult<T>.Failure(userMessage, problemDetails?.ErrorCode);
    }
}
```

### 12. 错误消息映射可配置化

**问题**: ExceptionMessageMapper硬编码,扩展性差

**方案**: 使用配置文件 + 默认映射结合

```csharp
// 可配置的错误消息映射
public interface IErrorMessageMapper
{
    string GetUserMessage(Exception exception);
    string GetUserMessage(ErrorCode errorCode);
}

public class ConfigurableErrorMessageMapper : IErrorMessageMapper
{
    private readonly IOptions<ErrorMessageOptions> _options;
    private readonly ILogger<ConfigurableErrorMessageMapper> _logger;
    
    // 默认映射表
    private static readonly Dictionary<Type, string> DefaultTypeMessages = new()
    {
        [typeof(ValidationException)] = "输入数据验证失败,请检查后重试",
        [typeof(NotFoundException)] = "请求的数据不存在",
        [typeof(ConflictException)] = "数据已被其他用户修改,请刷新后重试",
        [typeof(UnauthorizedException)] = "您没有权限执行此操作",
        [typeof(HttpRequestException)] = "网络连接失败,请检查网络后重试",
        [typeof(TaskCanceledException)] = "操作已取消",
        [typeof(TimeoutException)] = "操作超时,请稍后重试"
    };
    
    public string GetUserMessage(Exception exception)
    {
        // 1. 优先使用AppException的UserMessage
        if (exception is AppException appEx && !string.IsNullOrEmpty(appEx.UserMessage))
            return appEx.UserMessage;
        
        // 2. 查找配置文件中的自定义消息
        var exceptionTypeName = exception.GetType().Name;
        if (_options.Value.TypeMessages.TryGetValue(exceptionTypeName, out var configMessage))
            return configMessage;
        
        // 3. 使用默认映射
        if (DefaultTypeMessages.TryGetValue(exception.GetType(), out var defaultMessage))
            return defaultMessage;
        
        // 4. 兜底消息
        return "操作失败,请稍后重试";
    }
    
    public string GetUserMessage(ErrorCode errorCode)
    {
        // 1. 查找配置文件
        var codeString = errorCode.ToCode();
        if (_options.Value.CodeMessages.TryGetValue(codeString, out var configMessage))
            return configMessage;
        
        // 2. 使用枚举默认消息
        return errorCode.GetDefaultMessage();
    }
}

// 配置选项
public class ErrorMessageOptions
{
    public Dictionary<string, string> TypeMessages { get; set; } = new();
    public Dictionary<string, string> CodeMessages { get; set; } = new();
}
```

---

# Part 3: Component Design

## 新增组件

### Server端

```
LYBT.Infrastructure/
└── Logging/
    ├── SensitiveDataMasker.cs              # 已存在,需整合LogSanitizer功能
    ├── SensitiveDataDestructuringPolicy.cs # 已存在
    ├── CorrelationIdEnricher.cs            # 新增: CorrelationId日志富集
    ├── UserContextEnricher.cs              # 新增: 用户上下文富集
    └── SerilogConfiguration.cs             # 新增: 统一配置入口

LYBT.WebAPI/
├── Middleware/
│   ├── CorrelationIdMiddleware.cs          # 新增: CorrelationId处理
│   └── GlobalExceptionHandler.cs           # 删除: 由IExceptionHandler替代
├── ExceptionHandlers/                       # 新增目录
│   ├── BusinessExceptionHandler.cs         # 新增: 业务异常处理
│   └── SystemExceptionHandler.cs           # 新增: 系统异常处理
└── Extensions/
    └── ProblemDetailsExtensions.cs         # 新增: Problem Details配置扩展

LYBT.Shared.Models/
├── Errors/                                  # 新增目录
│   ├── ErrorCode.cs                        # 新增: 统一错误码枚举
│   ├── ErrorCodeExtensions.cs              # 新增: 错误码扩展方法
│   └── ProblemDetailsResponse.cs           # 新增: Client端Problem Details模型
└── Exceptions/
    ├── AppException.cs                     # 修改: 添加ErrorCode属性
    ├── ConflictException.cs                # 新增: 并发冲突异常
    └── UnauthorizedException.cs            # 新增: 权限不足异常
```

### Client端

```
LYBT.Desktop.Infrastructure/
├── Logging/
│   ├── DesktopSerilogConfiguration.cs      # 新增: 客户端Serilog配置
│   ├── CorrelationIdContext.cs             # 新增: 客户端CorrelationId上下文
│   └── CorrelationIdDelegatingHandler.cs   # 新增: HTTP请求CorrelationId注入
├── ErrorHandling/                           # 新增目录
│   ├── ApiErrorParser.cs                   # 新增: API错误响应解析
│   └── ConfigurableErrorMessageMapper.cs   # 新增: 可配置错误消息映射
└── Services/
    └── ErrorHandling/
        └── ErrorHandlingService.cs         # 修改: 增强日志记录

LYBT.Desktop.Foundation/
└── Exceptions/
    ├── StandardExceptionHandler.cs         # 修改: 支持Problem Details
    └── ExceptionMessageMapper.cs           # 标记Obsolete,迁移到ConfigurableErrorMessageMapper
```

## 修改组件

1. **Program.cs (Server)**: 重构为两阶段初始化 + Problem Details配置
2. **appsettings.json**: 增强Serilog配置节 + 错误消息配置
3. **AppException.cs**: 添加ErrorCode属性
4. **ValidationException.cs**: 适配新的错误码体系
5. **ErrorHandlingServiceExtensions.cs (Client)**: 替换Debug provider为Serilog

---

# Part 4: Configuration Design

### Server端 appsettings.json

```json
{
  "Serilog": {
    "Using": ["Serilog.Sinks.Console", "Serilog.Sinks.File"],
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "theme": "Serilog.Sinks.SystemConsole.Themes.AnsiConsoleTheme::Code, Serilog.Sinks.Console",
          "outputTemplate": "{Timestamp:HH:mm:ss.fff} [{Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/lybt-api-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] [{SourceContext}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ],
    "Enrich": ["FromLogContext", "WithMachineName", "WithThreadId"],
    "Properties": {
      "Application": "LYBT.WebAPI"
    }
  }
}
```

### Client端配置

```json
{
  "Logging": {
    "LogDirectory": "%LOCALAPPDATA%/LYBTZYZS/logs",
    "RetainedDays": 30,
    "MinimumLevel": "Information"
  },
  "ErrorMessages": {
    "TypeMessages": {
      "HttpRequestException": "网络连接失败,请检查网络设置",
      "TaskCanceledException": "操作已取消"
    },
    "CodeMessages": {
      "ERR_40003": "数据已被其他用户修改,请刷新后重试"
    }
  }
}
```

---

# Part 5: NuGet Dependencies

### Server端
**已有**:
- Serilog.AspNetCore (8.0.0)
- Serilog.Sinks.File (5.0.0)
- Serilog.Sinks.Console (5.0.1)
- Serilog.Enrichers.Environment (2.3.0)
- Serilog.Enrichers.Thread (3.1.0)

**需新增**:
- Serilog.Sinks.MSSqlServer (6.6.0) - 数据库日志Sink

### Client端 (需新增)
- Serilog (4.0.2) - 与Server端版本一致
- Serilog.Sinks.File (5.0.0)
- Serilog.Extensions.Logging (8.0.0) - 集成Microsoft.Extensions.Logging
- Serilog.Enrichers.Environment (2.3.0)
- Serilog.Enrichers.Thread (3.1.0)

---

# Part 6: Migration Strategy

### Phase 1: Server端日志重构 (低风险)
1. 实现两阶段初始化
2. 添加CorrelationId中间件
3. 整合敏感数据脱敏

### Phase 2: Server端错误处理重构 (中风险)
1. 添加ErrorCode枚举
2. 实现IExceptionHandler处理器链
3. 配置Problem Details服务
4. 扩展异常类层次结构

### Phase 3: Client端集成 (中风险)
1. 添加Serilog NuGet包
2. 配置日志输出
3. 实现CorrelationId传递
4. 重构StandardExceptionHandler
5. 实现Problem Details解析

### Phase 4: 统一与优化
1. 验证端到端追踪
2. 验证错误处理流程
3. 调整日志级别
4. 性能测试

---

# Part 7: Testing Strategy

1. **单元测试**:
   - SensitiveDataMasker测试
   - CorrelationIdMiddleware测试
   - BusinessExceptionHandler测试
   - ErrorCodeExtensions测试
   - ApiErrorParser测试

2. **集成测试**:
   - 端到端CorrelationId追踪
   - Problem Details响应格式验证
   - 异常处理流程验证

3. **手动测试**:
   - 日志文件输出格式和Rolling策略
   - Client端错误提示消息
