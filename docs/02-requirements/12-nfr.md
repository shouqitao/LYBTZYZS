# 非功能需求 (Non-Functional Requirements)

> 版本: v2.0 | 日期: 2026-06-15 | 状态: 重建

## 概述

本文档定义凌隐宝堂中医诊所管理系统的非功能性需求，覆盖性能、数据、可用性、安全、可维护性、兼容性六个维度。所有指标基于小型中医诊所（1-3 名医生同时操作、1-5 台终端）的典型场景设定。

NFR 编号采用 `NFR-{CATEGORY}-{NNN}` 格式（如 `NFR-PERF-001`），ID 一经发布即稳定，被 `docs/03-architecture/` 与 `docs/05-development/` 交叉引用。

---

## 性能 (Performance)

### NFR-PERF-001: API 响应时间

P95（95% 请求应在指定时间内完成）分级目标：

| 操作类型 | 目标 (P95) | 示例 | 当前配置参考 |
|----------|-----------|------|-------------|
| 简单查询（单实体 CRUD） | < 500ms | GET /api/v1/patients/{id} | 慢查询阈值 1000ms |
| 列表查询（分页） | < 1s | GET /api/v1/herbs?page=1&pageSize=20 | 默认分页 20 条 |
| 复杂聚合（跨表操作） | < 2s | POST /api/v1/medical-cases（含 Consultation + Prescription） | 数据库命令超时 30s |
| 批量导入 | < 5s | POST /api/v1/herbs/import | API 请求超时 60s |

**验收标准**:
- [x] 在标准数据量（患者 5000 + 医案 25000）下，上述 P95 指标达标（基准测试见 `docs/05-development/09-performance-baseline.md`）
- [ ] 慢查询监控（`SlowQueryThresholdMs=1000`）在生产环境启用

### NFR-PERF-002: Desktop 客户端响应

| 指标 | 目标 | 说明 |
|------|------|------|
| 冷启动（双击 → 登录页） | < 5s | WPF + Prism 初始化 + LocalDB 检查，非关键模块后台延迟加载 |
| 热启动（最小化恢复） | < 1s | — |
| 页面切换（导航到新模块） | < 1s | Prism Region 导航 + ViewModel 初始化 + 首屏数据加载 |
| 表单保存响应 | < 2s | 含网络往返（远程）或本地写入（本地） |
| 搜索响应（防抖后） | < 1s | 输入停止后触发搜索 → 结果渲染完成 |

### NFR-PERF-003: 客户端运行环境

| 组件 | 最低 | 推荐 | 理想 |
|------|------|------|------|
| 内存 | 4 GB | 8 GB | 16 GB |
| 操作系统 | Windows 10 (1809+) | Windows 11 | Windows 11 |
| 运行时 | .NET 8 Desktop Runtime | 同左 | 同左 |
| 磁盘 | 应用 ~100 MB + LocalDB 数据库 | 同左 | 同左 |

Desktop 应用典型内存占用 ~90-160 MB（WPF + Prism + 数据 + 缓存 < 5 MB）。

### NFR-PERF-004: 并发能力

| 指标 | 目标 | 当前配置 |
|------|------|---------|
| 同时操作用户数 | 1-3 人 | 数据库连接池 `MaxConnections=20` |
| 全局 API 速率限制 | 200 次/分钟 | `SecurityOptions.GlobalLimit` |
| 登录端点速率限制 | 5 次/窗口（内网 20 次） | `SecurityOptions.LoginLimit` |
| 本地登录限流 | 5 次/分钟 | `LocalJwtConfig` |

连接池（20）对 1-3 并发用户已充分冗余。速率限制配置已定义，生产部署时启用。

---

## 数据 (Data)

### NFR-DATA-001: 数据规模

| 实体 | 年新增 | 5 年总量 | 单条大小 | 说明 |
|------|--------|---------|---------|------|
| 患者 (Patient) | 300-500 | ~2500 | ~1KB | 总量上限 5000 |
| 医案 (MedicalCase) | 2000-5000 | ~25000 | ~3KB（含 Consultation + Prescription） | 日均 5-15 个 |
| 药材 (Herb) | 极少新增 | 600-1000 | ~0.5KB | 基于中药典 |
| 验方 (Formula) | 50-100 | 200-500 | ~2KB（含 FormulaHerbItems） | 个人 + 共享 |
| 处方药材 (PrescriptionHerbItem) | 10000-25000 | ~125000 | ~0.2KB | 每处方平均 5-10 味药 |
| 系统日志 (SystemLog) | ~100000 | 保留 90 天 ~25000 | ~0.3KB | 自动清理 |
| 安全审计日志 (SecurityAuditLog) | ~5000 | 保留 365 天 ~5000 | ~0.5KB | 登录/登出/权限变更 |

### NFR-DATA-002: 数据库容量预估

| 数据库 | 5 年预估容量 | 说明 |
|--------|-------------|------|
| SQL Server（远程） | ~200MB 数据 + ~50MB 索引 | 含日志表定期清理后 |
| SQL Server LocalDB（本地） | ~100MB | 同步数据子集，不含完整日志 |

> **注**: 本地模式当前使用 SQL Server LocalDB（Sprint 2 起从 SQLite 迁移而来）。早期 SQLite 方案已废弃，不再支持。

数据量级别（万级）不需要分区策略或读写分离。默认分页 20 条/页，可选 [10, 20, 50, 100]，覆盖所有列表场景。

### NFR-DATA-003: 索引策略

| 实体 | 关键索引 | 理由 |
|------|---------|------|
| Patient | Name, PhoneNumber, IdCardNumber | 搜索频率高 |
| MedicalCase | PatientId + VisitDate, DoctorId, Status | 按患者查询 + 按日期排序 |
| Herb | Name, PinyinName, Category | 处方中按名称/拼音搜索 |
| Formula | Name, CreatedBy, IsShared | 按名称搜索 + 按创建人筛选 |

当前数据量下，B-Tree 索引足以满足性能要求，无需全文检索。

---

## 可用性 (Availability)

### NFR-AVAIL-001: 数据备份策略

| 数据库 | 备份方式 | 频率 | 保留期 | 存储位置 |
|--------|---------|------|--------|---------|
| SQL Server（远程） | 自动全量备份 | 每日 | 7 天 | 服务器本地磁盘 |
| SQL Server LocalDB（本地） | 登录成功后自动备份 | 每次登录 | 7 天（最多 7 个文件） | `%AppData%/LYBTZYZS/Backup/` |

- **远程备份**: SQL Server Agent 或维护计划，文件命名 `LYBTDB_{yyyyMMdd}.bak`
- **本地备份**: Desktop 本地模式登录成功后通过 T-SQL `BACKUP DATABASE` 执行（实现: `ILocalDbBackupService` / `LocalDbBackupService`），fire-and-forget 不阻塞用户操作，超过 7 天的文件自动删除

**验收标准**:
- [x] LocalDB 备份通过 BACKUP DATABASE T-SQL 执行
- [x] 备份 fire-and-forget，登录后不阻塞
- [x] 超过 7 天的备份文件自动删除
- [ ] 远程 SQL Server 备份文件可成功还原（运维手册）

### NFR-AVAIL-002: 故障恢复目标

| 指标 | 目标 | 说明 |
|------|------|------|
| RTO（恢复时间目标） | 1 小时 | 从发现故障到恢复服务 |
| RPO（恢复点目标） | 24 小时 | 最多丢失 1 天的数据 |
| 降级模式 | 本地模式即时可用 | 服务器故障时切换 LocalDB 继续工作 |

**恢复优先级**: 本地模式降级（即时）> 从备份还原（1h 内）> 重新部署（1h 内）

**验收标准**:
- [ ] 服务器不可达时，手动切换本地模式后 < 30 秒可继续使用核心功能
- [ ] SQL Server 备份还原流程有文档化操作手册

### NFR-AVAIL-003: 数据库重试与容错

| 配置 | 值 | 说明 |
|------|---|------|
| 最大重试次数 | 3 | 瞬时故障自动重试 |
| 基础延迟 | 1000ms | 指数退避 |
| 最大延迟 | 10000ms | 重试间隔上限 |

实现: EF Core `EnableRetryOnFailure` + 自定义 `RetryPolicyOptions`。医案并发冲突（`DbUpdateConcurrencyException`）另设最多 3 次重试。

### NFR-AVAIL-004: 单实例与本地模式

Desktop 通过 `Mutex`（`Global\LYBTZYZS_Shell_Instance`）强制单实例运行，避免本地 LocalDB 文件锁冲突。本地模式为单用户独占，不存在多客户端并发写入同一 LocalDB 的场景。

---

## 安全 (Security)

### NFR-SEC-001: 认证与会话

| 配置 | 远程模式 | 本地模式 |
|------|---------|---------|
| AccessToken 有效期 | 60 分钟（v1.0 硬编码；可配置为 v2.0 规划） | 1 年 |
| RefreshToken 有效期 | 7 天（族旋转） | 不适用 |
| AutoLoginToken | 服务端可撤销，成功后轮换 | 不适用 |
| 登录限流 | 5 次/窗口（内网 20 次） | 5 次/分钟 |
| 账户锁定 | 可配置 `MaxFailedCount` / `LockoutMinutes` | 同左 |
| 保留用户名 | admin/administrator/root/system/superadmin/sysadmin | 同左 |

JWT 采用 Family-based Token Rotation 检测重放攻击（令牌族命中已撤销成员即全族撤销）。

### NFR-SEC-002: 密码策略

| 配置 | 值 |
|------|---|
| 最小长度 | 8 字符 |
| 复杂度 | 数字 + 小写 + 大写 + 特殊字符 |
| 首次登录强制修改 | 是 |
| 重置需 SuperAdmin 权限 | 是 |

### NFR-SEC-003: 传输与响应头安全

| 场景 | 措施 |
|------|------|
| 远程模式 | 生产强制 HTTPS + HSTS（max-age=1 年） |
| 安全响应头 | CSP / X-Frame-Options / X-Content-Type-Options / Referrer-Policy |
| CorrelationId | 端到端注入，贯穿日志与响应头 |

### NFR-SEC-004: 敏感数据分级与脱敏

| 级别 | 定义 | 字段示例 | 存储保护 | 日志保护 |
|------|------|---------|---------|---------|
| L1-高敏感 | 直接标识个人身份或联系方式 | IdNumber, PhoneNumber | 明文存储（远程 HTTPS + DB 访问控制） | 部分脱敏（保留前3后4） |
| L2-一般敏感（个人） | 个人敏感信息 | Address, AllergyHistory, MedicalHistory | 明文存储 + 访问控制 | 默认脱敏 / Hash 摘要 |
| L2-一般敏感（医疗） | 医疗诊断信息 | TcmDiagnosis, PresentIllness, TongueDiagnosis, PulseDiagnosis | 明文存储 + 访问控制 | 不记录到日志 |
| L3-普通 | 业务标识信息 | Name, Gender, BirthDate, HerbName | 明文存储 | 正常记录 |

**脱敏机制**: 通过 `[SensitiveData]` 特性声明字段级别与脱敏策略，在 Serilog `SensitiveDataMaskingEnricher` 层对业务代码透明地完成日志脱敏；在 API 响应层对非特权角色按策略掩码。

> **DPAPI 范围**: Windows DPAPI 当前仅用于照片、用户密码、令牌等凭证类数据的本地保护。**IdCardNumber 与 PhoneNumber 在数据库中为明文存储**，依靠传输加密（HTTPS）+ 数据库访问控制 + 日志脱敏三重保护，不实施字段级加密。

### NFR-SEC-005: 审计日志

| 日志类型 | 保留期 | 清理方式 |
|----------|--------|---------|
| 安全审计日志（SecurityAuditLog） | 365 天（可配置） | `SecurityAuditCleanupService` 定时清理 |
| 系统日志（SystemLog） | 90 天 | `LogCleanupService`（每 24h，每批 1000 条） |
| 文件日志（Serilog RollingFile） | 30 天 | Serilog 自动管理 |

审计覆盖：认证事件（登录/登出/失败/锁定）、权限变更、医案编辑（20 字段差异追踪）、敏感数据访问。

**验收标准**:
- [x] 安全审计日志默认 365 天可查询，保留期可通过系统配置调整
- [x] 超期日志自动清理，无需手动干预

---

## 可维护性 (Maintainability)

### NFR-MAINT-001: 架构测试守护

架构约束通过 `tests/LYBT.Tests.Architecture/` 自动化守护，CI 阻断违规：

| 约束 | 规则 |
|------|------|
| Server 模块隔离 | `LYBT.Module.*` 模块之间禁止直接引用（通过 WebAPI / Facade 协作） |
| 分层依赖方向 | Controller → Service → Repository → DbContext 单向依赖，禁止反向 |
| Desktop/Server 隔离 | `src/Client/` 与 `src/Server/` 禁止跨模块直接引用 |
| 测试替身限制 | Server 集成测试禁止使用 NSubstitute（采用真实 SQL Server + Respawn） |

### NFR-MAINT-002: 代码规范

| 维度 | 规范 | 落地 |
|------|------|------|
| 语言 | 中文业务文档与注释；英文技术标识符 | 评审 + `.editorconfig` |
| 命名 | `PascalCase`（公共）、`_camelCase`（私有字段）、`I` 前缀（接口） | `.editorconfig` |
| 包版本 | 统一在 `Directory.Packages.props` 声明，`.csproj` 不带版本号 | 中央包管理 |
| 注释 | 默认不添加代码注释，除非用户/评审要求 | 评审 |
| 测试哲学 | Testing Trophy（集成优先，Server 零 Mock） | `tests/LYBT.Tests.Server/` |

### NFR-MAINT-003: 结构化日志与可观测性

- 两阶段 Serilog 引导（WebAPI + Desktop）：捕获启动期错误到最终 Logger 就绪
- 全局 `CorrelationId` 贯穿 HTTP → Service → Repository → 日志
- 日志级别可通过 `/api/v1/Diagnostics` 端点在线调整（SuperAdmin），调试模式最长 120 分钟自动过期
- 全局异常处理（Dispatcher + AppDomain）统一捕获，生产环境屏蔽堆栈、返回友好中文消息 + TraceId

---

## 兼容性 (Compatibility)

### NFR-COMP-001: 操作系统

| 平台 | 支持 | 说明 |
|------|------|------|
| Windows 11 | ✅ 完全支持 | 推荐平台 |
| Windows 10 (1809+) | ✅ 完全支持 | 最低要求 |
| Windows 8.1 及以下 | ❌ 不支持 | .NET 8 Desktop Runtime 不支持 |
| Linux / macOS | ❌ 不支持 | WPF 桌面客户端仅 Windows |

Server 端（ASP.NET Core）理论上跨平台，但当前部署目标为 Windows Server + IIS。

### NFR-COMP-002: 运行时与框架

| 组件 | 版本 |
|------|------|
| .NET | 8.0 (LTS) |
| ASP.NET Core | 8.0 |
| EF Core | 8.0 |
| WPF | .NET 8 Desktop |
| Prism | 9.x（MVVM 框架） |

### NFR-COMP-003: 数据库

| 数据库 | 角色 | 版本 |
|--------|------|------|
| SQL Server | 远程主库 | 2019+（含 Express） |
| SQL Server LocalDB | 本地模式 | 2019+（随 Desktop 安装） |

> **废弃说明**: 早期本地模式使用 SQLite，自 Sprint 2 起迁移至 SQL Server LocalDB。SQLite 不再受支持，相关代码与文档已移除。

### NFR-COMP-004: 双模式兼容

| 维度 | 远程模式 | 本地模式 |
|------|---------|---------|
| 数据库 | SQL Server（远程） | SQL Server LocalDB |
| API 路由 | Refit 远程调用 | `SwitchingApiClient` 路由到内嵌 LocalWebAPI |
| Service 层 | 完全复用 | 完全复用（同一 Service 实现） |
| 认证 | 完整 JWT + Refresh | 简化 JWT（1 年） |
| 切换方式 | URL 配置 | URL 指向 localhost 即自动切换本地 |

两种模式共用同一套 Service 层实现，业务行为一致；差异仅限认证复杂度与数据存储位置。

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-17 | v1.0 | 初始版本（性能/数据/可用性/安全 4 维度） |
| 2026-06-15 | v2.0 | 重建：新增可维护性与兼容性维度；修正 DPAPI 范围（仅照片/密码/令牌，IdCardNumber/PhoneNumber 明文）；稳定 NFR ID 以支持交叉引用；SQLite 废弃说明 |
| 2026-06-25 | v2.1 | 修正跨文档不一致：AccessToken 2h→30min；备份保留 30d→7d；RTO 30min→1h |
| 2026-06-28 | v2.2 | 文档对齐：AccessToken 统一 60 分钟（代码 `AuthController.cs:98 AddMinutes(60)`），「可配置」标 v2.0；与 02-auth/03-users 密码策略统一 |
