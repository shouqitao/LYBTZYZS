# 异常处理策略 产品需求文档

---

## 1. Problem Statement

### 1.1 问题描述

中医诊所管理系统涉及多层架构 (Desktop WPF 客户端 + ASP.NET Core 服务端)，异常可能在任何层级发生: 数据库操作失败、网络请求超时、业务规则违反、并发冲突等。缺乏统一的异常处理策略会导致: 用户看到技术性错误堆栈 (如 NullReferenceException)、不同模块错误格式不一致、生产环境泄露敏感信息、异常无法追踪定位。

### 1.2 用户痛点

| 角色 | 痛点 | 影响 |
|------|------|------|
| 医生 | 操作失败时看到英文技术错误消息，无法理解问题原因 | 被迫中断诊疗流程，需要求助 IT 人员 |
| 医生 | 网络波动导致操作失败，不知道是否可以重试 | 反复操作或放弃操作，影响工作效率 |
| 管理员 | 用户报告 "系统出错" 但无法提供定位信息 | 排查问题耗时长，无法快速响应 |
| 开发人员 | 不同模块异常处理方式不统一 (有的返回 Result，有的 throw) | 维护成本高，容易遗漏异常处理 |

### 1.3 证据

- 代码审计发现: Service 层存在 47 处 InvalidOperationException 和 11 处 Result.Failure("硬编码消息")，缺乏统一异常类型
- 当前 Desktop 端统一使用 MessageBox 展示所有错误，未区分业务错误和系统错误
- 生产环境曾返回完整 stackTrace，存在信息泄露风险

---

## 2. Target Users

| 角色 | 在本模块中的交互 |
|------|-----------------|
| 所有角色 (医生/管理员/前台) | 接收异常处理后的中文用户友好错误消息 |
| 管理员 | 收集用户反馈的追踪码，提交给技术支持 |
| 开发人员 | 开发环境查看 stackTrace 和详细异常信息，通过追踪码定位生产异常 |

> 异常处理是系统基础设施，对终端用户透明运作。用户仅在操作失败时感知其存在。

---

## 3. Strategic Context

### 3.1 业务目标

| 目标 | 对应 |
|------|------|
| 用户体验一致性 | 所有错误场景返回中文用户友好消息，消除技术性错误暴露 |
| 问题快速定位 | 追踪码 + CorrelationId + 结构化日志，缩短问题排查时间 |
| 安全合规 | 生产环境隐藏技术细节 (stackTrace/exceptionType)，防止信息泄露 |
| 开发效率 | 统一异常类型体系 + ExceptionHandler 自动映射，减少重复的异常处理代码 |

### 3.2 Why Now

系统进入正式开发阶段，Service 层存在 47 处 InvalidOperationException 和 11 处硬编码 Result.Failure，异常处理方式不统一。在功能模块持续增加前建立统一异常体系，避免技术债务随代码规模指数增长。

---

## 4. Solution Overview

异常处理模块采用分层架构，服务端和客户端各自承担明确职责:

**服务端 (ASP.NET Core):**
- **IExceptionHandler 链式处理器**: BusinessExceptionHandler (优先) -> SystemExceptionHandler (兜底)，自动捕获异常并转换为 RFC 7807 ProblemDetails 标准响应
- **AppException 类型体系**: 6 种具体异常类型 (Business/NotFound/Conflict/Validation/Unauthorized/Api)，每种对应特定 HTTP 状态码
- **环境感知**: 开发环境返回 stackTrace，生产环境隐藏技术细节

**客户端 (WPF Desktop):**
- **DesktopExceptionHandler**: 全局异常兜底 (AppDomain.UnhandledException + TaskScheduler.UnobservedTaskException)
- **ClientErrorMessageMapper**: HTTP 状态码 + 业务错误码 -> 中文用户友好消息
- **ServiceResult 模式**: SafeExecuteAsync 包裹异步操作，异常自动转为 Failure 结果
- **分层通知**: 业务错误用 Toast，系统错误用对话框 (遵循 ui-patterns.md 3.3 节)

**异常处理流程:**
```
[服务端]
Service 抛出 AppException → BusinessExceptionHandler 捕获 → ProblemDetails 响应 (400/404/409/401)
Service 抛出未知异常    → SystemExceptionHandler 兜底  → ProblemDetails 响应 (500) + Error 日志

[客户端]
API 响应错误 → ClientErrorMessageMapper 映射中文消息 → 严重度分级 → Toast / 对话框展示
本地操作异常 → DesktopExceptionHandler 捕获 → SafeExecuteAsync → ServiceResult.Failure
全局未处理   → AppDomain.UnhandledException → Critical 日志 + 追踪码 + 对话框
```

---

## 5. Success Metrics

| 指标 | 当前 | v1.0 目标 | 衡量方式 |
|------|------|----------|---------|
| 技术性错误暴露率 | 存在 (stackTrace 可能暴露) | 0% (生产环境零技术细节泄露) | 代码审查 + 渗透测试 |
| 错误消息中文覆盖率 | 部分 | 100% (所有用户可见错误为中文) | ClientErrorMessageMapper 覆盖率统计 |
| 异常类型统一率 | ~50% (47处 InvalidOperationException) | 100% (全部使用 AppException 子类) | 代码扫描 |
| 问题定位时间 | 依赖用户描述 | < 5 分钟 (通过追踪码) | 支持工单统计 |
| 错误码覆盖模块数 | 0 | 7 个模块全覆盖 | ClientErrorMessageMapper 模块统计 |

---

## 6. Epic Hypothesis

We believe that 实现统一的分层异常处理体系 (AppException 类型体系 + IExceptionHandler 链式处理器 + ClientErrorMessageMapper 中文映射 + 追踪码) for 诊所全部用户和开发团队 will achieve 用户友好的错误体验与高效的问题排查能力。We'll know we're right when 生产环境零技术细节泄露、所有用户可见错误为中文消息、且通过追踪码可在 5 分钟内定位任何异常。

---

## 7. User Stories

### 优先级汇总

| US 编号 | 名称 | Priority |
|---------|------|----------|
| US-ERR-001 | 服务端全局异常处理 | Should |
| US-ERR-002 | ProblemDetails 标准化 | Should |
| US-ERR-003 | 客户端异常处理 | Should |
| US-ERR-004 | 异常类型体系 | Should |
| US-ERR-005 | 异常严重度分级 | Should |
| US-ERR-006 | 客户端错误消息映射体系 | Should |
| US-ERR-007 | 错误追踪码 | Could |
| US-ERR-008 | 异常通知类型映射 | Could |

---

### US-ERR-001: 服务端全局异常处理

> As a 系统, I want to 通过 IExceptionHandler 链式处理器自动捕获所有未处理异常并转换为标准化 JSON 响应,
> so that 客户端始终收到格式一致的错误响应，而非原始异常信息。

**Acceptance Criteria:**
- [ ] BusinessException 抛出 -> 返回 400 + ProblemDetails
- [ ] NotFoundException 抛出 -> 返回 404 + ProblemDetails
- [ ] ConflictException 抛出 -> 返回 409 + ProblemDetails
- [ ] ValidationException 抛出 -> 返回 400 + ProblemDetails + errors 字段
- [ ] UnauthorizedException 抛出 -> 返回 401 + ProblemDetails
- [ ] 未知异常抛出 -> 返回 500 + ProblemDetails (生产环境隐藏详情)

**Business Rules:**
1. 处理器链: BusinessExceptionHandler (优先) -> SystemExceptionHandler (兜底)
2. BusinessExceptionHandler 仅处理 AppException 及其子类，其他异常传递给下一个处理器
3. SystemExceptionHandler 兜底处理所有未被前者处理的异常
4. 业务异常 (AppException) 记录 Warning 级别日志
5. 系统异常 (非 AppException) 记录 Error 级别日志
6. 日志包含: ExceptionType, ErrorCode, Message, CorrelationId, RequestPath, HttpMethod, UserId

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 中间件自动注册，所有 API 端点生效 |
| 本地 | 不适用 (服务端功能) |

### US-ERR-002: ProblemDetails 标准化

> As a 客户端开发者, I want to 所有错误响应遵循 RFC 7807 Problem Details 标准格式,
> so that 客户端可以用统一的解析逻辑处理所有错误响应。

**Acceptance Criteria:**
- [ ] 所有错误响应 Content-Type -> application/problem+json
- [ ] ProblemDetails 包含 type, title, status, detail, instance
- [ ] ProblemDetails 包含 errorCode, correlationId, traceId, timestamp
- [ ] 开发环境额外包含 stackTrace
- [ ] 生产环境不包含 stackTrace

**Business Rules:**
1. 标准字段: type (RFC URI), title, status (HTTP状态码), detail (用户友好消息), instance (请求路径)
2. 扩展字段: errorCode (类型化错误码), correlationId, traceId, timestamp
3. ValidationException 额外包含 errors 字典 (字段名 -> 错误消息数组)
4. ConflictException 额外包含 entityType, entityId
5. 开发环境额外包含 exceptionType, stackTrace
6. Content-Type: application/problem+json

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 所有 API 错误响应遵循此格式 |
| 本地 | 客户端使用 ClientProblemDetails 模型解析服务端返回的 ProblemDetails |

### US-ERR-003: 客户端异常处理

> As a 医生, I want to 操作失败时看到中文友好提示而非技术错误信息,
> so that 我能理解问题原因并决定是否重试。

**Acceptance Criteria:**
- [ ] 未处理异常 -> 全局捕获，记录 Critical 日志
- [ ] TimeoutException -> CanRetry=true，提示用户可重试
- [ ] HttpRequestException -> 提示网络错误
- [ ] 未知异常 -> 用户友好消息 "操作失败，请稍后重试"

**Business Rules:**
1. 全局异常注册: AppDomain.UnhandledException + TaskScheduler.UnobservedTaskException
2. 用户友好消息映射: ExceptionMessageMapper 根据异常类型生成中文提示
3. 异常严重度分级决定日志级别: Information/Warning/Error/Critical
4. 可重试判断: TimeoutException, HttpRequestException, TaskCanceledException, SocketException -> 可重试
5. SafeExecuteAsync: 包裹异步操作，自动捕获异常返回 ServiceResult.Failure
6. ServiceResult 模式: 异常转换为 ServiceResult<T>.Failure(userMessage)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 处理 API 调用产生的异常 |
| 本地 | 处理本地操作产生的异常 |

### US-ERR-004: 异常类型体系

> As a 开发人员, I want to 使用分层的异常类型体系抛出业务异常,
> so that 每种异常自动映射到正确的 HTTP 状态码和错误类别，无需手动处理。

**Acceptance Criteria:**
- [ ] 每种异常类型映射到正确的 HTTP 状态码
- [ ] NotFoundException.User(guid) -> 404 + ErrorCode=UserNotFound
- [ ] ConflictException.Duplicate("用户", "用户名", "admin") -> 409
- [ ] ValidationException 支持多字段错误收集

**Business Rules:**
1. AppException (基类): 包含 ErrorCode (字符串), TypedErrorCode (枚举), UserMessage, ShowDetailToUser
2. BusinessException: HTTP 400, Category=Business, 附带 BusinessRule 描述
3. NotFoundException: HTTP 404, Category=Resource, 附带 ResourceType + ResourceId，提供静态工厂方法 (User/Patient/Herb/Formula/MedicalCase/Consultation/Prescription)
4. ConflictException: HTTP 409, Category=Concurrency, 附带 EntityType + EntityId + CurrentVersion + ExpectedVersion，提供工厂方法 (MedicalCaseVersion/MedicalCaseLocked/Duplicate)
5. ValidationException: HTTP 400, Category=Validation, 附带 Errors 字典 + FieldName，支持链式 AddError
6. UnauthorizedException: HTTP 401, Category=Authentication, 附带 AuthenticationScheme + FailureReason，提供工厂方法 (InvalidPassword/CredentialsExpired/UserDisabled/UserLocked/TokenExpired/DeviceMismatch/SessionExpired)
7. ApiException: HTTP 取决于 StatusCode, Category=External, 附带 StatusCode + ResponseContent + RequestUrl + HttpMethod

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 服务端抛出，中间件处理 |
| 本地 | 客户端直接捕获处理 |

### US-ERR-005: 异常严重度分级

> As a 开发人员, I want to 异常按严重程度自动分为四级,
> so that 日志级别和用户通知方式能根据严重度自动适配。

**Acceptance Criteria:**
- [ ] HttpRequestException -> Information 级别 (网络临时问题)
- [ ] ArgumentException -> Warning 级别
- [ ] OutOfMemoryException -> Error 级别
- [ ] AppDomain 未处理异常 -> Critical 级别

**Business Rules:**
1. Information (0): 信息级别，如正常业务流程中的预期异常
2. Warning (1): 警告级别，如参数错误 (ArgumentException, InvalidOperationException)
3. Error (2): 错误级别，如未授权访问 (UnauthorizedAccessException, OutOfMemoryException)
4. Critical (3): 严重级别，如全局未处理异常 (AppDomain.UnhandledException)
5. 严重度映射日志级别: Information->LogLevel.Information, Warning->LogLevel.Warning, Error->LogLevel.Error, Critical->LogLevel.Critical

> **[已修订 2026-02-21]** 错误消息文案要求简化，PRD 不再硬性规定具体文案内容，允许实现自行调整措辞。原因: 文案属过度规范，具体措辞由实现决定。参考: ERR-07。[实现状态] 代码实现已接受 (Sprint3)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 不适用 (客户端功能) |
| 本地 | DesktopExceptionHandler 根据异常类型自动确定严重度 |

### US-ERR-006: 客户端错误消息映射体系

> As a 医生, I want to 所有错误消息都是中文且与具体操作相关,
> so that 我能立即理解出了什么问题，而不是看到 "Error 10101" 这样的技术编号。

**Acceptance Criteria:**
- [ ] 服务端返回 errorCode=10004 -> 用户看到"密码不正确"
- [ ] 服务端返回 HTTP 401 (无 errorCode) -> 用户看到"登录已过期，请重新登录"
- [ ] 未知错误码 -> 用户看到"操作失败，请稍后重试"

**Business Rules:**
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

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 解析服务端返回的 ProblemDetails 中的 errorCode |
| 本地 | 解析本地操作产生的异常类型 |

### US-ERR-007: 错误追踪码

> As a 管理员, I want to 系统错误消息包含短追踪码,
> so that 用户反馈问题时我可以通过追踪码快速定位具体异常。

**Acceptance Criteria:**
- [ ] 系统错误消息 -> 包含追踪码
- [ ] 业务错误消息 (如"密码不正确") -> 不包含追踪码
- [ ] 日志中可通过追踪码检索到对应异常详情

**Business Rules:**
1. 追踪码格式: 8 位短码 (时间戳+随机数)
2. 仅在系统错误级别 (Error/Critical) 附加追踪码，业务错误不附加
3. 追踪码展示格式: "如需帮助，请提供追踪码: XXXXXXXX"
4. 追踪码同时记录到日志中，支持通过追踪码定位具体异常

#### 追踪码展示示例

```
服务器内部错误，请联系管理员。

如需帮助，请提供追踪码: A3F8B2C1
```

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 同时记录到服务端日志 (通过 CorrelationId 关联) |
| 本地 | 记录到本地日志文件 |

### US-ERR-008: 异常通知类型映射

> As a 医生, I want to 不同严重程度的错误以不同方式展示 (轻量 Toast vs 阻塞对话框),
> so that 业务错误不打断我的操作流程，而系统错误能引起我的注意。

**Acceptance Criteria:**
- [ ] BusinessException -> Toast 红色通知，不自动消失
- [ ] HttpRequestException -> Toast 黄色警告，5 秒后消失
- [ ] UnauthorizedException -> 对话框，手动关闭
- [ ] 系统错误 -> 对话框 + 追踪码

**Business Rules:**
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

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | API 调用异常的 Desktop 端展示 |
| 本地 | 本地操作异常的 Desktop 端展示 |

---

## 8. Out of Scope

| 排除项 | 原因 |
|--------|------|
| 异常自动恢复/自愈机制 | 复杂度高，v1.0 仅做异常捕获和展示 |
| 分布式追踪 (OpenTelemetry) | 单体部署场景不需要，后续版本微服务化时考虑 |
| 异常统计仪表盘 | 非当前优先级，运维阶段按需增加 |
| 用户自定义错误消息 | 诊所场景不需要，固定中文消息即可 |
| 异常通知推送 (邮件/短信) | 小型诊所规模不需要实时告警 |

---

## 9. Dependencies & Risks

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| 错误码映射遗漏 | 用户看到通用兜底消息而非具体错误描述 | ClientErrorMessageMapper 覆盖 7 模块 90+ 场景，兜底消息 "操作失败，请稍后重试" 可接受 |
| 生产环境 stackTrace 泄露 | 暴露内部实现细节，增加攻击面 | IExceptionHandler 环境感知，仅 Development 环境返回 stackTrace |
| 异常处理器链顺序错误 | BusinessException 被 SystemExceptionHandler 错误处理，返回 500 | 架构测试保证注册顺序: BusinessExceptionHandler 优先 |
| Toast 组件未实现 | US-ERR-008 依赖 ui-patterns.md 3.3 节 Toast 组件 | 当前使用 MessageBox 降级展示，Toast 实现后切换 |
| Service 层异常迁移不完整 | 47 处 InvalidOperationException 部分遗漏 | 决策 #8 (A1 统一异常体系重构) 分批迁移，代码扫描验证 |

---

## 10. Open Questions

| ID | 问题 | 状态 |
|----|------|------|
| OQ-ERR-01 | Toast 组件何时实现? 当前 MessageBox 降级方案是否可接受? | 延期。当前使用 MessageBox，Toast 组件实现后迁移 |
| OQ-ERR-02 | 追踪码是否需要持久化存储以支持历史查询? | 延期。v1.0 仅记录到日志文件，通过日志检索 |
| OQ-ERR-03 | 批量操作 (如药材导入) 的错误聚合展示方式? | 待定。当前 Result<T> 保留用于批量操作，展示方式待 UI 设计 |
| OQ-ERR-04 | 离线模式下异常日志是否需要在恢复网络后同步到服务端? | 延期。v1.0 本地日志不同步，后续版本考虑 |

---

## Data Model

### 异常类型继承体系

```
Exception
  └- AppException (ErrorCode, TypedErrorCode, UserMessage, ShowDetailToUser)
       ├- BusinessException (BusinessRule) -> HTTP 400
       ├- NotFoundException (ResourceType, ResourceId) -> HTTP 404
       ├- ConflictException (EntityType, EntityId, CurrentVersion, ExpectedVersion) -> HTTP 409
       ├- ValidationException (Errors, FieldName) -> HTTP 400
       ├- UnauthorizedException (AuthenticationScheme, FailureReason) -> HTTP 401
       └- ApiException (StatusCode, ResponseContent, RequestUrl, HttpMethod) -> HTTP varies
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

> **[已修订 2026-02-21]** 错误分类枚举值对齐代码定义，PRD 枚举值以代码实际实现为准。原因: 枚举值细节差异不影响功能，PRD 对齐代码。参考: ERR-08。[实现状态] 代码实现已接受 (Sprint3)

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

> **[已修订 2026-02-21]** 错误日志格式要求放宽，日志输出格式允许与 PRD 描述存在差异。原因: 现有日志格式可接受，PRD 放宽格式要求。参考: ERR-09。[实现状态] 代码实现已接受 (Sprint3)

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

## Error Codes

> 错误码采用 MCCEE 5 位数编号体系，按模块分组。详细错误码映射见 US-ERR-006 业务错误码映射表。

| 模块 | 错误码范围 | 示例 |
|------|-----------|------|
| 用户/认证 | 1xxxx | InvalidCredentials(10101), TokenExpired(10201), UnauthorizedAccess(10300) |
| 患者管理 | 2xxxx | PatientNotFound(20001), DuplicateIdCard(20101) |
| 医案管理 | 3xxxx | MedicalCaseNotFound(30101), InvalidStatusTransition(30301) |
| 处方管理 | 4xxxx | (预留，当前归入医案 304xx) |
| 药材管理 | 5xxxx | HerbNotFound(50101), HerbImportFailed(50301) |
| 验方管理 | 6xxxx | FormulaNotFound(60101), FormulaHerbValidation(60201) |
| 数据同步 | 7xxxx | SyncEntityType(70101), SyncUploadFailed(70201) |

---

## Decision Log

| # | 决策 | 结论 | 日期 |
|---|------|------|------|
| 1 | 异常处理器链式设计 | BusinessExceptionHandler 优先处理 AppException，SystemExceptionHandler 兜底处理所有异常 | 2026-02-11 |
| 2 | 生产环境信息隐藏 | 生产环境 ProblemDetails 不包含 stackTrace 和 exceptionType | 2026-02-11 |
| 3 | 客户端 ServiceResult 模式 | SafeExecuteAsync 包裹异步操作，异常自动转为 Failure 结果 | 2026-02-11 |
| 4 | ExceptionFactory 静态工厂 | 提供按业务实体分组的便捷工厂方法，统一异常创建方式 | 2026-02-11 |
| 5 | 异常展示遵循 UI 规范 | 遵循 ui-patterns.md 3.3 节: 业务错误用 Toast，系统错误用对话框。代码需从统一 MessageBox 重构为分层展示 | 2026-02-17 |
| 6 | 错误消息映射体系 | ClientErrorMessageMapper 覆盖 HTTP 状态码 + 40+ 业务错误码到中文消息，纳入 v1.0 | 2026-02-17 |
| 7 | 追踪码纳入 v1.0 | 系统错误附加 8 位短追踪码，同步记录到日志，方便用户反馈定位。业务错误不附加 | 2026-02-17 |
| 8 | **A1: 统一异常体系重构** | Service 层全面采用 throw BusinessException/NotFoundException，消除 InvalidOperationException (47处) 和 Result.Failure("硬编码") (11处)。MedicalCase CQRS Service 直接 throw 域异常 -> ExceptionHandler 统一映射 HTTP 4xx。BaseService 模块方法签名从 Task\<Result\<T\>\> 改为 Task\<T\>。新增 ~8 个 ErrorCode (PrescriptionAlreadyPrinted/PrescriptionRequired/InvalidStatusTransition/CompletedCaseImmutable/HerbAlreadyVerified 等)。Result\<T\> 仅保留用于批量操作和验证聚合。Desktop LocalData 9处保持 InvalidOperationException (SYNC-D02 后整体删除) | 2026-02-22 |

### 修订历史

| 日期 | 修订项 | 原因 | 参考 |
|------|--------|------|------|
| 2026-02-21 | 错误消息文案要求简化 | PRD 不再硬性规定具体文案内容，允许实现自行调整措辞 | ERR-07 |
| 2026-02-21 | 错误分类枚举值对齐代码 | 枚举值细节差异不影响功能，PRD 对齐代码 | ERR-08 |
| 2026-02-21 | 错误日志格式要求放宽 | 现有日志格式可接受，PRD 放宽格式要求 | ERR-09 |

---

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-11 | v1.0 | 初始版本，从代码实现逆向工程 |
| 2026-02-17 | v2.0 | R8 深化: 新增 FR-ERR-006 (错误消息映射体系)、FR-ERR-007 (追踪码)、FR-ERR-008 (异常通知类型映射)、3 条新决策 |
| 2026-02-17 | v2.1 | PRD审查修复: D2-errorCode示例格式对齐5位数体系(AUTH-101->ERR-10101) |
| 2026-02-18 | v2.2 | 错误码全量分配: 范围表更新为5位MCCEE体系，新增同步模块7xxxx，处方模块4xxxx标注预留 |
| 2026-02-21 | v2.3 | PRD vs Code 偏差分析修订: 3 项修订, 0 项延期标注 |
| 2026-02-22 | v2.4 | A1 统一异常体系重构决策: 新增决策 #8 |
| 2026-03-06 | v3.0 | PRD 全面重写: FR->US 格式迁移，新增 Problem Statement/Strategic Context/Success Metrics/Epic Hypothesis/Out of Scope/Dependencies & Risks/Open Questions 7 个章节 |
