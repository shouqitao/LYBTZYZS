---
type: module
title: 患者管理模块
tags: [module, patient, sensitive-data]
created: 2026-06-10
updated: 2026-06-12
source: docs/02-requirements/patients.md
---

## 概述

患者管理模块维护诊所服务对象的个人信息和医疗背景（过敏史、病史）。患者是医案的必要参与者——没有患者就无法创建医案。本模块涉及敏感个人信息（身份证号、电话等），因此有专门的数据分类与保护机制。

## 实体字段

`Patient` 继承 `BaseEntity`，22个字段：

| 字段 | 类型 | 敏感级别 | 说明 |
|------|------|---------|------|
| `Name` | string(100) | L3 | 姓名，必填 |
| `PinYinCode` | string(50)? | — | 自动生成 |
| `Gender` | Gender enum | L3 | 性别 |
| `BirthDate` | DateTime? | L3 | 出生日期 |
| `IdNumber` | string(50)? | **L1** | 身份证号，唯一，`[SensitiveData(Partial)]` |
| `PhoneNumber` | string(20)? | **L1** | 电话，唯一，`[SensitiveData(Partial)]` |
| `Address` | string(256)? | L2 | 地址，`[SensitiveData(Default)]` |
| `AllergyHistory` | string(500)? | L2 | 过敏史，`[SensitiveData(Hash)]` |
| `MedicalHistory` | string(1000)? | L2 | 病史，`[SensitiveData(Hash)]` |
| `EmergencyContactPhone` | string? | **L1** | 紧急联系人电话，`[SensitiveData(Partial)]` |
| `EmergencyContactName` | string? | — | 紧急联系人 |
| `EmergencyContactRelation` | string? | — | 关系 |
| `MaritalStatus` | int | — | 婚姻状况 |
| `BloodType` | int | — | 血型 |
| `IdType` | int | — | 证件类型 |
| `Status` | CommonStatus | — | 启用/禁用 |
| `DisableReason` | string(128)? | — | 禁用原因 |
| `LastVisitTime` | DateTime? | — | 最后就诊（自动更新） |
| `VisitCount` | int | — | 就诊次数 |
| `Age` | int? | — | `[NotMapped]` 从 BirthDate 计算 |

## 核心能力

| 能力 | 说明 |
|------|------|
| 患者 CRUD | 创建/查看/更新/软删除/恢复，自动生成拼音码 |
| 状态管理 | 启用/禁用，禁用被拒绝如有 Draft/Active 医案 |
| 批量操作 | Excel 导入（max 1000 行）、Excel 导出（max 10000 行）、批量删除 |
| 引用安全 | 删除前检查 MedicalCase 引用 |
| 敏感数据 | `[SensitiveData]` 标注驱动日志脱敏（v1 仅日志，v2.0 计划 AES-256+DPAPI） |
| 身份证读卡 | Desktop 集成身份证读卡器硬件 |

## 敏感数据分级

| 级别 | 字段 | 日志脱敏 | v1 本地存储 | v2.0 计划 |
|------|------|---------|-----------|----------|
| **L1** | IdNumber, PhoneNumber, EmergencyContactPhone | 保留前3后4 | 明文 | AES-256 + DPAPI |
| **L2** | Address, AllergyHistory, MedicalHistory | 部分遮蔽/哈希 | 明文 | 明文 |
| **L3** | Name, Gender, BirthDate | 正常 | 明文 | 明文 |

## API 端点 (14个)

路由前缀 `/api/v1/patients`，认证: `DoctorOrAdmin`

| 方法 | 端点 | 认证 | 说明 |
|------|------|------|------|
| GET | `/patients` | DoctorOrAdmin | 分页列表 (OutputCache) |
| GET | `/patients/{id}` | DoctorOrAdmin | 详情 |
| POST | `/patients` | DoctorOrAdmin | 创建 |
| PUT | `/patients/{id}` | DoctorOrAdmin | 更新 |
| DELETE | `/patients/{id}` | DoctorOrAdmin | 软删除 |
| POST | `/patients/import` | DoctorOrAdmin | Excel 导入 |
| GET | `/patients/import-template` | AllowAnonymous | 下载模板 |
| GET | `/patients/export` | DoctorOrAdmin | Excel 导出 |
| POST | `/patients/{id}/restore` | DoctorOrAdmin | 恢复 |
| POST | `/patients/batch-delete` | DoctorOrAdmin | 批量删除 |
| POST | `/patients/{id}/toggle-status` | **AdminOnly** | 启禁用 |
| GET | `/patients/{id}/check-reference` | DoctorOrAdmin | 引用检查 |
| POST | `/patients/batch-check-reference` | DoctorOrAdmin | 批量引用检查 |
| GET | `/patients/statistics` | DoctorOrAdmin | 统计 |

## 服务端架构

```
PatientsController (DoctorOrAdmin)
    │
    ├── IPatientService (18 方法)
    │   ├── CRUD + Search + Toggle/Restore + Reference
    │   ├── Entity-direct-return (4 方法, 合并自 IPatientServiceOptimized)
    │   └── 委托 → IPatientImportExportService (3 方法)
    │
    ├── IPatientRepository → PatientRepository (internal)
    │   └── BaseRepository<Patient> → AppDbContext
    │
    └── IPatientCrossModuleService (Infrastructure)
        └── GetBasicInfo / Exists / CheckReference
```

Mapper: `PatientMapper` (Mapperly)。注意 `Age` 是 `[NotMapped]`，需手动赋值。

## 患者生命周期

```
Created → Enabled (默认)
         ↕ (Admin 切换)
         Disabled
         ↓ (无 Draft/Active 医案时才允许禁用)
```

| 场景 | Enabled | Disabled |
|------|---------|----------|
| 创建医案 | 允许 | 禁止 (ERR-30105) |
| 前台列表 | 可见 | 自动过滤 |
| 医生列表 | 可见 | 标注"已禁用" |
| 医生查看历史姓名 | 全名 | 掩码 "张*" |
| 管理员查看姓名 | 全名 | 全名 |

## 桌面架构

```
Admin/Clinical Workspace
  └── PatientMasterDetailControl (嵌入)
        │
        PatientMasterDetailViewModel (MasterDetailViewModelBase)
          ├── PatientService → IPatientDataSource (Local/Remote)
          ├── PatientSearchCache (LRU 10项, 5分钟)
          ├── PatientImportExecutor (BackgroundWorker)
          ├── PatientCardReaderIntegration (身份证读卡器)
          └── MedicalCaseStartCoordinator (多医生未完成检测)
```

## 角色权限

| 角色 | 权限 |
|------|------|
| SuperAdmin | 全部 CRUD + 启禁用 |
| Admin | 全部 CRUD + 启禁用 |
| Doctor | 创建 + 编辑/删除自己的；查看全部 |
| Receptionist | 创建 + 查看启用患者 |

## 关键业务规则

1. **四必填**: Name, IdNumber (18位), PhoneNumber, Address
2. **双唯一**: 电话 + 身份证号，创建/更新时检查 DB + 批量内 HashSet 去重
3. **禁用守卫**: 有 Draft/Active 医案时禁止禁用
4. **删除守卫**: 有任何医案关联时禁止删除 (ERR-20004)
5. **软删除 + 恢复**: Restore 使用 `IgnoreQueryFilters()`
6. **所有权校验**: Doctor 仅操作自己创建的患者
7. **Admin专属**: 启禁用操作仅 Admin 可执行

## 跨模块关系

| 方向 | 模块 | 接口 | 用途 |
|------|------|------|------|
| 被依赖 | MedicalCase | `IPatientCrossModuleService` | 医案关联患者 |
| 被依赖 | Registration | `IPatientService` | 挂号创建时搜索/选择患者 |
| 被依赖 | Sync | `SyncRepository` | 数据同步 |
| 桌面被依赖 | MedicalCase | `PatientSelectionControl` | 医案创建时选择患者 |

## 错误码

| 范围 | 类别 | 数量 |
|------|------|------|
| ERR-200xx | 核心 (NotFound/IdCardExists/PhoneExists/HasRef) | 6 |
| ERR-207xx | 业务规则 (Duplicate/NotDeleted/Empty/Exceeded) | 5 |
| ERR-208xx | 导入 (FileEmpty/Format/Size/NoWorksheet/RowExceeded) | 5 |

## 已知陷阱

| 问题 | 说明 |
|------|------|
| `Age` 计算属性 | Mapperly 无法映射 `[NotMapped]` 字段，需手动 `dto.Age = entity.Age` |
| `FindAsync` + 软删除 | 不在 ChangeTracker 中时应用全局 `IsDeleted` 过滤，恢复需 `IgnoreQueryFilters()` |
| 批量导入行数偏移 | rowCount 包含表头行，限制检查用 `rowCount - 1 > 1000` |

## 相关链接

- [`docs/04-api-reference/patients.md`](../../04-api-reference/patients.md) — API 端点详细文档
- [`docs/07-concepts/patient-status-lifecycle.md`](../patient-status-lifecycle.md) — 生命周期状态图
- [`docs/07-concepts/sensitive-data-classification.md`](../sensitive-data-classification.md) — 敏感数据分级
