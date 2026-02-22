# 设计与计划文档

本目录包含项目的设计方案和实施计划。

---

## 活跃文档

当前开发周期中频繁参考的文档。

| 文件 | 内容 | 最后更新 |
|------|------|----------|
| [Sprint 路线图](2026-02-21-unified-architecture-sprint-roadmap.md) | 305 项任务，5 个 Sprint 分配 | 2026-02-21 |
| [功能检查清单](2026-02-11-system-function-checklist.md) | 全系统功能完成状态追踪 | 2026-02-22 |
| [系统架构图](2026-02-21-system-architecture-diagrams.md) | 21 张 Mermaid 架构图 (领域模型/部署/数据流等) | 2026-02-21 |
| [设计问题与解决方案](2026-02-21-design-issues-solutions.md) | 8 项架构设计问题的修复方案 | 2026-02-21 |
| [架构深度对比](2026-02-21-architecture-deep-comparison.md) | D1-D8 八维度 PRD-代码偏差评估基线 | 2026-02-21 |

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

### 一次性分析报告 (4 个)

| 文件 | 说明 |
|------|------|
| prd-code-deep-scan-report | PRD-代码深度扫描报告 |
| deviation-triage-checklist | 偏差分类检查清单 |
| prd-design-gap-analysis | PRD-设计缺口分析 |
| code-fix-backlog | 代码修复积压列表 |

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

*文档版本: v1.0 | 最后更新: 2026-02-22*
