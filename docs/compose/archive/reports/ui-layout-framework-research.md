# 桌面应用固定布局框架调研 — 3 方案（已归档）

> **归档说明**: 本报告已于 2026-09-17 归档。原始文件路径: `docs/compose/reports/ui-layout-framework-research.md`
> **归档原因**: 决策已定并落地为 SSOT `docs/07-ui-ux/desktop-layout-framework.md`
> **原始日期**: 2026-08-22 | **方式**: web_search 4 轮 + 代码分析

## 归档内容摘要

- **本文档只呈现 3 个方案与事实，不做推荐**。选择权在用户。
- **现状基线**: `src/Client/Desktop/Shell/Views/MainWindow.xaml`（已通读全文 178 行）
- **固定元素（现状）**: 侧边栏 (Primary Brush) + 内容区 ContentRegion (Border Margin=4) + 状态栏 32px (API|模式|用户|时间)

### 3 方案概览

| 方案 | 核心思路 | 优势 | 劣势 |
|------|---------|------|------|
| **方案 1** | 纯侧边栏（现状扩展） | 改动最小 | Master-Detail 布局受限 |
| **方案 2** | 三栏端规则：固定头栏 48px + 可折叠侧栏 240/64 + 状态栏 32px | 符合医疗软件规范，支持 Master-Detail | 需重构 MainWindow |
| **方案 3** | 无侧边栏（汉堡菜单） | 内容区最大 | 导航不可见，违反医疗软件规范 |

## 后续落地文档

- **布局框架决策**: [`ui-layout-framework-decision.md`](../reports/ui-layout-framework-decision.md)（选择方案 2）
- **布局框架 SSOT**: [`docs/07-ui-ux/desktop-layout-framework.md`](../../07-ui-ux/desktop-layout-framework.md)（定版 48/240/64/32/220ms/1280-1440-1920）
- **对标自检**: [`desktop-frame-selfcheck-2026-08-23.md`](../reports/desktop-frame-selfcheck-2026-08-23.md)
