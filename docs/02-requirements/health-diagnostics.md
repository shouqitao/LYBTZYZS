# 系统健康与诊断 需求规格

## 概述

系统健康检查和运行时诊断模块，提供服务端探活、数据库连接状态检测以及运行时日志级别动态管理功能。健康检查支持负载均衡器/监控系统集成，诊断功能用于生产环境问题排查。

---

## 用户角色

| 角色 | 在本模块中的操作权限 |
|------|---------------------|
| SuperAdmin | 全部操作 (健康检查 + 诊断工具) |
| Admin | 详细健康检查 |
| Doctor | 基础健康检查 |
| Receptionist | 基础健康检查 |
| 匿名用户 | 基础健康检查、Ping |

> 诊断工具 (日志级别管理) 仅限 SuperAdmin 角色。

---

## 功能清单

### FR-SYS-001: 基础健康检查

- **描述**: 提供快速探活端点，返回服务运行状态和时间戳
- **业务规则**:
  1. 匿名访问，不需要认证
  2. 返回 status ("Healthy") + timestamp (UTC)
  3. 此端点不执行任何数据库或外部依赖检查
- **远程模式**: GET `/api/v1/health`，返回 200 + JSON
- **本地模式**: 不适用 (纯客户端，无服务端)
- **验收标准**:
  - [ ] 匿名请求 -> 返回 200 + `{"status":"Healthy","timestamp":"..."}`
  - [ ] 服务运行中始终返回 Healthy

### FR-SYS-002: Ping 端点

- **描述**: 最轻量的探活检查，返回 pong 消息
- **业务规则**:
  1. 匿名访问
  2. 返回 message ("pong") + timestamp (UTC)
- **远程模式**: GET `/api/v1/health/ping`，返回 200 + JSON
- **本地模式**: 不适用
- **验收标准**:
  - [ ] 匿名请求 -> 返回 200 + `{"message":"pong","timestamp":"..."}`

> **[已修订 2026-02-21]** 健康检查响应格式要求简化，JSON 字段名和结构允许与 PRD 示例存在差异
> 原因: 格式细节属过度规范，以代码实际响应为准  |  参考: SYS-03

### FR-SYS-003: 详细健康检查

- **描述**: 包含数据库连接状态和迁移检查的详细健康报告
- **业务规则**:
  1. 需要认证 (Bearer Token)
  2. 检查数据库连接 (CanConnectAsync)
  3. 仅关系型数据库检查待执行迁移数 (InMemory 数据库跳过迁移检查)
  4. 无待执行迁移 -> Healthy; 有待执行迁移 -> Degraded; 连接失败 -> Unhealthy
  5. Healthy 返回 200，Degraded/Unhealthy 返回 503
  6. 返回数据库检查耗时 (毫秒)
- **远程模式**: GET `/api/v1/health/details`，返回 200/503 + JSON
- **本地模式**: 不适用
- **验收标准**:
  - [ ] 未认证请求 -> 返回 401
  - [ ] 数据库正常 + 无待执行迁移 -> 返回 200 + status="Healthy"
  - [ ] 数据库正常 + 有待执行迁移 -> 返回 503 + status="Degraded"
  - [ ] 数据库连接失败 -> 返回 503 + status="Unhealthy"
  - [ ] 返回 database.duration 耗时毫秒数

> **[已修订 2026-02-21]** 健康检查超时配置要求放宽，数据库检查超时等参数允许运行时调整
> 原因: 可运行时调整，不需要 PRD 硬性规定  |  参考: SYS-04

### FR-SYS-004: 获取日志级别状态

- **描述**: 查询当前日志级别配置和调试模式状态
- **业务规则**:
  1. 仅 SuperAdmin 可访问
  2. 返回: currentLevel, defaultLevel, isDebugModeActive, debugModeStartedAt, debugModeExpiresAt, remainingMinutes
  3. remainingMinutes 仅在调试模式激活时返回
- **远程模式**: GET `/api/v1/diagnostics/logging/status`，返回 200 + JSON
- **本地模式**: 不适用
- **验收标准**:
  - [ ] 非 SuperAdmin 请求 -> 返回 403
  - [ ] 默认状态 -> isDebugModeActive=false, currentLevel=defaultLevel
  - [ ] 调试模式激活时 -> 返回完整的调试状态信息

> **[已修订 2026-02-21]** 诊断端点路径对齐代码实现，PRD 中的端点路径以代码实际注册路由为准
> 原因: 端点路径细节差异不影响功能，PRD 对齐代码  |  参考: SYS-05

### FR-SYS-005: 启用临时调试模式

- **描述**: 临时降低日志级别以捕获更多诊断信息，到期自动恢复
- **业务规则**:
  1. 仅 SuperAdmin 可操作
  2. 可指定目标级别: Verbose/Debug/Information (默认 Debug)
  3. 可指定持续时间: 1-120 分钟 (默认 30 分钟)
  4. durationMinutes 超过 120 自动截断为 120
  5. 到期后自动恢复默认日志级别 (Timer 机制)
  6. 启用新的调试模式会覆盖前一次 (停止旧 Timer，设置新 Timer)
  7. 操作记录 Warning 级别日志 (包含操作者信息)
- **远程模式**: POST `/api/v1/diagnostics/logging/debug/enable`
- **本地模式**: 不适用
- **请求体**:
  ```json
  {
    "level": "Debug",
    "durationMinutes": 30
  }
  ```
- **验收标准**:
  - [ ] 非 SuperAdmin -> 返回 403
  - [ ] 默认参数 -> 启用 Debug 级别，30 分钟后自动恢复
  - [ ] durationMinutes=150 -> 自动截断为 120
  - [ ] 返回 previousLevel + currentLevel + startedAt + expiresAt + durationMinutes

### FR-SYS-006: 禁用调试模式

- **描述**: 手动禁用调试模式，恢复默认日志级别
- **业务规则**:
  1. 仅 SuperAdmin 可操作
  2. 恢复默认级别 (DefaultLevel)
  3. 停止自动过期 Timer
  4. 清除调试模式状态 (StartedAt, ExpiresAt)
  5. 操作记录 Warning 级别日志
- **远程模式**: POST `/api/v1/diagnostics/logging/debug/disable`
- **本地模式**: 不适用
- **验收标准**:
  - [ ] 调试模式激活时禁用 -> 恢复到 defaultLevel
  - [ ] 未激活时禁用 -> 无副作用，返回当前状态
  - [ ] 返回 previousLevel + currentLevel

### FR-SYS-007: 手动设置日志级别

- **描述**: 直接设置指定的日志级别，持久生效直到重启或再次设置
- **业务规则**:
  1. 仅 SuperAdmin 可操作
  2. 支持级别: Verbose/Debug/Information/Warning/Error/Fatal
  3. Level 参数必填，为空返回 400
  4. 无效级别名返回 400 + validLevels 列表
  5. 此操作不设置自动过期 (与调试模式不同)
  6. 操作记录 Warning 级别日志
- **远程模式**: POST `/api/v1/diagnostics/logging/level`
- **本地模式**: 不适用
- **请求体**:
  ```json
  {
    "level": "Warning"
  }
  ```
- **验收标准**:
  - [ ] level 为空 -> 返回 400 + "日志级别不能为空"
  - [ ] level="InvalidLevel" -> 返回 400 + "无效的日志级别" + validLevels
  - [ ] level="Warning" -> 返回 200 + previousLevel + currentLevel="Warning"

### FR-SYS-008: Server 端数据库启动诊断

- **描述**: DatabaseStartupDiagnostics 在 Server 启动时自动检测 SQL Server 连接状态，输出详细故障排查建议
- **业务规则**:
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
- **远程模式**: Server 启动时自动执行
- **本地模式**: 不适用
- **验收标准**:
  - [ ] SQL Server 正常 -> Information 日志包含连接耗时
  - [ ] SQL Server 不可达 -> Error 日志包含故障排查建议
  - [ ] 诊断失败不阻塞应用启动 (可继续运行但功能降级)

### FR-SYS-009: Desktop 端启动性能诊断

- **描述**: StartupDiagnostics 记录 WPF 客户端各启动阶段的耗时和性能瓶颈，生成启动诊断报告
- **业务规则**:
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
- **远程模式**: 不适用 (Desktop 专属)
- **本地模式**: Desktop 启动时自动执行
- **验收标准**:
  - [ ] 启动完成后日志中包含完整的启动诊断报告
  - [ ] 慢步骤 (>3s) 在报告中被标记
  - [ ] 启动失败的步骤包含错误信息和步骤名称
  - [ ] 总启动时间等于各步骤耗时之和 (误差 <100ms)

---

## API 响应 JSON 模型

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

## 与 logging.md 的职责边界

| 职责 | 归属文档 | 说明 |
|------|---------|------|
| 日志框架配置 (Serilog) | logging.md | FR-LOG-001 |
| 安全审计日志记录 | logging.md | FR-LOG-002 |
| 敏感数据脱敏 | logging.md | FR-LOG-003 |
| LoggingLevelManager 内部机制 | logging.md | FR-LOG-004 |
| 健康检查 API (/health/*) | health-diagnostics.md | FR-SYS-001~003 |
| 日志级别管理 API (/diagnostics/*) | health-diagnostics.md | FR-SYS-004~007 |

> FR-LOG-004 定义 LoggingLevelManager 的内部行为 (线程安全/Timer/Dispose)；FR-SYS-004~007 定义通过 DiagnosticsController 暴露的 API 端点。两者是"内部实现"与"外部接口"的关系，不存在功能重叠。

---

## 数据模型

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

## 错误码

| 场景 | HTTP | 错误消息 | 触发条件 |
|------|------|----------|----------|
| 未认证 | 401 | Unauthorized | 访问 /health/details 未携带 Token |
| 权限不足 | 403 | Forbidden | 非 SuperAdmin 访问 /diagnostics/* |
| 日志级别为空 | 400 | 日志级别不能为空 | SetLoggingLevel 时 level 为空 |
| 无效日志级别 | 400 | 无效的日志级别 | SetLoggingLevel 时 level 不在枚举范围内 |
| 数据库降级 | 503 | Degraded | 详细健康检查发现待执行迁移 |

---

## 决策记录

| # | 决策 | 结论 | 日期 |
|---|------|------|------|
| 1 | 健康检查与诊断合并为一个PRD | 两者功能紧密关联且端点量少 | 2026-02-11 |
| 2 | 调试模式最大时长 | 120 分钟硬上限，防止生产环境长期 Debug 影响性能 | 2026-02-11 |
| 3 | InMemory 数据库跳过迁移检查 | 测试环境使用 InMemory 无迁移概念 | 2026-02-11 |
| 4 | 审计日志 v1.0 无管理界面 | 小型诊所场景通过数据库直查即可。v2.0 考虑增加查询 UI | 2026-02-17 |
| 5 | 启动诊断不阻塞应用 | Server/Desktop 启动诊断失败均不阻塞启动，记录错误日志后继续运行 | 2026-02-17 |
| 6 | Desktop 慢步骤阈值 3 秒 | 启动步骤耗时超过 3 秒标记为 Slow，便于定位启动性能瓶颈 | 2026-02-17 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-11 | v1.0 | 初始版本，从代码实现逆向工程 |
| 2026-02-17 | v1.1 | Round 7 深化: 新增 API 响应 JSON 模型 (5个示例)、与 logging.md 职责边界说明 |
| 2026-02-17 | v2.0 | Round 7 深化: 新增 FR-SYS-008 (Server 启动诊断)、FR-SYS-009 (Desktop 启动诊断)、2 条新决策 |
| 2026-02-21 | v2.1 | PRD vs Code 偏差分析修订: 3 项修订, 0 项延期标注 |
