# OpenSpec Proposal: document-client-api-layer

## 元数据

- **提案ID**: document-client-api-layer
- **创建日期**: 2025-11-30
- **状态**: Completed
- **关联**: OpenSpec refactor-webapi-layer

## Why

WebAPI层重构(refactor-webapi-layer)完成后，需要配套文档记录Client端API对接层的设计规范，确保前后端API契约一致性，并为未来开发提供指导。

## What Changes

1. **创建client-api-conventions spec** - 记录Client端Refit接口设计规范
2. **验证现有实现** - 确认所有Refit接口符合规范
3. **补充代码注释** - 在关键接口添加规范说明

## 问题陈述

### 背景

WebAPI重构确定了以下关键决策：
- 批量删除使用Client端循环模式，而非Server端batch endpoint
- 状态变更使用标准Update API，而非专用ToggleStatus端点
- MedicalCase作为DDD聚合根，子资源通过聚合根路径访问

### 当前状态

Client端API层分析结果显示实现已符合规范：

| Refit接口 | 状态 | 说明 |
|----------|------|------|
| IUserApi | 符合 | 无BatchDelete方法 |
| IHerbApi | 符合 | 无BatchDelete方法 |
| IFormulaApi | 符合 | 无BatchDelete方法 |
| IMedicalCaseApi | 符合 | 使用聚合根模式 |

### 需要做的

创建规范文档，将隐式的设计决策显式化，便于：
- 新开发者理解API设计原则
- Code Review时有据可依
- 未来功能开发保持一致性

## 受影响的组件

### 文档
- `openspec/specs/client-api-conventions/spec.md` - 新增

### 代码（轻微）
- `src/Client/Desktop/Core/LYBT.Desktop.Contracts/Api/*.cs` - 可选添加规范注释

## 成功标准

1. client-api-conventions spec创建完成
2. spec与webapi-cleanup形成互补
3. 现有Refit接口验证通过

## 风险评估

- **风险**: 低 - 纯文档工作，无代码变更风险

---

**提案状态**: Draft
