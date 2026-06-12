---
type: concept
title: 处方完整性检查器
created: 2026-06-10
updated: 2026-06-10
tags: [validation, business-rule, ui-feedback]
related: [business-rules, prescription-decision-gate, medical-case-module]
sources: ["docs/03-architecture/medicalcase-workspace-ui-optimization-plan.md"]
---

# 处方完整性检查器

## 定义

**处方完整性检查器 (Prescription Completeness Checker)** 是一个前端逻辑组件，用于在医生完成看诊前，实时评估当前医案是否满足业务规则 BR-003（完成校验）的要求，并通过可视化的方式（如绿/红状态指示）提供即时反馈。

## 核心功能

该检查器监听以下关键状态的变化，并动态更新检查结果列表：

1. **中医诊断**：检查 `Consultation.TcmDiagnosis` 是否非空。
2. **处方需求标记**：检查 `NeedsPrescription` 是否已明确选择（非 Null）。
3. **处方药材**：若标记为“需要处方”，检查药材列表是否非空。
4. **处方配置**：检查 `DosageCount`（剂数）和 `Usage`（用法）是否已填写。

## 交互反馈

*   **通过状态 (Green)**：所有校验项满足，显示“可以完成看诊”。
*   **失败状态 (Red)**：列出具体缺失项（如“✗ 中医诊断未填写”），并禁用或警告“完成看诊”按钮。

## 技术实现

*   **逻辑层**：实现 `PrescriptionCompletenessChecker` 类，订阅 ViewModel 中的属性变更事件。
*   **UI 层**：在处方区底部绑定检查结果集合，使用 DataTemplate 根据严重程度（Error/Warning）渲染不同的图标和颜色。

## 业务价值

通过将原本在提交时才触发的后端校验前置到 UI 层，显著降低了医生因遗漏必填项而导致的操作挫败感，提升了诊疗流程的流畅度。

## 相关概念

- [业务规则](../01-product/06-clinical-workflow.md)：直接对应 BR-003 完成校验规则。
- [处方决策门](28-formula-validation-workflow.md)：依赖处方决策状态作为校验前提。
- completeness-check-visualization (规划中)：该检查器的 UI 表现形式。
