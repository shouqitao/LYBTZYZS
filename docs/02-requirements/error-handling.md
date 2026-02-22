# 异常处理策略 需求规格

## 概述

系统采用分层异常处理架构，服务端通过 IExceptionHandler 链式处理器自动捕获异常并转换为 RFC 7807 ProblemDetails 标准响应，客户端通过 DesktopExceptionHandler 提供用户友好提示和全局异常兜底。异常体系基于 AppException 基类，派生出 BusinessException、NotFoundException、ConflictException、ValidationException、UnauthorizedException、ApiException 六种具体异常类型。

---

## 用户角色

| 角色 | 在本模块中的交互 |
|------|-----------------|
| 所有角色 | 接收异常处理后的用户友好错误消息 |
| 开发人员 | 开发环境可查看 stackTrace 和详细异常信息 |

> 异常处理是系统基础设施，对所有角色透明运作。

---

## 功能清单

### FR-ERR-001: 服务端全局异常处理

- **描述**: 通过 IExceptionHandler 链式处理器自动捕获所有未处理异常，转换为标准化 JSON 响应
- **业务规则**:
  1. 处理器链: BusinessExceptionHandler (优先) -> SystemExceptionHandler (兜底)
  2. BusinessExceptionHandler 仅处理 AppException 及其子类，其他异常传递给下一个处理器
  3. SystemExceptionHandler 兜底处理所有未被前者处理的异常
  4. 业务异常 (AppException) 记录 Warning 级别日志
  5. 系统异常 (非 AppException) 记录 Error 级别日志
  6. 日志包含: ExceptionType, ErrorCode, Message, CorrelationId, RequestPath, HttpMethod, UserId
- **远程模式**: 中间件自动注册，所有 API 端点生效
- **本地模式**: 不适用 (服务端功能)
- **验收标准**:
  - [ ] BusinessException 抛出 -> 返回 400 + ProblemDetails
  - [ ] NotFoundException 抛出 -> 返回 404 + ProblemDetails
  - [ ] ConflictException 抛出 -> 返回 409 + ProblemDetails
  - [ ] ValidationException 抛出 -> 返回 400 + ProblemDetails + errors 字段
  - [ ] UnauthorizedException 抛出 -> 返回 401 + ProblemDetails
  - [ ] 未知异常抛出 -> 返回 500 + ProblemDetails (生产环境隐藏详情)

### FR-ERR-002: ProblemDetails 标准化

- **描述**: 所有错误响应遵循 RFC 7807 Problem Details 标准格式
- **业务规则**:
  1. 标准字段: type (RFC URI), title, status (HTTP状态码), detail (用户友好消息), instance (请求路径)
  2. 扩展字段: errorCode (类型化错误码), correlationId, traceId, timestamp
  3. ValidationException 额外包含 errors 字典 (字段名 -> 错误消息数组)
  4. ConflictException 额外包含 entityType, entityId
  5. 开发环境额外包含 exceptionType, stackTrace
  6. Content-Type: application/problem+json
- **远程模式**: 所有 API 错误响应
- **本地模式**: 客户端使用 ClientProblemDetails 模型解析服务端返回的 ProblemDetails
- **验收标准**:
  - [ ] 所有错误响应 Content-Type -> application/problem+json
  - [ ] ProblemDetails 包含 type, title, status, detail, instance
  - [ ] ProblemDetails 包含 errorCode, correlationId, traceId, timestamp
  - [ ] 开发环境额外包含 stackTrace
  - [ ] 生产环境不包含 stackTrace

### FR-ERR-003: 客户端异常处理

- **描述**: Desktop 客户端通过 DesktopExceptionHandler 统一处理异常，提供用户友好消息和全局兜底
- **业务规则**:
  1. 全局异常注册: AppDomain.UnhandledException + TaskScheduler.UnobservedTaskException
  2. 用户友好消息映射: ExceptionMessageMapper 根据异常类型生成中文提示
  3. 异常严重度分级决定日志级别: Information/Warning/Error/Critical
  4. 可重试判断: TimeoutException, HttpRequestException, TaskCanceledException, SocketException -> 可重试
  5. SafeExecuteAsync: 包裹异步操作，自动捕获异常返回 ServiceResult.Failure
  6. ServiceResult 模式: 异常转换为 ServiceResult<T>.Failure(userMessage)
- **远程模式**: 处理 API 调用产生的异常
- **本地模式**: 处理本地操作产生的异常
- **验收标准**:
  - [ ] 未处理异常 -> 全局捕获，记录 Critical 日志
  - [ ] TimeoutException -> CanRetry=true，提示用户可重试
  - [ ] HttpRequestException -> 提示网络错误
  - [ ] 未知异常 -> 用户友好消息 "操作失败，请稍后重试"

### FR-ERR-004: 异常类型体系

- **描述**: 基于 AppException 的分层异常类型体系，每种异常对应特定 HTTP 状态码和错误类别
- **业务规则**:
  1. AppException (基类): 包含 ErrorCode (字符串), TypedErrorCode (枚举), UserMessage, ShowDetailToUser
  2. BusinessException: HTTP 400, Category=Business, 附带 BusinessRule 描述
  3. NotFoundException: HTTP 404, Category=Resource, 附带 ResourceType + ResourceId，提供静态工厂方法 (User/Patient/Herb/Formula/MedicalCase/Consultation/Prescription)
  4. ConflictException: HTTP 409, Category=Concurrency, 附带 EntityType + EntityId + CurrentVersion + ExpectedVersion，提供工厂方法 (MedicalCaseVersion/MedicalCaseLocked/Duplicate)
  5. ValidationException: HTTP 400, Category=Validation, 附带 Errors 字典 + FieldName，支持链式 AddError
  6. UnauthorizedException: HTTP 401, Category=Authentication, 附带 AuthenticationScheme + FailureReason，提供工厂方法 (InvalidPassword/CredentialsExpired/UserDisabled/UserLocked/TokenExpired/DeviceMismatch/SessionExpired)
  7. ApiException: HTTP 取决于 StatusCode, Category=External, 附带 StatusCode + ResponseContent + RequestUrl + HttpMethod
- **远程模式**: 服务端抛出，中间件处理
- **本地模式**: 客户端直接捕获处理
- **验收标准**:
  - [ ] 每种异常类型映射到正确的 HTTP 状态码
  - [ ] NotFoundException.User(guid) -> 404 + ErrorCode=UserNotFound
  - [ ] ConflictException.Duplicate("用户", "用户名", "admin") -> 409
  - [ ] ValidationException 支持多字段错误收集

### FR-ERR-005: 异常严重度分级

- **描述**: 客户端异常按严重程度分四级，决定日志级别和用户通知方式
- **业务规则**:
  1. Information (0): 信息级别，如正常业务流程中的预期异常
  2. Warning (1): 警告级别，如参数错误 (ArgumentException, InvalidOperationException)
  3. Error (2): 错误级别，如未授权访问 (UnauthorizedAccessException, OutOfMemoryException)
  4. Critical (3): 严重级别，如全局未处理异常 (AppDomain.UnhandledException)
  5. 严重度映射日志级别: Information->LogLevel.Information, Warning->LogLevel.Warning, Error->LogLevel.Error, Critical->LogLevel.Critical
- **远程模式**: 不适用 (客户端功能)
- **本地模式**: DesktopExceptionHandler 根据异常类型自动确定严重度
- **验收标准**:
  - [ ] HttpRequestException -> Information 级别 (网络临时问题)
  - [ ] ArgumentException -> Warning 级别
  - [ ] OutOfMemoryException -> Error 级别
  - [ ] AppDomain 未处理异常 -> Critical 级别

> **[已修订 2026-02-21]** 错误消息文案要求简化，PRD 不再硬性规定具体文案内容，允许实现自行调整措辞
> 原因: 文案属过度规范，具体措辞由实现决定  |  参考: ERR-07

### FR-ERR-006: 客户端错误消息映射体系

- **描述**: 通过 ClientErrorMessageMapper 将 HTTP 状态码和业务错误码映射为中文用户友好消息，确保用户不直接看到技术细节
- **业务规则**:
  1. HTTP 状态码映射: 覆盖常见 HTTP 错误码到中文消息
  2. 业务错误码映射: 按模块分组 (7 个模块)，覆盖 90+ 业务场景
  3. 操作失败兜底: 未匹配到具体错误码时返回通用消息 "操作失败，请稍后重试"
  4. 优先级: 业务错误码 > HTTP 状态码 > 通用兜底

#### HTTP 状态码映射

| 状态码 | 用户消息 |
|--------|---------|
| 400 | 请求参数无效，请检查输入 |
| 401 | 登录已过期，请重新登录 |
| 403 | 您没有权限执行此操作 |
| 404 | 请求的数据不存在 |
| 409 | 数据已被其他用户修改，请刷新后重试 |
| 429 | 操作过于频繁，请稍后再试 |
| 500 | 服务器内部错误 |
| 502 | 服务暂时不可用 |
| 503 | 服务暂时不可用，请稍后重试 |
| 504 | 请求超时，请稍后重试 |

#### 业务错误码映射 (按模块)

| 模块 | 错误码范围 | 覆盖场景 |
|------|-----------|---------|
| 用户/认证 | 1xxxx (101xx~103xx) | 密码错误/Token过期/权限不足等 |
| 患者管理 | 2xxxx (200xx~208xx) | 患者不存在/身份证重复/引用保护/导入等 |
| 医案管理 | 3xxxx (301xx~306xx) | 医案不存在/权限/状态转换/处方/并发等 |
| 处方管理 | 4xxxx (预留) | 当前处方错误归入医案 304xx |
| 药材管理 | 5xxxx (501xx~503xx) | 药材不存在/批量操作/导入等 |
| 验方管理 | 6xxxx (601xx~603xx) | 验方不存在/药材验证/批量操作等 |
| 数据同步 | 7xxxx (701xx~705xx) | 实体类型/上传/医案同步/删除/客户端等 |

- **远程模式**: 解析服务端返回的 ProblemDetails 中的 errorCode
- **本地模式**: 解析本地操作产生的异常类型
- **验收标准**:
  - [ ] 服务端返回 errorCode=10004 -> 用户看到"密码不正确"
  - [ ] 服务端返回 HTTP 401 (无 errorCode) -> 用户看到"登录已过期，请重新登录"
  - [ ] 未知错误码 -> 用户看到"操作失败，请稍后重试"

### FR-ERR-007: 错误追踪码

- **描述**: 异常消息支持附加短追踪码，方便用户反馈问题时提供定位信息
- **业务规则**:
  1. 追踪码格式: 8 位短码 (时间戳+随机数)
  2. 仅在系统错误级别 (Error/Critical) 附加追踪码，业务错误不附加
  3. 追踪码展示格式: "如需帮助，请提供追踪码: XXXXXXXX"
  4. 追踪码同时记录到日志中，支持通过追踪码定位具体异常

#### 追踪码展示示例

```
服务器内部错误，请联系管理员。

如需帮助，请提供追踪码: A3F8B2C1
```

- **远程模式**: 同时记录到服务端日志 (通过 CorrelationId 关联)
- **本地模式**: 记录到本地日志文件
- **验收标准**:
  - [ ] 系统错误消息 -> 包含追踪码
  - [ ] 业务错误消息 (如"密码不正确") -> 不包含追踪码
  - [ ] 日志中可通过追踪码检索到对应异常详情

### FR-ERR-008: 异常通知类型映射

- **描述**: 将异常类型映射到 ui-patterns.md 定义的通知层级 (Toast / 对话框)，确保异常展示方式与 UI 规范一致
- **业务规则**:
  1. 遵循 ui-patterns.md 第 3.3 节通知规范
  2. 异常严重度决定通知类型
  3. 可重试异常提供重试按钮

#### 异常到通知类型映射

| 异常类型 | 通知方式 | 持续时间 | 说明 |
|----------|---------|---------|------|
| ValidationException | Toast (红色) | 不自动消失 | 字段级错误同时在表单内显示 |
| NotFoundException | Toast (红色) | 不自动消失 | 数据不存在 |
| BusinessException | Toast (红色) | 不自动消失 | 业务规则违反 |
| ConflictException | Toast (红色) | 不自动消失 | 并发冲突，建议刷新 |
| UnauthorizedException | **对话框** | 手动关闭 | 需要重新登录，阻塞当前操作 |
| HttpRequestException | Toast (黄色/警告) | 5 秒 | 网络临时问题，可重试 |
| TimeoutException | Toast (黄色/警告) | 5 秒 | 超时，可重试 |
| 未知异常 / 系统错误 | **对话框** (含追踪码) | 手动关闭 | 需要用户关注并可能反馈 |

> **已确定**: 遵循 ui-patterns.md 3.3 节标准。当前代码统一使用 MessageBox，需重构为 Toast + 对话框分层展示。

- **远程模式**: API 调用异常的 Desktop 端展示
- **本地模式**: 本地操作异常的 Desktop 端展示
- **验收标准**:
  - [ ] BusinessException -> Toast 红色通知，不自动消失
  - [ ] HttpRequestException -> Toast 黄色警告，5 秒后消失
  - [ ] UnauthorizedException -> 对话框，手动关闭
  - [ ] 系统错误 -> 对话框 + 追踪码

---

## 数据模型

### 异常类型继承体系

```
Exception
  └─ AppException (ErrorCode, TypedErrorCode, UserMessage, ShowDetailToUser)
       ├─ BusinessException (BusinessRule) -> HTTP 400
       ├─ NotFoundException (ResourceType, ResourceId) -> HTTP 404
       ├─ ConflictException (EntityType, EntityId, CurrentVersion, ExpectedVersion) -> HTTP 409
       ├─ ValidationException (Errors, FieldName) -> HTTP 400
       ├─ UnauthorizedException (AuthenticationScheme, FailureReason) -> HTTP 401
       └─ ApiException (StatusCode, ResponseContent, RequestUrl, HttpMethod) -> HTTP varies
```

### ProblemDetails 响应结构

| 字段 | 类型 | 说明 |
|------|------|------|
| type | string | RFC 问题类型 URI |
| title | string | 错误标题 (如 "验证失败", "资源未找到") |
| status | int | HTTP 状态码 |
| detail | string | 用户友好的详细描述 |
| instance | string | 请求路径 |
| errorCode | string | 类型化错误码 (如 "ERR-10101"，5位数编号体系) |
| correlationId | string | 请求关联ID |
| traceId | string | 请求追踪ID |
| timestamp | DateTimeOffset | 时间戳 |
| errors | Dictionary | 验证错误详情 (仅 ValidationException) |
| entityType | string | 实体类型 (仅 ConflictException) |
| entityId | string | 实体ID (仅 ConflictException) |
| exceptionType | string | 异常类型全名 (仅开发环境) |
| stackTrace | string | 堆栈跟踪 (仅开发环境) |

### ClientProblemDetails (客户端解析模型)

| 字段 | 类型 | 说明 |
|------|------|------|
| Status | int? | HTTP 状态码 |
| Title | string? | 错误标题 |
| Detail | string? | 错误详情 |
| Type | string? | 错误类型 URI |
| Instance | string? | 请求实例 |
| ErrorCode | string? | 业务错误码 |
| CorrelationId | string? | 关联ID |
| TraceId | string? | 追踪ID |
| Timestamp | DateTimeOffset? | 时间戳 |
| Errors | Dictionary? | 验证错误 |

**便捷属性**: IsValidationError, IsNotFoundError, IsUnauthorizedError, IsForbiddenError, IsConcurrencyError, IsServerError

### ExceptionSeverity 枚举

| 值 | 名称 | 说明 |
|----|------|------|
| 0 | Information | 信息级别 |
| 1 | Warning | 警告级别 |
| 2 | Error | 错误级别 |
| 3 | Critical | 严重错误级别 |

> **[已修订 2026-02-21]** 错误分类枚举值对齐代码定义，PRD 枚举值以代码实际实现为准
> 原因: 枚举值细节差异不影响功能，PRD 对齐代码  |  参考: ERR-08

### ErrorCategory 枚举

| 类别 | 对应异常 | 标题文本 |
|------|----------|----------|
| Validation | ValidationException | "验证失败" |
| Authentication | UnauthorizedException | "身份认证失败" |
| Authorization | (权限不足) | "权限不足" |
| Resource | NotFoundException | "资源未找到" |
| Business | BusinessException | "业务规则错误" |
| Concurrency | ConflictException | "并发冲突" |
| System | (系统异常) | "系统错误" |
| External | ApiException | "外部服务错误" |
| Configuration | (配置异常) | "配置错误" |
| General | (默认) | "操作失败" |

> **[已修订 2026-02-21]** 错误日志格式要求放宽，日志输出格式允许与 PRD 描述存在差异
> 原因: 现有日志格式可接受，PRD 放宽格式要求  |  参考: ERR-09

### SystemExceptionHandler 异常映射表

| 异常类型 | HTTP | 标题 | 详情 |
|----------|------|------|------|
| FluentValidation.ValidationException | 400 | 验证失败 | 请求数据验证失败，请检查输入 |
| UnauthorizedAccessException | 403 | 权限不足 | 您没有权限执行此操作 |
| OperationCanceledException | 499 | 请求已取消 | 客户端取消了请求 |
| TimeoutException | 504 | 请求超时 | 服务器处理请求超时 |
| DbUpdateConcurrencyException | 409 | 并发冲突 | 数据已被其他用户修改 |
| DbUpdateException | 500 | 数据库错误 | 数据保存失败 |
| HttpRequestException | 502 | 外部服务错误 | 调用外部服务失败 |
| ArgumentException | 400 | 参数错误 | 请求参数无效 |
| NullReferenceException | 500 | 服务器内部错误 | 处理请求时发生错误 |
| InvalidOperationException | 500 | 操作无效 | 操作无法执行 |
| 其他 | 500 | 服务器内部错误 | 处理请求时发生错误 |

---

## 决策记录

| # | 决策 | 结论 | 日期 |
|---|------|------|------|
| 1 | 异常处理器链式设计 | BusinessExceptionHandler 优先处理 AppException，SystemExceptionHandler 兜底处理所有异常 | 2026-02-11 |
| 2 | 生产环境信息隐藏 | 生产环境 ProblemDetails 不包含 stackTrace 和 exceptionType | 2026-02-11 |
| 3 | 客户端 ServiceResult 模式 | SafeExecuteAsync 包裹异步操作，异常自动转为 Failure 结果 | 2026-02-11 |
| 4 | ExceptionFactory 静态工厂 | 提供按业务实体分组的便捷工厂方法，统一异常创建方式 | 2026-02-11 |
| 5 | 异常展示遵循 UI 规范 | 遵循 ui-patterns.md 3.3 节: 业务错误用 Toast，系统错误用对话框。代码需从统一 MessageBox 重构为分层展示 | 2026-02-17 |
| 6 | 错误消息映射体系 | ClientErrorMessageMapper 覆盖 HTTP 状态码 + 40+ 业务错误码到中文消息，纳入 v1.0 | 2026-02-17 |
| 7 | 追踪码纳入 v1.0 | 系统错误附加 8 位短追踪码，同步记录到日志，方便用户反馈定位。业务错误不附加 | 2026-02-17 |
| 8 | **A1: 统一异常体系重构** | Service 层全面采用 throw BusinessException/NotFoundException，消除 InvalidOperationException (47处) 和 Result.Failure("硬编码") (11处)。MedicalCase CQRS Service 直接 throw 域异常→ExceptionHandler 统一映射 HTTP 4xx。BaseService 模块方法签名从 Task\<Result\<T\>\> 改为 Task\<T\>。新增 ~8 个 ErrorCode (PrescriptionAlreadyPrinted/PrescriptionRequired/InvalidStatusTransition/CompletedCaseImmutable/HerbAlreadyVerified 等)。Result\<T\> 仅保留用于批量操作和验证聚合。Desktop LocalData 9处保持 InvalidOperationException (SYNC-D02 后整体删除) | 2026-02-22 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-11 | v1.0 | 初始版本，从代码实现逆向工程 |
| 2026-02-17 | v2.0 | R8 深化: 新增 FR-ERR-006 (错误消息映射体系)、FR-ERR-007 (追踪码)、FR-ERR-008 (异常通知类型映射)、3 条新决策 |
| 2026-02-17 | v2.1 | PRD审查修复: D2-errorCode示例格式对齐5位数体系(AUTH-101->ERR-10101) |
| 2026-02-18 | v2.2 | 错误码全量分配: 范围表更新为5位MCCEE体系，新增同步模块7xxxx，处方模块4xxxx标注预留 |
| 2026-02-21 | v2.3 | PRD vs Code 偏差分析修订: 3 项修订, 0 项延期标注 |
| 2026-02-22 | v2.4 | **A1 统一异常体系重构决策**: 新增决策 #8 -- Service 层全面 throw 域异常取代 InvalidOperationException(47处)/Result.Failure(11处); 新增 ~8 个 ErrorCode; BaseService 方法签名简化; Result 模式仅保留批量/验证场景 |
