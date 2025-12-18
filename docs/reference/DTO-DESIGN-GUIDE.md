# DTO设计规范

## 概述

本文档定义LYBTZYZS项目的DTO（数据传输对象）设计规范，遵循Microsoft官方最佳实践，确保代码简洁、可维护、安全。

## 设计原则

### 1. 扁平化设计

DTO不使用继承链，所有字段直接在类中声明：

```csharp
// 正确 - 扁平化设计
public class PatientListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// 错误 - 继承链
public class PatientListDto : StatusDto { }
public class StatusDto : TimestampDto { }
```

**理由**: 继承增加理解成本，但DTO字段各不相同，复用价值有限。

### 2. 一文件一类

每个DTO单独一个文件，文件名与类名一致：

```
Contracts/
├── Patients/
│   ├── PatientListDto.cs      → public class PatientListDto
│   ├── PatientDetailDto.cs    → public class PatientDetailDto
│   └── PatientInputDto.cs     → public class PatientInputDto
```

**理由**: 便于定位、便于Git diff、便于并行编辑。

### 3. 按模块组织

DTO按业务模块放置在对应文件夹：

```
Contracts/
├── Prescriptions/    # 处方模块
├── Formulas/         # 验方模块
├── Herbs/            # 中药材模块
├── Patients/         # 患者模块
├── MedicalCases/     # 病案模块
├── Users/            # 用户模块
└── Consultation/     # 诊疗模块
```

## 标准DTO类型

每个实体最多4种DTO类型：

| 类型 | 命名模式 | 用途 | 字段范围 |
|------|----------|------|----------|
| **ListDto** | `{Entity}ListDto` | 列表视图 | Id + 显示必需字段 |
| **DetailDto** | `{Entity}DetailDto` | 详情视图 | 所有可读字段 |
| **InputDto** | `{Entity}InputDto` | 创建/编辑 | 所有可写字段 + 可选Id |
| **ItemInputDto** | `{Entity}ItemInputDto` | 子项输入 | 子项的输入字段 |

### ListDto设计

用于列表展示，只包含必要字段：

```csharp
public class PatientListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public Gender Gender { get; set; }
    public int? Age { get; set; }
    public CommonStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### DetailDto设计

用于详情展示，包含所有可读字段：

```csharp
public class PatientDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? PinYinCode { get; set; }
    public Gender Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public int? Age { get; set; }  // 计算字段
    public string? IdNumber { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? AllergyHistory { get; set; }
    public string? MedicalHistory { get; set; }
    public CommonStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

### InputDto设计

用于创建和编辑，遵循Over-posting防护原则：

```csharp
public class PatientInputDto
{
    // 可选Id：null=创建，有值=更新
    public Guid? Id { get; set; }

    // 用户可写字段
    [Required(ErrorMessage = "患者姓名不能为空")]
    [StringLength(50, ErrorMessage = "患者姓名长度不能超过50个字符")]
    public string Name { get; set; } = string.Empty;

    public string? PinYinCode { get; set; }
    public Gender Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public string? IdNumber { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Address { get; set; }
    public string? AllergyHistory { get; set; }
    public string? MedicalHistory { get; set; }

    // 注意：不包含以下字段
    // - Age (计算字段，由Service从BirthDate计算)
    // - Status (通过专用API修改)
    // - CreatedAt/UpdatedAt (系统管理)
}
```

#### InputDto字段规则

| 规则 | 说明 | 示例 |
|------|------|------|
| **只含可写字段** | 用户可以输入/修改的字段 | Name, PhoneNumber, Address |
| **排除系统字段** | 由系统自动管理的字段 | CreatedAt, UpdatedAt, CreatedBy |
| **排除计算字段** | 由服务层计算的字段 | Age (从BirthDate计算) |
| **排除状态字段** | 通过专用API修改的字段 | Status (通过Enable/Disable API) |
| **带验证注解** | 使用DataAnnotation验证 | [Required], [StringLength] |

**安全原理**: 防止Over-posting攻击，恶意用户无法通过提交额外字段来修改不应被修改的属性。

### ItemInputDto设计

用于聚合根的子项输入：

```csharp
public class PrescriptionItemInputDto
{
    public Guid? Id { get; set; }

    [Required]
    public Guid HerbId { get; set; }

    [StringLength(100)]
    public string? HerbName { get; set; }

    [Required]
    public string Unit { get; set; } = string.Empty;

    public int Dosage { get; set; }

    [Range(0, 10000)]
    public decimal UnitPrice { get; set; }
}
```

## 三层命名对应

形成Server-Shared-Client一致的命名对应关系：

| 层级 | 列表类型 | 详情类型 | 输入类型 |
|------|----------|----------|----------|
| **Server Entity** | - | `{Entity}Model` | - |
| **Shared DTO** | `{Entity}ListDto` | `{Entity}DetailDto` | `{Entity}InputDto` |
| **Desktop Model** | `{Entity}Item` | `{Entity}DetailModel` | (直接使用DTO) |

### 后缀含义

| 后缀 | 层级 | 含义 |
|------|------|------|
| `Model` | Server | 数据库实体 |
| `Dto` | Shared | API数据传输契约 |
| `Item` | Desktop | 列表项UI绑定模型 |
| `DetailModel` | Desktop | 详情编辑UI绑定模型 |
| `PrintModel` | Desktop | 打印视图模型 |

## 特殊DTO类型

### Statistics DTO

聚合统计结果使用record定义：

```csharp
public record PrescriptionStatistics(
    int TotalCount,
    int TodayCount,
    decimal TodayAmount
);
```

### Query参数

简单查询使用方法参数：

```csharp
public async Task<IActionResult> Index(
    string? keyword,
    int page = 1,
    int pageSize = 20)
```

复杂查询使用record：

```csharp
public record PrescriptionQueryParams(
    string? Keyword,
    DateTime? StartDate,
    DateTime? EndDate,
    Guid? PatientId,
    int Page = 1,
    int PageSize = 20
);
```

## 禁止事项

1. **禁止DTO继承链** - 不使用BaseDto、StatusDto等基类
2. **禁止过多接口** - 仅保留必要的IIdentifiable<T>
3. **禁止单文件多类** - 每个DTO独立文件
4. **禁止InputDto包含状态字段** - Status通过专用API修改
5. **禁止Desktop层使用Dto后缀** - Desktop本地模型使用Item/Model后缀

## 参考资料

- [Microsoft ASP.NET Core Web API Best Practices](https://learn.microsoft.com/en-us/aspnet/core/web-api/)
- [Over-posting Prevention](https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api)
- [DTO Pattern](https://learn.microsoft.com/en-us/aspnet/web-api/overview/data/using-web-api-with-entity-framework/part-5)
