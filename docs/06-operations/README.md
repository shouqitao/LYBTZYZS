# 运维文档

## 部署架构

```
[Desktop Client (WPF)]
     │
     ├── 远程模式 ──→ [LYBT.WebAPI] ──→ [SQL Server]
     │                   (Kestrel)
     │
     └── 本地模式 ──→ [SQLite 本地文件]
                       (%APPDATA%\LYBT\data\)
```

---

## 概述

本目录包含 LYBT 系统的运维相关文档，涵盖部署、配置、日志和健康检查。

| 文档 | 内容 |
|------|------|
| [deployment.md](./deployment.md) | 服务端部署、客户端部署、数据库运维 |
| [configuration.md](./configuration.md) | 完整配置项说明 (JWT/密码策略/会话/限流/数据库/缓存/Kestrel) |

---

## 日志系统

### 日志输出

| 目标 | 级别 | 说明 |
|------|------|------|
| Console | Information+ | 开发调试 |
| File | Information+ | 本地日志文件 (30天轮转) |
| SQL Server | Warning+ | 数据库持久化 (SystemLogs 表) |

### 日志文件

- 路径: `logs/lybt-web-api-{date}.log`
- 轮转: 每日轮转，最多 30 个文件
- 单文件上限: 10MB
- 格式: `{Timestamp} [{Level}] [{CorrelationId}] [{SourceContext}] {Message}`

### 运行时日志调整

通过 Diagnostics API 动态调整日志级别 (需 SuperAdmin 权限)，详见 [API 参考 - Diagnostics](../04-api-reference/README.md):

```bash
# 查看当前日志级别
GET /api/v1/diagnostics/logging/status

# 启用调试模式 (临时，最长 120 分钟)
POST /api/v1/diagnostics/logging/debug/enable
{
  "level": "Debug",
  "durationMinutes": 30
}

# 禁用调试模式
POST /api/v1/diagnostics/logging/debug/disable
```

---

## 健康检查

### 端点

| 端点 | 权限 | 说明 |
|------|------|------|
| `GET /api/v1/health` | 匿名 | 基础探活 (返回 `Healthy` + 时间戳) |
| `GET /api/v1/health/ping` | 匿名 | Ping/Pong |
| `GET /api/v1/health/details` | 已认证 | 详细检查 (含数据库连接、迁移状态) |

### 响应示例

```json
{
  "status": "Healthy",
  "timestamp": "2026-02-10T10:00:00Z",
  "database": {
    "status": "Healthy",
    "duration": 15
  }
}
```

状态值: `Healthy` / `Degraded` / `Unhealthy`。`Degraded` 返回 503。

详见 [API 参考 - Health](../04-api-reference/README.md)。

---

**变更记录**

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.1 | 拆分部署和配置内容到独立文档，精简为索引+日志+健康检查 |
| 2026-02-10 | v1.0 | 初始版本 |
