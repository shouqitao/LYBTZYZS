# 模块级设计文档补全 实施计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use compose:subagent (recommended) or compose:execute to implement this plan task-by-task.

**Goal:** 为 10 个 v1.0 模块创建独立设计文档，从架构级细化到模块级。

**Architecture:** 按模块复杂度分 4 批次实施，每个模块设计文档包含 7 个标准章节（概述/接口/状态机/数据流/异常/规则/交互）。

**Tech Stack:** Markdown 文档，Mermaid 图表，遵循现有 `03-architecture/` 目录结构。

## Global Constraints

- 文档权威原则：01-product > 02-requirements > 03-architecture
- v1.0 范围：141 US = 10 模块
- 新文件保存在 `docs/03-architecture/modules/` 目录
- 遵循现有 ADR 决策（15个）
- 与 `04-api-reference/` 端点文档保持一致
- 中文正文，英文技术标识符

---

## 批次 1：P0 核心模块

### Task 1: MedicalCase 模块设计文档

**Covers:** [S1, S2, S3]

**Files:**
- Create: `docs/03-architecture/modules/medical-case.md`

**Interfaces:**
- Consumes: ADR-0001（聚合根）、ADR-0008（Token安全）、04-data-model.md
- Produces: 19 个 US 的设计映射

**Steps:**
- [ ] 创建文件，编写模块概述（职责/边界/依赖）
- [ ] 编写接口契约（IMedicalCaseFacade、MedicalCasesController 端点映射）
- [ ] 编写状态机图（Active↔Suspended→Completed/Cancelled）
- [ ] 编写数据流时序图（挂号→诊断→开方→打印）
- [ ] 编写异常处理规范（BR-001/BR-003/打印保护）
- [ ] 编写业务规则（聚合根约束/跨模块引用）
- [ ] 编写模块交互（与 Registration/Auth/Patients/Herbs/Formulas/Printing/Reports 的关系）
- [ ] 映射 19 个 US 到设计文档章节
- [ ] Commit: `docs: add MedicalCase module design`

### Task 2: Registration 模块设计文档

**Covers:** [S1, S2, S3]

**Files:**
- Create: `docs/03-architecture/modules/registration.md`

**Interfaces:**
- Consumes: ADR-0009（双模式）、ADR-0013（SignalR）、05-dual-mode.md
- Produces: 8 个 US 的设计映射

**Steps:**
- [ ] 创建文件，编写模块概述
- [ ] 编写接口契约（IRegistrationService、RegistrationController 端点）
- [ ] 编写状态机（Waiting→InProgress→Completed/Cancelled）
- [ ] 编写双模式工作流（远程链 + 本地链 + QuickVisit）
- [ ] 编写 SignalR 推送设计（ADR-0013 范围）
- [ ] 编写异常处理（挂号冲突/权限检查）
- [ ] 编写业务规则（BR-001 联动/原子创建）
- [ ] 编写模块交互（与 MedicalCase/Auth/Patients 的关系）
- [ ] Commit: `docs: add Registration module design`

### Task 3: Auth 模块设计文档

**Covers:** [S1, S2, S3]

**Files:**
- Create: `docs/03-architecture/modules/auth.md`

**Interfaces:**
- Consumes: ADR-0004（用户上下文）、ADR-0005（SuperAdmin）、ADR-0008（Token安全）、09-security-architecture.md
- Produces: 13 个 US 的设计映射

**Steps:**
- [ ] 创建文件，编写模块概述
- [ ] 编写接口契约（IAuthService、AuthController 端点）
- [ ] 编写 Token 生命周期（AccessToken/RefreshToken/AutoLoginToken）
- [ ] 编写多角色认证流程（远程/本地/简化）
- [ ] 编写异常处理（锁定/限流/枚举防护）
- [ ] 编写业务规则（WorkFactor/旧会话清理/审计日志）
- [ ] 编写模块交互（与 Users/双模式的关系）
- [ ] Commit: `docs: add Auth module design`

---

## 批次 2：P1 业务模块

### Task 4: Patients 模块设计文档

**Covers:** [S1, S2]

**Files:**
- Create: `docs/03-architecture/modules/patients.md`

**Steps:**
- [ ] 创建文件，编写概述/接口/数据流/异常/规则/交互
- [ ] 重点：拼音搜索算法、读卡器集成、敏感数据脱敏、引用检查(BR-DEL-001)
- [ ] Commit: `docs: add Patients module design`

### Task 5: Herbs 模块设计文档

**Covers:** [S1, S2]

**Files:**
- Create: `docs/03-architecture/modules/herbs.md`

**Steps:**
- [ ] 创建文件，编写概述/接口/状态机(Draft↔Validated)/数据流/异常/规则/交互
- [ ] 重点：引用检查(BR-DEL-001)、Excel导入、拼音检索
- [ ] Commit: `docs: add Herbs module design`

### Task 6: Formulas 模块设计文档

**Covers:** [S1, S2]

**Files:**
- Create: `docs/03-architecture/modules/formulas.md`

**Steps:**
- [ ] 创建文件，编写概述/接口/验证状态机(Draft↔Validated)/延迟绑定/共享机制/交互
- [ ] Commit: `docs: add Formulas module design`

---

## 批次 3：P2 标准模块

### Task 7: Users 模块设计文档

**Covers:** [S1, S2]

**Files:**
- Create: `docs/03-architecture/modules/users.md`

**Steps:**
- [ ] 创建文件，编写概述/接口/四级权限/批量操作/Restore/交互
- [ ] Commit: `docs: add Users module design`

### Task 8: Printing 模块设计文档

**Covers:** [S1, S2]

**Files:**
- Create: `docs/03-architecture/modules/printing.md`

**Steps:**
- [ ] 创建文件，编写概述/双引擎渲染(A5/A4/QuestPDF)/模板体系/打印保护/交互
- [ ] Commit: `docs: add Printing module design`

### Task 9: Reports 模块设计文档

**Covers:** [S1, S2]

**Files:**
- Create: `docs/03-architecture/modules/reports.md`

**Steps:**
- [ ] 创建文件，编写概述/聚合查询/时间范围/交互
- [ ] Commit: `docs: add Reports module design`

---

## 批次 4：P3 补全

### Task 10: Platform 模块设计补全

**Covers:** [S1, S2]

**Files:**
- Modify: `docs/03-architecture/11a-shell.md` (已有)
- Modify: `docs/03-architecture/11b-configuration.md` (已有)
- Modify: `docs/03-architecture/11c-error-handling.md` (已有)
- Modify: `docs/03-architecture/11d-observability.md` (已有)
- Modify: `docs/03-architecture/11e-cardreader.md` (已有)

**Steps:**
- [ ] 审查现有 5 个文件，检查是否符合标准模板
- [ ] 补充缺失章节（接口契约/数据流/模块交互）
- [ ] 确保与新创建的模块设计文档一致
- [ ] Commit: `docs: complete Platform module design coverage`

---

## 批次 5：终验

### Task 11: 交叉验证与链接

**Covers:** [S4, S5, S6]

**Steps:**
- [ ] 验证每个模块设计文档的接口契约与 `04-api-reference/` 一致
- [ ] 验证状态机与 `07-medical-cases.md` 需求一致
- [ ] 验证业务规则与 `02-requirements/` US 映射完整
- [ ] 验证异常处理与 `06-error-handling.md` 规范一致
- [ ] 更新 `03-architecture/README.md` 补充模块设计文档索引
- [ ] 更新 `docs/02-requirements/13-traceability-matrix.md` 补充模块设计追溯
- [ ] Commit: `docs: module design cross-validation complete`

---

## Self-Review

1. **Spec coverage:** [S1]-[S6] 均有对应 Task。✅
2. **No placeholders:** 所有步骤使用动词开头的具体动作。✅
3. **Type consistency:** 模块名/US编号/接口名称在各 Task 间一致。✅
