# LYBT.Shared.ExceptionHandling 代码知识

统一异常处理体系: 异常类型层次、Server/Desktop 端异常处理器、错误消息映射、RFC 7807 ProblemDetails 支持。

## 代码文件结构

```
Exceptions/
├── Base/
│   └── AppException.cs              # 应用程序基础异常类 (所有业务异常的根)
├── Business/
│   ├── BusinessException.cs         # 业务规则异常 (HTTP 400)
│   ├── ValidationException.cs       # 数据验证异常 (HTTP 400)
│   ├── NotFoundException.cs         # 资源未找到异常 (HTTP 404)
│   └── ConflictException.cs         # 资源冲突异常 (HTTP 409)
├── External/
│   └── ApiException.cs              # 外部 API 调用异常 (HTTP 状态码动态)
├── Security/
│   └── UnauthorizedException.cs     # 未授权异常 (HTTP 401)
└── Factory/
    └── ExceptionFactory.cs          # 异常工厂 (按领域分组的便捷创建方法)

Handlers/
├── Server/
│   ├── BusinessExceptionHandler.cs  # 服务端业务异常处理器 (IExceptionHandler)
│   └── SystemExceptionHandler.cs    # 服务端系统异常兜底处理器 (IExceptionHandler)
├── Desktop/
│   ├── ExceptionSeverity.cs         # 异常严重程度枚举
│   ├── IDesktopExceptionHandler.cs  # Desktop 端异常处理器接口
│   └── DesktopExceptionHandler.cs   # Desktop 端异常处理器实现

Mappers/
├── ExceptionMessageMapper.cs        # 异常类型到中文消息映射 (静态)
├── ClientErrorMessageMapper.cs      # 客户端错误消息映射 (静态，含 HTTP/ErrorCode/ProblemDetails)
├── IErrorMessageMapper.cs           # 错误消息映射接口 (基于 ErrorCode 枚举)
├── ConfigurableErrorMessageMapper.cs# 可配置错误消息映射 (从 IConfiguration 读取)
└── ExceptionSeverityMapper.cs       # 异常严重度到通知类型映射 [SUSPECT]

ProblemDetails/
├── ProblemTypeUris.cs               # RFC 7807 问题类型 URI 常量
├── ProblemDetailsFactory.cs         # ProblemDetails 创建工厂 [SUSPECT]
├── ProblemDetailsExtensions.cs      # ProblemDetails 扩展方法 [SUSPECT]
└── ClientProblemDetails.cs          # 客户端 ProblemDetails 响应模型 [SUSPECT]

Extensions/
├── ServiceCollectionExtensions.cs   # DI 注册扩展 (Server/Desktop/Shared 三种模式)
└── ApplicationBuilderExtensions.cs  # ASP.NET Core 中间件配置 [DEAD]
```

### Exceptions/Base/AppException.cs
**AppException** : Exception | 应用程序基础异常，所有业务异常的根类

属性: ErrorCode (字符串格式), TypedErrorCode (ErrorCode 枚举), UserMessage (用户友好消息), ShowDetailToUser。支持字符串错误码和类型化错误码两种构造方式。

| 方法 | 说明 |
|------|------|
| GetHttpStatusCode() | 获取 HTTP 状态码，基于 TypedErrorCode 映射，默认 500 |
| Category (属性) | 获取错误类别 (ErrorCategory 枚举) |

### Exceptions/Business/BusinessException.cs
**BusinessException** : AppException | 业务规则违反异常，HTTP 400

属性: BusinessRule (违反的业务规则描述)。Category 固定为 ErrorCategory.Business。

### Exceptions/Business/ValidationException.cs
**ValidationException** : AppException | 数据验证失败异常，HTTP 400

属性: Errors (Dictionary\<string, string[]\> 字段错误集合), FieldName (单字段验证), HasErrors (计算属性)。TypedErrorCode 固定为 EC.ValidationFailed。

| 方法 | 说明 |
|------|------|
| AddError(fieldName, errorMessage) | 链式添加验证错误，返回 this |

### Exceptions/Business/NotFoundException.cs
**NotFoundException** : AppException | 资源未找到异常，HTTP 404

属性: ResourceType, ResourceId。提供 User/Patient/Herb/Prescription/MedicalCase/Formula 静态工厂方法。

### Exceptions/Business/ConflictException.cs
**ConflictException** : AppException | 资源冲突异常，HTTP 409

属性: ResourceType (别名 EntityType), ResourceId (别名 EntityId), CurrentVersion, ExpectedVersion。

| 方法 | 说明 |
|------|------|
| MedicalCaseVersion(caseId, expected, current) | 静态工厂: 医案版本冲突 |
| MedicalCaseLocked(caseId, lockedBy) | 静态工厂: 医案被锁定 |
| Duplicate(resourceType, fieldName, value) | 静态工厂: 数据重复冲突 |

### Exceptions/External/ApiException.cs
**ApiException** : AppException | 外部 API 调用异常，HTTP 状态码动态

属性: StatusCode (HttpStatusCode), ResponseContent, RequestUrl, HttpMethod。

| 方法 | 说明 |
|------|------|
| Unauthorized(message) | 静态工厂: 401 |
| Forbidden(message) | 静态工厂: 403 |
| ServiceUnavailable(message) | 静态工厂: 503 |
| Timeout(message) | 静态工厂: 408 |

### Exceptions/Security/UnauthorizedException.cs
**UnauthorizedException** : AppException | 未授权异常，HTTP 401

属性: AuthenticationScheme, FailureReason。

| 方法 | 说明 |
|------|------|
| InvalidPassword() | 静态工厂: 密码错误 |
| CredentialsExpired() | 静态工厂: 凭据过期 |
| InvalidRefreshToken() | 静态工厂: 刷新令牌无效 |
| UserDisabled() | 静态工厂: 用户被禁用 |
| UserLocked() | 静态工厂: 用户被锁定 |
| PasswordChangeRequired() | 静态工厂: 首次登录需改密码 |
| DeviceMismatch() | 静态工厂: 设备指纹不匹配 |
| SessionExpired() | 静态工厂: 会话过期 |

### Exceptions/Factory/ExceptionFactory.cs
**ExceptionFactory** (static class) | 按领域分组的异常创建入口

包含 User, Patient, Herb, Prescription, MedicalCase, Formula 六个静态内部类，每个类提供 NotFound/Duplicate/InUse 等便捷工厂方法。内部委托到对应的异常类静态工厂。

### Handlers/Server/BusinessExceptionHandler.cs
**BusinessExceptionHandler** : IExceptionHandler | 服务端业务异常处理器

仅处理 AppException 及其子类，记录 Warning 级别日志，返回 ApiResponse JSON (含 errorCode/correlationId/traceId)。

| 方法 | 说明 |
|------|------|
| TryHandleAsync(httpContext, exception, ct) | 处理 AppException，非 AppException 返回 false 交给下一个处理器 |

### Handlers/Server/SystemExceptionHandler.cs
**SystemExceptionHandler** : IExceptionHandler | 服务端系统异常兜底处理器

处理所有未被 BusinessExceptionHandler 捕获的异常，始终返回 true。按异常类型映射 HTTP 状态码 (FluentValidation -> 400, UnauthorizedAccess -> 403, OperationCanceled -> 499, Timeout -> 504, DbUpdateConcurrency -> 409 等)。开发环境返回详细信息 (stackTrace)。

| 方法 | 说明 |
|------|------|
| TryHandleAsync(httpContext, exception, ct) | 兜底处理所有异常，返回 ApiResponse JSON |

### Handlers/Desktop/ExceptionSeverity.cs
**ExceptionSeverity** (enum) | 异常严重程度: Information(0)/Warning(1)/Error(2)/Critical(3)

### Handlers/Desktop/IDesktopExceptionHandler.cs
**IDesktopExceptionHandler** | Desktop 端统一异常处理接口

| 方法 | 说明 |
|------|------|
| HandleException(exception, context) | 同步处理异常 |
| HandleExceptionAsync(exception, context) | 异步处理异常 |
| LogException(exception, severity) | 按严重级别记录异常 |
| GetUserFriendlyMessage(exception) | 获取用户友好消息 |
| CanRetry(exception) | 判断异常是否可重试 |
| RegisterGlobalExceptionHandlers() | 注册 AppDomain.UnhandledException 和 TaskScheduler.UnobservedTaskException |
| UnregisterGlobalExceptionHandlers() | 注销全局异常处理器 |
| HandleException\<T\>(exception, methodName, context) | 处理异常返回 ServiceResult\<T\> |
| HandleExceptionWithResult(exception, methodName, context) | 处理异常返回 ServiceResult |
| SafeExecuteAsync\<T\>(operation, methodName, context) | 安全执行操作，自动捕获异常 |
| SafeExecuteAsync(operation, methodName, context) | 安全执行无返回值操作 |

### Handlers/Desktop/DesktopExceptionHandler.cs
**DesktopExceptionHandler** : IDesktopExceptionHandler | Desktop 端异常处理器实现

实现全部接口方法。CanRetry 对 Timeout/HttpRequest/TaskCanceled/Socket 异常返回 true。日志级别由 DetermineLogLevel 根据异常类型动态决定 (OutOfMemory -> Error, ArgumentNull -> Warning, HttpRequest -> Information 等)。

### Mappers/ExceptionMessageMapper.cs
**ExceptionMessageMapper** (static class) | 异常类型到中文用户消息的静态映射

| 方法 | 说明 |
|------|------|
| GetUserFriendlyMessage(exception) | 按异常类型返回中文消息，支持 HttpRequestException 状态码解析和 AggregateException 展开 |
| GetHttpStatusMessage(statusCode) | HTTP 状态码到中文消息映射 |

### Mappers/ClientErrorMessageMapper.cs
**ClientErrorMessageMapper** (static class) | 客户端综合错误消息映射器，全项目最大的 Mapper

| 方法 | 说明 |
|------|------|
| GetUserMessageFromStatusCode(statusCode) | HTTP 状态码到中文消息 |
| GetUserMessageFromErrorCode(errorCode) | ErrorCode 数值/字符串到中文消息 (内含完整的错误码映射表 0-7xxxx) |
| GetUserFriendlyMessage(exception) | 从异常获取用户消息，支持 AppException/HttpRequest/Refit.ApiException(反射) |
| GetUserMessageFromProblemDetails(problemDetails) | 从 ClientProblemDetails 提取用户消息 |
| GetSafeOperationFailureMessage(operationName, exception) | 带操作名的安全错误消息 |
| GetSafeMessageWithTrackingCode(operationName, exception, include) | 带追踪码的错误消息 |
| GetMessageWithTrackingCode(message, include) | 带追踪码的通用消息 |
| GetShortTrackingCode() | 获取 8 位短追踪码 |
| GetFullTrackingCode() | 获取完整追踪码 |
| TraceIdProvider (属性) | 静态委托，客户端启动时配置追踪 ID 提供器 |

### Mappers/IErrorMessageMapper.cs
**IErrorMessageMapper** | 基于 ErrorCode 枚举的错误消息映射接口

| 方法 | 说明 |
|------|------|
| GetUserMessage(errorCode) | 获取用户友好消息 |
| GetTechnicalMessage(errorCode) | 获取技术消息 (日志用) |
| GetUserMessage(errorCode, args) | 支持参数格式化的用户消息 |

### Mappers/ConfigurableErrorMessageMapper.cs
**ConfigurableErrorMessageMapper** : IErrorMessageMapper | 可配置错误消息映射器

从 IConfiguration 的 `Lybt:ErrorMessages:{ErrorCode}:{MessageType}` 路径读取消息，未配置则回退到 ErrorMessages 静态默认值。

### Mappers/ExceptionSeverityMapper.cs
**ExceptionSeverityMapper** (static class) | 异常严重度到通知类型映射

**ExceptionNotificationMapping** (record) | 映射结果: NotificationType(string)/RequiresDialog(bool)/RequiresDetailedLog(bool)

| 方法 | 说明 |
|------|------|
| ToNotificationMapping(severity) | 映射: Info->Toast, Warning->Toast, Error->Dialog, Critical->Dialog+详细日志 |
| ToNotificationType(severity) | 获取通知类型字符串 |
| RequiresDialog(severity) | 判断是否需要弹窗 |

### ProblemDetails/ProblemTypeUris.cs
**ProblemTypeUris** (static class) | RFC 7807 问题类型 URI 常量

包含 BadRequest/Unauthorized/Forbidden/NotFound/Conflict 等标准 URI 常量。

| 方法 | 说明 |
|------|------|
| GetByStatusCode(statusCode) | 根据 HTTP 状态码返回对应的 RFC URI |

### ProblemDetails/ProblemDetailsFactory.cs
**ProblemDetailsFactory** (static class) | ProblemDetails 创建工厂

| 方法 | 说明 |
|------|------|
| Create(AppException, instance, correlationId, traceId) | 从 AppException 创建 ProblemDetails，ValidationException/ConflictException 有特殊处理 |
| Create(ErrorCode, instance, correlationId, traceId, detail) | 从 ErrorCode 枚举创建 ProblemDetails |
| CreateValidationProblem(errors, instance, correlationId, traceId) | 创建验证错误的 ProblemDetails |

### ProblemDetails/ProblemDetailsExtensions.cs
**ProblemDetailsExtensions** (static class) | ProblemDetails 扩展方法

| 方法 | 说明 |
|------|------|
| GetCorrelationId(HttpContext) | 从请求头 X-Correlation-Id 获取，回退到 TraceIdentifier |
| ToProblemDetails(AppException, HttpContext) | AppException 转 ProblemDetails |
| ToProblemDetails(ErrorCode, HttpContext, detail) | ErrorCode 转 ProblemDetails |
| GetErrorCode(ProblemDetails) | 从 Extensions 中提取 errorCode |
| GetCorrelationId(ProblemDetails) | 从 Extensions 中提取 correlationId |
| GetValidationErrors(ProblemDetails) | 从 Extensions 中提取 errors 字典 |

### ProblemDetails/ClientProblemDetails.cs
**ClientProblemDetails** | 客户端 ProblemDetails 响应模型，用于反序列化服务端 RFC 7807 响应

属性: Status, Title, Detail, Type, Instance, ErrorCode, CorrelationId, TraceId, Timestamp, Errors, ResourceType, ResourceId, BusinessRule。便捷判断属性: IsValidationError/IsNotFoundError/IsUnauthorizedError/IsForbiddenError/IsConcurrencyError/IsServerError。

| 方法 | 说明 |
|------|------|
| GetUserMessage() | 获取用户消息，优先 Detail 回退 Title |
| GetValidationErrorMessage() | 格式化验证错误为多行字符串 |

### Extensions/ServiceCollectionExtensions.cs
**ServiceCollectionExtensions** (static class) | DI 注册扩展

| 方法 | 说明 |
|------|------|
| AddServerExceptionHandling() | 注册 BusinessExceptionHandler + SystemExceptionHandler 异常处理链 + IErrorMessageMapper |
| AddDesktopExceptionHandling() | 注册 IDesktopExceptionHandler + IErrorMessageMapper |
| AddSharedExceptionHandling() | 仅注册 IErrorMessageMapper (无处理器) |

### Extensions/ApplicationBuilderExtensions.cs
**ApplicationBuilderExtensions** (static class) | ASP.NET Core 中间件配置

| 方法 | 说明 |
|------|------|
| UseExceptionHandlingMiddleware(app) | 配置 app.UseExceptionHandler，启用 IExceptionHandler 链 |

## 死代码与废弃标记

| 类型/方法 | 状态 | 替代方案 | 清理计划 |
|-----------|------|----------|----------|
| ApplicationBuilderExtensions.UseExceptionHandlingMiddleware | [DEAD] | 无外部调用，WebAPI 可能直接调用 UseExceptionHandler | 确认 WebAPI Program.cs 是否使用，若未使用则清理 |
| AddServerExceptionHandling | [DEAD] | 仅在 ServiceCollectionExtensions 定义，无外部调用 | 确认 WebAPI 是否通过其他方式注册异常处理器 |
| AddDesktopExceptionHandling | [DEAD] | 仅在 ServiceCollectionExtensions 定义，无外部调用 | 确认 Shell 是否通过其他方式注册 |
| AddSharedExceptionHandling | [DEAD] | 仅在 ServiceCollectionExtensions 定义，无外部调用 | 同上 |
| ExceptionSeverityMapper + ExceptionNotificationMapping | [SUSPECT] | 仅在自身文件内使用，无外部调用方 | 确认 Desktop 端是否通过反射或间接方式使用 |
| ProblemDetailsExtensions.ToProblemDetails | [SUSPECT] | 仅在自身文件定义，无外部调用方 | BusinessExceptionHandler 直接使用 ProblemDetailsFactory 而非此扩展方法 |
| ClientProblemDetails | [SUSPECT] | 仅被 ClientErrorMessageMapper 引用 | 确认客户端 HTTP 层是否使用此类反序列化服务端响应 |
| ExceptionMessageMapper | [SUSPECT] | 仅被 DesktopExceptionHandler 内部调用，与 ClientErrorMessageMapper 功能重叠 | 考虑合并或明确分工 |

## 设计分析

| 文件/目录 | 问题 | 分析 | 建议 |
|-----------|------|------|------|
| ExceptionMessageMapper vs ClientErrorMessageMapper | 功能重叠 | 两个静态类都做异常到中文消息映射，ExceptionMessageMapper 更简单 (类型映射)，ClientErrorMessageMapper 更全面 (HTTP/ErrorCode/ProblemDetails/追踪码) | 保留 ClientErrorMessageMapper 作为客户端主映射器，ExceptionMessageMapper 作为 Desktop 简单场景的轻量替代 |
| ClientErrorMessageMapper | 文件过大 (638 行) | 包含 HTTP 映射、ErrorCode 映射表 (274 条)、异常消息映射、ProblemDetails 解析、追踪码等多个关注点 | 考虑拆分: ErrorCodeMessageTable (纯映射表) + ClientErrorMessageMapper (逻辑) + TrackingCodeService (追踪码) |
| ConflictException | EntityType/EntityId 属性别名设计 | ResourceType/EntityType 互为别名 (get/set 指向同一字段)，为向后兼容设计 | 可接受，但应在未来统一为 ResourceType |
| ProblemDetailsFactory vs BusinessExceptionHandler | 响应格式不一致 | ProblemDetailsFactory 生成 RFC 7807 ProblemDetails，BusinessExceptionHandler 生成 ApiResponse | Server 实际使用 ApiResponse 格式，ProblemDetails 体系可能未被使用 |
| AddServerExceptionHandling 等注册方法 | 未被调用 | 三个 DI 注册方法均无外部调用方 | 确认是否为预留接口或已被替代 |

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| ClientErrorMessageMapper 通过反射处理 Refit.ApiException | 为避免 Shared 层直接依赖 Refit 包，使用类型名匹配 + 反射获取 StatusCode/Content | 反射可能因 Refit 版本升级而失效，需在 Refit 升级后验证 |
| ExceptionSeverityMapper 使用字符串而非枚举引用 NotificationType | 避免 Shared 层依赖 Desktop.Infrastructure 的 NotificationType 枚举 | Desktop 调用方需手动将字符串 "Info"/"Warning"/"Error" 转换为 NotificationType 枚举 |
| BusinessExceptionHandler 返回 ApiResponse 而非 ProblemDetails | 项目选择了自定义 ApiResponse 格式而非标准 RFC 7807 | ProblemDetails 相关代码 (Factory/Extensions) 在 Server 端可能未被实际使用，客户端 ClientProblemDetails 需要确认是否作为 fallback 解析 |
| SystemExceptionHandler 在开发环境暴露 StackTrace | 通过 IHostEnvironment.IsDevelopment() 判断 | 确保生产环境 ASPNETCORE_ENVIRONMENT 不设为 Development |
| ValidationException.AddError 返回 this 实现链式调用 | 设计上允许 `new ValidationException().AddError(...).AddError(...)` | 注意 AddError 修改自身状态 (可变操作)，与项目不可变原则有冲突但异常构造是合理例外 |
