# 部署指南

本文档包含服务端部署、客户端部署和数据库运维的详细操作说明。

> 部署架构总览见 [README.md](./README.md)

---

## 服务端部署

### 发布形式

| 形式 | 说明 | 适用场景 |
| ------ | ------ | --------- |
| **Framework-Dependent**（推荐） | 依赖目标机器 .NET 8 Runtime，包小 (~27MB) | 有服务器管理权限的场景 |
| Self-Contained | 自带运行时，包大 (~70MB) | 无法控制目标机器环境的场景 |

### 运行方式

| 方式 | 说明 | 文档 |
| ------ | ------ | ------ |
| **Linux 直接运行** | nohup 后台进程，轻量 | 见下方 Linux 部署 |
| Windows Service | 独立进程，开机自启 | — |
| IIS | 需要 IIS 环境，图形化管理 | 备选方案 |

两种方式均使用 `dotnet publish` 产出部署包，通过 FlashFXP（SFTP）上传至服务器。

---

### Linux 公网服务器部署

> **当前生产环境**: 60.190.215.86 (Ubuntu)

#### 服务器信息

| 项目 | 值 |
| ------ | ----- |
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
# LYBT WebAPI 测试环境启动脚本（2026-08-13 v3——US-SHELL-024 四层防护：PID 文件 + 端口释放确认 + health 探测 + 部署日志）
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
PID_FILE=lybt-api.pid
PORT=5000

echo "[部署] ====== LYBT WebAPI 部署开始: $(date '+%F %T') ======"

# ── 第 1 层：停止旧进程（PID 文件优先，校验防 PID 复用；无 PID 文件回退 pkill）──
OLD_PID=""
if [ -f "$PID_FILE" ]; then
    OLD_PID=$(cat "$PID_FILE")
    if [ -n "$OLD_PID" ] && [ -d "/proc/$OLD_PID" ] && grep -q "LYBT.WebAPI.dll" "/proc/$OLD_PID/cmdline" 2>/dev/null; then
        echo "[部署] 停止旧进程 PID=$OLD_PID（来自 $PID_FILE）"
        kill "$OLD_PID" 2>/dev/null
    else
        echo "[部署] PID 文件存在但进程 $OLD_PID 非 WebAPI 或已退出（防 PID 复用——忽略，回退 pkill）"
        OLD_PID=""
    fi
    rm -f "$PID_FILE"
fi
if [ -z "$OLD_PID" ]; then
    pkill -f "dotnet.*LYBT.WebAPI.dll" 2>/dev/null
    echo "[部署] 停止旧进程（pkill 回退路径）"
fi

# ── 第 2 层：端口释放确认（≤30s；超时 kill -9；仍占用 → 报错退出不启动）──
for i in $(seq 1 30); do
    if ! ss -tlnp 2>/dev/null | grep -q ":$PORT "; then
        echo "[部署] 端口已释放（$PORT 空闲，等待 ${i}s）"
        break
    fi
    if [ "$i" -eq 15 ]; then
        echo "[部署] 端口 $PORT 等待 15s 仍占用——强制 kill -9 残留进程"
        pkill -9 -f "dotnet.*LYBT.WebAPI.dll" 2>/dev/null
    fi
    sleep 1
    if [ "$i" -eq 30 ]; then
        echo "[部署] 错误：端口 $PORT 30s 内未释放——放弃启动（可能被非 WebAPI 进程占用）"
        exit 1
    fi
done

# ── 第 3 层：启动新进程 + PID 文件 + health 探测 ──
setsid nohup /home/player/.dotnet/dotnet LYBT.WebAPI.dll --environment Production > logs/webapi.log 2>&1 < /dev/null &
NEW_PID=$!
echo "$NEW_PID" > "$PID_FILE"
echo "[部署] 启动新进程 PID=$NEW_PID（已写 $PID_FILE）"

for i in $(seq 1 60); do
    if curl -s -o /dev/null -w "%{http_code}" --max-time 3 http://localhost:$PORT/health 2>/dev/null | grep -qE "200|503"; then
        echo "[部署] 启动成功：health 探测通过（${i}s）PID=$NEW_PID"
        echo "[部署] ====== LYBT WebAPI 部署完成: $(date '+%F %T') ======"
        exit 0
    fi
    sleep 1
done

echo "[部署] 错误：health 探测 60s 未通过——打印日志尾部："
tail -30 logs/webapi.log
echo "[部署] ====== LYBT WebAPI 部署失败（见上方日志）======"
exit 1
```

> 若服务器已有 start.sh，只需修改其中密码/密钥值后执行；`setsid` 确保进程脱离 SSH 会话不被回收。
> **单实例双保险（US-SHELL-024）**：start.sh 脚本层（PID 文件+端口释放+health 探测）+ Program.cs 程序层（Mutex `Global\LYBTZYZS_WebAPI_Instance`——Linux 下 named mutex 映射 /tmp 文件锁，等价全局；已有实例直接拒绝启动 exit 1）。

> **配置原则（2026-08-12 用户确立）：配置跟着变量走**——`appsettings.{环境}.json` + 环境变量覆盖是唯一机制，环境切了值自然切。**每个环境内部必须「唯一一致」**（一个配置项一个来源，无重复冲突键）；**跨环境各走各的值**（Dev/Test/Prod 密码本来就不同——这是分文件的意义）。
>
> **测试部署密码表**（权威来源，API 测试登录用）：
>
> | 环境 | 权威来源 | sysadmin 密码 |
> | ------ | --------- | -------------- |
> | 本机开发 | appsettings.Development.json | `DevPass123!` |
> | 自动化测试 | appsettings.Test.json | `TestAdmin2025@` |
> | **服务器测试发布** | **start.sh `DefaultPasswords__SysAdminPassword`** | **= start.sh 实际值**（唯一权威，改动时同步本表） |
> | 正式生产 | 环境变量注入（正式发布时换强随机） | `${DefaultPasswords__SysAdminPassword}` 占位符 → env |
>
> ⚠️ 登录 401 时先查本表（用对应环境的权威密码），勿凭记忆猜密码。sysadmin 密码被安全设计保护（K4：生产禁默认回退 + ResetPassword 拒绝重置 sysadmin），改密只能走 ChangePassword（需旧密码）。
>
> **环境名（模式）设置——业界标准（2026-08-13 确认）**：环境名只从**环境变量/启动参数**读取，**不能从配置文件设定**（微软官方 + 社区共识）：
>
> ```
> 优先级：--environment 命令行参数（最高）
>        > ASPNETCORE_ENVIRONMENT 环境变量（标准）
>        > DOTNET_ENVIRONMENT（.NET 通用 fallback）
>        > launchSettings.json（仅开发/IDE，不随发布）
> 默认值：Production（未设置时）
> ```
>
> **为什么不能放配置文件**：环境名必须在加载配置文件**之前**确定（决定加载哪个 `appsettings.{env}.json`）——鸡生蛋问题。官方定位 `ASPNETCORE_ENVIRONMENT` 为**宿主机级配置**（host-level），不属于应用配置。
>
> **各部署方式设置环境名**：
>
> | 方式 | 做法 |
> | ------ | ------ |
> | 服务器 start.sh | `export ASPNETCORE_ENVIRONMENT=Production`（当前已用） |
> | Docker | `ENV ASPNETCORE_ENVIRONMENT=Production` 或 docker-compose `environment:` |
> | IIS | web.config `aspNetCore environmentVariables` |
> | Azure | App Settings `ASPNETCORE_ENVIRONMENT` |
> | 开发本机 | launchSettings.json（IDE 自动） |

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

1. **HTTP/HTTPS 双协议（P2-09 US-SHELL-025 2026-08-14）**: Http `0.0.0.0:5000` 默认开启 + Https `0.0.0.0:5001` 默认关闭——配置在 `config/appsettings.Production.json` 的 `Server:Endpoints` 段（非 `Kestrel:Endpoints`——避开 ASP.NET Core 内建端点绑定防双重监听）：
   - 开关：`Server:Endpoints:Http:Enabled`（默认 true）/ `Server:Endpoints:Https:Enabled`（默认 false）
   - 证书：`Server:Endpoints:Https:Certificate:Path` + `Password`（生产正式证书；Path 留空时用 `dotnet dev-certs https` 开发证书）
   - 启用 HTTPS：改 `Https:Enabled=true` + 填证书路径 → 重启 start.sh（脚本无需改——Kestrel 自动监听双端口）
   - 验证：`ss -tln | grep -E ':5000|:5001'` + `curl -k https://localhost:5001/health`
   - 启动日志显示 `[启动] Listening on http://0.0.0.0:5000`（及启用时的 Https 行）
   - 风险：生产 HTTPS 需正式证书 + 公网防火墙开放 5001；测试环境默认 HTTP（反向代理处理 TLS 更安全——旧注释的推荐路径）
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
| -------- | -------- | ------ |
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
>
> - **测试环境**：可用默认密码（或临时环境变量）验证功能——不敏感，正式发布前更换即可
> - **正式发布**：必须通过环境变量 `DefaultPasswords__SysAdminPassword` / `DefaultPasswords__NewUserPassword` 注入强随机密码（≥12 位混合大小写+数字），禁止沿用测试默认值
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

### ForceResetOnStartup 用法（2026-08-12 修复——开发/测试环境密码重置）

`SystemAdmin:ForceResetOnStartup=true` 时启动重置 sysadmin 密码及锁定状态（**PBKDF2 哈希——UserManager.ResetPasswordAsync**）：

- **开发环境**（ASPNETCORE_ENVIRONMENT=Development）：直接触发（宽松）
- **非开发环境**（如测试部署 Production 名）：需 `InitialSetupToken` 验证通过（安全门控——与 AllowAutoCreateInProduction 同模式；**不要求 AllowAutoCreateInProduction=true**——ForceReset 只重置不创建，更安全）
- 新密码来源：`DefaultPasswords:SysAdminPassword`（环境变量注入——未配置时仅重置状态并警告）
- 生产默认 false——生产必须显式开启 + token 验证，安全语义保持

```bash
# 测试部署重置 sysadmin 密码示例
export SystemAdmin__ForceResetOnStartup=true
export SystemAdmin__InitialSetupToken="<token>"
export DefaultPasswords__SysAdminPassword="<新密码>"
```

### 配置优先级（2026-08-12 CFG-BATCH2 修正）

**环境变量 > runtime-overrides.json > appsettings.{Environment}.json > appsettings.json**

- 环境变量 = 部署权威（start.sh 注入——最高优先）
- runtime-overrides.json = 运行时微调（低于部署——SHELL-018 配置中心写回目标）
- 占位符未展开（`${VAR}` 字面）或空串 → 视为无效 → 回退下一级有效值（ConfigurationPostProcessor——已知键清单：Jwt:SecretKey/DefaultConnection/密码/Token/FeedUrl）；全部无效时由配置校验器拦截提示

### 配置集中（ADR-0019 2026-08-12：Server 端 config/ 目录）

发布产物配置全部集中在 `config/` 子目录（`{BaseDir}/config/`）：

```
发布产物/
├── LYBT.WebAPI.dll + 依赖
└── config/
    ├── appsettings.json
    ├── appsettings.{env}.json（Development/Test/Production）
    ├── clinic.config.json
    └── runtime-overrides.json（运行时写，已有）
```

- 配置加载链（Program）读 `{BaseDir}/config/`——发布后稳定；缺文件自动生成模板到 config/（配置闭环）
- 优先级不变：环境变量 > runtime-overrides.json > appsettings.{env}.json > appsettings.json
- EF 工具（AppDbContextFactory）同 config/ 路径

### 配置闭环（2026-08-12：环境配置文件缺失自动生成）

WebAPI 启动时若 `appsettings.json` / `appsettings.{Environment}.json` 缺失，自动生成默认模板（含双下划线占位符）——启动顺利走到配置校验器提示注入，而非「配置空」隐晦错误。已存在文件**不覆盖**（自定义配置优先）。

```bash
# 首次部署：复制模板后替换占位符（或直接注入环境变量——模板占位符即变量名）
# 模板占位符 = 运维注入名（配置唯一化）：Jwt__SecretKey / DefaultPasswords__SysAdminPassword /
#             DefaultPasswords__NewUserPassword / SystemAdmin__InitialSetupToken /
#             ConnectionStrings__DefaultConnection
```

### 发布前门禁（决策 2026-08-12：L3 冒烟第 0 步——拦截上线坑 #1 #3 #9）

```bash
# L3 系统层冒烟（WebApplicationFactory 真实启动 Remote WebAPI）——发布前必跑
dotnet test tests/LYBT.Tests.Server/ --filter "FullyQualifiedName~WebApiSystemTests|FullyQualifiedName~DeploymentConfigTests|FullyQualifiedName~DbContextMappingSmokeTests"
# 通过 = 启动无崩溃 + 路由注册全 + FallbackPolicy 401 + 配置契约校验（JWT/占位符/密码）
# 真实 SQL 验证（列映射 #2）需 TEST_DB_CONNECTION 环境变量（测试库 192.168.190.243 LYBTDB_Test）
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
| --- | ------ | ------ | ------ | --------- |
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
| 11 | **全局 SplitQuery + 远程 SQL = 连接失败（2026-08-13 splitquery-fix）** | 种子失败（IdentitySeedData SplitQueryingEnumerable 连接错）/PUT formula 500——4 轮本地修复全过但真机全败 | 全局 `UseQuerySplittingBehavior(SplitQuery)` 把含 Include 查询拆多条 SQL 独立连接——远程 SQL（243，延迟 ~0.42s）多连接失败/超时 | 移除全局 SplitQuery（`DatabaseServiceCollectionExtensions.cs`——已修 0329bc785）改 EF 默认 SingleQuery；确有需要（大 Include 集合笛卡尔爆炸）查询级 `AsSplitQuery()`。教训：**全局配置必须与部署环境匹配**——本地低延迟测不出远程 DB 问题 |
| 12 | **多进程/端口占用（2026-08-13 P2-07 US-SHELL-024——真机 06:18 双 dotnet 并存）** | 旧进程未释放 5000 → 新进程启动失败 → 双 dotnet 并存（写入冲突风险）；start.sh 仅 pkill+sleep 2 无端口检查/无 PID 文件/无启动验证 | 脚本层无防护 + 程序层无单实例 | start.sh 四层防护（PID 文件 + 端口释放确认 ≤30s + health 探测 ≤60s + 部署日志）＋ Program.cs Mutex（`Global\LYBTZYZS_WebAPI_Instance`——Linux 下 named mutex 映射 /tmp 文件锁，已有实例 exit 1 拒绝；**真机实测通过**：双开被拒 + 单进程 + health 200）。已修（Program.cs + start.sh v3） |

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
| ------ | --------- | --------- |
| 启动即退出，无日志 | 配置文件缺失或 JSON 格式错误 | 检查 `appsettings.json` 是否存在且格式正确 |
| "Connection refused" | SQL Server 未启动或端口被占 | 确认 SQL Server 服务运行中，检查连接字符串 |
| "Invalid JWT SecretKey" | 密钥长度不足 | SecretKey 至少 32 字符 |
| 端口 5000/5001 被占 | 其他进程占用 | `netstat -ano | findstr :5000` 定位进程 |
| 迁移失败 | 数据库版本不匹配 | 运行 `dotnet ef database update` 应用待执行迁移 |

### 客户端常见问题

| 症状 | 可能原因 | 解决方案 |
| ------ | --------- | --------- |
| 登录后白屏 | API 地址配置错误 | 检查 Desktop 配置中 Server URL 是否正确 |
| 本地模式数据丢失 | LocalDB 文件被删除或损坏 | 检查 `%LOCALAPPDATA%\LYBTZYZS\data\` 目录 |
| "Token Expired" 频繁弹出 | 客户端与服务端时钟偏差过大 | 同步系统时间，或调大 `ClockSkewSeconds` |

### 数据库问题

| 症状 | 可能原因 | 解决方案 |
| ------ | --------- | --------- |
| 慢查询告警 | 缺少索引或数据量增长 | 检查 `Database.Monitoring.SlowQueryThresholdMs` 日志 |
| 连接池耗尽 | 连接泄漏或并发过高 | 检查 `MaxConnections` 配置，排查未释放的 DbContext |
| 迁移冲突 | 多人同时生成迁移 | 合并迁移文件后重新 `dotnet ef database update` |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
| ------ | ------ | ---------- |
| 2026-02-10 | v1.0 | 从 README.md 拆分，初始版本 |
| 2026-02-22 | v1.1 | 新增故障排查章节 (服务端/客户端/数据库) |
| 2026-06-25 | v1.2 | 修正 Desktop 日志路径 %APPDATA%\LYBT → %LOCALAPPDATA%\LYBTZYZS（与代码一致） |
| 2026-06-25 | v1.3 | 新增环境变量参考表、部署后健康检查验证、IIS 配置要点 |
| 2026-07-13 | v1.4 | 新增 Linux 公网服务器部署章节 (60.190.215.86) |
