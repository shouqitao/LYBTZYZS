# sysadmin 配置设计 spec（双模式区分）

> **日期**：2026-06-28
> **状态**：📝 待审
> **范围**：sysadmin 配置的远程/本地区分与各自细节——服务端配置 API（业务可改/敏感只读）+ 重启机制 + 客户端配置中心双模式面板 + 安全。**文档更新可立即执行；代码改动（ConfigurationController 扩展/重启端点/UI 面板）标「待实施」，由后续代码 plan 承载（用户「先文档不代码」）。**
> **配套**：US-SHELL-017/018、`06-operations/02-configuration.md`、ADR-0013、WebAPI `appsettings.json`

---

## [S1] 背景与目标

sysadmin 在双模式下配置对象本质不同：远程模式管「服务端(WebAPI/SQL Server/公网) + 客户端(Desktop)」两层；本地模式管「本地全栈(LocalWebAPI+LocalDB+Desktop)」。当前服务端配置无 UI/API（需登录服务器改文件），US-SHELL-018 仅管客户端。本 spec 锁定双模式配置边界与服务端 API 设计。

用户决策（2026-06-28）：服务端配置走 API（方案 B）；业务参数可改、敏感只读（方案 A）；重启走 API 延迟重启（方案 A，全远程闭环）。

---

## [S2] 服务端配置 API（仅远程模式，扩展 ConfigurationController）

**现状**：`ConfigurationController` 有 `GET /configuration`、`GET /configuration/{key}`、`POST /configuration/validate`（只读）。

**扩展**：

| 端点 | 方法 | 范围 | 说明 |
|------|------|------|------|
| `/configuration` | GET | 全部节 | 敏感项脱敏展示（SecretKey/密码/连接串显示 `***` 或掩码） |
| `/configuration/{section}` | GET | 单节 | 同上脱敏 |
| `/configuration/{section}` | **PUT** | **仅业务参数** | 见下方白名单 |
| `/configuration/validate` | POST | 全部 | 校验生产配置完整性（已有） |
| `/configuration/restart` | **POST** | — | 延迟重启（见 [S3]） |

**PUT 白名单（业务/运维参数，可改）**：
- `Session`（TimeoutMinutes/AllowConcurrentSessions/SlidingExpiration）
- `Security.RateLimiting`（各限额/窗口/白名单 IP）
- `ClinicSettings`（诊所名/营业时间/挂号费/分页/药材角色序）
- `FeatureToggles`（各功能开关）
- `SystemAdmin.SessionTimeoutMinutes`（仅会话超时；`AutoCreateOnStartup`/`AllowAutoCreateInProduction` 不可改）
- `MemoryCache`（SizeLimit/Expiration 等）
- `Serilog:MinimumLevel`（已有 Diagnostics API 热更新，复用）

**PUT 黑名单（敏感/基础设施，返回 403）**：
- `Jwt.SecretKey`、`Jwt.AccessTokenExpirationMinutes`（Token 策略改需重部署）
- `ConnectionStrings`
- `DefaultPasswords`
- `Kestrel`（端口/限制）
- `SystemAdmin.AllowAutoCreateInProduction`、`InitialSetupToken`（生产门控）
- `Database`（连接池/迁移策略）

**写回**：PUT 成功 → 持久化到服务端 `appsettings.json`（自定义写回，IConfiguration 只读快照不直接支持）+ 返回 `{ "applied": true, "restartRequired": true/false, "effectiveMode": "restart"|"hot" }`。

---

## [S3] 重启机制（POST /configuration/restart）

**流程**：
1. sysadmin 改配置 → 系统提示「需重启生效」
2. sysadmin 点「应用并重启」→ 二次确认对话框（显示将重启 + 倒计时）
3. `POST /configuration/restart` → 服务端延迟 30 秒执行 `IHostApplicationLifetime.StopApplication()`
4. Windows Service / 进程管理器（配置恢复策略）自动拉起 WebAPI
5. 重启完成 → sysadmin 端轮询健康检查确认恢复

**防护**：
- **权限**：sysadmin only（`IsSysAdmin=true`）
- **审计**：每次重启写审计日志（D1：操作人/时间/原因）
- **限频**：每小时最多 3 次重启（防误操作/DoS）
- **优雅**：延迟 30 秒给在途请求完成时间；返回响应后再生效
- **通知**：重启前通过 SignalR（如有）或状态端点告知在线用户

---

## [S4] 客户端配置中心（US-SHELL-018，双模式面板）

**SysadminHomeView 面板按模式区分**：

| 模式 | 面板 | 数据源 |
|------|------|--------|
| **远程** | ① 客户端配置（本机 Desktop） ② 服务端配置（调服务端 API） | ① 客户端 appsettings ② 服务端 Configuration API |
| **本地** | ① 本地配置（全栈：LocalWebAPI+LocalDB+Desktop） ② 备份恢复（US-SHELL-013） | 客户端 appsettings（含 OfflineMode/LocalApiBaseUrl/Jwt 本地等） |

**客户端配置 7 组**（沿用 US-SHELL-018）：诊所信息 / 会话(ClientSession) / 连接(ApiClient) / 安全策略(DefaultPassword) / 功能开关(FeatureToggle，热更新) / 读卡器(CardReader) / 系统信息(只读)。

**服务端配置面板（仅远程）**：调用 [S2] API，展示 GET 全部节（脱敏）；业务参数行可编辑（PUT）；敏感行只读标记 🔒；改后提示「重启生效」+「应用并重启」按钮。

**本地配置面板**：客户端 7 组 + LocalWebAPI 特有（OfflineMode.LocalApiBaseUrl 5300、本地 Jwt 等，可改）+ 备份恢复入口。

---

## [S5] 双模式配置对象区分矩阵

| 配置对象 | 远程模式 | 本地模式 |
|---|---|---|
| 服务端 WebAPI/SQL Server/公网 | Configuration API（业务可改/敏感只读 + 重启） | 无独立服务端（LocalWebAPI 内嵌） |
| LocalWebAPI（内嵌） | 不适用 | 归「本地配置」面板（appsettings OfflineMode/Jwt 本地） |
| Desktop 客户端 | 「客户端配置」面板 | 「本地配置」面板（全栈） |
| 数据库 | 远程 SQL Server（靠 ConnectionStrings，只读） | LocalDB（备份恢复 US-SHELL-013） |
| 配置中心入口 | SysadminHomeView（两面板） | SysadminHomeView（单面板+备份恢复） |

---

## [S6] 安全（公网部署关键）

- **传输**：公网必须 HTTPS（Kestrel Https 端点 + 反向代理 TLS）
- **认证**：所有 Configuration API 端点 `[Authorize(Policy=SysAdminOnly)]`（新策略，仅 `IsSysAdmin=true`）
- **审计**：每次 GET/PUT/Restart 写审计日志（D1 SecurityAuditLog：操作人/时间/IP/变更内容 diff）
- **脱敏**：GET 响应敏感字段掩码（SecretKey→`***`、连接串→`Server=***`、密码→`***`）
- **防篡改**：PUT 写回 appsettings.json 前备份原文件（`appsettings.json.bak.{timestamp}`）
- **限频**：重启每小时 ≤3 次（[S3]）；PUT 每分钟 ≤10 次

---

## [S7] 文档更新清单（可立即执行，纯文档）

| 文件 | 更新 |
|------|------|
| `docs/02-requirements/11a-shell.md` US-SHELL-018 | 补「双模式面板」段（远程两面板/本地单面板+备份）；补「服务端配置 API」依赖注 |
| `docs/04-api-reference/10-configuration.md` | 补 PUT /configuration/{section}（白/黑名单）、POST /configuration/restart 端点说明（标 🚧 v1.0 待实现） |
| `docs/06-operations/02-configuration.md` | 补「sysadmin 远程配置管理」段（API 边界 + 重启机制 + 双模式区分矩阵） |
| `docs/01-product/02-personas.md` sysadmin 段 | 补「远程管服务端配置（API+重启）；本地管全栈」 |
| `docs/02-requirements/13-traceability-matrix.md` | US-SHELL-018 补「双模式面板+服务端 API 依赖」注 |
| `docs/03-architecture/decisions/` | 新建 ADR-0014「sysadmin 配置双模式 + 服务端 API + 延迟重启」 |

---

## [S8] 代码待实施清单（本 spec 仅记录，不执行）

遵循「先文档不代码」，以下由后续代码 plan 承载：
- ConfigurationController 扩展：PUT /configuration/{section}（白名单校验 + 写回 appsettings.json + 脱敏 GET）
- POST /configuration/restart（延迟重启 + 防护）
- 新策略 `SysAdminOnly`（仅 IsSysAdmin）
- US-SHELL-018 SysadminHomeView 双模式面板（远程两面板/本地单面板）+ 服务端配置面板（调 API）
- 审计集成（D1 SecurityAuditLog 记配置变更/重启）
- appsettings.json 写回工具（备份 + 原子写）

---

## [S9] 验收

1. 文档清楚反映 sysadmin 配置双模式区分（服务端 API + 客户端配置中心 + 本地全栈）
2. 服务端配置 API 边界明确（业务可改白名单 / 敏感只读黑名单）
3. 重启机制记录（延迟重启 + 防护）
4. US-SHELL-018 双模式面板设计落位
5. 安全措施完整（脱敏/审计/限频/HTTPS）
6. ADR-0014 新建记录决策
