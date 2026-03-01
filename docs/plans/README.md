# 设计与计划文档

本目录包含项目的设计方案和实施计划。

---

## 活跃文档

当前开发周期中频繁参考的文档。

| 文件 | 内容 | 最后更新 |
|------|------|----------|
| [剩余任务](2026-03-01-remaining-tasks.md) | Sprint 5 遗留 + 审计报告 OPEN 项整合 | 2026-03-01 |

## 归档文档

已完成任务的设计/计划、一次性分析报告、任务已录入路线图的设计文档。

归档文件保存在 [`archive/`](archive/) 子目录中，git 历史完整保留。

### 已完成任务 (12 个)

| 文件 | 完成日期 |
|------|----------|
| documentation-system-design/plan | 2026-02-10 |
| requirements-deepening-design/plan | 2026-02-10 |
| doc-code-alignment-design/plan | 2026-02-10 |
| remove-entity-audit-plan | 2026-02-10 |
| prd-completion-design/plan/impl-plan | 2026-02-11 |
| test-restructure-design/plan | 2026-02-08 |

### Sprint 已完成设计 (3 个, 2026-02-25 归档)

| 文件 | 完成日期 | 说明 |
|------|----------|------|
| sprint1-design | 2026-02-25 | Sprint 1 全部 33/33 完成 |
| design-issues-solutions | 2026-02-25 | 8 项设计问题全部实施 |
| design-deepening-phase3 | 2026-02-25 | 数据流/状态流设计已实施 |

### Sprint 5 完成归档 (8 个, 2026-02-28 归档)

| 文件 | 完成日期 | 说明 |
|------|----------|------|
| sprint3-design | 2026-02-28 | Sprint 3 全部完成，已无参考需求 |
| sprint-readiness-assessment | 2026-02-28 | Sprint 1-5 均已启动，评估使命完成 |
| architecture-deep-comparison | 2026-02-28 | 架构分析已转化为 Sprint 任务 |
| a4-03-authorization-handler-evaluation | 2026-02-28 | Sprint 4 评估，Sprint 4 已完成 |
| system-function-checklist | 2026-02-28 | 功能清单使命完成，剩余任务已独立提取 |
| system-architecture-diagrams | 2026-02-28 | 架构参考文档，不影响 Sprint 5 剩余工作 |
| unified-architecture-sprint-roadmap | 2026-02-28 | Sprint 5 剩余任务已独立提取，路线图使命完成 |
| full-sprint-design | 2026-02-28 | Sprint 1-4 已执行完毕，Sprint 5 有独立文档 |

### 跨模块解耦 (2 个, 2026-02-25 归档)

| 文件 | 完成日期 | 说明 |
|------|----------|------|
| cross-module-decoupling-design | 2026-02-23 | D5 ISP 拆分 + Sync 解耦 (PR #2263) |
| cross-module-decoupling-plan | 2026-02-23 | 同上 |

### Sprint 5 审计与收尾 (2 个, 2026-03-01 归档)

| 文件 | 完成日期 | 说明 |
|------|----------|------|
| code-vs-prd-full-audit-report | 2026-02-28 | 全量审计报告，发现项已分批处理 |
| sprint5-remaining-tasks | 2026-02-28 | 原 28 项任务清单，14 项已完成，剩余已转入新文档 |

### 一次性分析报告 (7 个)

| 文件 | 说明 |
|------|------|
| prd-code-deep-scan-report | PRD-代码深度扫描报告 |
| deviation-triage-checklist | 偏差分类检查清单 |
| prd-design-gap-analysis | PRD-设计缺口分析 |
| code-fix-backlog | 代码修复积压列表 |
| code-doc-audit-report | 代码-文档审查报告 (2026-02-25 归档) |
| architecture-analysis-report | 架构分析报告 (2026-02-25 归档) |
| d2-d5-design-patterns-dependencies | D2-D5 设计模式依赖分析 (2026-02-25 归档) |

### 设计文档 (任务已录入路线图) (8 个)

| 文件 | 说明 |
|------|------|
| auth-architecture-refactor-design | 认证架构重构 |
| desktop-ui-ux-optimization | Desktop UI/UX 优化 |
| viewmodel-refactoring-design | ViewModel 重构 |
| dead-code-cleanup-design | 死代码清理 |
| unify-control-data-binding | 数据绑定统一 |
| resource-sink-refactor-design + refactor | 资源管理重构 |
| prd-deepening-outline | PRD 深化大纲 |

---

## 管理规则

1. **新设计文档** 创建在 `plans/` 根目录，命名: `YYYY-MM-DD-<topic>-design.md`
2. **任务完成后** 移动到 `archive/`
3. **活跃文档** 保持在 5-8 个以内，超出时评估归档

---

*文档版本: v1.3 | 最后更新: 2026-03-01*
