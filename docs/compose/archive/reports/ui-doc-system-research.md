# UI/UX 文档体系调研报告（已归档）

> **归档说明**: 本报告已于 2026-09-17 归档。原始文件路径: `docs/compose/reports/ui-doc-system-research.md`
> **归档原因**: 调研结论已落地为 `docs/07-ui-ux/` 完整文档体系
> **原始日期**: 2026-08-22 | **角度**: 文档体系（附加调研，R01-R22 之外）

## 归档内容摘要

### 调研目标

回答：**一个完整的 UI/UX 文档体系应该包含哪些文档？每个文档的定位、内容、与其他文档的关系是什么？** 并为 LYBTZYZS 给出可落地的文档体系建议。

### 核心结论（先说答案）

一个完整的 UI/UX 文档体系是 **四层 + 一治理** 的金字塔，缺一不可：

```
第4层  交付与度量（Handoff & Measurement）
  └─ 设计标注/切图/动效规格 + 可用性测试报告 + 埋点度量

第3层  规格与模式（Specification & Patterns）
  └─ 页面详细规格 + 交互模式库 + 信息架构/导航图 + 用户流程图

第2层  系统与组件（System & Components）
  └─ 设计系统（Design System）：Token + 组件库 + 图标/插画 + 内容指南

第1层  研究与策略（Research & Strategy）
  └─ 用户研究：画像/旅程图/竞品分析 + 产品需求：PRD/用户故事 + 战略：愿景/原则

贯穿  治理（Governance）：版本/贡献/废弃/审计流程 — 单一可信源（SSOT）
```

**LYBTZYZS 当前覆盖度**：第1层 80%（US/权限/流程较全，画像/旅程待补）、第2层 60%（`TcmBrands/Spacing/Surfaces` 有 Token，组件 16 个有代码但缺"用法+变体+可访问性"文档）、第3层 40%（本次 `desktop-ui-detailed-design.md` v2.0 首次补全 17 页详细规格，之前仅 10 个 `.pen`）、第4层 20%（无标注/动效/可用性报告）。本次调研即为补齐第2-3 层的设计。

## 后续落地文档

- **UI/UX 文档体系**: [`docs/07-ui-ux/README.md`](../../07-ui-ux/README.md)
- **桌面端需求**: [`docs/07-ui-ux/desktop-ui-requirements.md`](../../07-ui-ux/desktop-ui-requirements.md)
- **桌面端详细设计**: [`docs/07-ui-ux/desktop-ui-detailed-design.md`](../../07-ui-ux/desktop-ui-detailed-design.md)
- **桌面端设计规范**: [`docs/07-ui-ux/desktop-design-spec.md`](../../07-ui-ux/desktop-design-spec.md)
- **桌面端设计 Token**: [`docs/07-ui-ux/desktop-design-tokens.md`](../../07-ui-ux/desktop-design-tokens.md)
- **桌面端布局框架**: [`docs/07-ui-ux/desktop-layout-framework.md`](../../07-ui-ux/desktop-layout-framework.md)
- **桌面端 UX 用户旅程**: [`docs/07-ui-ux/desktop-ux-user-journeys.md`](../../07-ui-ux/desktop-ux-user-journeys.md)
- **桌面端 UX 交互规范**: [`docs/07-ui-ux/desktop-ux-interaction-spec.md`](../../07-ui-ux/desktop-ux-interaction-spec.md)
- **桌面端 UX 错误处理**: [`docs/07-ui-ux/desktop-ux-error-handling.md`](../../07-ui-ux/desktop-ux-error-handling.md)
- **桌面端 UX 加载状态**: [`docs/07-ui-ux/desktop-ux-loading-states.md`](../../07-ui-ux/desktop-ux-loading-states.md)
- **ViewModel 层设计**: [`docs/07-ui-ux/viewmodel-layer-design.md`](../../07-ui-ux/viewmodel-layer-design.md)
