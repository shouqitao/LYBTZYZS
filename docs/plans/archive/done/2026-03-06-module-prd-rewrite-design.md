# 模块 PRD 全面重写设计

**日期**: 2026-03-06
**状态**: 已确认，待执行

---

## 背景

现有 14 个模块需求文件 (docs/02-requirements/) 采用 "需求规格" 格式，包含概述、用户角色、功能清单 (FR)、数据模型、错误码、决策记录。功能清单质量高 (131 FR，含验收标准和双模式行为)，但缺少 PRD Development 框架要求的 6 个关键章节: Problem Statement、Strategic Context、Success Metrics、Epic Hypothesis、Out of Scope、Dependencies & Risks。

## 目标

将所有 14 个模块文件从 "需求规格" 升级为完整的 "模块 PRD"，遵循 PRD Development 10 章节标准结构，同时将 131 个 FR 重写为 User Story 格式。

---

## 新模块 PRD 结构

```
# [模块名] 产品需求文档

## 1. Problem Statement                ← 新增
   ### 1.1 问题描述
   ### 1.2 用户痛点 (角色/痛点/影响 表格)
   ### 1.3 证据

## 2. Target Users                     ← 现有"用户角色"升级
   (角色/权限 表格)

## 3. Strategic Context                ← 新增
   ### 3.1 业务目标 (目标/对应模块 表格)
   ### 3.2 Why Now

## 4. Solution Overview                ← 现有"概述"升级
   (模块高层描述 + 核心能力)

## 5. Success Metrics                  ← 新增
   (指标/当前/目标/衡量方式 表格)

## 6. Epic Hypothesis                  ← 新增
   "We believe that... for... will achieve... We'll know we're right when..."

## 7. User Stories                     ← 现有 FR 重写
   ### US-[MODULE]-NNN: [标题]
   > As a [角色], I want to [操作], so that [价值]。

   **Acceptance Criteria:**
   - [ ] 条件 → 预期结果

   **Business Rules:**
   1. 规则描述

   **Dual Mode:**
   | 模式 | 行为 |
   |------|------|
   | 远程 | API 端点 + 行为 |
   | 本地 | 本地行为 |

## 8. Out of Scope                     ← 新增
   (排除项/原因 表格)

## 9. Dependencies & Risks             ← 新增
   (风险/影响/缓解措施 表格)

## 10. Open Questions                  ← 新增
   (ID/问题/状态 表格)

## Data Model                          ← 保留
## Error Codes                         ← 保留
## Decision Log                        ← 保留
## Change Log                          ← 更新
```

---

## 编号迁移规则

| 现有 | 新编号 | 示例 |
|------|--------|------|
| FR-AUTH-001 | US-AUTH-001 | 用户登录 |
| FR-USER-001 | US-USER-001 | 查看用户列表 |
| FR-PAT-001 | US-PAT-001 | 新增患者 |
| FR-HERB-001 | US-HERB-001 | 创建药材 |
| FR-FORM-001 | US-FORM-001 | 创建验方 |
| FR-MC-001 | US-MC-001 | 创建医案 |
| FR-SYNC-001 | US-SYNC-001 | 获取同步元数据 |
| FR-PRINT-001 | US-PRINT-001 | 打印处方 |
| FR-CARD-001 | US-CARD-001 | 读取身份证 |
| FR-SYS-001 | US-SYS-001 | 基础健康检查 |
| FR-ERR-001 | US-ERR-001 | 全局异常兜底 |
| FR-LOG-001 | US-LOG-001 | 结构化日志记录 |
| FR-SHELL-001 | US-SHELL-001 | 模块化加载 |
| FR-CFG-001 | US-CFG-001 | 应用配置管理 |

---

## User Story 格式规范

### 模板

```markdown
### US-[MODULE]-NNN: [简短标题]

> As a [角色], I want to [具体操作],
> so that [业务价值/用户收益]。

**Acceptance Criteria:**
- [ ] [前置条件/操作] → [预期结果]

**Business Rules:**
1. [规则描述]

**Dual Mode:**
| 模式 | 行为 |
|------|------|
| 远程 | [API 端点 + 行为描述] |
| 本地 | [本地行为描述] |
```

### 转换原则

1. **信息零丢失**: 现有 FR 的所有技术细节 (API 端点、错误码、双模式行为) 必须保留
2. **角色明确化**: "As a" 使用具体角色 (Doctor/Admin/User)，不用笼统的 "用户"
3. **价值导向**: "so that" 必须是业务价值，不是技术描述
4. **验收标准分离**: 从 FR 正文中提取为独立 Checkbox 列表
5. **业务规则分离**: 从 FR 正文中提取为独立编号列表
6. **双模式统一**: 远程/本地行为统一为表格格式
7. **修订注释保留**: 现有 `[已修订]` 注释迁移到 Decision Log

---

## 新增章节内容指导

### Problem Statement
- 聚焦该模块解决的具体业务问题
- 痛点表使用 角色/痛点/影响 三列
- 证据来源: 产品需求分析、临床工作流观察、行业合规要求

### Strategic Context
- 业务目标关联顶层 PRD (prd.md) 的 4.1 业务目标
- Why Now 说明模块在 v1.0 的必要性

### Success Metrics
- 每个模块 3-5 个可量化指标
- 包含 当前/目标/衡量方式 三列
- 指标应可验证 (日志统计、用户反馈、零投诉等)

### Epic Hypothesis
- 一段话的可验证假设
- 格式: "We believe that [方案] for [用户] will achieve [目标]. We'll know we're right when [指标]."

### Out of Scope
- 明确该模块 v1.0 不做什么
- 包含 排除项/原因 两列
- 部分可标注 "v2.0+ 考虑"

### Dependencies & Risks
- 模块级技术和业务风险
- 包含 风险/影响/缓解措施 三列

### Open Questions
- 模块级未决问题
- 包含 ID/问题/状态 三列
- ID 格式: OQ-[MODULE]-NN

---

## 执行策略

### Phase 1: 参考实现 (auth.md)
手动完成 auth.md 的完整重写，作为其他模块的模板参考。

### Phase 2: 中等模块 (5 个, 并行)
users.md, patients.md, herbs.md, formulas.md, medical-cases.md

### Phase 3: 小模块 Batch 1 (4 个, 并行)
sync.md, printing.md, card-reader.md, health-diagnostics.md

### Phase 4: 小模块 Batch 2 (4 个, 并行)
error-handling.md, logging.md, desktop-shell.md, configuration.md

### Phase 5: 索引更新
- 更新 docs/02-requirements/README.md (编号体系 FR→US)
- 更新 docs/02-requirements/01-prd.md (Requirements Index)
- 更新 docs/README.md (文件计数、日期)

---

## 模块清单 (14 个)

| # | 模块 | 文件 | FR 数 | 复杂度 |
|---|------|------|-------|--------|
| 1 | 认证与会话管理 | auth.md | 13 | 中 |
| 2 | 用户管理 | users.md | 12 | 中 |
| 3 | 患者管理 | patients.md | 13 | 中 |
| 4 | 药材管理 | herbs.md | 13 | 中 |
| 5 | 验方管理 | formulas.md | 13 | 中 |
| 6 | 医案管理 | medical-cases.md | 18 | 高 |
| 7 | 数据同步 | sync.md | 8 | 中 |
| 8 | 打印 | printing.md | 4 | 低 |
| 9 | 身份证读卡器 | card-reader.md | 2 | 低 |
| 10 | 系统健康与诊断 | health-diagnostics.md | 9 | 低 |
| 11 | 异常处理策略 | error-handling.md | 8 | 低 |
| 12 | 日志与审计 | logging.md | 7 | 低 |
| 13 | Desktop Shell | desktop-shell.md | 7 | 低 |
| 14 | 配置参数 | configuration.md | 4 | 低 |

**总计: 131 FR → 131 US**

---

## 联动更新

| 文件 | 更新内容 |
|------|----------|
| docs/02-requirements/README.md | FR→US 编号体系说明，文件数更新 |
| docs/02-requirements/01-prd.md | Section 7 Requirements Index 编号更新 |
| docs/README.md | 文件计数、最后更新日期 |
| docs/02-requirements/17-nfr.md | 无变更 (非功能性需求不涉及 US 转换) |
| docs/02-requirements/18-ui-patterns.md | 无变更 |
