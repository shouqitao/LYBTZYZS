---
type: module
title: 验方管理模块
tags: [module, formula, prescription]
created: 2026-06-10
updated: 2026-06-12
source: docs/02-requirements/06-formulas.md
---

## 概述

验方（经验方）模块管理中医师的常用方剂模板。验方由多味药材组成，可直接导入处方，加速开方流程。本模块的核心特色是**延迟绑定**——导入的药材可先以文字形式保留，管理员后续再逐一绑定到系统药材库。

## 实体结构

### Formula（验方主体）

继承 `BaseEntity`：

| 字段 | 类型 | 说明 |
|------|------|------|
| `Name` | string(200) | 验方名称 |
| `Category` | string(50)? | 分类 |
| `Effect` | string(500)? | 功效 |
| `Indication` | string(1000)? | 主治 |
| `Usage` | string(500)? | 用法 |
| `Property` | string(300)? | 性味归经 |
| `Remark` | string(500)? | 备注 |
| `Status` | CommonStatus | 启用/禁用 |
| `ValidationStatus` | FormulaValidationStatus | Draft/Validated |
| `FormulaType` | FormulaType | Classic/Experience (默认 Experience) |
| `IsShared` | bool | 共享标记 |
| `UserId` | Guid? | 创建者 |
| `Herbs` | ICollection\<FormulaHerbItem\> | 药材组成（子集合） |

### FormulaHerbItem（药材项，延迟绑定）

| 字段 | 类型 | 说明 |
|------|------|------|
| `HerbId` | Guid? | **Nullable** — 延迟绑定核心 |
| `HerbName` | string(100) | 显示名称 |
| `OriginalHerbName` | string(100)? | 导入原始名称 |
| `IsValidated` | bool | 是否已绑定系统药材 |
| `Dosage` | int | 剂量 |
| `Unit` | string(16) | 单位 (g/ml) |
| `Usage` | string(200)? | 单味用法 |
| `ProcessingMethod` | string(100)? | 炮制方法 |
| `DecocteMethod` | DecocteMethod | 煎煮方式 |

## 延迟绑定机制

```
导入/创建 → HerbId = null, IsValidated = false (Draft)
         ↓
  自动匹配 (名称/拼音 → IHerbCrossModuleService)
  ├── 匹配成功 → HerbId 填充, IsValidated = true
  └── 匹配失败 → HerbId = null, 等待管理员手动绑定
         ↓
  管理员调用 POST /formulas/{id}/herbs/{itemId}/validate
         ↓
  全部药材绑定完成 → ValidationStatus 自动转为 Validated
```

## 验证工作流

```
Draft ──(全部药材绑定)──→ Validated
  │                        │
  │                        ├── Enabled + Validated → 可导入处方
  │                        └── Disabled → 不可导入
  └── 始终不可导入处方
```

**双门控 (MC-D08)**: 处方导入要求 `ValidationStatus == Validated` **且** `Status == Enabled`。

## 核心能力

| 能力 | 说明 |
|------|------|
| 验方 CRUD | 创建/查看/更新/软删除/恢复 |
| 延迟绑定 | 导入时自动匹配，失败项等管理员手动绑定 |
| 验证管理 | 逐项绑定药材，全部完成后自动转 Validated |
| 批量操作 | 导入/导出、批量删除、批量启禁用 |
| 处方导入 | Validated+Enabled 验方可直接导入处方（复制药材列表） |
| 无定价 | 验方不含价格，导入处方时从药材库实时获取 (FORM-D02) |

## API 端点 (15个)

路由前缀 `/api/v1/formulas`，认证: `DoctorOrAdmin`

| 方法 | 端点 | 说明 |
|------|------|------|
| GET | `/formulas` | 分页列表 |
| GET | `/formulas/{id}` | 详情 (含药材) |
| POST | `/formulas` | 创建 |
| PUT | `/formulas/{id}` | 更新 (整体替换 Herbs) |
| DELETE | `/formulas/{id}` | 软删除 |
| POST | `/formulas/batch-import` | JSON 批量导入 |
| GET | `/formulas/export` | Excel 导出 |
| GET | `/formulas/import-template` | 下载模板 (AllowAnonymous) |
| GET | `/formulas/pending-validation` | 待验证列表 |
| POST | `/formulas/{id}/herbs/{itemId}/validate` | 绑定单味药材 |
| POST | `/formulas/{id}/toggle-status` | 启禁用 |
| POST | `/formulas/{id}/restore` | 恢复 |
| POST | `/formulas/batch-delete` | 批量删除 |
| POST | `/formulas/batch-enable` | 批量启用 |
| POST | `/formulas/batch-disable` | 批量禁用 |

## 服务端架构

```
FormulasController (DoctorOrAdmin)
    │
    ├── IFormulaService (12 方法)
    │   ├── CRUD + Search + ValidateHerb + Pending
    │   ├── Toggle/Restore/BatchDelete/BatchUpdateStatus
    │   └── 委托 → IFormulaImportExportService (3 方法)
    │
    └── IFormulaRepository → FormulaRepository
        └── GetByIdWithHerbsAsync / GetPagedWithDetailsAsync
```

Mapper: `FormulaMapper` (Mapperly)。注意 `IsShared ↔ !IsPersonal` 互逆映射需手动处理。

## 验方导入处方流程

```
医案处方编辑 → 打开 FormulaImportDialog
  → IFormulaSearchProvider (跨模块接口)
  → 过滤: Validated + Enabled (MC-D08)
  → 医生选择验方 → 预览药材组成
  → 复制到处方 (DialogParameters)
  → 处方记录来源验方名 (ReferencedFormulas)
  → 单价从药材库实时获取 (FORM-D02)
```

## 关键业务规则

| 规则 | 说明 |
|------|------|
| 至少1味药材 | 创建/更新时验证 |
| 整体替换 Herbs | 更新使用 Clear()+Add()，无部分更新 (Design Decision 002) |
| 所有权校验 | Doctor 仅操作自己的验方，Admin 可操作所有 |
| 无定价 | 价格在处方创建时从药材库解析 |
| FormulaType 默认 Experience | DTO 无此字段，服务端设置 |
| 草稿不可导入处方 | 需 Validated + Enabled 双门控 |

## 跨模块关系

| 方向 | 模块 | 接口 | 用途 |
|------|------|------|------|
| 依赖 | Herbs | `IHerbCrossModuleService` | 药材自动匹配 + 验证绑定 + 价格获取 |
| 桌面依赖 | Herbs | `IHerbSearchProvider` | 药材搜索 UI |
| 被依赖 | MedicalCase | `IFormulaSearchProvider` | 处方导入验方 |
| 被依赖 | Sync | `SyncRepository` | 数据同步 |

## 错误码

| 范围 | 类别 | 数量 |
|------|------|------|
| ERR-601xx | 核心 | 6 |
| ERR-602xx | 药材验证 | 6 |
| ERR-603xx | 批量操作 | 5 |

## 相关链接

- [`docs/04-api-reference/05-formulas.md`](../../04-api-reference/05-formulas.md) — API 端点详细文档
- [`docs/07-concepts/28-formula-validation-workflow.md`](../formula-validation-workflow.md) — 验证工作流
