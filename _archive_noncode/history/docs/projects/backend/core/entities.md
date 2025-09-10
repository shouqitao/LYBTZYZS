# LYBT.Entities 项目文档

## 📋 项目概述

**LYBT.Entities**是系统的实体模型层，定义了凌隐宝堂中医诊所系统的所有核心业务实体和数据结构。作为领域模型的核心载体，Entities项目为整个系统提供了统一、标准化的数据模型定义，确保前后端以及各业务模块之间的数据一致性。

### 项目职责
- **核心实体定义**: 定义8个核心业务实体的完整数据结构和约束
- **实体关系映射**: 配置实体间的主外键关系和导航属性
- **数据验证规则**: 定义字段级别的验证注解和业务规则约束
- **枚举类型管理**: 统一管理系统中所有枚举类型和常量定义
- **审计字段标准**: 为所有实体提供统一的审计字段结构

### 在系统中的位置
Entities作为领域层的核心，被Infrastructure的AppDbContext直接映射，同时为所有8个业务模块提供标准的实体定义。它是连接数据库物理存储和业务逻辑的重要桥梁。

### 关键业务价值
- **数据标准化**: 确保整个系统的数据结构一致性和完整性
- **类型安全**: 通过强类型实体减少运行时数据错误
- **业务语义**: 实体名称和结构直接反映中医诊疗业务语义
- **扩展友好**: 清晰的实体结构便于后续功能扩展和维护

## 🏗️ 技术架构

### 项目架构设计
Entities采用领域驱动设计(DDD)的实体建模方式：

```
基础实体层 (BaseEntity)
    ↓
核心业务实体层 (8个核心实体)
    ↓
枚举和常量层 (CommonStatus等枚举)
    ↓
验证规则层 (Data Annotations)
```

### 核心技术栈
- **.NET 8.0**: 现代C#语言特性，记录类型和可空引用类型
- **System.ComponentModel.DataAnnotations**: 实体验证注解
- **System.ComponentModel.DataAnnotations.Schema**: 数据库映射配置
- **Entity Framework Core**: 实体映射和关系配置支持

### 依赖项目列表
**直接依赖**:
- 无外部项目依赖（纯实体定义）

**被依赖项目**:
- `LYBT.Infrastructure` - 通过AppDbContext映射实体
- `LYBT.Shared.Models` - DTO类型转换映射
- 所有8个业务模块 - 实体类型引用

### 设计模式采用
- **Entity Pattern**: 每个业务概念对应独立实体类
- **Value Object Pattern**: 枚举类型作为值对象使用
- **Base Class Pattern**: BaseEntity提供公共字段和行为
- **Repository Pattern**: 为EF Core Repository提供实体定义

## 🎯 功能规范

### 必须实现的功能清单

#### 1. 核心实体定义(8个)
- ✅ **UserModel**: 系统用户实体(医生、管理员)
- ✅ **PatientModel**: 患者档案实体
- ✅ **MedicalCaseModel**: 医疗案例实体(诊疗流程容器)
- ✅ **ConsultationModel**: 看诊诊断实体(中医四诊记录)
- ✅ **PrescriptionModel**: 处方主实体
- ✅ **PrescriptionItemModel**: 处方药材明细实体
- ✅ **HerbModel**: 中药材实体
- ✅ **FormulaTemplateModel**: 验方模板实体

#### 2. 系统支撑实体
- ✅ **AdminSecretModel**: 超级管理员密钥实体
- ✅ **BaseEntity**: 实体基类(审计字段)

#### 3. 枚举类型定义
- ✅ **CommonStatus**: 通用状态枚举(Active/Inactive/Deleted)
- ✅ **UserRole**: 用户角色枚举(Admin/Doctor)
- ✅ **Gender**: 性别枚举(Male/Female/Other)
- ✅ **MedicalCaseStatus**: 医案状态(Registered/InProgress/Completed)

### 实体关系定义规范

#### 核心业务关系
```csharp
// 1:N 关系
Patient → MedicalCase (一个患者多个医案)
MedicalCase → Consultation (一个医案一个诊断记录)
MedicalCase → Prescription (一个医案多个处方)
Prescription → PrescriptionItem (一个处方多个药材)

// 引用关系  
User → MedicalCase (医生创建医案)
Herb → PrescriptionItem (药材应用到处方)
FormulaTemplate → Prescription (验方应用到处方)
```

### 数据模型定义

#### BaseEntity基类
```csharp
public abstract class BaseEntity
{
    [Key]
    public Guid Id { get; set; }
    
    [Required]
    public DateTime CreateTime { get; set; }
    
    public DateTime? UpdateTime { get; set; }
    
    [Required]
    public CommonStatus Status { get; set; } = CommonStatus.Active;
}
```

#### UserModel用户实体
```csharp
public class UserModel : BaseEntity
{
    [Required]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;
    
    [Required]
    [StringLength(100)]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;
    
    [Required]
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;
    
    [Required]
    [StringLength(20)]
    public string FullName { get; set; } = string.Empty;
    
    [StringLength(20)]
    public string? Phone { get; set; }
    
    [Required]
    public UserRole Role { get; set; }
    
    public bool IsFirstLogin { get; set; } = true;
    
    // 导航属性
    public virtual ICollection<MedicalCaseModel> MedicalCases { get; set; } = new List<MedicalCaseModel>();
}
```

#### PatientModel患者实体
```csharp
public class PatientModel : BaseEntity
{
    [Required]
    [StringLength(20)]
    public string Name { get; set; } = string.Empty;
    
    [Required]
    public Gender Gender { get; set; }
    
    [Required]
    public DateTime DateOfBirth { get; set; }
    
    [StringLength(20)]
    public string? Phone { get; set; }
    
    [StringLength(18)]
    public string? IdNumber { get; set; }
    
    [StringLength(200)]
    public string? Address { get; set; }
    
    [StringLength(20)]
    public string? EmergencyContactName { get; set; }
    
    [StringLength(20)]
    public string? EmergencyContactPhone { get; set; }
    
    [StringLength(500)]
    public string? Remark { get; set; }
    
    // 导航属性
    public virtual ICollection<MedicalCaseModel> MedicalCases { get; set; } = new List<MedicalCaseModel>();
}
```

#### MedicalCaseModel医案实体
```csharp
public class MedicalCaseModel : BaseEntity
{
    [Required]
    public Guid PatientId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [StringLength(100)]
    public string? CaseNumber { get; set; }
    
    [Required]
    public DateTime VisitDate { get; set; }
    
    [Required]
    public MedicalCaseStatus CaseStatus { get; set; } = MedicalCaseStatus.Registered;
    
    [StringLength(500)]
    public string? ChiefComplaint { get; set; }
    
    [StringLength(1000)]
    public string? Remark { get; set; }
    
    // 导航属性
    [ForeignKey("PatientId")]
    public virtual PatientModel Patient { get; set; } = null!;
    
    [ForeignKey("UserId")]
    public virtual UserModel User { get; set; } = null!;
    
    public virtual ConsultationModel? Consultation { get; set; }
    public virtual ICollection<PrescriptionModel> Prescriptions { get; set; } = new List<PrescriptionModel>();
}
```

#### ConsultationModel诊断实体
```csharp
public class ConsultationModel : BaseEntity
{
    [Required]
    public Guid MedicalCaseId { get; set; }
    
    [StringLength(500)]
    public string? InitialComplaint { get; set; }
    
    [StringLength(1000)]
    public string? PresentIllnessHistory { get; set; }
    
    [StringLength(500)]
    public string? PastMedicalHistory { get; set; }
    
    // 中医四诊
    [StringLength(500)]
    public string? Inspection { get; set; }  // 望诊
    
    [StringLength(500)]
    public string? Auscultation { get; set; } // 闻诊
    
    [StringLength(500)]
    public string? Inquiry { get; set; }     // 问诊
    
    [StringLength(500)]
    public string? Palpation { get; set; }   // 切诊
    
    [StringLength(500)]
    public string? TCMDiagnosis { get; set; } // 中医诊断
    
    [StringLength(500)]
    public string? Treatment { get; set; }    // 治法
    
    [StringLength(1000)]
    public string? Note { get; set; }
    
    // 导航属性
    [ForeignKey("MedicalCaseId")]
    public virtual MedicalCaseModel MedicalCase { get; set; } = null!;
}
```

#### PrescriptionModel处方实体
```csharp
public class PrescriptionModel : BaseEntity
{
    [Required]
    public Guid MedicalCaseId { get; set; }
    
    [Required]
    public Guid PatientId { get; set; }
    
    [StringLength(500)]
    public string? Indication { get; set; }
    
    [Range(1, 100)]
    public int DosageCount { get; set; } = 7;
    
    [StringLength(200)]
    public string? Advice { get; set; }
    
    [StringLength(100)]
    public string? FormulaSource { get; set; }
    
    [Column(TypeName = "decimal(10,2)")]
    public decimal TotalPrice { get; set; }
    
    // 导航属性
    [ForeignKey("MedicalCaseId")]
    public virtual MedicalCaseModel MedicalCase { get; set; } = null!;
    
    [ForeignKey("PatientId")]
    public virtual PatientModel Patient { get; set; } = null!;
    
    public virtual ICollection<PrescriptionItemModel> PrescriptionItems { get; set; } = new List<PrescriptionItemModel>();
}
```

#### HerbModel药材实体
```csharp
public class HerbModel : BaseEntity
{
    [Required]
    [StringLength(50)]
    public string Name { get; set; } = string.Empty;
    
    [StringLength(50)]
    public string? PinYinCode { get; set; }
    
    [StringLength(50)]
    public string? Origin { get; set; }
    
    [StringLength(30)]
    public string? Spec { get; set; }
    
    [Required]
    [StringLength(10)]
    public string Unit { get; set; } = "克";
    
    [Required]
    [Column(TypeName = "decimal(8,2)")]
    public decimal Price { get; set; }
    
    [Column(TypeName = "decimal(8,2)")]
    public decimal? CostPrice { get; set; }
    
    [StringLength(500)]
    public string? Effect { get; set; }
    
    [StringLength(200)]
    public string? Usage { get; set; }
    
    [StringLength(500)]
    public string? Remark { get; set; }
    
    // 导航属性
    public virtual ICollection<PrescriptionItemModel> PrescriptionItems { get; set; } = new List<PrescriptionItemModel>();
}
```

### 业务规则约束
1. **主键规则**: 所有实体使用Guid类型主键，确保分布式唯一性
2. **审计规则**: 所有实体继承BaseEntity，自动包含审计字段
3. **软删除**: 使用Status字段标记删除，不进行物理删除
4. **外键约束**: 所有外键字段必须有对应的导航属性
5. **字符串长度**: 所有字符串字段必须定义合理的最大长度
6. **必填字段**: 关键业务字段使用[Required]注解强制约束
7. **枚举约束**: 状态字段统一使用枚举类型，避免魔法数字

## 📋 开发规范

### 代码结构要求
```
src/Server/Core/LYBT.Entities/
├── Models/
│   ├── Users/
│   │   └── UserModel.cs              # 用户实体
│   ├── Patients/
│   │   └── PatientModel.cs           # 患者实体
│   ├── MedicalCases/
│   │   ├── MedicalCaseModel.cs       # 医案实体
│   │   └── ConsultationModel.cs      # 诊断实体
│   ├── Prescriptions/
│   │   ├── PrescriptionModel.cs      # 处方实体
│   │   └── PrescriptionItemModel.cs  # 处方明细实体
│   ├── Herbs/
│   │   └── HerbModel.cs              # 药材实体
│   ├── Formulas/
│   │   └── FormulaTemplateModel.cs   # 验方实体
│   └── AdminSecrets/
│       └── AdminSecretModel.cs       # 管理员密钥实体
├── Base/
│   └── BaseEntity.cs                 # 实体基类
└── Enums/
    ├── CommonStatus.cs               # 通用状态枚举
    ├── UserRole.cs                   # 用户角色枚举
    ├── Gender.cs                     # 性别枚举
    └── MedicalCaseStatus.cs          # 医案状态枚举
```

### 命名规范
- **实体类**: PascalCase + Model后缀 (UserModel, PatientModel)
- **属性名**: PascalCase，符合业务语义 (FullName, DateOfBirth)
- **外键字段**: 相关实体名 + Id (PatientId, UserId)
- **枚举类型**: PascalCase，无后缀 (CommonStatus, UserRole)
- **枚举值**: PascalCase，语义明确 (Active, Inactive)
- **导航属性**: PascalCase，复数形式表示集合 (MedicalCases)

### 质量标准
- **数据注解**: 所有实体必须有完整的验证注解
- **可空性**: 正确使用可空引用类型，明确区分必填和可选字段
- **字符串长度**: 根据业务需求合理设置StringLength限制
- **外键完整性**: 外键字段和导航属性必须配对定义
- **枚举使用**: 状态和分类字段优先使用枚举而非字符串
- **注释文档**: 复杂业务字段添加XML注释说明

### 测试要求
- **实体验证测试**: 验证数据注解的正确性
- **关系映射测试**: 测试实体间导航属性的正确性
- **边界值测试**: 测试字符串长度和数值范围限制
- **枚举测试**: 确保枚举值的业务语义正确

## 🔌 集成接口

### 对外提供的接口
Entities项目作为纯数据模型层，通过以下方式对外提供实体定义：

#### 实体类型导出
```csharp
// 其他项目引用实体类型
using LYBT.Entities.Models.Users;
using LYBT.Entities.Models.Patients;
using LYBT.Entities.Enums;

// EF Core DbContext中使用
public DbSet<UserModel> Users { get; set; }
public DbSet<PatientModel> Patients { get; set; }
```

### 依赖的外部接口
- **System.ComponentModel.DataAnnotations**: .NET数据验证注解
- **System.ComponentModel.DataAnnotations.Schema**: EF Core映射注解
- **Microsoft.EntityFrameworkCore**: EF Core导航属性支持

### 数据传输格式
Entities定义了以下核心数据结构：

#### 用户实体结构
```json
{
    "id": "guid",
    "username": "string(50)",
    "email": "string(100)",
    "fullName": "string(20)",
    "role": "Admin|Doctor",
    "status": "Active|Inactive|Deleted",
    "createTime": "datetime",
    "updateTime": "datetime?"
}
```

#### 患者实体结构
```json
{
    "id": "guid",
    "name": "string(20)",
    "gender": "Male|Female|Other",
    "dateOfBirth": "datetime",
    "phone": "string(20)?",
    "idNumber": "string(18)?",
    "address": "string(200)?",
    "status": "Active|Inactive|Deleted"
}
```

### 错误处理规范
实体层通过数据注解提供验证约束：
- **[Required]**: 字段不能为空
- **[StringLength]**: 字符串长度限制  
- **[EmailAddress]**: 邮箱格式验证
- **[Range]**: 数值范围验证

## ⚙️ 配置管理

### 配置项定义
Entities项目本身不包含配置，但为其他项目提供配置支持：

#### EF Core映射配置支持
```csharp
// 在AppDbContext中配置实体映射
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // 用户实体配置
    modelBuilder.Entity<UserModel>(entity =>
    {
        entity.HasIndex(e => e.Username).IsUnique();
        entity.HasIndex(e => e.Email).IsUnique();
    });
    
    // 患者实体配置
    modelBuilder.Entity<PatientModel>(entity =>
    {
        entity.HasIndex(e => e.Phone);
        entity.HasIndex(e => e.IdNumber);
    });
    
    // 关系配置
    modelBuilder.Entity<MedicalCaseModel>()
        .HasOne(m => m.Patient)
        .WithMany(p => p.MedicalCases)
        .HasForeignKey(m => m.PatientId)
        .OnDelete(DeleteBehavior.Restrict);
}
```

### 环境变量要求
无直接环境变量依赖，通过EF Core间接使用：
- **数据库连接字符串**: 影响实体的物理存储
- **环境标识**: 影响数据验证的严格程度

### 部署配置说明
- **数据库迁移**: 实体变更会生成对应的数据库迁移脚本
- **索引优化**: 生产环境需要为查询频繁的字段创建索引
- **约束检查**: 生产环境启用完整的数据库约束检查

## 🧪 测试规范

### 单元测试要求
- **测试框架**: xUnit + FluentAssertions
- **测试范围**: 实体验证规则、属性访问、枚举值
- **测试数据**: 使用Builder模式创建测试实体

#### 示例测试结构
```csharp
public class UserModelTests
{
    [Fact]
    public void UserModel_RequiredFields_ShouldNotBeNull()
    {
        // Arrange & Act
        var user = new UserModel();
        var context = new ValidationContext(user);
        var results = new List<ValidationResult>();
        
        // Assert
        Validator.TryValidateObject(user, context, results, true)
            .Should().BeFalse();
        results.Should().Contain(r => r.MemberNames.Contains("Username"));
    }
    
    [Theory]
    [InlineData("test@example.com", true)]
    [InlineData("invalid-email", false)]
    public void UserModel_EmailValidation_ShouldWork(string email, bool isValid)
    {
        // Arrange
        var user = CreateValidUser();
        user.Email = email;
        
        // Act & Assert
        var context = new ValidationContext(user);
        var results = new List<ValidationResult>();
        var valid = Validator.TryValidateObject(user, context, results, true);
        
        valid.Should().Be(isValid);
    }
}
```

### 集成测试要求
- **EF Core映射测试**: 确保实体能正确映射到数据库
- **关系完整性测试**: 测试外键约束和导航属性
- **数据迁移测试**: 验证实体变更对应的迁移脚本

### 测试覆盖率目标
- **实体类覆盖率**: >90%
- **验证规则覆盖率**: 100%
- **枚举类覆盖率**: 100%
- **导航属性覆盖率**: >85%

### 测试数据准备
```csharp
public static class EntityTestDataBuilder
{
    public static UserModel CreateValidUser(string username = "testuser")
    {
        return new UserModel
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = $"{username}@test.com",
            PasswordHash = "hashed-password",
            FullName = "测试用户",
            Role = UserRole.Doctor,
            Status = CommonStatus.Active,
            CreateTime = DateTime.UtcNow
        };
    }
    
    public static PatientModel CreateValidPatient(string name = "测试患者")
    {
        return new PatientModel
        {
            Id = Guid.NewGuid(),
            Name = name,
            Gender = Gender.Male,
            DateOfBirth = DateTime.Now.AddYears(-30),
            Phone = "13800138000",
            Status = CommonStatus.Active,
            CreateTime = DateTime.UtcNow
        };
    }
}
```

## 🚀 部署说明

### 构建要求
- **.NET 8.0 SDK**: 编译实体类和验证注解
- **无特殊运行时要求**: 纯模型定义，无依赖库

### 部署步骤
Entities作为依赖库自动包含在主应用中：

#### 1. 编译验证
```bash
# 编译检查
dotnet build LYBT.Entities.csproj

# 验证输出
dotnet pack --no-build --verbosity normal
```

#### 2. 数据库迁移准备
```bash
# 生成迁移（当实体结构变更时）
dotnet ef migrations add UpdateEntityStructure \
    --project LYBT.Infrastructure \
    --startup-project LYBT.WebAPI

# 更新数据库结构
dotnet ef database update \
    --project LYBT.Infrastructure \
    --startup-project LYBT.WebAPI
```

### 环境依赖
- **运行时**: .NET 8.0 Runtime或更高版本
- **数据库**: SQL Server支持对应的数据类型映射

### 运行监控

#### 数据完整性监控
- **外键约束**: 监控数据库外键约束违反情况
- **数据验证**: 监控实体验证失败的异常日志
- **枚举值检查**: 确保枚举字段值在有效范围内

#### 性能监控指标
- **实体序列化性能**: 监控大量数据的序列化时间
- **查询性能**: 监控复杂导航属性查询的执行时间
- **内存使用**: 监控大量实体对象的内存占用

## 📚 相关文档

### 相关项目文档链接
- [LYBT.Infrastructure项目文档](./infrastructure.md) - 数据访问和EF Core映射配置
- [LYBT.Shared.Models项目文档](../../shared/shared-models.md) - DTO和实体映射关系
- [后端业务模块文档](../modules/) - 各业务模块如何使用实体

### API文档链接
- [实体验证规范](../../../api/entity-validation.md) - 数据验证注解使用指南
- [数据库设计文档](../../../database/database-design.md) - 实体对应的数据库表结构

### 技术规范引用
- [领域模型设计指南](../../../development/domain-model-design.md) - 实体建模最佳实践
- [EF Core实体配置](../../../development/ef-core-entity-configuration.md) - 实体映射配置规范
- [数据注解使用规范](../../../development/data-annotations-guide.md) - 验证注解标准化使用

---

**文档版本**: v1.0  
**创建日期**: 2025-09-01  
**最后更新**: 2025-09-01  
**维护者**: UltraThink项目组  
**审核状态**: ✅ 已审核通过