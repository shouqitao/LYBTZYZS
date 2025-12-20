# Design: consolidate-exception-handling

## 1. 架构设计

### 1.1 分层架构 (解决循环引用)

为避免循环引用问题，引入 **Primitives 层** 作为最底层基础类型：

```
┌─────────────────────────────────────────────────────────────────┐
│                    应用层 (Desktop/WebAPI)                       │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│              LYBT.Shared.ExceptionHandling                       │
│  (异常处理器、异常类型、ProblemDetails、消息映射)                   │
│  依赖: Models, Primitives                                        │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                   LYBT.Shared.Models                             │
│  (Result<T>, ServiceResult<T>, API契约模型)                       │
│  依赖: Primitives                                                 │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                 LYBT.Shared.Primitives                           │
│  (ErrorCode, ErrorCategory, ErrorMessages - 最底层，零依赖)        │
│  依赖: 无                                                         │
└─────────────────────────────────────────────────────────────────┘
```

**设计原则：**
- ErrorCode 是错误处理的核心概念，属于 Primitives 层
- Result<T> 可以包含 ErrorCode，因为 Models 引用 Primitives
- ExceptionHandling 可以使用 ServiceResult，因为它引用 Models
- 无循环依赖

### 1.2 项目位置

```
src/
├── Shared/
│   ├── LYBT.Shared.Primitives/          # 新项目 (最底层基础类型)
│   ├── LYBT.Shared.Models/              # 现有 (引用 Primitives)
│   ├── LYBT.Shared.ExceptionHandling/   # 现有 (引用 Models + Primitives)
│   └── LYBT.Shared.Contracts/           # 现有
├── Server/
│   ├── Core/
│   │   └── LYBT.Infrastructure/         # 现有 (引用 ExceptionHandling)
│   └── Services/
│       └── LYBT.WebAPI/                 # 现有 (引用 ExceptionHandling)
└── Client/
    └── Desktop/
        └── Core/
            ├── LYBT.Desktop.Foundation/ # 现有 (引用 ExceptionHandling)
            └── LYBT.Desktop.Models/     # 现有 (引用 ExceptionHandling)
```

### 1.3 项目结构详细设计

#### LYBT.Shared.Primitives (最底层)

```
LYBT.Shared.Primitives/
├── LYBT.Shared.Primitives.csproj        # 零依赖
│
└── ErrorCodes/                           # 错误码体系
    ├── ErrorCode.cs                     # 错误码枚举 (5位数分区)
    ├── ErrorCategory.cs                 # 错误分类 + 严重程度
    ├── ErrorCodeExtensions.cs           # 扩展方法 (HTTP映射等)
    └── ErrorMessages.cs                 # 错误消息映射 (中/英)
```

#### LYBT.Shared.ExceptionHandling

```
LYBT.Shared.ExceptionHandling/
├── LYBT.Shared.ExceptionHandling.csproj
│
├── Exceptions/                           # 异常类定义
│   ├── Base/
│   │   └── AppException.cs              # 基类异常
│   ├── Business/
│   │   ├── ValidationException.cs       # 验证异常 (400)
│   │   ├── NotFoundException.cs         # 资源未找到 (404)
│   │   ├── ConflictException.cs         # 资源冲突 (409)
│   │   └── BusinessException.cs         # 业务规则异常 (400)
│   ├── Security/
│   │   └── UnauthorizedException.cs     # 未授权 (401)
│   ├── External/
│   │   └── ApiException.cs              # 外部API调用异常
│   └── Factory/
│       └── ExceptionFactory.cs          # 异常工厂
│
├── ErrorCodes/                           # 错误码体系
│   ├── ErrorCode.cs                     # 错误码枚举 (5位数)
│   ├── ErrorCodeExtensions.cs           # 扩展方法
│   ├── ErrorCategory.cs                 # 错误分类
│   └── ErrorMessages.cs                 # 错误消息映射 (中/英)
│
├── Handlers/                             # 异常处理器
│   ├── Abstractions/
│   │   └── IAppExceptionHandler.cs      # 统一处理器接口
│   ├── Server/
│   │   ├── BusinessExceptionHandler.cs  # 业务异常处理 (Server)
│   │   └── SystemExceptionHandler.cs    # 系统异常处理 (Server)
│   └── Desktop/
│       ├── DesktopExceptionHandler.cs   # 桌面端处理器
│       └── ExceptionSeverity.cs         # 异常严重程度
│
├── ProblemDetails/                       # RFC 7807 支持
│   ├── ProblemDetailsFactory.cs         # 统一创建工厂
│   └── ProblemDetailsExtensions.cs      # 扩展方法
│
├── Mappers/                              # 消息映射
│   ├── IErrorMessageMapper.cs           # 映射接口
│   ├── ConfigurableErrorMessageMapper.cs # 可配置映射
│   └── ExceptionMessageMapper.cs        # 异常消息映射
│
└── Extensions/                           # DI扩展
    └── ServiceCollectionExtensions.cs   # 服务注册扩展
```

---

## 2. 核心类设计

### 2.1 AppException基类

```csharp
namespace LYBT.Shared.ExceptionHandling.Exceptions.Base;

/// <summary>
/// 应用程序异常基类 - 所有业务异常的基类
/// 符合RFC 7807 Problem Details规范
/// </summary>
public class AppException : Exception
{
    /// <summary>
    /// 错误码字符串 (向后兼容)
    /// </summary>
    public string? ErrorCode { get; }

    /// <summary>
    /// 类型化错误码 (推荐使用)
    /// </summary>
    public ErrorCode? TypedErrorCode { get; }

    /// <summary>
    /// 错误分类
    /// </summary>
    public ErrorCategory Category => TypedErrorCode?.GetCategory() ?? ErrorCategory.Unknown;

    /// <summary>
    /// 用户友好的错误消息
    /// </summary>
    public string? UserMessage { get; }

    /// <summary>
    /// 是否向用户展示详细信息
    /// </summary>
    public bool ShowDetailToUser { get; }

    /// <summary>
    /// 关联数据 (用于日志和调试)
    /// </summary>
    public IDictionary<string, object?> Data { get; } = new Dictionary<string, object?>();

    // 构造函数...

    /// <summary>
    /// 获取HTTP状态码
    /// </summary>
    public virtual int GetHttpStatusCode() => 500;
}
```

### 2.2 ErrorCode枚举

```csharp
namespace LYBT.Shared.ExceptionHandling.ErrorCodes;

/// <summary>
/// 错误码枚举 - 5位数分层设计
/// 规则: MMXXX (MM=模块代码 00-99, XXX=错误代码 000-999)
/// </summary>
public enum ErrorCode
{
    #region 00xxx - 通用错误
    Unknown = 0,
    InvalidRequest = 1,
    NotFound = 2,
    ValidationFailed = 3,
    Unauthorized = 4,
    Forbidden = 5,
    ConcurrencyConflict = 6,
    Timeout = 7,
    ServiceUnavailable = 8,
    InternalError = 9,
    DatabaseError = 10,
    ConfigurationError = 11,
    RateLimitExceeded = 12,
    #endregion

    #region 1xxxx - 用户模块 (Users)
    UserNotFound = 10001,
    UserNameExists = 10002,
    // ... 其他用户错误码
    #endregion

    // ... 其他模块
}
```

### 2.3 ErrorMessages映射

```csharp
namespace LYBT.Shared.ExceptionHandling.ErrorCodes;

/// <summary>
/// 错误消息映射 - 支持中英文
/// </summary>
public static class ErrorMessages
{
    private static readonly Dictionary<ErrorCode, (string Zh, string En)> _messages = new()
    {
        // 通用错误
        [ErrorCode.Unknown] = ("未知错误", "Unknown error"),
        [ErrorCode.InvalidRequest] = ("请求参数无效", "Invalid request parameters"),
        [ErrorCode.NotFound] = ("资源未找到", "Resource not found"),
        [ErrorCode.ValidationFailed] = ("验证失败", "Validation failed"),
        [ErrorCode.Unauthorized] = ("未授权访问", "Unauthorized access"),
        [ErrorCode.Forbidden] = ("禁止访问", "Access forbidden"),
        [ErrorCode.ConcurrencyConflict] = ("并发冲突", "Concurrency conflict"),
        [ErrorCode.Timeout] = ("操作超时", "Operation timed out"),
        [ErrorCode.ServiceUnavailable] = ("服务不可用", "Service unavailable"),
        [ErrorCode.InternalError] = ("内部服务器错误", "Internal server error"),
        [ErrorCode.DatabaseError] = ("数据库操作失败", "Database operation failed"),
        [ErrorCode.ConfigurationError] = ("配置错误", "Configuration error"),
        [ErrorCode.RateLimitExceeded] = ("请求频率过高", "Rate limit exceeded"),

        // 用户模块
        [ErrorCode.UserNotFound] = ("用户不存在", "User not found"),
        [ErrorCode.UserNameExists] = ("用户名已存在", "Username already exists"),
        // ... 更多映射
    };

    /// <summary>
    /// 获取错误消息
    /// </summary>
    public static string Get(ErrorCode code, bool english = false)
    {
        if (_messages.TryGetValue(code, out var msg))
            return english ? msg.En : msg.Zh;
        return code.ToString();
    }

    /// <summary>
    /// 获取带格式化参数的错误消息
    /// </summary>
    public static string GetFormatted(ErrorCode code, bool english = false, params object[] args)
    {
        var template = Get(code, english);
        return args.Length > 0 ? string.Format(template, args) : template;
    }
}
```

### 2.4 ProblemDetailsFactory

```csharp
namespace LYBT.Shared.ExceptionHandling.ProblemDetails;

using Microsoft.AspNetCore.Mvc;

/// <summary>
/// ProblemDetails工厂 - 统一创建RFC 7807响应
/// </summary>
public static class ProblemDetailsFactory
{
    /// <summary>
    /// 从AppException创建ProblemDetails
    /// </summary>
    public static ProblemDetails Create(
        AppException exception,
        string instance,
        string correlationId,
        string traceId)
    {
        var details = new ProblemDetails
        {
            Status = exception.GetHttpStatusCode(),
            Title = GetTitle(exception),
            Detail = exception.UserMessage ?? exception.Message,
            Instance = instance,
            Type = GetTypeUri(exception.GetHttpStatusCode())
        };

        // 标准扩展属性
        details.Extensions["correlationId"] = correlationId;
        details.Extensions["traceId"] = traceId;
        details.Extensions["timestamp"] = DateTimeOffset.UtcNow;

        // 错误码扩展
        if (!string.IsNullOrEmpty(exception.ErrorCode))
            details.Extensions["errorCode"] = exception.ErrorCode;

        if (exception.TypedErrorCode.HasValue)
        {
            details.Extensions["errorCodeInt"] = (int)exception.TypedErrorCode.Value;
            details.Extensions["errorCategory"] = exception.Category.ToString();
        }

        // 异常特定扩展
        AddExceptionSpecificExtensions(details, exception);

        return details;
    }

    private static string GetTitle(AppException exception) => exception switch
    {
        ValidationException => "验证失败",
        NotFoundException => "资源未找到",
        UnauthorizedException => "未授权",
        ConflictException => "资源冲突",
        BusinessException => "业务错误",
        ApiException => "API调用异常",
        _ => "应用程序异常"
    };

    private static string GetTypeUri(int statusCode) => statusCode switch
    {
        400 => "https://tools.ietf.org/html/rfc7231#section-6.5.1",
        401 => "https://tools.ietf.org/html/rfc7235#section-3.1",
        403 => "https://tools.ietf.org/html/rfc7231#section-6.5.3",
        404 => "https://tools.ietf.org/html/rfc7231#section-6.5.4",
        409 => "https://tools.ietf.org/html/rfc7231#section-6.5.8",
        422 => "https://tools.ietf.org/html/rfc4918#section-11.2",
        500 => "https://tools.ietf.org/html/rfc7231#section-6.6.1",
        _ => $"https://httpstatuses.com/{statusCode}"
    };

    private static void AddExceptionSpecificExtensions(
        ProblemDetails details,
        AppException exception)
    {
        switch (exception)
        {
            case ValidationException ve when ve.HasErrors:
                details.Extensions["errors"] = ve.Errors;
                if (!string.IsNullOrEmpty(ve.FieldName))
                    details.Extensions["fieldName"] = ve.FieldName;
                break;

            case NotFoundException nfe:
                if (!string.IsNullOrEmpty(nfe.ResourceType))
                    details.Extensions["resourceType"] = nfe.ResourceType;
                if (!string.IsNullOrEmpty(nfe.ResourceId))
                    details.Extensions["resourceId"] = nfe.ResourceId;
                break;

            case ConflictException ce:
                if (!string.IsNullOrEmpty(ce.ResourceType))
                    details.Extensions["resourceType"] = ce.ResourceType;
                if (!string.IsNullOrEmpty(ce.ResourceId))
                    details.Extensions["resourceId"] = ce.ResourceId;
                break;

            case UnauthorizedException ue:
                if (!string.IsNullOrEmpty(ue.FailureReason))
                    details.Extensions["failureReason"] = ue.FailureReason;
                break;

            case BusinessException be:
                if (!string.IsNullOrEmpty(be.BusinessRule))
                    details.Extensions["businessRule"] = be.BusinessRule;
                break;
        }
    }
}
```

---

## 3. 迁移映射表

### 3.1 异常类迁移

| 源位置 | 目标位置 | 备注 |
|--------|----------|------|
| `LYBT.Shared.Models.Exceptions.AppException` | `LYBT.Shared.ExceptionHandling.Exceptions.Base.AppException` | 基类 |
| `LYBT.Shared.Models.Exceptions.ValidationException` | `LYBT.Shared.ExceptionHandling.Exceptions.Business.ValidationException` | |
| `LYBT.Shared.Models.Exceptions.NotFoundException` | `LYBT.Shared.ExceptionHandling.Exceptions.Business.NotFoundException` | |
| `LYBT.Shared.Models.Exceptions.ConflictException` | `LYBT.Shared.ExceptionHandling.Exceptions.Business.ConflictException` | |
| `LYBT.Shared.Models.Exceptions.BusinessException` | `LYBT.Shared.ExceptionHandling.Exceptions.Business.BusinessException` | |
| `LYBT.Shared.Models.Exceptions.UnauthorizedException` | `LYBT.Shared.ExceptionHandling.Exceptions.Security.UnauthorizedException` | |
| `LYBT.Shared.Models.Exceptions.ApiException` | `LYBT.Shared.ExceptionHandling.Exceptions.External.ApiException` | |
| `LYBT.Shared.Models.Exceptions.ExceptionFactory` | `LYBT.Shared.ExceptionHandling.Exceptions.Factory.ExceptionFactory` | |

### 3.2 错误码迁移

| 源位置 | 目标位置 |
|--------|----------|
| `LYBT.Shared.Models.Errors.ErrorCode` | `LYBT.Shared.ExceptionHandling.ErrorCodes.ErrorCode` |
| `LYBT.Shared.Models.Errors.ErrorCodeExtensions` | `LYBT.Shared.ExceptionHandling.ErrorCodes.ErrorCodeExtensions` |

### 3.3 处理器迁移

| 源位置 | 目标位置 |
|--------|----------|
| `LYBT.WebAPI.ExceptionHandlers.BusinessExceptionHandler` | `LYBT.Shared.ExceptionHandling.Handlers.Server.BusinessExceptionHandler` |
| `LYBT.WebAPI.ExceptionHandlers.SystemExceptionHandler` | `LYBT.Shared.ExceptionHandling.Handlers.Server.SystemExceptionHandler` |
| `LYBT.Desktop.Foundation.Exceptions.StandardExceptionHandler` | `LYBT.Shared.ExceptionHandling.Handlers.Desktop.DesktopExceptionHandler` |
| `LYBT.Desktop.Foundation.Exceptions.ExceptionMessageMapper` | `LYBT.Shared.ExceptionHandling.Mappers.ExceptionMessageMapper` |

### 3.4 其他迁移

| 源位置 | 目标位置 |
|--------|----------|
| `LYBT.Infrastructure.Errors.IErrorMessageMapper` | `LYBT.Shared.ExceptionHandling.Mappers.IErrorMessageMapper` |
| `LYBT.Infrastructure.Errors.ConfigurableErrorMessageMapper` | `LYBT.Shared.ExceptionHandling.Mappers.ConfigurableErrorMessageMapper` |

---

## 4. 迁移策略 (直接替换，无兼容层)

### 4.1 策略说明

采用**直接替换**策略，不保留旧代码:
- 创建新项目后，直接更新所有引用
- 删除旧位置的异常类、错误码、处理器
- 使用IDE的全局替换功能批量更新命名空间

### 4.2 命名空间替换规则

| 旧命名空间 | 新命名空间 |
|------------|------------|
| `LYBT.Shared.Models.Exceptions` | `LYBT.Shared.ExceptionHandling.Exceptions` |
| `LYBT.Shared.Models.Errors` | `LYBT.Shared.ExceptionHandling.ErrorCodes` |
| `LYBT.WebAPI.ExceptionHandlers` | `LYBT.Shared.ExceptionHandling.Handlers.Server` |
| `LYBT.Infrastructure.Errors` | `LYBT.Shared.ExceptionHandling.Mappers` |
| `LYBT.Desktop.Foundation.Exceptions` | `LYBT.Shared.ExceptionHandling.Handlers.Desktop` |

### 4.3 清理清单

迁移完成后删除以下文件:
- `src/Shared/LYBT.Shared.Models/Exceptions/*.cs` (8个文件)
- `src/Shared/LYBT.Shared.Models/Errors/*.cs` (2个文件)
- `src/Server/Core/LYBT.Infrastructure/Errors/*.cs` (2个文件)
- `src/Server/Services/LYBT.WebAPI/ExceptionHandlers/*.cs` (2个文件)
- `src/Client/Desktop/Core/LYBT.Desktop.Foundation/Exceptions/*.cs` (4个文件)
- `src/Client/Desktop/Core/LYBT.Desktop.Models/Exceptions/*.cs` (1个文件)

---

## 5. DI注册

### 5.1 Server端注册

```csharp
// Program.cs 或 Startup.cs
services.AddExceptionHandling(options =>
{
    options.UseBusinessExceptionHandler = true;
    options.UseSystemExceptionHandler = true;
    options.DefaultLanguage = "zh-CN";
});

// 等价于:
builder.Services.AddExceptionHandler<BusinessExceptionHandler>();
builder.Services.AddExceptionHandler<SystemExceptionHandler>();
builder.Services.AddSingleton<IErrorMessageMapper, ConfigurableErrorMessageMapper>();
builder.Services.AddProblemDetails();
```

### 5.2 Desktop端注册

```csharp
// App.xaml.cs
containerRegistry.RegisterSingleton<IExceptionHandler, DesktopExceptionHandler>();
containerRegistry.RegisterSingleton<IExceptionMessageMapper, ExceptionMessageMapper>();
```

---

## 6. 测试策略

### 6.1 单元测试

```
tests/UnitTests/Shared/LYBT.Shared.ExceptionHandling.Tests/
├── Exceptions/
│   ├── AppExceptionTests.cs
│   ├── ValidationExceptionTests.cs
│   └── ...
├── ErrorCodes/
│   ├── ErrorCodeTests.cs
│   └── ErrorMessagesTests.cs
├── Handlers/
│   ├── BusinessExceptionHandlerTests.cs
│   └── SystemExceptionHandlerTests.cs
└── ProblemDetails/
    └── ProblemDetailsFactoryTests.cs
```

### 6.2 测试覆盖目标

| 组件 | 目标覆盖率 |
|------|------------|
| 异常类 | 90% |
| 错误码/消息 | 100% |
| 处理器 | 85% |
| ProblemDetails | 95% |
| 总体 | ≥80% |
