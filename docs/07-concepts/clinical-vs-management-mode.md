---
type: concept
title: 临床与管理模式对比
tags: [ui-ux, workspace, medical-case, design-pattern]
related: [workspace-modes, clinical-ui-layout-strategy, medical-case-module]
created: 2026-06-10
updated: 2026-06-10
sources: ["docs/03-architecture/medicalcase-workspace-current-state.md"]
---
# 临床与管理模式对比

## 定义

**临床模式 (Clinical Mode)** 与 **管理模式 (Management Mode)** 是医案工作台针对不同用户场景设计的两种交互上下文。
*   **临床模式**: 面向医生在接诊过程中的快速操作，强调效率、简洁和流程引导。
*   **管理模式**: 面向管理员或医生事后查阅、修正医案，强调信息完整性和审计追踪。

## 模式对比表

| 维度 | 临床模式 (Clinical) | 管理模式 (Management) |
|------|---------------------|-----------------------|
| **入口** | 待诊队列 / 患者选择页 | 医案管理列表页 |
| **默认状态** | 编辑中 (Editing) | 只读 (ReadOnly) |
| **UI 布局** | Compact (紧凑)，隐藏非核心字段 | Full (完整)，显示所有字段及审计信息 |
| **底部按钮** | [挂起] [打印] [完成] | [编辑] [打印] |
| **离开行为** | 触发 REG-BR-002 (挂起/完成/取消) | 触发 REG-BR-002 (保存/放弃/取消) |
| **数据校验** | 强制 REG-BR-003 完成前校验 | 无强制校验，允许部分保存 |
| **滚动支持** | 需支持 ScrollViewer (当前缺失) | 支持 ScrollViewer |
| **TabIndex** | 优化键盘导航 (8-13) | 标准导航 (1-7) |

## 实现现状与差距

根据 [医案工作台当前状态审计](../../03-architecture/medicalcase-workspace-current-state.md)，当前实现存在以下差距：

1.  **UI 未完全区分**: `MedicalCaseEditControl` 虽然支持 `WorkspaceMode` 枚举，但 XAML 中未完全根据模式隐藏/显示相应控件（如审计字段在 Compact 模式下仍占用空间或未正确隐藏）。
2.  **按钮逻辑缺失**: 临床模式所需的底部按钮组（挂起/完成）未在 UI 中集成，导致医生无法通过标准流程结束接诊。
3.  **验证逻辑缺失**: 临床模式要求的 REG-BR-003 完成前校验未在客户端实现，导致医生可能在数据不全时尝试完成医案，仅依赖服务端报错。

## 设计原则

1.  **效率优先 (Clinical)**: 减少鼠标点击次数，提供快捷键支持，默认聚焦于核心诊疗字段（现病史、诊断、处方）。
2.  **完整性优先 (Management)**: 展示所有元数据（创建时间、更新人、版本号），支持历史版本对比和详细审计。
3.  **状态驱动**: 按钮的可用性与医案状态（Active/Suspended/Completed）严格绑定，防止非法操作。

## 相关链接

*   [[workspace-modes]]
*   [[clinical-ui-layout-strategy]]
*   [[medical-case-module]]
*   [[business-rules]]
