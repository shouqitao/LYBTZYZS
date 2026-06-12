# 日志与审计 产品需求文档

---

## 1. Problem Statement

### 1.1 问题描述

中医诊所管理系统处理大量患者敏感数据 (诊断记录、处方信息、个人身份信息)，系统运行过程中产生海量操作日志和安全事件。缺乏结构化日志体系意味着：故障排查依赖人工翻阅散乱日志，安全事件无法追溯，敏感数据可能泄露到日志文件，过期日志无人清理占满磁盘。

### 1.2 用户痛点

| 角色 | 痛点 | 影响 |
|------|------|------|
| 运维人员 | 故障发生后无法快速定位问题根因，日志缺乏请求关联 | 故障恢复时间长，影响诊疗连续性 |
| 安全管理者 | 无法追溯谁在什么时间执行了什么操作 | 安全事件无法审计，合规风险 |
| 运维人员 | 日志中可能包含患者手机号、身份证号等敏感信息 | 日志泄露等同于患者隐私泄露 |
| 运维人员 | 日志文件持续增长无自动清理 | 磁盘空间耗尽导致系统不可用 |

### 1.3 证据

- 医疗行业合规要求: 安全审计日志至少保留 1 年
- 运维实践: 结构化日志 + CorrelationId 是微服务/分布式系统的标准做法
- 数据安全: 医疗数据脱敏是 HIPAA/等保等安全框架的基本要求

---

## 2. Target Users

| 角色 | 在本模块中的操作权限 |
|------|---------------------|
| SuperAdmin | 查看系统日志 + 管理日志级别 (通过 DiagnosticsController) |
| 所有角色 | 操作行为被审计日志自动记录 (被动参与) |

> 日志系统是基础设施层，对业务用户透明。日志级别管理见 [health-diagnostics.md](15-health-diagnostics.md) US-SYS-004~007。

---

## 3. Strategic Context

### 3.1 业务目标

| 目标 | 对应 |
|------|------|
| 故障快速定位 | 结构化日志 + CorrelationId 端到端追踪，缩短故障恢复时间 |
| 安全合规 | 安全审计日志独立存储，满足医疗行业审计要求 |
| 数据安全 | 敏感数据自动脱敏，防止日志泄露导致患者隐私暴露 |
| 系统稳定 | 自动清理过期日志，防止磁盘空间耗尽 |

### 3.2 Why Now

系统进入正式版开发阶段，认证体系 (auth.md) 已实现完整的安全事件流。日志与审计是安全事件可追溯的基础设施保障，也是医疗行业合规的前提条件。缺少结构化日志，认证模块产生的安全事件无处落地。

---

## 4. Solution Overview

日志与审计模块采用 Serilog 结构化日志框架，提供完整的日志生命周期管理:

**核心能力:**
- **结构化日志**: Serilog 框架，自动注入 CorrelationId、MachineName、ThreadId 等上下文属性
- **安全审计**: 独立记录所有认证相关安全事件，持久化到 SecurityAuditLogs 表
- **敏感数据脱敏**: SensitiveDataAttribute 标记 + SensitiveDataMasker 自动脱敏，双重保障
- **运行时级别管理**: LoggingLevelManager 动态调整日志级别，支持调试模式自动恢复
- **自动清理**: 后台服务定期清理过期日志 (系统日志 90 天 / 审计日志 365 天)
- **API 请求日志**: ApiLoggingFilter 自动记录 Controller Action 执行信息

**日志流向:**
```
Server 端:
  Controller Action → ApiLoggingFilter → Serilog → Console + File + SQL Server (SystemLog)
  认证事件 → SecurityAuditService → SecurityAuditLogs 表

Desktop 端:
  ViewModel/Service → Serilog → Console + File (%LOCALAPPDATA%/LYBTZYZS/logs/)
  CorrelationId → AsyncLocalCorrelationIdProvider → CorrelationIdEnricher → 每条日志
```

---

## 5. Success Metrics

| 指标 | 当前 | v1.0 目标 | 衡量方式 |
|------|------|----------|---------|
| 日志 CorrelationId 覆盖率 | 0% | 100% Server 端请求日志含 CorrelationId | 日志采样检查 |
| 敏感数据脱敏率 | 0% | 100% 标记字段脱敏 | 日志审计抽检 |
| 安全事件记录完整性 | 0% | 100% 认证事件有审计记录 | SecurityAuditLog 与登录日志交叉比对 |
| 磁盘空间告警 | 无自动清理 | 零磁盘空间耗尽事件 | 运维监控 |
| 故障定位耗时 | 人工翻阅 (30min+) | < 5 分钟 (CorrelationId 追踪) | 运维反馈 |

---

## 6. Epic Hypothesis

We believe that 实现 Serilog 结构化日志 + CorrelationId 追踪 + 敏感数据脱敏 + 安全审计日志 + 自动清理 for 运维人员和安全管理者 will achieve 故障快速定位、安全事件可追溯、患者隐私保护、系统长期稳定运行。We'll know we're right when CorrelationId 覆盖率 100%、敏感数据零泄露到日志、安全审计日志完整覆盖所有认证事件。

---

## 7. User Stories

### 优先级汇总

| US 编号 | 名称 | Priority |
|---------|------|----------|
| US-LOG-001 | 结构化日志 | Should |
| US-LOG-002 | 安全审计日志 | Should |
| US-LOG-003 | 敏感数据脱敏 | Should |
| US-LOG-004 | 运行时日志级别管理 | Could |
| US-LOG-005 | 系统日志后台清理 | Could |
| US-LOG-006 | 安全审计日志后台清理 | Could |
| US-LOG-007 | API 请求自动日志 | Should |

---

### US-LOG-001: 结构化日志

> As a 运维人员, I want to 系统自动生成包含 CorrelationId 等上下文属性的结构化日志,
> so that 我可以通过 CorrelationId 快速追踪一个请求在系统中的完整链路。

**Acceptance Criteria:**
- [ ] 每条日志包含 CorrelationId 属性
- [ ] Server 端请求日志的 CorrelationId 与请求头 X-Correlation-Id 一致
- [ ] Desktop 端日志包含 AsyncLocal 注入的 CorrelationId
- [ ] 日志文件按天自动滚动

**Business Rules:**
1. 使用 Serilog 作为日志框架
2. 日志属性自动注入: CorrelationId, MachineName, ThreadId
3. CorrelationId 注入机制:
   - Server 端: 从 HttpContext.Request.Headers["X-Correlation-Id"] 获取，回退到 TraceIdentifier
   - Desktop 端: 从 AsyncLocal<string> 获取 (AsyncLocalCorrelationIdProvider)
   - 通过 CorrelationIdEnricher 自动富集每条日志
4. 日志输出: Console + File (按天滚动) + SQL Server (SystemLog 表)
5. 默认级别: Information，可通过 DiagnosticsController 动态调整

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | Server 端日志写入 Console + File + SQL Server |
| 本地 | Desktop 端日志写入 Console + File |

### US-LOG-002: 安全审计日志

> As a 安全管理者, I want to 系统自动记录所有认证相关安全事件到独立的审计日志表,
> so that 我可以追溯谁在什么时间执行了什么安全操作，满足医疗行业合规要求。

**Acceptance Criteria:**
- [ ] 登录成功 → 写入 EventType="Login", Success=true
- [ ] 登录失败 → 写入 EventType="LoginFailed", Success=false, ErrorMessage 不为空
- [ ] Token 刷新 → 写入 EventType="RefreshToken"
- [ ] 审计记录包含 IpAddress 和 UserAgent

**Business Rules:**
1. 事件类型: Login, Logout, RefreshToken, TokenRevoked, LoginFailed, PasswordChange, UserDisabled 等
2. 每条审计记录包含: EventType, UserId, UserType, UserName, IpAddress, UserAgent, Success, ErrorMessage, Metadata (JSON), CreatedAt
3. 审计日志不可修改和删除 (仅追加)
4. UserId 为可选字段 (如 LoginFailed 可能无已知用户)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 写入 SQL Server SecurityAuditLogs 表 |
| 本地 | 不适用 (本地模式无完整认证流程) |

### US-LOG-003: 敏感数据脱敏

> As a 安全管理者, I want to 日志中的敏感数据 (手机号、身份证号、密码、Token) 被自动脱敏,
> so that 即使日志文件泄露也不会暴露患者隐私。

**Acceptance Criteria:**
- [ ] 标记 [SensitiveData(ContactInfo)] 的手机号 → 日志中显示 138\*\*\*\*1234
- [ ] 标记 [SensitiveData(IdentityInfo)] 的身份证号 → 日志中显示 110\*\*\*\*\*\*\*1234
- [ ] MaskingMode.Full → "[已隐藏]"
- [ ] 文本中的 "password=abc123" → "password=[REDACTED]"
- [ ] Bearer Token → "Bearer [REDACTED]"
- [ ] URI 参数 "?token=xyz" → "?token=***"

**Business Rules:**
1. 属性级脱敏: SensitiveDataAttribute 标记 Property，日志输出时自动脱敏
2. 敏感数据类型: PersonalInfo (个人信息), MedicalInfo (医疗信息), ContactInfo (联系信息), IdentityInfo (身份信息), FinancialInfo (财务信息)
3. 脱敏模式:
   - Default: 中间用*替代 (如 "abc***xyz")
   - Partial: 按类型智能脱敏 (手机号: 138\*\*\*\*1234; 身份证: 110\*\*\*\*\*\*\*1234)
   - Full: 完全隐藏 → "[已隐藏]"
   - Hash: SHA256 短哈希 → "[REDACTED:A1B2C3D4]"
4. 文本级脱敏: 自动检测并脱敏文本中的密码、Token、连接字符串、Bearer Token
5. URI 脱敏: 查询参数中的 password/token/key/secret 自动替换为 ***
6. 敏感字段名列表: Password, Token, AccessToken, RefreshToken, Secret, ConnectionString, CreditCard 等 30+ 字段名
7. SensitiveDataDestructuringPolicy: Serilog 日志析构时自动触发属性级脱敏

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | Server 端日志自动脱敏 |
| 本地 | Desktop 端日志自动脱敏 |

### US-LOG-004: 运行时日志级别管理

> As a 运维人员, I want to 在不重启应用的情况下动态调整日志级别,
> so that 我可以在排查问题时临时开启 Debug 日志，排查完成后自动恢复。

**Acceptance Criteria:**
- [ ] EnableDebugMode → LevelSwitch.MinimumLevel 降低到指定级别
- [ ] DisableDebugMode → 恢复到 DefaultLevel
- [ ] Timer 到期 → 自动调用 DisableDebugMode
- [ ] 并发调用 → 线程安全，无数据竞争

**Business Rules:**
1. LoggingLevelManager 持有全局 LoggingLevelSwitch 单例
2. 默认级别: Information
3. 调试模式: 临时降低级别 + Timer 自动恢复 (详见 US-SYS-005/006)
4. 手动设置: SetLevel 直接修改级别，无自动过期 (详见 US-SYS-007)
5. 线程安全: 所有操作使用 lock 保护
6. 实现 IDisposable: 释放时清理 Timer

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 通过 DiagnosticsController API 管理 |
| 本地 | Desktop 端可直接使用 LoggingLevelManager |

### US-LOG-005: 系统日志后台清理

> As a 运维人员, I want to 系统自动清理过期的系统日志且保留 Error/Fatal 级别日志,
> so that 磁盘空间不会被过期日志耗尽，同时严重错误日志永久可查。

**Acceptance Criteria:**
- [ ] 启动延迟 5 分钟后首次执行清理
- [ ] 90 天前的 Information/Warning 日志被删除
- [ ] 90 天前的 Error/Fatal 日志保留
- [ ] 清理过程中数据库仍可正常读写

**Business Rules:**
1. 后台服务 (BackgroundService)，应用启动后延迟 5 分钟开始首次执行
2. 执行周期: 每 24 小时 (可配置)
3. 默认保留天数: 90 天 (可配置，对应 NFR-SEC-005)
4. **Error/Fatal 级别日志永久保留**，仅清理 Warning 及以下级别
5. 分批删除: 每批 1000 条，批间延迟 100ms，避免锁表
6. 使用原生 SQL 执行: `DELETE TOP (@batchSize) FROM SystemLogs WHERE ...`
7. 清理失败不影响应用运行 (异常隔离)
8. 可通过配置节 `Lybt:Logging:Cleanup` 完全禁用

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | Server 端自动运行 |
| 本地 | 不适用 (Desktop 端日志存储为本地文件，由文件滚动策略管理) |

### US-LOG-006: 安全审计日志后台清理

> As a 运维人员, I want to 系统自动清理超过保留期限的安全审计日志,
> so that 在满足 365 天合规保留要求的前提下控制存储增长。

**Acceptance Criteria:**
- [ ] 凌晨 3:00 自动触发清理
- [ ] 365 天前的审计日志被删除
- [ ] 365 天内的审计日志完整保留
- [ ] 清理过程中审计日志仍可正常写入

**Business Rules:**
1. 后台服务 (BackgroundService)，每日凌晨 3:00 执行
2. 默认保留天数: **365 天** (可配置，对应 NFR-D04 / NFR-SEC-005)
3. 配置节: `Lybt:SecurityAudit:Cleanup`
4. 分批删除: 每批 1000 条，避免大事务锁表
5. 清理失败不影响应用运行 (异常隔离)
6. 执行日志: 记录清理条数和截止日期

> **注意**: 当前代码硬编码 30 天，需修改为可配置且默认 365 天以匹配 NFR-SEC-005。

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | Server 端自动运行 |
| 本地 | 不适用 (本地模式无安全审计日志) |

### US-LOG-007: API 请求自动日志

> As a 运维人员, I want to 每个 API 请求自动生成包含耗时和参数的日志,
> so that 我可以监控 API 性能并快速定位慢请求和异常请求。

**Acceptance Criteria:**
- [ ] 每个 API 请求生成 started + completed/failed 日志对
- [ ] completed 日志包含准确的耗时毫秒数
- [ ] 包含 password/token 的参数被脱敏
- [ ] CorrelationId 与请求中间件注入的值一致

**Business Rules:**
1. 实现为 IAsyncActionFilter，全局注册
2. Action 开始: 记录 Information 级别日志 `[API] >>> {Action} started. CorrelationId={CorrelationId}`
3. Action 完成: 记录 Information 级别日志 `[API] <<< {Action} completed in {Duration}ms`
4. Action 异常: 记录 Error 级别日志 `[API] !!! {Action} failed after {Duration}ms`
5. 参数记录 (Debug 级别):
   - 敏感字段名自动检测 (SensitiveDataMasker.IsSensitiveFieldName)
   - 复杂对象仅显示类型名 `[{TypeName}]`
   - 字符串值截断至 100 字符
6. 自动注入 CorrelationId 到日志上下文

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | Server 端全局启用 |
| 本地 | 不适用 (Desktop 端无 Controller) |

---

## 8. Out of Scope

| 排除项 | 原因 |
|--------|------|
| 异常主动告警 (邮件/Webhook/桌面通知) | v1.0 仅保证 Error/Fatal 日志永久保留供查询，主动告警延期到 v2.0 (LOG-D06) |
| 日志可视化仪表盘 (ELK/Grafana) | 小型诊所规模不需要，v1.0 通过 SQL 查询 SystemLog 表满足需求 |
| 分布式追踪 (OpenTelemetry) | 单体架构不需要分布式追踪，CorrelationId 已满足当前需求 |
| 日志格式模板硬性规范 | 日志格式由 Serilog 配置决定，PRD 不做硬性规定 |
| 结构化日志字段命名硬性规范 | 字段命名由框架和 Enricher 约定，PRD 不做硬性规定 |

---

## 9. Dependencies & Risks

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| SQL Server 日志表增长过快 | 磁盘空间耗尽，影响业务数据写入 | LogCleanupService 定期清理 + 分批删除避免锁表 |
| 脱敏规则遗漏新增敏感字段 | 新字段未脱敏导致隐私泄露 | SensitiveDataAttribute 显式标记 + 文本级正则兜底 |
| 安全审计日志硬编码 30 天保留期 | 不满足医疗行业 365 天合规要求 | 需修改为可配置且默认 365 天 (已知问题) |
| 日志清理 BackgroundService 崩溃 | 过期日志无法清理 | 异常隔离 + 清理失败不影响主流程 |
| Desktop 端日志文件未清理 | 本地磁盘占满 | 文件滚动策略: 按天滚动 + 保留 30 个文件 + 单文件 10MB 限制 |

### 模块依赖

| 依赖模块 | 依赖方向 | 说明 |
|----------|----------|------|
| auth.md | 日志 ← 认证 | 认证事件触发安全审计日志写入 |
| health-diagnostics.md | 日志 ← 诊断 | DiagnosticsController 管理日志级别 (US-SYS-004~007) |
| medical-cases.md | 交叉引用 | MedicalCaseAuditLog 归属 US-MC-012，本模块仅引用 |

---

## 10. Open Questions

| ID | 问题 | 状态 |
|----|------|------|
| OQ-LOG-01 | 安全审计日志保留期从代码硬编码 30 天修改为可配置 365 天的时间节点? | 已识别，待排期 |
| OQ-LOG-02 | Desktop 端是否需要支持日志上传到 Server 端集中查询? | 延期到 v2.0 评估 |
| OQ-LOG-03 | 日志清理 BackgroundService 是否需要健康检查集成 (如清理失败 N 次后告警)? | 延期到 v2.0，与异常告警体系一并设计 |

---

## 审计体系交叉引用

| 审计类型 | 归属文档 | US 编号 | 说明 |
|----------|---------|---------|------|
| 安全审计日志 (SecurityAuditLog) | logging.md | US-LOG-002 | 认证事件审计 |
| 医案变更审计 (MedicalCaseAuditLog) | [medical-cases.md](07-medical-cases.md) | US-MC-012 | 医案字段级变更审计 |
| API 请求日志 (ApiLoggingFilter) | logging.md | US-LOG-007 | Controller Action 执行日志 |

---

## Data Model

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

## Error Codes

> 日志模块为基础设施层，不定义面向客户端的业务错误码。安全审计相关错误码归入认证模块 (auth.md)。

---

## Configuration

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

## Decision Log

| 编号 | 问题 | 影响范围 | 状态 |
|------|------|----------|------|
| LOG-D01 | CorrelationId 双端统一方案 | US-LOG-001 | 已确定: Server 端从 HttpContext 获取，Desktop 端从 AsyncLocal 获取，Enricher 统一注入 |
| LOG-D02 | 脱敏策略双重保障 | US-LOG-003 | 已确定: 属性级 (SensitiveDataAttribute) + 文本级 (正则匹配) 双重保障 |
| LOG-D03 | 审计日志独立存储 | US-LOG-002 | 已确定: SecurityAuditLog 独立表，不与 SystemLog 混合 |
| LOG-D04 | 安全审计保留 365 天 | US-LOG-006 | 已确定: 代码原硬编码 30 天，需改为可配置且默认 365 天 (对齐 NFR-D04)。医疗行业常见合规要求 |
| LOG-D05 | 系统日志 Error/Fatal 永久保留 | US-LOG-005 | 已确定: LogCleanupService 仅清理 Warning 及以下级别，Error/Fatal 永久保留供事后分析 |
| LOG-D06 | 异常告警延期到 v2.0 | Out of Scope | 已确定: v1.0 仅保证 Error/Fatal 日志永久保留供查询，不实现主动告警 (邮件/Webhook/桌面通知) |
| LOG-D07 | 医案审计归属 medical-cases.md | 交叉引用 | 已确定: MedicalCaseAuditLog 归属 FR-MC-012 (业务审计)，logging.md 仅交叉引用 |

### 修订历史

| 日期 | 修订项 | 原因 | 参考 |
|------|--------|------|------|
| 2026-02-21 | 结构化日志字段命名要求简化 | 字段命名属过度规范，由 Serilog Enricher 实现决定 | LOG-08 |
| 2026-02-21 | 日志级别配置要求放宽 | 运行时可调整，不需要 PRD 硬性规定 | LOG-05 |
| 2026-02-21 | 日志格式模板要求简化 | 日志格式模板属过度规范，由 Serilog 配置决定 | LOG-06 |
| 2026-02-21 | 日志轮转配置要求放宽 | 不影响功能，运行时可通过配置文件调整 | LOG-07 |

---

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-11 | v1.0 | 初始版本，从代码实现逆向工程 |
| 2026-02-17 | v2.0 | Round 7 深化: 新增 FR-LOG-005~007 (后台清理+API日志)、Desktop 配置参数表、审计体系交叉引用、4 条新决策 |
| 2026-02-21 | v2.1 | PRD vs Code 偏差分析修订: 4 项修订, 0 项延期标注 |
| 2026-03-06 | v3.0 | PRD 全面重写: FR→US 格式迁移，新增 Problem Statement/Strategic Context/Success Metrics/Epic Hypothesis/Out of Scope/Dependencies & Risks/Open Questions 7 个章节 |
