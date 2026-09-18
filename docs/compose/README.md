# 过程文档索引（compose/）

> 计划、报告、任务书等过程文档统一归档于此。正式目录（架构/需求/API）只放当前态定义。
> **规则**：过程文档不删除、不放入正式目录；已交付 Spec 与被取代报告移入 `archive/`。

---

## 目录结构

| 目录 | 用途 | 说明 |
|------|------|------|
| `specs/` | 活跃任务 Spec | 按 feature 组织；`status: delivered/done` 的已移入 `archive/specs/` |
| `specs/architecture-optimization-2026-09-16/` | 架构优化设计切片 | 设计 01–06，5 项已落地（ADR-0028/0029/0030），design-06 部分落地（i18n 替换未完成） |
| `reports/` | 调查/审计/评审报告 | 按时间倒序列出（见下）；被后续报告取代的已移入 `archive/reports/` |
| `plans/` | 实施计划/路线图 | 见下 |
| `archive/` | 归档 | 含 `specs/`、`reports/` 两个子目录与既有平铺归档文件 |

---

## specs/ — 当前活跃 Spec

| Spec | 状态 | 日期 | 说明 |
|------|------|------|------|
| [`desktop-navigation-viewmodel-design-2026-09-18.md`](specs/desktop-navigation-viewmodel-design-2026-09-18.md) | **正式文档已同步（N7）** | 2026-09-18 | Desktop 导航架构详细设计（角色守卫/参数契约/单门面/返回路径/对话框收敛；切片 N1–N7）。N7：`desktop-ui-detailed-design.md` §3/§5 + `02-desktop.md` 导航节 + `11a-shell.md` US-SHELL-003/005 状态列已同步；架构测试 4 项已入 `tests/LYBT.Tests.Architecture/`。**代码切片 N1–N6 仍待实施** |
| [`architecture-optimization-2026-09-16/design-01-cancellation-token.md`](specs/architecture-optimization-2026-09-16/design-01-cancellation-token.md) | 已落地 | 2026-09-16 | CancellationToken 全链传播（桌面客户端） |
| [`architecture-optimization-2026-09-16/design-02-domain-error-layer.md`](specs/architecture-optimization-2026-09-16/design-02-domain-error-layer.md) | 已落地 | 2026-09-16 | 领域错误层统一（`ApiClientException`） |
| [`architecture-optimization-2026-09-16/design-03-cross-aggregate-transactions.md`](specs/architecture-optimization-2026-09-16/design-03-cross-aggregate-transactions.md) | 已落地 | 2026-09-16 | 跨聚合写一致性（ADR-0030） |
| [`architecture-optimization-2026-09-16/design-04-cache-strategy.md`](specs/architecture-optimization-2026-09-16/design-04-cache-strategy.md) | 已落地 | 2026-09-16 | 桌面响应缓存（ADR-0029） |
| [`architecture-optimization-2026-09-16/design-05-httpclient-resilience.md`](specs/architecture-optimization-2026-09-16/design-05-httpclient-resilience.md) | 已落地 | 2026-09-16 | HttpClient 池化与弹性（ADR-0028，T3） |
| [`architecture-optimization-2026-09-16/design-06-theme-token-i18n.md`](specs/architecture-optimization-2026-09-16/design-06-theme-token-i18n.md) | **部分落地** | 2026-09-16 | 主题 Token 化 / 样式提取 / i18n — 死资源清理+样式提取已完成，i18n 替换未完成 |
| [`desktop-view-inventory.md`](specs/desktop-view-inventory.md) | 活跃参考 | 2026-09-13 | Desktop View/Control/Dialog 全景清单（代码对齐） |
| [`doc-architecture-redesign.md`](specs/doc-architecture-redesign.md) | 历史设计 | — | 文档架构重设计方案（内容层去重） |
| [`doc-architecture-deep-redesign.md`](specs/doc-architecture-deep-redesign.md) | 历史设计 | — | 文档架构深化精设计方案（Diátaxis + C4） |

> 已交付 Spec（`status: delivered/done`）共 10 份，已移至 [`archive/specs/`](archive/specs/)。

---

## reports/ — 报告（时间倒序）

| 日期 | 报告 |
|------|------|
| 2026-09-18 | [`desktop-navigation-audit-2026-09-18.md`](reports/desktop-navigation-audit-2026-09-18.md) — 审查报告（设计输入；正式文档 N7 已按其结论同步） |
| 2026-09-17 | [`senior-architect-deep-assessment-2026-09-17.md`](reports/senior-architect-deep-assessment-2026-09-17.md) |
| 2026-09-17 | [`full-project-architecture-rereview-2026-09-17.md`](reports/full-project-architecture-rereview-2026-09-17.md) |
| 2026-09-16 | [`full-project-architecture-review-2026-09-16.md`](reports/full-project-architecture-review-2026-09-16.md) |
| 2026-09-16 | [`architecture-optimization-2026-09-16.md`](reports/architecture-optimization-2026-09-16.md) |
| 2026-09-16 | [`frontend-architecture-audit-2026-09-16.md`](reports/frontend-architecture-audit-2026-09-16.md) |
| 2026-09-14 | [`vm-layer-postrefactor-audit-2026-09-14.md`](reports/vm-layer-postrefactor-audit-2026-09-14.md) |
| 2026-08-29 | [`desktop-view-design-audit-2026-08-29.md`](reports/desktop-view-design-audit-2026-08-29.md) |
| 2026-08-29 | [`vm-layer-audit-2026-08-29.md`](reports/vm-layer-audit-2026-08-29.md) |
| 2026-08-29 | [`login-refactor-2026-08-29.md`](reports/login-refactor-2026-08-29.md) |
| 2026-08-26 | [`vm-view-code-review.md`](reports/vm-view-code-review.md) |
| 2026-08-24 | [`shell-vm-research-and-plan-2026-08-24.md`](reports/shell-vm-research-and-plan-2026-08-24.md) |
| 2026-08-23 | [`pen-template-audit-2026-08-23.md`](reports/pen-template-audit-2026-08-23.md) |
| 2026-08-23 | [`desktop-frame-selfcheck-2026-08-23.md`](reports/desktop-frame-selfcheck-2026-08-23.md) |
| 2026-08-23 | [`desktop-frame-inventory-2026-08-23.md`](reports/desktop-frame-inventory-2026-08-23.md) |
| 2026-08-23 | [`design-coverage-check-2026-08-23.md`](reports/design-coverage-check-2026-08-23.md) |
| 2026-08-23 | [`design-requirements-trace-audit-patient-list.md`](reports/design-requirements-trace-audit-patient-list.md) |
| 2026-08-23 | [`middle-zone-audit-admin.md`](reports/middle-zone-audit-admin.md) |
| 2026-08-23 | [`middle-zone-audit-clinical.md`](reports/middle-zone-audit-clinical.md) |
| 2026-08-23 | [`middle-zone-audit-catalog.md`](reports/middle-zone-audit-catalog.md) |
| 2026-08-23 | [`middle-zone-audit-sysadmin.md`](reports/middle-zone-audit-sysadmin.md) |
| 2026-08-22 | [`design-spec-gap-analysis.md`](reports/design-spec-gap-analysis.md) |
| 2026-08-13 | [`vm-interaction-phase1-2026-08-13.md`](reports/vm-interaction-phase1-2026-08-13.md) |

> 已归档报告见 [`archive/reports/`](archive/reports/)。

---

## plans/ — 计划

| 计划 | 说明 |
|------|------|
| [`ui-ux-complete-plan.md`](plans/ui-ux-complete-plan.md) | UI/UX 完整实施计划 |
| [`ui-design-plan.md`](plans/ui-design-plan.md) | UI 设计计划 |
| [`ui-left-sidebar-research-2026-08-23.md`](plans/ui-left-sidebar-research-2026-08-23.md) | 左侧栏调研 |
| [`ui-layout-framework-research-2026-08-23.md`](plans/ui-layout-framework-research-2026-08-23.md) | 布局框架调研（方案选择依据） |
| [`shell-component-extraction-plan.md`](plans/shell-component-extraction-plan.md) | Shell 公共组件抽取计划 |
| [`middle-zone-redesign-proposal.md`](plans/middle-zone-redesign-proposal.md) | 中间区重设计提案 |
| [`view-redraw-changelog.md`](plans/view-redraw-changelog.md) | 逐页重画变更日志 |
| [`design-optimization-plan.md`](plans/design-optimization-plan.md) | 设计优化总计划（源自 design-review R1-R58） |
| [`phase2-migration-sequencing.md`](plans/phase2-migration-sequencing.md) | Phase2 迁移排序与依赖图 |
| [`test-coverage-plan.md`](plans/test-coverage-plan.md) | 测试覆盖计划 |
| [`e2e-integration-test-plan.md`](plans/e2e-integration-test-plan.md) | E2E 集成测试计划 |

---

## archive/ — 归档说明

- **`archive/specs/`**：已交付 Spec（`status: delivered` 或 `status: done`），共 10 份（2026-09-16/17 架构审查与修复系列）。
- **`archive/reports/`**：已被后续报告取代的 2026-08 报告（设计审查、UI 调研、测试框架审查、TD-003、Sprint Review 等），共 9 份。
- **`archive/`（平铺）**：更早的代码审查与架构审查轮次报告（R1–R5、code-review-* 等），16 份。

> 归档原则：文件只移动不删除；交叉引用在移动后同步更新为新路径。

