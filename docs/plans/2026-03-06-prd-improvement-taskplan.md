# PRD 完善任务清单

> **创建日期**: 2026-03-06
> **范围**: 基于 PM Skills 框架 + 项目现状分析，设计 PRD 文档完善路线
> **方法论**: prd-development (8 Phase) + user-story-mapping (Jeff Patton) + prioritization-advisor

---

## 现状评估

### 已有资产 (高质量，保留复用)

| 资产 | 文件 | 质量评级 |
|------|------|---------|
| 顶层 PRD 框架 | `prd.md` (10 章节, 290 行) | B+ (框架完整，部分章节可深化) |
| 模块级 FR | 14 个模块文件, 131 FR | A (结构规范，双模式标注完整) |
| 产品愿景 | `vision.md` (详细流程图 + 模块依赖 + 路线图) | A |
| 临床工作流 | `clinical-workflow.md` (端到端时序图, 异常路径) | A |
| 用户角色 | `user-roles.md` (权限矩阵) | B+ |
| 术语表 | `glossary.md` | A (GLOSSARY-01 已修复) |
| NFR + UI 规范 | `nfr.md` + `ui-patterns.md` | B+ |

### 缺失维度 (按 PM Skills 框架对标)

| PRD 标准维度 | 当前状态 | 差距 |
|-------------|---------|------|
| Executive Summary | 已有 (prd.md S1) | 微调 |
| Problem Statement + Evidence | **全部 16 模块均已完成** (2026-03-06 确认) | 已完成 |
| Target Users + Personas | 基础角色描述 (prd.md S3) | 缺深度画像 |
| JTBD (Jobs-to-Be-Done) | 无 | 可从 clinical-workflow 推导 |
| User Story Mapping | 无 (131 FR 是规格，非用户故事) | 核心缺失 |
| 优先级排序 | 无 (131 FR 无 MoSCoW/RICE 标注) | 核心缺失 |
| Success Metrics | 已有 (prd.md S6) | 微调 |
| Out of Scope | 已有 (prd.md S8) | 完整 |
| Dependencies & Risks | 已有 (prd.md S9) | 完整 |
| 审计偏差 | 17 项: 12 项已修 (模块文件+glossary), 5 项待修 (server.md PRD-02/03/04/07/08) | 仅 server.md 待修 |

---

## 任务分阶段设计

### Phase 0: 审计偏差修复 (前置，必做)

**目标**: PRD 与代码对齐，建立文档可信度
**时间**: 1-2 小时
**依赖**: 2026-03-06-prd-reorganization-design.md 修复清单

| Task | 优先级 | 涉及文件 | 说明 |
|------|--------|---------|------|
| P0-1: 修复 HIGH 偏差 (PRD-01~04) | CRITICAL | data-model.md, server.md | Patient 缺字段 / BaseEntity 不完整 / 已删除模块 |
| P0-2: 修复 MEDIUM 偏差 (PRD-05~12) | HIGH | data-model.md, sync.md, medical-cases.md, herbs.md, formulas.md, patients.md | 字段位置 / 术语 / HTTP Method / 状态码 |
| P0-3: 修复 LOW 偏差 (PRD-13~17) | MEDIUM | card-reader.md, desktop-shell.md | 配置方式 / 字段名 / 步骤数 |
| P0-4: 修复术语 (GLOSSARY-01) | HIGH | glossary.md | Draft=0 -> Suspended=0 |

**验收标准**: 全部 17 项偏差标记 FIXED，glossary 与代码一致

---

### ~~Phase 1: 模块级 Problem Statement 补齐~~ (已完成)

> **2026-03-06 调研确认**: 全部 16 个模块文件均已有完整的 Problem Statement (1.1 问题描述 + 1.2 用户痛点 + 1.3 证据)。包括基础设施模块 (health-diagnostics / error-handling / logging / desktop-shell / configuration / users / card-reader) 也已补齐。此 Phase 跳过。

---

### Phase 2: Proto-Persona 深化

**目标**: 将 prd.md S3 的基础角色扩展为可操作的 Proto-Persona
**时间**: 1-1.5 小时
**PM Skill**: `proto-persona`
**输出文件**: `docs/01-product/personas.md` (新建)

| Task | 角色 | 补充维度 |
|------|------|---------|
| P2-1: 医生 (Doctor) Proto-Persona | 主要角色 | 日常时间线、技术采纳曲线、关键挫败时刻、核心 JTBD |
| P2-2: 管理员 (Admin) Proto-Persona | 次要角色 | 管理痛点、药材维护频率、数据审核场景 |
| P2-3: 前台 (Receptionist) Proto-Persona | 次要角色 | 高峰时段工作流、患者沟通场景 |

**格式参考 (每个 Persona)**:
```markdown
### [角色名]: [人名]

**背景**: [年龄/从业年限/诊所规模]
**一天的工作**: [典型时间线]
**技术能力**: [具体描述，不是"基本/中等/高"]
**Goals**: [3 个核心目标]
**Frustrations**: [3 个主要挫败点]
**Success Criteria**: [用户认为什么情况下产品"成功了"]
**Quote**: ["一句话代表这个人的心声"]
```

---

### Phase 3: Jobs-to-Be-Done 分析

**目标**: 从用户任务视角审视功能覆盖度，发现盲区
**时间**: 1.5-2 小时
**PM Skill**: `jobs-to-be-done`
**数据来源**: clinical-workflow.md (已有详细流程) + vision.md (已有依赖矩阵)
**输出**: 追加到 `docs/01-product/personas.md` 或独立 `docs/01-product/jtbd.md`

| Task | JTBD 主体 | 说明 |
|------|----------|------|
| P3-1: 医生核心 JTBD | Doctor | "当我接诊复诊患者时，我想快速回顾上次诊断和处方，以便..." |
| P3-2: 医生处方 JTBD | Doctor | "当我开具处方时，我想从常用验方中快速导入药材，以便..." |
| P3-3: 管理员药材 JTBD | Admin | "当我需要更新药材价格时，我想批量导入新价格表，以便..." |
| P3-4: 前台挂号 JTBD | Receptionist | "当患者到达时，我想通过身份证快速登记，以便..." |
| P3-5: 医生离线 JTBD | Doctor | "当诊所网络故障时，我想继续完整诊疗流程，以便..." |

**JTBD 格式**:
```
When [situation], I want to [motivation], so I can [expected outcome].
```

**覆盖度审查**: 完成 JTBD 后，逐条与 131 FR 交叉检查:
- 每个 JTBD 是否有对应 FR 支撑?
- 是否存在 FR 无法对应到任何 JTBD? (可能是过度设计)
- 是否存在 JTBD 无 FR 支撑? (需求盲区)

---

### Phase 4: User Story Mapping

**目标**: 建立 Activity -> Step -> Task 的用户旅程地图，为 131 FR 提供"为什么"的视角
**时间**: 2-3 小时
**PM Skill**: `user-story-mapping` (Jeff Patton 框架)
**数据来源**: clinical-workflow.md 流程 + Phase 3 JTBD + 现有 131 FR
**输出文件**: `docs/02-requirements/user-story-map.md` (新建)

#### 故事地图骨架 (基于现有 clinical-workflow)

```
Persona: 中医医生 (Doctor)
Narrative: 完成一次完整诊疗 (从患者到达到处方打印)

[患者识别] -> [创建医案] -> [中医诊断] -> [处方决策] -> [处方开具] -> [保存与打印]
     |              |             |              |              |              |
  搜索患者      新建医案     填写现病史     决定是否处方    选择药材       聚合保存
  读卡识别      加载模板     望诊记录       导入验方       设置剂量       打印处方
  快速建档      关联患者     闻诊记录       复制历史方     计算费用       打印日志
                            问诊记录                      调整配伍
                            切诊/脉诊
                            辨证论治
```

| Task | 说明 |
|------|------|
| P4-1: 定义 3 个核心 Narrative | 首诊流程 / 复诊流程 / 验方管理流程 |
| P4-2: 逐 Activity 分解 Steps | 基于 clinical-workflow.md 现有时序图 |
| P4-3: 逐 Step 分解 Tasks | 每个 Task 映射到对应 FR 编号 |
| P4-4: 纵向优先级切片 | 上层 = Must Have (v1.0 核心) / 中层 = Should Have / 下层 = v2.0 |
| P4-5: Gap 分析 | 识别无 FR 支撑的 Tasks (需求盲区) |

---

### Phase 5: 131 FR 优先级排序

**目标**: 为每个 FR 标注优先级，明确 v1.0 的 Must/Should/Could
**时间**: 1.5-2 小时
**PM Skill**: `prioritization-advisor`
**推荐框架**: MoSCoW (原因: 项目已进入开发阶段，需要简单直接的分类法，不需要 RICE 打分)

| Task | 说明 |
|------|------|
| P5-1: 选定优先级框架 | MoSCoW: Must / Should / Could / Won't |
| P5-2: 排序标准定义 | Must = 核心诊疗流程不可或缺; Should = 显著提升效率; Could = 锦上添花; Won't = 延期到 v2.0 |
| P5-3: 逐模块标注优先级 | 在每个模块 FR 表格中新增 Priority 列 |
| P5-4: 交叉验证 | Phase 4 故事地图的纵向切片 vs MoSCoW 标注是否一致 |
| P5-5: 汇总到 prd.md | prd.md S7 索引表增加优先级统计列 |

**预期分布** (基于项目现状推测):
- **Must**: ~80 FR (核心诊疗流程 + 认证 + 基础 CRUD)
- **Should**: ~30 FR (批量导入、高级搜索、审计功能)
- **Could**: ~15 FR (配置化、诊断工具、高级日志)
- **Won't (v2.0)**: ~6 FR (已在 Out of Scope 中声明)

---

### Phase 6: 顶层 PRD 深化

**目标**: 基于 Phase 1-5 的产出，反向充实 prd.md 各章节
**时间**: 1-1.5 小时
**PM Skill**: `prd-development` Phase 1 + 6 (精修 Executive Summary + Success Metrics)

| Task | prd.md 章节 | 更新内容 |
|------|------------|---------|
| P6-1: 精修 Executive Summary | S1 | 融入 JTBD 视角，从"我们在做什么"升级为"我们为谁解决什么" |
| P6-2: 深化 Problem Statement | S2 | 添加来自各模块 Phase 1 汇总的证据链 |
| P6-3: 链接 Personas | S3 | 引用 personas.md，替代内联描述 |
| P6-4: 添加 JTBD 章节 | S3.x (新) | 引用 jtbd.md 或内联核心 JTBD |
| P6-5: 精修 Success Metrics | S6 | 区分 Primary / Secondary / Guardrail 指标 |
| P6-6: 更新 Requirements Index | S7 | 增加优先级统计、User Story Map 链接 |
| P6-7: 更新 Open Questions | S10 | 标注已决/未决状态 |

---

## 执行顺序与依赖关系

```
Phase 0 (审计修复, 仅 server.md) -- 无依赖，立即执行
    |
    v
Phase 1 -- 已完成，跳过
    |
    v
Phase 2 (Personas) + Phase 3 (JTBD) -- 可并行，依赖 Phase 0
    |              |
    v              v
Phase 4 (User Story Map) -- 依赖 Phase 2 + 3
    |
    v
Phase 5 (优先级排序) -- 依赖 Phase 4 (故事地图提供排序依据)
    |
    v
Phase 6 (顶层 PRD 深化) -- 依赖全部前置 Phase
```

**总时间估算**: 7-10 小时 (可分 2-3 个工作会话完成)

| Phase | 时间 | 可否由 AI 独立执行 |
|-------|------|-------------------|
| Phase 0 | 30min | 可以 (仅 server.md 5 项修复) |
| ~~Phase 1~~ | ~~已完成~~ | -- |
| Phase 2 | 1-1.5h | 需确认 (人物画像需用户验证真实性) |
| Phase 3 | 1.5-2h | 部分可以 (JTBD 推导可自动化，覆盖度审查需确认) |
| Phase 4 | 2-3h | 可以 (基于 clinical-workflow 机械分解) |
| Phase 5 | 1.5-2h | 需确认 (优先级是业务决策，需用户拍板) |
| Phase 6 | 1-1.5h | 可以 (汇总整合) |

---

## 各 Phase 产出物清单

| Phase | 新建文件 | 修改文件 |
|-------|---------|---------|
| Phase 0 | 无 | data-model.md, server.md, glossary.md, sync.md, medical-cases.md, herbs.md, formulas.md, patients.md, card-reader.md, desktop-shell.md |
| Phase 1 | 无 | patients.md, herbs.md, formulas.md, auth.md, sync.md, printing.md |
| Phase 2 | `docs/01-product/personas.md` | prd.md (S3 链接) |
| Phase 3 | `docs/01-product/jtbd.md` | prd.md (S3 链接) |
| Phase 4 | `docs/02-requirements/user-story-map.md` | prd.md (S7 链接) |
| Phase 5 | 无 | 14 个模块文件 (添加 Priority 列), prd.md (S7 统计) |
| Phase 6 | 无 | prd.md (S1/S2/S3/S6/S7/S10) |

**新建**: 3 个文件
**修改**: ~20 个文件 (部分跨 Phase 重复)

---

## YAGNI 裁剪记录

以下 PM Skills 模块评估后**不纳入**本次任务:

| PM Skill | 不纳入原因 |
|----------|----------|
| `discovery-process` / `discovery-interview-prep` | 用户即为诊所方，需求已通过开发过程确认，无需正式 discovery |
| `tam-sam-som-calculator` | 内部工具项目，非商业化产品，市场规模无意义 |
| `customer-journey-map` (体验视角) | clinical-workflow.md 已覆盖完整流程，增加情绪曲线投入产出比低 |
| `positioning-statement` / `press-release` | 非商业化产品，无需外部定位 |
| `roadmap-planning` (细化到 Sprint) | 已有 v1.0/v2.0 划分 + Phase 5 MoSCoW 排序即够，Sprint 级规划属于项目管理 |
| `lean-ux-canvas` | 产品已在开发中，非探索期 |
| `pestel-analysis` | 非商业化产品 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-03-06 | v1.0 | 初始任务清单设计 |
| 2026-03-06 | v1.1 | 调研更新: Phase 1 已完成 (全部 16 模块 Problem Statement 已有); Phase 0 缩减为仅 server.md; 总时间 10-14h -> 7-10h; 新建详细实施计划 `2026-03-06-prd-improvement-plan.md` |
