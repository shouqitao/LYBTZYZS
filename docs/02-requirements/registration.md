# Registration (挂号管理) -- 模块 PRD

> **版本**: v2.0
> **创建日期**: 2026-03-06
> **模块编号**: REG
> **依赖模块**: Auth, Patients, Users, MedicalCase

---

## 1. Problem Statement

### 1.1 问题描述

当前系统中，患者到达诊所后直接由医生在医案模块创建就诊记录，缺少系统化的分流和排队机制。前台与医生之间的患者流转依赖口头沟通，无法追踪等待时长和就诊量。

### 1.2 用户痛点

| 角色 | 痛点 | 影响 |
|------|------|------|
| 前台 (Receptionist) | 无法系统化管理患者排队顺序，高峰期混乱 | 患者等待体验差，可能遗漏 |
| 医生 (Doctor) | 不知道有多少患者在等待，无法规划接诊节奏 | 被动等待前台通知，效率低 |
| 管理员 (Admin) | 无法统计日均就诊量和等待时长 | 缺少运营数据支撑决策 |

### 1.3 证据

- 中医诊所行业标准: 挂号/登记是诊疗流程的标准第一步 (参考 OpenMRS Visit/Encounter 模式)
- clinical-workflow.md 阶段 1a/1b 已定义两种入口流程 (前台挂号 + 医生直接看诊)
- 用户反馈: 前台需要一个独立的挂号界面管理等待患者

---

## 2. Target Users

| 角色 | 权限 | 主要操作 |
|------|------|---------|
| Receptionist (前台) | 创建挂号、取消挂号、查看全部队列 | 患者到达时创建挂号记录，指派医生 |
| Doctor (医生) | 查看个人队列、从队列接诊、直接看诊 | 按序接诊或跳过挂号直接看诊 |
| Admin (管理员) | 查看全部队列和历史 (只读) | 统计就诊量 |
| SuperAdmin (超管) | 与 Admin 相同 (只读) | 系统级监控 |

> SuperAdmin 遵循权限值层级 (USER-D04)，在挂号模块与 Admin 权限对等: 查看全部队列和历史 (只读)，不参与挂号创建和取消操作。

---

## 3. Strategic Context

### 3.1 业务目标

- 100% 就诊可追溯: 每次就诊都有 Registration 记录，统一数据模型
- 支持两种入口模式: 有前台时走排队流程，无前台时医生直接看诊
- 为运营报表提供数据基础: COUNT(Registration) 即日均就诊量

### 3.2 Why Now

Registration 纳入 v1.0 的决策依据 (OQ-04 CLOSED):
- clinical-workflow.md 已定义完整流程，设计已成熟
- MedicalCase 模块已稳定，集成风险低
- 前台角色 (Receptionist) 已在 UserRole 枚举中定义，权限体系就绪

---

## 4. Solution Overview

### 4.1 核心能力

| 能力 | 说明 |
|------|------|
| 前台挂号 | Receptionist 查询/创建患者，指派医生，进入等待队列 |
| 医生直接看诊 | Doctor 选择患者，系统后台静默创建 Registration + MedicalCase |
| 等待队列 | 按挂号时间升序展示 Waiting 状态的挂号记录 |
| 状态自动联动 | Registration 状态跟随 MedicalCase 状态自动流转 |
| 取消分流 | 根据 Source 执行不同取消策略 (前台手动 vs 医生自动) |

### 4.2 Registration 生命周期

```
前台模式: (创建) -> Waiting -> InProgress -> Completed
                       |
                       v
                   Cancelled

医生模式: (创建) -> InProgress -> Completed
                       |
                       v
                   Cancelled
```

### 4.3 两种模式流程

**前台模式 (Source=Receptionist)**:
1. 前台查询患者 (不存在则提示创建)
2. 选择患者，指派医生
3. 创建 Registration (Waiting)
4. 医生从队列选中 -> 自动创建 MedicalCase -> Registration (InProgress)
5. 医案完成 -> Registration (Completed)
6. 或医案取消 -> Registration 回退 (Waiting) -> 前台手动取消 (Cancelled)

**医生模式 (Source=Doctor)**:
1. 医生查询患者 (不存在则提示创建)
2. 选择患者 -> 系统自动创建 Registration (InProgress) + MedicalCase
3. 医生无感知 Registration 存在
4. 医案完成 -> Registration (Completed)
5. 或医案取消 -> Registration (Cancelled) 自动闭环

---

## 5. Success Metrics

| 指标 | 目标 | 衡量方式 |
|------|------|---------|
| 挂号使用率 | 100% 就诊有 Registration 记录 | COUNT(Registration) / COUNT(MedicalCase) = 1.0 |
| 平均等待时间 | < 15 分钟 (Waiting -> InProgress) | AVG(InProgress时间 - CreatedAt) WHERE Source=Receptionist |
| 模式分布比 | 可追踪 | COUNT by Source |
| 取消率 | < 10% | COUNT(Cancelled) / COUNT(Total) |

---

## 6. Epic Hypothesis

We believe that **providing a systematic registration and queuing mechanism** for **Receptionists and Doctors** will **reduce patient wait confusion, enable data-driven capacity planning, and ensure 100% visit traceability**. We will know this is true when **every MedicalCase has an associated Registration record and average wait time is under 15 minutes**.

---

## 7. User Stories

### 优先级汇总

| 优先级 | 数量 | US 编号 |
|--------|------|---------|
| Must | 6 | US-REG-001 ~ 006 |
| Should | 1 | US-REG-007 |
| Could | 0 | - |
| **合计** | **7** | |

---

### US-REG-001: 前台创建挂号 (Must)

**As a** Receptionist
**I want to** 查询患者并创建挂号记录
**So that** 患者进入等待队列，医生可以按序接诊

**验收标准:**
- [ ] 前台可通过姓名/拼音码/身份证号查询患者
- [ ] 患者不存在时提示是否创建新患者
- [ ] 选择创建: 补充必填信息 (姓名、手机号) 后创建患者，返回挂号界面
- [ ] 选择患者后，指派医生 (从可用医生列表选择)
- [ ] 创建 Registration: Source=Receptionist, Status=Waiting
- [ ] 仅 Receptionist 角色可操作

**Dual Mode:**

| 模式 | 行为 |
|------|------|
| 远程 | WPF -> HTTP POST /api/registrations -> RegistrationController -> RegistrationService -> SQL Server |
| 本地 | WPF -> LocalDataSource -> SQLite |

---

### US-REG-002: 医生快速看诊 (Must)

**As a** Doctor
**I want to** 选择患者后直接进入看诊
**So that** 不需要额外的挂号步骤，前台不在时也能快速开始

**验收标准:**
- [ ] 医生可通过姓名/拼音码/身份证号查询患者
- [ ] 患者不存在时提示是否创建新患者
- [ ] 选择创建: 补充必填信息后创建患者，直接进入看诊
- [ ] 系统自动创建 Registration: Source=Doctor, Status=InProgress, DoctorId=当前医生
- [ ] 同时自动创建 MedicalCase，关联 RegistrationId
- [ ] 医生无感知 Registration 的存在 (后台静默)

**Dual Mode:**

| 模式 | 行为 |
|------|------|
| 远程 | WPF -> HTTP POST /api/registrations/quick-visit -> RegistrationController -> RegistrationService + MedicalCaseService -> SQL Server |
| 本地 | WPF -> LocalDataSource -> SQLite (事务: Registration + MedicalCase 同步创建) |

---

### US-REG-003: 查看挂号队列 (Must)

**As a** Doctor
**I want to** 查看当前等待接诊的患者队列
**So that** 按序选择患者开始看诊

**验收标准:**
- [ ] 显示所有 Status=Waiting 且 DoctorId=当前医生的挂号记录
- [ ] 列表信息: 患者姓名、挂号时间、等待时长
- [ ] 按挂号时间升序排列 (先到先诊)
- [ ] 医生选中患者后，自动创建 MedicalCase，Registration 状态转为 InProgress
- [ ] Receptionist 可查看全部医生的队列 (只读)

**Dual Mode:**

| 模式 | 行为 |
|------|------|
| 远程 | WPF -> HTTP GET /api/registrations/queue -> RegistrationController -> RegistrationService -> SQL Server |
| 本地 | WPF -> LocalDataSource -> SQLite |

---

### US-REG-004: 前台取消挂号 (Must)

**As a** Receptionist
**I want to** 取消等待中的挂号记录
**So that** 患者临时不看或医生建议取消时能正确处理

**验收标准:**
- [ ] 仅 Receptionist 可取消 Source=Receptionist 的挂号
- [ ] 仅 Status=Waiting 的挂号可取消
- [ ] 取消前校验 (REG-BR-001): 无关联医案 OR 关联医案状态为 Cancelled
- [ ] 有 Active/Suspended/Completed 医案时拒绝取消，提示原因
- [ ] 取消后 Status -> Cancelled
- [ ] Doctor 无权执行此操作

**Dual Mode:**

| 模式 | 行为 |
|------|------|
| 远程 | WPF -> HTTP PUT /api/registrations/{id}/cancel -> RegistrationController -> RegistrationService -> SQL Server |
| 本地 | WPF -> LocalDataSource -> SQLite |

---

### US-REG-005: 状态自动跟随医案完成 (Must)

**As a** system
**I want to** 医案完成时自动更新 Registration 状态
**So that** 挂号记录与医案状态保持一致

**验收标准:**
- [ ] MedicalCase.Status 变为 Completed 时，关联 Registration.Status 自动变为 Completed
- [ ] 无需人工操作
- [ ] 适用于所有 Source 类型

**Dual Mode:**

| 模式 | 行为 |
|------|------|
| 远程 | MedicalCaseService.CompleteAsync() 内部调用 RegistrationService.CompleteByMedicalCase() |
| 本地 | 同远程，事务内联动更新 |

---

### US-REG-006: 医案取消联动 (Must)

**As a** system
**I want to** 医案取消时根据挂号来源执行不同策略
**So that** 前台挂号走前台取消流程，医生挂号自动闭环

**验收标准:**
- [ ] 医案取消时检查 Registration.Source:
  - Source=Receptionist: Registration.Status 回退为 Waiting (等前台取消)
  - Source=Doctor: Registration.Status 自动变为 Cancelled
- [ ] Source=Receptionist 回退后，MedicalCaseId **保留** (用于恢复原医案)
- [ ] Source=Doctor 自动取消后，流程完全闭环

**Dual Mode:**

| 模式 | 行为 |
|------|------|
| 远程 | MedicalCaseService.CancelAsync() 内部根据 Source 调用 RegistrationService 不同方法 |
| 本地 | 同远程，事务内联动更新 |

---

### US-REG-007: 挂号历史查询 (Should)

**As a** Receptionist/Doctor
**I want to** 查询历史挂号记录
**So that** 统计就诊量、追溯就诊历史

**验收标准:**
- [ ] 支持按日期范围、患者、医生筛选
- [ ] 显示: 挂号时间、患者、医生、Source、Status、关联医案编号
- [ ] 支持导出 (Could, 后续考虑)

**Dual Mode:**

| 模式 | 行为 |
|------|------|
| 远程 | WPF -> HTTP GET /api/registrations/history -> RegistrationController -> RegistrationService -> SQL Server |
| 本地 | WPF -> LocalDataSource -> SQLite |

---

## 8. Out of Scope (v1.0 排除项)

| 排除项 | 原因 |
|--------|------|
| 预约挂号 | v2.0 考虑，当前仅支持现场挂号 |
| 排班管理 | 医生排班属于独立模块，v1.0 由管理员人工指派 |
| 多科室分诊 | 单诊所单科室 (中医)，无分诊需求 |
| 挂号费管理 | 当前诊所不收挂号费，费用在处方环节 |
| 挂号号码/序号 | 小诊所患者量不大 (日均 < 50)，按时间排序即可 |

---

## 9. Dependencies & Risks

### 依赖

| 依赖模块 | 依赖内容 | 影响 |
|---------|---------|------|
| Patients | 患者查询和创建 | Registration 必须关联有效 Patient |
| Users | 医生列表 (Role=Doctor) | 指派医生需从 User 表筛选 |
| MedicalCase | 医案创建和状态变更 | Registration 状态联动依赖 MedicalCase 事件 |
| Auth | 角色权限验证 | Receptionist/Doctor 操作权限区分 |

### 风险

| 风险 | 影响 | 缓解 |
|------|------|------|
| MedicalCase 取消联动复杂性 | Source 分流逻辑增加代码复杂度 | 状态机严格定义，禁止非法转换 |
| 两种模式数据一致性 | 医生模式静默创建可能遗漏 | Registration 创建封装在 Service 层，统一入口 |
| 离线模式同步冲突 | Registration + MedicalCase 需原子同步 | 事务保证一致性 |

---

## 10. Open Questions

| 编号 | 问题 | 状态 | 决策 |
|------|------|------|------|
| REG-OQ-01 | Registration 是否需要独立的导航页面? | OPEN | 待 UI 设计确认 |
| REG-OQ-02 | 医生模式下是否需要确认对话框? | CLOSED | 不需要，静默创建 (D3) |
| REG-OQ-03 | Receptionist 是否可以为自己创建挂号? | OPEN | 待确认 |

---

## Data Model

详见 [data-model.md](../03-architecture/data-model.md) Registration 章节。

核心关系:
- Registration -> Patient: N:1
- Registration -> User (Doctor): N:1
- Registration -> MedicalCase: 1:0..1

---

## Error Codes

| 错误码 | 名称 | 说明 |
|--------|------|------|
| REG-70001 | RegistrationNotFound | 挂号记录不存在 |
| REG-70002 | InvalidStatusTransition | 非法状态转换 |
| REG-70003 | CancelNotAllowed | 有活跃/已完成医案，不允许取消 |
| REG-70004 | UnauthorizedCancel | 无权取消此挂号 (非 Receptionist 或非本人创建) |
| REG-70005 | PatientDisabled | 患者已禁用，不允许创建挂号 |
| REG-70006 | DoctorNotAvailable | 指派医生不可用 (禁用/不存在) |
| REG-70007 | DuplicateWaiting | 该患者已有等待中的挂号记录 |

---

## Decision Log

| 编号 | 决策 | 理由 | 来源 |
|------|------|------|------|
| REG-D01 | Registration 作为独立实体 | 数据一致性 + 审计完整性 + 报表统计 | registration-module-design.md D1 |
| REG-D02 | Source 字段区分双模式入口 | 行业标准 Visit/Encounter 模式 (OpenMRS, Oscar EMR) | registration-module-design.md D2 |
| REG-D03 | 医生模式跳过 Waiting | 医生直接看诊无需排队 | registration-module-design.md D3 |
| REG-D04 | 取消挂号按 Source 分流 | 职责对等: 前台发起由前台闭环，医生自动创建由系统闭环 | registration-module-design.md D4 |
| REG-D05 | 取消前置校验 (REG-BR-001) | 有进行中/已完成医案时保护数据一致性 | registration-module-design.md D5 |
| REG-D06 | 患者不存在时支持创建 | 两种模式终点不变，患者创建是中间插入步骤 | registration-module-design.md D6 |

---

## Business Rules

| 编号 | 规则 | 说明 |
|------|------|------|
| REG-BR-001 | 取消挂号前置校验 | 无关联医案 OR 关联医案状态为 Cancelled，否则拒绝取消 |
| REG-BR-002 | 前台挂号取消权限 | Source=Receptionist 的挂号仅 Receptionist 可取消，Doctor 无权 |
| REG-BR-003 | 医生模式跳过 Waiting | Source=Doctor 创建时直接进入 InProgress，不经过队列 |
| REG-BR-004 | 患者不存在时创建 | 查询无结果时提示创建患者，补充必填信息后继续原流程 |
| REG-BR-005 | 回退后医生重新接诊恢复原医案 | Source=Receptionist 医案取消回退 Waiting 后，医生从队列选中时**恢复**原 MedicalCase (IsDeleted=false, Status -> Active)，保留诊断/处方数据。v1.0 不支持改派，仅原医生可重新接诊 |

---

## Change Log

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-03-06 | v1.0 | 初始版本: 7 US (6 Must + 1 Should)，双模式设计 |
| 2026-03-06 | v2.0 | **PRD 标准化**: 补全 10 章节结构 (Problem Statement / Target Users / Strategic Context / Solution Overview / Success Metrics / Epic Hypothesis / Out of Scope / Dependencies & Risks / Open Questions); 新增 Error Codes (7 个) / Decision Log (6 个) / Dual Mode 表格 (7 US); 新增 Business Rules 章节 |
