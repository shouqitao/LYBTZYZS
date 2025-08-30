# LYBT.Entities

> **数据实体模块**  
> 系统核心数据模型和实体类定义

## 🎯 模块概述

LYBT.Entities是系统的数据实体模块，定义了所有业务核心实体类、基础模型和数据注释规范，是业务逻辑、数据访问和接口传输的基础结构层。

## 📦 核心功能

- **实体定义**: 定义所有业务核心实体类，如用户、患者、医案、诊断等
- **基础模型**: 提供BaseEntity等通用基类和接口
- **数据验证**: 通过数据注释约定字段类型、必填/非必填规则
- **EF映射**: 为Entity Framework Core提供数据库表结构映射
- **类型安全**: 强类型实体确保编译时类型检查

## 🏗️ 核心实体

### 8个业务核心实体

| 实体类 | 功能描述 | 状态 |
|--------|----------|------|
| **UserModel** | 用户账户信息(Admin/Doctor) | ✅ 完成 |
| **PatientModel** | 患者档案基本信息 | ✅ 完成 |
| **MedicalCaseModel** | 医疗案例(看诊流程容器) | ✅ 完成 |
| **ConsultationModel** | 看诊诊断(中医四诊记录) | ✅ 完成 |
| **PrescriptionModel** | 处方管理(中药配方) | ✅ 完成 |
| **HerbModel** | 中药材信息管理 | ✅ 完成 |
| **FormulaTemplateModel** | 验方模板管理 | ✅ 完成 |
| **AdminSecretModel** | 管理员密钥表 | ✅ 完成 |

### 基础设施实体

```csharp
// 基础实体类
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime? UpdateTime { get; set; }
    public bool IsDeleted { get; set; }
}

// 审计实体
public abstract class AuditableEntity : BaseEntity
{
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
}
```

## 🔗 实体关系

### 核心业务关系

```
PatientModel (患者)
    ↓ 1:N
MedicalCaseModel (医案)
    ↓ 1:1
ConsultationModel (诊断)
    ↓ 1:0..1
PrescriptionModel (处方)
    ↓ N:M
HerbModel (药材)

FormulaTemplateModel (验方) ←→ PrescriptionModel (引用关系)
UserModel (用户) → MedicalCaseModel (医生关联)
```

### 关系说明
- **患者 ↔ 医案**: 一对多关系，患者可以有多次就诊记录
- **医案 ↔ 诊断**: 一对一关系，每个医案对应一次诊断记录
- **诊断 ↔ 处方**: 一对零一关系，诊断后可选择开具处方
- **处方 ↔ 药材**: 多对多关系，处方包含多种药材及用量
- **验方 ↔ 处方**: 引用关系，处方可以基于验方模板创建

## 🏛️ 技术实现

### 数据注释

```csharp
public class PatientModel : BaseEntity
{
    [Required(ErrorMessage = "患者姓名不能为空")]
    [StringLength(50, ErrorMessage = "患者姓名长度不能超过50字符")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "性别不能为空")]
    public Gender Gender { get; set; }

    [Range(0, 150, ErrorMessage = "年龄必须在0-150之间")]
    public int Age { get; set; }

    [Phone(ErrorMessage = "手机号码格式不正确")]
    [StringLength(20)]
    public string? PhoneNumber { get; set; }
}
```

### 枚举定义

```csharp
// 用户角色
public enum UserRole
{
    Admin = 1,      // 系统管理员
    Doctor = 2      // 医生
}

// 医案状态
public enum MedicalCaseStatus
{
    Registered = 1,     // 已登记
    InProgress = 2,     // 诊疗中
    Completed = 3,      // 已完成
    Cancelled = 4       // 已取消
}

// 处方状态
public enum PrescriptionStatus
{
    Draft = 1,          // 草稿
    Confirmed = 2,      // 已确认
    Dispensed = 3       // 已配药
}
```

## 📊 数据库映射

### EF Core配置

实体类通过Entity Framework Core自动映射到数据库表：

```csharp
// 在AppDbContext中配置
public DbSet<UserModel> Users { get; set; }
public DbSet<PatientModel> Patients { get; set; }
public DbSet<MedicalCaseModel> MedicalCases { get; set; }
public DbSet<ConsultationModel> Consultations { get; set; }
public DbSet<PrescriptionModel> Prescriptions { get; set; }
public DbSet<HerbModel> Herbs { get; set; }
public DbSet<FormulaTemplateModel> FormulaTemplates { get; set; }
```

### 表命名约定

| 实体类 | 数据库表名 | 说明 |
|--------|------------|------|
| UserModel | Users | 用户表 |
| PatientModel | Patients | 患者表 |
| MedicalCaseModel | MedicalCases | 医案表 |
| ConsultationModel | Consultations | 诊断表 |
| PrescriptionModel | Prescriptions | 处方表 |
| HerbModel | Herbs | 药材表 |

## 🧪 数据验证

### 验证规则

- **必填字段**: 使用`[Required]`注解标记
- **字符串长度**: 使用`[StringLength]`限制最大长度
- **数值范围**: 使用`[Range]`限制数值区间
- **格式验证**: 使用`[Phone]`、`[Email]`等格式验证
- **自定义验证**: 实现`IValidatableObject`接口

### 示例验证

```csharp
public class ConsultationModel : BaseEntity, IValidatableObject
{
    [Required(ErrorMessage = "主诉不能为空")]
    [StringLength(500, ErrorMessage = "主诉长度不能超过500字符")]
    public string ChiefComplaint { get; set; } = string.Empty;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(ChiefComplaint))
        {
            yield return new ValidationResult("主诉不能为空白字符", new[] { nameof(ChiefComplaint) });
        }
    }
}
```

## 🔧 使用指南

### 新增实体步骤

1. 继承`BaseEntity`或`AuditableEntity`
2. 添加业务属性和数据注释
3. 在`AppDbContext`中添加`DbSet<T>`
4. 生成并应用数据库迁移
5. 创建对应的DTO和映射配置

### 实体设计原则

- **单一职责**: 每个实体类只表示一个业务概念
- **数据完整性**: 通过约束和验证保证数据质量
- **可扩展性**: 预留扩展字段，支持业务发展
- **性能考虑**: 避免过深的导航属性嵌套

## 📈 性能优化

- **延迟加载**: 默认使用延迟加载减少查询开销
- **索引优化**: 为常用查询字段创建数据库索引
- **批量操作**: 使用EF Core的批量更新功能
- **查询投影**: 使用Select投影减少数据传输

---

> 📌 **开发提醒**: 修改实体结构后务必生成数据库迁移并同步更新对应的DTO模型

