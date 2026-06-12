# 监控与告警指南

本文档定义 LYBT 系统的监控指标、告警规则和日常巡检流程。系统基于 ASP.NET Core HealthCheck + Serilog + SQL Server SystemLogs 构建，不依赖外部监控服务。

> 备份恢复见 [07-backup-recovery.md](./07-backup-recovery.md)；配置项见 [02-configuration.md](./02-configuration.md)。

---

## 监控架构

```
[Desktop Clients] ──→ [WebAPI (Kestrel)] ──→ [SQL Server]
                            │
                    ┌───────┼───────┐
                    ▼       ▼       ▼
              Console    File    SystemLogs
             (Debug)   (30d)    (Warning+)
```

---

## 健康检查端点

### 匿名端点（探活/负载均衡）

| 端点 | 方法 | 说明 |
|------|------|------|
| `/health` | GET | ASP.NET Core HealthCheck 中间件，返回 `Healthy`/`Degraded`/`Unhealthy` |
| `/health/database` | GET | 数据库连接专用检查 |

### 认证端点（运维详情）

| 端点 | 方法 | 权限 | 说明 |
|------|------|------|------|
| `/api/v1/health` | GET | 匿名 | 业务层健康状态 + 时间戳 |
| `/api/v1/health/ping` | GET | 匿名 | Ping/Pong |
| `/api/v1/health/details` | GET | 已认证 | 详细检查（数据库连接、迁移状态） |

### 响应示例

```json
{
  "status": "Healthy",
  "timestamp": "2026-06-12T10:00:00Z",
  "database": {
    "status": "Healthy",
    "duration": 15
  }
}
```

**状态码映射**：`Healthy` → 200，`Degraded` → 503，`Unhealthy` → 503。

---

## 日志系统

### 日志输出目标

| 目标 | 级别 | 存储 | 保留 |
|------|------|------|------|
| Console | Information+ | 标准输出 | 会话级 |
| File | Information+ | `logs/lybt-web-api-{date}.log` | 30天轮转，单文件 10MB |
| SQL Server | Warning+ | `SystemLogs` 表 | 可配置（默认 365天） |

### 日志格式

```
{Timestamp} [{Level}] [{CorrelationId}] [{SourceContext}] {Message}
```

### 运行时级别调整

SuperAdmin 可通过 Diagnostics API 临时调整日志级别：

```bash
# 查看当前状态
GET /api/v1/diagnostics/logging/status

# 启用调试模式（最长 120 分钟）
POST /api/v1/diagnostics/logging/debug/enable
{ "level": "Debug", "durationMinutes": 30 }

# 禁用调试模式
POST /api/v1/diagnostics/logging/debug/disable
```

---

## 诊断端点

| 端点 | 方法 | 说明 |
|------|------|------|
| `/api/v1/diagnostics/db-info` | GET | 数据库信息（版本、大小、连接数） |
| `/api/v1/diagnostics/version` | GET | 应用版本信息 |
| `/api/v1/diagnostics/recent-logs` | GET | 最近日志条目 |
| `/api/v1/diagnostics/logging/status` | GET | 当前日志级别配置 |
| `/api/v1/diagnostics/logging/debug/enable` | POST | 启用调试模式 |
| `/api/v1/diagnostics/logging/debug/disable` | POST | 禁用调试模式 |
| `/api/v1/diagnostics/logging/level` | POST | 设置日志级别 |

所有诊断端点需 SuperAdmin 权限。

---

## 告警规则

### 关键告警

| 编号 | 条件 | 级别 | 通知 | 处理 |
|------|------|------|------|------|
| ALT-001 | `/health` 返回 `Unhealthy` 超过 2 分钟 | **严重** | 即时 | 检查 SQL Server 连接、磁盘空间、服务状态 |
| ALT-002 | 数据库连接失败（`/health/database` 非 200） | **严重** | 即时 | 检查 SQL Server 服务、连接字符串、网络 |
| ALT-003 | WebAPI 进程崩溃或无响应 | **严重** | 即时 | 查看事件查看器、重启服务 `sc start LYBT-WebAPI` |

### 警告告警

| 编号 | 条件 | 级别 | 通知 | 处理 |
|------|------|------|------|------|
| ALT-004 | API 响应时间 > 5 秒（P95） | 警告 | 工作时间 | 检查 SystemLogs 慢查询、数据库负载 |
| ALT-005 | 磁盘空间 < 10% | 警告 | 工作时间 | 清理日志、备份文件 |
| ALT-006 | 日志中出现 `Error` 级别条目 > 10 条/小时 | 警告 | 工作时间 | 查看 SystemLogs 表定位问题 |
| ALT-007 | 同步失败率 > 20% | 警告 | 工作时间 | 检查网络连接、Token 有效性 |

### 信息告警

| 编号 | 条件 | 级别 | 通知 | 处理 |
|------|------|------|------|------|
| ALT-008 | 每日备份未执行 | 信息 | 每日汇总 | 检查 SQL Server Agent 作业状态 |
| ALT-009 | 客户端连接数异常增长（> 2x 均值） | 信息 | 每日汇总 | 确认是否有异常客户端 |

---

## 日常巡检

### 每日检查（5 分钟）

```powershell
# 1. 健康检查
Invoke-RestMethod -Uri "http://localhost:5000/health" -Method Get

# 2. 数据库连接
Invoke-RestMethod -Uri "http://localhost:5000/health/database" -Method Get

# 3. 最近错误日志
$query = "SELECT TOP 20 * FROM SystemLogs WHERE Level = 'Error' ORDER BY TimeStamp DESC"
Invoke-Sqlcmd -Query $query -Database LYBT_DB
```

### 每周检查（15 分钟）

```powershell
# 1. 备份验证
RESTORE VERIFYONLY FROM DISK = N'<最新备份路径>'

# 2. 数据库完整性
DBCC CHECKDB ([LYBT_DB])

# 3. 磁盘空间
Get-PSDrive -Name D | Select-Object Used, Free

# 4. 日志体积
Get-ChildItem "logs\" | Measure-Object -Property Length -Sum
```

### 每月检查（30 分钟）

1. 审查 SystemLogs 中 `Error`/`Fatal` 趋势
2. 检查备份保留策略执行情况
3. 确认安全日志（登录失败、权限拒绝）无异常
4. 检查数据库增长趋势，评估存储容量

---

## 监控脚本

### Windows 任务计划程序探活脚本

```powershell
# monitor-lybt.ps1 — 每 5 分钟由任务计划调用
$baseUrl = "http://localhost:5000"
$response = try {
    Invoke-RestMethod -Uri "$baseUrl/health" -Method Get -TimeoutSec 10
} catch {
    $null
}

if ($null -eq $response -or $response.status -ne "Healthy") {
    $body = "LYBT WebAPI unhealthy at $(Get-Date)`nStatus: $($response.status)"
    # 写入事件日志
    Write-EventLog -LogName Application -Source "LYBT Monitor" -EntryType Error -EventId 1001 -Message $body
    # 可选：发送邮件/钉钉通知
}
```

注册任务计划：

```powershell
$action = New-ScheduledTaskAction -Execute "powershell.exe" -Argument "-File C:\Scripts\monitor-lybt.ps1"
$trigger = New-ScheduledTaskTrigger -RepetitionInterval (New-TimeSpan -Minutes 5) -At "00:00" -Once
Register-ScheduledTask -TaskName "LYBT Health Monitor" -Action $action -Trigger $trigger -User "SYSTEM"
```

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-06-12 | v1.0 | 初始版本 |
