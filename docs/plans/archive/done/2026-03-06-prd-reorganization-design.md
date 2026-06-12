# PRD 全面整理设计方案

> **创建日期**: 2026-03-06
> **设计者**: PM (Claude)
> **范围**: 顶层 PRD 新建 + 审计偏差修复 + 术语一致性修复

---

## 一、背景与目标

### 现状

- **产品层** (01-product): 5 文件 -- vision, glossary, user-roles, clinical-workflow, README
- **需求层** (02-requirements): 16 文件 -- 131 FR (14 模块) + NFR + UI 交互规范
- **审计偏差**: 2026-02-28 全量审计发现 17 个 PRD 问题 (PRD-01~17)
- **术语不一致**: glossary.md 中 MedicalCaseStatus 仍列 Draft=0，但系统已改用 Suspended

### 缺失

现有文档偏重"模块级功能规格"，缺少标准 PRD 的上层框架:
- Executive Summary (一段话概览)
- Problem Statement (带证据的问题陈述)
- Success Metrics (可量化的成功指标)
- Out of Scope (明确不做什么)
- Dependencies & Risks (依赖和风险)
- Open Questions (未决问题)

### 目标

1. 新建顶层 PRD 文档，补齐框架层
2. 修复 17 个审计偏差，让文档与代码对齐
3. 修复术语不一致问题
4. 更新索引文件

---

## 二、设计决策

| 决策 | 选择 | 理由 |
|------|------|------|
| 顶层 PRD 位置 | `docs/02-requirements/prd.md` | 它是需求层的总纲 |
| 定位 | 混合型 (上半商业 + 下半技术) | 需向诊所方展示价值，也需指导开发 |
| 现有模块文件 | 保留，通过链接引用 | 131 FR 质量高，不重写 |
| Section 7 | 索引式，不重复 FR | 避免内容重复，链接到各模块文件 |
| 审计修复 | 随本次整理一并完成 | 减少重复工作 |

---

## 三、顶层 PRD 结构

```
docs/02-requirements/prd.md (~250 行)
  1. Executive Summary
  2. Problem Statement
  3. Target Users & Personas
  4. Strategic Context
  5. Solution Overview
  6. Success Metrics
  7. Requirements Index (链接到 16 个模块文件)
  8. Out of Scope
  9. Dependencies & Risks
  10. Open Questions
  变更记录
```

### 各章节内容来源

| 章节 | 内容来源 | 新增/引用 |
|------|---------|----------|
| 1. Executive Summary | 从 vision.md 提炼一段话 | 新增 |
| 2. Problem Statement | 中医诊所数字化痛点分析 | 新增 |
| 3. Target Users | 引用 user-roles.md + 补充 persona | 引用+扩展 |
| 4. Strategic Context | 从 vision.md 业务目标 + Why Now | 新增 |
| 5. Solution Overview | 引用 clinical-workflow.md 流程图 | 引用 |
| 6. Success Metrics | 基于 NFR 指标设定量化目标 | 新增 |
| 7. Requirements Index | 复用 README.md 模块索引表 | 引用 |
| 8. Out of Scope | 从 vision.md "系统不包含" 迁移扩展 | 迁移+扩展 |
| 9. Dependencies & Risks | 从审计报告 + SYNC-D02 提取 | 新增 |
| 10. Open Questions | 审计未决项 + 架构待定项 | 新增 |

---

## 四、审计偏差修复清单

### HIGH (4 项)

| ID | 描述 | 修复方案 |
|----|------|---------|
| PRD-01 | data-model.md: Patient 缺字段 | 补充 IdType/EmergencyContact*/DisableReason |
| PRD-02 | server.md: BaseEntity 表格不完整 | 补充 UpdatedBy/RowVersion |
| PRD-03 | server.md: BaseRepository 方法列表不准 | 对齐代码实际方法签名 |
| PRD-04 | server.md: 模块列表含已删除模块 | 移除 Module.Consultation + Module.Prescriptions |

### MEDIUM (8 项)

| ID | 描述 | 修复方案 |
|----|------|---------|
| PRD-05 | data-model.md: Herb 缺 Remark | 补充字段 |
| PRD-06 | data-model.md: PrintCount/LastPrintedAt 位置错 | 从 Prescription 移到 MedicalCase |
| PRD-07 | server.md: "14 个标准方法" 说法不准 | 修正为实际数量 |
| PRD-08 | server.md: BaseReadRepository 不存在 | 移除该引用 |
| PRD-09 | sync.md: DisplayName vs EntityName | 统一为代码中的 EntityName |
| PRD-10 | medical-cases.md: Draft vs Suspended 术语 | 全文替换 Draft -> Suspended |
| PRD-11 | herbs/formulas: Create 应返回 201 | 对齐 Patient 的 201 标准 |
| PRD-12 | patients.md: CheckReference HTTP Method | POST -> GET 对齐代码 |

### LOW (5 项)

| ID | 描述 | 修复方案 |
|----|------|---------|
| PRD-13 | card-reader.md: 配置方式不一致 | 更新为代码实际实现 |
| PRD-14 | card-reader.md: DPAPI 照片加密未实现 | 标注 v2.0 规划 |
| PRD-15 | card-reader.md: 患者去重仅精确匹配 | 更新为实际实现状态 |
| PRD-16 | card-reader.md: RealName -> Name | 字段名更正 |
| PRD-17 | desktop-shell.md: 步骤数不匹配 | 6步 -> 5步 |

### 额外修复

| 项 | 描述 | 修复方案 |
|----|------|---------|
| GLOSSARY-01 | MedicalCaseStatus Draft=0 过时 | 替换为 Suspended=0，对齐 MC-D20 决策 |

---

## 五、文件变更清单

| 操作 | 文件 | 说明 |
|------|------|------|
| 新建 | `docs/02-requirements/prd.md` | 顶层 PRD |
| 修改 | `docs/02-requirements/README.md` | 添加 prd.md 索引 |
| 修改 | `docs/01-product/glossary.md` | MedicalCaseStatus 修复 |
| 修改 | `docs/03-architecture/data-model.md` | PRD-01/05/06 |
| 修改 | `docs/03-architecture/server.md` | PRD-02/03/04/07/08 |
| 修改 | `docs/02-requirements/sync.md` | PRD-09 |
| 修改 | `docs/02-requirements/medical-cases.md` | PRD-10 |
| 修改 | `docs/02-requirements/herbs.md` | PRD-11 |
| 修改 | `docs/02-requirements/formulas.md` | PRD-11 |
| 修改 | `docs/02-requirements/patients.md` | PRD-12 |
| 修改 | `docs/02-requirements/card-reader.md` | PRD-13/14/15/16 |
| 修改 | `docs/02-requirements/desktop-shell.md` | PRD-17 |

**总计**: 1 新建 + 12 修改 = 13 个文件

---

## 六、风险与缓解

| 风险 | 缓解措施 |
|------|---------|
| 修改 server.md/data-model.md 引入新偏差 | 每个修改前读取代码确认实际状态 |
| 顶层 PRD 与模块文件内容重复 | PRD 仅做索引引用，不复制 FR 内容 |
| medical-cases.md Draft->Suspended 替换遗漏 | 全文搜索确认 |

---

## 变更记录

| 日期 | 版本 | 变更内容 |
|------|------|----------|
| 2026-03-06 | v1.0 | 初始设计 |
