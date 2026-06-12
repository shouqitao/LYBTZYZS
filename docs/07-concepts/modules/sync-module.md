---
type: module
title: 数据同步模块
tags: [module, sync, dual-mode]
created: 2026-06-10
updated: 2026-06-12
source: docs/02-requirements/sync.md
---

## 概述

同步模块实现 Desktop 本地数据库与远程 SQL Server 之间的双向数据同步。它是双模式架构的关键闭环——没有同步，本地创建的数据将停留在设备上，无法汇总到服务器。

## 同步的实体类型

| 实体 | 同步粒度 | 状态 |
|------|---------|------|
| Herb | 单实体 | ✅ 已实现 |
| Patient | 单实体 | ✅ 已实现 |
| Formula | 实体 + 子集合 (FormulaHerbItem) | ✅ 已实现 |
| MedicalCase | 完整聚合 (Consultation + Prescription + Items) | ⏳ 设计完成，实现待定 |

## 同步流程 (5阶段)

```
Metadata → Compare → Upload → Download → Delete
  (元数据)   (对比)    (上传)    (下载)    (删除)
```

### 阶段 1: 元数据获取

`GET /sync/metadata?entityType=Herb` → 返回 `SyncMetadataDto[]`
- 包含 EntityId, Checksum (SHA256), LastModifiedAt, IsDeleted
- **包含软删除记录**（使用 `IgnoreQueryFilters`），确保删除操作能同步

### 阶段 2: 对比 (差异检测)

`POST /sync/compare` → 返回 `SyncCompareResultDto`

| 差异类型 | 含义 | 处理 |
|---------|------|------|
| LocalOnly | 仅本地有 | 需上传 |
| ServerOnly | 仅服务器有 | 需下载 |
| Modified | 双方都有但校验和不同 | 冲突，需人工处理 |
| Identical | 校验和相同 | 跳过（不返回） |

### 阶段 3: 上传

`POST /sync/upload` → 接收 `SyncUploadInputDto`
- 每个实体反序列化后判断：不存在则添加、存在且 `OverwriteConflicts=true` 则覆盖
- Formula 特殊处理：子集合整体替换 (RemoveRange + Add)
- 单次 `SaveChangesAsync` 提交所有上传

### 阶段 4: 下载

`POST /sync/download` → 返回 JSON 序列化的实体数据
- Formula 下载包含 `.Herbs` 子集合

### 阶段 5: 删除

`POST /sync/delete` → 软删除 + 引用完整性检查
- Herb: 检查处方引用
- Patient: 检查医案引用
- Formula/MedicalCase: 直接软删除

## 校验和机制

**算法**: SHA256(JSON序列化(业务字段))
- 排除审计字段 (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy, RowVersion)
- **关键约束**: 服务端和桌面端 `ChecksumHelper` 实现必须保持一致

| 实体 | 包含字段 |
|------|---------|
| Herb | Name, PinYinCode, Category, Unit, Price, Effect, ... |
| Patient | Name, IdNumber, PhoneNumber, Address, AllergyHistory, ... |
| Formula | 验方字段 + Herbs[] (按 HerbId 排序) |
| MedicalCase | 医案字段 + Consultation + Prescription + Items[] |

## 冲突解决策略

**检测-报告-人工处理**，不自动合并。

1. 对比阶段检测到 Modified 差异
2. Desktop 显示 `SyncConflictDialog`，逐项展示冲突
3. 用户选择：
   - **Use Local** — 上传本地版本，覆盖服务器
   - **Use Server** — 下载服务器版本，覆盖本地
   - **Skip** — 双方保持各自版本
4. 批量操作："Use All Local" / "Use All Server"

## API 端点 (6个)

| 方法 | 端点 | 说明 |
|------|------|------|
| GET | `/api/v1/sync/entity-types` | 支持的实体类型 |
| GET | `/api/v1/sync/metadata?entityType=` | 获取元数据列表 |
| POST | `/api/v1/sync/compare` | 差异对比 |
| POST | `/api/v1/sync/upload` | 上传实体数据 |
| POST | `/api/v1/sync/download` | 下载实体数据 |
| POST | `/api/v1/sync/delete` | 软删除 |

认证: `DoctorOrAdmin` | 错误码: 7xxxx 范围

## 桌面架构

```
SyncView
  │
  SyncViewModel (阶段驱动的状态机)
  │
  ├── ISyncService (LocalData 实现)
  ├── IDialogService → SyncConflictDialog
  │
  ├── SyncErrorClassifier (异常→错误分类)
  ├── SyncResolutionBuilder (用户选择→DTO)
  └── SyncItemViewModelFactory
```

**状态机**: `Idle → CheckingDifferences → ReviewingDifferences → ExecutingSync → Completed` (或 `Failed`)

## 关键业务规则

| 规则 | 说明 |
|------|------|
| 依赖排序 | MedicalCase 同步时强制: Herb → Patient → MedicalCase |
| 患者去重 | 上传时按 IdCardNumber 匹配，如已存在则重映射 GUID |
| 编号重分配 | CaseNumber/PrescriptionNumber 本地生成，上传时服务器重新分配 |
| 打印字段不同步 | IsPrinted/PrintCount/PrintVersion/PrintLog 仅本地 |
| 幂等性 | SHA256 校验和确保已同步数据自动跳过 |
| 模式切换守卫 | 本地有 Active/Suspended 医案时禁止切换到远程模式 |
| MedicalCase 仅 Completed | Active/Suspended 医案不可同步 |

## 相关链接

- [`docs/04-api-reference/sync.md`](../../04-api-reference/sync.md) — API 端点详细文档
- [`docs/07-concepts/sync-conflict-resolution.md`](../sync-conflict-resolution.md) — 冲突解决详情
