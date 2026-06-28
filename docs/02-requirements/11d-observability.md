# Observability (可观测性: 日志与健康诊断)

> 版本: v1.0 | 日期: 2026-06-28 | 状态: ✅ 已完成
> Split from 11-platform.md (2026-06-28)

## 模块概述

可观测性模块合并了 Logging & Audit（US-LOG-001~007）与 Health & Diagnostics（US-SYS-001~009），共 16 US。日志与审计基于 Serilog 提供结构化日志、安全审计、敏感数据脱敏、运行时级别管理与自动清理；健康检查与诊断提供系统可观测性：匿名轻量探活、认证详细检查、SuperAdmin 专属日志级别管理。`LoggingLevelManager` 跨越两域——既管日志级别（LOG-005），又被诊断 API 驱动（SYS-005~009）。

服务端输出到 Console + File + SQL Server（SystemLog 表），Desktop 输出到 Console + File（`%LOCALAPPDATA%/LYBTZYZS/logs/`）。调试模式硬上限 120 分钟，到期自动恢复。

---

## Logging & Audit

> 原 7 US，保留 **7 US**：US-LOG-001~007。

### US-LOG-001: 结构化日志（Serilog）

**角色**: 运维人员
**优先级**: Should
**状态**: ✅ 已实现

**作为** 运维人员，**我想要** 系统生成含 CorrelationId 等上下文属性的结构化日志，**以便** 通过 CorrelationId 快速追踪请求完整链路。

**验收标准**:
- [ ] 每条日志含 CorrelationId 属性
- [ ] 自动注入 MachineName、ThreadId
- [ ] 日志文件按天滚动

**业务规则**:
1. 使用 Serilog 框架。
2. 服务端输出：Console + File（按天滚动）+ SQL Server（SystemLog 表）。
3. Desktop 输出：Console + File（`lybt-desktop-{Date}.log`，保留 30 个，单文件 10MB）。
4. 默认级别 Information，可通过 DiagnosticsController 动态调整。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | Server 写 Console + File + SQL Server |
| 本地 | Desktop 写 Console + File |

**实现参考**: Serilog 配置（`Program.cs`）、`SystemLog` 表

---

### US-LOG-002: 两阶段 Serilog 引导

**角色**: 开发人员
**优先级**: Should
**状态**: ✅ 已实现

**作为** 开发人员，**我想要** 启动阶段用 bootstrap logger 捕获早期错误再切换最终 logger，**以便** 启动失败也有日志可查。

**验收标准**:
- [ ] 启动早期使用 `logs/bootstrap-.log`（按天滚动，保留 7 天）
- [ ] 依赖注入就绪后切换最终 logger
- [ ] WebAPI 与 Desktop 均实现两阶段引导

**业务规则**:
1. 两阶段 bootstrap 在 WebAPI + Desktop 均生效。
2. bootstrap logger 在配置系统就绪前捕获启动错误。
3. 切换后最终 logger 接管全部后续日志。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（Desktop 同样两阶段） |

**实现参考**: `src/Server/Services/LYBT.WebAPI/Program.cs`、`src/Client/Desktop/Shell/App.xaml.cs:41`

---

### US-LOG-003: 敏感数据脱敏

**角色**: 安全管理者
**优先级**: Should
**状态**: ✅ 已实现

**作为** 安全管理者，**我想要** 日志中敏感数据（手机号/身份证号/密码/Token）被自动脱敏，**以便** 即使日志文件泄露也不暴露患者隐私。

**验收标准**:
- [ ] `[SensitiveData(ContactInfo)]` 手机号 → `138****1234`
- [ ] `[SensitiveData(IdentityInfo)]` 身份证号 → `110********1234`
- [ ] `MaskingMode.Full` → "[已隐藏]"
- [ ] 文本中 `password=abc123` → `password=[REDACTED]`
- [ ] Bearer Token → `Bearer [REDACTED]`

**业务规则**:
1. 属性级脱敏：`SensitiveDataAttribute` + `SensitiveDataMasker`。
2. 敏感数据类型：PersonalInfo/MedicalInfo/ContactInfo/IdentityInfo/FinancialInfo。
3. 脱敏模式：Default/Partial/Full/Hash。
4. 文本级脱敏：正则检测密码/Token/连接字符串/Bearer Token。
5. URI 脱敏：查询参数 password/token/key/secret → ***。
6. `SensitiveDataDestructuringPolicy` 在 Serilog 析构时触发。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 同下 |
| 本地 | 完全一致（Desktop 同策略） |

**实现参考**: `SensitiveDataAttribute`、`SensitiveDataMasker`、`SensitiveDataDestructuringPolicy`

---

### US-LOG-004: 审计日志（可配置保留期）

**角色**: 安全管理者
**优先级**: Should
**状态**: ✅ 已实现

**作为** 安全管理者，**我想要** 所有认证相关安全事件记录到独立审计表，**以便** 追溯谁在何时执行了什么安全操作，满足医疗行业合规。

**验收标准**:
- [ ] 登录成功 → EventType="Login", Success=true
- [ ] 登录失败 → EventType="LoginFailed", Success=false, ErrorMessage 非空
- [ ] Token 刷新 → EventType="RefreshToken"
- [ ] 记录含 IpAddress 与 UserAgent
- [ ] 审计日志仅追加，不可修改/删除

**业务规则**:
1. 事件类型：Login/Logout/RefreshToken/TokenRevoked/LoginFailed/PasswordChange/UserDisabled 等。
2. `SecurityAuditLog` 独立表，与 SystemLog 分离。
3. 保留期 `SecurityOptions.AuditRetentionDays`，默认 365 天，范围 30-3650。
4. UserId 可选（LoginFailed 可能无已知用户）。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 写入 SQL Server `SecurityAuditLogs` 表 |
| 本地 | 不适用（本地模式无完整认证流程） |

**实现参考**: `SecurityAuditService`、`SecurityAuditLog` 实体、`SecurityOptions.AuditRetentionDays`

---

### US-LOG-005: 日志级别动态调整

**角色**: 运维人员
**优先级**: Could
**状态**: ✅ 已实现

**作为** 运维人员，**我想要** 不重启应用动态调整日志级别，**以便** 排查问题时临时开启 Debug，完成后自动恢复。

**验收标准**:
- [ ] `EnableDebugMode` → LevelSwitch 降低到指定级别
- [ ] `DisableDebugMode` → 恢复 DefaultLevel
- [ ] Timer 到期 → 自动恢复
- [ ] 并发调用线程安全

**业务规则**:
1. `LoggingLevelManager` 持有全局 `LoggingLevelSwitch` 单例。
2. 默认 Information；调试模式临时降低 + Timer 自动恢复。
3. 手动 `SetLevel` 无自动过期。
4. 所有操作 `lock` 保护，实现 `IDisposable`。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | 通过 `DiagnosticsController` API 管理 |
| 本地 | Desktop 直接使用 `LoggingLevelManager` |

**实现参考**: `LoggingLevelManager`（API 详见下方 Health & Diagnostics US-SYS-005~009）

---

### US-LOG-006: CorrelationId 注入

**角色**: 开发人员
**优先级**: Should
**状态**: ✅ 已实现

**作为** 开发人员，**我想要** CorrelationId 自动注入每条日志与 API 请求日志，**以便** 跨模块跨层关联同一请求的所有日志。

**验收标准**:
- [ ] 每个 API 请求生成 started + completed/failed 日志对
- [ ] completed 日志含准确耗时毫秒数
- [ ] 含 password/token 的参数被脱敏
- [ ] CorrelationId 与中间件注入值一致

**业务规则**:
1. `ApiLoggingFilter`（`IAsyncActionFilter`）全局注册。
2. started：`[API] >>> {Action} started`；completed：`[API] <<< {Action} completed in {Duration}ms`。
3. 参数记录（Debug 级别）：敏感字段自动检测脱敏，复杂对象显示类型名，字符串截断 100 字符。
4. CorrelationId 经 `CorrelationIdEnricher` 自动注入。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | Server 全局启用 |
| 本地 | 不适用（Desktop 无 Controller） |

**实现参考**: `ApiLoggingFilter`、`CorrelationIdEnricher`

---

### US-LOG-007: 日志自动清理（默认 365 天）

**角色**: 运维人员
**优先级**: Could
**状态**: ✅ 已实现

**作为** 运维人员，**我想要** 系统自动清理过期日志且保留 Error/Fatal，**以便** 磁盘不被过期日志耗尽，严重错误永久可查。

**验收标准**:
- [ ] 启动延迟 5 分钟后首次执行
- [ ] 90 天前的 Information/Warning 日志被删除
- [ ] 90 天前的 Error/Fatal 日志保留
- [ ] 365 天前的安全审计日志被删除
- [ ] 清理过程数据库仍可正常读写

**业务规则**:
1. 系统日志清理（`LogCleanupService`）：每 24 小时，默认保留 90 天，仅清理 Warning 及以下，Error/Fatal 永久保留。
2. 分批删除每批 1000 条，批间延迟 100ms，避免锁表。
3. 安全审计清理：每日凌晨 3:00，保留 365 天（可配）。
4. 清理失败异常隔离，不影响主流程；可通过 `Lybt:Logging:Cleanup` 禁用。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | Server 自动运行 |
| 本地 | 不适用（Desktop 日志由文件滚动策略管理） |

**实现参考**: `LogCleanupService`、`SecurityOptions.AuditRetentionDays`、`Lybt:Logging:Cleanup` 配置节

---

## Health & Diagnostics

> 原 9 US，保留 **9 US**：US-SYS-001~009。

### US-SYS-001: 匿名存活探针（/health）

**角色**: 监控系统
**优先级**: Could
**状态**: ✅ 已实现

**作为** 运维/监控系统，**我想要** 通过轻量匿名端点检查服务是否存活，**以便** 实时掌握运行状态并在异常时立即告警。

**验收标准**:
- [ ] 匿名请求 → 200 + `{"status":"Healthy","timestamp":"..."}`
- [ ] 服务运行中始终返回 Healthy
- [ ] 不执行任何数据库或外部依赖检查

**业务规则**:
1. 匿名访问，不需要认证。
2. 返回 status + timestamp(UTC)。
3. 此端点不查 DB，保证最轻量。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | `GET /api/v1/health` |
| 本地 | 不适用（纯客户端无服务端探针） |

**实现参考**: `src/Server/Services/LYBT.WebAPI/Controllers/HealthController.cs:19`

---

### US-SYS-002: Ping 端点（/ping）

**角色**: 负载均衡器
**优先级**: Could
**状态**: ✅ 已实现

**作为** 负载均衡器，**我想要** 通过最轻量端点探测服务可达，**以便** 快速进行流量分发决策。

**验收标准**:
- [ ] 匿名请求 → 200 + `{"message":"pong","timestamp":"..."}`
- [ ] 不执行任何业务逻辑

**业务规则**:
1. 匿名访问。
2. 返回 message("pong") + timestamp(UTC)。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | `GET /api/v1/health/ping` |
| 本地 | 不适用 |

**实现参考**: `HealthController.cs:19`

---

### US-SYS-003: 详细健康检查（/details，含 DB）

**角色**: 管理员
**优先级**: Should
**状态**: ✅ 已实现

**作为** Admin，**我想要** 查看含数据库状态的详细健康报告，**以便** 在用户报障前主动发现数据库连接或迁移问题。

**验收标准**:
- [ ] 未认证 → 401
- [ ] DB 正常 + 无待执行迁移 → 200 + status="Healthy"
- [ ] DB 正常 + 有待执行迁移 → 503 + status="Degraded"
- [ ] DB 连接失败 → 503 + status="Unhealthy"
- [ ] 返回 database.duration 耗时毫秒数

**业务规则**:
1. 需认证（Bearer Token）。
2. 检查 `CanConnectAsync` + 待执行迁移数（InMemory 跳过迁移检查）。
3. 无待执行迁移 → Healthy；有 → Degraded；连接失败 → Unhealthy。
4. 返回数据库检查耗时。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | `GET /api/v1/health/details` |
| 本地 | 不适用 |

**实现参考**: `HealthController.cs:19`、数据库健康检查服务

---

### US-SYS-004: 健康状态 503 返回

**角色**: 监控系统
**优先级**: Should
**状态**: ✅ 已实现

**作为** 监控系统，**我想要** 健康检查在系统 Degraded/Unhealthy 时返回 503，**以便** 上游系统能根据 HTTP 状态码做故障转移决策。

**验收标准**:
- [ ] Healthy → HTTP 200
- [ ] Degraded（有待执行迁移） → HTTP 503
- [ ] Unhealthy（DB 连接失败） → HTTP 503
- [ ] 超时返回 Degraded（而非 Unhealthy）避免不必要的故障转移

**业务规则**:
1. Healthy 返回 200，Degraded/Unhealthy 返回 503。
2. 超时级联防护：超时返回 Degraded，上游可继续路由流量。
3. diagnostics API 对审计日志查询失败返回 Degraded，不抛 500。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | `/health/details` 按状态返回 200/503 |
| 本地 | 不适用 |

**实现参考**: `HealthController.cs:19`

---

### US-SYS-005: 日志级别状态查询

**角色**: 超级管理员
**优先级**: Could
**状态**: ✅ 已实现

**作为** SuperAdmin，**我想要** 查询当前日志级别配置与调试模式状态，**以便** 判断是否需要调整。

**验收标准**:
- [ ] 非 SuperAdmin → 403
- [ ] 默认状态 → `isDebugModeActive=false`、`currentLevel=defaultLevel`
- [ ] 调试模式激活 → 返回 startedAt/expiresAt/remainingMinutes

**业务规则**:
1. 仅 SuperAdmin 可访问。
2. 返回 currentLevel/defaultLevel/isDebugModeActive/debugModeStartedAt/debugModeExpiresAt/remainingMinutes。
3. remainingMinutes 仅调试模式激活时返回。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | `GET /api/v1/diagnostics/logging/status` |
| 本地 | 不适用 |

**实现参考**: `src/Server/Services/LYBT.WebAPI/Controllers/DiagnosticsController.cs:20`、`LoggingLevelManager`

---

### US-SYS-006: 启用调试模式（定时，≤120 分钟）

**角色**: 超级管理员
**优先级**: Could
**状态**: ✅ 已实现

**作为** SuperAdmin，**我想要** 临时降低日志级别捕获诊断信息，**以便** 不重启服务排查生产问题，到期自动恢复。

**验收标准**:
- [ ] 非 SuperAdmin → 403
- [ ] 默认参数 → Debug 级别，30 分钟后自动恢复
- [ ] `durationMinutes=150` → 自动截断为 120
- [ ] 返回 previousLevel/currentLevel/startedAt/expiresAt

**业务规则**:
1. 仅 SuperAdmin 可操作。
2. 目标级别 Verbose/Debug/Information（默认 Debug）。
3. 持续时间 1-120 分钟（默认 30），超 120 自动截断。
4. 到期 Timer 自动恢复默认级别。
5. 新调试模式覆盖前一次（停旧 Timer，设新 Timer）；操作记 Warning 日志。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | `POST /api/v1/diagnostics/logging/debug/enable` |
| 本地 | 不适用 |

**实现参考**: `DiagnosticsController.cs:20`、`LoggingLevelManager`

---

### US-SYS-007: 禁用调试模式

**角色**: 超级管理员
**优先级**: Could
**状态**: ✅ 已实现

**作为** SuperAdmin，**我想要** 手动禁用调试模式恢复默认级别，**以便** 排查完成后立即恢复正常配置，不必等自动过期。

**验收标准**:
- [ ] 调试模式激活时禁用 → 恢复 defaultLevel
- [ ] 未激活时禁用 → 无副作用，返回当前状态
- [ ] 返回 previousLevel/currentLevel

**业务规则**:
1. 仅 SuperAdmin 可操作。
2. 恢复默认级别，停止自动过期 Timer，清除调试状态。
3. 操作记 Warning 日志。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | `POST /api/v1/diagnostics/logging/debug/disable` |
| 本地 | 不适用 |

**实现参考**: `DiagnosticsController.cs:20`、`LoggingLevelManager`

---

### US-SYS-008: 设置显式日志级别

**角色**: 超级管理员
**优先级**: Could
**状态**: ✅ 已实现

**作为** SuperAdmin，**我想要** 直接设置指定日志级别，**以便** 按运维需要精确控制日志输出粒度。

**验收标准**:
- [ ] level 为空 → 400 "日志级别不能为空"
- [ ] level 无效 → 400 "无效的日志级别" + validLevels
- [ ] level="Warning" → 200 + previousLevel + currentLevel="Warning"

**业务规则**:
1. 仅 SuperAdmin 可操作。
2. 支持级别：Verbose/Debug/Information/Warning/Error/Fatal。
3. 此操作不设自动过期（与调试模式不同）；操作记 Warning 日志。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | `POST /api/v1/diagnostics/logging/level` |
| 本地 | 不适用 |

**实现参考**: `DiagnosticsController.cs:20`、`LoggingLevelManager`

---

### US-SYS-009: 调试模式自动过期

**角色**: 系统
**优先级**: Could
**状态**: ✅ 已实现

**作为** 系统，**我想要** 调试模式到期自动恢复默认级别，**以便** 防止生产环境长期 Debug 导致性能下降与磁盘耗尽。

**验收标准**:
- [ ] Timer 到期 → 自动调用 DisableDebugMode
- [ ] 最长 120 分钟，超时自动截断
- [ ] 过期后 isDebugModeActive=false

**业务规则**:
1. 硬上限 120 分钟（SYS-D02）。
2. Timer 机制到期自动恢复 DefaultLevel。
3. 与手动禁用共用恢复路径。

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | Server Timer 自动触发 |
| 本地 | 不适用 |

**实现参考**: `LoggingLevelManager`（Timer + `IDisposable`）

---

## 变更记录

| 版本 | 日期 | 变更 | 原因 |
|------|------|------|------|
| v1.0 | 2026-06-28 | Split from 11-platform.md; merged Logging & Health into Observability module | 文档结构优化 S4 批次 3 |
