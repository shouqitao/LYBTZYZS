# PRD Review & Refine 修复计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 修复 PRD Review & Refine 阶段发现的 9 个问题，确保 15 模块 138 US 的文档体系完全一致。

**Architecture:** 纯文档修复任务，不涉及代码。按文件分组，先修快速同步项 (README/data-model)，再补 registration.md 标准章节，最后更新 user-story-map 和 clinical-workflow。

**Tech Stack:** Markdown 文档

---

## Phase 1: 快速同步 (README + data-model)

### Task 1.1: 更新 README.md 模块索引

**Files:**
- Modify: `docs/02-requirements/README.md`

**Step 1:** 在模块索引表 (L26 配置参数行之后) 新增 Registration 行:

```markdown
| 挂号管理 | [registration](../../../02-requirements/08-registration.md) | US-REG-001 ~ 007 | 7 |
```

**Step 2:** 更新底部统计 (L30):

```markdown
> **总计: 138 个 User Stories (15 个模块) + NFR 文档 (性能/数据量/可用性/安全)**
```

**Step 3:** 在 US 编号规则表 (L41) 的模块缩写中追加 `REG`:

```markdown
| 模块缩写 | `AUTH` / `USER` / `PAT` / `HERB` / `FORM` / `MC` / `SYNC` / `PRINT` / `CARD` / `SYS` / `ERR` / `LOG` / `SHELL` / `CFG` / `REG` | 见模块索引 |
```

**Step 4:** 更新变更记录追加 v2.1

**验证:** Registration 出现在索引表，总数显示 138

---

### Task 1.2: 更新 data-model.md 新增 Registration 实体

**Files:**
- Modify: `docs/03-architecture/04-data-model.md`

**Step 1:** 在 ER 图 mermaid 块中新增 Registration 关系:

```mermaid
Registration }o--|| Patient : "N:1"
Registration }o--|| User : "N:1 (指派医生)"
Registration ||--o| MedicalCase : "1:0..1"
```

**Step 2:** 在独立实体图中新增 Registration:

```mermaid
subgraph Independent["独立实体"]
    Patient
    User
    Herb
    Formula
    Registration
end

Registration -.->|PatientId| Patient
Registration -.->|DoctorId| User
Registration -.->|MedicalCaseId| MC
```

**Step 3:** 在实体字段定义区域新增 Registration 实体表:

```markdown
## Registration (挂号记录)

| 字段 | 类型 | 约束 | 说明 |
|------|------|------|------|
| Id | Guid | PK | 主键 (继承 BaseEntity) |
| PatientId | Guid | FK, Required | 关联患者 |
| DoctorId | Guid | FK, Required | 指派医生 |
| MedicalCaseId | Guid? | FK, Nullable | 关联医案 (接诊后填入) |
| Source | RegistrationSource | Required | Receptionist / Doctor |
| Status | RegistrationStatus | Required | Waiting / InProgress / Completed / Cancelled |

> Registration 继承 BaseEntity (含 Id, CreatedAt, UpdatedAt, IsDeleted 等)
```

**验证:** data-model.md ER 图包含 Registration，字段表完整

---

## Phase 2: registration.md 标准章节补全

### Task 2.1: 补全 registration.md 10 章节结构

**Files:**
- Modify: `docs/02-requirements/08-registration.md`

**需补充的章节 (按 PRD Development 标准):**

1. **Problem Statement** (S1): 患者到达后缺少系统化的分流和排队机制，前台与医生之间的信息传递依赖口头沟通
2. **Target Users** (S2): 重构现有权限矩阵为标准 Target Users 格式
3. **Strategic Context** (S3): 业务目标 + Why Now (Registration 纳入 v1.0 决策)
4. **Solution Overview** (S4): 扩展现有模块概述，增加核心能力列表和生命周期
5. **Success Metrics** (S5): 挂号使用率、平均等待时间、模式分布比
6. **Epic Hypothesis** (S6): We believe that...
7. **Out of Scope** (S8): 预约挂号、排班管理、多科室分诊
8. **Dependencies & Risks** (S9): 依赖 Patients/Users/MC 模块
9. **Open Questions** (S10): 待确认问题
10. **Error Codes**: REG 错误码定义 (7xxxx 分区)
11. **Decision Log**: 引用设计文档中的 6 个决策
12. **Dual Mode 表格**: 为 7 个 US 补充远程/本地行为

**验证:** registration.md 包含完整的 PRD 10+4 章节结构

---

## Phase 3: user-story-map.md 更新

### Task 3.1: 新增 Registration 到 Narrative 1

**Files:**
- Modify: `docs/02-requirements/19-user-story-map.md`

**Step 1:** 在 Narrative 1 Activity 1 (患者登记) 和 Activity 2 (创建医案) 之间插入新 Activity: "挂号分流"

```markdown
### Activity 1.5: 挂号分流

**Steps:**
1. 前台创建挂号 (US-REG-001)
2. 医生从队列选择 (US-REG-003)
3. 或医生直接看诊 (US-REG-002)

**Tasks (纵向优先级):**

| Must Have | Should Have | Could Have |
|-----------|------------|------------|
| 前台创建挂号 (US-REG-001) | 挂号历史查询 (US-REG-007) | |
| 医生快速看诊 (US-REG-002) | | |
| 查看挂号队列 (US-REG-003) | | |
| 前台取消挂号 (US-REG-004) | | |
| 状态自动跟随医案完成 (US-REG-005) | | |
| 医案取消联动 (US-REG-006) | | |
```

**Step 2:** 更新 Release Slices:

Release 1 (Must Have) 新增:
```markdown
**挂号管理**: US-REG-001, 002, 003, 004, 005, 006 (6)
```
Must Have 合计: 45 -> **51 US**

Release 2 (Should Have) 新增:
```markdown
**挂号管理**: US-REG-007 (1)
```
Should Have 合计: 53 -> **54 US**

**Step 3:** 更新分布统计表:

```markdown
| Must Have | 51 | 37.0% |
| Should Have | 54 | 39.1% |
| Could Have | 33 | 23.9% |
| **合计** | **138** | **100%** |
```

**Step 4:** 更新文件头的数据来源:

```markdown
> **数据来源**: clinical-workflow.md + JTBD (10 个) + 138 US (15 个模块)
```

**Step 5:** 更新 Release 2 认证部分 (US-AUTH-007 已 Removed):

```markdown
**认证**: US-AUTH-004, 006, 011, 013 (4)
```
(移除 US-AUTH-007，53 - 1 + 1 REG + 1 AUTH-013 调整 = 54)

**验证:** user-story-map.md 总数 138，MoSCoW 分布 51/54/33

---

## Phase 4: clinical-workflow.md 补充

### Task 4.1: 补充 Registration 取消联动描述

**Files:**
- Modify: `docs/01-product/06-clinical-workflow.md`

**Step 1:** 在跨模块联动章节 (Section 四或八) 新增 Registration 状态联动说明:

```markdown
### Registration 医案取消联动

| 触发事件 | Source=Receptionist | Source=Doctor |
|---------|-------------------|---------------|
| 医案创建 | Status -> InProgress | (创建即 InProgress) |
| 医案 Completed | Status -> Completed | Status -> Completed |
| 医案 Cancelled | Status -> Waiting (等前台取消) | Status -> Cancelled (自动) |

取消挂号前置校验: 无关联医案 OR 关联医案 Cancelled。
```

**验证:** clinical-workflow.md 包含 Registration 取消联动规则

---

## 执行检查清单

- [ ] Task 1.1: README.md 索引更新 (15 模块, 138 US)
- [ ] Task 1.2: data-model.md 新增 Registration 实体
- [ ] Task 2.1: registration.md 标准章节补全
- [ ] Task 3.1: user-story-map.md 新增 Registration + 更新统计
- [ ] Task 4.1: clinical-workflow.md 补充取消联动

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-03-06 | v1.0 | 初始修复计划: 5 Tasks, 4 Phases |
