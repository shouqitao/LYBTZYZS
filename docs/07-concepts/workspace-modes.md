---
type: concept
title: 工作区模式
created: 2026-06-10
updated: 2026-06-10
tags: [ui, workflow, desktop-client, user-experience]
related: [clinical-workflow, user, mvvm-prism]
sources: ["docs/01-product/user-roles.md"]
---

# 工作区模式

工作区模式是Desktop客户端根据用户角色提供的不同用户界面和工作流程入口，旨在优化不同角色用户的操作效率和体验。

## 模式定义

系统定义了两种主要工作区：

1.  **临床工作区（Clinical）**：面向医生（Doctor）角色。核心流程为：患者选择 → 创建医案 → 诊断 → 开方 → 完成。
2.  **管理工作区（Management）**：面向管理员（Admin）角色。核心流程为：医案列表 → 查看/编辑医案 → 审计日志。

## 行为差异

两种工作区在交互行为上存在显著差异：

| 行为 | 临床工作区 | 管理工作区 |
|------|------------|------------|
| 编辑状态 | 默认处于编辑（Editing）状态 | 默认处于只读（ReadOnly）状态，需手动切换 |
| 返回导航 | 返回患者选择页 | 返回医案列表页 |
| 保存后状态 | 切换到只读状态，留在当前界面 | 切换到只读状态，留在当前界面 |
| 未保存修改处理 | 自动暂存 | 弹窗确认（保存/放弃/取消） |

## 设计意义

工作区模式通过角色定制化界面，减少了无关信息的干扰，引导用户专注于其核心任务。临床工作区强调诊疗流程的连贯性，管理工作区则侧重于数据的审查和管理。

> **注意**: 工作区模式定义角色级别的 UI 入口，而[临床与管理模式对比](clinical-vs-management-mode.md)定义同一视图内的交互行为差异（编辑/只读、按钮布局等），两者互补。

## 相关概念

- [clinical-workflow](clinical-workflow.md)：工作区模式是临床工作流在前端的具体实现。
- [clinical-vs-management-mode](clinical-vs-management-mode.md)：同一视图内的交互行为差异。
- user：工作区根据用户角色进行分配。
- [mvvm-prism](mvvm-prism.md)：工作区的实现依赖于WPF MVVM和Prism的区域导航机制。