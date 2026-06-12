---
type: concept
title: 异常处理与错误追踪
tags: [concept, error-handling, logging, architecture]
created: 2026-06-10
updated: 2026-06-10
source: docs/03-architecture/06-error-handling.md
---

## 概述

系统采用 **"异常即信号"** 的设计哲学：Service 层通过抛出类型化异常表达业务违规，全局异常处理器链统一捕获并转换为标准化 API 响应。所有请求通过 **CorrelationId** 实现端到端追踪，贯穿 Server 日志、API 响应和 Desktop 客户端，确保问题可追溯。

## 核心内容

### 异常类型体系

详见[异常类型体系](exception-hierarchy.md)完整定义。

### NotFoundException 静态工厂

提供类型安全的快捷构造方法，避免魔法字符串：

```csharp
throw NotFoundException.Patient(patientId);   // ErrorCode = PatientNotFound (20001)
throw NotFoundException.MedicalCase(caseId);  // ErrorCode = MedicalCaseNotFound (30001)
throw NotFoundException.Herb(herbId);         // ErrorCode = HerbNotFound (50001)
```

### 全局异常处理器链

注册于 `AddServerExceptionHandling()`，按顺序执行：

```
请求 → CorrelationIdMiddleware → Controller → Service 抛异常
                                                    ↓
                               BusinessExceptionHandler (#1)
                                 ├─ 匹配 AppException 及子类
                                 ├─ 返回 ApiResponse (Warning 日志)
                                 └─ 不匹配 → 传递下一个
                                                    ↓
                               SystemExceptionHandler (#2, 兜底)
                                 ├─ 处理 FluentValidation.ValidationException → 400
                                 ├─ 处理 UnauthorizedAccessException → 403
                                 ├─ 处理 DbUpdateConcurrencyException → 409
                                 ├─ 处理 TimeoutException → 504
                                 └─ 处理其他 → 500 (生产环境隐藏 StackTrace)
```

| 异常类型 | HTTP 状态码 | 说明 |
|----------|-----------|------|
| `AppException` 及子类 | 按类型映射 | 业务异常（BusinessException=400, NotFound=404, Conflict=409） |
| `FluentValidation.ValidationException` | 400 | 模型验证失败 |
| `UnauthorizedAccessException` | 403 | 权限不足 |
| `DbUpdateConcurrencyException` | 409 | EF Core 乐观并发冲突 |
| `TimeoutException` | 504 | 服务器超时 |
| 其他 | 500 | 内部错误（生产环境隐藏细节） |

### ErrorCode 统一错误码

定义于 `LYBT.Shared.Primitives.ErrorCodes.ErrorCode` 枚举，采用 **MCCEE 分区规则**（M=模块, CC=子类别, EE=序号）：

| 分区 | 模块 | 示例 |
|------|------|------|
| `0xxxx` | 通用 | `Unknown(0)`, `RateLimitExceeded(12)` |
| `1xxxx` | 用户/认证 | `UserNotFound(10001)`, `AuthInvalidCredentials(10101)` |
| `2xxxx` | 患者 | `PatientNotFound(20001)`, `PatientPhoneDuplicate(20701)` |
| `3xxxx` | 医案 | `MedicalCaseNotFound(30001)`, `McInvalidStatusTransition(30301)` |
| `4xxxx` | 处方 | `PrescriptionNotFound(40001)` |
| `5xxxx` | 草药 | `HerbNotFound(50001)`, `HerbImportFileEmpty(50301)` |
| `6xxxx` | 配方 | `FormulaNotFound(60001)`, `FormulaHerbItemNotFound(60202)` |
| `7xxxx` | 同步 | `UnsupportedEntityType(70101)`, `SyncDataConflict(70103)` |

**扩展方法**：
- `ToFormattedString()` — 格式化为 `"ERR-30001"`
- `ToHttpStatusCode()` — 映射到 HTTP 状态码
- `ToCategory()` — 映射到 `ErrorCategory` 枚举（Validation/Authentication/Business 等）
- `GetModuleName()` — 根据数值范围返回模块名

### API 响应格式

**业务异常响应**（BusinessExceptionHandler 返回）：

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

**ProblemDetails 响应**（非异常路径，如 401/404/429）：

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

### CorrelationId 全链路追踪

**生成与传播流程**：

```
Desktop                          Server
  │                                │
  ├─ LoggingHttpHandler            │
  │  添加 traceparent header ──────┤
  │                                ├─ CorrelationIdMiddleware
  │                                │  提取/生成 CorrelationId (12位短GUID)
  │                                │  注入 HttpContext.Items["CorrelationId"]
  │                                │  注入 LogContext (Serilog)
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
```

**提供者体系**：

| 提供者 | 适用场景 | 实现 |
|--------|---------|------|
| `HttpContextCorrelationIdProvider` | Server 端 | 从 `HttpContext.Items` 读取 |
| `AsyncLocalCorrelationIdProvider` | Desktop 端 / 后台任务 | 基于 `AsyncLocal<string?>` 传递 |

**Serilog 富集器** — `CorrelationIdEnricher` 确保所有日志事件包含 CorrelationId，包括非 HTTP 上下文的后台任务。

### Desktop 端异常处理

`DesktopExceptionHandler` (`IDesktopExceptionHandler`) 提供客户端侧处理能力：

- 全局注册 `AppDomain.UnhandledException` 和 `TaskScheduler.UnobservedTaskException`
- `CanRetry()` 判定可重试异常（Timeout/HttpRequest/Socket 异常）
- `SafeExecuteAsync()` 包装异步操作，捕获异常返回 `ServiceResult`

### 敏感数据保护

错误消息中自动脱敏的字段：患者身份证号、手机号、地址、过敏史、病史。StackTrace 在生产环境中被隐藏，防止内部实现泄露。

## 相关链接

- overview — 系统架构总览
- [medical-case-module](modules/medical-case-module.md) — 医案模块的业务异常示例
- [auth-module](modules/auth-module.md) — 认证相关异常类型（UserNotFound, AuthInvalidCredentials）
