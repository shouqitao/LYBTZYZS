# LYBT.Entities 命名规范标准

> **文档类型**: 开发规范  
> **创建日期**: 2025-01-19  
> **适用范围**: LYBT.Entities项目所有实体类

## 📋 总体规范

### 实体类命名

**规则**: 所有实体类必须以 `Model` 后缀结尾，与文件名保持一致

```csharp
// ✅ 正确示例
public class UserModel        // 文件名: UserModel.cs
public class PatientModel     // 文件名: PatientModel.cs  
public class MedicalCaseModel // 文件名: MedicalCaseModel.cs

// ❌ 错误示例  
public class User      // 文件名: UserModel.cs (类名与文件名不符)
public class Patient   // 文件名: PatientModel.cs (类名与文件名不符)
```

### 字段命名统一标准

#### 时间字段命名

```csharp
public DateTime CreatedAt { get; set; }      // 创建时间
public DateTime? UpdatedAt { get; set; }     // 更新时间
public DateTime? LastLoginAt { get; set; }   // 最后登录时间
public DateTime? LastVisitAt { get; set; }   // 最后就诊时间
public DateTime? ExpiresAt { get; set; }     // 过期时间
```

#### 审计字段命名

```csharp
public Guid? CreatedBy { get; set; }    // 创建者ID
public Guid? UpdatedBy { get; set; }    // 更新者ID
public byte[] RowVersion { get; set; }  // 并发控制版本
public bool IsDeleted { get; set; }     // 软删除标记
```

#### 业务状态字段

```csharp
public CommonStatus Status { get; set; }           // 通用状态
public MedicalCaseStatus CaseStatus { get; set; }  // 医案状态
public PrescriptionStatus Status { get; set; }     // 处方状态
```

## 🎯 BaseEntity 继承规范

### 标准继承模式

```csharp
namespace LYBT.Entities.模块名
{
    /// <summary>
    /// 实体描述 - UltraThink v2.0架构标准版
    /// 继承BaseEntity提供统一基础字段和审计功能
    /// </summary>
    [Table("数据库表名")]
    public class 实体名Model : BaseEntity
    {
        // 业务专属字段
        [Required]
        [StringLength(100)]
        [DisplayName("字段中文名")]
        public string PropertyName { get; set; } = string.Empty;
        
        // 导航属性
        public virtual RelatedModel? Related { get; set; }
    }
}
```

### 字段移除清单

**继承BaseEntity后，实体类中需要移除以下重复字段**:

```csharp
// ❌ 需要移除的字段 (BaseEntity已提供)
public Guid Id { get; set; }                    // BaseEntity.Id
public DateTime CreatedTime { get; set; }       // BaseEntity.CreatedAt  
public DateTime CreatedAt { get; set; }         // BaseEntity.CreatedAt
public DateTime? UpdateTime { get; set; }       // BaseEntity.UpdatedAt
public DateTime? UpdatedAt { get; set; }        // BaseEntity.UpdatedAt
public Guid? CreatedBy { get; set; }            // BaseEntity.CreatedBy
public Guid? UpdatedBy { get; set; }            // BaseEntity.UpdatedBy
public byte[] RowVersion { get; set; }          // BaseEntity.RowVersion
public bool IsDeleted { get; set; }             // BaseEntity.IsDeleted
```

## 📊 实体重构清单

### Phase 1: 核心实体 (高优先级)

| 实体文件 | 当前类名 | 目标类名 | 重复字段移除 | 状态 |
|---------|---------|---------|-------------|------|
| `UserModel.cs` | `User` | `UserModel` | Id, CreatedTime, UpdateTime, RowVersion | ⏳ 待重构 |
| `PatientModel.cs` | `Patient` | `PatientModel` | Id, CreatedAt, UpdateTime, RowVersion | ⏳ 待重构 |
| `MedicalCaseModel.cs` | `MedicalCase` | `MedicalCaseModel` | Id | ⏳ 待重构 |

### Phase 2: 诊疗实体 (中优先级)

| 实体文件 | 当前类名 | 目标类名 | 重复字段移除 | 状态 |
|---------|---------|---------|-------------|------|
| `ConsultationModel.cs` | `Consultation` | `ConsultationModel` | Id | ⏳ 待重构 |
| `PrescriptionModel.cs` | `Prescription` | `PrescriptionModel` | Id, RowVersion | ⏳ 待重构 |
| `PrescriptionItem.cs` | `PrescriptionItem` | `PrescriptionItemModel` | 无 | ⏳ 待重构 |

### Phase 3: 药材实体 (低优先级)

| 实体文件 | 当前类名 | 目标类名 | 重复字段移除 | 状态 |
|---------|---------|---------|-------------|------|
| `HerbModel.cs` | `Herb` | `HerbModel` | Id | ⏳ 待重构 |
| `FormulaModel.cs` | `Formula` | `FormulaModel` | 无 | ⏳ 待重构 |
| `FormulaHerbItem.cs` | `FormulaHerbItem` | `FormulaHerbItemModel` | 无 | ⏳ 待重构 |

### Phase 4: 支持实体 (维持现状)

| 实体文件 | 当前类名 | 目标类名 | 说明 | 状态 |
|---------|---------|---------|------|------|
| `AuthSessionModel.cs` | `AuthSessionModel` | `AuthSessionModel` | 已符合命名规范 | ✅ 无需修改 |
| `AdminSecretModel.cs` | `AdminSecretModel` | `AdminSecretModel` | 已符合命名规范 | ✅ 无需修改 |

## 🔧 重构实施指南

### 步骤1: 单个实体重构

```csharp
// 重构前 (以UserModel.cs为例)
namespace LYBT.Entities.Users
{
    [Table("Users")]
    public class User  // ❌ 类名与文件名不符
    {
        [Key] public Guid Id { get; set; }  // ❌ 重复字段
        public DateTime CreatedTime { get; set; }  // ❌ 重复字段
        public DateTime? UpdateTime { get; set; }  // ❌ 重复字段
        public byte[] RowVersion { get; set; }     // ❌ 重复字段
        
        // 业务字段...
        public string Username { get; set; }
    }
}

// 重构后
namespace LYBT.Entities.Users
{
    [Table("Users")]  
    public class UserModel : BaseEntity  // ✅ 继承BaseEntity
    {
        // ✅ 移除重复字段，保留业务字段
        public string Username { get; set; } = string.Empty;
        public string RealName { get; set; } = string.Empty;
        // ... 其他业务字段
    }
}
```

### 步骤2: 配套数据库迁移

```bash
# 生成迁移文件 
dotnet ef migrations add UnifyBaseEntityInheritance --project LYBT.Infrastructure --startup-project LYBT.WebAPI

# 应用迁移
dotnet ef database update --project LYBT.Infrastructure --startup-project LYBT.WebAPI
```

### 步骤3: 更新引用项目

```csharp
// Service、Repository等项目中更新类名引用
// 从 User 改为 UserModel
// 从 Patient 改为 PatientModel  
// 等等...
```

## ⚠️ 注意事项

### 破坏性变更

**实体类重命名**会影响：
- Service层的类型引用
- Repository层的泛型参数
- DTO映射配置
- 单元测试代码

### 数据库影响

**继承BaseEntity**会影响：
- 表结构变更（新增审计字段）
- 现有数据迁移需求  
- 索引策略调整

### 向后兼容

**建议使用别名保持兼容**：
```csharp
// 临时兼容别名
using User = LYBT.Entities.Users.UserModel;
using Patient = LYBT.Entities.Patients.PatientModel;
```

## 📈 预期收益

### 代码质量提升

- **统一性**: 所有实体遵循相同的命名规范和继承体系
- **维护性**: 基础字段变更只需修改BaseEntity一个文件
- **可读性**: 类名与文件名一致，降低团队协作成本

### 架构标准化

- **审计完整**: 所有实体自动具备创建、更新、删除审计功能
- **并发安全**: 统一的乐观并发控制机制  
- **扩展便利**: 新增实体只需继承BaseEntity即可获得完整基础功能

---

**实施状态**: ⏳ **BaseEntity已创建，等待实体重构** | **优先级**: 🔥 **高** | **预期完成**: Phase 1-3 分阶段实施