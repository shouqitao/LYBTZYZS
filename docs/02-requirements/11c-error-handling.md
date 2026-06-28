# Error Handling (异常处理)

> 版本: v1.0 | 日期: 2026-06-28 | 状态: ✅ 已完成
> Split from 11-platform.md (2026-06-28)

## 模块概述

异常处理采用分层架构：服务端 `IExceptionHandler` 链式处理器（`BusinessExceptionHandler` → `SystemExceptionHandler`）将异常转为 `ApiResponse` JSON；客户端 `DesktopExceptionHandler` 全局兜底 + `ClientErrorMessageMapper` 映射中文消息。`AppException` 体系含 6 种具体异常，各自映射 HTTP 状态码。

> 原 8 US，保留 **8 US**：US-ERR-001~008（含原 US-SHELL-006 全局异常处理合并到 US-ERR-001）。

---

### US-ERR-001: 全局异常处理（Dispatcher+AppDomain）

**角色**: 系统
**优先级**: Must
**状态**: ✅ 已实现

**作为** 系统，**我想要** 全局捕获所有未处理异常（AppDomain + Dispatcher + TaskScheduler 级），**以便** 异常不致崩溃应用且始终被记录（原 US-SHELL-006 全局异常处理已并入本 US）。

**验收标准**:
- [ ] Desktop 端 `AppDomain.UnhandledException` + `TaskScheduler.UnobservedTaskException` 全局注册
- [ ] 服务端 `BusinessExceptionHandler`（优先）→ `SystemExceptionHandler`（兜底）链式处理
- [ ] 业务异常记 Warning；系统异常记 Error
- [ ] 日志含 ExceptionType/ErrorCode/CorrelationId/RequestPath/UserId

**业务规则**:
1. 处理器链顺序：`BusinessExceptionHandler` 仅处理 `AppException` 子类，其余传递给 `SystemExceptionHandler`。
2. `SafeExecuteAsync` 包裹异步操作，异常自动转为 `ServiceResult.Failure`。
3. 级联故障防护：handler 自身异常时由下一 handler 兜底；最大重抛 3 次。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 中间件自动注册，所有 API 端点生效 |
| 本地 | Desktop 全局异常兜底处理本地操作异常 |

**实现参考**: `LYBT.Shared.ExceptionHandling/`（`BusinessExceptionHandler`、`SystemExceptionHandler`）、`DesktopExceptionHandler`

---

### US-ERR-002: 中文友好错误消息

**角色**: 所有用户
**优先级**: Must
**状态**: ✅ 已实现

**作为** 用户，**我想要** 操作失败时看到中文友好提示而非英文技术错误，**以便** 理解问题原因并决定是否重试。

**验收标准**:
- [ ] 服务端返回 errorCode → 用户看到对应中文消息（如"密码不正确"）
- [ ] HTTP 401 → "登录已过期，请重新登录"
- [ ] 未知错误码 → "操作失败，请稍后重试"
- [ ] 业务错误不含追踪码，系统错误含追踪码

**业务规则**:
1. `ClientErrorMessageMapper` 覆盖 HTTP 状态码 + 7 模块 90+ 业务错误码。
2. 优先级：业务错误码 > HTTP 状态码 > 通用兜底。
3. 错误码分区：1xxxx 认证、2xxxx 患者、3xxxx 医案、5xxxx 药材、6xxxx 验方、7xxxx 同步。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 解析服务端 ProblemDetails 的 errorCode |
| 本地 | 解析本地操作异常类型 |

**实现参考**: `ClientErrorMessageMapper`、`ExceptionMessageMapper`

---

### US-ERR-003: 追踪 ID（TraceId）

**角色**: 管理员
**优先级**: Should
**状态**: ✅ 已实现

**作为** 管理员，**我想要** 系统错误消息附带 8 位短追踪码，**以便** 用户反馈问题时通过追踪码快速定位具体异常。

**验收标准**:
- [ ] 系统错误消息 → 含追踪码（8 位时间戳+随机数）
- [ ] 业务错误消息 → 不含追踪码
- [ ] 日志可通过追踪码检索到对应异常详情

**业务规则**:
1. 追踪码格式：8 位短码（时间戳+随机数）。
2. 仅 Error/Critical 级别附加追踪码。
3. 展示格式："如需帮助，请提供追踪码: XXXXXXXX"。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同步记录到服务端日志（经 CorrelationId 关联） |
| 本地 | 记录到本地日志文件 |

**实现参考**: `DesktopExceptionHandler`（追踪码生成）、Serilog 日志

---

### US-ERR-004: CorrelationId 端到端追踪

**角色**: 开发人员
**优先级**: Should
**状态**: ✅ 已实现

**作为** 开发人员，**我想要** 每个请求/操作携带 CorrelationId 贯穿全链路，**以便** 故障发生时快速追踪一个请求在系统中的完整链路。

**验收标准**:
- [ ] 服务端请求日志的 CorrelationId 与请求头 `X-Correlation-Id` 一致
- [ ] Desktop 端日志含 AsyncLocal 注入的 CorrelationId
- [ ] ProblemDetails 响应含 correlationId 字段

**业务规则**:
1. 服务端：从 `HttpContext.Request.Headers["X-Correlation-Id"]` 获取，回退到 `TraceIdentifier`。
2. Desktop：从 `AsyncLocal<string>` 获取（`AsyncLocalCorrelationIdProvider`）。
3. `CorrelationIdEnricher` 自动富集每条日志。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（Desktop 端 AsyncLocal 注入） |

**实现参考**: `AsyncLocalCorrelationIdProvider`、`CorrelationIdEnricher`、CorrelationId 中间件

---

### US-ERR-005: 生产环境堆栈屏蔽

**角色**: 安全管理者
**优先级**: Must
**状态**: ✅ 已实现

**作为** 安全管理者，**我想要** 生产环境错误响应隐藏堆栈跟踪与异常类型，**以便** 防止内部实现泄露扩大攻击面。

**验收标准**:
- [ ] Production 环境 ProblemDetails 不含 `stackTrace` / `exceptionType`
- [ ] Development 环境额外包含 `stackTrace`
- [ ] ValidationException 额外含 `errors` 字典

**业务规则**:
1. 环境感知：仅 Development 返回堆栈。
2. 标准字段：type/title/status/detail/instance。
3. 扩展字段：errorCode/correlationId/traceId/timestamp。
4. ConflictException 额外含 entityType/entityId。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 服务端按环境决定返回字段 |
| 本地 | Desktop 使用 `ClientProblemDetails` 解析 |

**实现参考**: `BusinessExceptionHandler`、`SystemExceptionHandler`、`ClientProblemDetails`

---

### US-ERR-006: 验证错误统一格式（422）

**角色**: 开发人员
**优先级**: Should
**状态**: ✅ 已实现

**作为** 开发人员，**我想要** 验证错误以统一格式返回（含字段级 errors 字典），**以便** 客户端能在表单内精确高亮错误字段。

**验收标准**:
- [ ] `ValidationException` → 400 + `errors` 字典（字段名 → 错误消息数组）
- [ ] 支持多字段错误收集（链式 `AddError`）
- [ ] 字段级错误同时在表单内显示 + 顶部错误摘要

**业务规则**:
1. `ValidationException`(HTTP 400, Category=Validation) 附带 `Errors` 字典 + `FieldName`。
2. `FluentValidation.ValidationException` 映射为 400 "验证失败"。
3. `ValidationException` 支持链式 `AddError` 收集多字段错误。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 服务端统一格式返回 |
| 本地 | 完全一致（LocalWebAPI 复用 handler） |

**实现参考**: `ValidationException`、`BusinessExceptionHandler`

---

### US-ERR-007: 业务异常分类

**角色**: 开发人员
**优先级**: Should
**状态**: ✅ 已实现

**作为** 开发人员，**我想要** 使用分层的异常类型体系抛出业务异常，**以便** 每种异常自动映射到正确 HTTP 状态码与错误类别。

**验收标准**:
- [ ] `BusinessException` → 400
- [ ] `NotFoundException` → 404（含 ResourceType/ResourceId）
- [ ] `ConflictException` → 409（含 EntityType/EntityId/版本）
- [ ] `UnauthorizedException` → 401（含 FailureReason）
- [ ] 每类提供静态工厂方法（如 `NotFoundException.User(guid)`）

**业务规则**:
1. `AppException` 基类含 ErrorCode/TypedErrorCode/UserMessage/ShowDetailToUser。
2. 6 种具体异常：Business/NotFound/Conflict/Validation/Unauthorized/Api。
3. 每类提供按实体分组的工厂方法（User/Patient/Herb/Formula/MedicalCase/...）。
4. A1 统一异常体系重构：Service 层采用 throw 域异常，消除 47 处 `InvalidOperationException`。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 服务端抛出，中间件处理 |
| 本地 | Desktop 直接捕获处理 |

**实现参考**: `AppException` 体系（`LYBT.Shared.ExceptionHandling`）、`ExceptionFactory`

---

### US-ERR-008: 异常层级（Validation/NotFound/Conflict → Business）

**角色**: 开发人员
**优先级**: Should
**状态**: ✅ 已实现

**作为** 开发人员，**我想要** 异常类型遵循清晰的继承层级与严重度分级，**以便** 日志级别与用户通知方式能根据严重度自动适配。

**验收标准**:
- [ ] `ValidationException`/`NotFoundException`/`ConflictException` → Toast 红色（不自动消失）
- [ ] `UnauthorizedException` → 对话框（手动关闭）
- [ ] `HttpRequestException`/`TimeoutException` → Toast 黄色（5 秒，可重试）
- [ ] 未知异常 → 对话框 + 追踪码

**业务规则**:
1. 继承层级：`Exception → AppException → {Business, NotFound, Conflict, Validation, Unauthorized, Api}`。
2. 严重度分级：Information(0)/Warning(1)/Error(2)/Critical(3)。
3. 严重度映射日志级别，并决定通知类型（Toast vs 对话框）。
4. 遵循 UI 通知规范：业务错误用 Toast，系统错误用对话框。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | API 调用异常的 Desktop 展示 |
| 本地 | 本地操作异常的 Desktop 展示 |

**实现参考**: `ErrorSeverity`、`ErrorCategory`、`DesktopExceptionHandler`

---

## 变更记录

| 版本 | 日期 | 变更 | 原因 |
|------|------|------|------|
| v1.0 | 2026-06-28 | Split from 11-platform.md into focused module | 文档结构优化 S4 批次 3 |
