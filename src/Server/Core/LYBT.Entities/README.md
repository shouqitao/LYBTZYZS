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
