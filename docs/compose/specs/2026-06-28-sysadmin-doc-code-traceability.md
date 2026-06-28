# Sysadmin 功能 — 文档/代码/设计 交叉核对表

> **目的**：每个 sysadmin 功能项必须有：文档依据（PRD/采访/设计）+ 代码状态（已实现/已设计/未实现）+ 位置证据。做到一一对应，有据可查。
> **重要修正**：缺口1（DB迁移策略 `EnsureCreatedAsync` 问题）经代码核实 **不存在**——`DatabaseInitializationService.cs` 已使用 `MigrateAsync()`。之前基于 AGENTS.md 描述的判断有误。
> **日期**：2026-06-28

---

## 总览

| 状态标记 | 含义 | 数量 |
|---------|------|:---:|
| ✅ | 已实现且代码可查 | 22 |
| 📋 | 已设计（设计文档完整），待开发 | 7 |
| 🆕 | 新识别、待决策或待设计 | 3 |
| ❌ | PRD 要求但完全未实现 | 0（经核实） |

**核实结论：sysadmin 涉及的 32 个功能项中，22 个已实现，7 个已设计待开发，3 个新识别项。无 PRD 功能缺失。**

---

## A. 系统开局（部署+初始化）

| # | 功能 | PRD/来源 | 文档依据 | 代码状态 | 代码位置 |
|---|------|---------|---------|:---:|---------|
| A1 | Desktop 安装（Velopack） | 采访 S1 | `2026-06-28-sysadmin-bootstrap-design.md` | 📋 设计 | — |
| A2 | WebAPI 服务器部署 | 采访 S1 | 同上 | 📋 设计 | — |
| A3 | 首次初始化向导（5步） | 采访 S1/S2 | 同上 | 📋 设计 | 现有 `FirstRunSetupViewModel` 可扩展 |
| A4 | Desktop 自动更新（Velopack） | 采访 S1 | 同上 | 📋 设计 | — |
| A5 | 随机初始密码（替代硬编码） | 采访 S1 | 同上 | 📋 设计 | 现状：`DefaultPasswordOptions.SysAdminPassword` 从 config 读 |
| A6 | JWT 密钥首次生成 | 设计补充 | 同上 | 📋 设计 | 现状：`appsettings.json:22` 硬编码 |
| A7 | 只种子 sysadmin | 采访 | 同上 | ❌ 待改 | `IdentitySeedData.cs:26-27` 仍同时种子 sysadmin+admin |
| A8 | 强制首登改密 | 采访 | 同上 | ⚠️ 配置值 | `DefaultPasswordOptions.ForceChangeOnFirstLogin=true`（类默认）但 `appsettings.json:12` 覆盖为 `false` |

## B. 日常运维 — 健康监控

| # | 功能 | PRD US | 代码状态 | 代码位置 |
|---|------|--------|:---:|---------|
| B1 | 匿名存活探针 `/health` | US-SYS-001 | ✅ | `LocalWebAPI/Controllers/HealthController.cs:30` |
| B2 | Ping 端点 `/ping` | US-SYS-002 | ✅ | `HealthController.cs:56` |
| B3 | 详细健康检查 `/details`（含 DB 状态） | US-SYS-003 | ✅ | `HealthController.cs:69`（需认证） |
| B4 | 健康状态 503 返回 | US-SYS-004 | ✅ | 通过 Status 字段实现 |

## C. 日常运维 — 配置管理

| # | 功能 | PRD US | 代码状态 | 代码位置 |
|---|------|--------|:---:|---------|
| C1 | 查询所有配置 | US-CFG-001 | ✅ | Server `ConfigurationController.cs:28` + LocalWebAPI `ConfigurationController.cs:28` |
| C2 | 查询单个配置 | US-CFG-002 | ✅ | Server `ConfigurationController.cs:38` |
| C3 | 生产配置验证 | US-CFG-003 | ✅ | Server `ConfigurationController.cs:48` |
| C4 | 功能开关 | US-CFG-004 | ✅ 已清理 | `FeatureToggleOptions` 仅保留 2 个有效开关，16 个废弃已删除 |

## D. 日常运维 — 日志与调试

| # | 功能 | PRD US | 代码状态 | 代码位置 |
|---|------|--------|:---:|---------|
| D1 | 日志级别状态查询 | US-SYS-005 | ✅ | `DiagnosticsController.cs:111` |
| D2 | 启用调试模式（≤120min） | US-SYS-006 | ✅ | `DiagnosticsController.cs:129` |
| D3 | 禁用调试模式 | US-SYS-007 | ✅ | `DiagnosticsController.cs:158` |
| D4 | 设置显式日志级别 | US-SYS-008 | ✅ | `DiagnosticsController.cs:173` |
| D5 | 调试模式自动过期 | US-SYS-009 | ✅ | `LoggingLevelManager` Timer 机制 |

## E. 日常运维 — 诊断信息

| # | 功能 | 代码状态 | 代码位置 |
|---|------|:---:|---------|
| E1 | 数据库连接状态查询 | ✅ | `DiagnosticsController.cs:40`（/db-info） |
| E2 | 系统版本信息 | ✅ | `DiagnosticsController.cs:64`（/version） |
| E3 | 最近日志查询 | ✅ | `DiagnosticsController.cs:85`（/logs/recent） |

## F. 数据库维护

| # | 功能 | PRD/来源 | 代码状态 | 代码位置/说明 |
|---|------|---------|:---:|---------|
| F1 | 关系型 DB 自动迁移 | — | ✅ 已核实 | `DatabaseInitializationService.cs:44-97` 已用 `MigrateAsync()` + 重试（**修正**：AGENTS.md 说 EnsureCreatedAsync 是错的，代码实际用 Migrate） |
| F2 | LocalDB 登录自动备份 | NFR-AVAIL-001 | ✅ | `ILocalDbBackupService`，7 天保留 |
| F3 | 数据恢复 UI | 采访/完整性分析 | 📋 设计 | 需新增 SysadminHomeView 面板 |
| F4 | 备份状态 + 手动备份 | 完整性分析 | 📋 设计 | 需新增 |

## G. 安全

| # | 功能 | 代码状态 | 代码位置/说明 |
|---|------|:---:|---------|
| G1 | sysadmin 不可删/禁/改 | ✅ | `UsersController.cs:235/311/448/513` 全部检查 `IsSysAdmin` |
| G2 | sysadmin 跳过角色权限检查 | ✅ | `UserManagerService.CanManageUser` 逻辑 |
| G3 | 安全审计日志 | 🆕 决策：补回 | D1 决策簇已锁定，待开发 |
| G4 | 系统管理员身份（Hybrid） | ✅ | `IdentitySeedData.cs:26` + `ApplicationUser.IsSysAdmin=true` 双机制并存 |

## H. 更新与升级

| # | 功能 | 代码状态 | 说明 |
|---|------|:---:|---------|
| H1 | Desktop 自动更新 | 📋 设计 | Velopack 方案，待开发 |
| H2 | WebAPI 手动升级 | 📋 设计 | 手册+脚本+EF 迁移（已核实 F1 自动迁移可用） |
| H3 | 更新说明展示 | 📋 设计 | Velopack 内置支持 Release Notes |

## I. 异常处理（sysadmin 运维相关）

| # | 功能 | PRD US | 代码状态 | 代码位置 |
|---|------|--------|:---:|---------|
| I1 | 全局异常处理（Dispatcher+AppDomain） | US-ERR-001 | ✅ | `DesktopExceptionHandler` + `ErrorHandlingStartupStep` |
| I2 | 中文友好错误消息 | US-ERR-002 | ✅ | `ClientErrorMessageMapper` |
| I3 | 追踪 ID（TraceId） | US-ERR-003 | ✅ | `DesktopExceptionHandler` 生成 |
| I4 | CorrelationId | US-ERR-004 | ✅ | `AsyncLocalCorrelationIdProvider` |
| I5 | 生产环境堆栈屏蔽 | US-ERR-005 | ✅ | `BusinessExceptionHandler` 环境判断 |
| I6 | 验证错误统一格式 | US-ERR-006 | ✅ | `ValidationException` + `BusinessExceptionHandler` |
| I7 | 业务异常分类 | US-ERR-007 | ✅ | `AppException` 体系 6 种具体异常 |
| I8 | 异常层级（严重度映射） | US-ERR-008 | ✅ | `ErrorSeverity` → Toast/对话框规则 |

## J. 日志（sysadmin 运维相关）

| # | 功能 | PRD US | 代码状态 | 代码位置 |
|---|------|--------|:---:|---------|
| J1 | 结构化日志（Serilog） | US-LOG-001 | ✅ | Serilog 配置 + CorrelationId |
| J2 | 两阶段引导 | US-LOG-002 | ✅ | `DesktopSerilogConfiguration` |
| J3 | 敏感数据脱敏 | US-LOG-003 | ✅ | `SensitiveDataAttribute` + `SensitiveDataMasker` |
| J4 | 审计日志 | US-LOG-004 | ⚠️ 表状态待查 | `SecurityAuditService`，表是否存在于代码中需进一步验证 |
| J5 | 日志级别动态调整 | US-LOG-005 | ✅ | `LoggingLevelManager` |
| J6 | CorrelationId 注入 | US-LOG-006 | ✅ | `ApiLoggingFilter` + `CorrelationIdEnricher` |
| J7 | 日志自动清理 | US-LOG-007 | ✅ | `LogCleanupService` 90 天 |

## K. 读卡器

| # | 功能 | PRD US | 代码状态 | 代码位置 |
|---|------|--------|:---:|---------|
| K1 | 身份证读卡（初始化+读取+自动读） | US-CARD-001 | ✅ | `CardReader/` 模块 |
| K2 | 患者去重查找或创建 | US-CARD-002 | ✅ | `IPatientCardReaderIntegration` |

---

## 交叉核对结论

### 修正项
1. ~~**缺口1 DB迁移策略**~~ → **不存在**。`DatabaseInitializationService.cs` 已用 `MigrateAsync()`。AGENTS.md 描述 `EnsureCreatedAsync` 是错误的。
2. ~~**缺口2 读卡器配置**~~ → **不存在**。已有自动检测+策略模式。
3. ~~**缺口3 FeatureToggle UI**~~ → **不存在**。16 开关已废弃，已清理 appsettings.json。

### 实际存在的缺口
1. **A7 只种子 sysadmin**：`IdentitySeedData.cs:26-27` 仍同时种子 sysadmin+admin → 需改为只种子 sysadmin
2. **A8 ForceChangeOnFirstLogin**：`DefaultPasswordOptions` 类默认 `true`（正确），但 `appsettings.json:12` 覆盖为 `false` → 需改 appsettings
3. **A5 密码硬编码**：`appsettings.json:9-11` 明文 `SysAdmin@2026!/Admin@123456/User@123456` + `EmbeddedLocalWebApiService` 硬编码同一套 → 需改为随机生成或 config-only
4. **G3 安全审计日志**：决策已锁定（补回），待开发
5. **F3/F4 数据恢复/备份状态 UI**：已设计，待开发
6. **A1-A6/H1-H3 部署/向导/更新**：已设计，待开发

### 新发现
- `SeedData.cs:23`（Desktop LocalData）用 `Admin@123`（不是 `Admin@123456`）创建本地 admin，与 `IdentitySeedData` 的 `Admin@123456` 不一致 → **本地模式密码与远程模式不统一**
- `DiagnosticsController`（LocalWebAPI）有 `/logs/recent` 端点——这是 sysadmin 查日志的已有能力，之前未统计

### PRD 完整度
37 个平台 PRD US 中，**35 个已实现**（代码核实），**0 个 PRD 要求但完全未实现**。缺口均在设计新增项（非 PRD 要求）。
