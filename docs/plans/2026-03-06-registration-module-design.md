# Registration 模块设计文档

> **创建日期**: 2026-03-06
> **状态**: 已确认
> **PRD**: [registration.md](../02-requirements/registration.md)
> **依赖模块**: Auth, Patients, Users, MedicalCase

---

## 设计背景

v1.0 PRD Phase 8 补全时确认 Registration (挂号) 纳入 v1.0 范围 (OQ-04 CLOSED)。clinical-workflow.md 已有流程设计，本文档聚焦技术设计决策。

---

## 核心设计决策

### D1: Registration 作为独立实体

**决策**: Registration 有独立的数据库表，不是 MedicalCase 的前置状态字段。

**理由**:
- 数据一致性: 每次就诊都有 Registration 记录，MedicalCaseId 非空 FK
- 审计完整性: 100% 就诊可追溯
- 报表统计: 统一查询 COUNT(Registration) 即日均就诊量

### D2: 双模式入口 (Source 字段)

**决策**: Registration.Source 区分 Receptionist / Doctor，决定状态流转和权限。

**前台模式** (Source=Receptionist):
- 前台手动创建 -> Waiting -> 医生从队列选中 -> InProgress
- 医案取消 -> 回退 Waiting (等前台取消)
- 取消权限: 仅 Receptionist

**医生模式** (Source=Doctor):
- 选择患者 -> 系统自动创建 Registration (直接 InProgress) + MedicalCase
- 医案取消 -> 自动 Cancelled
- 医生全程无感知 Registration 的存在

**理由**: 行业标准 Visit/Encounter 模式 (OpenMRS, Oscar EMR 等)。统一数据模型，两种入口终点一致。

### D3: 医生模式跳过 Waiting

**决策**: Source=Doctor 创建时直接 InProgress，不经过 Waiting 和队列。

**理由**: 医生直接看诊不需要排队等待。如果经过 Waiting 再从队列选，多一步无意义操作。

### D4: 取消挂号的分流策略

**决策**:
- Source=Receptionist: 医案取消 -> 回退 Waiting -> 前台手动取消 (Receptionist 权限)
- Source=Doctor: 医案取消 -> 自动 Cancelled (无需手动操作)

**理由**: 职责对等 -- 前台发起的流程由前台闭环；医生自动创建的由系统自动闭环。

### D5: 取消挂号前置校验

**决策**: 取消挂号要求 -- 无关联医案 OR 关联医案状态为 Cancelled。

**理由**: 有进行中 (Active/Suspended) 或已完成 (Completed) 的医案时，不允许取消挂号，保护数据一致性。

### D6: 患者不存在时的创建分支

**决策**: 两种模式均支持查询患者不存在时提示创建，创建后继续原模式流程。

| 模式 | 患者已存在 | 患者不存在 -> 创建后 |
|------|----------|-------------------|
| 前台 | 进入挂号界面 | 进入挂号界面 |
| 医生 | 直接看诊 | 直接看诊 |

**理由**: 不论患者是否存在，两种模式的终点不变。患者创建是中间插入步骤，不改变流程走向。

---

## 状态机

```
Waiting <-----> InProgress -----> Completed
   |
   v
Cancelled
```

### 完整转换矩阵

| 当前状态 | 目标状态 | 触发 | Source 限制 | 方式 |
|---------|---------|------|-----------|------|
| (new) | Waiting | 前台创建挂号 | Receptionist | 手动 |
| (new) | InProgress | 医生选择患者 | Doctor | 自动 |
| Waiting | InProgress | 医生从队列选中 | Receptionist | 自动 (创建 MC) |
| Waiting | Cancelled | 手动取消 | Receptionist | 手动 (REG-BR-001 校验) |
| InProgress | Completed | 医案 Completed | All | 自动跟随 |
| InProgress | Waiting | 医案 Cancelled | Receptionist | 自动回退 |
| InProgress | Cancelled | 医案 Cancelled | Doctor | 自动 |

### 禁止的转换

| 转换 | 原因 |
|------|------|
| Completed -> 任何 | 已完成不可变更 |
| Cancelled -> 任何 | 已取消不可变更 |
| InProgress -> Cancelled (Source=Receptionist) | 必须先回退 Waiting 再由前台取消 |
| Waiting -> Cancelled (Source=Doctor) | Doctor 模式不经过 Waiting |

---

## 实体关系

```
Registration (1) ---> (1) Patient
Registration (1) ---> (1) User [Doctor]
Registration (0..1) ---> (0..1) MedicalCase
```

### 与 MedicalCase 的关系

- MedicalCase.RegistrationId: 非空 FK (每个医案必须有挂号记录)
- Registration.MedicalCaseId: 可空 FK (Waiting 状态时无医案)
- 医案取消回退 Waiting 时: Registration.MedicalCaseId 清空

---

## 与现有模块的集成点

### MedicalCase 模块

| 事件 | Registration 响应 |
|------|------------------|
| MedicalCase 创建 | Registration.MedicalCaseId 填入; Status -> InProgress |
| MedicalCase Completed | Registration.Status -> Completed |
| MedicalCase Cancelled (Source=Receptionist) | Registration.Status -> Waiting; MedicalCaseId 清空 |
| MedicalCase Cancelled (Source=Doctor) | Registration.Status -> Cancelled |

### Patients 模块

| 场景 | 行为 |
|------|------|
| 查询患者不存在 | 提示创建，创建后继续挂号/看诊流程 |
| 患者禁用 | 不允许创建挂号 (复用 MedicalCase 的 Patient.Status=Enabled 校验) |

---

## Sprint 分配

| Sprint | US | 说明 |
|--------|-----|------|
| Sprint 2 | US-REG-001~006 (6 Must) | Registration 核心功能 + v1.0-alpha |
| Sprint 3 | US-REG-007 (1 Should) | 挂号历史查询 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-03-06 | v1.0 | 初始设计: 6 个核心决策 + 状态机 + 集成点 |
