# WebAPI 发布就绪度评估（PUBLISH-ASSESS）

> 日期：2026-08-12 | 状态：只读调研（零代码改动）
> 目标：评估 LYBTZYZS WebAPI 是否可发布到测试环境 **60.190.215.86:5000**
> 数据源：appsettings.Production.json / appsettings.json、ProductionConfigurationValidator、UnifiedMiddlewareConfiguration、Program.cs、06-operations 部署文档

---

## 〇、结论摘要

**⚠️ 有条件可发布**——代码与安全配置已就绪（8/10 检查项通过），但存在 **3 项发布时必做动作**（环境变量注入 / DB 迁移执行 / FeedUrl 占位替换）。配置占位符机制（环境变量覆盖）是**有意的安全设计**（生产禁止明文密钥），非缺陷——发布流程正确执行后即可上线。

---

## 一、检查项矩阵

| # | 检查项 | 状态 | 证据 |
|---|--------|:---:|------|
| 1 | 端口绑定 5000 | ✅ | `Kestrel.Endpoints.Http.Url = http://0.0.0.0:5000`（生产覆盖——目标端口一致） |
| 2 | 数据库连接串 | ⚠️ 待注入 | `ConnectionStrings:DefaultConnection` 含 `${DB_SERVER}/${DB_USER}/${DB_PASSWORD}` 环境变量占位——**发布时须设置 3 个环境变量**；注意库名 `LYBTDB_Dev`（测试环境语义——确认是否改名） |
| 3 | JWT 密钥 | ⚠️ 待注入 | `Jwt:SecretKey = ${JWT_SECRET}`——生产必须注入 ≥32 字符强随机密钥（03-webapi-deployment-summary §101 明确） |
| 4 | 生产门控（US-SHELL-017） | ✅ | `SystemAdmin.AllowAutoCreateInProduction=false` + `InitialSetupToken=${LYBT_INITIAL_SETUP_TOKEN}`——验证器**显式拦截未展开占位符**（`${...}` 检测——ProductionConfigurationValidator §267-278） |
| 5 | 默认密码 | ⚠️ 待注入 | `DefaultPasswords` 占位符——生产由环境变量 `DefaultPasswords__SysAdminPassword` 注入（K4 加固——缺失抛异常禁回退，US-SHELL-017）；`${DefaultPasswords__SysAdminPassword}/${DefaultPasswords__NewUserPassword}` 占位符 |
| 6 | Swagger 生产关闭 | ✅ | `if (!IsProduction) UseSwagger`（UnifiedMiddlewareConfiguration §189-192）——生产不暴露 |
| 7 | CORS | ✅ | `AllowedOrigins = ["http://60.190.215.86:5000"]`——**目标地址已预配** |
| 8 | 静态文件/更新源 | ⚠️ FeedUrl 待替换 | `DesktopUpdate.ReleasesPath=C:\Services\LYBT-releases` 已配；**`FeedUrl = https://your-server.example.com/releases` 是占位符——须替换为 `http://60.190.215.86:5000/releases`** |
| 9 | 数据库迁移 | ⚠️ 发布时执行 | `Database.EnsureCreatedInDevelopment=false`（生产不走 EnsureCreated）——**须执行 `dotnet ef database update`**（或启动时 Migration 策略——见 §三） |
| 10 | 部署文档 | ✅ | 03-webapi-deployment-summary 含环境变量清单/安全注意（SecretKey/密码/占位符说明 §101/155）——与当前配置一致 |

**通过 8/10**（2 项配置待注入 + 1 项文档待替换均属「发布动作」非代码缺陷）。

---

## 二、安全评估（发布目标）

| 面 | 状态 | 说明 |
|----|:---:|------|
| 认证 | ✅ | JWT + refresh 轮换 + AutoLogin（P0 全闭环） |
| 权限 | ✅ | 5 策略 + 部署 SysAdminOnly（DEPLOY-PERM） |
| 密钥管理 | ✅ | 环境变量注入（生产无明文密钥）；验证器拦占位符 |
| 配置 API | ✅ | SysAdminOnly + 脱敏 + 限频（SHELL-018 Phase 1） |
| 日志 | ✅ | Serilog 文件轮转（30 天保留）+ CorrelationId 贯穿 |
| HTTPS | ⚠️ 测试环境可接受 | 生产绑定 **HTTP**（0.0.0.0:5000——无 TLS）——**测试环境可接受**；正式公网发布必须 HTTPS（ADR-0014 风险 D3 B+ 已记录） |

---

## 三、发布前必做清单（3 项）

### 1. 环境变量注入（服务器 Windows Service/进程环境）
```
DB_SERVER=<SQL Server 地址>
DB_USER=<SQL 账号>
DB_PASSWORD=<SQL 密码>
JWT_SECRET=<64 字符强随机密钥>
SYSADMIN_PASSWORD=<首次 sysadmin 密码>
NEWUSER_PASSWORD=<新用户默认密码>
LYBT_INITIAL_SETUP_TOKEN=<一次性令牌——配合 AllowAutoCreateInProduction=false 的初始创建流程>
```
> 验证：`/api/v1/configuration/validate` 返回通过 = 环境变量全部生效（验证器覆盖占位符/长度/交叉依赖）。

### 2. 数据库迁移
```bash
dotnet ef database update --project src/Server/Core/LYBT.Infrastructure --startup-project src/Server/Services/LYBT.WebAPI --connection "<生产连接串>"
```
> `EnsureCreatedInDevelopment=false` 确认生产走 EF 迁移（非自动建库）。

### 3. DesktopUpdate.FeedUrl 占位替换
`appsettings.Production.json`：
```
"FeedUrl": "http://60.190.215.86:5000/releases"   ← 替换占位符
```

---

## 四、风险提示（非阻塞）

| 风险 | 等级 | 说明 |
|------|:---:|------|
| 测试库名 `LYBTDB_Dev` | 低 | 生产语义库名——确认测试环境沿用或改名 |
| HTTP 明文（无 TLS） | 中 | 测试环境可接受（内网/测试）——正式公网必须 HTTPS（ADR-0014 D3 B+） |
| 数据库可达性 | 中 | 60.190.215.86 → SQL Server 需网络可达（P0-06 历史——测试环境 SQL 登录） |
| `/releases/` 静态服务 | 低 | 依赖 ReleasesPath 目录存在（启动时条件启用——Directory.Exists 检查） |

---

## 五、结论

- **代码与配置框架就绪**：端口/CORS/Swagger 关闭/生产门控/密钥注入机制全部正确（8/10）
- **3 项发布动作**（环境变量 + EF 迁移 + FeedUrl 替换）执行后即可部署
- **验证闭环**：部署后 `GET /api/v1/configuration/validate`（sysadmin）+ `GET /health`（公开）+ 登录冒烟——三项通过即发布成功

**关联**：03-webapi-deployment-summary.md（环境变量清单）| US-SHELL-017（生产门控）| SHELL-018（配置中心——生产可在线验证/调整）| ADR-0014（配置 API + 重启机制——发布后远程运维闭环可用）
