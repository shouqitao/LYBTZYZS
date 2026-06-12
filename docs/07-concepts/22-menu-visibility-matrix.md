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

在 [本地模式](01-dual-mode-architecture.md) 下，部分依赖远程服务端的功能在菜单层额外禁用（灰显/不可点击，非隐藏）：

| 菜单项 | 远程模式 | 本地模式 | 原因 |
| :--- | :---: | :---: | :--- |
| **数据同步** → 上传 | ✅ | 🔒 | 需要远程 SQL Server |
| **数据同步** → 下载 | ✅ | 🔒 | 需要远程 SQL Server |
| **数据同步** → 冲突解决 | ✅ | 🔒 | 无远程数据源 |
| **系统健康** → 服务端详情 | ✅ | 🔒 | LocalWebAPI 健康检查仍可用，但服务端探活无意义 |
| **用户管理** → 密码重置邮件 | ✅ | 🔒 | 无邮件服务 |
| **打印日志查询** (远程) | ✅ | 🔒 | 远程 MedicalPrintLog 不存在 |

**本地模式仍可用**：所有 CRUD 操作（患者/药材/验方/医案/挂号）、本地打印、LocalWebAPI 健康检查、Diagnostics API。

**实现方式**：`MenuManager` 读取 `SwitchingApiClient.IsLocalMode`，对上表中的菜单项设置 `IsEnabled = false` + 显示提示"此功能需要连接远程服务器"。

## 价值

*   **安全性**：从 UI 层面阻断非授权角色的操作路径。
*   **易用性**：减少前台接待和医生的认知负荷，仅展示与其工作相关的功能。
*   **合规性**：确保系统操作符合诊所的管理规范。

## 相关链接

*   desktop-shell（规划中）
*   menu-manager（规划中）
*   [工作区模式](04-workspace-modes.md)