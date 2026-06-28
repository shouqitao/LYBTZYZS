# 文档查漏补缺与优化 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 落地 `2026-06-28-docs-optimization-design.md` 的三层决策（追溯基础设施 + 核心缺口处理 + 质量治理），使文档体系满足软件需求文档规范。

**Architecture:** 三层并行推进——第 1 层建追溯矩阵与反链基础设施；第 2 层按 7 项缺口判定处理文档（移除审核/边界声明/SignalR 占位/业务规则）；第 3 层机械改动（弱反映项/glossary/边缘 US/治理）。以 spec 为依据，文档为权威，纯文档改动不改代码。

**Tech Stack:** Markdown 文档；验证用 `grep`（残留检查）+ `read`（内容确认）。

## Global Constraints

- **依据**：`docs/compose/specs/2026-06-28-docs-optimization-design.md` [S1]-[S9]
- **权威原则**：文档为设计权威（基线 §7）；代码不符处文档保留 + 标注
- **语言**：中文正文，英文标识符/路径
- **范围**：纯文档改动，**不改任何代码**
- **提交**：git commit 需用户确认；本 plan 的 commit 步骤为建议，执行 agent 在 commit 前询问用户
- **验证惯例**：每任务末尾用 `grep` 确认残留清零、`read` 确认改动生效

---

### Task 1: 追溯矩阵基础设施（[S4]）

**Covers:** [S4]
**Files:**
- Create: `docs/02-requirements/13-traceability-matrix.md`
- Modify: `docs/02-requirements/README.md`（索引补矩阵行）
- Modify: 12 个 ADR（`docs/03-architecture/decisions/0001-0012*.md`）末尾加「关联 US」段
- Modify: `docs/03-architecture/11-business-flows.md`（每个 Flow 加「覆盖 US」表头）
- Modify: `docs/compose/specs/2026-06-28-user-expectation-interview.md`（每个问题点标注对应 US）

**Interfaces:**
- Produces: `13-traceability-matrix.md` 八列表（US ID | 优先级 | 关联 ADR | 关联 Flow | 关联 API | 实现文件 | 访谈问题点 | 状态）

- [ ] **Step 1: 生成矩阵数据** — 遍历 `docs/02-requirements/` 各模块 US，提取每个 US 的优先级/实现参考/双模式端点/交叉引用，汇总为八列表
- [ ] **Step 2: 写矩阵文件** — Create `13-traceability-matrix.md`，含表头说明 + 按模块分节的八列表（覆盖全部 136 US）
- [ ] **Step 3: README 索引** — `02-requirements/README.md` 文件索引表补 `13-traceability-matrix.md` 行
- [ ] **Step 4: ADR 反链** — 12 个 ADR 文件末尾各加「## 关联 US」段（列出该 ADR 影响的 US ID）
- [ ] **Step 5: Flow 表头** — `11-business-flows.md` 每个 Flow 标题下加「**覆盖 US**: US-XXX, US-YYY」行
- [ ] **Step 6: 访谈标注** — `user-expectation-interview.md` 每个问题点（R1-R13/D1-D20/A1-A12/S1-S5/X1-X4）后加「→ US-XXX / 系统外 / 移除」标注
- [ ] **Step 7: 验证** — `grep -c "US-" 13-traceability-matrix.md` 确认覆盖 136；`read` 抽查 3 个 ADR 反链、2 个 Flow 表头
- [ ] **Step 8: Commit**（询问用户后）— `git add docs/02-requirements/13-traceability-matrix.md docs/02-requirements/README.md docs/03-architecture/decisions/ docs/03-architecture/11-business-flows.md docs/compose/specs/2026-06-28-user-expectation-interview.md && git commit -m "docs(traceability): 新增 US 追溯矩阵 + ADR/Flow 反链 + 访谈标注"`

---

### Task 2: 移除「医案审核」清理（[S5]）

**Covers:** [S5]
**Files:**
- Modify: `docs/01-product/01-vision.md`（:23 痛点、:48 卖点、:188 Admin 任务）
- Modify: `docs/01-product/02-personas.md`（:115 Admin 职责、:150 权限矩阵、:309/:349 admin 审核行）
- Modify: `docs/02-requirements/01-prd.md`（:46 Admin「数据审核」）
- Modify: `docs/03-architecture/09-security-architecture.md`（:293/:311 权限矩阵「医案审核」行）
- Modify: `docs/01-product/03-glossary.md`（补「医生负责制」定义）

**Interfaces:**
- Produces: 文档体系不再含「医案审核」语义；glossary 明示「v1.0 医生对医案负责，Admin 不审核」

- [ ] **Step 1: grep 定位** — `grep -rn "审核" docs/01-product docs/02-requirements/01-prd.md docs/03-architecture/09-security-architecture.md`，核对 spec [S5] 清理清单
- [ ] **Step 2: vision 清理** — `01-vision.md` 删:48「医案审核看板」卖点行；:23 痛点「审核信息不互通」改「诊疗信息不互通」；:188 Admin 任务删「医案审核」
- [ ] **Step 3: personas 清理** — `02-personas.md` :115 Admin 职责改「数据维护、用户/药材/验方管理」；:150 权限矩阵 Admin 医案列改「可查询（MC-005/006），不可创建/编辑/审核」；删:309/:349 admin 审核行
- [ ] **Step 4: PRD 清理** — `01-prd.md` :46 Admin「数据审核」改「数据维护」
- [ ] **Step 5: 架构清理** — `09-security-architecture.md` 删:293/:311「医案审核」权限行
- [ ] **Step 6: glossary 定义** — `03-glossary.md` 补「**医生负责制**：v1.0 医生对医案完整内容负责；Admin 不审核医案；变更追溯由审计日志（D1）保障」
- [ ] **Step 7: 验证** — `grep -rn "医案审核\|审核医案\|审核看板" docs/01-product docs/02-requirements/01-prd.md docs/03-architecture/09-security-architecture.md` 期望零命中（compose 历史归档保留）
- [ ] **Step 8: Commit**（询问用户后）— `git commit -m "docs(review-remove): 移除误入的医案审核功能描述，确立医生负责制"`

---

### Task 3: 系统边界声明（[S6]）

**Covers:** [S6]
**Files:**
- Modify: `docs/01-product/01-vision.md`（「系统边界」段补范围外）
- Modify: `docs/02-requirements/01-prd.md`（补「范围外」小节）
- Modify: `docs/03-architecture/11-business-flows.md`（标注打印为终点）

**Interfaces:**
- Produces: vision/PRD 明示「系统止于打印处方笺；付费/发药/库存线下」

- [ ] **Step 1: vision 边界** — `01-vision.md`「系统边界」段补：「**范围外**：收费、发药、库存管理——患者凭打印的处方笺（含详情+价格）线下付费取药」
- [ ] **Step 2: PRD 范围外** — `01-prd.md` 补「## 范围外」小节，列收费/发药/库存为线下流程
- [ ] **Step 3: Flow 标注** — `11-business-flows.md` Flow 1（首诊全流程）步骤 12（打印）后加「**系统终点**：后续付费/发药为线下，不在系统范围（X2.2 系统外）」
- [ ] **Step 4: 验证** — `grep -n "范围外\|系统终点\|线下付费" docs/01-product/01-vision.md docs/02-requirements/01-prd.md docs/03-architecture/11-business-flows.md` 确认三处均有
- [ ] **Step 5: Commit**（询问用户后）

---

### Task 4: SignalR 推送决策落地（[S3]）

**Covers:** [S3]
**Files:**
- Create: `docs/03-architecture/decisions/0013-signalr-realtime-push.md`（ADR 占位）
- Modify: `docs/02-requirements/08-registration.md`（新增 US-REG-008 实时推送）
- Modify: `docs/02-requirements/README.md`（REG 数 7→8，US 总数 136→137）
- Modify: `docs/03-architecture/01-system-overview.md`（架构图标注 SignalR Hub）

**Interfaces:**
- Produces: ADR-0013（占位，待专项设计）；US-REG-008；US 总数更新为 137

> 注：本任务仅落地「v1.0 做 SignalR 推送」的范围决策 + 占位；双模式推送机制细节由后续 SignalR 专项 spec 承载（[S3] 已声明）。

- [ ] **Step 1: ADR-0013 占位** — Create `0013-signalr-realtime-push.md`，含 Status「Accepted（范围决策），实现细节待专项 spec」、Context（R10 访谈：医生需实时看待诊列表，参考市面医院系统）、Decision（v1.0 用 SignalR 推送挂号变更到医生工作台）、Consequences（双模式推送机制待设计：远程 Hub 在 WebAPI / 本地模式待定 / 降级轮询）
- [ ] **Step 2: 新增 US-REG-008** — `08-registration.md` 末尾补 `### US-REG-008: 医生工作台待诊列表实时更新`（角色 Doctor；优先级 Must；验收：新挂号 N 秒内出现在医生待诊列表、状态变更实时同步、推送失败降级轮询；业务规则：依赖 SignalR，见 ADR-0013；实现参考：SignalR Hub）
- [ ] **Step 3: US 总数更新** — `02-requirements/README.md` REG 数 7→8、合计 136→137；REG 总览补 US-REG-008 行
- [ ] **Step 4: 架构图标注** — `01-system-overview.md` 架构图 WebAPI 侧补「SignalR Hub」组件 + 说明「v1.0 实时推送，见 ADR-0013」
- [ ] **Step 5: 验证** — `read 0013-signalr-realtime-push.md` 确认结构；`grep "US-REG-008" docs/02-requirements/` 确认 README 与 08-registration 均含；`grep "137" docs/02-requirements/README.md` 确认总数
- [ ] **Step 6: Commit**（询问用户后）

---

### Task 5: D14 / A2 / R9 业务规则补充（[S7]）

**Covers:** [S7]
**Files:**
- Modify: `docs/02-requirements/09-printing.md`（D14 处方笺单联）
- Modify: `docs/02-requirements/05-herbs.md`（A2 调价审计）
- Modify: `docs/02-requirements/08-registration.md`（R9 大屏标注）
- Modify: `docs/02-requirements/13-reports.md`（A7 标待讨论）—— 若无此文件则在 04-api-reference/13-reports.md 顶部标注

**Interfaces:** 无新接口，补业务规则文本。

- [ ] **Step 1: D14** — `09-printing.md` US-PRINT-001 业务规则段补「v1.0 处方笺**单联**打印（患者持单付费取药）；多联分发（药房联/存根联）属 v2.0」
- [ ] **Step 2: A2** — `05-herbs.md` 补业务规则「调价操作写审计日志（操作人/时间/旧价→新价）；历史处方价格由 `PrescriptionItem` 快照隔离，不受调价影响；最新开方调用最新价格。不建独立调价历史实体（并入 D1 审计）」
- [ ] **Step 3: R9** — `08-registration.md` 补业务规则「患者侧大屏叫号 v2.0/按需；v1.0 候诊队列仅前台/医生端可见（US-REG-004）」
- [ ] **Step 4: A7** — `13-reports.md`（api-reference 或 requirements）顶部加「报表清单（日营业额/就诊量/热门药材/医生工作量等）待专项讨论后补 US」
- [ ] **Step 5: 验证** — `grep "单联\|处方笺" docs/02-requirements/09-printing.md`；`grep "调价.*审计\|PrescriptionItem 快照" docs/02-requirements/05-herbs.md`；`grep "大屏" docs/02-requirements/08-registration.md`
- [ ] **Step 6: Commit**（询问用户后）

---

### Task 6: 弱反映项补全（[S8]）

**Covers:** [S8]
**Files:**
- Modify: `docs/02-requirements/07-medical-cases.md`（D7 辨证录入、D8 必填性、D6 复制处方 US）
- Modify: `docs/02-requirements/05-herbs.md`（D13 剂量单位）
- Modify: `docs/02-requirements/11-platform.md`（S5 强制本地）
- Modify: `docs/01-product/03-glossary.md`（小术语）

**Interfaces:** 补业务规则 + 新 US（D6 复制处方）。

- [ ] **Step 1: D7 辨证录入** — `07-medical-cases.md` US-MC-002 业务规则补「辨证录入：主诉/现病史/舌诊/脉诊/辨证为结构化字段（见 04-data-model Consultation）；舌象/脉象提供常用选项选择器 + 自由文本；v1.0 不做智能辅助诊断」
- [ ] **Step 2: D8 必填性** — `07-medical-cases.md` BR-003 表格补各字段必填性（主诉/现病史/舌诊/脉诊/辨证 必填；既往史 选填）
- [ ] **Step 3: D6 复制处方 US** — `07-medical-cases.md` 新增 `### US-MC-019: 复制上次处方微调`（角色 Doctor；优先级 Should；验收：复诊时一键复制患者最近已完成医案的处方、复制后可增删改药材、保存为新医案处方；业务规则：仅复制药材/剂量，价格按当前最新；注意 US-MC-018 是「批量详情」，复制处方独立为 MC-019）。同步更新 `02-requirements/README.md` MC 数 18→19、US 总数→138
- [ ] **Step 4: D13 剂量单位** — `05-herbs.md` 补业务规则「剂量单位：默认 g（克）；v1.0 不支持单位换算（钱/g 转换）；单位为自由文本字段，Doctor 录入时自定」；glossary 补「剂量单位」术语
- [ ] **Step 5: S5 强制本地** — `11-platform.md` US-SHELL-007 业务规则补「v1.0 仅支持用户主动切换；运维强制某机器走本地策略属 v2.0（SystemAdminOptions 扩展）」
- [ ] **Step 6: glossary 小术语** — `03-glossary.md` 补：草稿水印（未完成医案打印叠加「草稿」）、等候时长、quickVisit（医生快速就诊原子事务）
- [ ] **Step 7: 验证** — `grep "US-MC-019" docs/02-requirements/`；`grep "138" docs/02-requirements/README.md`；`read` 抽查 glossary 新术语
- [ ] **Step 8: Commit**（询问用户后）

---

### Task 7: 边缘 US 修正（[S8]）

**Covers:** [S8]
**Files:**
- Modify: `docs/02-requirements/07-medical-cases.md`（MC-018 编号、MC-008/009 边界）
- Modify: `docs/02-requirements/03-users.md`（USER-011/012 验收细化）

**Interfaces:** 无新接口，细化验收文本。

- [ ] **Step 1: MC-018 编号澄清** — `07-medical-cases.md` US-MC-018 标题确认「批量详情查询（≤50）」（复制处方已挪到 MC-019，见 Task 6）；加交叉引用注
- [ ] **Step 2: MC-008/009 边界** — `07-medical-cases.md` US-MC-008/009 加边界说明「历史聚合查询（跨医案 Consultation/Prescription 列表） vs US-MC-006 query?type=（当前医案查询）—— 两者不重叠，MC-008/009 聚合历史，MC-006 查当前」
- [ ] **Step 3: USER-011 验收细化** — `03-users.md` US-USER-011（Restore）验收标准从 3 条扩展：恢复后用户状态、关联医案归属保留、仅 SuperAdmin 可执行、已禁用用户不可恢复 等
- [ ] **Step 4: USER-012 拆分验收** — `03-users.md` US-USER-012（批量）验收拆分「批量删除/批量启用/批量禁用」三种操作的独立验收条件
- [ ] **Step 5: 验证** — `read` 抽查 MC-018/MC-008/USER-011/USER-012 确认细化
- [ ] **Step 6: Commit**（询问用户后）

---

### Task 8: 结构治理（[S8]）

**Covers:** [S8]
**Files:**
- Modify: `docs/compose/` 下已完成的 plan/spec 迁 archive
- Modify: 各文档变更记录规范化（抽查）

**Interfaces:** 无。

- [ ] **Step 1: compose 归档判定** — 遍历 `docs/compose/specs/` 与 `docs/compose/plans/`（非 archive），对照 git log/实现状态，识别已完成的（如 2026-06-22 shell-redesign 系列已实施）应迁 archive
- [ ] **Step 2: 迁移** — 将已完成的 plan/spec 移至对应 `archive/` 子目录；更新 `docs/compose/README.md` 的索引状态
- [ ] **Step 3: 断链检查** — `grep -rn "\[.*\](.*\.md)" docs/` 抽查内部链接，识别 404 断链（指向不存在文件）；修复或标注
- [ ] **Step 4: 变更记录规范** — 抽查 5 个文档底部变更记录，确认本次改动有日期+版本+说明条目；缺失的补
- [ ] **Step 5: 验证** — 迁移后 `ls docs/compose/specs/*.md`（非 archive）确认仅活跃项；`grep` 断链抽查
- [ ] **Step 6: Commit**（询问用户后）— `git commit -m "docs(governance): compose 归档清理 + 断链修复 + 变更记录规范"`

---

### Task 9: 终验与 spec 标记（[S9]）

**Covers:** [S9]
**Files:**
- Modify: `docs/compose/specs/2026-06-28-docs-optimization-design.md`（状态 📝→✅，补完成日期）
- Modify: `docs/compose/reports/`（新建完成报告，可选）

**Interfaces:** 无。

- [ ] **Step 1: S9 验收逐项** —
  - 7 项缺口反映：`grep` 各标注存在
  - 追溯矩阵覆盖：`grep -c "US-" 13-traceability-matrix.md` ≥137（含 REG-008/MC-019）
  - 审核清理：`grep "医案审核" docs/01-product docs/02-requirements/01-prd.md docs/03-architecture/09-security-architecture.md` 零命中
  - 系统边界：vision/PRD 含「范围外」
  - SignalR：ADR-0013 + US-REG-008 存在
  - 机械改动：Task 6/7/8 完成
- [ ] **Step 2: 标记 spec** — `2026-06-28-docs-optimization-design.md` 顶部状态改「✅ 已实施 2026-06-28」
- [ ] **Step 3: 完成报告** —（可选）Create `docs/compose/reports/2026-06-28-docs-optimization-complete.md`，记录改动汇总 + US 总数终值（138）+ 遗留（SignalR 专项、A7 报表清单）
- [ ] **Step 4: Commit**（询问用户后）

---

## 自审

**1. Spec 覆盖**：[S1]背景（无需任务）、[S2]判定汇总（Task 4/5/6 体现）、[S3]SignalR（Task 4）、[S4]追溯（Task 1）、[S5]审核移除（Task 2）、[S6]边界（Task 3）、[S7]D14/A2/A7/R9（Task 5）、[S8]机械改动（Task 6/7/8）、[S9]验收（Task 9）—— 全覆盖。✅
**2. Placeholder 扫描**：无 TBD/TODO；SignalR 细节明确委托专项（非 placeholder，是范围声明）。✅
**3. 类型/命名一致**：US-REG-008（Task 4）、US-MC-019（Task 6）、ADR-0013（Task 4）跨任务一致；US 总数 137（Task 4 后）→138（Task 6 后）逐步更新。✅

> 注：US 总数因新增 REG-008 与 MC-019 从 136 增至 138；Task 4 先到 137，Task 6 到 138，各 Task 同步更新 README。
