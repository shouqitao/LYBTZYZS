# 挂号管理 (Registration Management)

> 版本: v2.0 | 日期: 2026-06-15 | 状态: 重建

## 模块概述

挂号（Registration）是患者就诊流程的系统化入口，用于管理患者分流、排队顺序和就诊可追溯性。系统支持**两种来源模式**：

- **前台模式（Source=Receptionist）**：前台接待员创建 `Waiting` 状态挂号，患者进入排队队列，医生从队列接诊。
- **医生模式（Source=Doctor）**：医生通过 QuickVisit 直接创建 `InProgress` 状态挂号，同时静默创建关联医案，跳过排队。

两种模式通过 `Source` 字段区分，医案状态变更时根据 Source 执行不同的联动策略（US-REG-007）。挂号模块确保 100% 就诊可追溯（COUNT(Registration) / COUNT(MedicalCase) = 1.0），为运营报表提供数据基础。

## 双模式工作流

> 详见 [R10 挂号→接诊工作流重设计 spec](../compose/specs/2026-06-28-registration-workflow-redesign.md)（S2/S3/S4）。此处的「双模式」指**部署模式**（远程/本地）下的工作流分野，区别于上文「两源模型」——后者描述同一远程模式内按 `Source` 字段的来源区分。

### 远程模式（有前台，挂号驱动）

```
前台建档(首诊) + 挂号(Waiting) → 待诊队列
        ↓ SignalR 推送通知医生（仅远程，见 ADR-0013）
医生待诊列表 → 选患者 →「开始就诊」StartVisit（US-REG-005）
        ↓ 原子事务：创建 MedicalCase(Active) + Registration(InProgress) + 返回 MedicalCaseId
导航医案编辑 → 望闻问切 → 开方 → 打印（系统终点）

急诊/特殊通道：医生 QuickVisit（US-REG-002）→ 选/建患者 → 原子创建 Registration+MedicalCase → 直接看诊
```

**要素**：前台挂号驱动；待诊队列；SignalR 推送（仅远程）；StartVisit 原子创建医案；QuickVisit 急诊通道并存。

### 本地模式（无前台，医生独立）

```
患者到诊 → 医生选/建患者（Patient 模块）→ 直接开医案（MedicalCase）→ 看诊 → 打印
```

**要素**：
- **取消挂号**：无 Registration 环节（医生看诊时不停下挂号）
- **无待诊队列**：待诊清单恒空
- **来一个看一个**：医生直接 Patient→MedicalCase
- **无 SignalR**：无队列无需推送
- 本质等同远程的 QuickVisit 急诊模式

**模式适用性**：Registration 模块在本地模式**不激活**；本地仅用 Patient + MedicalCase 模块。

## 业务规则

### 两源模型

| 属性 | 前台模式（Source=Receptionist） | 医生模式（Source=Doctor） |
|------|------------------------------|--------------------------|
| 创建者 | Receptionist | Doctor |
| 初始状态 | Waiting（排队） | InProgress（直接看诊） |
| 是否进入队列 | 是 | 否 |
| 医案创建时机 | 医生接诊时 | 创建挂号同时创建医案 |
| 取消策略 | 前台手动取消 | 医案取消自动闭环 |

### 状态流

```
前台模式: Waiting → InProgress → Completed
             ↓
         Cancelled

医生模式: InProgress → Completed
             ↓
         Cancelled
```

### 业务规则清单

| 编号 | 规则 | 说明 |
|------|------|------|
| REG-BR-001 | 取消前置校验 | 仅 Status=Waiting 的挂号可取消；有活跃/已完成医案时拒绝取消 |
| REG-BR-002 | 前台取消权限 | Source=Receptionist 的挂号仅 Receptionist 可取消，Doctor 无权 |
| REG-BR-003 | 医生模式跳过 Waiting | Source=Doctor 创建时直接进入 InProgress，不经过队列 |
| REG-BR-004 | 患者不存在时创建 | 查询无结果时提示创建患者 |
| REG-BR-005 | 回退后恢复原医案 | Source=Receptionist 医案取消回退 Waiting 后，医生重新接诊时恢复原 MedicalCase（IsDeleted=false, Status→Active） |
| REG-BR-006 | 患者侧大屏叫号（R9 决策） | 患者侧候诊大屏叫号属 **v2.0 / 按需**，v1.0 不实现；v1.0 候诊队列仅前台端（US-REG-004）与医生端可见 |

### QuickVisit 原子性

医生快速就诊（US-REG-002）使用 `TransactionScope(ReadCommitted)` 包裹 Registration + MedicalCase 两个实体的创建，确保原子性：要么同时成功，要么同时回滚。

### 并发保护

| 约束 | 规则 |
|------|------|
| PatientId+Date+Status 唯一约束 | 同一患者同一日期只允许一条活跃挂号（Status=Waiting 或 InProgress），防止重复挂号 |

### 用户角色权限

| 角色 | 主要操作 |
|------|---------|
| Receptionist (0) | 创建挂号（Waiting）、取消挂号、查看全部队列 |
| Doctor (1) | 查看个人队列、从队列接诊、QuickVisit 直接看诊 |
| Admin (10) | 查看全部队列和历史（只读统计） |
| SuperAdmin (100) | 与 Admin 相同（只读监控） |

## 用户故事

### US-REG-001: 前台创建挂号（Waiting 排队）

**角色**: 前台接待员
**优先级**: Must
**状态**: 🔴 权限策略 DoctorOrAdmin 阻断 Receptionist

**作为** 前台接待员，**我想要** 查询患者并创建挂号记录，**以便** 患者进入等待队列，医生可以按序接诊。

**验收标准**:
- [ ] 前台可通过姓名/拼音码/身份证号查询患者
- [ ] 患者不存在时提示是否创建新患者（REG-BR-004）
- [ ] 选择创建：补充必填信息（姓名、手机号）后创建患者，返回挂号界面
- [ ] 选择患者后，指派医生（从可用医生列表选择）
- [ ] 创建 Registration：`Source=Receptionist`、`Status=Waiting`
- [ ] 仅 Receptionist 角色可操作
- [ ] 患者 `Status=Disabled` → 返回 422（REG-70005）

**业务规则**:
1. Source=Receptionist，Status=Waiting（进入排队队列）
2. 指派医生（UserId）必填
3. 患者必须存在且 Enabled
4. 同一患者同日唯一活跃挂号约束（并发保护）
5. 仅 Receptionist 可创建

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/Registrations` |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `src/Server/Services/LYBT.WebAPI/Controllers/RegistrationsController.cs:24`、`IRegistrationService`

---

### US-REG-002: 医生快速就诊（QuickVisit 原子事务）

**角色**: 医生
**优先级**: Must
**状态**: 🧲 v1.0 待激活（急诊通道[远程] + 常规模式[本地]；当前 `QuickVisitAsync` Service 方法存在但无调用方＝死代码，见 [R10 spec S6](../compose/specs/2026-06-28-registration-workflow-redesign.md)）

> **定位补注**：QuickVisit 承担双重职能——(1) 远程模式下的**急诊/特殊通道**（前台不在或急症时医生直接接诊）；(2) 本地模式的**常规看诊入口**（本地取消挂号，「来一个看一个」本质即 QuickVisit）。当前为死代码，v1.0 待接线激活（QuickView UI 待实施）。

**作为** 医生，**我想要** 选择患者后直接进入看诊，**以便** 不需要额外的挂号步骤，前台不在时也能快速开始。

**验收标准**:
- [ ] 医生可通过姓名/拼音码/身份证号查询患者
- [ ] 患者不存在时提示是否创建新患者（REG-BR-004）
- [ ] 系统自动创建 Registration：`Source=Doctor`、`Status=InProgress`、`DoctorId=当前医生`（REG-BR-003）
- [ ] 同时自动创建 MedicalCase，关联 RegistrationId
- [ ] Registration + MedicalCase 在同一事务内（TransactionScope ReadCommitted），原子性保证
- [ ] 医生无感知 Registration 的存在（后台静默）

**业务规则**:
1. Source=Doctor，Status=InProgress（REG-BR-003，跳过 Waiting）
2. 自动创建 MedicalCase，关联 RegistrationId
3. **QuickVisit 原子性**：`TransactionScope(ReadCommitted)` 包裹 Registration + MedicalCase，要么同时成功要么同时回滚
4. 医生无感知 Registration 存在（静默创建）
5. 受 BR-001 单活跃医案约束（[07-medical-cases.md](07-medical-cases.md)）

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | POST `/api/v1/Registrations/quick-visit`（RegistrationService + IMedicalCaseCommandService） |
| 本地 | 完全一致（通过统一 Service 层，事务内联动创建） |

**实现参考**: `src/Server/Services/LYBT.WebAPI/Controllers/RegistrationsController.cs:24`、`IRegistrationService`（注入 `IMedicalCaseCommandService`）

---

### US-REG-003: 查看挂号详情

**角色**: 前台接待员、医生、管理员
**优先级**: Must
**状态**: ✅ 已实现

**作为** 前台或医生，**我想要** 查看单条挂号的完整信息，**以便** 了解患者指派、关联医案和状态流转。

**验收标准**:
- [ ] 有效 ID → 返回 `RegistrationDetailDto`（含患者姓名、医生姓名、状态、来源、关联医案 ID）
- [ ] 挂号不存在 → 返回 404（REG-70001）
- [ ] 关联医案 ID 可选（未接诊时为 null）

**业务规则**:
1. 返回挂号完整信息
2. 包含关联医案 ID（MedicalCaseId，接诊前为 null）
3. 包含患者姓名、医生姓名（冗余快照）
4. 权限：Receptionist/Doctor/Admin/SuperAdmin 均可查看

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | GET `/api/v1/Registrations/{id}` |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `src/Server/Services/LYBT.WebAPI/Controllers/RegistrationsController.cs:24`

---

### US-REG-004: 分页查询挂号 + 查看排队

**角色**: 前台接待员、医生、管理员
**优先级**: Must
**状态**: ✅ 已实现

**作为** 医生，**我想要** 查看当前等待接诊的患者队列，**以便** 按序选择患者开始看诊。

**验收标准**:
- [ ] 显示所有 `Status=Waiting` 且 `DoctorId=当前医生` 的挂号记录（Doctor 视图）
- [ ] Receptionist 可查看全部医生的队列（只读）
- [ ] 列表信息：患者姓名、挂号时间、等待时长
- [ ] 按挂号时间升序排列（先到先诊）
- [ ] 支持按日期范围、患者、医生、状态筛选
- [ ] 显示挂号时间、患者、医生、Source、Status、关联医案编号

**业务规则**:
1. Doctor 查看个人队列（Waiting 且 DoctorId=自己）
2. Receptionist/Admin 查看全部队列（只读）
3. 按挂号时间升序排列（先到先诊）
4. 列表信息含患者姓名、挂号时间、等待时长
5. 状态着色：Waiting=黄色、InProgress=蓝色、Completed=灰色、Cancelled=红色

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | GET `/api/v1/Registrations?status=&doctorId=&patientId=&startDate=&endDate=&page=&pageSize=` 或 GET `/api/v1/Registrations/queue` |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `src/Server/Services/LYBT.WebAPI/Controllers/RegistrationsController.cs:24`、`IRegistrationRepository`

---

### US-REG-005: 开始就诊（Waiting→InProgress）

**角色**: 医生
**优先级**: Must
**状态**: 🔴 代码待对齐（**D8 bug**：StartVisit 不创建医案 + 返回 RegistrationId 冒充 MedicalCaseId → Desktop 导航到空医案。**修复方向**——改为原子事务：`Registration.Status=InProgress` + 创建 `MedicalCase(Active)` 关联 `RegistrationId` + 返回 `MedicalCaseId`；复用 BR-001 单活跃医案约束，碰撞时提示「重开现有医案」。见 [R10 spec S5](../compose/specs/2026-06-28-registration-workflow-redesign.md)）

**作为** 医生，**我想要** 从队列选中患者开始就诊，**以便** 系统自动创建医案并将挂号状态转为进行中。

**验收标准**:
- [ ] 医生选中 Waiting 挂号 → 自动创建 MedicalCase，Registration 状态转为 InProgress
- [ ] Registration.MedicalCaseId 填充为新建医案 ID
- [ ] 若患者已有活跃医案 → 触发 BR-001 碰撞处理（[07-medical-cases.md](07-medical-cases.md)）

**业务规则**:
1. Waiting → InProgress 状态转换
2. 同时创建 MedicalCase 并关联 RegistrationId
3. 受 BR-001 单活跃医案约束
4. 医案创建后医生进入诊疗工作流

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | PUT `/api/v1/Registrations/{id}/start` 或通过接诊接口 |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `src/Server/Services/LYBT.WebAPI/Controllers/RegistrationsController.cs:24`、`IRegistrationService`

---

### US-REG-006: 取消挂号（仅 Waiting）

**角色**: 前台接待员
**优先级**: Must
**状态**: 🔴 权限策略 DoctorOrAdmin 阻断 Receptionist

**作为** 前台接待员，**我想要** 取消等待中的挂号记录，**以便** 患者临时不看或医生建议取消时能正确处理。

**验收标准**:
- [ ] 仅 Receptionist 可取消 `Source=Receptionist` 的挂号（REG-BR-002）
- [ ] 仅 `Status=Waiting` 的挂号可取消（REG-BR-001）
- [ ] 取消前校验：无关联医案 OR 关联医案状态为 Cancelled
- [ ] 有 Active/Suspended/Completed 医案时拒绝取消，提示原因（REG-70003）
- [ ] 取消后 Status → Cancelled
- [ ] Doctor 无权执行此操作（REG-70004）

**业务规则**:
1. **REG-BR-001 取消前置校验**：无关联医案 OR 关联医案状态为 Cancelled，否则拒绝取消
2. **REG-BR-002 前台取消权限**：Source=Receptionist 的挂号仅 Receptionist 可取消
3. 仅 `Status=Waiting` 可取消（InProgress 的挂号需先取消医案联动处理）
4. 取消后 Status=Cancelled

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | PUT `/api/v1/Registrations/{id}/cancel` |
| 本地 | 完全一致（通过统一 Service 层） |

**实现参考**: `src/Server/Services/LYBT.WebAPI/Controllers/RegistrationsController.cs:24`、`IRegistrationService`

---

### US-REG-007: 医案联动（完成/取消自动回写）

**角色**: 系统
**优先级**: Must
**状态**: ✅ 已实现

**作为** 系统，**我想要** 医案状态变更时自动更新关联 Registration 状态，**以便** 挂号记录与医案状态保持一致，无需人工操作。

**验收标准**:
- [ ] MedicalCase 完成时（CaseStatus=Completed）→ 关联 Registration.Status 自动变为 Completed
- [ ] MedicalCase 取消时（IsDeleted=true）→ 根据 Source 执行不同策略：
  - Source=Receptionist：Registration.Status 回退为 Waiting（等前台取消），MedicalCaseId 保留（用于恢复原医案，REG-BR-005）
  - Source=Doctor：Registration.Status 自动变为 Cancelled（流程完全闭环）
- [ ] 适用于所有 Source 类型
- [ ] 联动在 MedicalCaseService 内部触发，无需人工操作

**业务规则**:
1. **完成联动**：MedicalCase.CompleteAsync() 内部调用 RegistrationService.CompleteByMedicalCase()，Registration → Completed
2. **取消联动（Source-aware）**：MedicalCase.CancelAsync() 内部根据 Source 调用不同方法
   - Source=Receptionist：回退为 Waiting，MedicalCaseId 保留（REG-BR-005，支持恢复原医案）
   - Source=Doctor：自动变为 Cancelled（闭环）
3. 联动在事务内执行，保证一致性

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | MedicalCaseService 内部触发（CompleteAsync/CancelAsync 调用 RegistrationService） |
| 本地 | 完全一致（通过统一 Service 层，事务内联动更新） |

**实现参考**: `src/Server/Services/LYBT.WebAPI/Controllers/RegistrationsController.cs:24`、`src/Server/Modules/LYBT.Module.MedicalCase/Interfaces/IMedicalCaseFacade.cs:15`

---

### US-REG-008: 医生工作台待诊列表实时更新

**角色**: 医生
**优先级**: Must
**状态**: 🧲 v1.0 待实现

**作为** 医生，**我想要** 工作台待诊列表实时刷新，**以便** 第一时间看到新挂号和状态变更，无需手动刷新。

**验收标准**:
- [ ] 新挂号（指派给当前医生）创建后 N 秒内自动出现在医生工作台待诊列表（N 待专项 spec 定义，目标 ≤ 3 秒）
- [ ] 挂号状态变更（开始就诊 / 取消）实时同步到相关医生的待诊列表
- [ ] 推送通道失败时，医生端自动降级为轮询（复用 [US-REG-004](#us-reg-004-分页查询挂号--查看排队) 候诊队列接口），保证列表最终一致
- [ ] 仅推送与当前医生相关的事件（DoctorId 过滤），不越权推送

**业务规则**:
1. 依赖 SignalR 推送通道，架构决策见 [ADR-0013](../03-architecture/decisions/0013-signalr-realtime-push.md)
2. 推送内容：新挂号、挂号状态变更（开始就诊 / 取消）
3. 降级策略：推送失败回退轮询，复用 US-REG-004 候诊队列
4. 双模式推送机制（远程 Hub / 本地模式）待 SignalR 专项 spec 定案，本 US 仅锚定范围

**双模式**:
| 模式 | 行为 |
|------|------|
| 远程 | SignalR Hub（部署于 WebAPI，公网可达）推送挂号变更事件 |
| 本地 | 待专项 spec 定案（候选：LocalWebAPI 内嵌 Hub / 前端轮询降级） |

**实现参考**: SignalR Hub（待专项 spec 设计，见 [ADR-0013](../03-architecture/decisions/0013-signalr-realtime-push.md)）

---

## 边界条件验收标准

### InProgress 状态取消

- [ ] Status=InProgress 的挂号不可直接取消（REG-BR-001），需先取消关联医案
- [ ] 医生模式（Source=Doctor）InProgress 挂号 → 取消医案后 Registration 自动变为 Cancelled（US-REG-007 闭环）
- [ ] 前台模式（Source=Receptionist）InProgress 挂号 → 取消医案后 Registration 回退为 Waiting（US-REG-007）

### 同日重复挂号

- [ ] 同一患者同一天由前台创建第二条 Waiting 挂号 → 返回 422（PatientId+Date+Status 唯一约束）
- [ ] 同一患者同一天已有 Waiting 挂号，医生 QuickVisit → 返回 422（唯一约束冲突），提示先处理已有挂号
- [ ] 同一患者同一天已有 Cancelled 挂号，再次创建 Waiting 挂号 → 允许（Cancelled 不参与唯一约束）

## 交叉引用

- [医案管理 BR-001 单活跃医案约束](07-medical-cases.md)（QuickVisit 受此约束）
- [医案管理 US-MC-011 完成医案](07-medical-cases.md)（完成联动触发）
- [医案管理 US-MC-014 取消医案](07-medical-cases.md)（取消联动 Source-aware 策略）
- [患者管理](04-patients.md)（挂号依赖患者存在）
- [用户管理](03-users.md)（医生列表 Role=Doctor）
- [术语表 Registration = 挂号](../01-product/03-glossary.md)

## 变更记录

| 日期 | 变更 | 原因 |
|------|------|------|
| 2026-06-28 | 新增「双模式工作流」段（远程挂号驱动/本地取消挂号）；US-REG-002 状态改 🧲 v1.0 待激活+定位补注；US-REG-005 D8 修复方向补注 | R10 spec S8 文档更新 |
| 2026-06-25 | 补充 InProgress 取消、同日重复挂号边界条件验收标准 | 需求文档验收标准完善 |
| 2026-06-28 | 补 REG-BR-006 患者侧大屏叫号业务规则（R9：v2.0/按需，v1.0 候诊队列仅前台/医生端） | spec S7 弱反映项补全 |
