# 异常处理与错误追踪架构

> 覆盖 DOC3-11 (异常体系架构) 和 DOC3-14 (CorrelationId 全链路追踪)

## 概述

系统采用 **"异常即信号"** 的设计哲学：Service 层通过抛出类型化异常表达业务违规，全局异常处理器链统一捕获并转换为标准化 API 响应。所有请求通过 CorrelationId 实现端到端追踪，贯穿 Server 日志、API 响应和 Desktop 客户端。

## 异常类型体系

继承链位于 `LYBT.Shared.ExceptionHandling.Exceptions` 命名空间：

```
Exception
 └── AppException                       # 应用基类 (默认 500)
      ├── BusinessException             # 业务规则违反 (400)
      ├── NotFoundException             # 资源不存在 (404)，含静态工厂方法
      ├── ValidationException           # 验证失败 (400)，含字段级错误集合
      └── ConflictException             # 并发/唯一约束冲突 (409)
```

### AppException 核心属性

| 属性 | 类型 | 说明 |
|------|------|------|
| `TypedErrorCode` | `ErrorCode?` | 类型化错误码枚举（推荐使用） |
| `ErrorCode` | `string?` | 格式化错误码字符串 (`ERR-30001`)，向后兼容 |
| `UserMessage` | `string?` | 面向用户的友好消息 |
| `GetHttpStatusCode()` | `int` | 基于 `TypedErrorCode` 映射 HTTP 状态码 |
| `Category` | `ErrorCategory` | 错误类别 (Validation/Authentication/Business 等) |

### NotFoundException 静态工厂

```csharp
throw NotFoundException.Patient(patientId);   // ErrorCode = PatientNotFound (20001)
throw NotFoundException.MedicalCase(caseId);  // ErrorCode = MedicalCaseNotFound (30001)
throw NotFoundException.Herb(herbId);         // ErrorCode = HerbNotFound (50001)
```

## 全局异常处理器链

注册于 `AddServerExceptionHandling()` (`LYBT.Shared.ExceptionHandling.Extensions.ServiceCollectionExtensions`)：

```
请求 → CorrelationIdMiddleware → ... → Controller → Service 抛异常
                                                         ↓
                                    BusinessExceptionHandler (IExceptionHandler #1)
                                      ├─ 匹配 AppException 及子类 → ApiResponse (Warning 日志)
                                      └─ 不匹配 → 传递下一个
                                                         ↓
                                    SystemExceptionHandler (IExceptionHandler #2, 兜底)
                                      └─ 处理所有其他异常 → ApiResponse (Error 日志)
                                         开发环境: 包含 StackTrace
                                         生产环境: 隐藏内部细节
```

**SystemExceptionHandler** 内置异常类型映射：

| 异常类型 | HTTP 状态码 | 说明 |
|----------|-----------|------|
| `FluentValidation.ValidationException` | 400 | FluentValidation 验证失败 |
| `UnauthorizedAccessException` | 403 | 权限不足 |
| `OperationCanceledException` | 499 | 客户端取消请求 |
| `TimeoutException` | 504 | 服务器超时 |
| `DbUpdateConcurrencyException` | 409 | EF Core 并发冲突 |
| `DbUpdateException` | 500 | 数据库保存失败 |
| `HttpRequestException` | 502 | 外部服务调用失败 |
| 其他 | 500 | 默认内部错误 |

### Desktop 端异常处理

`DesktopExceptionHandler` (`IDesktopExceptionHandler`) 提供客户端侧异常处理：
- 全局注册 `AppDomain.UnhandledException` 和 `TaskScheduler.UnobservedTaskException`
- `CanRetry()` 判定可重试异常 (Timeout/HttpRequest/Socket)
- `SafeExecuteAsync()` 包装异步操作，捕获异常返回 `ServiceResult`

## ErrorCode 统一错误码

定义于 `LYBT.Shared.Primitives.ErrorCodes.ErrorCode` 枚举，采用 **MCCEE 分区规则** (M=模块, CC=子类别, EE=序号)：

| 分区 | 模块 | 示例 |
|------|------|------|
| `0xxxx` | 通用 | `Unknown(0)`, `RateLimitExceeded(12)` |
| `1xxxx` | 用户/认证 | `UserNotFound(10001)`, `AuthInvalidCredentials(10101)` |
| `2xxxx` | 患者 | `PatientNotFound(20001)`, `PatientPhoneDuplicate(20701)` |
| `3xxxx` | 医案 | `MedicalCaseNotFound(30001)`, `McInvalidStatusTransition(30301)` |
| `4xxxx` | 处方 | `PrescriptionNotFound(40001)` |
| `5xxxx` | 药材 | `HerbNotFound(50001)`, `HerbImportFileEmpty(50301)` |
| `6xxxx` | 验方 | `FormulaNotFound(60001)`, `FormulaHerbItemNotFound(60202)` |
| `7xxxx` | 同步 | `UnsupportedEntityType(70101)`, `SyncDataConflict(70103)` |

### 扩展方法 (ErrorCodeExtensions)

- `ToFormattedString()` -- 格式化为 `"ERR-30001"`
- `ToHttpStatusCode()` -- 映射到 HTTP 状态码 (400/401/403/404/409/422/429/500/503)
- `ToCategory()` -- 映射到 `ErrorCategory` 枚举
- `GetModuleName()` -- 根据数值范围返回模块名

### ErrorMessages 错误消息

`ErrorMessages` 静态类维护中英文双语消息映射表，提供 `Get(code)` / `GetUserMessage(code)` / `GetTechnicalMessage(code)` 方法。

## API 响应格式

### 业务异常响应 (ApiResponse)

`BusinessExceptionHandler` 返回 `ApiResponse` 格式：

```json
{
  "success": false,
  "message": "该患者已有进行中的医案，请先完成现有医案",
  "data": null,
  "errors": {
    "code": "ERR-30103",
    "correlationId": "a1b2c3d4e5f6",
    "traceId": "a1b2c3d4e5f6"
  },
  "timestamp": 1740000000,
  "requestId": "a1b2c3d4e5f6"
}
```

### StatusCode 错误响应 (ProblemDetails)

非异常路径的 HTTP 错误 (如 401/404/429) 通过 `ProblemDetailsConfiguration` 返回 RFC 7807 格式：

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.4",
  "title": "资源未找到",
  "status": 404,
  "detail": "请求的资源不存在",
  "instance": "/api/patients/xxx",
  "correlationId": "a1b2c3d4e5f6",
  "traceId": "a1b2c3d4e5f6",
  "timestamp": "2026-02-26T00:00:00Z"
}
```

`ProblemDetailsFactory` 可从 `AppException` 或 `ErrorCode` 创建 ProblemDetails 实例，自动注入 `errorCode`、`correlationId`、`traceId` 扩展字段。

## CorrelationId 全链路追踪 (DOC3-14)

### 生成与传播

`CorrelationIdMiddleware` (`LYBT.WebAPI.Middleware`) 在管道最早期注册，执行顺序：

1. **读取**: 优先 `traceparent` (W3C Trace Context)，回退到 `X-Correlation-ID` 请求头
2. **生成**: 若均无则生成 12 位短格式 GUID (`Guid.NewGuid().ToString("N")[..12]`)
3. **注入**: 设置 `HttpContext.TraceIdentifier` 和 `HttpContext.Items["CorrelationId"]`
4. **响应**: 通过 `OnStarting` 回调写入 `X-Correlation-ID` 响应头
5. **日志**: 通过 `LogContext.PushProperty("CorrelationId", ...)` 注入 Serilog 上下文

### 提供者体系 (ICorrelationIdProvider)

```
ICorrelationIdProvider
 ├── HttpContextCorrelationIdProvider   # Server 端，从 HttpContext.Items 读取
 └── AsyncLocalCorrelationIdProvider    # Desktop 端，基于 AsyncLocal<string?> 传递
```

### Serilog 富集器 (CorrelationIdEnricher)

`CorrelationIdEnricher` (`LYBT.Shared.Logging.Enrichers`) 作为 `LogContext.PushProperty` 的补充机制：
- 优先使用 LogContext 中已注入的 CorrelationId (由中间件设置)
- 若 LogContext 中不存在，则从 `ICorrelationIdProvider` 获取
- 确保所有日志事件 (包括非 HTTP 上下文的后台任务) 都包含 CorrelationId

### Desktop 客户端追踪

`LoggingHttpHandler` (`LYBT.Desktop.Infrastructure.Http`) 负责客户端侧追踪：
- 使用 `System.Diagnostics.Activity` 获取追踪 ID
- 自动添加 `traceparent` 请求头 (W3C 标准)
- 记录请求/响应日志时附带 CorrelationId
- `ProblemDetailsParser` 从错误响应中提取 `correlationId` 字段用于客户端关联

### 追踪流程

```
Desktop                          Server
  │                                │
  ├─ LoggingHttpHandler            │
  │  添加 traceparent header ──────┤
  │                                ├─ CorrelationIdMiddleware
  │                                │  提取/生成 CorrelationId
  │                                │  注入 LogContext
  │                                │     │
  │                                │  Controller → Service → Repository
  │                                │  (所有日志自动携带 CorrelationId)
  │                                │     │
  │                                │  异常处理器
  │                                │  响应中包含 correlationId
  │                                ├──────────────────────────┤
  ├─ ProblemDetailsParser          │
  │  解析响应中的 correlationId    │
  │  关联本地日志                  │
  │                                │
```

## Rate Limiting 错误响应

当触发速率限制时，系统返回结构化的 `ApiResponse`，包含 `ErrorCode.RateLimitExceeded` (HTTP 429)：

```json
{
  "success": false,
  "message": "请求过于频繁，请稍后重试",
  "errors": {
    "code": "ERR-00012",
    "correlationId": "a1b2c3d4e5f6"
  }
}
```

速率限制中间件 (`UseRateLimiter()`) 在路由之后、认证之前执行，由 `ProblemDetailsConfiguration.UseStatusCodePagesWithProblemDetails()` 处理 429 状态码响应。

---

最后更新: 2026-02-26
