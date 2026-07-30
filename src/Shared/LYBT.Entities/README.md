# LYBT.Entities

> Server端领域模型层 | 20个实体文件 | 10个领域目录

## 项目定位

- **层级**: Server Core层
- **职责**: 定义所有业务实体类、基类、接口和标记特性。纯POCO项目，仅依赖System.ComponentModel.DataAnnotations

## 目录结构

```
LYBT.Entities/
├── Common/                        # 基类和接口(4文件)
│   ├── BaseEntity.cs              # 实体基类(Id/审计/并发/软删除)
│   ├── IAuditableEntity.cs        # 审计接口
│   ├── ISoftDeletable.cs          # 软删除接口
│   └── SystemLog.cs               # 系统日志(独立主键 int Id)
├── Auth/                          # 认证实体(4文件)
│   ├── AuthSessionModel.cs        # 认证会话(不继承BaseEntity)
│   ├── RefreshToken.cs            # JWT刷新令牌
│   ├── AutoLoginToken.cs          # 自动登录令牌
│   └── SecurityAuditLog.cs        # 安全审计日志(不继承BaseEntity)
├── MedicalCases/                  # 医案聚合根(3文件)
│   ├── MedicalCaseModel.cs        # 聚合根(DDD域方法)
│   ├── MedicalCaseAuditLog.cs     # 医案审计日志
│   └── MedicalCasePrintLog.cs     # 打印日志
├── Consultations/
│   └── ConsultationModel.cs       # 诊断(内部实体，共享主键)
├── Prescriptions/
│   ├── PrescriptionModel.cs       # 处方(内部实体，外键关联)
│   └── PrescriptionItem.cs        # 处方药材项(值对象)
├── Patients/
│   └── PatientModel.cs            # 患者实体
├── Users/
│   └── UserModel.cs               # 用户实体
├── Herbs/
│   └── HerbModel.cs               # 药材实体
├── Formulas/
│   ├── FormulaModel.cs            # 验方实体
│   └── FormulaHerbItem.cs         # 验方药材项(值对象)
└── Attributes/
    └── SensitiveDataAttribute.cs  # 敏感数据标记特性
```

## 核心实体

| 实体 | 基类 | 说明 |
|------|------|------|
| MedicalCase | BaseEntity | 聚合根，含DDD域方法(Complete/Suspend/SoftDelete) |
| Consultation | BaseEntity | 诊断，与MedicalCase共享主键(1:1) |
| Prescription | BaseEntity | 处方，MedicalCaseId外键(1:0..1) |
| PrescriptionItem | 无 | 值对象，不继承BaseEntity |
| Patient | BaseEntity | 患者档案，含Age计算属性 |
| User | BaseEntity | 用户账户(Admin/Doctor) |
| Herb | BaseEntity | 中药材信息 |
| Formula | BaseEntity | 验方模板 |
| FormulaHerbItem | 无 | 值对象，支持延迟绑定 |
| AuthSession | 无 | 认证会话，独立生命周期 |
| RefreshToken | BaseEntity | JWT刷新令牌，含重放攻击检测 |
| AutoLoginToken | BaseEntity | 自动登录令牌(30天有效) |
| SecurityAuditLog | 无 | 安全审计日志 |
| MedicalCaseAuditLog | 无 | 医案变更审计日志 |
| MedicalCasePrintLog | BaseEntity | 打印日志 |
| SystemLog | 无 | Serilog系统日志(int主键) |

## BaseEntity基类

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | Guid | 主键 |
| CreatedAt | DateTime | 创建时间 |
| UpdatedAt | DateTime? | 更新时间 |
| CreatedBy | Guid? | 创建人 |
| UpdatedBy | Guid? | 更新人 |
| RowVersion | byte[]? | 乐观锁版本号([Timestamp]) |
| IsDeleted | bool | 软删除标记 |

## 设计依据

- 贫血模型为默认；MedicalCase作为唯一例外采用充血模型(DDD聚合根)
- BaseEntity统一审计字段和软删除标记，避免重复定义
- 值对象(PrescriptionItem/FormulaHerbItem)不继承BaseEntity，没有独立生命周期
- 日志/审计实体(SystemLog/AuthSession/SecurityAuditLog/MedicalCaseAuditLog)只写不改，有独立Id
- Data Annotations用于字段级约束，Fluent API用于表级配置

## 依赖关系

### 依赖
- 无(纯实体定义)

### 被依赖
- LYBT.Infrastructure (AppDbContext、EF配置)
- 所有Server业务模块
- LYBT.Shared.Models (DTO映射)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2026-03-01 | 根据实际目录结构重写README |
| 2025-12-04 | 按README规范重写文档 |

## 开发笔记

# LYBT.Entities 代码知识

## 模块概述

Server 端领域模型层 -- 定义所有业务实体、基类和接口。纯 POCO 项目，不依赖 EF Core 或任何框架，仅依赖 System.ComponentModel.DataAnnotations。

### 目录结构

```
LYBT.Entities/
├── Common/                           # 基类和接口
│   ├── BaseEntity.cs                 # 实体基类（Id/审计/并发/软删除）
│   ├── IAuditableEntity.cs           # 审计接口
│   ├── ISoftDeletable.cs             # 软删除接口
│   └── SystemLog.cs                  # 系统日志实体
├── MedicalCases/                     # 医案聚合根
│   ├── MedicalCaseModel.cs           # 聚合根实体（DDD 域方法）
│   ├── MedicalCaseAuditLog.cs        # 审计日志实体
│   └── MedicalCasePrintLog.cs        # 打印日志实体
├── Consultations/
│   └── ConsultationModel.cs          # 诊断实体（MedicalCase 内部实体）
├── Prescriptions/
│   ├── PrescriptionModel.cs          # 处方实体（MedicalCase 内部实体）
│   └── PrescriptionItem.cs           # 处方药材项（值对象，不继承 BaseEntity）
├── Patients/
│   └── PatientModel.cs               # 患者实体
├── Users/
│   └── UserModel.cs                  # 用户实体
├── Herbs/
│   └── HerbModel.cs                  # 药材实体
├── Formulas/
│   ├── FormulaModel.cs               # 验方实体
│   └── FormulaHerbItem.cs            # 验方药材项
├── Auth/
│   ├── AuthSessionModel.cs           # 认证会话
│   ├── RefreshToken.cs               # 刷新令牌
│   ├── SecurityAuditLog.cs           # 安全审计日志
│   └── AutoLoginToken.cs             # 自动登录令牌
└── Attributes/
    └── SensitiveDataAttribute.cs     # 敏感数据标记特性
```

## 架构决策

| 决策 | 原因 | 日期 | 关联 OpenSpec |
|------|------|------|--------------|
| BaseEntity 统一基类 (Id/审计/并发/软删除) | 所有业务实体共享统一字段结构 | 初始设计 | - |
| MedicalCase 是唯一聚合根 | DDD 设计，Consultation 和 Prescription 是内部实体 | Epic #1612 | refactor-server-ddd-aggregates |
| Consultation 共享主键 (Id = MedicalCase.Id) | 一医案一诊断，强制 1:1 关系 | - | refactor-server-ddd-aggregates |
| Prescription 使用独立 ID + MedicalCaseId 外键 | 一医案至多一处方 (1:0..1)，可独立创建/删除 | - | simplify-medicalcase-dataflow |
| 内部实体移除反向导航属性 | DDD 原则: 内部实体不应反向导航到聚合根 | - | refactor-server-ddd-aggregates |
| PrescriptionItem 不继承 BaseEntity | 值对象语义，没有 RowVersion/IsDeleted/审计字段 | - | - |
| DoctorId 重命名为 UserId | 统一命名，避免与 User 实体的 Id 混淆 | - | simplify-medicalcase-dataflow |
| 诊断精简为 4 个核心字段 | 移除冗余字段，保留 PresentIllness/TongueDiagnosis/PulseDiagnosis/TcmDiagnosis | - | refactor-diagnosis-fields |
| 打印字段迁移到 MedicalCase 层级 | 打印是医案级操作，不是处方级 | T2-X8-09 | - |

## 核心实体详解

### BaseEntity (所有业务实体的基类)

```csharp
public abstract class BaseEntity : IAuditableEntity, ISoftDeletable
{
    Guid Id           // 主键 (默认 Guid.NewGuid())
    DateTime CreatedAt    // 创建时间 (默认 DateTime.UtcNow)
    DateTime? UpdatedAt   // 更新时间
    Guid? CreatedBy       // 创建者ID
    Guid? UpdatedBy       // 更新者ID
    byte[]? RowVersion    // 乐观并发控制 ([Timestamp])
    bool IsDeleted        // 软删除标记
}
```

### MedicalCase (聚合根)

```
MedicalCase : BaseEntity
├── PatientId, PatientName      # 跨聚合引用 (仅ID + 冗余名称)
├── UserId, DoctorName          # 医生引用 (重命名自 DoctorId)
├── CaseNumber                  # 业务编号 (MC20251219001)
├── CaseStatus                  # Active / Suspended / Completed
├── NeedsPrescription           # null: 未标记 / true / false
├── CompletedAt                 # 完成时间
├── Remark                      # 备注
├── PrintVersion/LastPrintedAt/PrintCount/IsPrinted  # 打印管理
├── Consultation (1:1)          # 诊断记录 (共享主键)
├── Prescription (1:0..1)       # 处方信息 (MedicalCaseId 外键)
└── PrintLogs (1:N)             # 打印日志

DDD 域方法:
├── Complete()          # 设置 Completed + CompletedAt
├── Suspend()           # 设置 Suspended
├── SoftDelete()        # 设置 IsDeleted = true
└── UpdateConsultation(4 params)  # 更新诊断 4 个核心字段

计算属性:
├── IsLocked    # IsCompleted && 非当天
├── IsActive    # Suspended || Active
└── IsCompleted # CaseStatus == Completed
```

### Consultation (诊断 -- MedicalCase 内部实体)

```
Consultation : BaseEntity
├── PresentIllness       # 现病史 (2000字)
├── TongueDiagnosis      # 舌诊 (500字)
├── PulseDiagnosis       # 脉诊 (500字)
└── TcmDiagnosis         # 中医辨证 (500字，必填)
// Id 与 MedicalCase.Id 共享 (共享主键)
// 无反向导航属性
// PrescriptionEnabled 已移除 -> MedicalCase.NeedsPrescription
```

### Prescription (处方 -- MedicalCase 内部实体)

```
Prescription : BaseEntity
├── MedicalCaseId        # 外键
├── PrescriptionNumber   # 编号 (RX-YYYYMMDD-NNNN)
├── DosageCount          # 帖数 (默认 7)
├── Discount             # 折扣 decimal(3,2) (默认 1.0)
├── Usage                # 用法
├── Advice               # 医嘱
├── ReferencedFormulas   # 引用验方 (逗号分隔)
├── Remark               # 备注
└── Items (1:N)          # 处方药材项
// 无反向导航属性
// 打印字段已迁移到 MedicalCase (T2-X8-09)
// Indication/FormulaSource 已删除
```

### PrescriptionItem (值对象 -- 不继承 BaseEntity)

```
PrescriptionItem
├── Id               # 主键 (Guid)
├── PrescriptionId   # 外键
├── HerbId           # 药材ID
├── HerbName         # 药材名称
├── Dosage           # 剂量 (整数克)
├── Unit             # 单位 (默认 "g")
├── DecocteMethod    # 煎法 (枚举)
├── UnitPrice        # 单价 decimal(18,2)
├── Amount           # 小计 (计算属性: UnitPrice * Dosage)
├── Usage            # 用法说明
└── Remark           # 备注
// 不继承 BaseEntity，没有 RowVersion/IsDeleted/审计字段
```

### MedicalCaseAuditLog (审计日志)

```
MedicalCaseAuditLog (不继承 BaseEntity，有自己的 Id/CreatedAt)
├── MedicalCaseId     # 关联医案
├── OperatorId/Name/Role  # 操作者信息
├── OperationType     # Create/Update/SoftDelete
├── ChangedFields     # JSON 格式
├── OldValues/NewValues  # JSON 格式
├── Reason            # 修改原因
└── MedicalCase (导航属性)
```

### SensitiveDataAttribute

```csharp
[SensitiveData(SensitiveDataType.ContactInfo)]  // 标记敏感字段
// 支持: PersonalInfo, MedicalInfo, ContactInfo, IdentityInfo, FinancialInfo
// 脱敏模式: Default, Partial, Full, Hash
```

## 死代码与废弃标记

| 类型/方法 | 状态 | 替代方案 | 清理计划 |
|-----------|------|----------|----------|
| (无 [Obsolete] 标记) | - | - | - |

## 已知陷阱

| 问题 | 原因 | 解决方案 |
|------|------|----------|
| BaseEntity.CreatedAt 默认值 DateTime.UtcNow 与 AppDbContext 中 DateTime.Now 冲突 | 实体默认用 UTC，但 AppDbContext.SetAuditFields() 会覆盖为 DateTime.Now | 以 AppDbContext 为准，实体默认值仅在非 EF Core 场景使用 |
| PrescriptionItem 没有 RowVersion | 不继承 BaseEntity，是值对象 | BaseRepository.SaveChangesAsync 中通过 Metadata 检查属性是否存在 |
| MedicalCase.IsLocked 使用 CompletedAt ?? CreatedAt | CompletedAt 为 null 时回退到 CreatedAt | 业务逻辑: Completed 状态一定有 CompletedAt (Complete() 域方法设置) |
| Consultation.TcmDiagnosis 在 Entity 层是可空的 | 数据库允许 null，但业务规则要求必填 | 验证在 Service 层 (FluentValidation) 和 CommandService 中执行 |
| MedicalCaseAuditLog 不继承 BaseEntity | 审计日志有自己的生命周期，不需要 RowVersion/IsDeleted | 有自己的 Id 和 CreatedAt，通过 [ForeignKey] 关联 MedicalCase |

## OpenSpec 追踪

| OpenSpec ID | 内容 | 状态 |
|-------------|------|------|
| simplify-medicalcase-dataflow | MedicalCase 数据流简化，DoctorId -> UserId | 已完成 |
| refactor-server-ddd-aggregates | 移除 Consultation/Prescription 反向导航 | 已完成 |
| refactor-diagnosis-fields | 诊断精简为 4 核心字段 | 已完成 |
| consultation-field-alignment | PrescriptionEnabled 移除，统一到 MedicalCase | 已完成 |
| refactor-medicalcase-management | MedicalCaseAuditLog 实体 (LIFECYCLE-008) | 已完成 |
| refactor-login-authentication | AutoLoginToken 实体 (CVT-001) | 已完成 |

## 代码文件结构

```
Common/
├── IAuditableEntity.cs      # 审计接口 (CreatedAt/CreatedBy/UpdatedAt/UpdatedBy)
├── ISoftDeletable.cs        # 软删除接口 (IsDeleted)
├── BaseEntity.cs            # 实体基类，实现 IAuditableEntity + ISoftDeletable
└── SystemLog.cs             # 系统日志实体 (独立主键 int Id，不继承 BaseEntity)
Auth/
├── AuthSessionModel.cs      # 认证会话 (独立实体，不继承 BaseEntity)
├── RefreshToken.cs          # JWT 刷新令牌 : BaseEntity，含域方法
├── AutoLoginToken.cs        # 自动登录令牌 : BaseEntity，含域方法
└── SecurityAuditLog.cs      # 安全审计日志 (独立实体，不继承 BaseEntity)
MedicalCases/
├── MedicalCaseModel.cs      # 医案聚合根 : BaseEntity，含 DDD 域方法
├── MedicalCaseAuditLog.cs   # 医案审计日志 (独立实体，不继承 BaseEntity)
└── MedicalCasePrintLog.cs   # 医案打印日志 : BaseEntity
Consultations/
└── ConsultationModel.cs     # 诊断实体 : BaseEntity (MedicalCase 内部实体，共享主键)
Prescriptions/
├── PrescriptionModel.cs     # 处方实体 : BaseEntity (MedicalCase 内部实体，外键关联)
└── PrescriptionItem.cs      # 处方药材项 (值对象，不继承 BaseEntity)
Patients/
└── PatientModel.cs          # 患者实体 : BaseEntity，含 Age 计算属性
Users/
└── UserModel.cs             # 用户实体 : BaseEntity
Herbs/
└── HerbModel.cs             # 药材实体 : BaseEntity
Formulas/
├── FormulaModel.cs          # 验方实体 : BaseEntity
└── FormulaHerbItem.cs       # 验方药材项 (值对象，不继承 BaseEntity)
Attributes/
└── SensitiveDataAttribute.cs # 敏感数据标记特性 + SensitiveDataType/MaskingMode 枚举
```

### Common/IAuditableEntity.cs
**IAuditableEntity** : interface | 审计实体接口，定义创建和更新审计字段

| 属性 | 说明 |
|------|------|
| CreatedAt { get; set; } | 创建时间 (DateTime) |
| CreatedBy { get; set; } | 创建者ID (Guid?) |
| UpdatedAt { get; set; } | 更新时间 (DateTime?) |
| UpdatedBy { get; set; } | 更新者ID (Guid?) |

### Common/ISoftDeletable.cs
**ISoftDeletable** : interface | 软删除接口

| 属性 | 说明 |
|------|------|
| IsDeleted { get; set; } | 软删除标记 (bool) |

### Common/BaseEntity.cs
**BaseEntity** (abstract) : IAuditableEntity, ISoftDeletable | 所有业务实体的基类

| 属性 | 说明 |
|------|------|
| Id | Guid 主键，默认 Guid.NewGuid() |
| CreatedAt | 创建时间，默认 DateTime.UtcNow |
| UpdatedAt | 更新时间 (DateTime?) |
| CreatedBy | 创建者ID (Guid?) |
| UpdatedBy | 更新者ID (Guid?) |
| RowVersion | 乐观并发控制 ([Timestamp], byte[]?) |
| IsDeleted | 软删除标记，默认 false |

### Common/SystemLog.cs
**SystemLog** | 系统日志实体，int Id 主键，用于 Serilog SQL Server Sink

### Auth/AuthSessionModel.cs
**AuthSession** | 认证会话实体，不继承 BaseEntity，[Table("AuthSessions")]

### Auth/RefreshToken.cs
**RefreshToken** : BaseEntity | JWT 刷新令牌，含 Token Family 重放攻击检测

| 方法/属性 | 说明 |
|-----------|------|
| IsActive (NotMapped) | 计算属性: 未撤销 + 未删除 + 未使用 + 未过期 + 未绝对过期 |
| IsReplayAttack | 计算属性: IsUsed 为 true 表示重放攻击 |
| IsValid() | 检查 Token 是否有效 (逻辑同 IsActive) |
| Revoke(reason, revokedBy?) | 撤销 Token，设置撤销信息和时间 |
| RecordUsage() | 增加 UsageCount，更新 LastUsedAt |
| MarkAsUsed(replacedByToken?) | 标记已使用 (Token 轮换)，设置 ReplacedByToken |

### Auth/AutoLoginToken.cs
**AutoLoginToken** : BaseEntity | "记住密码"自动登录令牌，30天有效期

| 方法/属性 | 说明 |
|-----------|------|
| IsActive (NotMapped) | 计算属性: 未撤销 + 未删除 + 未使用 + 未过期 |
| IsReplayAttack | 计算属性: IsUsed 为 true 表示重放攻击 |
| IsValid() | 检查 Token 是否有效 |
| Revoke(reason, revokedBy?) | 撤销 Token |
| RecordUsage() | 记录使用 |
| MarkAsUsed(replacedByToken?) | 标记已使用 (Token 轮换) |

### Auth/SecurityAuditLog.cs
**SecurityAuditLog** | 安全审计日志，不继承 BaseEntity，记录认证安全事件

### MedicalCases/MedicalCaseModel.cs
**MedicalCase** : BaseEntity | 聚合根，管理完整诊疗流程

| 方法/属性 | 说明 |
|-----------|------|
| IsLocked (计算) | 已完成 且 非当天 -> 锁定 |
| IsActive (计算) | Suspended 或 Active 状态 |
| IsCompleted (计算) | CaseStatus == Completed |
| Complete() | 设置 Completed 状态 + CompletedAt |
| Suspend() | 设置 Suspended 状态 |
| SoftDelete() | 设置 IsDeleted = true |
| UpdateConsultation(4 params) | 更新诊断的 4 个核心字段 |

### MedicalCases/MedicalCaseAuditLog.cs
**MedicalCaseAuditLog** | 医案审计日志，不继承 BaseEntity，有独立 Id/CreatedAt

### MedicalCases/MedicalCasePrintLog.cs
**MedicalCasePrintLog** : BaseEntity | 打印日志，FK 关联 MedicalCase

### Consultations/ConsultationModel.cs
**Consultation** : BaseEntity | 诊断实体，Id 与 MedicalCase 共享主键，4 个核心诊断字段

### Prescriptions/PrescriptionModel.cs
**Prescription** : BaseEntity | 处方实体，MedicalCaseId 外键，含 Items 集合

### Prescriptions/PrescriptionItem.cs
**PrescriptionItem** | 值对象，不继承 BaseEntity，Amount 为计算属性 (UnitPrice * Dosage)

### Patients/PatientModel.cs
**Patient** : BaseEntity | 患者实体，含 [SensitiveData] 标记字段，Age 计算属性

### Users/UserModel.cs
**User** : BaseEntity | 用户实体，含安全字段 (PasswordHash/FailedLoginCount/LockoutEnd)

### Herbs/HerbModel.cs
**Herb** : BaseEntity | 药材实体，含价格和分类信息

### Formulas/FormulaModel.cs
**Formula** : BaseEntity | 验方模板实体，含 Herbs 集合 (FormulaHerbItem)

### Formulas/FormulaHerbItem.cs
**FormulaHerbItem** | 值对象，不继承 BaseEntity，支持延迟绑定 (OriginalHerbName + IsValidated)

### Attributes/SensitiveDataAttribute.cs
**SensitiveDataAttribute** : Attribute (sealed) | 敏感数据标记，用于日志脱敏和加密存储

| 属性 | 说明 |
|------|------|
| DataType | SensitiveDataType 枚举 (PersonalInfo/MedicalInfo/ContactInfo/IdentityInfo/FinancialInfo) |
| RequireLogMasking | 是否需要日志脱敏，默认 true |
| MaskingMode | 脱敏模式 (Default/Partial/Full/Hash) |

## 设计分析

### RefreshToken 与 AutoLoginToken 的代码重复
RefreshToken 和 AutoLoginToken 拥有几乎完全相同的域方法签名 (IsValid/Revoke/RecordUsage/MarkAsUsed/IsReplayAttack)。两个类各自独立实现，没有抽取共享基类或接口。当前体量下可接受，但如果后续新增更多 Token 类型，建议抽取 `IRevocableToken` 接口或 `RevocableTokenBase` 基类。

### 不继承 BaseEntity 的实体分类
项目中有两类不继承 BaseEntity 的实体:
1. **值对象** (PrescriptionItem, FormulaHerbItem): 依附于聚合，没有独立生命周期，不需要审计/并发/软删除
2. **日志/审计实体** (SystemLog, AuthSession, SecurityAuditLog, MedicalCaseAuditLog): 只写不改，有独立的 Id 和 CreatedAt，不需要 RowVersion/IsDeleted

## 模块演进记录

- **初始设计**: BaseEntity 统一基类，各领域实体分目录
- **UltraThink v2.0**: 架构简化 -- Consultation 合并 BaseConsultation
- **Epic #1612**: DDD 聚合根 -- MedicalCase 成为聚合根，Consultation/Prescription 移除反向导航
- **OpenSpec: refactor-diagnosis-fields**: 诊断字段精简 -- 移除冗余字段，保留 4 个核心
- **OpenSpec: simplify-medicalcase-dataflow**: DoctorId -> UserId 重命名，字段删除 (Indication/FormulaSource/ConsultationDate)
- **T2-X8-09**: 打印字段从 Prescription 迁移到 MedicalCase 层级
- **SensitiveDataAttribute**: 新增敏感数据标记特性，支持日志脱敏
