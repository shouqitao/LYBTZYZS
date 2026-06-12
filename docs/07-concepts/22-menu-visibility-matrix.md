---
type: concept
title: 菜单可见性矩阵
tags: [security, rbac, ui]
related: [desktop-shell, menu-manager, workspace-modes, user-personas]
created: 2026-06-10
updated: 2026-06-10
sources: ["docs/02-requirements/12-desktop-shell.md"]
---

# 菜单可见性矩阵

**菜单可见性矩阵** 是一套根据用户角色（SuperAdmin, Admin, Doctor, Receptionist）动态显示或隐藏菜单项的规则集。它是 Desktop Shell 中 MenuManager 的核心配置依据，旨在防止越权操作并简化不同角色的工作界面。

## 矩阵定义

| 菜单项 | SuperAdmin | Admin | Doctor | Receptionist |
| :--- | :---: | :---: | :---: | :---: |
| **新建患者** | ✅ | ✅ | ✅ | ✅ |
| **新建医案** | ❌ | ❌ | ✅ | ❌ |
| **打印** | ✅ | ✅ | ✅ | ❌ |
| **患者管理** | ✅ | ✅ | ✅ | ✅ |
| **医案管理** | ✅ | ✅ | ✅ | ❌ |
| **验方管理** | ✅ | ✅ | ✅ | ❌ |
| **药材管理** | ✅ | ✅ | ❌ | ❌ |
| **用户管理** | ✅ | ✅ | ❌ | ❌ |
| **数据同步** | ✅ | ✅ | ✅ | ❌ |
| **系统设置** | ✅ | ❌ | ❌ | ❌ |
| **系统健康** | ✅ | ✅ | ❌ | ❌ |

> **注**：✅ 表示可见，❌ 表示隐藏。

## 实现机制

1.  **角色识别**：用户登录成功后，Shell 获取当前用户的角色标识。
2.  **命令绑定**：`MenuManager` 根据角色加载对应的命令集合。
3.  **UI 更新**：通过数据绑定或代码后台，将不可见的菜单项 `Visibility` 设置为 `Collapsed`。
4.  **快捷键保护**：即使菜单隐藏，全局快捷键（如 Ctrl+N）也会根据角色权限进行拦截或放行。

## 本地模式差异

在 [本地模式](dual-mode-architecture.md) 下，部分依赖服务端功能的菜单项（如某些高级同步选项或云端备份）可能会被额外禁用。具体的本地模式禁用列表仍在定义中（见 Open Question OQ-SHELL-03）。

## 价值

*   **安全性**：从 UI 层面阻断非授权角色的操作路径。
*   **易用性**：减少前台接待和医生的认知负荷，仅展示与其工作相关的功能。
*   **合规性**：确保系统操作符合诊所的管理规范。

## 相关链接

*   desktop-shell（规划中）
*   menu-manager（规划中）
*   [工作区模式](workspace-modes.md)