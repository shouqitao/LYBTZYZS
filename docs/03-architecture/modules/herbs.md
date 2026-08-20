# Herbs 模块设计

> 复杂度: 中低 | 状态: 草稿

## 概述

Herbs 管理中药药材信息，支持拼音搜索、引用检查、批量导入/导出。

**职责**: 药材 CRUD/搜索/批量操作、拼音自动生成、引用检查、Excel 导入导出。

**依赖**: 上游无，下游 MedicalCase（处方价格）、Formula（验方组成）。

## API 端点

| HTTP | 路由 | 权限 |
|------|------|------|
| GET | `/api/v1/herbs` | Doctor/Admin |
| POST | `/api/v1/herbs` | Admin+ |
| PUT | `/api/v1/herbs/{id}` | Admin+ |
| DELETE | `/api/v1/herbs/{id}` | Admin+ |
| POST | `/api/v1/herbs/batch-import` | Admin+ |
| GET | `/api/v1/herbs/{id}/check-reference` | Doctor/Admin |

## 独有设计

### 引用检查
`IHerbReferenceRepository` 检查处方/验方引用数。删除前检查，有引用返回警告。

### 批量导入
支持 Skip/Update/Error 三种重复策略（`DuplicateStrategy`）。

## 业务规则

| 规则 | 描述 |
|------|------|
| 拼音自动生成 | Name 变更时自动更新 PinYinCode |
| 名称唯一 | 创建/更新时校验 |
| 引用检查 | 删除前检查是否被处方/验方引用 |
| 批量导入 | 三种重复策略 |

## 已知问题

- `IHerbReferenceRepository` 已注册但 Service 未注入/未使用
- 删除/批量删除均不检查引用 → 被处方引用的药材可静默软删
- `IHerbImportExportService` 在 PRD 中引用，仓库中无此文件
