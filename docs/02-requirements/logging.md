# 日志与审计体系 需求规格

## 概述

系统采用 Serilog 结构化日志框架，支持 CorrelationId 端到端请求追踪、敏感数据自动脱敏、运行时日志级别动态调整。安全审计日志独立记录所有认证事件，系统日志持久化到 SQL Server 供运维查询。

---

## 用户角色

| 角色 | 在本模块中的交互 |
|------|-----------------|
| SuperAdmin | 查看系统日志 + 管理日志级别 (通过 DiagnosticsController) |
| 所有角色 | 操作行为被审计日志自动记录 |

> 日志系统是基础设施层，对业务用户透明。日志级别管理见 [health-diagnostics.md](health-diagnostics.md) FR-SYS-004~007。

---

## 功能清单

> **[已修订 2026-02-21]** 结构化日志字段命名要求简化，PRD 不再硬性规定字段名称，由 Serilog Enricher 实现决定
> 原因: 字段命名属过度规范，实际命名由框架和 Enricher 约定  |  参考: LOG-08
> [实现状态] 代码实现已接受 (Sprint3)

### FR-LOG-001: 结构化日志

- **描述**: 基于 Serilog 的结构化日志系统，自动注入 CorrelationId、MachineName、ThreadId 等上下文属性
- **业务规则**:
  1. 使用 Serilog 作为日志框架
  2. 日志属性自动注入: CorrelationId, MachineName, ThreadId
  3. CorrelationId 注入机制:
     - Server端: 从 HttpContext.Request.Headers["X-Correlation-Id"] 获取，回退到 TraceIdentifier
     - Desktop端: 从 AsyncLocal<string> 获取 (AsyncLocalCorrelationIdProvider)
     - 通过 CorrelationIdEnricher 自动富集每条日志
  4. 日志输出: Console + File (按天滚动) + SQL Server (SystemLog 表)
  5. 默认级别: Information，可通过 DiagnosticsController 动态调整
- **远程模式**: Server 端日志写入 Console + File + SQL Server
- **本地模式**: Desktop 端日志写入 Console + File
- **验收标准**:
  - [ ] 每条日志包含 CorrelationId 属性
  - [ ] Server 端请求日志的 CorrelationId 与请求头一致
  - [ ] Desktop 端日志包含 AsyncLocal 注入的 CorrelationId
  - [ ] 日志文件按天自动滚动

### FR-LOG-002: 安全审计日志

- **描述**: 独立记录所有认证相关安全事件，持久化到 SecurityAuditLogs 表
- **业务规则**:
  1. 事件类型: Login, Logout, RefreshToken, TokenRevoked, LoginFailed, PasswordChange, UserDisabled 等
  2. 每条审计记录包含: EventType, UserId, UserType, UserName, IpAddress, UserAgent, Success, ErrorMessage, Metadata (JSON), CreatedAt
  3. 审计日志不可修改和删除 (仅追加)
  4. UserId 为可选字段 (如 LoginFailed 可能无已知用户)
- **远程模式**: 写入 SQL Server SecurityAuditLogs 表
- **本地模式**: 不适用 (本地模式无完整认证流程)
- **验收标准**:
  - [ ] 登录成功 -> 写入 EventType="Login", Success=true
  - [ ] 登录失败 -> 写入 EventType="LoginFailed", Success=false, ErrorMessage 不为空
  - [ ] Token 刷新 -> 写入 EventType="RefreshToken"
  - [ ] 审计记录包含 IpAddress 和 UserAgent

### FR-LOG-003: 敏感数据脱敏

- **描述**: 通过 SensitiveDataAttribute 标记和 SensitiveDataMasker 自动脱敏，防止敏感信息泄露到日志
- **业务规则**:
  1. 属性级脱敏: SensitiveDataAttribute 标记 Property，日志输出时自动脱敏
  2. 敏感数据类型: PersonalInfo (个人信息), MedicalInfo (医疗信息), ContactInfo (联系信息), IdentityInfo (身份信息), FinancialInfo (财务信息)
  3. 脱敏模式:
     - Default: 中间用*替代 (如 "abc***xyz")
     - Partial: 按类型智能脱敏 (手机号: 138\*\*\*\*1234; 身份证: 110\*\*\*\*\*\*\*1234)
     - Full: 完全隐藏 -> "[已隐藏]"
     - Hash: SHA256 短哈希 -> "[REDACTED:A1B2C3D4]"
  4. 文本级脱敏: 自动检测并脱敏文本中的密码、Token、连接字符串、Bearer Token
  5. URI 脱敏: 查询参数中的 password/token/key/secret 自动替换为 ***
  6. 敏感字段名列表: Password, Token, AccessToken, RefreshToken, Secret, ConnectionString, CreditCard 等 30+ 字段名
  7. SensitiveDataDestructuringPolicy: Serilog 日志析构时自动触发属性级脱敏
- **远程模式**: Server 端日志自动脱敏
- **本地模式**: Desktop 端日志自动脱敏
- **验收标准**:
  - [ ] 标记 [SensitiveData(ContactInfo)] 的手机号 -> 日志中显示 138\*\*\*\*1234
  - [ ] 标记 [SensitiveData(IdentityInfo)] 的身份证号 -> 日志中显示 110\*\*\*\*\*\*\*1234
  - [ ] MaskingMode.Full -> "[已隐藏]"
  - [ ] 文本中的 "password=abc123" -> "password=[REDACTED]"
  - [ ] Bearer Token -> "Bearer [REDACTED]"
  - [ ] URI 参数 "?token=xyz" -> "?token=***"

> **[已修订 2026-02-21]** 日志级别配置要求放宽，运行时可通过 DiagnosticsController 动态调整，具体默认级别允许实现偏差
> 原因: 运行时可调整，不需要 PRD 硬性规定  |  参考: LOG-05
> [实现状态] 代码实现已接受 (Sprint3)

### FR-LOG-004: 运行时日志级别管理

- **描述**: LoggingLevelManager 支持运行时动态调整日志级别，配合 DiagnosticsController 提供 API 管理入口
- **业务规则**:
  1. LoggingLevelManager 持有全局 LoggingLevelSwitch 单例
  2. 默认级别: Information
  3. 调试模式: 临时降低级别 + Timer 自动恢复 (详见 FR-SYS-005/006)
  4. 手动设置: SetLevel 直接修改级别，无自动过期 (详见 FR-SYS-007)
  5. 线程安全: 所有操作使用 lock 保护
  6. 实现 IDisposable: 释放时清理 Timer
- **远程模式**: 通过 DiagnosticsController API 管理
- **本地模式**: Desktop 端可直接使用 LoggingLevelManager
- **验收标准**:
  - [ ] EnableDebugMode -> LevelSwitch.MinimumLevel 降低到指定级别
  - [ ] DisableDebugMode -> 恢复到 DefaultLevel
  - [ ] Timer 到期 -> 自动调用 DisableDebugMode
  - [ ] 并发调用 -> 线程安全，无数据竞争

### FR-LOG-005: 系统日志后台清理

- **描述**: LogCleanupService 定期清理过期的系统日志，Error/Fatal 级别日志永久保留
- **业务规则**:
  1. 后台服务 (BackgroundService)，应用启动后延迟 5 分钟开始首次执行
  2. 执行周期: 每 24 小时 (可配置)
  3. 默认保留天数: 90 天 (可配置，对应 NFR-SEC-005)
  4. **Error/Fatal 级别日志永久保留**，仅清理 Warning 及以下级别
  5. 分批删除: 每批 1000 条，批间延迟 100ms，避免锁表
  6. 使用原生 SQL 执行: `DELETE TOP (@batchSize) FROM SystemLogs WHERE ...`
  7. 清理失败不影响应用运行 (异常隔离)
  8. 可通过配置节 `Lybt:Logging:Cleanup` 完全禁用
- **远程模式**: Server 端自动运行
- **本地模式**: 不适用 (Desktop 端日志存储为本地文件，由文件滚动策略管理)
- **验收标准**:
  - [ ] 启动延迟 5 分钟后首次执行清理
  - [ ] 90 天前的 Information/Warning 日志被删除
  - [ ] 90 天前的 Error/Fatal 日志保留
  - [ ] 清理过程中数据库仍可正常读写

### FR-LOG-006: 安全审计日志后台清理

- **描述**: SecurityAuditCleanupService 定期清理过期的安全审计日志
- **业务规则**:
  1. 后台服务 (BackgroundService)，每日凌晨 3:00 执行
  2. 默认保留天数: **365 天** (可配置，对应 NFR-D04 / NFR-SEC-005)
  3. 配置节: `Lybt:SecurityAudit:Cleanup`
  4. 分批删除: 每批 1000 条，避免大事务锁表
  5. 清理失败不影响应用运行 (异常隔离)
  6. 执行日志: 记录清理条数和截止日期
- **远程模式**: Server 端自动运行
- **本地模式**: 不适用 (本地模式无安全审计日志)
- **验收标准**:
  - [ ] 凌晨 3:00 自动触发清理
  - [ ] 365 天前的审计日志被删除
  - [ ] 365 天内的审计日志完整保留
  - [ ] 清理过程中审计日志仍可正常写入

> **注意**: 当前代码硬编码 30 天，需修改为可配置且默认 365 天以匹配 NFR-SEC-005。

> **[已修订 2026-02-21]** 日志格式模板要求简化，PRD 不再硬性规定日志输出格式模板，由实现自行决定
> 原因: 日志格式模板属过度规范，实际格式由 Serilog 配置决定  |  参考: LOG-06
> [实现状态] 代码实现已接受 (Sprint3)

### FR-LOG-007: API 请求自动日志

- **描述**: ApiLoggingFilter 自动记录所有 Controller Action 的执行信息，包含参数脱敏和耗时统计
- **业务规则**:
  1. 实现为 IAsyncActionFilter，全局注册
  2. Action 开始: 记录 Information 级别日志 `[API] >>> {Action} started. CorrelationId={CorrelationId}`
  3. Action 完成: 记录 Information 级别日志 `[API] <<< {Action} completed in {Duration}ms`
  4. Action 异常: 记录 Error 级别日志 `[API] !!! {Action} failed after {Duration}ms`
  5. 参数记录 (Debug 级别):
     - 敏感字段名自动检测 (SensitiveDataMasker.IsSensitiveFieldName)
     - 复杂对象仅显示类型名 `[{TypeName}]`
     - 字符串值截断至 100 字符
  6. 自动注入 CorrelationId 到日志上下文
- **远程模式**: Server 端全局启用
- **本地模式**: 不适用 (Desktop 端无 Controller)
- **验收标准**:
  - [ ] 每个 API 请求生成 started + completed/failed 日志对
  - [ ] completed 日志包含准确的耗时毫秒数
  - [ ] 包含 password/token 的参数被脱敏
  - [ ] CorrelationId 与请求中间件注入的值一致

---

### 审计体系交叉引用

| 审计类型 | 归属文档 | FR 编号 | 说明 |
|----------|---------|---------|------|
| 安全审计日志 (SecurityAuditLog) | logging.md | FR-LOG-002 | 认证事件审计 |
| 医案变更审计 (MedicalCaseAuditLog) | [medical-cases.md](medical-cases.md) | FR-MC-012 | 医案字段级变更审计 |
| API 请求日志 (ApiLoggingFilter) | logging.md | FR-LOG-007 | Controller Action 执行日志 |

---

## 数据模型

### SecurityAuditLog

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | Guid | PK | 主键 |
| EventType | string | Required, MaxLength(50) | 事件类型 |
| UserId | Guid? | Optional | 用户ID |
| UserType | string? | MaxLength(50) | 用户类型 (User/SuperAdmin) |
| UserName | string? | MaxLength(256) | 用户名称 |
| IpAddress | string? | MaxLength(50) | 客户端IP |
| UserAgent | string? | MaxLength(500) | 客户端UA |
| Success | bool | Required | 操作是否成功 |
| ErrorMessage | string? | MaxLength(500) | 错误消息 |
| Metadata | string? | | 扩展元数据 (JSON) |
| CreatedAt | DateTime | Default=UtcNow | 创建时间 |

### SystemLog

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | int | PK, AutoIncrement | 主键 |
| Timestamp | DateTime | Required | 日志时间戳 |
| Level | string | MaxLength(50) | 日志级别 |
| Message | string | Required | 日志消息 |
| Exception | string? | | 异常信息 |
| LoggerName | string? | MaxLength(255) | 日志来源 |
| UserId | Guid? | | 用户ID |
| RequestId | string? | MaxLength(36) | 请求ID |
| CorrelationId | string? | MaxLength(36) | 关联ID |
| MachineName | string? | MaxLength(100) | 机器名 |
| ThreadId | int? | | 线程ID |
| Properties | string? | | 扩展属性 (JSON) |

### SensitiveDataType 枚举

| 值 | 名称 | 说明 |
|----|------|------|
| PersonalInfo | 个人信息 | 姓名等 |
| MedicalInfo | 医疗信息 | 病史、诊断等 |
| ContactInfo | 联系信息 | 手机号 |
| IdentityInfo | 身份信息 | 身份证号 |
| FinancialInfo | 财务信息 | 费用等 |

### MaskingMode 枚举

| 值 | 名称 | 说明 |
|----|------|------|
| Default | 默认脱敏 | 中间用*替代 |
| Partial | 部分隐藏 | 按数据类型智能处理 |
| Full | 完全隐藏 | 显示 "[已隐藏]" |
| Hash | 哈希脱敏 | SHA256 短哈希标识 |

---

## 配置参数

> **[已修订 2026-02-21]** 日志轮转配置要求放宽，保留天数、文件大小等参数允许实现偏差
> 原因: 不影响功能，运行时可通过配置文件调整  |  参考: LOG-07
> [实现状态] 代码实现已接受 (Sprint3)

### Server 端

| 参数 | 说明 | 默认值 |
|------|------|--------|
| Serilog:MinimumLevel:Default | 默认日志级别 | Information |
| Serilog:MinimumLevel:Override:Microsoft | Microsoft 命名空间级别 | Warning |
| Serilog:WriteTo:Console | 控制台输出 | 启用 |
| Serilog:WriteTo:File | 文件输出 (bootstrap) | `logs/bootstrap-.log`，按天滚动，保留 7 天 |
| Serilog:WriteTo:MSSqlServer | SQL Server 输出 | SystemLogs 表 |
| Lybt:Logging:Cleanup:Enabled | 系统日志清理开关 | true |
| Lybt:Logging:Cleanup:RetentionDays | 系统日志保留天数 | 90 |
| Lybt:Logging:Cleanup:CleanupIntervalHours | 清理执行间隔 | 24 |
| Lybt:Logging:Cleanup:InitialDelayMinutes | 启动后延迟执行 | 5 |
| Lybt:Logging:Cleanup:BatchSize | 每批删除条数 | 1000 |
| Lybt:SecurityAudit:Cleanup:RetentionDays | 安全审计日志保留天数 | 365 |

### Desktop 端

| 参数 | 说明 | 默认值 |
|------|------|--------|
| 日志路径 | 本地日志存储目录 | `%LOCALAPPDATA%/LYBTZYZS/logs/` |
| 文件名模式 | 日志文件命名 | `lybt-desktop-{Date}.log` |
| 滚动策略 | 文件切分方式 | 按天滚动 (RollingInterval.Day) |
| 保留文件数 | 最多保留日志文件数量 | 30 个 |
| 单文件大小限制 | 单个日志文件最大尺寸 | 10 MB |
| MinimumLevel:Override:Microsoft | Microsoft 命名空间级别 | Warning |
| MinimumLevel:Override:System | System 命名空间级别 | Warning |
| CorrelationId 提供者 | 关联 ID 来源 | AsyncLocalCorrelationIdProvider |
| 脱敏策略 | 敏感数据析构 | SensitiveDataDestructuringPolicy (同 Server 端) |

---

## 决策记录

| # | 决策 | 结论 | 日期 |
|---|------|------|------|
| 1 | CorrelationId 双端统一 | Server 端从 HttpContext 获取，Desktop 端从 AsyncLocal 获取，Enricher 统一注入 | 2026-02-11 |
| 2 | 脱敏策略 | 属性级 (SensitiveDataAttribute) + 文本级 (正则匹配) 双重保障 | 2026-02-11 |
| 3 | 审计日志独立存储 | SecurityAuditLog 独立表，不与 SystemLog 混合 | 2026-02-11 |
| 4 | 安全审计保留 365 天 | 代码原硬编码 30 天，需改为可配置且默认 365 天 (对齐 NFR-D04)。医疗行业常见合规要求 | 2026-02-17 |
| 5 | 系统日志 Error/Fatal 永久保留 | LogCleanupService 仅清理 Warning 及以下级别，Error/Fatal 永久保留供事后分析 | 2026-02-17 |
| 6 | 异常告警 v2.0 范围 | v1.0 仅保证 Error/Fatal 日志永久保留供查询，不实现主动告警 (邮件/Webhook/桌面通知) | 2026-02-17 |
| 7 | 医案审计归属 medical-cases.md | MedicalCaseAuditLog 归属 FR-MC-012 (业务审计)，logging.md 仅交叉引用 | 2026-02-17 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-11 | v1.0 | 初始版本，从代码实现逆向工程 |
| 2026-02-17 | v2.0 | Round 7 深化: 新增 FR-LOG-005~007 (后台清理+API日志)、Desktop 配置参数表、审计体系交叉引用、4 条新决策 |
| 2026-02-21 | v2.1 | PRD vs Code 偏差分析修订: 4 项修订, 0 项延期标注 |
