# DTO设计标准

> **DTO (Data Transfer Object) 设计规范与最佳实践**
> 跨端统一 | 类型安全 | 验证增强 | UltraThink简化
> **版本**: v2.0 | **更新**: 2025-10-29

---

## 📋 目录

1. [设计原则](#设计原则)
2. [DTO基类体系](#dto基类体系)
3. [命名规范](#命名规范)
4. [CRUD操作DTO模式](#crud操作dto模式)
5. [验证规范](#验证规范)
6. [DTO与Entity映射](#dto与entity映射)
7. [跨端统一标准](#跨端统一标准)
8. [最佳实践](#最佳实践)
9. [常见反模式](#常见反模式)

---

## 设计原则

### 核心原则（UltraThink v2.0简化）

#### 1. **最小化基础类**
- ✅ DTO基类只包含必要字段（ID、时间戳、状态）
- ✅ 避免过度继承和抽象
- ❌ 不要创建深层继承链（最多3层）

#### 2. **职责单一**
- ✅ 每个DTO只服务一个特定场景（查询/创建/更新）
- ✅ 避免"万能DTO"（一个DTO用于所有操作）
- ❌ 不要在DTO中包含业务逻辑

#### 3. **跨端一致**
- ✅ Client端和Server端使用相同的DTO定义
- ✅ 所有DTO定义在 `LYBT.Shared.Models` 项目中
- ✅ 确保数据结构在前后端完全对齐

#### 4. **类型安全**
- ✅ 使用强类型枚举（避免 `int` 表示状态）
- ✅ 必填字段使用非空类型（`string`），可选字段使用可空类型（`string?`）
- ✅ 使用 `Guid` 作为主键类型（不使用 `int`）

#### 5. **验证前置**
- ✅ 使用DataAnnotations和FluentValidation双重验证
- ✅ 验证规则在DTO定义时就明确
- ✅ 验证失败抛出 `ValidationException`

---

## DTO基类体系

### 基类继承链

```
BaseDto (ID)
  └─ TimestampDto (ID + CreatedAt + UpdatedAt)
       └─ StatusDto (ID + CreatedAt + UpdatedAt + Status)
```

### 1. BaseDto - 基础DTO

**职责**: 提供唯一标识符

```csharp
/// <summary>
/// 基础DTO抽象类 - 提供Guid类型的ID字段
/// UltraThink简化：最小化基础类，只包含ID
/// </summary>
public abstract class BaseDto : IIdentifiable<Guid>
{
    /// <summary>唯一标识符</summary>
    [DisplayName("ID")]
    public Guid Id { get; set; }
}
```

**使用场景**:
- 需要ID但不需要时间戳的DTO（如简单枚举项）

### 2. TimestampDto - 时间戳DTO

**职责**: 包含ID和审计时间字段

```csharp
/// <summary>
/// 时间戳DTO抽象类 - 包含ID和审计时间字段
/// UltraThink简化：统一审计时间管理
/// </summary>
public abstract class TimestampDto : BaseDto, IAuditable
{
    /// <summary>创建时间</summary>
    [DisplayName("创建时间")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    /// <summary>更新时间</summary>
    [DisplayName("更新时间")]
    public DateTime? UpdatedAt { get; set; }
}
```

**使用场景**:
- 需要追踪创建和更新时间的DTO

### 3. StatusDto - 状态管理DTO

**职责**: 包含ID、时间戳和状态字段

```csharp
/// <summary>
/// 状态管理DTO抽象类 - 包含ID、时间戳和状态字段
/// UltraThink简化：合并状态和时间戳管理
/// </summary>
public abstract class StatusDto : TimestampDto, IStatusManageable
{
    /// <summary>状态</summary>
    [DisplayName("状态")]
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;

    /// <summary>是否启用 - 根据Status计算得出</summary>
    [DisplayName("是否启用")]
    public bool IsEnabled => Status == CommonStatus.Enabled;
}
```

**使用场景**:
- 需要状态管理的实体DTO（如User、Patient、Herb）

---

## 命名规范

### DTO命名模式

| DTO类型 | 命名规则 | 示例 |
|---------|---------|------|
| **查询DTO** | `{Entity}Dto` | `PatientDto`, `HerbDto` |
| **创建DTO** | `{Entity}CreateDto` | `PatientCreateDto`, `HerbCreateDto` |
| **更新DTO** | `{Entity}UpdateDto` | `PatientUpdateDto`, `HerbUpdateDto` |
| **搜索DTO** | `{Entity}SearchDto` | `PatientSearchDto`, `HerbSearchDto` |
| **简化DTO** | `Simplified{Entity}Dto` | `SimplifiedMedicalCaseDto` |
| **操作DTO** | `{Entity}OperationDto` | `PatientOperationDto` |
| **统计DTO** | `{Entity}StatisticsDto` | `PatientStatisticsDto` |

### 文件组织规范

```
LYBT.Shared.Models/
└── Contracts/
    ├── Common/            # 通用DTO基类
    │   ├── DtoBase.cs
    │   ├── PagedResult.cs
    │   └── ApiResponse.cs
    ├── Patients/          # 患者相关DTO
    │   ├── PatientDtos.cs              # PatientDto, PatientCreateDto, PatientUpdateDto, PatientSearchDto
    │   ├── PatientOperationDtos.cs     # 患者操作相关DTO
    │   └── PatientStatisticsDtos.cs    # 患者统计DTO
    └── Herbs/             # 药材相关DTO
        ├── HerbDtos.cs                 # HerbDto, HerbCreateDto, HerbUpdateDto, HerbSearchDto
        └── HerbOperationDtos.cs        # 药材操作相关DTO
```

**规则**:
- ✅ 相关DTO放在同一文件中（如 `PatientDto` 和 `PatientCreateDto`）
- ✅ 文件名使用复数形式（`PatientDtos.cs`）
- ✅ 模块名作为一级目录（`Patients/`, `Herbs/`）

---

## CRUD操作DTO模式

### 1. 查询DTO（Dto）

**继承**: `StatusDto`

**职责**: 返回完整实体数据（包括ID、时间戳、状态）

**示例**:

```csharp
/// <summary>
/// 患者信息DTO - UltraThink v2.0简化版
/// </summary>
public class PatientDto : StatusDto
{
    /// <summary>患者姓名</summary>
    [DisplayName("患者姓名")]
    public string Name { get; set; } = string.Empty;

    /// <summary>性别</summary>
    [DisplayName("性别")]
    public Gender Gender { get; set; }

    /// <summary>出生日期</summary>
    [DisplayName("出生日期")]
    public DateTime? BirthDate { get; set; }

    /// <summary>手机号码</summary>
    [DisplayName("手机号码")]
    public string? PhoneNumber { get; set; }

    // ... 其他字段
}
```

**关键点**:
- ✅ 继承 `StatusDto` 获得ID、时间戳、状态
- ✅ 包含所有业务字段
- ✅ 可空字段使用 `?` 标记（`DateTime?`, `string?`）

### 2. 创建DTO（CreateDto）

**继承**: `CreateDtoBase`

**职责**: 创建新实体（不包含ID，由系统生成）

**示例**:

```csharp
/// <summary>
/// 患者创建DTO
/// </summary>
public class PatientCreateDto : CreateDtoBase
{
    /// <summary>患者姓名</summary>
    [Required(ErrorMessage = "患者姓名不能为空")]
    [StringLength(50, ErrorMessage = "患者姓名长度不能超过50个字符")]
    [DisplayName("患者姓名")]
    public string Name { get; set; } = string.Empty;

    /// <summary>性别</summary>
    [DisplayName("性别")]
    public Gender Gender { get; set; }

    /// <summary>出生日期</summary>
    [DisplayName("出生日期")]
    public DateTime? BirthDate { get; set; }

    // ... 其他字段
}
```

**关键点**:
- ✅ 继承 `CreateDtoBase` 获得Status和Remark
- ❌ 不包含ID（由系统生成）
- ❌ 不包含时间戳（由系统自动设置）
- ✅ 使用DataAnnotations验证必填字段

### 3. 更新DTO（UpdateDto）

**继承**: `UpdateDtoBase`

**职责**: 更新现有实体（包含ID用于标识）

**示例**:

```csharp
/// <summary>
/// 患者更新DTO
/// </summary>
public class PatientUpdateDto : UpdateDtoBase
{
    /// <summary>患者姓名</summary>
    [Required(ErrorMessage = "患者姓名不能为空")]
    [StringLength(50, ErrorMessage = "患者姓名长度不能超过50个字符")]
    [DisplayName("患者姓名")]
    public string Name { get; set; } = string.Empty;

    /// <summary>性别</summary>
    [DisplayName("性别")]
    public Gender Gender { get; set; }

    // ... 其他字段
}
```

**关键点**:
- ✅ 继承 `UpdateDtoBase` 获得ID、Status、Remark
- ✅ 包含ID用于标识要更新的实体
- ❌ 不包含CreatedAt（创建时间不可修改）
- ✅ UpdatedAt由系统自动设置

### 4. 搜索DTO（SearchDto）

**继承**: `PagedQueryBaseDto`

**职责**: 提供搜索条件和分页参数

**示例**:

```csharp
/// <summary>
/// 患者搜索DTO
/// </summary>
public class PatientSearchDto : PagedQueryBaseDto
{
    /// <summary>患者姓名（模糊搜索）</summary>
    [DisplayName("患者姓名")]
    public string? Name { get; set; }

    /// <summary>手机号码（模糊搜索）</summary>
    [DisplayName("手机号码")]
    public string? PhoneNumber { get; set; }

    /// <summary>状态</summary>
    [DisplayName("状态")]
    public CommonStatus? Status { get; set; }

    /// <summary>创建日期范围-开始</summary>
    [DisplayName("创建日期范围-开始")]
    public DateTime? CreateStartDate { get; set; }

    /// <summary>创建日期范围-结束</summary>
    [DisplayName("创建日期范围-结束")]
    public DateTime? CreateEndDate { get; set; }
}
```

**关键点**:
- ✅ 继承 `PagedQueryBaseDto` 获得分页参数（PageIndex, PageSize）
- ✅ 所有搜索条件字段都使用可空类型（`string?`, `CommonStatus?`）
- ✅ 日期范围搜索使用StartDate/EndDate命名模式

---

## 验证规范

### 双重验证机制

#### 1. DataAnnotations（基础验证）

**用途**: 简单的字段级验证（必填、长度、格式）

**示例**:

```csharp
/// <summary>患者姓名</summary>
[Required(ErrorMessage = "患者姓名不能为空")]
[StringLength(50, ErrorMessage = "患者姓名长度不能超过50个字符")]
[DisplayName("患者姓名")]
public string Name { get; set; } = string.Empty;

/// <summary>手机号码</summary>
[RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "手机号码格式不正确")]
[DisplayName("手机号码")]
public string? PhoneNumber { get; set; }
```

#### 2. FluentValidation（复杂验证）

**用途**: 复杂的业务规则验证（跨字段、异步验证）

**示例**:

```csharp
/// <summary>
/// 患者创建DTO验证器
/// </summary>
public class PatientCreateDtoValidator : AbstractValidator<PatientCreateDto>
{
    public PatientCreateDtoValidator()
    {
        // 患者姓名验证
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("患者姓名不能为空")
            .MaximumLength(50).WithMessage("患者姓名长度不能超过50个字符");

        // 手机号码验证（可选，但格式必须正确）
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号码格式不正确")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        // 出生日期验证（不能是未来日期）
        RuleFor(x => x.BirthDate)
            .LessThanOrEqualTo(DateTime.Today).WithMessage("出生日期不能是未来日期")
            .When(x => x.BirthDate.HasValue);

        // 身份证号码验证（可选，但格式必须正确）
        RuleFor(x => x.IdNumber)
            .Matches(@"^\d{15}$|^\d{17}[\dXx]$").WithMessage("身份证号码格式不正确")
            .When(x => !string.IsNullOrEmpty(x.IdNumber));
    }
}
```

### 验证规范总结

| 验证类型 | 工具 | 场景 |
|---------|------|------|
| **必填验证** | DataAnnotations | `[Required]` |
| **长度验证** | DataAnnotations | `[StringLength]` |
| **格式验证** | DataAnnotations/FluentValidation | `[RegularExpression]` 或 `.Matches()` |
| **条件验证** | FluentValidation | `.When(x => ...)` |
| **跨字段验证** | FluentValidation | 在同一验证器中访问多个字段 |
| **异步验证** | FluentValidation | `.MustAsync()` |

---

## DTO与Entity映射

### AutoMapper配置

#### 1. 标准映射（简单对齐）

**场景**: DTO字段名与Entity字段名完全一致

**示例**:

```csharp
/// <summary>
/// 患者映射配置
/// </summary>
public class PatientMappingProfile : Profile
{
    public PatientMappingProfile()
    {
        // Entity → Dto（查询）
        CreateMap<Patient, PatientDto>()
            .ReverseMap(); // Dto → Entity（双向映射）

        // CreateDto → Entity（创建）
        CreateMap<PatientCreateDto, Patient>();

        // UpdateDto → Entity（更新）
        CreateMap<PatientUpdateDto, Patient>();
    }
}
```

#### 2. 自定义映射（字段名不一致）

**场景**: DTO字段名与Entity字段名不同，需要显式映射

**示例**:

```csharp
CreateMap<Patient, PatientDto>()
    .ForMember(dest => dest.BirthDate, opt => opt.MapFrom(src => src.DateOfBirth)) // 字段名映射
    .ForMember(dest => dest.Age, opt => opt.Ignore()); // 忽略计算属性
```

#### 3. 条件映射

**场景**: 根据条件决定是否映射

**示例**:

```csharp
CreateMap<PatientUpdateDto, Patient>()
    .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.Now)) // 自动设置更新时间
    .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null)); // 忽略null值
```

### 映射规范

✅ **推荐做法**:
- 使用 `.ReverseMap()` 简化双向映射
- 使用 `.ForMember()` 处理特殊字段
- 使用 `.Ignore()` 忽略计算属性

❌ **避免做法**:
- 不要在映射配置中包含业务逻辑
- 不要使用 `.ConstructUsing()` 创建复杂对象
- 不要在映射中调用外部服务

---

## 跨端统一标准

### Shared.Models职责边界

| 层级 | 使用DTO | 说明 |
|------|---------|------|
| **Client端** | ✅ 使用Shared.Models中的DTO | 通过ApiService发送/接收DTO |
| **Server端** | ✅ 使用Shared.Models中的DTO | Controller接收DTO，Service返回DTO |
| **WebAPI** | ✅ 使用Shared.Models中的DTO | 统一的API契约 |

### 跨端一致性规则

#### 1. 统一枚举定义

**❌ 错误示例**（Client和Server分别定义）:

```csharp
// ❌ Client端定义
public enum Gender { Male = 0, Female = 1 }

// ❌ Server端定义
public enum Gender { Unknown = 0, Male = 1, Female = 2 } // 不一致！
```

**✅ 正确示例**（Shared.Models统一定义）:

```csharp
// ✅ Shared.Models/Enums/Gender.cs
public enum Gender
{
    [Description("男性")]
    Male = 0,

    [Description("女性")]
    Female = 1,

    [Description("未知")]
    Unknown = 2
}
```

#### 2. 统一验证规则

**✅ 推荐做法**:
- 将FluentValidation验证器也放在Shared.Models中
- Client端和Server端使用相同的验证器
- 确保前后端验证逻辑完全一致

**示例结构**:

```
LYBT.Shared.Models/
└── Validators/
    ├── PatientCreateDtoValidator.cs    # Client + Server共享
    ├── PatientUpdateDtoValidator.cs    # Client + Server共享
    └── HerbCreateDtoValidator.cs       # Client + Server共享
```

---

## 最佳实践

### 1. 使用不可变DTO（只读属性）

**❌ 避免**:

```csharp
public class PatientDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
```

**✅ 推荐**（使用 `init` 访问器）:

```csharp
public class PatientDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
}
```

**原因**: 防止DTO在传输过程中被意外修改

### 2. 使用记录类型（Record）简化DTO

**✅ 适用场景**: 简单的查询DTO或响应DTO

```csharp
/// <summary>
/// 登录响应DTO（使用Record）
/// </summary>
public record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User
);
```

**优势**:
- ✅ 更简洁的语法
- ✅ 自动实现值相等性
- ✅ 不可变性（默认只读）

**限制**:
- ❌ 不适用于需要继承的DTO
- ❌ 不适用于需要验证的CreateDto/UpdateDto

### 3. 使用分页结果包装器

**✅ 统一的分页响应格式**:

```csharp
/// <summary>
/// 分页结果模型
/// </summary>
public class PagedResult<T>
{
    /// <summary>数据列表</summary>
    public List<T> Items { get; set; } = new();

    /// <summary>总记录数</summary>
    public int TotalCount { get; set; }

    /// <summary>当前页码</summary>
    public int PageIndex { get; set; }

    /// <summary>每页大小</summary>
    public int PageSize { get; set; }

    /// <summary>总页数</summary>
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);

    /// <summary>是否有上一页</summary>
    public bool HasPreviousPage => PageIndex > 1;

    /// <summary>是否有下一页</summary>
    public bool HasNextPage => PageIndex < TotalPages;
}
```

### 4. 使用ApiResponse统一响应格式

**✅ 统一的API响应格式**:

```csharp
/// <summary>
/// 统一API响应格式
/// </summary>
public class ApiResponse<T>
{
    /// <summary>是否成功</summary>
    public bool Success { get; set; }

    /// <summary>响应数据</summary>
    public T? Data { get; set; }

    /// <summary>错误消息</summary>
    public string? Message { get; set; }

    /// <summary>错误代码</summary>
    public string? ErrorCode { get; set; }

    /// <summary>时间戳</summary>
    public DateTime Timestamp { get; set; } = DateTime.Now;
}
```

---

## 常见反模式

### ❌ 反模式1：在DTO中包含业务逻辑

**错误示例**:

```csharp
public class PatientDto
{
    public string Name { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }

    // ❌ 业务逻辑不应在DTO中
    public bool IsEligibleForDiscount()
    {
        var age = CalculateAge();
        return age >= 60 || age <= 12;
    }
}
```

**正确做法**:
- ✅ 业务逻辑应该在Service层
- ✅ DTO只包含数据字段和简单的计算属性（如Age）

### ❌ 反模式2：DTO与Entity完全一致

**错误示例**:

```csharp
// ❌ PatientDto与Patient实体完全一样
public class PatientDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; // ❌ 敏感字段不应暴露
    public byte[] RowVersion { get; set; } // ❌ 技术字段不应暴露
}
```

**正确做法**:
- ✅ DTO应该只包含需要传输的字段
- ✅ 隐藏敏感字段（PasswordHash）
- ✅ 隐藏技术字段（RowVersion, CreatedBy, UpdatedBy）

### ❌ 反模式3：使用"万能DTO"

**错误示例**:

```csharp
// ❌ 一个DTO用于所有操作
public class PatientDto
{
    public Guid? Id { get; set; } // ❌ 创建时为null，更新时有值
    public string Name { get; set; } = string.Empty;
    public DateTime? CreatedAt { get; set; } // ❌ 创建时应该由系统设置
}
```

**正确做法**:
- ✅ 为不同操作创建专用DTO（PatientDto, PatientCreateDto, PatientUpdateDto）
- ✅ 每个DTO只包含该操作需要的字段

### ❌ 反模式4：在DTO中包含导航属性

**错误示例**:

```csharp
// ❌ DTO中包含导航属性
public class PatientDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<MedicalCase> MedicalCases { get; set; } = new(); // ❌ 直接引用Entity
}
```

**正确做法**:
- ✅ 使用DTO表示关联关系（`List<MedicalCaseDto>`）
- ✅ 或者只包含关联ID（`List<Guid> MedicalCaseIds`）

---

## 参考资料

### 相关文档
- [Shared.Models README](../../../../src/Shared/LYBT.Shared.Models/README.md) - 完整的DTO项目文档
- [三层对齐架构](../README.md) - 架构总览
- [AutoMapper官方文档](https://docs.automapper.org/)
- [FluentValidation官方文档](https://docs.fluentvalidation.net/)

### 代码示例
- `src/Shared/LYBT.Shared.Models/Contracts/Patients/PatientDtos.cs` - 完整的CRUD DTO示例
- `src/Shared/LYBT.Shared.Models/Contracts/Common/DtoBase.cs` - DTO基类定义
- `src/Server/Modules/LYBT.Module.Patients/Mapping/PatientMappingProfile.cs` - AutoMapper配置示例

---

**文档版本**: v2.0
**最后更新**: 2025-10-29
**维护者**: 项目架构组

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
