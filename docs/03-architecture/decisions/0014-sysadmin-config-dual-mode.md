# ADR-0014: sysadmin 配置双模式 + 服务端 Configuration API + 延迟重启

## 状态
Accepted（范围决策）— v1.0 文档基线已锁定；**代码实现（ConfigurationController 扩展 / 重启端点 / SysAdminOnly 策略 / 双模式面板）属待实施范围，由后续代码 plan 承载**（用户「先文档不代码」）。

## 上下文

sysadmin 在双模式下配置对象本质不同：

- **远程模式**：管「服务端（WebAPI / SQL Server / 公网）+ 客户端（Desktop）」两层。
- **本地模式**：管「本地全栈（LocalWebAPI + LocalDB + Desktop）」一层。

当前服务端配置无 UI/API——需登录服务器手动改 `appsettings.json`。`US-SHELL-018` 仅管客户端配置（7 组），无法覆盖服务端业务参数（Session/RateLimiting/ClinicSettings 等）。

小诊所场景无专职运维，sysadmin 需全远程闭环：在 Desktop 配置中心即可查看并修改服务端业务参数，敏感配置（JWT/连接串/密码）只读保护，配置变更后通过 API 延迟重启生效，无需 SSH/RDP 登录服务器。

> 配套：[US-SHELL-017/018](../../02-requirements/11a-shell.md)、[06-operations/02-configuration.md](../../06-operations/02-configuration.md)、[ADR-0013](0013-signalr-realtime-push.md)、WebAPI `appsettings.json`。用户决策（2026-06-28）：服务端配置走 API（方案 B）；业务参数可改、敏感只读（方案 A）；重启走 API 延迟重启（方案 A，全远程闭环）。

## 决策

1. **服务端 Configuration API 扩展**（仅远程模式，扩展 `ConfigurationController`）：
   - `GET /configuration`、`GET /configuration/{section}`：敏感字段脱敏展示（SecretKey/连接串/密码→`***`）。
   - `PUT /configuration/{section}`：仅业务参数白名单可改（Session / Security.RateLimiting / ClinicSettings / FeatureToggles / SystemAdmin.SessionTimeoutMinutes / MemoryCache / Serilog:MinimumLevel）；敏感/基础设施黑名单（Jwt.SecretKey / ConnectionStrings / DefaultPasswords / Kestrel / 生产门控 / Database）返回 403。写回服务端 `appsettings.json`（备份原文件）+ 返回 `applied`/`restartRequired`/`effectiveMode`。
   - `POST /configuration/restart`：延迟 30 秒执行 `IHostApplicationLifetime.StopApplication()`，Windows Service / 进程管理器自动拉起。

2. **US-SHELL-018 双模式面板**：SysadminHomeView 按连接模式区分——远程模式两面板（① 客户端配置 本机 Desktop 7 组 + ② 服务端配置 调 Configuration API）；本地模式单面板（本地全栈 LocalWebAPI + LocalDB + Desktop）+ 备份恢复（US-SHELL-013）。

3. **本地全栈单面板**：本地模式无独立服务端（LocalWebAPI 内嵌），所有配置直接读写本机 appsettings（含 `OfflineMode`/`LocalApiBaseUrl`/本地 Jwt 等）。

## 理由

- **sysadmin 全远程闭环**：小诊所无专职运维，sysadmin 在 Desktop 即可完成服务端业务参数调整 + 重启，无需登录服务器（符合全远程运维诉求）。
- **业务可改、敏感只读**：业务/运维参数（Session/限流/诊所设置）调整频繁且安全风险可控，放行 API 修改；敏感/基础设施配置（JWT 密钥/连接串/密码/端口）改需重部署或涉安全，强制只读 403，防止误操作扩大攻击面。
- **延迟重启**：`IHostApplicationLifetime.StopApplication()` + 进程管理器自动拉起，给在途请求 30 秒完成时间，避免粗暴中断；Service 配置恢复策略保证重启后自动恢复。

## 后果

### 待实施项（代码 plan 承载，非本 ADR 范围）

| 议题 | 结论 / 待决项 |
|------|--------|
| 新策略 `SysAdminOnly` | 仅 `IsSysAdmin=true` 放行；与现有 `AdminOrSuperAdmin` 并存 |
| `appsettings.json` 写回工具 | 自定义写回（IConfiguration 只读快照不直接支持）；备份 `appsettings.json.bak.{timestamp}` + 原子写 |
| 审计集成（D1） | 每次 GET/PUT/Restart 写 `SecurityAuditLog`（操作人/时间/IP/变更 diff） |
| 重启 API 防护 | 限频（每小时 ≤3 次）+ 二次确认 + 通知在线用户 |

### 即时影响

- 新增 [服务端 Configuration API](../../04-api-reference/10-configuration.md) 端点说明（PUT / POST restart / GET 脱敏，均 🧲 v1.0 待实现）。
- [US-SHELL-018](../../02-requirements/11a-shell.md) 补「双模式面板」设计段。
- [06-operations/02-configuration.md](../../06-operations/02-configuration.md) 补「sysadmin 远程配置管理」段（API 边界 + 重启机制 + 双模式区分矩阵 + 安全）。
- [02-personas.md](../../01-product/02-personas.md) sysadmin 段补双模式配置管理职责。

### 风险

- **公网暴露**：服务端 Configuration API 暴露公网，需 HTTPS + SysAdminOnly 策略 + 审计 + 脱敏 + 限频（D3 B+ 安全方案）。
- **写回原子性**：`appsettings.json` 写回需原子操作（备份 + 临时文件 + rename），避免写入中断致配置损坏。
- **重启可用性**：延迟重启期间服务短暂不可用，需通知机制（SignalR/状态端点）告知在线用户。

## 交叉引用

- [US-SHELL-017: 生产环境安全门控](../../02-requirements/11a-shell.md)（SystemAdminOptions 生产门控，PUT 黑名单含 `AllowAutoCreateInProduction`/`InitialSetupToken`）
- [US-SHELL-018: sysadmin 配置中心](../../02-requirements/11a-shell.md)（双模式面板的直接产物）
- [US-SHELL-013: 数据库备份恢复](../../02-requirements/11a-shell.md)（本地模式面板的备份恢复入口）
- [ADR-0002: 双模式架构](0002-dual-mode-architecture.md)
- [ADR-0009: URL 驱动双模式](0009-url-driven-dual-mode.md)
- [sysadmin 配置设计 spec](../../compose/specs/2026-06-28-sysadmin-config-design.md)

## 变更记录

| 日期 | 变更 | 原因 |
|------|------|------|
| 2026-06-28 | 新建 ADR-0014，锁定 sysadmin 配置双模式 + 服务端 Configuration API + 延迟重启范围决策 | sysadmin 全远程闭环诉求（小诊所无专职运维）+ 服务端配置无 UI/API 现状 |

## 关联 US

- **US-SHELL-017**（生产环境安全门控：PUT 黑名单涉 `SystemAdminOptions` 生产门控字段）
- **US-SHELL-018**（sysadmin 配置中心：本 ADR 的直接产物——双模式面板 + 服务端配置面板依赖 Configuration API）
- **US-SHELL-013**（数据库备份恢复：本地模式面板的备份恢复入口）
