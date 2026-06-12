---
type: module
title: 药材管理模块
tags: [module, herb, prescription]
created: 2026-06-10
updated: 2026-06-12
source: docs/02-requirements/herbs.md
---

## 概述

药材管理模块负责中药材库的完整生命周期管理，包括基本信息维护、分类、价格管理、启用/禁用状态控制、批量导入导出及引用安全检查。该模块是处方开方的基础依赖——没有药材数据，开方功能无法使用，因此是系统上线的前置条件。

## 实体字段

`Herb` 继承 `BaseEntity`：

| 字段 | 类型 | 说明 |
|------|------|------|
| `Name` | string(100) | 药材名称，必填，唯一 |
| `PinYinCode` | string(50)? | 拼音码，自动生成 |
| `Category` | string(50)? | 分类（补血药、补气药等） |
| `Properties` | string(100)? | 性味（甘、温） |
| `Origin` | string(100)? | 产地 |
| `Spec` | string(100)? | 规格 |
| `Unit` | string(10) | 单位，默认"克" |
| `Price` | decimal(18,2) | 单价 |
| `CostPrice` | decimal(18,2)? | 成本价 |
| `Effect` | string(500)? | 功效说明 |
| `Usage` | string(500)? | 用法用量 |
| `Status` | CommonStatus | 启用/禁用 |

## 核心能力

| 能力 | 说明 |
|------|------|
| 药材 CRUD | 创建/查看/更新/软删除/恢复，自动生成拼音码 |
| 状态管理 | 启用/禁用切换（单个 + 批量），禁用药材开方时不可选 |
| 批量操作 | Excel 导入、JSON 批量导入（最多 10000 条）、Excel/JSON 导出、批量删除 |
| 引用安全 | 删除前检查 PrescriptionItem / FormulaItem 引用，有引用则禁止删除并建议禁用 |
| 内存缓存 | Desktop 全量预加载到内存（IHerbCacheService），开方时 0ms 纯内存过滤 |
| 价格快照 | 处方创建时记录药材单价，历史处方金额不受后续改价影响 |

## API 端点 (18个)

路由前缀 `/api/v1/herbs`，认证: `DoctorOrAdmin`

| 方法 | 端点 | 说明 |
|------|------|------|
| GET | `/herbs` | 分页列表 (OutputCache) |
| GET | `/herbs/{id}` | 详情 |
| POST | `/herbs` | 创建 |
| PUT | `/herbs/{id}` | 更新 |
| DELETE | `/herbs/{id}` | 软删除 |
| GET | `/herbs/search` | 搜索（名称/拼音） |
| POST | `/herbs/import` | Excel 导入 |
| GET | `/herbs/export` | Excel 导出 |
| GET | `/herbs/import-template` | 下载模板 (AllowAnonymous) |
| POST | `/herbs/batch-import` | JSON 批量导入 |
| GET | `/herbs/export-all` | JSON 全量导出 |
| GET | `/herbs/{id}/check-reference` | 单个引用检查 |
| POST | `/herbs/batch-check-reference` | 批量引用检查 (max 100) |
| POST | `/herbs/{id}/toggle-status` | 启禁用 |
| POST | `/herbs/{id}/restore` | 恢复 |
| POST | `/herbs/batch-enable` | 批量启用 |
| POST | `/herbs/batch-disable` | 批量禁用 |
| POST | `/herbs/batch-delete` | 批量删除 |

## 服务端架构

```
HerbsController (DoctorOrAdmin)
    │
    ├── IHerbService (17 方法)
    │   ├── CRUD + Search + Toggle/Restore
    │   └── 委托 → IHerbImportExportService (5 方法)
    │
    ├── IHerbRepository → HerbRepository (internal)
    │   └── BaseRepository<Herb> → AppDbContext
    │
    └── IHerbReferenceRepository → HerbReferenceRepository
        └── 跨聚合查询 (PrescriptionItems + FormulaHerbItems)
```

Mapper: `HerbMapper` (Mapperly 编译时源生成)

## 批量导入管道

两条路径共存：

| 路径 | 说明 | 去重 |
|------|------|------|
| **Excel (服务端)** | EPPlus 解析 .xlsx，逐行验证导入 | 无 |
| **DTO (客户端解析)** | Desktop 解析后发送 `List<HerbInputDto>` | Skip/Update/Error 三种策略 |

DTO 路径细节：
- 上限 10000 条 (BR-006)
- 每项自动生成 PinYinCode → `ExistsByNameAsync` 去重检查 → 按策略处理
- 返回 `HerbBatchImportResultDto`（successCount/failureCount/skippedCount）

## 缓存策略

**Desktop IHerbCacheService**：
- 三级索引：`Dictionary<Guid>` (ID查找) + `Dictionary<string>` (拼音前缀) + `Dictionary<string>` (分类)
- 启动/登录时全量预加载，开方时纯内存搜索
- 增量更新：单条 CRUD 同步更新内存
- 全量重载触发：批量导入完成、模式切换、同步完成、空闲>30分钟

**服务端**：`ICacheInvalidationService` 标签驱逐 `"herbs"`

## 角色权限

| 角色 | 权限 |
|------|------|
| SuperAdmin | 全部 CRUD |
| Admin | 全部 CRUD |
| Doctor | 创建 + 编辑/删除/启禁用自己创建的；查看全部 |
| Receptionist | 无权限 |

## 关键业务规则

1. **引用保护**: 删除前检查 PrescriptionItem + FormulaHerbItem，有引用则拒绝，建议禁用
2. **软删除 + 恢复**: IsDeleted=true，Restore 使用 `IgnoreQueryFilters()`
3. **所有权校验**: Doctor 仅操作自己创建的药材
4. **价格快照**: 处方创建时记录 UnitPrice，后续改价不影响历史处方
5. **并发策略**: Last-Write-Wins，不强制 RowVersion
6. **批量限制**: BatchImport max 10000, BatchCheckReference max 100
7. **错误隔离**: 批量操作逐项 try-catch，单项失败不中止整批

## 跨模块关系

| 方向 | 模块 | 接口 | 用途 |
|------|------|------|------|
| 被依赖 | Formula | `IHerbCrossModuleService` | 验方药材绑定 + 导入自动匹配 |
| 被依赖 | MedicalCase | `PrescriptionItem.HerbId` | 处方引用 |
| 被依赖 | Sync | `SyncRepository` | 数据同步 |
| 桌面被依赖 | Formula/MedicalCase | `IHerbSearchProvider` | 跨模块药材搜索 |

## 错误码

| 范围 | 类别 | 数量 |
|------|------|------|
| ERR-501xx | 核心 (NotFound/Validation/Permission) | 5 |
| ERR-502xx | 批量操作 (Empty/Exceeded/ItemError) | 6 |
| ERR-503xx | Excel导入 (FileEmpty/Format/Size) | 5 |

## 相关链接

- [`docs/04-api-reference/herbs.md`](../../04-api-reference/herbs.md) — API 端点详细文档
- [`docs/07-concepts/herb-cache-strategy.md`](../herb-cache-strategy.md) — 缓存策略详情
- [`docs/07-concepts/pinyin-search-implementation.md`](../pinyin-search-implementation.md) — 拼音搜索
