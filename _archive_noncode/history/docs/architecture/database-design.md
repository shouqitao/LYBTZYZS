# 数据库设计文档

## 📋 数据库概述

凌隐宝堂中医诊所诊疗系统采用SQL Server数据库，基于Entity Framework Core 8.0.17的Code First模式设计。所有8个业务模块共享统一的`AppDbContext`数据上下文。

**设计原则**：
- **统一数据上下文** - 所有模块共享AppDbContext，简化数据访问
- **软删除策略** - 使用Status字段标记删除，保证数据完整性
- **审计字段** - 自动维护CreateTime、UpdateTime时间戳
- **外键约束** - 确保数据关系完整性和业务规则
- **索引优化** - 基于业务查询模式优化性能

## 🏗️ 核心实体关系图

```
Users (用户表)
├── AuthSessions (认证会话) - 一对多
├── MedicalCases (医疗案例) - 一对多 (作为医生)
├── Consultations (看诊记录) - 一对多 (作为医生)
└── Prescriptions (处方记录) - 一对多 (作为医生)

Patients (患者表)
├── MedicalCases (医疗案例) - 一对多
├── Consultations (看诊记录) - 一对多
└── Prescriptions (处方记录) - 一对多

MedicalCases (医疗案例)
├── Consultation (看诊记录) - 一对一
└── Prescription (处方) - 一对一 (可选)

Prescriptions (处方表)
└── PrescriptionItems (处方条目) - 一对多

Formulas (验方表)
└── FormulaHerbs (验方药材) - 一对多

Herbs (药材表) - 独立表，供选择使用
```

## 📊 核心数据表设计

### 1. Users - 用户表

**功能**：统一用户管理，包含医生功能

| 字段名 | 类型 | 长度 | 必填 | 索引 | 说明 |
|-------|------|------|------|------|------|
| Id | uniqueidentifier | - | ✅ | PK | 用户唯一标识 |
| UserName | nvarchar | 50 | ✅ | UNIQUE | 登录用户名 |
| RealName | nvarchar | 50 | ✅ | - | 真实姓名 |
| PinYinCode | nvarchar | 50 | - | - | 拼音码(快速搜索) |
| PhoneNumber | nvarchar | 20 | - | - | 电话号码 |
| Email | nvarchar | 100 | - | - | 邮箱地址 |
| Role | int | - | ✅ | - | 角色(Admin/Doctor) |
| Status | int | - | ✅ | - | 状态(启用/禁用) |
| PasswordHash | nvarchar | 256 | ✅ | - | 密码哈希 |
| FailedLoginCount | int | - | ✅ | - | 失败登录次数 |
| LockoutEnd | datetime2 | - | - | - | 锁定结束时间 |
| **医生专属字段** |
| Specialty | nvarchar | 200 | - | - | 专长 |
| RegistrationFee | decimal(18,2) | - | - | - | 挂号费 |
| LicenseNumber | nvarchar | 50 | - | - | 执业证书号 |
| Introduction | nvarchar | 1000 | - | - | 简介 |
| **审计字段** |
| CreatedTime | datetime2 | - | ✅ | - | 创建时间 |
| UpdateTime | datetime2 | - | - | - | 更新时间 |
| LastLoginTime | datetime2 | - | - | - | 最后登录时间 |
| Remark | nvarchar | 500 | - | - | 备注 |

### 2. Patients - 患者表

**功能**：患者档案管理和基础接待

| 字段名 | 类型 | 长度 | 必填 | 索引 | 说明 |
|-------|------|------|------|------|------|
| Id | uniqueidentifier | - | ✅ | PK | 患者唯一标识 |
| Name | nvarchar | 100 | ✅ | - | 患者姓名 |
| PinYinCode | nvarchar | 20 | - | - | 拼音码(快速搜索) |
| Gender | int | - | ✅ | - | 性别 |
| BirthDate | datetime2 | - | - | - | 出生日期 |
| IdType | int | - | ✅ | - | 证件类型 |
| IdNumber | nvarchar | 50 | - | - | 证件号码 |
| PhoneNumber | nvarchar | 20 | - | - | 手机号码 |
| Address | nvarchar | 256 | - | - | 地址 |
| AllergyHistory | nvarchar | 500 | - | - | 过敏史 |
| **扩展信息** |
| MaritalStatus | int | - | ✅ | - | 婚姻状态 |
| BloodType | int | - | ✅ | - | 血型 |
| EmergencyContactName | nvarchar | 100 | - | - | 紧急联系人姓名 |
| EmergencyContactPhone | nvarchar | 20 | - | - | 紧急联系人电话 |
| EmergencyContactRelation | nvarchar | 50 | - | - | 紧急联系人关系 |
| **业务字段** |
| Status | int | - | ✅ | - | 患者状态 |
| DisableReason | nvarchar | 128 | - | - | 禁用原因 |
| LastVisitTime | datetime2 | - | - | - | 最后就诊时间 |
| VisitCount | int | - | ✅ | - | 就诊次数 |
| **审计字段** |
| CreatedAt | datetime2 | - | ✅ | - | 创建时间 |
| UpdateTime | datetime2 | - | - | - | 更新时间 |
| CreatedBy | uniqueidentifier | - | - | - | 创建者ID |
| UpdatedBy | uniqueidentifier | - | - | - | 更新者ID |

### 3. MedicalCases - 医疗案例表

**功能**：诊疗流程容器，统一管理整个看诊过程

| 字段名 | 类型 | 长度 | 必填 | 索引 | 说明 |
|-------|------|------|------|------|------|
| Id | uniqueidentifier | - | ✅ | PK | 医案唯一标识 |
| PatientId | uniqueidentifier | - | ✅ | FK | 患者ID |
| DoctorId | uniqueidentifier | - | ✅ | FK | 医生ID |
| ConsultationId | uniqueidentifier | - | - | FK | 诊断记录ID |
| PrescriptionId | uniqueidentifier | - | - | FK | 处方ID |
| Status | nvarchar | 50 | ✅ | INDEX | 状态(Registered/InProgress/Completed) |
| Remark | nvarchar | 500 | - | - | 备注 |
| **审计字段** |
| CreatedTime | datetime2 | - | ✅ | - | 创建时间 |
| UpdateTime | datetime2 | - | - | - | 更新时间 |

**关系定义**：
- **1:1 关系**: MedicalCase ↔ Consultation (一个医案对应一次诊断)
- **1:1 关系**: MedicalCase ↔ Prescription (可选，一个医案可能有处方)

### 4. Consultations - 看诊表

**功能**：中医四诊数据记录，纯数据存储专业化

| 字段名 | 类型 | 长度 | 必填 | 索引 | 说明 |
|-------|------|------|------|------|------|
| Id | uniqueidentifier | - | ✅ | PK | 看诊唯一标识 |
| MedicalCaseId | uniqueidentifier | - | ✅ | FK | 医疗案例ID |
| PatientId | uniqueidentifier | - | ✅ | FK | 患者ID |
| UserId | uniqueidentifier | - | ✅ | FK | 医生ID |
| **主诉与病史** |
| ChiefComplaint | nvarchar | 500 | - | - | 主诉 |
| PresentIllness | nvarchar | 1000 | - | - | 现病史 |
| **中医四诊** |
| Inspection | nvarchar | 500 | - | - | 望诊 |
| AuscultationOlfaction | nvarchar | 500 | - | - | 闻诊 |
| Inquiry | nvarchar | 1000 | - | - | 问诊 |
| Palpation | nvarchar | 500 | - | - | 切诊 |
| **诊断与治疗** |
| TCMDiagnosis | nvarchar | 500 | - | - | 中医诊断 |
| TreatmentPrinciple | nvarchar | 500 | - | - | 治疗原则 |
| MedicalAdvice | nvarchar | 500 | - | - | 医嘱 |
| Remark | nvarchar | 1000 | - | - | 备注 |
| **审计字段** |
| CreatedTime | datetime2 | - | ✅ | - | 创建时间 |
| UpdateTime | datetime2 | - | - | - | 更新时间 |

### 5. Prescriptions - 处方表

**功能**：处方管理和智能建议

| 字段名 | 类型 | 长度 | 必填 | 索引 | 说明 |
|-------|------|------|------|------|------|
| Id | uniqueidentifier | - | ✅ | PK | 处方唯一标识 |
| MedicalCaseId | uniqueidentifier | - | ✅ | FK | 医疗案例ID |
| PatientId | uniqueidentifier | - | ✅ | FK | 患者ID |
| UserId | uniqueidentifier | - | ✅ | FK | 医生ID |
| Indication | nvarchar | 200 | - | - | 适应症 |
| DosageCount | int | - | ✅ | - | 剂数 |
| Discount | decimal(18,2) | - | ✅ | - | 折扣 |
| Advice | nvarchar | 500 | - | - | 医嘱 |
| FormulaSource | nvarchar | 100 | - | - | 验方来源 |
| Status | int | - | ✅ | - | 处方状态 |
| Remark | nvarchar | 500 | - | - | 备注 |
| **审计字段** |
| CreatedTime | datetime2 | - | ✅ | - | 创建时间 |
| UpdateTime | datetime2 | - | - | - | 更新时间 |

### 6. PrescriptionItems - 处方条目表

**功能**：处方药材明细（**注意**：需重命名为PrescriptionHerbItems）

| 字段名 | 类型 | 长度 | 必填 | 索引 | 说明 |
|-------|------|------|------|------|------|
| Id | uniqueidentifier | - | ✅ | PK | 条目唯一标识 |
| PrescriptionId | uniqueidentifier | - | ✅ | FK | 处方ID |
| HerbId | uniqueidentifier | - | ✅ | FK | 药材ID |
| HerbName | nvarchar | 100 | ✅ | - | 药材名称 |
| Quantity | decimal(10,3) | - | ✅ | - | 数量 |
| Unit | nvarchar | 10 | ✅ | - | 单位 |
| UnitPrice | decimal(18,2) | - | ✅ | - | 单价 |
| Usage | nvarchar | 100 | - | - | 用法 |
| Remark | nvarchar | 200 | - | - | 备注 |

**重要**：根据命名统一要求，此表需要重命名为`PrescriptionHerbItems`。

### 7. Herbs - 药材表

**功能**：中药材信息管理（仅处方用药，不含库存）

| 字段名 | 类型 | 长度 | 必填 | 索引 | 说明 |
|-------|------|------|------|------|------|
| Id | uniqueidentifier | - | ✅ | PK | 药材唯一标识 |
| Name | nvarchar | 100 | ✅ | INDEX | 药材名称 |
| PinYinCode | nvarchar | 20 | - | INDEX | 拼音码 |
| Origin | nvarchar | 50 | - | - | 产地 |
| Spec | nvarchar | 50 | - | - | 规格 |
| Unit | nvarchar | 10 | ✅ | - | 单位 |
| Price | decimal(18,2) | ✅ | - | - | 单价 |
| CostPrice | decimal(18,2) | - | - | - | 成本价 |
| Effect | nvarchar | 256 | - | - | 功效 |
| Usage | nvarchar | 256 | - | - | 用法 |
| Status | int | - | ✅ | - | 状态 |
| Remark | nvarchar | 200 | - | - | 备注 |
| **审计字段** |
| CreatedTime | datetime2 | - | ✅ | - | 创建时间 |
| UpdateTime | datetime2 | - | - | - | 更新时间 |

### 8. Formulas - 验方表

**功能**：经典验方模板库

| 字段名 | 类型 | 长度 | 必填 | 索引 | 说明 |
|-------|------|------|------|------|------|
| Id | uniqueidentifier | - | ✅ | PK | 验方唯一标识 |
| Name | nvarchar | 200 | ✅ | - | 验方名称 |
| Effect | nvarchar | 500 | - | - | 主治功效 |
| Usage | nvarchar | 500 | - | - | 用法用量 |
| Property | nvarchar | 300 | - | - | 方解 |
| Status | int | - | ✅ | - | 状态 |
| IsShared | bit | - | ✅ | - | 是否共享 |
| Remark | nvarchar | 500 | - | - | 备注 |
| **审计字段** |
| CreatedTime | datetime2 | - | ✅ | - | 创建时间 |
| UpdateTime | datetime2 | - | - | - | 更新时间 |

**注意**：验方药材组成通过 `FormulaHerbItem` 导航属性管理，但在EF Core配置中被Ignore。

### 9. AuthSessions - 认证会话表

**功能**：JWT认证会话管理

| 字段名 | 类型 | 长度 | 必填 | 索引 | 说明 |
|-------|------|------|------|------|------|
| Id | uniqueidentifier | - | ✅ | PK | 会话唯一标识 |
| UserId | uniqueidentifier | - | ✅ | FK,INDEX | 用户ID |
| TokenHash | nvarchar | 256 | ✅ | - | Token哈希 |
| IpAddress | nvarchar | 45 | - | - | IP地址 |
| UserAgent | nvarchar | 500 | - | - | 用户代理 |
| LoginTime | datetime2 | - | ✅ | INDEX | 登录时间 |
| ExpiryTime | datetime2 | - | ✅ | - | 过期时间 |
| Status | int | - | ✅ | INDEX | 会话状态 |
| **审计字段** |
| CreatedTime | datetime2 | - | ✅ | - | 创建时间 |
| UpdateTime | datetime2 | - | - | - | 更新时间 |

### 10. AdminSecrets - 管理员密钥表

**功能**：超级管理员账户管理

| 字段名 | 类型 | 长度 | 必填 | 索引 | 说明 |
|-------|------|------|------|------|------|
| Id | uniqueidentifier | - | ✅ | PK | 记录唯一标识 |
| Username | nvarchar | 50 | ✅ | UNIQUE | 用户名 |
| PasswordHash | nvarchar | 500 | ✅ | - | 密码哈希 |

**种子数据**：
- Username: `sysadmin`
- Password: `Admin@123456`（已哈希存储）

## 🔄 数据关系设计

### 核心业务关系

#### 1. 用户 → 业务数据 (一对多)
```sql
Users 1---n AuthSessions (用户认证会话)
Users 1---n MedicalCases (医生看诊案例)
Users 1---n Consultations (医生诊断记录)
Users 1---n Prescriptions (医生处方记录)
```

#### 2. 患者 → 医疗记录 (一对多)
```sql
Patients 1---n MedicalCases (患者医疗案例)
Patients 1---n Consultations (患者诊断记录)
Patients 1---n Prescriptions (患者处方记录)
```

#### 3. 医疗案例聚合 (一对一)
```sql
MedicalCases 1---1 Consultations (医案对应诊断)
MedicalCases 1---0..1 Prescriptions (医案可选处方)
```

#### 4. 处方详情 (一对多)
```sql
Prescriptions 1---n PrescriptionItems (处方药材明细)
Herbs 1---n PrescriptionItems (药材在处方中使用)
```

#### 5. 验方组成 (逻辑关系)
```sql
Formulas 1---n FormulaHerbItems (验方药材组成)
-- 注意：在EF配置中被Ignore，通过业务逻辑管理
```

### 外键约束

| 子表 | 外键字段 | 父表 | 约束行为 |
|-----|---------|------|---------|
| AuthSessions | UserId | Users | Cascade |
| MedicalCases | PatientId | Patients | Restrict |
| MedicalCases | DoctorId | Users | Restrict |
| Consultations | MedicalCaseId | MedicalCases | Restrict |
| Consultations | PatientId | Patients | Restrict |
| Consultations | UserId | Users | Restrict |
| Prescriptions | MedicalCaseId | MedicalCases | Restrict |
| Prescriptions | PatientId | Patients | Restrict |
| Prescriptions | UserId | Users | Restrict |
| PrescriptionItems | PrescriptionId | Prescriptions | Cascade |
| PrescriptionItems | HerbId | Herbs | Restrict |

## 📊 索引优化策略

### 主要索引

#### 1. 唯一索引
- `Users.UserName` - 确保用户名唯一
- `AdminSecrets.Username` - 确保管理员用户名唯一

#### 2. 业务查询索引
- `Herbs.Name` - 药材名称快速搜索
- `Herbs.PinYinCode` - 拼音码快速搜索
- `MedicalCases.Status` - 按状态筛选医案
- `AuthSessions.UserId` - 用户会话查询
- `AuthSessions.LoginTime` - 按登录时间排序
- `AuthSessions.Status` - 按会话状态筛选

#### 3. 外键索引
- `MedicalCases.PatientId` - 患者医案查询
- `MedicalCases.DoctorId` - 医生医案查询
- `Consultations.MedicalCaseId` - 医案诊断查询
- `Consultations.PatientId` - 患者诊断历史
- `Consultations.UserId` - 医生诊断记录

## 🛠️ 数据库配置

### 连接配置
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTDB;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true"
  }
}
```

### 连接池配置
- **最大连接数**: 20 (适合<20人员规模)
- **最小连接数**: 2
- **连接超时**: 30秒
- **命令超时**: 120秒

### EF Core配置
```csharp
services.AddDbContext<AppDbContext>(options =>
{
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);
    });
});
```

## 📝 数据库迁移

### 迁移命令
```bash
# 添加迁移 (必须使用Infrastructure项目)
dotnet ef migrations add MigrationName 
  --project src/Server/Core/LYBT.Infrastructure 
  --startup-project src/Server/Services/LYBT.WebAPI

# 更新数据库
dotnet ef database update 
  --project src/Server/Core/LYBT.Infrastructure 
  --startup-project src/Server/Services/LYBT.WebAPI

# 查看迁移历史
dotnet ef migrations list 
  --project src/Server/Core/LYBT.Infrastructure 
  --startup-project src/Server/Services/LYBT.WebAPI
```

### 重要迁移记录

#### 计划中的命名统一迁移
```sql
-- PrescriptionItems → PrescriptionHerbItems
ALTER TABLE PrescriptionItems RENAME TO PrescriptionHerbItems;
ALTER INDEX IX_PrescriptionItems_PrescriptionId 
  RENAME TO IX_PrescriptionHerbItems_PrescriptionId;
ALTER INDEX IX_PrescriptionItems_HerbId 
  RENAME TO IX_PrescriptionHerbItems_HerbId;
```

## 🔒 数据安全

### 安全策略
- **密码存储**: BCrypt哈希+盐值加密
- **软删除**: 使用Status字段，避免物理删除
- **审计跟踪**: CreateTime、UpdateTime自动维护
- **访问控制**: 基于Role的权限控制

### 敏感数据处理
- **密码哈希**: 使用ASP.NET Core Identity PasswordHasher
- **个人信息**: 患者证件号码、联系方式加密存储预留
- **审计日志**: 操作记录通过日志系统记录

## 📈 性能监控

### 关键指标
- **连接池使用率** < 80%
- **查询响应时间** < 2秒
- **数据库CPU使用率** < 70%
- **磁盘空间使用率** < 80%

### 健康检查
- 数据库连接性检查
- 表空间大小监控
- 索引碎片检查
- 慢查询监控

---

**文档版本**: v1.0  
**创建时间**: 2025-09-01  
**维护者**: UltraThink项目组  
**更新状态**: 基于现有AppDbContext创建完成