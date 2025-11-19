## 🎯 项目概述

LYBT.Entities是系统的数据实体核心模块，定义了所有业务核心实体类、基础模型和数据注释规范。作为整个系统的数据结构基础，为业务逻辑、数据访问和接口传输提供统一的类型安全保障。

## 📦 核心功能

- **实体定义**: 8个核心业务实体，覆盖中医诊所完整业务流程
- **基础模型**: BaseEntity通用基类，提供统一的ID、时间戳、软删除支持
- **数据验证**: 完整的Data Annotations验证规则，确保数据完整性
- **EF映射**: 无缝集成Entity Framework Core，自动生成数据库结构
- **类型安全**: 强类型实体确保编译时类型检查和运行时安全

## 🏗️ 核心实体架构

### 8个业务核心实体

| 实体类 | 功能描述 | 关系 | 状态 |
| ------------------------- | ---------------------- | ---------------------- | ---- |
| **UserModel** | 用户账户信息(Admin/Doctor角色) | 1:N → MedicalCase |  完成 |
| **PatientModel** | 患者档案基本信息 | 1:N → MedicalCase |  完成 |
| **MedicalCaseModel** | 医疗案例(看诊流程容器) | 1:1 → Consultation |  完成 |
| **ConsultationModel** | 看诊诊断(中医四诊记录) | 1:0..1 → Prescription |  完成 |
| **PrescriptionModel** | 处方管理(中药配方) | N:M → Herbs |  完成 |
| **PrescriptionItemModel** | 处方条目(药材用量) | N:1 → Prescription |  完成 |
| **HerbModel** | 中药材信息管理 | 1:N → PrescriptionItem |  完成 |
| **FormulaModel** | 验方模板管理 | 1:N → FormulaHerbItem |  完成 |

### 支持实体

| 实体类 | 功能描述 | 关系 | 状态 |
| -------------------- | ------ | ------------- | ---- |
| **FormulaHerbItem** | 验方药材条目 | N:1 → Formula |  完成 |
| **AuthSessionModel** | 用户会话管理 | N:1 → User |  完成 |
| **AdminSecretModel** | 管理员密钥表 | 独立表 |  完成 |

## 🔗 实体关系架构

### 核心业务流程关系

```
UserModel (用户/医生)
    ↓ 1:N
PatientModel (患者档案)
    ↓ 1:N  
MedicalCaseModel (医疗案例/看诊会话)
    ↓ 1:1
ConsultationModel (诊断记录/四诊)
    ↓ 1:0..1 (可选开方)
PrescriptionModel (处方)
    ↓ 1:N
PrescriptionItemModel (处方条目)
    ↓ N:1
HerbModel (中药材)

验方支持流程:
FormulaModel (验方模板)
    ↓ 1:N
FormulaHerbItem (验方药材)
    ↓ 引用关系
PrescriptionModel (基于验方开方)
```

### 关系说明

- **用户 → 医案**: 一对多，医生可以处理多个医案
- **患者 → 医案**: 一对多，患者可以有多次就诊记录(支持复诊)
- **医案 → 诊断**: 一对一，每个医案对应一次完整诊断记录
- **诊断 → 处方**: 一对零一，诊断后可选择开具处方
- **处方 → 条目**: 一对多，处方包含多个药材条目
- **条目 → 药材**: 多对一，每个条目对应一种药材及用量
- **验方 → 药材**: 一对多，验方模板包含标准药材配置

## 🏛️ 基础设施架构

### BaseEntity基类

```csharp
public abstract class BaseEntity
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public DateTime? UpdatedAt { get; set; }

    public Guid? CreatedBy { get; set; }

    public Guid? UpdatedBy { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = new byte[8];

    public bool IsDeleted { get; set; } = false;
}
```

### 核心枚举定义

```csharp
// 用户角色 (RBAC权限控制)
public enum UserRole
{
    Admin = 1,      // 系统管理员(全权限)
    Doctor = 2      // 医生(诊疗权限)
}

// 用户状态
public enum UserStatus
{
    Active = 1,     // 激活
    Inactive = 2,   // 停用
    Locked = 3      // 锁定
}

// 医案状态 (完整状态机)
public enum MedicalCaseStatus
{
    Registered = 1,     // 已登记(初始状态)
    InProgress = 2,     // 诊疗中
    Completed = 3,      // 已完成
    Cancelled = 4       // 已取消
}

// 处方状态
public enum PrescriptionStatus
{
    Draft = 1,          // 草稿(可编辑)
    Confirmed = 2,      // 已确认(不可编辑)
    Dispensed = 3       // 已配药(完成)
}

// 性别
public enum Gender
{
    Male = 1,       // 男
    Female = 2,     // 女
    Other = 3       // 其他
}
```

## 📊 数据库映射

### EF Core DbContext配置

```csharp
// 在AppDbContext中的DbSet定义
public DbSet<UserModel> Users { get; set; }
public DbSet<PatientModel> Patients { get; set; }
public DbSet<MedicalCaseModel> MedicalCases { get; set; }
public DbSet<ConsultationModel> Consultations { get; set; }
public DbSet<PrescriptionModel> Prescriptions { get; set; }
public DbSet<PrescriptionItemModel> PrescriptionItems { get; set; }
public DbSet<HerbModel> Herbs { get; set; }
public DbSet<FormulaModel> Formulas { get; set; }
public DbSet<FormulaHerbItem> FormulaHerbItems { get; set; }
public DbSet<AuthSessionModel> AuthSessions { get; set; }
public DbSet<AdminSecretModel> AdminSecrets { get; set; }
```

### 数据库表映射

| 实体类 | 数据库表名 | 主键 | 说明 |
| --------------------- | ----------------- | -------- | ----- |
| UserModel | Users | Id(Guid) | 用户账户表 |
| PatientModel | Patients | Id(Guid) | 患者档案表 |
| MedicalCaseModel | MedicalCases | Id(Guid) | 医疗案例表 |
| ConsultationModel | Consultations | Id(Guid) | 诊断记录表 |
| PrescriptionModel | Prescriptions | Id(Guid) | 处方主表 |
| PrescriptionItemModel | PrescriptionItems | Id(Guid) | 处方明细表 |
| HerbModel | Herbs | Id(Guid) | 药材信息表 |
| FormulaModel | Formulas | Id(Guid) | 验方模板表 |

## 🧪 数据验证架构

### 完整验证示例

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
    [StringLength(20, ErrorMessage = "手机号码长度不能超过20字符")]
    public string? PhoneNumber { get; set; }

    [StringLength(500, ErrorMessage = "地址长度不能超过500字符")]
    public string? Address { get; set; }
}
```

### 中医特色字段

```csharp
public class ConsultationModel : BaseEntity
{
    [Required(ErrorMessage = "主诉不能为空")]
    [StringLength(500, ErrorMessage = "主诉长度不能超过500字符")]
    public string ChiefComplaint { get; set; } = string.Empty;

    [StringLength(1000, ErrorMessage = "现病史长度不能超过1000字符")]
    public string? PresentIllness { get; set; }

    // 中医四诊
    [StringLength(500, ErrorMessage = "望诊记录长度不能超过500字符")]
    public string? Inspection { get; set; }  // 望诊

    [StringLength(500, ErrorMessage = "闻诊记录长度不能超过500字符")]
    public string? Auscultation { get; set; }  // 闻诊

    [StringLength(500, ErrorMessage = "问诊记录长度不能超过500字符")]
    public string? Inquiry { get; set; }  // 问诊

    [StringLength(500, ErrorMessage = "切诊记录长度不能超过500字符")]
    public string? Palpation { get; set; }  // 切诊

    // 中医诊断
    [StringLength(200, ErrorMessage = "中医诊断长度不能超过200字符")]
    public string? TcmDiagnosis { get; set; }

    [StringLength(200, ErrorMessage = "治法长度不能超过200字符")]
    public string? TreatmentMethod { get; set; }
}
```

## 🔧 使用指南

### 新增实体步骤

1. **创建实体类**
 
   ```csharp
   public class NewEntityModel : BaseEntity
   {
       // 添加业务属性和数据注释
   }
   ```

2. **添加到DbContext**
 
   ```csharp
   public DbSet<NewEntityModel> NewEntities { get; set; }
   ```

3. **生成数据库迁移**
 
   ```bash
   dotnet ef migrations add AddNewEntity --project LYBT.Infrastructure --startup-project LYBT.WebAPI
   ```

4. **应用迁移**
 
   ```bash
   dotnet ef database update --project LYBT.Infrastructure --startup-project LYBT.WebAPI
   ```

### 实体设计原则

- **单一职责**: 每个实体类只表示一个业务概念
- **数据完整性**: 通过约束和验证保证数据质量
- **中医特色**: 体现中医四诊合参的诊疗特点
- **可扩展性**: 预留扩展字段，支持业务发展
- **性能考虑**: 避免过深的导航属性嵌套

## 📈 性能优化

### 查询优化

- **延迟加载**: 默认使用延迟加载减少查询开销
- **Include预加载**: 对必需的关联数据使用Include
- **投影查询**: 使用Select投影减少数据传输
- **分页查询**: 对大量数据使用Skip/Take分页

### 索引策略

```sql
-- 常用查询字段创建索引
CREATE INDEX IX_Patients_PhoneNumber ON Patients(PhoneNumber);
CREATE INDEX IX_MedicalCases_PatientId ON MedicalCases(PatientId);
CREATE INDEX IX_MedicalCases_DoctorId ON MedicalCases(DoctorId);
CREATE INDEX IX_MedicalCases_Status ON MedicalCases(Status);
CREATE INDEX IX_Prescriptions_MedicalCaseId ON Prescriptions(MedicalCaseId);
```

## 🎯 分层架构特点

**适合小型中医诊所(<20人)的精简设计**:

-  **实体精简**: 8个核心实体覆盖完整业务流程，避免过度设计
-  **关系清晰**: 业务关系映射直观，易于理解和维护
-  **中医特色**: 四诊合参、辨证论治的中医诊疗流程支持
-  **类型安全**: 强类型枚举和验证规则，确保数据一致性
-  **扩展友好**: 基础架构完善，支持业务功能逐步扩展

## 📚 相关文档

- [LYBT.Infrastructure](../LYBT.Infrastructure/README.md) - 基础设施层(包含AppDbContext)
- [数据库迁移指南](../LYBT.Infrastructure/README.md#🗃️-数据库迁移管理) - EF Core迁移操作指南
- [API规范文档](../../Services/LYBT.WebAPI/README.md) - RESTful API设计规范

---

> 📌 **开发提醒**: 修改实体结构后务必生成数据库迁移并同步更新对应的DTO模型
> 🎆 **成果**: 实体模型设计简洁高效，完美支持中医诊所核心业务流程

## 📦 项目结构

```
LYBT.Entities/
├── Models/                     # 核心业务实体
│   ├── UserModel.cs           # 用户账户实体
│   ├── PatientModel.cs        # 患者档案实体
│   ├── MedicalCaseModel.cs    # 医疗案例实体
│   ├── ConsultationModel.cs   # 诊疗记录实体
│   ├── PrescriptionModel.cs   # 处方实体
│   ├── PrescriptionItemModel.cs # 处方条目实体
│   ├── HerbModel.cs           # 中药材实体
│   └── FormulaModel.cs        # 验方模板实体
├── Enums/                      # 枚举定义
│   ├── UserRole.cs            # 用户角色枚举
│   ├── UserStatus.cs          # 用户状态枚举
│   ├── MedicalCaseStatus.cs   # 医案状态枚举
│   ├── PrescriptionStatus.cs  # 处方状态枚举
│   └── Gender.cs              # 性别枚举
└── Infrastructure/             # 基础设施与基类
    └── BaseEntity.cs          # 所有实体的通用基类
```

## 🛠 技术栈

- **.NET 8**: 目标框架
- **Entity Framework Core 8**: ORM框架，用于定义数据实体与数据库的映射关系
- **Data Annotations**: 数据验证特性

##  快速开始

此项目是一个类库，不包含可执行文件。可以通过解决方案或以下命令进行构建：

```bash
# 还原解决方案依赖
dotnet restore LYBT.All.sln

# 构建此项目
dotnet build src/Server/Core/LYBT.Entities/LYBT.Entities.csproj
```

## 🔌 API 接口

此项目为数据实体层，不直接对外提供任何API接口。它定义的数据结构被业务服务层和数据访问层使用。
