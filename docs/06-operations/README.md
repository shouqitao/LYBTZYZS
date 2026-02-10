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

## 服务端部署

### 运行环境

| 组件 | 要求 |
|------|------|
| 运行时 | .NET 8.0 Runtime |
| 数据库 | SQL Server 2019+ |
| 操作系统 | Windows Server 2019+ / Linux |
| 端口 | 5000 (HTTP) / 5001 (HTTPS) |

### 发布命令

```bash
# 发布为自包含 (推荐)
dotnet publish src/Server/Services/LYBT.WebAPI -c Release -r win-x64 --self-contained

# 发布为框架依赖
dotnet publish src/Server/Services/LYBT.WebAPI -c Release
```

### 目录结构

```
deploy/
  LYBT.WebAPI.exe          # 主程序
  appsettings.json         # 主配置
  appsettings.Production.json  # 生产环境覆盖
  logs/                    # 日志目录 (自动创建)
```

---

## 配置说明

### 关键配置项

| 配置节 | 说明 | 文件 |
|--------|------|------|
| `ConnectionStrings` | 数据库连接 | appsettings.json |
| `Jwt` | Token 签名密钥、过期时间 | appsettings.json |
| `PasswordPolicy` | 密码复杂度要求 | appsettings.json |
| `Session` | 会话超时、并发控制 | appsettings.json |
| `Security.RateLimiting` | 限流策略 | appsettings.json |
| `Database` | 连接池、重试策略 | appsettings.json |
| `Serilog` | 日志级别、输出目标 | appsettings.json |

### JWT 配置

```json
{
  "Jwt": {
    "SecretKey": "...",                      // 生产环境必须更换
    "Issuer": "LYBT.WebAPI",
    "Audience": "LYBT.Client",
    "AccessTokenExpirationMinutes": 30,      // Access Token 有效期
    "RefreshTokenExpirationDays": 7,         // Refresh Token 有效期
    "ClockSkewSeconds": 300                  // 时钟偏差容忍
  }
}
```

### 限流配置

```json
{
  "Security": {
    "RateLimiting": {
      "Enabled": true,
      "GlobalLimit": { "PermitLimit": 200, "WindowSeconds": 60 },
      "LoginLimit": { "PermitLimit": 5, "WindowSeconds": 60 },
      "ApiLimit": { "PermitLimit": 100, "WindowSeconds": 60 },
      "WhitelistedIPs": ["127.0.0.1", "::1"]
    }
  }
}
```

### 数据库配置

```json
{
  "Database": {
    "AutoMigrate": false,                    // 生产环境关闭自动迁移
    "ConnectionPool": {
      "MaxConnections": 20,
      "MinConnections": 2,
      "ConnectionTimeoutSeconds": 30,
      "CommandTimeoutSeconds": 30
    },
    "RetryPolicy": {
      "MaxRetryCount": 3,
      "BaseDelayMs": 1000,
      "MaxDelayMs": 10000
    },
    "Monitoring": {
      "Enabled": true,
      "SlowQueryThresholdMs": 1000
    }
  }
}
```

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

通过 Diagnostics API 动态调整日志级别 (需 SuperAdmin 权限):

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

---

## 数据库运维

### 迁移

```bash
# 生成迁移
dotnet ef migrations add <MigrationName> -p src/Server/Core/LYBT.Infrastructure -s src/Server/Services/LYBT.WebAPI

# 应用迁移
dotnet ef database update -s src/Server/Services/LYBT.WebAPI
```

### 日志清理

系统内置日志自动清理:

```json
{
  "Logging": {
    "Cleanup": {
      "Enabled": true,
      "RetentionDays": 90,
      "CleanupIntervalHours": 24,
      "BatchSize": 1000
    }
  }
}
```

---

## 客户端部署

### Desktop 安装包

WPF Desktop 客户端通过 ClickOnce 或 MSI 分发。

### 本地模式数据

- SQLite 数据库: `%APPDATA%\LYBT\data\lybt-local.db`
- 日志文件: `%APPDATA%\LYBT\logs\`
- 配置文件: `%APPDATA%\LYBT\config\`

---

**变更记录**

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 初始版本 |
