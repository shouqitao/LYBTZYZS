# PRD 与设计文档对比分析

> **创建时间**: 2026-02-21
> **分析范围**: 16 个 PRD 文档 + 6 个架构文档 + 21 个设计文档 + 系统功能清单
> **目的**: 检查需求文档和设计文档的对应完整性，评估软件逻辑流程是否完整

---

## 一、PRD 覆盖状况

### 1.1 PRD 文档总览

| 类别 | PRD 文档 | FR 数量 | 版本 | 深化状态 |
|------|---------|---------|------|----------|
| 核心业务 | auth.md | 13 (FR-AUTH-001~013) | v1.3 | 已深化 |
| 核心业务 | users.md | 12 (FR-USER-001~012) | v1.3 | 已深化 |
| 核心业务 | patients.md | 13 (FR-PAT-001~013) | - | 已深化 |
| 核心业务 | herbs.md | 13 (FR-HERB-001~013) | - | 已深化 |
| 核心业务 | formulas.md | 13 (FR-FORM-001~013) | - | 已深化 |
| 核心业务 | medical-cases.md | 18 (FR-MC-001~018) | - | 已深化 |
| 支撑模块 | sync.md | 8 (FR-SYNC-001~008) | v3.1 | 已深化 (含 MedicalCase 同步) |
| 支撑模块 | printing.md | 4 (FR-PRINT-001~004) | v2.4 | 已深化 (含排版规格) |
| 支撑模块 | card-reader.md | 2 (FR-CARD-001~002) | v1.3 | 完整 |
| 基础设施 | health-diagnostics.md | 9 (FR-SYS-001~009) | v2.0 | 已深化 |
| 基础设施 | error-handling.md | 8 (FR-ERR-001~008) | v2.2 | 已深化 |
| 基础设施 | logging.md | 7 (FR-LOG-001~007) | v2.0 | 已深化 |
| 基础设施 | desktop-shell.md | 7 (FR-SHELL-001~007) | v2.1 | 已深化 |
| 基础设施 | configuration.md | 4 (FR-CFG-001~004) | v2.1 | 已深化 |
| 跨切面 | nfr.md | ~20 条规范 | v1.3 | 已深化 (含缓存/分页) |
| 跨切面 | ui-patterns.md | ~10 条规范 | v1.1 | 完整 |
| **合计** | **16 个文档** | **131+ FR** | | |

### 1.2 PRD 技术深度评估

PRD 文档已深度技术化，超越传统"需求文档"范畴，已吸收大量设计内容:

| 技术内容类型 | 覆盖的 PRD | 代表性示例 |
|-------------|-----------|-----------|
| API 端点定义 (方法+路径+参数) | auth, users, patients, herbs, formulas, medical-cases, sync, health-diagnostics | 每个 FR 都有远程/本地模式说明 |
| DTO 定义 (字段+类型+约束) | sync (4个), medical-cases, printing | SyncMetadataDto, MedicalCaseSyncDto, PrescriptionPrintModel |
| 数据模型 (实体+字段+关系) | 全部 14 个功能模块 | 每个 PRD 都有数据模型章节 |
| 错误码体系 (编号+HTTP+消息) | 7 个模块, 90+ 场景 | 5 位 MCCEE 编号体系 (1xxxx~7xxxx) |
| UI 布局 ASCII 图 | printing, sync, desktop-shell | A5 处方笺排版, 冲突解决对比, 启动画面 |
| 状态机定义 | auth (登录状态), medical-cases (编辑模式), desktop-shell (会话) | AuthState, MedicalCaseStatus, SessionState |
| 决策记录 | 全部 PRD | 每个 PRD 含独立决策表 (共 60+ 条决策) |
| 交叉引用 | 多个 PRD 互引 | auth<->users (AUTH-D07), medical-cases<->printing (MC-D15), logging<->health-diagnostics |

**结论**: PRD 层完整且已高度技术化。

---

## 二、架构文档覆盖状况

### 2.1 架构文档总览

| 文档 | 内容 | 覆盖范围 |
|------|------|---------|
| system-overview.md | 三层架构 (Server/Shared/Client), 33 个项目结构, 依赖方向规则, 模块通信 | 系统级 |
| data-model.md | EF Core 实体关系图, MedicalCase 聚合根边界, 14 个实体定义 | 数据层 |
| dual-mode.md | 远程/本地双模式策略模式, ConnectionMode, DataSource 切换 | 架构模式 |
| desktop.md | WPF + Prism 客户端架构, Shell/Roles/Modules/Core 分层 | 客户端 |
| server.md | ASP.NET Core 服务端架构, Controller->Service->Repository 分层 | 服务端 |
| shared.md | 共享层 (DTO/Components/Utilities) | 共享层 |

### 2.2 ADR (架构决策记录)

| ADR | 决策内容 | 影响模块 |
|-----|---------|---------|
| 0001 | MedicalCase 聚合根 | medical-cases |
| 0002 | 双模式架构 | sync, 全部模块 |
| 0003 | 集成优先测试策略 | 测试 |
| 0004 | 用户上下文传播 | auth, 全部模块 |
| 0005 | SuperAdmin 认证模块 | auth, users |
| 0006 | 组件分解模式 | desktop |

**结论**: 架构层覆盖系统级设计，提供了分层规则、模式选择和通信机制。

---

## 三、设计文档 (docs/plans/) 覆盖状况

### 3.1 设计文档分类

| 类别 | 文档 | 与 PRD 的关系 |
|------|------|-------------|
| **重构设计** | viewmodel-refactoring, dead-code-cleanup, desktop-ui-ux-optimization, unify-control-data-binding, resource-sink-refactor | 代码质量改进, 非功能设计 |
| **认证重构** | auth-architecture-refactor-design | 对应 auth.md (唯一有对应设计文档的业务模块) |
| **测试重构** | test-restructure-design/plan | 测试架构, 非功能设计 |
| **文档体系** | documentation-system-design/plan, requirements-deepening, doc-code-alignment, prd-completion, prd-deepening-outline | 文档改进, 非功能设计 |
| **数据清理** | remove-entity-audit-plan | 技术债务, 非功能设计 |
| **功能清单** | system-function-checklist | 实现状态评估, 非功能设计 |

### 3.2 关键发现: 缺少模块级技术设计文档

**除 auth-architecture-refactor-design 外，没有任何业务模块有独立的技术设计文档。**

| PRD 模块 | 对应设计文档 | 状态 |
|----------|------------|------|
| auth.md | auth-architecture-refactor-design.md | **有** |
| users.md | (无) | 缺失 |
| patients.md | (无) | 缺失 |
| herbs.md | (无) | 缺失 |
| formulas.md | (无) | 缺失 |
| medical-cases.md | (无) | 缺失 |
| sync.md | (无) | 缺失 |
| printing.md | (无) | 缺失 |
| card-reader.md | (无) | 缺失 |
| health-diagnostics.md | (无) | 缺失 |
| error-handling.md | (无) | 缺失 |
| logging.md | (无) | 缺失 |
| desktop-shell.md | (无) | 缺失 |
| configuration.md | (无) | 缺失 |

### 3.3 影响评估

此缺失的实际影响需要结合以下因素评估:

1. **PRD 已技术化**: PRD 文档已包含大量设计级内容 (API 规格, DTO, 错误码, 状态机), 部分弥补了设计文档的缺失
2. **架构文档已覆盖系统级**: 分层规则, 模式选择, 通信机制已有文档
3. **代码已实现**: 系统功能清单显示 97.5% FR 已实现, 代码本身是"活的设计文档"
4. **缺失的是中间层**: 从"PRD 需求"到"代码实现"之间的**模块级技术设计**没有独立文档化

---

## 四、软件逻辑流程完整性分析

### 4.1 已完整的流程

| 流程 | 覆盖文档 | 完整度 | 说明 |
|------|---------|--------|------|
| 认证流程 (登录->Token刷新->超时->登出) | auth.md FR-AUTH-001~013 | 完整 | 含状态机、事件体系、失败分级 |
| 用户 CRUD + 权限管理 | users.md FR-USER-001~012 | 完整 | 含四层角色、批量操作、密码管理 |
| 患者 CRUD + 导入导出 | patients.md FR-PAT-001~013 | 完整 | 含引用检查、身份证匹配、状态管理 |
| 药材 CRUD + 导入导出 | herbs.md FR-HERB-001~013 | 完整 | 含分类管理、批量操作、状态切换 |
| 验方 CRUD + 延迟绑定 | formulas.md FR-FORM-001~013 | 完整 | 含药材组成管理、共享机制、批量导入 |
| 医案生命周期 (创建->诊断->处方->打印->完成) | medical-cases.md FR-MC-001~018 | 完整 | 含聚合保存、锁定规则、审计日志 |
| 处方打印 (预览->打印->版本管理->日志) | printing.md FR-PRINT-001~004 | 完整 | 含 A5/A4 排版规格、分页规则 |
| 数据同步 (比对->冲突解决->上传/下载) | sync.md FR-SYNC-001~008 | 完整 | 含 MedicalCase 聚合同步、患者去重 |
| 异常处理链 (抛出->捕获->映射->展示) | error-handling.md FR-ERR-001~008 | 完整 | 含服务端/客户端双端处理、通知类型映射 |
| 日志体系 (结构化->审计->脱敏->清理) | logging.md FR-LOG-001~007 | 完整 | 含 CorrelationId 追踪、后台清理 |
| 模式切换 (远程<->本地) | sync.md FR-SYNC-008 + dual-mode.md | 完整 | 含未同步检查、回退策略 |

### 4.2 缺失或不完整的跨模块流程

#### GAP-FLOW-1: 端到端临床工作流 (未文档化)

**描述**: 从患者到达到离开的完整临床流程没有作为一个连贯的端到端流程文档化。

**涉及模块**: patients -> medical-cases -> printing -> sync

**预期流程**:
```
患者到达
  ├─ Receptionist: 读卡/登记 (card-reader + patients)
  │   ├─ 身份证已存在 -> 加载历史信息
  │   └─ 身份证不存在 -> 快速创建患者
  ├─ Doctor: 查看待诊队列 (medical-cases FR-MC-017)
  ├─ Doctor: 创建医案 (FR-MC-001)
  ├─ Doctor: 填写诊断 (FR-MC-002)
  ├─ Doctor: 开具处方 (FR-MC-004)
  │   ├─ 可选: 从验方导入 (FR-MC-016)
  │   └─ 可选: 复制历史处方
  ├─ Doctor: 保存医案 (FR-MC-005)
  ├─ Doctor: 打印处方 (FR-PRINT-001)
  │   └─ 打印后 IsPrinted=true (MC-D15)
  ├─ Doctor: 完成医案 (FR-MC-007)
  │   └─ 医案锁定 (FR-MC-014)
  └─ 患者离开
```

**当前状态**: 各步骤在各自 PRD 中有详细定义，但缺少一个统一的端到端流程图或时序图。

**影响**: 低。各模块 PRD 通过交叉引用已建立连接，开发者可拼合理解。但对新成员 onboarding 不够友好。

#### GAP-FLOW-2: 外出看诊离线完整工作流 (部分文档化)

**描述**: sync.md 的 "MedicalCase 同步设计" 章节有出诊场景描述，但流程停留在概述级别。

**当前覆盖**:
```
sync.md 已覆盖:
  [1] 出诊前准备 (药材/患者/验方同步)     概述级
  [2] 外出看诊 (查看历史/新建医案)         概述级
  [3] 返回同步 (上传/去重/冲突解决)        详细设计 (含 DTO, 错误码, UI)
```

**缺失部分**:
- 出诊前同步的具体操作步骤指南 (选择哪些数据同步)
- 离线期间的功能限制清单 (哪些功能可用/不可用)
- 返回后同步的完整操作手册 (步骤截图级)

**影响**: 中。技术实现已有设计，但运维/用户操作手册级别的流程不够详细。

#### GAP-FLOW-3: 安全事件联动流程 (分散文档化)

**描述**: 安全事件 (角色变更/用户禁用/密码修改) 触发的跨模块联动已在各 PRD 中通过决策记录交叉引用，但没有统一的安全事件流程图。

**涉及的交叉引用**:
- AUTH-D06: 单会话登录 -> Token Family 撤销 (auth.md)
- AUTH-D07: 角色变更 -> Token Family 撤销 (auth.md + users.md)
- FR-USER-005: 删除用户 -> Token 失效 + RefreshToken 清理 (users.md)
- FR-USER-011: 禁用用户 -> Token Family 失效 + 会话终止 (users.md)
- FR-USER-009: 密码修改 -> Token Family 失效 (users.md)

**当前状态**: 每个事件在各自 PRD 中有详细的业务规则和验收标准，通过决策编号 (AUTH-D06/D07, USER-D03) 建立关联。

**影响**: 低。信息完整但分散，可通过一个安全事件联动矩阵统一展示。

#### GAP-FLOW-4: Receptionist 角色完整工作流 (分散文档化)

**描述**: Receptionist 是最受限的角色，其功能边界分散在多个 PRD 中。

**已确定的权限**:
- patients: CRU (创建/查看/更新，无删除) -- users.md 决策 2
- card-reader: 使用读卡器 (前台挂号快速登记) -- card-reader.md
- medical-cases: 查看未完成医案简要提示 (时间+医生，不含诊断/处方详情) -- users.md 决策 2
- desktop-shell: 菜单子集可见 -- desktop-shell.md 菜单矩阵

**缺失**: 没有一个统一的 Receptionist 工作流文档描述其完整操作路径。

**影响**: 低。信息已分散在各 PRD 中，可通过 user-roles.md 补充。

### 4.3 模块内逻辑流程检查

| 模块 | 关键流程 | 状态 | 备注 |
|------|---------|------|------|
| auth | 登录状态机 (Idle->Validating->Active->Expired) | 完整 | FR-AUTH-010 有完整状态定义 |
| auth | Token 刷新失败分级 (网络->指数退避, Expired->AutoLogin, Revoked->清除) | 完整 | FR-AUTH-011 |
| auth | 凭证存储生命周期 (保存->DPAPI加密->HMAC校验->迁移) | 完整 | FR-AUTH-009 |
| medical-cases | 医案状态转换 (Draft->Active->Completed, 任意->Cancelled) | 完整 | FR-MC-007/008 + FR-MC-014 锁定规则 |
| medical-cases | 聚合保存流程 (MC+Consultation+Prescription+Items 原子操作) | 完整 | FR-MC-005 |
| medical-cases | 打印保护流程 (打印->IsPrinted=true->修改后重置->PrintVersion递增) | 完整 | MC-D15 + FR-PRINT-003 |
| sync | Checksum 比对流程 (元数据获取->SHA256比对->差异分类) | 完整 | FR-SYNC-002/003 |
| sync | MedicalCase 同步流程 (依赖排序->聚合上传->患者去重->编号重分配) | 完整 | sync.md v3.0 MedicalCase 同步设计 |
| sync | 冲突解决流程 (差异检测->左右对比UI->用户选择->执行) | 完整 | FR-SYNC-007 + 冲突解决 UI 章节 |
| sync | 模式切换流程 (未同步检查->网络检查->认证检查->切换->回退) | 完整 | FR-SYNC-008 |
| error-handling | 异常处理链 (抛出->ExceptionHandler->ProblemDetails->客户端解析->UI展示) | 完整 | FR-ERR-001~008 全链路 |
| logging | 日志生命周期 (写入->脱敏->存储->清理) | 完整 | FR-LOG-001~007 |
| desktop-shell | 启动流水线 (Pipeline->步骤执行->诊断->Splash->登录页) | 完整 | FR-SHELL-001/006 |
| desktop-shell | 会话生命周期 (登录->活跃->超时警告->过期->登出) | 完整 | FR-SHELL-003 |
| configuration | 生产环境启动验证 (Critical/Important/Optional 三级) | 完整 | FR-CFG-004 |
| nfr | 缓存失效 (写操作->标签清除->TTL 双保险) | 完整 | NFR 5.3 缓存失效映射 |
| nfr | 本地数据加密 (AES-256+DPAPI->ValueConverter->SQLite) | 完整 | NFR-SEC-004 |

---

## 五、缺口汇总与建议

### 5.1 文档层缺口

| 编号 | 缺口 | 优先级 | 建议 |
|------|------|--------|------|
| DOC-GAP-1 | 缺少模块级技术设计文档 (13/14 模块无独立设计文档) | **P3** | PRD 已技术化, 代码已实现, 补写设计文档 ROI 低。建议维护现有 PRD 质量即可 |
| ~~DOC-GAP-2~~ | ~~缺少端到端临床工作流图~~ | ~~P2~~ | **已完成**: `docs/01-product/clinical-workflow.md` (v1.0, 2026-02-21) |
| DOC-GAP-3 | 缺少外出看诊操作手册 | **P3** | 建议在 `docs/06-operations/` 新增, 待功能上线后编写 |
| DOC-GAP-4 | 安全事件联动未统一展示 | **P3** | 建议在 auth.md 新增"安全事件联动矩阵"章节 |
| DOC-GAP-5 | Receptionist 角色无统一工作流 | **P3** | 建议在 `docs/01-product/user-roles.md` 补充各角色工作流 |

### 5.2 PRD 内部一致性问题

| 编号 | 问题 | 涉及文档 | 说明 |
|------|------|---------|------|
| ~~CON-1~~ | ~~FR 编号不连续~~ | system-function-checklist.md | **已修复**: v3.0 更新 120->131 FR, 新增 11 条 FR 条目 (2026-02-21) |
| ~~CON-2~~ | ~~NFR-SEC-001 不活跃超时写 5 分钟~~ | nfr.md | **已修复**: 更新为 15 分钟 (2026-02-21) |
| ~~CON-3~~ | ~~system-function-checklist.md FR 计数过时~~ | system-function-checklist.md | **已修复**: v3.0 同步 (2026-02-21) |

### 5.3 流程完整性结论

| 维度 | 评估 | 得分 |
|------|------|------|
| 单模块内部流程完整性 | 所有 14 个功能模块的内部逻辑流程完整, 状态转换清晰, 错误处理覆盖 | **95/100** |
| 跨模块交叉引用 | PRD 之间通过决策编号 (AUTH-D06/D07, MC-D15, USER-D03, PAT-D03) 建立关联, 基本完整 | **85/100** |
| 端到端业务流程 | ~~缺少统一的临床工作流文档~~ 已补充 clinical-workflow.md | **85/100** |
| 安全流程一致性 | 安全事件联动通过交叉引用覆盖, 但缺少统一视图 | **80/100** |
| 双模式流程差异 | 每个 FR 都标注了远程/本地模式行为, 差异说明清晰 | **95/100** |
| **综合评分** | | **88/100** |

---

## 六、下一步行动建议

### 文档阶段 (已完成)

| # | 行动 | 状态 |
|---|------|------|
| 1 | 修复 NFR-SEC-001 不活跃超时值 (5->15 分钟) | **已完成** (v1.1) |
| 2 | 新增端到端临床工作流文档 `docs/01-product/clinical-workflow.md` | **已完成** (v1.1) |
| 3 | 更新系统功能清单 FR 计数 (120->131) | **已完成** (v1.1) |

### 代码阶段 (下一步)

> 以下为文档审查过程中发现的代码实现缺口，按优先级排列。
> 代码缺口详见 `system-function-checklist.md` v3.0 Section 3.1。

#### P1 -- 影响跨模块业务规则

| # | 缺口 | FR | 涉及层 | 说明 | 预估 |
|---|------|-----|--------|------|------|
| C1 | **患者状态管理 (启用/禁用)** | FR-PAT-013 | Server + Desktop | Patient 实体无 Status 字段。PatientsController 有注释 "无Status字段，无ToggleStatus端点"。缺失导致 MC-D16 (禁用患者禁止创建新医案 ERR-30105) 链路不可用。 | 1-2 天 |
| C2 | **Desktop 修改密码调用链** | FR-USER-009 | Desktop | Server API 已完整 (ChangePassword)，Desktop 仅占位实现。UserService.cs:325 有 TODO。 | 0.5 天 |

**C1 实施要点**:
- Patient 实体新增 `Status` 枚举属性 (Enabled/Disabled)，默认 Enabled
- EF Core Migration 新增字段 (不可空，默认 Enabled)
- PatientsController 新增 `PUT /api/v1/patients/{id}/toggle-status` 端点
- 业务规则: 有活跃医案 (Draft/Active) 的患者禁止禁用 (新增错误码)
- Desktop: PatientMasterDetailViewModel 新增禁用/启用按钮
- 联动: MedicalCaseService 创建医案时校验 Patient.Status (ERR-30105)
- 测试: Server 集成测试 + Desktop 单元测试

#### P2 -- 功能完善

| # | 缺口 | FR | 涉及层 | 说明 | 预估 |
|---|------|-----|--------|------|------|
| C3 | **患者->医案导航入口** | - | Desktop | PatientMasterDetailVM:408/418 两处 TODO 占位符。需实现从患者详情页导航到医案编辑/历史查看。 | 0.5 天 |
| C4 | **MedicalCase 查询 Repository 优化** | - | Server | 当前内存过滤 (MedicalCaseQueryService.cs:59)，应迁移到 Repository 层 IQueryable 查询。功能正确，性能优化项。 | 0.5 天 |

#### P2.5 -- 架构重构

| # | 缺口 | FR | 涉及层 | 说明 | 预估 |
|---|------|-----|--------|------|------|
| C6 | **打印层级重构 (处方层->医案层)** | FR-PRINT-001~004, FR-MC-015 | Server + Desktop | 打印从 Prescription 层提升到 MedicalCase 聚合根层。实体迁移: (1) MedicalCase 新增 PrintVersion 字段; (2) Prescription 移除 PrintVersion (保留 PrintCount/LastPrintedAt); (3) PrescriptionPrintLog 重命名为 MedicalCasePrintLog (FK PrescriptionId->MedicalCaseId, 新增 PrintType 枚举); (4) EF Core Migration; (5) PrescriptionPrintService 适配新模型; (6) Desktop ViewModel 更新; (7) 测试补充 | 2-3 天 |

**C6 实施要点**:
- MedicalCase 实体新增 `PrintVersion` (int, Default=1)，EF Core Migration (不可空，默认 1)
- Prescription 实体移除 `PrintVersion` 属性 (保留 `PrintCount` 和 `LastPrintedAt`)
- 新增 `PrintType` 枚举 (Prescription=0, Consultation=1, CaseSummary=2)
- `PrescriptionPrintLog` 重命名为 `MedicalCasePrintLog`: FK 从 `PrescriptionId` 改为 `MedicalCaseId`，新增 `PrintType` 字段
- `PrescriptionPrintService` 改为使用 `MedicalCase.PrintVersion` (非 Prescription.PrintVersion)
- MedicalCaseService 聚合保存中的打印保护逻辑: `MedicalCase.PrintVersion++` (非 Prescription)
- Server 集成测试 + Desktop 单元测试

#### P3 -- 低优先级

| # | 缺口 | FR | 涉及层 | 说明 | 预估 |
|---|------|-----|--------|------|------|
| C5 | **异常通知类型映射** | FR-ERR-008 | Desktop | ExceptionSeverity 枚举已有 (Information/Warning/Error/Critical)，缺少到 UI 通知类型 (Toast vs Dialog) 的显式映射。纯 UI 展示层。 | 0.5 天 |

### 文档阶段 P3 (后续)

| # | 行动 | 备注 |
|---|------|------|
| D1 | 补充 Receptionist 角色完整工作流 | 建议在 `docs/01-product/user-roles.md` 补充 |
| D2 | 补充安全事件联动矩阵 | 建议在 auth.md 新增章节 |
| D3 | 补充外出看诊操作手册 | 待功能上线后在 `docs/06-operations/` 编写 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-02-21 | v1.0 | 初始版本, 全量 PRD vs 设计文档对比分析 |
| 2026-02-21 | v1.1 | 执行 P1+P2 文档行动 (CON-1/2/3 修复, DOC-GAP-2 补充); 新增代码阶段任务 (C1~C5); 综合评分 85->88 |
| 2026-02-21 | v1.2 | 新增 C6 打印层级重构任务 (P2.5): 打印从处方层提升到医案层，含实体迁移方案和实施要点 |
