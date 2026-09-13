# 部署指南
> 版本: v1.0 | 日期: 2026-08-20

> 部署架构总览见 [README.md](./README.md)

本文档覆盖服务端（Linux 公网服务器）和 Desktop 客户端（Velopack 分发）的部署流程，包括编译发布、配置管理、环境变量参考和故障排查。

---

## 服务端部署

### 发布形式

| 形式 | 说明 | 适用场景 |
|------|------|----------|
| **Framework-Dependent**（推荐） | 依赖 .NET 8 Runtime，包小 (~27MB) | 有服务器管理权限 |
| Self-Contained | 自带运行时，包大 (~70MB) | 无法控制目标环境 |

### 运行方式

| 方式 | 说明 |
|------|------|
| **Linux 直接运行**（当前生产） | nohup 后台进程，轻量 |
| Windows Service | 独立进程，开机自启 |
| IIS | 备选方案 |

---

## Linux 公网服务器部署

> **当前生产环境**: 60.190.215.86 (Ubuntu)

| 项目 | 值 |
|------|-----|
| SSH | `player@60.190.215.86:5555` (SFTP) |
| 部署路径 | `/home/player/lybt-api` |
| API 端口 | 5000 |
| 数据库 | SQL Server @ 192.168.190.243 |

### 部署步骤

**1. 本地编译**
```bash
dotnet publish src/Server/Services/LYBT.WebAPI -c Release -o ./publish-webapi
```

**2. FlashFXP 上传** — 左侧 `./publish-webapi/` → 右侧 `/home/player/lybt-api/`（覆盖）

**3. 配置环境变量（start.sh）** — 核心变量：
```bash
export ASPNETCORE_ENVIRONMENT=Production
export ConnectionStrings__DefaultConnection="Server=192.168.190.243;Database=LYBTDB_Dev;..."
export Jwt__SecretKey="<Base64 编码密钥>"
export DefaultPasswords__SysAdminPassword="<密码>"
export DefaultPasswords__NewUserPassword="<密码>"
export SystemAdmin__AllowAutoCreateInProduction="false"
export SystemAdmin__InitialSetupToken="<一次性令牌>"
export Security__RateLimiting__Enabled="false"
```

**start.sh 要点**: PID 文件防重复 + 端口释放确认(≤30s) + health 探测(≤60s) + `setsid nohup` 脱离 SSH 会话。完整脚本含四层防护（US-SHELL-024），Program.cs Mutex `Global\LYBTZYZS_WebAPI_Instance` 单实例兜底。

**4. 重启 & 验证**
```bash
ssh -p 5555 player@60.190.215.86
cd /home/player/lybt-api && bash start.sh

# 验证: pgrep -af LYBT.WebAPI + curl localhost:5000/health + 登录冒烟
```

**全部通过 = 发布成功**。

### 配置原则

> **配置跟着变量走** — `appsettings.{环境}.json` + 环境变量覆盖是唯一机制。

| 环境 | sysadmin 密码来源 |
|------|-------------------|
| 本机开发 | appsettings.Development.json `DevPass123!` |
| 自动化测试 | appsettings.Test.json `TestAdmin2025@` |
| **服务器测试** | **start.sh `DefaultPasswords__SysAdminPassword`** |
| 正式生产 | 环境变量注入强随机密码 |

**环境名设置**（只从环境变量/启动参数读取，不从配置文件）：
```
--environment 命令行 > ASPNETCORE_ENVIRONMENT > DOTNET_ENVIRONMENT > 默认 Production
```

### 配置优先级

**环境变量 > runtime-overrides.json > appsettings.{Environment}.json > appsettings.json**

- 占位符 `${VAR}` 未展开或空串 → 视为无效 → 回退下一级
- 配置文件缺失时自动生成默认模板（不覆盖已有文件）

### 配置目录（ADR-0019）

发布产物配置集中在 `config/` 子目录：
```
{BaseDir}/config/
├── appsettings.json / appsettings.{env}.json
├── clinic.config.json
└── runtime-overrides.json（运行时写）
```

### HTTP/HTTPS 双协议

- Http `0.0.0.0:5000` 默认开启，Https `0.0.0.0:5001` 默认关闭
- 配置在 `Server:Endpoints` 段（非 `Kestrel:Endpoints`）
- 启用 HTTPS: `Https:Enabled=true` + 填证书路径 → 重启

### 热更新安全（P0-2，2026-08-21）

- `DeployService.SaveUpdatePackageAsync` 计算 ZIP 的 SHA256 并写入 `AppContext.BaseDirectory/.update-pending.sha256`（侧车），同时写入 `.update-pending` 指向 zip 路径
- `Program.Main` 热更新段启动时若存在 `.update-pending.sha256` 则强制校验 `ComputeSHA256(zipPath) == expected`，不一致 `Log.Fatal` + `Environment.Exit(1)` 拒绝解压（防篡改/RCE）
- 校验失败不解压、不删 zip 便于取证；成功后双 flag 均清理

### 双 JWT 密钥隔离校验（P0-4，ADR-0024，2026-08-21）

- 远程 `Jwt:SecretKey`（`JwtOptions`）与本地 `Jwt:SecretKey`（`LocalJwtOptions` 同节名但分宿主）生产**必须不同值**（`LocalJwtOptionsValidator` 强校验）
- 发布前 `grep -c "Jwt__SecretKey" start.sh` 确认双值不同；相同值启动 `Fatal`
- 本地 `SecurityAuditLogs` 已 SQLite 落库，联网后同步（v2.0 前先可追溯）

### 患者敏感字段加密密钥（P1-9，2026-08-21）

- `Security__AesKey`（Base64 32B，`SecurityOptions.AesKey`）生产必须配置，缺省回退测试固定密钥仅开发/测试
- 发布前校验：`python3 -c "import base64,os; print(base64.b64encode(os.urandom(32)).decode())"` 生成并注入
- 变更后历史明文自动回退解密，无需手动数据迁移（ValueConverter 容错）

### 发布前门禁

```bash
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~WebApiSystemTests|FullyQualifiedName~DeploymentConfigTests"
```
通过 = 启动无崩溃 + 路由注册全 + FallbackPolicy 401 + 配置契约校验。

热更新校验单测：`dotnet test --filter HotUpdateSha` 绿（篡改检测）

### Newman 验证

```bash
npx newman run tests/newman/lybt-full-api-collection.json -e tests/newman/env-full-test.json --timeout-request 15000
```

### 常用运维命令

```bash
ssh -p 5555 player@60.190.215.86
ps aux | grep dotnet                         # 服务状态
tail -50 /home/player/lybt-api/logs/*.log   # 日志
curl http://60.190.215.86:5000/health       # 健康检查
ss -tlnp | grep 5000                        # 端口
```

### ForceResetOnStartup

`SystemAdmin:ForceResetOnStartup=true` 时启动重置 sysadmin 密码。开发环境直接触发；非开发环境需 `InitialSetupToken` 验证。

---

## 发布踩坑清单

| # | 问题 | 根因 | 规避 |
|---|------|------|------|
| 1 | 环境变量键名必须双下划线 | ASP.NET Core `__` 覆盖机制 | start.sh 统一用双下划线 |
| 2 | JWT 密钥必须 Base64 | JwtOptions 校验器要求 | `python3 -c "import base64,os; print(base64.b64encode(os.urandom(48)).decode())"` |
| 3 | 缺默认密码环境变量 | 校验器要求小写+数字 | start.sh 同时设两个密码 |
| 4 | DB 连接串 Encrypt 兼容 | SQL Server 不支持强制加密 | `Encrypt=False;TrustServerCertificate=True` |
| 5 | 路由模板重复 version | 动作级和类级都写前缀 | 动作级写绝对路径 |
| 6 | 模块 DbContext 漏映射 | ApplyConfiguration 遗漏 | 新增模块必须注册实体配置 |
| 7 | 健康检查连接串 fallback | 只读一个配置键 | DatabaseConnectionResolver fallback 链 |
| 8 | Swagger 空白页 | 生产 CSP 阻止渲染 | `/swagger` 路径 CSP 豁免（仅在 Swagger 启用时生效） |
| 9 | Swagger 生产默认关 | 测试发布与正式发布同用 Production 环境名 | 配置文件默认 `Swagger:Enabled=false`；测试发布注入环境变量 `Swagger__Enabled=true`（免改文件） |
| 10 | SSH 密码认证失败 | PasswordAuthentication no | 开启 PasswordAuthentication yes |
| 11 | 全局 SplitQuery + 远程 SQL | 多连接超时 | 移除全局 SplitQuery，改查询级 |
| 12 | 多进程/端口占用 | 脚本层无防护 | start.sh 四层防护 + Program.cs Mutex |
| 13 | 本地模式首启种子失败报「User security stamp cannot be null」 | `EnsureUserAsync` 忽略 `CreateAsync` 结果；占位符密码先触发策略校验失败（在写 SecurityStamp 之前）→ 后续角色分配报与真因无关的 stamp 错误 | ① 种子校验 `CreateAsync` 结果并抛可执行错误（含配置键与密码策略）；② 本地模式注入合规 `DefaultPasswords__SysAdminPassword/NewUserPassword`（见 `05-development/01-setup.md` 本地模式） |

> **代码-文档一致性约定**：每次部署相关变更必须同步本清单与对应文档。

### Swagger 在线启用（测试发布）

生产默认关闭（`config/appsettings.Production.json` → `Swagger:Enabled=false`，安全默认，发布前无需手工改回）。
测试服务器需在线查看 API 文档时，在启动环境注入环境变量（配置源优先级：环境变量 > JSON 文件）：

```bash
# /home/player/lybt-api/.env（Program.cs 自动加载）或 systemd Environment=
Swagger__Enabled=true
```

| 状态 | `/swagger`（UI） | `/swagger/v1/swagger.json` | 下载主页「API 文档」入口 | `/swagger` CSP |
|------|------------------|---------------------------|------------------------|----------------|
| 生产默认（Enabled=false） | 401（FallbackPolicy） | 401 | 不显示（防死链） | 严格（不豁免） |
| 生产 + `Swagger__Enabled=true` | 200 | 200 | 显示 | 豁免 `script-src 'unsafe-eval'`（SwaggerUI 渲染所需） |
| 非生产（Development/Test） | 200 | 200 | 显示 | 同上 |

> 实测（2026-09-12 B-16）：生产默认与注入 env 两态均按上表验证；开关判定 SSOT =
> `Configuration/SwaggerAvailability.IsEnabled`（中间件装配 / 匿名兜底端点 / CSP 豁免 / 下载主页入口四处共用）。

---

## Desktop 客户端部署

### 打包

```bash
dotnet publish src/Client/Desktop -c Release -r win-x64 --self-contained true -o ./publish-desktop
vpk pack --packId lybt-desktop --packDir ./publish-desktop --version <版本>
```

### 服务器目录

```
/home/player/lybt-releases/    ← Velopack 发布包（独立于 WebAPI 目录）
    ├── Setup.exe               ← 安装包（免管理员，%LocalAppData%\LYBT）
    ├── RELEASES                ← 更新清单
    └── lybt-desktop-*.nupkg    ← 更新包
```

### 升级机制

启动时 `DesktopUpdateStartupStep` 后台检查 → 发现新版本 → 提示用户 → 下载更新 → 重启完成。

---

## 客户端本地模式数据

- 数据库: `%LOCALAPPDATA%\LYBTZYZS\data\lybt-local.mdf`
- 日志: `%LOCALAPPDATA%\LYBTZYZS\logs\`
- 配置: `%LOCALAPPDATA%\LYBTZYZS\config\`

---

## 环境变量参考

| 变量名 | 默认值 | 说明 |
|--------|--------|------|
| `ASPNETCORE_ENVIRONMENT` | `Production` | 运行环境 |
| `ASPNETCORE_URLS` | `http://localhost:5000` | 监听地址 |
| `ConnectionStrings__DefaultConnection` | — | SQL Server 连接串 |
| `Jwt__SecretKey` | — | JWT 签名密钥 (≥32字符) |
| `Jwt__AccessTokenExpirationMinutes` | `480` | Access Token 有效期 |
| `DefaultPasswords__SysAdminPassword` | — | sysadmin 默认密码 |
| `DefaultPasswords__NewUserPassword` | — | 新用户默认密码 |

> **双下划线约定**: `__` 分隔层级覆盖 JSON 配置。如 `ConnectionStrings__DefaultConnection` 覆盖 `ConnectionStrings:DefaultConnection`。

---

## IIS 配置要点

1. 安装 ASP.NET Core Hosting Bundle
2. 应用池：.NET CLR "无托管代码"，管道"集成"
3. 请求超时：`executionTimeout` 调大（报表导出等长操作）
4. WebSocket：如用 SignalR 需启用

> 推荐直接 Kestrel，IIS 仅备选。

## Windows Server 部署

```powershell
dotnet publish src/Server/Services/LYBT.WebAPI -c Release -o C:\Services\LYBT-API --self-contained false
sc.exe create LYBT-API binPath= "C:\Services\LYBT-API\LYBT.WebAPI.exe" start= auto
```

**SQL Server**: `CREATE DATABASE LYBTDB_Dev` → `dotnet ef database update`

---

## 数据库运维

```bash
# 生成迁移
dotnet ef migrations add <Name> -p src/Server/Core/LYBT.Infrastructure -s src/Server/Services/LYBT.WebAPI
# 应用迁移
dotnet ef database update -s src/Server/Services/LYBT.WebAPI
```

### P2-06-5 DDL 权限 Runbook（2026-08-21）
- **Serilog MSSql Sink** `AutoCreateSqlTable=true` 首次启动会建 `Logs` 表，需 DB 账号有 `CREATE TABLE` 权限；生产建议预建表并设 `AutoCreateSqlTable=false`（`LoggingBootstrap` 仅首次建表，失权限则启动失败，`06-operations` 未提示）。
- **迁移**需 `db_owner` 或 `db_ddladmin + db_datawriter/reader`；CI 迁移前 `dotnet ef database update` 校验 `__EFMigrationsHistory` 一致性。

---

## 故障排查

| 症状 | 可能原因 | 解决方案 |
|------|----------|----------|
| 启动即退出 | 配置缺失/JSON 错误 | 检查 appsettings.json |
| "Connection refused" | SQL Server 未启动 | 确认服务运行+连接串 |
| "Invalid JWT SecretKey" | 密钥不足 32 字符 | 用脚本生成 |
| 端口被占 | 其他进程 | `ss -tlnp | grep 5000` |
| 迁移失败 | 数据库版本不匹配 | `dotnet ef database update` |
| 登录后白屏 | API 地址配置错误 | 检查 Server URL |
| "Token Expired" 频繁 | 时钟偏差 | 同步时间或调大 ClockSkew |
