# PRD 完善实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 基于 PM Skills 框架，为 LYBTZYZS 项目 PRD 补齐 Personas、JTBD、User Story Map、优先级排序四大维度，并深化顶层 PRD 文档。

**Architecture:** 文档驱动，不涉及代码变更。所有产出物位于 `docs/` 目录。依赖链: 审计修复 -> Personas + JTBD (并行) -> User Story Map -> 优先级排序 -> PRD 深化。

**Tech Stack:** Markdown 文档，PM Skills 框架 (prd-development / user-story-mapping / prioritization-advisor / proto-persona / jobs-to-be-done)

**前置事实 (2026-03-06 调研确认):**
- Phase 1 (Problem Statement 补齐) 已在上次会话完成: 全部 16 个模块文件均已有完整的 Problem Statement (1.1 问题描述 + 1.2 用户痛点 + 1.3 证据)
- glossary.md GLOSSARY-01 (Draft->Suspended) 已修复
- data-model.md 部分审计项已修复
- server.md 审计修复 (PRD-02/03/04/07/08) 未执行
- 未修改文件清单 (git diff 确认): server.md

---

## Phase 0: 完成剩余审计修复

**目标:** 修复 server.md 中 5 项审计偏差 (PRD-02/03/04/07/08)
**时间:** 30 分钟
**依赖:** 无

### Task 0.1: 读取 server.md 现状和代码事实

**Files:**
- Read: `docs/03-architecture/03-server.md`
- Read: `src/Server/LYBT.Server/Domain/Entities/BaseEntity.cs`
- Read: `src/Server/LYBT.Server/Infrastructure/Repositories/BaseRepository.cs`

**Step 1:** 读取 server.md 全文，定位以下章节:
- BaseEntity 表格 (对应 PRD-02)
- BaseRepository 方法列表 (对应 PRD-03)
- 模块列表 (对应 PRD-04)
- "14 个标准方法" 说法 (对应 PRD-07)
- BaseReadRepository 引用 (对应 PRD-08)

**Step 2:** 读取 `BaseEntity.cs`，记录实际字段列表 (确认是否包含 UpdatedBy / RowVersion)

**Step 3:** 读取 `BaseRepository.cs`，记录实际公开方法签名列表

**Step 4:** 搜索确认 Module.Consultation 和 Module.Prescriptions 是否已在代码中删除:
```bash
grep -r "Module.Consultation\|Module.Prescriptions" src/
```

### Task 0.2: 修复 server.md 5 项审计偏差

**Files:**
- Modify: `docs/03-architecture/03-server.md`

**Step 1:** 修复 PRD-02: BaseEntity 表格补充 UpdatedBy / RowVersion (对齐代码实际字段)

**Step 2:** 修复 PRD-03: BaseRepository 方法列表对齐代码实际方法签名

**Step 3:** 修复 PRD-04: 模块列表移除 Module.Consultation + Module.Prescriptions

**Step 4:** 修复 PRD-07: "14 个标准方法" 修正为代码实际数量

**Step 5:** 修复 PRD-08: 移除 BaseReadRepository 引用 (该类不存在)

**Step 6:** 在 server.md 变更记录中追加修复条目

**验证:** Grep 确认 server.md 中不再包含 "BaseReadRepository"、"Module.Consultation"、"Module.Prescriptions"

---

## Phase 2: Proto-Persona 深化

**目标:** 创建 3 个角色的深度 Proto-Persona，替代 prd.md S3 中的基础角色描述
**时间:** 1-1.5 小时
**依赖:** Phase 0 完成
**PM Skill:** `proto-persona`

### Task 2.1: 收集 Persona 输入素材

**Files:**
- Read: `docs/01-product/04-user-roles.md` (权限矩阵)
- Read: `docs/01-product/06-clinical-workflow.md` (工作流，了解角色在各阶段的参与方式)
- Read: `docs/02-requirements/01-prd.md:43-70` (S3 现有角色描述)
- Read: `docs/02-requirements/07-medical-cases.md:1-60` (医案模块用户痛点，最真实的场景)
- Read: `docs/02-requirements/04-patients.md:1-30` (患者模块用户痛点)

**Step 1:** 读取上述文件，提取每个角色的:
- 具体操作权限 (来自 user-roles.md)
- 在核心流程中的参与节点 (来自 clinical-workflow.md)
- 痛点和证据 (来自各模块 Problem Statement)

### Task 2.2: 编写 Doctor Proto-Persona

**Files:**
- Create: `docs/01-product/02-personas.md`

**Step 1:** 创建文件，写入文档头部和 Doctor Persona

格式:
```markdown
# 用户画像 (Proto-Personas)

> **版本**: v1.0
> **创建日期**: 2026-03-06
> **数据来源**: 模块需求文档用户痛点 + 临床工作流分析

---

## 主要角色: 李医生 (Doctor)

**背景**: 52 岁，中医内科主治医师，从业 25 年。在凌隐宝堂坐诊，日均接诊 20 名患者。

**一天的工作**:
- 08:00 到诊所，打开系统查看今日预约
- 08:30-12:00 上午门诊 (约 12 名患者)
- 12:00-14:00 午休，偶尔整理验方
- 14:00-17:30 下午门诊 (约 8 名患者)
- 17:30 下班前检查未完成医案

**技术能力**: 能用 Windows 基本操作 (双击打开、输入、打印)，习惯用拼音输入法。不会安装软件，遇到弹窗会紧张。Excel 仅用于查看，不会编辑公式。

**Goals**:
1. 复诊时 10 秒内调出患者上次诊断和处方
2. 常用经验方一键导入，不用每次重新输入 8-15 味药
3. 处方费用自动计算，消除手工算错

**Frustrations**:
1. 纸质病历堆积如山，找一个患者的历史记录要翻 5-10 分钟
2. 手写处方字迹潦草，药房配药时经常打电话来确认
3. 外出看诊时断网，回来后手工抄写诊疗记录到系统

**Success Criteria**: "能让我把看病的时间还给病人，而不是花在找资料和算账上"

**Quote**: "我是看病的，不是做文书的。系统要帮我省事，不能比纸还麻烦。"
```

### Task 2.3: 编写 Admin + Receptionist Proto-Persona

**Files:**
- Modify: `docs/01-product/02-personas.md`

**Step 1:** 在 Doctor 之后追加 Admin Persona:

```markdown
## 次要角色: 王主任 (Admin)

**背景**: 45 岁，诊所负责人兼行政管理员。懂中医但主要负责运营管理。

**一天的工作**:
- 08:00 检查系统状态，处理前一天的异常
- 09:00 药材价格更新 (每月 1-2 次大批量，日常零星)
- 10:00-11:00 审核医案、处理数据问题
- 不定时 用户账号管理 (新人入职/离职处理)

**技术能力**: 会用 Excel 做简单表格，能导入导出文件。对系统管理有基本概念，但不会写 SQL。

**Goals**:
1. 药材库价格批量更新不影响历史处方
2. 一眼看出哪些医案有异常需要审核
3. 人员流动时 5 分钟内完成账号交接

**Frustrations**:
1. 药材涨价后不知道会不会影响已开的处方金额
2. 新诊所初始化要录入 400+ 种药材，逐条录入需要好几天
3. 离职员工账号忘记禁用，发现时已过去两周

**Success Criteria**: "药材数据准确、权限管控到位、出了问题能查到记录"

**Quote**: "我最怕的就是数据乱了理不清。"
```

**Step 2:** 追加 Receptionist Persona:

```markdown
## 次要角色: 小张 (Receptionist)

**背景**: 26 岁，前台接待，入职半年。负责患者登记和分诊。

**一天的工作**:
- 08:00 开机，准备读卡器
- 08:30-12:00 高峰期接待 (每 5-10 分钟一位患者)
- 14:00-17:00 下午相对轻松
- 间歇 补录患者信息、整理档案

**技术能力**: 日常使用微信、淘宝，对电脑操作熟练。打字速度快，习惯快捷键。

**Goals**:
1. 刷身份证 3 秒完成登记，不让患者等
2. 老患者一搜就出来，不用问"你上次来过吗"
3. 知道哪位医生在看几号患者，能给等候患者准确预估

**Frustrations**:
1. 高峰期手工填写登记表，排队的患者催得紧
2. 患者说"来过"但找不到登记记录，要重新填
3. 不确定医生的接诊进度，被患者追问"还要等多久"

**Success Criteria**: "患者从进门到坐下等号，不超过 2 分钟"

**Quote**: "前台最怕排长队，系统快一秒我就少被催一句。"
```

**Step 3:** 添加变更记录

### Task 2.4: 更新 prd.md S3 链接

**Files:**
- Modify: `docs/02-requirements/01-prd.md:43-70`

**Step 1:** 将 prd.md Section 3 中的内联 Persona 描述替换为简要版 + 链接:

```markdown
## 3. Target Users & Personas

### 3.1 角色定义

详细权限矩阵见 [user-roles.md](../01-product/04-user-roles.md)。

### 3.2 核心角色画像

详细 Proto-Persona (含日常时间线、痛点、成功标准) 见 [personas.md](../01-product/02-personas.md)。

| 角色 | 代表人物 | 使用频率 | 核心需求 |
|------|---------|---------|---------|
| 中医医生 (Doctor) | 李医生 | 每日 6-8h | 快速调阅患者历史、高效开方、积累验方 |
| 诊所管理员 (Admin) | 王主任 | 每日 1-2h | 药材库管理、数据审核、用户管理 |
| 前台接待 (Receptionist) | 小张 | 每日 4-6h | 快速登记、身份证读卡、挂号分诊 |
| 超级管理员 (SuperAdmin) | (系统角色) | 极低 | 系统初始化、诊断工具 |
```

---

## Phase 3: Jobs-to-Be-Done 分析

**目标:** 从用户任务视角梳理 JTBD，与 131 FR 交叉验证覆盖度
**时间:** 1.5-2 小时
**依赖:** Phase 2 完成 (Personas 提供 JTBD 上下文)
**PM Skill:** `jobs-to-be-done`

### Task 3.1: 收集 JTBD 输入素材

**Files:**
- Read: `docs/01-product/06-clinical-workflow.md` (全文，4 个流程)
- Read: `docs/01-product/02-personas.md` (刚创建的 Persona)
- Read: `docs/01-product/01-vision.md:15-22` (5 个业务目标)

**Step 1:** 从 clinical-workflow.md 的 4 个流程中提取核心 "situation -> motivation -> outcome" 组合

### Task 3.2: 编写 JTBD 文档

**Files:**
- Create: `docs/01-product/03-jtbd.md`

**Step 1:** 创建文件，编写结构化 JTBD。组织方式: 按角色分组，每组 3-5 个 JTBD。

```markdown
# Jobs-to-Be-Done 分析

> **版本**: v1.0
> **创建日期**: 2026-03-06
> **框架**: Clayton Christensen JTBD Theory
> **数据来源**: clinical-workflow.md + 模块需求文档 Problem Statement

---

## JTBD 格式

> When [situation], I want to [motivation], so I can [expected outcome].

---

## Doctor JTBD

### JTBD-D01: 复诊患者识别
**When** 一位老患者走进诊室说"我上次来过"，
**I want to** 通过姓名或拼音码在 5 秒内找到这位患者的完整档案和历史医案，
**So I can** 快速回顾上次诊断和处方，延续治疗方案而不遗漏关键信息。

**对应 FR**: FR-PAT-002 (搜索), FR-MC-002 (历史医案列表)
**对应流程**: clinical-workflow.md 流程 2 (复诊)

### JTBD-D02: 高效开方
**When** 我完成四诊合参并确定治法后，
**I want to** 从我的常用验方中一键导入药材组成，然后根据本次情况微调剂量，
**So I can** 在 3 分钟内完成一张完整处方，而不是手动逐味输入 8-15 味药材。

**对应 FR**: FR-MC-016 (验方导入), FR-MC-014 (药材添加), FR-MC-015 (剂量设置)
**对应流程**: clinical-workflow.md 流程 3 (验方使用)

### JTBD-D03: 复诊处方延续
**When** 复诊患者病情稳定需要继续上次的方子时，
**I want to** 一键复制上次处方到新医案，只修改个别药材和剂量，
**So I can** 避免从零开始重新开方，节省时间且减少遗漏。

**对应 FR**: FR-MC-018 (复制历史处方)
**对应流程**: clinical-workflow.md 流程 2 步骤 5

### JTBD-D04: 离线诊疗
**When** 我外出看诊或诊所网络故障时，
**I want to** 在本地模式下完整执行从患者查找到处方打印的全部流程，
**So I can** 不因技术问题中断诊疗，回到诊所后再同步数据。

**对应 FR**: FR-SYNC-001~008 (数据同步), 双模式架构
**对应流程**: vision.md 业务目标 5

### JTBD-D05: 经验方积累
**When** 我在临床中反复使用某个自创方剂并验证疗效后，
**I want to** 将它保存为验方模板并设置共享给团队，
**So I can** 建立个人的验方库，让好方子在诊所内传承。

**对应 FR**: FR-FORM-001 (创建验方), FR-FORM-010 (共享管理)
**对应流程**: clinical-workflow.md 流程 3 (验方创建)

---

## Admin JTBD

### JTBD-A01: 药材库初始化
**When** 诊所刚开业或系统上线需要录入完整药材库 (300-500 种) 时，
**I want to** 通过 Excel 文件一次性批量导入，系统自动检测重复并生成拼音码，
**So I can** 在 1 小时内完成药材库初始化，而不是花数天逐条录入。

**对应 FR**: FR-HERB-010 (批量导入)
**对应流程**: vision.md 流程 4 (药材管理)

### JTBD-A02: 药材价格更新
**When** 供应商调价后需要批量更新药材价格时，
**I want to** 更新价格而确保历史处方金额不受影响 (价格快照)，
**So I can** 安心改价，不用担心已有处方的结算数据被篡改。

**对应 FR**: FR-HERB-005 (编辑药材), FR-MC-003 (价格快照机制)
**对应流程**: vision.md 跨模块数据规则 "药材价格快照"

### JTBD-A03: 用户生命周期管理
**When** 有员工入职或离职时，
**I want to** 5 分钟内创建或禁用其系统账号，并确保权限立即生效，
**So I can** 消除权限漏洞，离职人员无法继续访问敏感数据。

**对应 FR**: FR-USER-001 (创建用户), FR-USER-006 (禁用用户)

---

## Receptionist JTBD

### JTBD-R01: 快速登记
**When** 患者到达前台递出身份证时，
**I want to** 刷卡后 3 秒内自动填充姓名、性别、出生日期、身份证号、住址，
**So I can** 在高峰期让每位患者的登记时间不超过 30 秒，减少排队等候。

**对应 FR**: FR-CARD-001 (读取身份证), FR-CARD-002 (患者匹配)
**对应流程**: clinical-workflow.md 阶段 1a

### JTBD-R02: 老患者识别
**When** 患者说"我以前来过"但我不确定时，
**I want to** 通过姓名或手机号模糊搜索快速找到匹配记录，
**So I can** 避免重复建档，直接在原有档案上继续。

**对应 FR**: FR-PAT-002 (搜索), FR-PAT-003 (查看详情)

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-03-06 | v1.0 | 初始版本: 3 角色 10 个 JTBD |
```

### Task 3.3: JTBD vs FR 覆盖度交叉审查

**Files:**
- Read: `docs/01-product/03-jtbd.md` (刚创建)
- Read: `docs/02-requirements/README.md` (131 FR 索引)

**Step 1:** 检查正向覆盖 -- 每个 JTBD 是否有 FR 支撑:
- 逐条检查 JTBD 中标注的 "对应 FR" 是否真实存在于模块文件中
- 记录缺失项

**Step 2:** 检查反向覆盖 -- 是否有 FR 无法对应到任何 JTBD:
- 扫描 14 个模块中未被 JTBD 引用的 FR
- 分析: 未引用的 FR 是否属于基础设施 (正常) 还是需求盲区 (异常)

**Step 3:** 将审查结果追加到 jtbd.md 末尾:
```markdown
## 覆盖度审查结果

### 正向覆盖 (JTBD -> FR)
[每个 JTBD 的 FR 映射验证结果]

### 反向覆盖 (FR -> JTBD)
[未被 JTBD 覆盖的 FR 列表及分析]

### 需求盲区
[如果发现 JTBD 无对应 FR 的情况，记录在此]
```

---

## Phase 4: User Story Mapping

**目标:** 建立 Jeff Patton 式故事地图，串联 Activities -> Steps -> Tasks，每个 Task 映射到 FR
**时间:** 2-3 小时
**依赖:** Phase 2 + 3 完成
**PM Skill:** `user-story-mapping`

### Task 4.1: 定义 Narrative 和 Activities 骨架

**Files:**
- Read: `docs/01-product/06-clinical-workflow.md` (4 个核心流程)
- Read: `docs/01-product/03-jtbd.md` (10 个 JTBD)
- Create: `docs/02-requirements/19-user-story-map.md`

**Step 1:** 创建文件，定义 3 个核心 Narrative (对应 clinical-workflow.md 的 3 个主要流程):

```markdown
# User Story Map

> **版本**: v1.0
> **创建日期**: 2026-03-06
> **框架**: Jeff Patton User Story Mapping
> **数据来源**: clinical-workflow.md + JTBD + 131 FR

---

## Narrative 1: 首诊完整流程

**Persona**: 李医生 (Doctor)
**Goal**: 完成一位新患者的首次就诊，从登记到处方打印

### Activities (Backbone)

[患者登记] -> [创建医案] -> [四诊合参] -> [处方开具] -> [保存与打印]

---

## Narrative 2: 复诊流程

**Persona**: 李医生 (Doctor)
**Goal**: 高效完成复诊，复用历史处方并微调

### Activities (Backbone)

[患者识别] -> [历史回顾] -> [本次诊断] -> [处方延续] -> [保存与打印]

---

## Narrative 3: 药材与验方管理

**Persona**: 王主任 (Admin) + 李医生 (Doctor)
**Goal**: 维护药材库和积累经验方

### Activities (Backbone)

[药材维护] -> [验方创建] -> [验方验证] -> [验方使用]
```

### Task 4.2: 分解 Narrative 1 的 Steps 和 Tasks

**Files:**
- Modify: `docs/02-requirements/19-user-story-map.md`
- Read: `docs/02-requirements/04-patients.md` (FR-PAT 列表)
- Read: `docs/02-requirements/07-medical-cases.md` (FR-MC 列表)
- Read: `docs/02-requirements/05-herbs.md` (FR-HERB 列表)
- Read: `docs/02-requirements/09-printing.md` (FR-PRINT 列表)

**Step 1:** 在 Narrative 1 下逐 Activity 分解:

```markdown
### Activity 1: 患者登记

**Steps:**
1. 搜索已有患者 (FR-PAT-002)
2. 刷身份证读卡 (FR-CARD-001)
3. 创建新患者 (FR-PAT-001)
4. 患者自动匹配 (FR-CARD-002)

**Tasks (纵向优先级):**

| Must Have | Should Have | Could Have |
|-----------|------------|------------|
| 姓名/拼音码搜索 (FR-PAT-002) | 身份证读卡自动填充 (FR-CARD-001) | 批量导入患者 (FR-PAT-010) |
| 创建患者基本信息 (FR-PAT-001) | 已有患者匹配 (FR-CARD-002) | 导出患者数据 (FR-PAT-011) |
| 查看患者详情 (FR-PAT-003) | 患者状态管理 (FR-PAT-013) | |

### Activity 2: 创建医案
[同样格式分解]

### Activity 3: 四诊合参
[同样格式分解]

### Activity 4: 处方开具
[同样格式分解]

### Activity 5: 保存与打印
[同样格式分解]
```

### Task 4.3: 分解 Narrative 2 和 3

**Files:**
- Modify: `docs/02-requirements/19-user-story-map.md`
- Read: `docs/02-requirements/06-formulas.md` (FR-FORM 列表)
- Read: `docs/02-requirements/10-sync.md` (FR-SYNC 列表)

**Step 1:** 按 Task 4.2 相同格式分解 Narrative 2 (复诊流程)

**Step 2:** 按相同格式分解 Narrative 3 (药材与验方管理)

### Task 4.4: 绘制纵向优先级切片线

**Files:**
- Modify: `docs/02-requirements/19-user-story-map.md`

**Step 1:** 在文档末尾添加汇总视图:

```markdown
## Release Slices

### Release 1 (v1.0 Must Have)
[列出所有 Narrative 中 Must Have 行的 FR 编号，汇总计数]

### Release 2 (v1.0 Should Have)
[列出 Should Have 行的 FR 编号]

### Future (Could Have / v2.0)
[列出 Could Have 和 v2.0 延期项]
```

### Task 4.5: Gap 分析

**Files:**
- Modify: `docs/02-requirements/19-user-story-map.md`

**Step 1:** 对比故事地图中的所有 Task 与 131 FR:
- 标记未出现在故事地图中的 FR (基础设施类 FR 不计入)
- 标记故事地图中无 FR 对应的 Task (需求盲区)

**Step 2:** 追加 Gap 分析章节:

```markdown
## Gap 分析

### 未映射到故事地图的 FR
[列出 FR 编号和原因 (如: 基础设施 / 跨模块 / 需补充)]

### 故事地图中无 FR 支撑的 Task
[如果有需求盲区，记录在此]
```

---

## Phase 5: 131 FR MoSCoW 优先级排序

**目标:** 为每个 FR 标注 MoSCoW 优先级
**时间:** 1.5-2 小时
**依赖:** Phase 4 完成 (故事地图的纵向切片提供初始排序依据)
**PM Skill:** `prioritization-advisor`

### Task 5.1: 定义 MoSCoW 排序标准

**Files:**
- Read: `docs/02-requirements/19-user-story-map.md` (Release Slices 章节)

**Step 1:** 确认排序标准 (需用户确认):

```markdown
MoSCoW 标准定义:

**Must Have** (系统无此功能则不可用):
- 核心诊疗流程 (患者查找 -> 创建医案 -> 诊断 -> 处方 -> 保存)
- 基础 CRUD (创建/查看/编辑/删除)
- 认证登录
- 基本搜索

**Should Have** (显著提升效率但非阻断):
- 批量导入/导出
- 高级搜索/过滤
- 验方共享
- 数据同步
- 打印功能

**Could Have** (锦上添花):
- 系统健康诊断
- 高级审计日志
- 配置参数管理
- 身份证读卡器

**Won't Have (this time)** (明确延期到 v2.0):
- prd.md S8 Out of Scope 中已声明的功能
```

### Task 5.2: 逐模块标注优先级

**Files:**
- Modify: `docs/02-requirements/02-auth.md` (FR-AUTH-001~013 添加 Priority 列)
- Modify: `docs/02-requirements/03-users.md` (FR-USER-001~012)
- Modify: `docs/02-requirements/04-patients.md` (FR-PAT-001~013)
- Modify: `docs/02-requirements/05-herbs.md` (FR-HERB-001~013)
- Modify: `docs/02-requirements/06-formulas.md` (FR-FORM-001~013)
- Modify: `docs/02-requirements/07-medical-cases.md` (FR-MC-001~018)
- Modify: `docs/02-requirements/10-sync.md` (FR-SYNC-001~008)
- Modify: `docs/02-requirements/09-printing.md` (FR-PRINT-001~004)
- Modify: `docs/02-requirements/16-card-reader.md` (FR-CARD-001~002)
- Modify: `docs/02-requirements/15-health-diagnostics.md` (FR-SYS-001~009)
- Modify: `docs/02-requirements/13-error-handling.md` (FR-ERR-001~008)
- Modify: `docs/02-requirements/14-logging.md` (FR-LOG-001~007)
- Modify: `docs/02-requirements/12-desktop-shell.md` (FR-SHELL-001~007)
- Modify: `docs/02-requirements/11-configuration.md` (FR-CFG-001~004)

**Step 1:** 在每个模块的功能清单表格中新增 `Priority` 列

**Step 2:** 逐条标注 M/S/C/W，参考 Phase 4 故事地图的 Release Slices

**Step 3:** 需要用户确认的边界案例单独标注 `TBD`

**注意:** 修改 FR 表格时，仅新增列，不修改现有内容

### Task 5.3: 交叉验证

**Files:**
- Read: `docs/02-requirements/19-user-story-map.md` (Release Slices)
- Read: 14 个模块文件 (刚标注的 Priority)

**Step 1:** 对比故事地图 Must Have 切片与 FR Priority=Must 的一致性
**Step 2:** 标记不一致项，调整或说明原因

### Task 5.4: 更新 prd.md 索引统计

**Files:**
- Modify: `docs/02-requirements/01-prd.md:167-187` (S7 Requirements Index)

**Step 1:** 在 S7 索引表中增加优先级统计列:

```markdown
| 模块 | 文件 | FR 编号范围 | 功能数 | Must | Should | Could |
|------|------|------------|--------|------|--------|-------|
| 认证与会话管理 | [auth.md](../../02-requirements/02-auth.md) | FR-AUTH-001~013 | 13 | X | Y | Z |
| ... | ... | ... | ... | ... | ... | ... |
```

**Step 2:** 在 S7 末尾添加 User Story Map 链接:

```markdown
### 7.5 用户故事地图

- [user-story-map.md](../../02-requirements/19-user-story-map.md) -- 3 个核心 Narrative 的 Jeff Patton 故事地图
```

---

## Phase 6: 顶层 PRD 深化

**目标:** 反向充实 prd.md 各章节，融合 Phase 2-5 产出
**时间:** 1-1.5 小时
**依赖:** Phase 2-5 全部完成
**PM Skill:** `prd-development`

### Task 6.1: 精修 Executive Summary

**Files:**
- Modify: `docs/02-requirements/01-prd.md:9-13` (S1)
- Read: `docs/01-product/03-jtbd.md` (核心 JTBD)

**Step 1:** 将 S1 从"我们在做什么"升级为"我们为谁解决什么":

现有: "凌隐宝堂中医诊所管理系统 (LYBTZYZS) 是一套专为小型中医诊所设计的..."
升级: "凌隐宝堂中医诊所管理系统解决中医医生在复诊患者信息调阅 (5-10 min -> 10s)、处方开具 (手写 -> 验方一键导入)、经验方传承 (纸质 -> 数字化) 三大核心痛点。..."

### Task 6.2: 深化 Problem Statement

**Files:**
- Modify: `docs/02-requirements/01-prd.md:17-40` (S2)

**Step 1:** 在 S2 中添加来自各模块汇总的量化证据:

```markdown
### 2.3 量化证据汇总

| 来源模块 | 证据 | 量化数据 |
|----------|------|---------|
| 医案管理 | 复诊翻阅历史 | 每次 5-10 分钟，日均 6-12 次 |
| 医案管理 | 处方手动计算错误率 | ~5% |
| 患者管理 | 纸质病历查找时间 | 每次 1-3 分钟 |
| 药材管理 | 初始化药材库耗时 | 手工逐条录入需数天 |
| 验方管理 | 经验方依赖个人记忆 | 人员离职即丢失 |
```

### Task 6.3: 精修 Success Metrics

**Files:**
- Modify: `docs/02-requirements/01-prd.md:134-164` (S6)

**Step 1:** 参照 prd-development Phase 6 格式，区分 Primary / Secondary / Guardrail:

```markdown
### 6.1 Primary Metric (主要优化指标)
**复诊效率**: 复诊患者从到达到处方打印的完整流程时间
- 当前 (纸质): 20-30 分钟
- 目标 (v1.0): < 10 分钟

### 6.2 Secondary Metrics
[保留现有表格，标记为 Secondary]

### 6.3 Guardrail Metrics (护栏指标)
| 指标 | 底线 | 说明 |
|------|------|------|
| 数据完整性 | 0 数据丢失 | 聚合保存事务性保证 |
| 系统可用性 | 离线模式可用 | 本地模式核心流程不依赖网络 |
```

### Task 6.4: 添加 JTBD 引用

**Files:**
- Modify: `docs/02-requirements/01-prd.md:43-70` (S3 区域)

**Step 1:** 在 S3 末尾添加 JTBD 引用:

```markdown
### 3.3 Jobs-to-Be-Done

核心用户任务分析见 [jtbd.md](../01-product/03-jtbd.md)。3 个角色共 10 个 JTBD，覆盖诊疗、管理、前台三大场景。

**Top 3 JTBD:**
1. **JTBD-D01**: 复诊时 5 秒内调出患者历史 (对应 FR-PAT-002, FR-MC-002)
2. **JTBD-D02**: 验方一键导入到处方 (对应 FR-MC-016)
3. **JTBD-D03**: 复制历史处方微调 (对应 FR-MC-018)
```

### Task 6.5: 更新 Open Questions

**Files:**
- Modify: `docs/02-requirements/01-prd.md:260-269` (S10)

**Step 1:** 更新每个 Open Question 的状态:

```markdown
| ID | 问题 | 状态 | 决策时间 |
|----|------|------|---------|
| OQ-01 | SYNC-D02 实施时机 | CLOSED: Sprint 4 | 2026-02-28 |
| OQ-02 | 本地模式数据库选择 | CLOSED: LocalDB | 2026-03-05 |
| ... | ... | ... | ... |
```

### Task 6.6: 更新 prd.md 相关文档表

**Files:**
- Modify: `docs/02-requirements/01-prd.md:273-283`

**Step 1:** 在相关文档表中添加新建的文件:

```markdown
| 用户画像 | [personas.md](../01-product/02-personas.md) | Proto-Persona (3 角色) |
| JTBD 分析 | [jtbd.md](../01-product/03-jtbd.md) | Jobs-to-Be-Done (10 个 JTBD) |
| 用户故事地图 | [user-story-map.md](../../02-requirements/19-user-story-map.md) | Jeff Patton 故事地图 (3 Narrative) |
```

### Task 6.7: 更新变更记录

**Files:**
- Modify: `docs/02-requirements/01-prd.md` (末尾变更记录)

**Step 1:** 追加变更条目:

```markdown
| 2026-03-XX | v1.1 | PRD 深化: 新增 Personas/JTBD/Story Map 链接; 精修 S1/S2/S6; 131 FR MoSCoW 排序; Open Questions 状态更新 |
```

---

## 执行检查清单

每个 Phase 完成后验证:

- [ ] Phase 0: server.md 无 "BaseReadRepository" / "Module.Consultation" / "Module.Prescriptions"
- [ ] Phase 2: personas.md 存在，包含 3 个角色，prd.md S3 已链接
- [ ] Phase 3: jtbd.md 存在，10 个 JTBD 均有 FR 映射，覆盖度审查完成
- [ ] Phase 4: user-story-map.md 存在，3 个 Narrative，Release Slices 完成，Gap 分析完成
- [ ] Phase 5: 14 个模块文件均有 Priority 列，prd.md S7 有统计
- [ ] Phase 6: prd.md S1/S2/S3/S6/S7/S10 已更新

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-03-06 | v1.0 | 初始实施计划 |
