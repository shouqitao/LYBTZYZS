# LYBT.Entities

> Server端数据实体层 | 8核心实体+枚举定义

## 项目定位

- **层级**: Server Core层
- **职责**: 定义所有业务实体类、基础模型和枚举类型

## 目录结构

```
LYBT.Entities/
├── Models/                     # 核心业务实体
│   ├── UserModel.cs
│   ├── PatientModel.cs
│   ├── MedicalCaseModel.cs
│   ├── ConsultationModel.cs
│   ├── PrescriptionModel.cs
│   ├── PrescriptionItemModel.cs
│   ├── HerbModel.cs
│   └── FormulaModel.cs
├── Enums/                      # 枚举定义
│   ├── UserRole.cs
│   ├── UserStatus.cs
│   ├── MedicalCaseStatus.cs
│   ├── PrescriptionStatus.cs
│   └── Gender.cs
└── Infrastructure/
    └── BaseEntity.cs           # 实体基类
```

## 核心实体

| 实体 | 说明 | 关系 |
|------|------|------|
| UserModel | 用户账户(Admin/Doctor) | 1:N → MedicalCase |
| PatientModel | 患者档案 | 1:N → MedicalCase |
| MedicalCaseModel | 医案(聚合根) | 1:1 → Consultation |
| ConsultationModel | 诊断(四诊记录) | 1:0..1 → Prescription |
| PrescriptionModel | 处方 | 1:N → PrescriptionItem |
| PrescriptionItemModel | 处方条目 | N:1 → Herb |
| HerbModel | 中药材 | 被Formula/Prescription引用 |
| FormulaModel | 验方模板 | 1:N → FormulaHerbItem |

## 核心枚举

| 枚举 | 值 | 说明 |
|------|------|------|
| UserRole | Admin, Doctor | 用户角色 |
| UserStatus | Active, Inactive, Locked | 用户状态 |
| MedicalCaseStatus | Registered, InProgress, Completed, Cancelled | 医案状态 |
| PrescriptionStatus | Draft, Confirmed, Dispensed | 处方状态 |
| Gender | Male, Female, Other | 性别 |

## BaseEntity基类

| 字段 | 类型 | 说明 |
|------|------|------|
| Id | Guid | 主键 |
| CreatedAt | DateTime | 创建时间 |
| UpdatedAt | DateTime? | 更新时间 |
| CreatedBy | Guid? | 创建人 |
| UpdatedBy | Guid? | 更新人 |
| RowVersion | byte[] | 乐观锁版本号 |
| IsDeleted | bool | 软删除标记 |

## Data Annotations规范

| 场景 | 使用方式 |
|------|----------|
| 字符串长度 | Entity [StringLength] |
| 必填验证 | Entity [Required] |
| 范围约束 | Entity [Range] |
| 表名/索引 | Fluent API (Configuration) |
| 枚举转换 | Fluent API |

## 依赖关系

### 依赖
- 无(纯实体定义)

### 被依赖
- LYBT.Infrastructure (AppDbContext)
- 所有Server业务模块
- LYBT.Shared.Models (DTO映射)

## 更新记录

| 日期 | 变更 |
|------|------|
| 2025-12-04 | 按README规范重写文档 |
| 2025-08-07 | 字段标准化 |
| 2025-08-02 | 系统管理员种子数据 |
