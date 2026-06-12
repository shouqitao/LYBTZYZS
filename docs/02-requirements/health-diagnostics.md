# 系统健康与诊断 产品需求文档

---

## 1. Problem Statement

### 1.1 问题描述

中医诊所管理系统部署后，运维人员缺乏系统运行状态的实时可见性。服务端是否存活、数据库连接是否正常、是否存在未执行的迁移 -- 这些关键信息只有在用户报障后才能被动发现。同时，生产环境出现问题时，默认日志级别 (Information) 无法捕获足够的诊断信息，而修改配置文件并重启服务的方式会导致服务中断，影响诊疗业务。

### 1.2 用户痛点

| 角色 | 痛点 | 影响 |
|------|------|------|
| SuperAdmin | 生产环境出现异常时，需要修改配置文件并重启服务才能获取 Debug 日志 | 重启导致所有在线用户断开，影响诊疗业务 |
| SuperAdmin | 临时开启 Debug 后忘记恢复，导致日志量激增影响性能 | 磁盘空间快速耗尽，系统性能下降 |
| Admin | 无法判断系统是否处于健康状态，只能等用户报障 | 被动运维，问题响应时间长 |
| 医生 | 不清楚系统是否可用，操作失败后才发现服务异常 | 诊疗效率受损，体验差 |

### 1.3 证据

- 运维经验: 小型系统缺乏监控是最常见的稳定性风险来源
- 生产排查实践: 90% 的生产问题需要 Debug 级别日志才能定位根因
- 行业标准: ASP.NET Core 内置 HealthCheck 框架是业界成熟方案

---

## 2. Target Users

| 角色 | 在本模块中的操作权限 |
|------|---------------------|
| SuperAdmin | 全部操作 (健康检查 + 诊断工具: 日志级别管理) |
| Admin | 详细健康检查 |
| Doctor | 基础健康检查 |
| Receptionist | 基础健康检查 |
| 匿名用户 | 基础健康检查、Ping |

> 诊断工具 (日志级别管理) 仅限 SuperAdmin 角色。

---

## 3. Strategic Context

### 3.1 业务目标

| 目标 | 对应 |
|------|------|
| 系统可观测性 | 健康检查端点让运维人员和监控系统实时掌握系统状态 |
| 无中断诊断 | 动态日志级别调整无需重启服务，不影响在线用户 |
| 安全的调试模式 | 调试模式自动过期 (最长 120 分钟)，防止长期 Debug 影响性能 |
| 启动可靠性 | Server/Desktop 启动诊断帮助快速定位启动阶段的连接和性能问题 |

### 3.2 Why Now

系统进入正式部署阶段，从开发环境迁移到生产环境后，"手动检查 + 重启排查" 的方式不再可接受。健康检查是负载均衡器集成的前提条件，动态日志管理是生产环境问题排查的基础能力。

---

## 4. Solution Overview

健康与诊断模块提供两大能力: **系统健康检查** 和 **运行时诊断**。

**健康检查 (Health Check):**
- **Ping**: 最轻量探活，返回 pong (匿名)
- **基础检查**: 服务存活状态 + 时间戳 (匿名)
- **详细检查**: 数据库连接 + 迁移状态 + 耗时报告 (需认证)

**运行时诊断 (Diagnostics):**
- **日志级别查询**: 查看当前日志配置和调试模式状态
- **临时调试模式**: 降低日志级别捕获诊断信息，到期自动恢复
- **手动级别设置**: 直接设定日志级别，持久生效直到重启

**启动诊断 (Startup Diagnostics):**
- **Server**: 启动时自动检测 SQL Server 连接，输出故障排查建议
- **Desktop**: 记录 WPF 各启动阶段耗时，检测性能瓶颈

```
运维/监控系统 → GET /health, /health/ping        → 探活 (匿名)
Admin/Doctor  → GET /health/details               → 详细状态 (认证)
SuperAdmin    → GET/POST /diagnostics/logging/*    → 日志级别管理 (SuperAdmin)
Server 启动   → DatabaseStartupDiagnostics        → 自动执行
Desktop 启动  → StartupDiagnostics                → 自动执行
```

---

## 5. Success Metrics

| 指标 | 当前 (无监控) | v1.0 目标 | 衡量方式 |
|------|-------------|----------|---------|
| 健康检查可用性 | 0% (无端点) | 100% 时间可用 | 监控系统定期探测 |
| 问题发现时间 | > 30 分钟 (用户报障) | < 1 分钟 (主动检测) | 健康检查轮询间隔 |
| 调试日志获取 | 需重启服务 (5-10 分钟) | 即时 (< 5 秒) | API 响应时间 |
| 调试模式遗忘率 | N/A | 0% (自动过期) | Timer 机制保障 |
| 启动问题定位 | 手动排查 (30+ 分钟) | 日志直接定位 (< 5 分钟) | 启动诊断报告 |

---

## 6. Epic Hypothesis

We believe that 提供健康检查端点 + 动态日志级别管理 + 启动诊断报告 for 运维人员和 SuperAdmin will achieve 系统可观测性和无中断生产环境问题排查能力。We'll know we're right when 健康检查端点 100% 可用、调试模式零遗忘 (全部自动过期)、且生产问题定位时间从 30 分钟降至 5 分钟以内。

---

## 7. User Stories

### 优先级汇总

| US 编号 | 名称 | Priority |
|---------|------|----------|
| US-SYS-001 | 基础健康检查 | Could |
| US-SYS-002 | Ping 端点 | Could |
| US-SYS-003 | 详细健康检查 | Could |
| US-SYS-004 | 获取日志级别状态 | Could |
| US-SYS-005 | 启用临时调试模式 | Could |
| US-SYS-006 | 禁用调试模式 | Could |
| US-SYS-007 | 手动设置日志级别 | Could |
| US-SYS-008 | Server 端数据库启动诊断 | Could |
| US-SYS-009 | Desktop 端启动性能诊断 | Could |

---

### US-SYS-001: 基础健康检查

> As a 运维人员/监控系统, I want to 通过轻量端点检查服务是否存活,
> so that 我可以实时掌握系统运行状态，在服务异常时立即收到告警。

**Acceptance Criteria:**
- [ ] 匿名请求 -> 返回 200 + `{"status":"Healthy","timestamp":"..."}`
- [ ] 服务运行中始终返回 Healthy
- [ ] 不执行任何数据库或外部依赖检查

**Business Rules:**
1. 匿名访问，不需要认证
2. 返回 status ("Healthy") + timestamp (UTC)
3. 此端点不执行任何数据库或外部依赖检查

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | GET `/api/v1/health`，返回 200 + JSON |
| 本地 | 不适用 (纯客户端，无服务端) |

### US-SYS-002: Ping 端点

> As a 负载均衡器, I want to 通过最轻量的端点探测服务存活,
> so that 我可以快速判断后端服务是否可达，进行流量分发决策。

**Acceptance Criteria:**
- [ ] 匿名请求 -> 返回 200 + `{"message":"pong","timestamp":"..."}`

**Business Rules:**
1. 匿名访问
2. 返回 message ("pong") + timestamp (UTC)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | GET `/api/v1/health/ping`，返回 200 + JSON |
| 本地 | 不适用 |

### US-SYS-003: 详细健康检查

> As a Admin, I want to 查看包含数据库状态的详细健康报告,
> so that 我可以在用户报障前主动发现数据库连接或迁移问题。

**Acceptance Criteria:**
- [ ] 未认证请求 -> 返回 401
- [ ] 数据库正常 + 无待执行迁移 -> 返回 200 + status="Healthy"
- [ ] 数据库正常 + 有待执行迁移 -> 返回 503 + status="Degraded"
- [ ] 数据库连接失败 -> 返回 503 + status="Unhealthy"
- [ ] 返回 database.duration 耗时毫秒数

**Business Rules:**
1. 需要认证 (Bearer Token)
2. 检查数据库连接 (CanConnectAsync)
3. 仅关系型数据库检查待执行迁移数 (InMemory 数据库跳过迁移检查)
4. 无待执行迁移 -> Healthy; 有待执行迁移 -> Degraded; 连接失败 -> Unhealthy
5. Healthy 返回 200，Degraded/Unhealthy 返回 503
6. 返回数据库检查耗时 (毫秒)

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | GET `/api/v1/health/details`，返回 200/503 + JSON |
| 本地 | 不适用 |

### US-SYS-004: 获取日志级别状态

> As a SuperAdmin, I want to 查询当前日志级别配置和调试模式状态,
> so that 我可以了解系统当前的日志捕获级别，判断是否需要调整。

**Acceptance Criteria:**
- [ ] 非 SuperAdmin 请求 -> 返回 403
- [ ] 默认状态 -> isDebugModeActive=false, currentLevel=defaultLevel
- [ ] 调试模式激活时 -> 返回完整的调试状态信息 (startedAt, expiresAt, remainingMinutes)

**Business Rules:**
1. 仅 SuperAdmin 可访问
2. 返回: currentLevel, defaultLevel, isDebugModeActive, debugModeStartedAt, debugModeExpiresAt, remainingMinutes
3. remainingMinutes 仅在调试模式激活时返回

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | GET `/api/v1/diagnostics/logging/status`，返回 200 + JSON |
| 本地 | 不适用 |

### US-SYS-005: 启用临时调试模式

> As a SuperAdmin, I want to 临时降低日志级别以捕获更多诊断信息,
> so that 我可以在不重启服务的情况下排查生产环境问题，且调试模式到期后自动恢复。

**Acceptance Criteria:**
- [ ] 非 SuperAdmin -> 返回 403
- [ ] 默认参数 -> 启用 Debug 级别，30 分钟后自动恢复
- [ ] durationMinutes=150 -> 自动截断为 120
- [ ] 返回 previousLevel + currentLevel + startedAt + expiresAt + durationMinutes

**Business Rules:**
1. 仅 SuperAdmin 可操作
2. 可指定目标级别: Verbose/Debug/Information (默认 Debug)
3. 可指定持续时间: 1-120 分钟 (默认 30 分钟)
4. durationMinutes 超过 120 自动截断为 120
5. 到期后自动恢复默认日志级别 (Timer 机制)
6. 启用新的调试模式会覆盖前一次 (停止旧 Timer，设置新 Timer)
7. 操作记录 Warning 级别日志 (包含操作者信息)

**Request Body:**
```json
{
  "level": "Debug",
  "durationMinutes": 30
}
```

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/diagnostics/logging/debug/enable` |
| 本地 | 不适用 |

### US-SYS-006: 禁用调试模式

> As a SuperAdmin, I want to 手动禁用调试模式并恢复默认日志级别,
> so that 我可以在排查完成后立即恢复正常日志配置，避免等待自动过期。

**Acceptance Criteria:**
- [ ] 调试模式激活时禁用 -> 恢复到 defaultLevel
- [ ] 未激活时禁用 -> 无副作用，返回当前状态
- [ ] 返回 previousLevel + currentLevel

**Business Rules:**
1. 仅 SuperAdmin 可操作
2. 恢复默认级别 (DefaultLevel)
3. 停止自动过期 Timer
4. 清除调试模式状态 (StartedAt, ExpiresAt)
5. 操作记录 Warning 级别日志

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/diagnostics/logging/debug/disable` |
| 本地 | 不适用 |

### US-SYS-007: 手动设置日志级别

> As a SuperAdmin, I want to 直接设置指定的日志级别,
> so that 我可以根据当前运维需要精确控制日志输出粒度。

**Acceptance Criteria:**
- [ ] level 为空 -> 返回 400 + "日志级别不能为空"
- [ ] level="InvalidLevel" -> 返回 400 + "无效的日志级别" + validLevels
- [ ] level="Warning" -> 返回 200 + previousLevel + currentLevel="Warning"

**Business Rules:**
1. 仅 SuperAdmin 可操作
2. 支持级别: Verbose/Debug/Information/Warning/Error/Fatal
3. Level 参数必填，为空返回 400
4. 无效级别名返回 400 + validLevels 列表
5. 此操作不设置自动过期 (与调试模式不同)
6. 操作记录 Warning 级别日志

**Request Body:**
```json
{
  "level": "Warning"
}
```

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/diagnostics/logging/level` |
| 本地 | 不适用 |

### US-SYS-008: Server 端数据库启动诊断

> As a 运维人员, I want to Server 启动时自动检测数据库连接状态并输出故障排查建议,
> so that 我可以在服务启动失败时快速定位数据库连接问题，而不需要手动逐项排查。

**Acceptance Criteria:**
- [ ] SQL Server 正常 -> Information 日志包含连接耗时
- [ ] SQL Server 不可达 -> Error 日志包含故障排查建议
- [ ] 诊断失败不阻塞应用启动 (可继续运行但功能降级)

**Business Rules:**
1. 在应用启动阶段 (Program.cs) 自动执行
2. 检查项: 数据库连接 (CanConnectAsync) + 连接池配置
3. 连接成功: 记录 Information 日志 (数据库名称 + 服务器地址 + 连接耗时)
4. 连接失败: 记录 Error 日志 + 输出故障排查建议列表:
   - 检查 SQL Server 服务是否启动
   - 检查连接字符串配置
   - 检查网络连通性和防火墙
   - 检查数据库权限
5. 诊断失败**不阻塞应用启动** (降级启动)
6. 诊断结果可通过 StartupDiagnostics 报告查看

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | Server 启动时自动执行 |
| 本地 | 不适用 |

### US-SYS-009: Desktop 端启动性能诊断

> As a 开发人员/运维人员, I want to 查看 Desktop 客户端各启动阶段的耗时报告,
> so that 我可以定位启动性能瓶颈，优化用户的首屏加载体验。

**Acceptance Criteria:**
- [ ] 启动完成后日志中包含完整的启动诊断报告
- [ ] 慢步骤 (>3s) 在报告中被标记
- [ ] 启动失败的步骤包含错误信息和步骤名称
- [ ] 总启动时间等于各步骤耗时之和 (误差 <100ms)

**Business Rules:**
1. 记录启动全过程: BeginStartup -> 各步骤 -> EndStartup
2. 每个步骤记录: 步骤名称、开始时间、结束时间、耗时、成功/失败状态、错误信息
3. 里程碑标记: RecordMarker 记录关键节点 (如 "Prism初始化完成"、"首屏渲染")
4. 慢步骤检测: 耗时超过 3 秒的步骤标记为 Slow
5. 失败步骤记录: 失败步骤包含错误消息
6. 诊断报告内容:
   - 总启动时间
   - 各步骤耗时列表 (按执行顺序)
   - 慢步骤列表
   - 失败步骤列表
   - 详细时间线
7. 报告通过 GetReport() 获取，输出到日志文件

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | 不适用 (Desktop 专属) |
| 本地 | Desktop 启动时自动执行 |

---

## 8. Out of Scope

| 排除项 | 原因 |
|--------|------|
| 分布式健康检查 (多节点聚合) | 小型诊所单节点部署，无需分布式健康聚合 |
| 审计日志管理界面 | v1.0 小型诊所通过数据库直查即可，v2.0 考虑 (SYS-D04) |
| 磁盘/CPU/内存系统指标监控 | 超出应用层健康检查范围，交由基础设施监控工具 |
| 日志文件在线查看/下载 | 增加安全风险，运维人员通过服务器直接访问日志文件 |
| 健康检查响应格式严格规范 | JSON 字段名和结构允许与 PRD 示例存在差异，以代码实际响应为准 |
| 健康检查超时硬性配置 | 数据库检查超时等参数允许运行时调整，不在 PRD 中硬性规定 |

---

## 9. Dependencies & Risks

| 风险 | 影响 | 缓解措施 |
|------|------|---------|
| SQL Server 服务未启动 | 详细健康检查返回 Unhealthy，功能降级 | 启动诊断提供排查建议，健康检查不阻塞基础功能 |
| 调试模式长期运行 | 日志量激增，磁盘空间耗尽，系统性能下降 | 硬上限 120 分钟 + Timer 自动过期机制 |
| 健康检查端点被滥用 | DDoS 风险，影响服务稳定性 | 匿名端点轻量化 (无 DB 查询)，详细检查需认证 |
| 诊断端点权限泄露 | 攻击者获取系统内部信息 | 仅 SuperAdmin 可访问诊断端点 |
| InMemory 数据库测试环境差异 | 迁移检查跳过导致测试覆盖不完整 | InMemory 环境明确跳过迁移检查 (SYS-D03) |

---

## 10. Open Questions

| ID | 问题 | 状态 |
|----|------|------|
| OQ-SYS-01 | 健康检查是否需要集成 ASP.NET Core 原生 HealthCheck 框架? | 已决定: v1.0 使用自定义实现，后续版本可迁移 |
| OQ-SYS-02 | 是否需要为健康检查端点添加缓存以减少数据库查询频率? | 待定。当前单诊所场景查询频率低，暂不需要 |
| OQ-SYS-03 | Desktop 端是否需要展示 Server 健康状态的 UI 组件? | 待定。v1.0 仅 API 端点，Desktop 端连接状态由认证模块处理 |

---

## API Response JSON Models

### GET /api/v1/health (基础)

```json
{
  "status": "Healthy",
  "timestamp": "2026-02-17T10:30:00Z"
}
```

### GET /api/v1/health/ping

```json
{
  "message": "pong",
  "timestamp": "2026-02-17T10:30:00Z"
}
```

### GET /api/v1/health/details (健康)

```json
{
  "status": "Healthy",
  "timestamp": "2026-02-17T10:30:00Z",
  "checks": {
    "database": {
      "status": "Healthy",
      "duration": 45,
      "server": "localhost",
      "database": "LYBTDB",
      "pendingMigrations": 0
    }
  }
}
```

### GET /api/v1/health/details (降级)

```json
{
  "status": "Degraded",
  "timestamp": "2026-02-17T10:30:00Z",
  "checks": {
    "database": {
      "status": "Degraded",
      "duration": 120,
      "server": "localhost",
      "database": "LYBTDB",
      "pendingMigrations": 2,
      "pendingMigrationNames": ["20260217_AddField", "20260218_UpdateIndex"]
    }
  }
}
```

### GET /api/v1/health/details (不健康)

```json
{
  "status": "Unhealthy",
  "timestamp": "2026-02-17T10:30:00Z",
  "checks": {
    "database": {
      "status": "Unhealthy",
      "duration": 5000,
      "error": "无法连接到数据库服务器"
    }
  }
}
```

### GET /api/v1/diagnostics/logging/status

```json
{
  "currentLevel": "Information",
  "defaultLevel": "Information",
  "isDebugModeActive": false,
  "debugModeStartedAt": null,
  "debugModeExpiresAt": null,
  "remainingMinutes": null
}
```

---

## Cross-Module Boundary

### 与 logging.md 的职责边界

| 职责 | 归属文档 | 说明 |
|------|---------|------|
| 日志框架配置 (Serilog) | logging.md | US-LOG-001 |
| 安全审计日志记录 | logging.md | US-LOG-002 |
| 敏感数据脱敏 | logging.md | US-LOG-003 |
| LoggingLevelManager 内部机制 | logging.md | US-LOG-004 |
| 健康检查 API (/health/*) | health-diagnostics.md | US-SYS-001~003 |
| 日志级别管理 API (/diagnostics/*) | health-diagnostics.md | US-SYS-004~007 |

> US-LOG-004 定义 LoggingLevelManager 的内部行为 (线程安全/Timer/Dispose)；US-SYS-004~007 定义通过 DiagnosticsController 暴露的 API 端点。两者是"内部实现"与"外部接口"的关系，不存在功能重叠。

---

## Data Model

### HealthCheck (内部类)

| 字段 | 类型 | 说明 |
|------|------|------|
| Name | string | 检查项名称 (如 "db") |
| Status | string | 状态: Healthy / Degraded / Unhealthy |
| Duration | long | 检查耗时 (毫秒) |

### EnableDebugModeRequest

| 字段 | 类型 | 必填 | 默认值 | 说明 |
|------|------|------|--------|------|
| Level | string? | 否 | "Debug" | 目标级别 (Verbose/Debug/Information) |
| DurationMinutes | int? | 否 | 30 | 持续时间 (1-120 分钟) |

### SetLoggingLevelRequest

| 字段 | 类型 | 必填 | 说明 |
|------|------|------|------|
| Level | string | 是 | 目标级别 (Verbose/Debug/Information/Warning/Error/Fatal) |

---

## Error Codes

| 场景 | HTTP | 错误消息 | 触发条件 |
|------|------|----------|----------|
| 未认证 | 401 | Unauthorized | 访问 /health/details 未携带 Token |
| 权限不足 | 403 | Forbidden | 非 SuperAdmin 访问 /diagnostics/* |
| 日志级别为空 | 400 | 日志级别不能为空 | SetLoggingLevel 时 level 为空 |
| 无效日志级别 | 400 | 无效的日志级别 | SetLoggingLevel 时 level 不在枚举范围内 |
| 数据库降级 | 503 | Degraded | 详细健康检查发现待执行迁移 |

---

## Decision Log

| 编号 | 问题 | 影响范围 | 状态 |
|------|------|----------|------|
| SYS-D01 | 健康检查与诊断合并为一个 PRD | 全模块 | 已确定: 两者功能紧密关联且端点量少，合并管理更高效 |
| SYS-D02 | 调试模式最大时长 | US-SYS-005 | 已确定: 120 分钟硬上限，防止生产环境长期 Debug 影响性能 |
| SYS-D03 | InMemory 数据库跳过迁移检查 | US-SYS-003 | 已确定: 测试环境使用 InMemory 无迁移概念，跳过迁移检查避免误报 |
| SYS-D04 | 审计日志 v1.0 无管理界面 | Out of Scope | 已确定: 小型诊所场景通过数据库直查即可。v2.0 考虑增加查询 UI |
| SYS-D05 | 启动诊断不阻塞应用 | US-SYS-008, US-SYS-009 | 已确定: Server/Desktop 启动诊断失败均不阻塞启动，记录错误日志后继续运行 |
| SYS-D06 | Desktop 慢步骤阈值 3 秒 | US-SYS-009 | 已确定: 启动步骤耗时超过 3 秒标记为 Slow，便于定位启动性能瓶颈 |

### 修订历史

| 日期 | 修订项 | 原因 | 参考 |
|------|--------|------|------|
| 2026-02-21 | 健康检查响应格式要求简化 | JSON 字段名和结构允许与 PRD 示例存在差异，格式细节属过度规范，以代码实际响应为准 | SYS-03 |
| 2026-02-21 | 健康检查超时配置要求放宽 | 数据库检查超时等参数允许运行时调整，不需要 PRD 硬性规定 | SYS-04 |
| 2026-02-21 | 诊断端点路径对齐代码实现 | 端点路径细节差异不影响功能，PRD 对齐代码 | SYS-05 |

---

## Change Log

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-11 | v1.0 | 初始版本，从代码实现逆向工程 |
| 2026-02-17 | v1.1 | Round 7 深化: 新增 API 响应 JSON 模型 (5个示例)、与 logging.md 职责边界说明 |
| 2026-02-17 | v2.0 | Round 7 深化: 新增 FR-SYS-008 (Server 启动诊断)、FR-SYS-009 (Desktop 启动诊断)、2 条新决策 |
| 2026-02-21 | v2.1 | PRD vs Code 偏差分析修订: 3 项修订, 0 项延期标注 |
| 2026-03-06 | v3.0 | PRD 全面重写: FR->US 格式迁移，新增 Problem Statement/Strategic Context/Success Metrics/Epic Hypothesis/Out of Scope/Dependencies & Risks/Open Questions 7 个章节，决策编号统一为 SYS-Dxx，修订注释迁移至 Decision Log |
