---
type: module
title: 验方管理模块 (Formula Module)
tags: [module, formula, herb, medical-case]
created: 2026-06-10
updated: 2026-06-10
source: docs/02-requirements/formulas.md
---

## 概述

验方管理模块负责中医诊所经验方（验方）的数字化存储、验证和共享。支持延迟绑定机制以承接旧系统迁移数据，通过验证工作流确保药材数据质量，并提供团队内优秀验方的流通机制。该模块为医生开方提供快速复用能力，显著提升诊疗效率。

## 核心能力

| 能力 | 说明 |
|------|------|
| **验方 CRUD** | 创建、查看、编辑、删除经验方模板，包含完整的药材组成管理 |
| **延迟绑定** | 导入时药材名称可暂不关联系统药材库（`HerbId` 可空），后续手动验证绑定 `OriginalHerbName` |
| **验证工作流** | Draft（未验证）→ Validated（全部药材已绑定），仅 Validated 验方可用于处方导入 |
| **共享机制** | 验方标记为共享后，其他医生可查看（只读），促进临床经验在团队内流通 |
| **批量操作** | 支持 JSON/Excel 批量导入、批量导出 Excel、批量删除/启用/禁用 |
| **双模式支持** | 远程（HTTP API）+ 本地（SQLite DataSource）两种部署模式 |

## 角色权限

| 角色 | 权限范围 |
|------|---------|
| **SuperAdmin** | CRUD 全部验方 |
| **Admin** | CRUD 全部验方 |
| **Doctor** | CRUD 自己创建的验方 + 查看共享验方（只读） |
| **Receptionist** | 无权限 |

> 端点受 `DoctorOrAdmin` 策略保护。资源级权限：Doctor 只能查看自己创建的或 `IsShared=true` 的验方。

## 关键业务规则

### 验方生命周期

```
创建/导入 → Draft (药材未验证)
         → 逐个验证药材绑定
         → 全部验证完成 → Validated
         → 启用 (Enabled) + 已验证 (Validated) → 可用于处方导入
         → 禁用 (Disabled) → 处方导入不可见
         → 软删除 → 可恢复
```

### 数据模型要点

**Formula（验方实体）**：
- `ValidationStatus`: 验证状态（Draft/Validated），默认 Draft
- `IsShared`: 是否共享，默认 false
- `UserId`: 创建用户 ID，用于资源级权限控制
- `Category` / `FormulaType`: 方剂分类和类型（Classic/Experience）

**FormulaHerbItem（验方药材项）**：
- `HerbId`: 可空外键，支持延迟绑定
- `OriginalHerbName`: 原始药材名称（导入时保留）
- `IsValidated`: 是否已验证绑定
- `DecocteMethod`: 煎法枚举，与医案处方模块保持一致

### 验证规则

- 验方必须至少包含一个药材项
- 仅 `ValidationStatus=Validated` 且 `Status=Enabled` 的验方可在处方导入时可见
- 药材验证需逐项绑定到系统药材库的 `Herb` 实体
- 批量导入时需处理重名验方的合并或跳过策略

## 相关链接

- [[formula]] - 验方实体定义和数据字典
- [[herb]] - 药材库模块，验方延迟绑定的目标数据源
- [[prescription]] - 处方模块，验方导入的目标使用场景
- [[medical-case]] - 医案模块，验方通过处方导入参与诊疗流程
