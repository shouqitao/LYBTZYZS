# Roadmap Planning 实施计划

> **For Claude:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task.

**Goal:** 基于 MoSCoW 优先级排序 (45 Must / 53 Should / 33 Could) 和代码实现现状，制定 v1.0 Sprint/Release 路线图。

**Architecture:** 文档驱动。通过 Code-PRD 对齐审计确定各 US 实现状态，然后基于依赖关系和优先级分配到 Sprint。产出物为 `docs/02-requirements/20-roadmap.md`。

**Tech Stack:** Markdown 文档，PM Skills 框架 (`roadmap-planning` skill)

**前置完成状态 (2026-03-06):**
- PRD 8 阶段全部完成 (prd.md v1.2)
- 131 US 已标注 MoSCoW 优先级 (14 模块文件)
- User Story Map 已完成 (user-story-map.md，4 Narrative + Release Slices)
- 代码已基本实现 (9 API Controller + 30 ViewModel)
- 最近一次全量审计: 2026-02-28 (28 OPEN 项)

---

## Phase 1: US 实现状态审计

**目标:** 确定 131 US 中哪些已实现、部分实现、未实现
**时间:** 1-2 小时
**依赖:** 无

### Task 1.1: 审计 Must Have US 实现状态 (45 US)

**Files:**
- Read: `docs/plans/archive/2026-02-28-code-vs-prd-full-audit-report.md` (最近审计报告)
- Read: 14 模块文件的"优先级汇总"表
- Read: 对应 Controller / ViewModel 源代码

**Step 1:** 读取 2026-02-28 审计报告，提取每个 US 的实现状态 (Implemented / Partial / Not Implemented)

**Step 2:** 对照代码验证关键模块:
- `src/Server/Services/LYBT.WebAPI/Controllers/` -- 每个 Controller 的 Action 方法 vs US 映射
- `src/Client/Desktop/Modules/` -- 每个 ViewModel 的功能 vs US 映射

**Step 3:** 创建审计矩阵 (追加到 findings.md):

```markdown
| US 编号 | Priority | 实现状态 | 备注 |
|---------|----------|---------|------|
| US-MC-001 | Must | Implemented | CreateAsync in MedicalCaseController |
| US-MC-002 | Must | Implemented | UpdateConsultation in Controller |
| ... | ... | ... | ... |
```

**验证:** Must Have 45 US 全部有明确的实现状态标注

### Task 1.2: 审计 Should Have US 实现状态 (53 US)

**Files:** 同 Task 1.1

**Step 1:** 对 53 个 Should Have US 执行同样的代码对照审计

**Step 2:** 特别关注以下高风险模块:
- SYNC (US-SYNC-001~007): 同步功能完整性
- PRINT (US-PRINT-001~003): 打印功能实现
- MC (US-MC-010/011/014~018): 医案高级功能

**验证:** Should Have 53 US 全部有明确状态标注

### Task 1.3: 审计 Could Have US 实现状态 (33 US)

**Step 1:** 快速扫描 33 个 Could Have US

**Step 2:** 标记已实现的 Could Have (可能有些已顺带实现)

**验证:** 131 US 审计矩阵完整

---

## Phase 2: 依赖关系分析

**目标:** 识别 US 之间的技术依赖，确定实施顺序约束
**时间:** 30-45 分钟
**依赖:** Phase 1 完成

### Task 2.1: 绘制模块级依赖图

**Files:**
- Read: `docs/01-product/06-clinical-workflow.md` Section 四 (跨模块交互矩阵)
- Read: `docs/02-requirements/19-user-story-map.md` (Narrative 顺序)

**Step 1:** 从 clinical-workflow.md 提取模块依赖链:

```
Auth -> Users -> Patients -> MedicalCase -> Printing
                Herbs -> Formulas -> MedicalCase
                CardReader -> Patients
                Config (独立)
                Sync (依赖 Herbs/Patients/Formulas)
                Shell (承载层)
                ERR/LOG/SYS (基础设施)
```

**Step 2:** 标注依赖类型:
- **阻塞依赖**: A 未完成则 B 无法开始 (如 Auth -> 所有其他模块)
- **增强依赖**: A 完成可增强 B 但非阻塞 (如 CardReader -> Patients)

### Task 2.2: 识别未实现 US 的依赖约束

**Step 1:** 对 Phase 1 中标记为 "Not Implemented" 或 "Partial" 的 US，检查其依赖是否已满足

**Step 2:** 标记依赖未满足的 US (需要先实现前置 US)

**验证:** 依赖图完整，无循环依赖

---

## Phase 3: Sprint 规划

**目标:** 将未完成的 US 分配到 Sprint，已完成的标记为 Done
**时间:** 1-1.5 小时
**依赖:** Phase 1 + 2 完成

### Task 3.1: 定义 Sprint 规划框架

**Files:**
- Create: `docs/02-requirements/20-roadmap.md`

**Step 1:** 创建文件，定义路线图框架:

```markdown
# v1.0 Release Roadmap

> **版本**: v1.0
> **创建日期**: 2026-03-0X
> **基于**: MoSCoW 优先级排序 (user-story-map.md) + Code-PRD 审计

---

## Sprint 规划原则

| 原则 | 说明 |
|------|------|
| Sprint 周期 | 2 周 |
| Must Have 优先 | Must Have US 排入最早可用 Sprint |
| 依赖顺序 | 按模块依赖链顺序排列 |
| 已完成标记 | 审计确认已实现的 US 标记为 Done |
| 容量估算 | 每 Sprint 预估 8-12 US (视复杂度) |

## Release 定义

| Release | 范围 | 标准 |
|---------|------|------|
| v1.0-alpha | 全部 Must Have (45 US) 实现并通过测试 | MVP 可用 |
| v1.0-beta | Must + Should (98 US) 实现 | 功能完整 |
| v1.0-rc | Must + Should + 部分 Could | 生产就绪 |
```

### Task 3.2: 分配 Must Have US 到 Sprint

**Step 1:** 将 45 Must Have US 按依赖链和实现状态分配:

- **Done (已完成):** 直接标记，不占 Sprint 容量
- **Sprint N:** 按依赖顺序分配未完成 US

**Step 2:** 按以下模块顺序分配 (基于依赖链):

```
Sprint 1 (基础层): Auth + Shell + Config
Sprint 2 (数据层): Users + Patients + Herbs
Sprint 3 (业务核心): MedicalCase (CRUD + 诊断 + 处方)
Sprint 4 (业务核心): MedicalCase (聚合保存 + 状态机 + 权限) + Formulas
Sprint 5 (收尾): 剩余 Must Have + 集成测试
```

**注意:** 如果审计发现大部分 Must Have 已实现，Sprint 数量会大幅减少

### Task 3.3: 分配 Should Have US 到 Sprint

**Step 1:** 将 53 Should Have US 按模块分组后顺序分配:

```
Sprint 6: Printing + CardReader + Sync (模式切换外的同步功能)
Sprint 7: MedicalCase 高级功能 (搜索/编辑模式/锁定/验方导入/历史复制)
Sprint 8: ERR + LOG + 用户高级功能
Sprint 9: 剩余 Should Have + 回归测试
```

### Task 3.4: Could Have 放入 Backlog

**Step 1:** 33 个 Could Have US 放入 Backlog，按模块分组但不分配 Sprint

**Step 2:** 标注哪些 Could Have 如果顺便实现了可以提前关闭

### Task 3.5: 生成路线图甘特视图

**Step 1:** 在 roadmap.md 末尾添加时间线视图:

```markdown
## 时间线视图

| Sprint | 周期 | 重点模块 | Must | Should | 目标 |
|--------|------|---------|------|--------|------|
| Done | - | 已实现 | XX | XX | 审计确认 |
| Sprint N | W1-W2 | [模块] | X | 0 | [目标] |
| Sprint N+1 | W3-W4 | [模块] | X | X | [目标] |
| ... | ... | ... | ... | ... | ... |
```

**验证:**
- 45 Must Have US 全部分配 (Done 或 Sprint)
- 53 Should Have US 全部分配 (Done 或 Sprint)
- 33 Could Have US 在 Backlog
- 依赖顺序无冲突

---

## Phase 4: 里程碑与验收标准

**目标:** 定义每个 Release 的验收标准和交付物
**时间:** 30 分钟
**依赖:** Phase 3 完成

### Task 4.1: 定义 Release 验收标准

**Files:**
- Modify: `docs/02-requirements/20-roadmap.md`

**Step 1:** 为每个 Release 定义 Exit Criteria:

```markdown
## Release 验收标准

### v1.0-alpha Exit Criteria
- [ ] 45 Must Have US 全部通过验收测试
- [ ] 核心流程端到端可用 (患者登记 -> 创建医案 -> 诊断 -> 处方 -> 保存 -> 完成)
- [ ] 编译零错误，零 Warning (关键)
- [ ] Server + Desktop + Architecture 测试全通过

### v1.0-beta Exit Criteria
- [ ] 98 US (Must + Should) 全部通过验收测试
- [ ] 打印功能可用
- [ ] 数据同步功能可用
- [ ] 身份证读卡器集成可用
- [ ] 性能指标满足 nfr.md 要求

### v1.0-rc Exit Criteria
- [ ] 所有 CRITICAL/HIGH 技术债务清零
- [ ] Code-PRD 审计 OPEN 项清零
- [ ] 用户验收测试 (UAT) 通过
```

### Task 4.2: 更新 prd.md 链接

**Files:**
- Modify: `docs/02-requirements/01-prd.md`

**Step 1:** 在 S7 Requirements Index 中添加 roadmap 链接:

```markdown
### 7.6 发布路线图

- [roadmap.md](../../02-requirements/20-roadmap.md) -- v1.0 Sprint 分配 + Release 验收标准
```

**Step 2:** 更新 prd.md 变更记录

**验证:** prd.md 中可通过链接访问 roadmap.md

---

## Phase 5: 同步 Planning-with-files

**目标:** 更新三文件反映路线图规划完成状态
**时间:** 10 分钟
**依赖:** Phase 4 完成

### Task 5.1: 更新三文件

**Files:**
- Modify: `task_plan.md`
- Modify: `findings.md`
- Modify: `progress.md`

**Step 1:** task_plan.md 覆盖为路线图规划任务内容

**Step 2:** findings.md 记录审计发现 (US 实现状态矩阵)

**Step 3:** progress.md 记录执行日志

---

## 执行检查清单

每个 Phase 完成后验证:

- [ ] Phase 1: 131 US 全部有实现状态标注 (Implemented / Partial / Not Implemented)
- [ ] Phase 2: 模块依赖图完整，未实现 US 的前置依赖已识别
- [ ] Phase 3: roadmap.md 存在，所有 US 分配到 Done / Sprint / Backlog
- [ ] Phase 4: 3 个 Release 有明确 Exit Criteria
- [ ] Phase 5: prd.md 已链接 roadmap.md

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-03-06 | v1.0 | 初始实施计划 |
