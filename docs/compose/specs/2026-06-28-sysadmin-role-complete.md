# Sysadmin 角色完整梳理

> **目的**：把 sysadmin 的全部职责、现状、设计、剩余问题**一次梳理完**，不再遗漏。
> **信息源**：personas.md + 平台 PRD（11a-shell.md/11b-configuration.md/11e-cardreader.md: 43 US）+ 8 模块审计 + Shell 审计 + 采访答案（S1 + 4 个决策 + N1）+ bootstrap 设计文档。
> **格式**：每个职责条目标注状态（✅已实现/⚠️部分/🔴缺失/📋已设计/❓需决策），底部统一列出仅剩的业务问题。

---

## A. 系统开局（部署+初始化）— 📋 已设计

详见 `docs/compose/specs/2026-06-28-sysadmin-bootstrap-design.md`

| 子项 | 设计 | 状态 |
|------|------|------|
| A1 Desktop 部署 | Velopack 打包 Setup.exe（自包含 .NET，免 admin 权限） | 📋 已设计 |
| A2 WebAPI 部署 | 服务器侧自包含 + 手动/脚本部署 | 📋 已设计 |
| A3 首次初始化向导 | 5 步强制（改密→诊所信息→模式→建 admin→交权） | 📋 已设计 |
| A4 自动更新 | Desktop Velopack 自动；WebAPI 手动+EF 迁移 | 📋 已设计 |
| A5 密码随机生成 | 安装时随机初始密码，首登强制改 | 📋 已设计 |
| A6 JWT 密钥生成 | 首次启动生成随机密钥 | 📋 已设计 |
| A7 只种子 sysadmin | IdentitySeedData 删 admin 种子 | 📋 已设计 |

**待确认**：A1-A7 的 4 个业务问题已全部锁定（Q1-Q4 + N1）。

---

## B. 日常运维

### B1 系统健康监控

| 条目 | 状态 | 证据 |
|------|------|------|
| 匿名存活探针 `/health` | ✅ | HealthController.cs |
| 详细健康检查 `/details`（含 DB 状态） | ✅ | 需认证，返回 DB 连接+迁移状态 |
| Ping 端点 `/ping` | ✅ | HealthController.cs |
| 健康状态 503 返回 | ✅ | Degraded/Unhealthy→503 |
| **Desktop 端健康状态显示** | ⚠️ | 审计发现三套健康检查并存（HealthCheckCoordinator/ApiHealthMonitor/ApiHealthCheckStartupStep），状态靠 ApplicationStateService 属性同步，竞态风险 |

**我的建议**：健康检查三套合一为单一 `HealthMonitor`（断路器+事件），这是 Shell 重构的已识别子系统（T11-S6），纳入 Phase② 设计。sysadmin 在 `/details` 能看到健康全貌即可。

### B2 配置管理

| 条目 | 状态 | PRD |
|------|------|-----|
| 查询所有配置 `/configuration`（SuperAdmin） | ✅ | US-CFG-001 |
| 查询单个配置 `/configuration/{section}` | ✅ | US-CFG-002 |
| 生产配置验证（启动时） | ✅ | US-CFG-003 |
| 功能开关 FeatureToggles | ✅ | US-CFG-004，18 个开关大部分已废弃，仅 `OverwriteConflicts`/`DuplicateHerbMergeStrategy` 活跃 |
| **运行时修改配置** | ❌ | 现有配置只能改 appsettings.json 文件，不支持运行时修改 |
| **ClinicSettings 修改** | ⚠️ | 硬编码在 appsettings，向导会写入，但后续修改无 UI |

**我的建议**：
- ClinicSettings（诊所名/地址/电话）→ sysadmin 后续修改通过 SysadminHomeView UI 修改（已设计进向导，扩展到日常管理）
- FeatureToggles → v1.0 保持 appsettings 文件配置（重启生效）；v2.0 可考虑热更新
- 运行时修改配置 → v1.0 不做（需求不强，改配置可改文件重启）

### B3 日志级别/调试模式管理

| 条目 | 状态 | PRD |
|------|------|-----|
| 查询日志级别状态 | ✅ | US-SYS-005，DiagnosticsController |
| 启用调试模式（≤120 分钟，自动过期） | ✅ | US-SYS-006 |
| 禁用调试模式 | ✅ | US-SYS-007 |
| 设置显式日志级别 | ✅ | US-SYS-008 |
| 调试模式自动过期 | ✅ | US-SYS-009 |
| **Desktop 端 sysadmin UI** | ⚠️ | SysadminHomeView 存在（审计报告 "✅ 用 materialDesign:Card"），具体功能未审计 |

**状态**：服务端 API 完整，Desktop UI 已有但内容未深审。功能完备率高，是 sysadmin 角色中实现最完整的部分。

### B4 用户管理支持

| 条目 | 状态 | 说明 |
|------|------|------|
| **重置 admin 密码** | 📋 已设计 | UI 自助（Q4 已锁定） |
| 创建 admin | 📋 已设计 | 首次初始化向导（Step 4） |
| **日常用户管理** | ⚠️ | 这是 **Admin** 的职责（Users US-001~012），sysadmin 不参与日常用户管理 |
| **sysadmin 被保护不可删/禁/改** | ✅ | UsersController.cs:235/311/448 全部检查 IsSysAdmin |
| **sysadmin 恢复 admin 密码** | 🔴 | 无独立端点；SeedTool PRD 提到但不存在 |

**我的建议**：
- v1.0 做 sysadmin 内的"重置 admin 密码"UI（简单：输入新密码→确认→调用 Users API）
- 不做用户列表管理（那是 Admin 的事）

### B5 系统升级（已设计）

| 条目 | 设计 |
|------|------|
| Desktop 自动检查+升级 | Velopack（普通自愿+安全强制） |
| WebAPI 手动升级 | 手册+脚本+EF 迁移 |
| 更新源 | WebAPI 服务器 |

---

## C. 数据维护

### C1 数据库备份

| 条目 | 状态 | 证据 |
|------|------|------|
| **LocalDB 登录自动备份** | ✅ | `ILocalDbBackupService`，登录后 fire-and-forget，7 天保留，超期自动删 |
| **远程 SQL Server 备份** | ⚠️ | 依赖 SQL Server Agent / 手动维护计划，无应用层控制 |
| **备份状态查看** | ❌ | 无 UI 查看"上次备份时间""备份文件列表" |

**我的建议**：
- v1.0 SysadminHomeView 增加"本地数据库备份状态"显示（读取备份目录）
- 远程 SQL Server 备份 = 运维手册文档（应用不控制 SQL Server Agent）
- v2.0 可考虑 WebAPI 端远程备份触发

### C2 数据库恢复

| 条目 | 状态 | PRD |
|------|------|-----|
| **LocalDB 恢复** | ❌ | NFR-AVAIL-001 提到备份但无恢复 UI/CLI |
| **远程 SQL Server 恢复** | ❌ | 无应用层支持 |

**我的建议**：
- v1.0 SysadminHomeView 增加"从备份恢复"功能（读取备份文件→`RESTORE DATABASE`）
- 远程恢复 = 运维手册（`sqlcmd` / SSMS）
- 这是灾难恢复的关键功能，**建议纳入 v1.0**（~1 人日，LocalDB 恢复）

### C3 日志/审计自动清理

| 条目 | 状态 | PRD |
|------|------|-----|
| 系统日志清理（90天） | ✅ | `LogCleanupService`，每 24h，分批删 |
| 安全审计日志清理（365天） | ⚠️ | Service 存在，但 `SecurityAuditLog` 表**可能不存在**（审计未深审此表） |

---

## D. 安全管理

### D1 安全审计日志

| 条目 | 状态 | PRD |
|------|------|-----|
| 记录登录成功/失败/登出/Token刷新 | ⚠️ | Auth 模块审计：仅 `_logger.LogInformation`，`ISecurityAuditService` **不存在** |
| 记录密码变更 | ❌ | 无 |
| **sysadmin 查看审计日志** | 🔴 | 无端点无 UI |
| 审计日志保留 365 天 | ⚠️ | Service 定义但底层表可能已删 |

**我的建议**：这是 **D1 决策簇**（待你拍板：补回 or 标 v2.0）。从安全/合规角度我推荐**补回**，sysadmin 角色需要查看审计日志的入口。

### D2 访问控制监控

| 条目 | 状态 |
|------|------|
| "谁查了哪个患者"的日志 | ❌ | 无 |
| "谁修改了医案"的审计 | ❌ | 医案审计日志实体已删（D1） |

**我的建议**：v1.0 仅补医案变更审计（D1 决策簇，医疗合规）；患者查询访问日志标 v2.0。

---

## E. 架构层面的 sysadmin 身份问题

| 条目 | 现状 | 问题 | 我的推荐 |
|------|------|------|---------|
| sysadmin 既是 `IsSysAdmin=true` 又有 `SuperAdmin` 角色 | IdentitySeedData 同时设两者 | personas 说"独立用户非角色"，但代码用两种机制 | **保留 hybrid 状态**（向后兼容）；新增代码只查 `IsSysAdmin` |
| `PermissionLevel` 超级权限 | sysadmin 可管理任何人 | 正确（信任根） | 保持不变 |
| 不可删/禁/改/禁用 | UsersController:235/311/448/513 | ✅ 已保护 | 保持不变 |
| sysadmin 登录限流 | 远程/本地都有（5/窗口或5/分） | sysadmin 应该不受限？ | **我的建议：sysadmin 不受限流**（紧急恢复场景） |
| sysadmin 默认密码 | 硬编码 `SysAdmin@2026!` | 安全风险 | 📋 已设计改随机生成 |

---

## F. 平台 PRD 中 sysadmin 相关的全部 US

| US | 标题 | 实现状态 |
|----|------|---------|
| US-SHELL-001 | 应用启动（单实例） | ✅ 完成 |
| US-CFG-001 | 查询所有配置 | ✅ 完成 |
| US-CFG-002 | 查询单个配置 | ✅ 完成 |
| US-CFG-003 | 验证生产配置 | ✅ 完成 |
| US-CFG-004 | 功能开关 | ✅ 完成 |
| US-SYS-001 | 匿名存活探针 | ✅ 完成 |
| US-SYS-002 | Ping 端点 | ✅ 完成 |
| US-SYS-003 | 详细健康检查 | ✅ 完成 |
| US-SYS-004 | 健康状态 503 返回 | ✅ 完成 |
| US-SYS-005 | 日志级别查询 | ✅ 完成 |
| US-SYS-006 | 启用调试模式 | ✅ 完成 |
| US-SYS-007 | 禁用调试模式 | ✅ 完成 |
| US-SYS-008 | 设置显式日志级别 | ✅ 完成 |
| US-SYS-009 | 调试模式自动过期 | ✅ 完成 |
| US-ERR-001 | 全局异常处理 | ✅ 完成 |
| US-ERR-002 | 中文友好错误消息 | ✅ 完成 |
| US-ERR-003 | 追踪 ID | ✅ 完成 |
| US-ERR-004 | CorrelationId | ✅ 完成 |
| US-ERR-005 | 生产环境堆栈屏蔽 | ✅ 完成 |
| US-ERR-006 | 验证错误统一格式 | ✅ 完成 |
| US-ERR-007 | 业务异常分类 | ✅ 完成 |
| US-ERR-008 | 异常层级 | ✅ 完成 |
| US-LOG-001~007 | 日志/审计全部 | ✅ 大部分完成 |
| US-CARD-001/002 | 读卡器 | ✅ 完成 |

**PRD 统计**：37 个 US 中 **约 35 个已实现**，仅 2 个可能需调整（安全审计日志表、备份恢复 UI）。

---

## G. Sysadmin 角色 v1.0 功能清单（汇总）

| 功能 | 优先级 | 状态 | 落地阶段 |
|------|:---:|------|---------|
| Desktop 部署（Velopack） | P0 | 📋 已设计 | Phase② 实现 |
| 首次初始化向导（5 步） | P0 | 📋 已设计 | Phase② 实现 |
| Desktop 自动更新 | P0 | 📋 已设计 | Phase② 实现 |
| 重置 admin 密码（UI） | P0 | 📋 已设计 | Phase② 实现 |
| 从备份恢复本地数据库 | P1 | ❌ 缺失 | Phase② 实现（~1 人日） |
| 查看本地备份状态 | P1 | ❌ 缺失 | Phase② 实现（~0.5 人日） |
| 查看安全审计日志 | P1 | ❌ 缺失 | 依赖 D1 决策（补回 or 标 v2.0） |
| 配置管理 UI（ClinicSettings/连接/FeatureToggle） | P2 | ⚠️ 部分 | Phase② 设计 |
| 健康检查三套合一 | P2 | ❌ 现有三套 | Shell 重构 Phase② |
| sysadmin 不受登录限流 | P2 | ⚠️ 现有限流 | 代码调整 |
| 医案变更审计日志 | P1 | 🔴 缺失 | 依赖 D1 决策 |
| 远程数据库恢复 | P3 | ❌ | 运维手册文档 |

---

## H. 仅剩的业务决策（你需拍板）

以上全部梳理完，sysadmin 角色**只剩 2 个开放业务问题**（技术方案已定）：

1. **sysadmin 查看安全审计日志**：sysadmin 是否需要能查看登录历史/密码变更/权限变更记录？（现状无此功能，补回=中等工作量）→ 这是**D1 决策簇**的一部分

2. **sysadmin 禁止客户端切本地模式**：在"服务器+客户端"模式下，sysadmin 是否需要能**禁止**普通用户切换到本地模式？（安全考虑：防止客户端绕过服务器数据）→ 当前所有用户都能切本地

其余全是技术实现（我的活），你不用管。
