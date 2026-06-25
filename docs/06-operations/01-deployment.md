# 部署指南

本文档包含服务端部署、客户端部署和数据库运维的详细操作说明。

> 部署架构总览见 [README.md](./README.md)

---

## 服务端部署

### 发布形式

| 形式 | 说明 | 适用场景 |
|------|------|---------|
| **Framework-Dependent**（推荐） | 依赖目标机器 .NET 8 Runtime，包小 (~27MB) | 有服务器管理权限的场景 |
| Self-Contained | 自带运行时，包大 (~70MB) | 无法控制目标机器环境的场景 |

### 运行方式

| 方式 | 说明 | 文档 |
|------|------|------|
| **Windows Service**（推荐） | 独立进程，开机自启，适合后台服务 | 待创建部署脚本 |
| IIS | 需要 IIS 环境，图形化管理 | 待创建部署脚本 |

两种方式均使用 `dotnet publish` 产出部署包，通过对应脚本部署。

### Desktop 自动升级

WebAPI 同时提供 Desktop 客户端发布包下载服务，支持客户端自动升级。

```
服务器目录结构:
C:\Services\LYBT-API\              ← WebAPI 运行目录（deploy.ps1 产出）
C:\Services\LYBT-releases\         ← Desktop 发布包（独立目录，不会被 publish 清空）
    ├── lybt-desktop-1.0.0.zip
    ├── lybt-desktop-1.1.0.zip
    └── lybt-desktop-1.2.0.zip
```

升级机制：
1. Desktop 启动时请求 `GET /api/version` 获取最新版本号
2. 版本低于服务端 → 从 `/releases/` 下载最新 zip 包
3. 本地解压替换 → 重启客户端

> **关键设计**：Releases 目录与 WebAPI 部署目录**完全独立**，确保 `dotnet publish` 不会清空历史发布包。

### 环境变量参考

| 变量名 | 默认值 | 说明 |
|--------|--------|------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | 运行环境（Development / Staging / Production） |
| `ASPNETCORE_URLS` | `http://localhost:5000` | 监听地址和端口 |
| `ConnectionStrings__LYBTDB` | — | SQL Server 连接字符串（覆盖 appsettings） |
| `ConnectionStrings__LYBTDesktop` | — | LocalDB 连接字符串（Desktop 嵌入式服务） |
| `Jwt__SecretKey` | — | JWT 签名密钥（≥32 字符，生产环境必须覆盖） |
| `Jwt__Issuer` | `LYBT.WebAPI` | JWT 签发者 |
| `Jwt__Audience` | `LYBT.Client` | JWT 受众 |
| `Jwt__AccessTokenExpirationMinutes` | `480` | Access Token 有效期（分钟） |
| `DefaultPasswords__SysAdminPassword` | — | sysadmin 默认密码（生产环境必须覆盖） |
| `DefaultPasswords__NewUserPassword` | — | 新用户默认密码（生产环境必须覆盖） |
| `DOTNET_ENVIRONMENT` | — | .NET 运行环境（备选） |

> **双下划线约定**：ASP.NET Core 通过 `__`（双下划线）分隔层级来覆盖 JSON 配置节。例如 `ConnectionStrings__LYBTDB` 覆盖 `ConnectionStrings:LYBTDB`。

### 发布命令

```bash
# 发布为框架依赖（推荐）
dotnet publish src/Server/Services/LYBT.WebAPI -c Release

# 发布为自包含（目标机器无 .NET Runtime 时使用）
dotnet publish src/Server/Services/LYBT.WebAPI -c Release -r win-x64 --self-contained true
```

### 部署后健康检查验证

部署完成后，按以下步骤验证系统状态：

```powershell
# 1. 等待服务完全启动（约 10-15 秒）
Start-Sleep -Seconds 15

# 2. 基础健康检查
$health = Invoke-RestMethod -Uri "http://localhost:5000/health" -Method Get
if ($health.status -ne "Healthy") { throw "Health check failed: $($health.status)" }

# 3. 数据库连接检查
$dbHealth = Invoke-RestMethod -Uri "http://localhost:5000/health/database" -Method Get
if ($dbHealth.status -ne "Healthy") { throw "Database unhealthy" }

# 4. 详细状态检查（需认证 Token）
$token = Invoke-RestMethod -Uri "http://localhost:5000/api/v1/auth/login" `
    -Method Post -ContentType "application/json" `
    -Body '{"username":"sysadmin","password":"SysAdmin@2026!"}' | Select-Object -ExpandProperty token
$headers = @{ Authorization = "Bearer $token" }
$details = Invoke-RestMethod -Uri "http://localhost:5000/api/v1/health/details" -Headers $headers
Write-Host "DB Status: $($details.database.status), Duration: $($details.database.duration)ms"

# 5. 验证端口监听
netstat -ano | findstr ":5000"
```

### IIS 配置要点

如使用 IIS 作为反向代理（非直接 Kestrel）：

1. **安装 ASP.NET Core Hosting Bundle** — 下载并安装 [.NET 8 Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/8.0)
2. **创建应用池** — .NET CLR 版本设为"无托管代码"，管道模式设为"集成"
3. **web.config** — `dotnet publish` 会自动生成，确认 `processPath` 指向 `dotnet.exe` 或发布目录的 `LYBT.WebAPI.exe`
4. **请求超时** — IIS 默认 2 分钟超时，长操作（如报表导出）需调大 `system.webServer/httpRuntime` 的 `executionTimeout`
5. **WebSocket** — 如使用 SignalR（未来扩展），需启用 WebSocket 协议

> **注意**：本项目推荐使用 Windows Service 直接运行 Kestrel，IIS 仅作为备选方案。

### 目录结构

```
publish/
  LYBT.WebAPI.exe          # 主程序
  appsettings.json         # 主配置
  appsettings.Production.json  # 生产环境覆盖
  logs/                    # 日志目录 (自动创建)
```

---

## 客户端部署

### Desktop 安装包

WPF Desktop 客户端通过 ClickOnce 或 MSI 分发。

### 本地模式数据

- SQL Server LocalDB 数据库: `%APPDATA%\LYBT\data\lybt-local.mdf`
- 日志文件: `%LOCALAPPDATA%\LYBTZYZS\logs\`
- 配置文件: `%APPDATA%\LYBT\config\`

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

## 故障排查

### 服务端启动失败

| 症状 | 可能原因 | 解决方案 |
|------|---------|---------|
| 启动即退出，无日志 | 配置文件缺失或 JSON 格式错误 | 检查 `appsettings.json` 是否存在且格式正确 |
| "Connection refused" | SQL Server 未启动或端口被占 | 确认 SQL Server 服务运行中，检查连接字符串 |
| "Invalid JWT SecretKey" | 密钥长度不足 | SecretKey 至少 32 字符 |
| 端口 5000/5001 被占 | 其他进程占用 | `netstat -ano | findstr :5000` 定位进程 |
| 迁移失败 | 数据库版本不匹配 | 运行 `dotnet ef database update` 应用待执行迁移 |

### 客户端常见问题

| 症状 | 可能原因 | 解决方案 |
|------|---------|---------|
| 登录后白屏 | API 地址配置错误 | 检查 Desktop 配置中 Server URL 是否正确 |
| 本地模式数据丢失 | LocalDB 文件被删除或损坏 | 检查 `%APPDATA%\LYBT\data\` 目录 |
| "Token Expired" 频繁弹出 | 客户端与服务端时钟偏差过大 | 同步系统时间，或调大 `ClockSkewSeconds` |

### 数据库问题

| 症状 | 可能原因 | 解决方案 |
|------|---------|---------|
| 慢查询告警 | 缺少索引或数据量增长 | 检查 `Database.Monitoring.SlowQueryThresholdMs` 日志 |
| 连接池耗尽 | 连接泄漏或并发过高 | 检查 `MaxConnections` 配置，排查未释放的 DbContext |
| 迁移冲突 | 多人同时生成迁移 | 合并迁移文件后重新 `dotnet ef database update` |

---

## 变更记录
| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-10 | v1.0 | 从 README.md 拆分，初始版本 |
| 2026-02-22 | v1.1 | 新增故障排查章节 (服务端/客户端/数据库) |
| 2026-06-25 | v1.2 | 修正 Desktop 日志路径 %APPDATA%\LYBT → %LOCALAPPDATA%\LYBTZYZS（与代码一致） |
| 2026-06-25 | v1.3 | 新增环境变量参考表、部署后健康检查验证、IIS 配置要点 |
