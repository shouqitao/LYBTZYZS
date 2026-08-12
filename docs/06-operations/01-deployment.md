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
| **Linux 直接运行** | nohup 后台进程，轻量 | 见下方 Linux 部署 |
| Windows Service | 独立进程，开机自启 | — |
| IIS | 需要 IIS 环境，图形化管理 | 备选方案 |

两种方式均使用 `dotnet publish` 产出部署包，通过 FlashFXP（SFTP）上传至服务器。

---

### Linux 公网服务器部署

> **当前生产环境**: 60.190.215.86 (Ubuntu)

#### 服务器信息

| 项目 | 值 |
|------|-----|
| IP | 60.190.215.86 |
| SSH 端口 | 5555 |
| 用户名 | player |
| 认证方式 | 密码认证（FlashFXP SFTP） |
| 操作系统 | Linux (Ubuntu) |
| dotnet 路径 | /home/player/.dotnet/dotnet |
| 部署路径 | /home/player/lybt-api |
| API 端口 | 5000 |
| 数据库 | SQL Server @ 192.168.190.243 |

#### FlashFXP 连接配置

```
协议：SFTP over SSH
主机：60.190.215.86
端口：5555
用户：player
密码：（从密钥管理获取）
```

#### 部署步骤（FlashFXP + SSH）— 完整 Runbook

> **2026-08-12 测试发布实战验证**：以下步骤是实际发布全过程（含环境变量配置），照此执行即可成功发布。

**第一步：本地编译**

```bash
dotnet publish src/Server/Services/LYBT.WebAPI -c Release -o ./publish-webapi
# 产出：D:\source\repos\LYBTZYZS\publish-webapi\（约 43MB / 138 文件）
```

**第二步：FlashFXP 上传**

1. 打开 FlashFXP → 连接 60.190.215.86:5555（SFTP）
2. 左侧窗格：定位到本地 `./publish-webapi/` 目录
3. 右侧窗格：定位到远程 `/home/player/lybt-api/`
4. 全选左侧文件 → 拖拽到右侧（覆盖旧文件）

**第三步：配置环境变量（start.sh）**

> ⚠️ **这是发布成功的关键**——`appsettings.Production.json` 含 `${VAR}` 占位符，服务启动前必须注入实际值（否则启动校验拦截）。完整 start.sh 内容：

```bash
#!/bin/bash
# LYBT WebAPI 测试环境启动脚本
# 位置：/home/player/lybt-api/start.sh
# 用法：bash start.sh

export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__DefaultConnection="Server=192.168.190.243;Database=LYBTDB_Dev;User ID=sa;Password=<SQL密码>;Encrypt=False;TrustServerCertificate=True;Connection Timeout=30;"
export Jwt__SecretKey="<Base64 编码密钥，见踩坑清单 #2>"
export DefaultPasswords__SysAdminPassword="<初始 sysadmin 密码>"
export DefaultPasswords__NewUserPassword="<新用户默认密码，含小写+数字>"
export SystemAdmin__AllowAutoCreateInProduction="false"
export SystemAdmin__InitialSetupToken="<一次性令牌>"
export Security__RateLimiting__Enabled="false"

cd /home/player/lybt-api
pkill -f "dotnet.*LYBT.WebAPI.dll" 2>/dev/null
sleep 2
setsid nohup /home/player/.dotnet/dotnet LYBT.WebAPI.dll --environment Production > logs/webapi.log 2>&1 < /dev/null &
echo "LYBT WebAPI started, PID=$!"
```

> 若服务器已有 start.sh，只需修改其中密码/密钥值后执行；`setsid` 确保进程脱离 SSH 会话不被回收。

**第四步：重启服务**

```bash
ssh -p 5555 player@60.190.215.86
cd /home/player/lybt-api && bash start.sh
exit
```

**第五步：完整验证（发布成功判定）**

```bash
# 1. 服务进程
pgrep -af "LYBT.WebAPI.dll"

# 2. 健康检查（含数据库）
curl http://localhost:5000/health          # 期望: Healthy
curl http://localhost:5000/health/database # 期望: Healthy

# 3. 登录冒烟（测试环境默认密码）
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"sysadmin","password":"<初始密码>"}'
# 期望: {"success":true,"data":{"token":"eyJ..."}}

# 4. 下载主页
curl -o /dev/null -w "%{http_code}" http://localhost:5000/  # 期望: 200

# 5. Swagger（测试环境开）
curl -o /dev/null -w "%{http_code}" http://localhost:5000/swagger  # 期望: 301→200
```

> **全部 5 项通过 = 发布成功**（2026-08-12 测试发布即按此验证全绿）。

#### 配置注意事项

1. **HTTPS 端点**: 已移除，仅使用 HTTP（生产环境由反向代理处理 TLS）
2. **密码策略**: 必须包含大小写字母和数字
3. **环境变量占位符**: Production 配置中不可使用 `${VAR}` 格式，需写入实际值
4. **dotnet 路径**: 必须使用完整路径 `/home/player/.dotnet/dotnet`

#### 常用运维命令

```bash
# SSH 连接
ssh -p 5555 player@60.190.215.86

# 查看服务状态
ps aux | grep dotnet

# 查看日志
tail -50 /home/player/lybt-api/logs/lybt-web-api-*.log

# 健康检查
curl http://60.190.215.86:5000/health

# 查看端口
ss -tlnp | grep 5000
```

### Desktop 客户端发布

WebAPI 同时提供 Desktop 客户端发布包下载服务（**Velopack 更新源**——US-SHELL-010）。

#### 服务器目录结构

```
/home/player/lybt-api/              ← WebAPI 运行目录
/home/player/lybt-releases/         ← Velopack 发布包（独立目录，不被 publish 清空）
    ├── Setup.exe                     ← 安装包（免管理员权限，%LocalAppData%\LYBT）
    ├── RELEASES                      ← Velopack 更新清单
    └── lybt-desktop-*.nupkg          ← 更新包（增量/全量）
```

#### 打包流程（开发者）

1. **本地打包**：
   ```bash
   # Windows（需 vpk CLI）
   scripts/velopack-pack.ps1 -Version <版本号>

   # 或手动：
   dotnet publish src/Client/Desktop -c Release -r win-x64 --self-contained true -o ./publish-desktop
   vpk pack --packId lybt-desktop --packDir ./publish-desktop --version <版本>
   ```

2. **FlashFXP 上传**：
   - 左侧：本地 `./releases/`（vpk 产出）
   - 右侧：远程 `/home/player/lybt-releases/`
   - 全选拖拽上传

3. **验证**：
   - 浏览器打开 `http://60.190.215.86:5000/`（下载页 → 下载 Setup.exe）
   - 下载安装 → 启动 → 检查版本号

#### 升级机制（Desktop 客户端）

1. 启动时 `DesktopUpdateStartupStep` 后台检查（Velopack UpdateManager ← `DesktopUpdate:FeedUrl`）
2. 发现新版本 → 提示用户 → 下载更新包 → 重启应用完成更新
3. 服务端 `GET /` 提供下载主页（公开——显示 Setup.exe 下载 + 版本号）；`/releases/` 静态托管更新源

> **关键设计**：Releases 目录与 WebAPI 部署目录**完全独立**，确保 `dotnet publish` 不会清空历史发布包。

### 环境变量参考

| 变量名 | 默认值 | 说明 |
|--------|--------|------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | 运行环境（Development / Staging / Production） |
| `ASPNETCORE_URLS` | `http://localhost:5000` | 监听地址和端口 |
| `ConnectionStrings__DefaultConnection` | — | SQL Server 连接字符串（覆盖 appsettings） |
| `ConnectionStrings__LYBTDesktop` | — | LocalDB 连接字符串（Desktop 嵌入式服务） |
| `Jwt__SecretKey` | — | JWT 签名密钥（≥32 字符，生产环境必须覆盖） |
| `Jwt__Issuer` | `LYBT.WebAPI` | JWT 签发者 |
| `Jwt__Audience` | `LYBT.Client` | JWT 受众 |
| `Jwt__AccessTokenExpirationMinutes` | `480` | Access Token 有效期（分钟） |
| `DefaultPasswords__SysAdminPassword` | — | sysadmin 默认密码（生产环境必须覆盖） |
| `DefaultPasswords__NewUserPassword` | — | 新用户默认密码（生产环境必须覆盖） |

> **测试/生产密码策略（2026-08-12）**：
> - **测试环境**：可用默认密码（或临时环境变量）验证功能——不敏感，正式发布前更换即可
> - **正式发布**：必须通过环境变量 `SYSADMIN_PASSWORD` / `NEWUSER_PASSWORD` 注入强随机密码（≥12 位混合大小写+数字），禁止沿用测试默认值
> - 生产门控（US-SHELL-017）：`AllowAutoCreateInProduction=false` + `InitialSetupToken` 一次性令牌——确保首次创建 sysadmin 走受控流程
| `DOTNET_ENVIRONMENT` | — | .NET 运行环境（备选） |

> **双下划线约定**：ASP.NET Core 通过 `__`（双下划线）分隔层级来覆盖 JSON 配置节。例如 `ConnectionStrings__DefaultConnection` 覆盖 `ConnectionStrings:DefaultConnection`。

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
    -Body '{"username":"sysadmin","password":"<从密钥管理获取，勿硬编码>"}' | Select-Object -ExpandProperty token
$headers = @{ Authorization = "Bearer $token" }
$details = Invoke-RestMethod -Uri "http://localhost:5000/api/v1/health/details" -Headers $headers
Write-Host "DB Status: $($details.database.status), Duration: $($details.database.duration)ms"

# 5. 验证端口监听
netstat -ano | findstr ":5000"
```

### 发布踩坑清单（Pitfalls）

> **2026-08-12 测试发布实战记录**——以下问题均在真实部署中发生，按「问题 → 现象 → 原因 → 规避」记录，供后续发布（含正式发布）直接参考。

| # | 问题 | 现象 | 根因 | 规避方法 |
|---|------|------|------|---------|
| 1 | **环境变量键名必须双下划线** | `JWT_SECRET` 设置了但校验报「未配置」 | 配置读取用 `Jwt__SecretKey`（ASP.NET Core 双下划线覆盖），不是大写单层名 | start.sh 统一用双下划线键：`Jwt__SecretKey`、`ConnectionStrings__DefaultConnection`、`SystemAdmin__InitialSetupToken` |
| 2 | **JWT 密钥必须 Base64 编码** | 启动报「JWT SecretKey 必须是有效的 Base64 字符串」 | `JwtOptions` 校验器要求 Base64 格式（≥32 字符） | 用 `python3 -c "import base64,os; print(base64.b64encode(os.urandom(48)).decode())"` 生成 |
| 3 | **缺默认密码环境变量** | 启动报「新用户密码不符合安全策略」 | `DefaultPasswords__NewUserPassword` 未设置，校验器要求小写+数字 | start.sh 同时设 `SysAdminPassword` + `NewUserPassword` |
| 4 | **DB 连接串 Encrypt 兼容** | 启动后 health Unhealthy / 登录 500，日志 `pre-login handshake error 35` | SQL Server 不支持强制加密（`Encrypt=True`），TLS 握手失败 | 内网/测试库用 `Encrypt=False;TrustServerCertificate=True` |
| 5 | **路由模板重复 version** | 启动崩溃 `route parameter 'version' appears more than one time` | 类级路由含 `api/v{version}` 且动作级又写完整前缀（CatalogController formulas 段——相对模板拼接出双 version） | 动作级写**绝对路径**（`/` 开头——覆盖类级前缀，version 参数仅一次；相对路径会拼出 `/api/v1/herbs/formulas` 错误语义）；已修（1f4f54a91） |
| 6 | **模块 DbContext 漏映射** | 登录 500，日志 `列名 'LastLoginTime' 无效` | IdentityDbContext 漏 `ApplyConfiguration(UserConfiguration)`，实体属性未映射到列 | 已修（a3ab01417）；新增模块 DbContext 必须注册实体配置 |
| 7 | **健康检查连接串 fallback** | `/health/database` Unhealthy「连接字符串未配置」 | SqlServerHealthCheck 只读 `Database:ConnectionString`，不读 `ConnectionStrings:DefaultConnection` | 已修（8d02ed365）——DatabaseConnectionResolver fallback 链 |
| 8 | **Swagger 空白页** | `/swagger` 跳转 index 后空白 | 生产严格 CSP（`require-trusted-types-for 'script'`）阻止 SwaggerUI 渲染 | `/swagger` 路径 CSP 豁免（保留核心防护）；已修（b1c2bf2bb） |
| 9 | **Swagger 生产默认关** | 测试环境看不到 API 清单 | `!IsProduction()` 才启用 Swagger，测试环境环境名是 Production | `Swagger:Enabled=true` 配置开关（测试开/正式关） |
| 10 | **SSH 密码认证** | FlashFXP 连接失败 | 服务器 `sshd_config PasswordAuthentication no` | 服务器开启 `PasswordAuthentication yes` + `systemctl restart sshd` |

> **代码-文档一致性约定（2026-08-12 确立）**：每次代码/配置变更（尤其部署相关——环境变量键名、校验规则、安全头、路由）必须同步本清单与对应文档；本清单是后续用户手册/运维手册的素材来源，不得滞后于代码。

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

- SQL Server LocalDB 数据库: `%LOCALAPPDATA%\LYBTZYZS\data\lybt-local.mdf`
- 日志文件: `%LOCALAPPDATA%\LYBTZYZS\logs\`
- 配置文件: `%LOCALAPPDATA%\LYBTZYZS\config\`

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
| 本地模式数据丢失 | LocalDB 文件被删除或损坏 | 检查 `%LOCALAPPDATA%\LYBTZYZS\data\` 目录 |
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
| 2026-07-13 | v1.4 | 新增 Linux 公网服务器部署章节 (60.190.215.86) |
