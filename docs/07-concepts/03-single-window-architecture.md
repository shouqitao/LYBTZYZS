---
type: concept
title: 单窗口架构 (Single Window Architecture)
created: 2026-06-11
updated: 2026-06-11
tags: [architecture, wpf, prism, ui-pattern]
related: [desktop-shell, navigation-coordinator, mvvm-prism]
sources: ["docs/06-operations/dead-code-analysis-frontend.md"]
---

# 单窗口架构 (Single Window Architecture)

## 定义

单窗口架构是一种桌面应用设计模式，指应用程序在整个生命周期中仅维持一个主窗口（Main Window），通过内部区域（Region）导航来切换不同的视图内容，而非弹出多个独立的顶层窗口。

## 在 LYBTZYZS 中的应用

LYBTZYZS 桌面客户端已从早期的多窗口模式迁移至单窗口架构。这一演进体现在以下方面：

1.  **登录流程简化**：原有的 `LoginWindow`（独立窗口）已被废弃，取而代之的是在主窗口区域内展示的 `LoginView`。这消除了窗口管理的复杂性，并提供了更流畅的用户体验。
2.  **统一的宿主框架**：Desktop Shell 作为唯一的主窗口，负责加载 Prism 模块、管理 Region 导航以及处理全局生命周期事件。
3.  **导航一致性**：所有业务模块（如医案、患者、药材）均通过 NavigationCoordinator 在主窗口的指定 Region 中进行加载和切换。

## 优势

*   **状态管理简化**：无需处理多个窗口间的焦点切换、Z轴顺序及数据同步问题。
*   **用户体验连贯**：避免了窗口弹出带来的视觉中断，保持了界面布局的一致性。
*   **资源开销降低**：减少了操作系统层面的窗口句柄消耗。

## 技术债务与清理

在架构迁移过程中，遗留了部分多窗口模式的代码痕迹。根据 [前端死代码分析清单](../../06-operations/dead-code-analysis-frontend.md)，`LoginWindow.xaml` 及其后台代码已确认为完全死代码（0 引用），应予以删除以净化代码库。

## 相关概念

*   Desktop Shell：单窗口架构的物理载体。
*   [MVVM Prism](18-mvvm-prism.md)：支持 Region 导航的技术框架。
*   NavigationCoordinator：实现单窗口内视图切换的核心协调器。
