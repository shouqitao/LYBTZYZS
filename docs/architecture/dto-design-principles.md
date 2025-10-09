# DTO 设计原则

> **文档版本**: v1.0
> **创建日期**: 2025-01-09
> **相关 Issue**: [#1094](https://github.com/shouqitao/LYBTZYZS/issues/1094)
> **状态**: 生效中

## 📋 概述

本文档规范化 LYBTZYZS 项目中 DTO (Data Transfer Object) 的设计原则、命名规范、使用场景和最佳实践,确保跨层传输对象的一致性与可维护性。

## 🎯 核心原则

### 1. 单一职责原则 (SRP)

每个 DTO 应该只服务于**一个明确的数据传输场景**,避免"万能 DTO"导致的耦合问题。

**✅ 推荐**:
```csharp
// 创建场景 - 只包含创建所需字段
public class ConsultationCreateDto
{
    public Guid MedicalCaseId { get; set; }
    public string ChiefComplaint { get; set; } = string.Empty;
    public string? PresentIllness { get; set; }
    // ... 只包含创建时必需的字段
}

// 更新场景 - 只包含可更新字段
public class ConsultationUpdateDto
{
    public string? ChiefComplaint { get; set; }
    public string? TCMDiagnosis { get; set; }
    // ... 不包含 Id、CreatedAt 等不可更新字段
}

// 展示场景 - 包含展示所需的完整信息
public class ConsultationDto
{
    public Guid Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;
    // ... 包含展示所需的所有字段
}
```

**❌ 避免**:
```csharp
// 反例：一个 DTO 用于所有场景,导致字段冗余和职责不清
public class ConsultationUniversalDto
{
    public Guid? Id { get; set; }  // 创建时为 null,更新时必需
    public Guid? MedicalCaseId { get; set; }  // 创建时必需,更新时可选?
    public string? PatientName { get; set; }  // 仅展示时需要
    // ... 字段含义模糊,使用场景不清晰
}
```

### 2. 不可变性 (Immutability)

DTO 应该设计为**读取后不可修改**,使用 `init` 访问器或构造函数注入确保数据完整性。

**✅ 推荐**:
```csharp
public class PatientDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
```

**⚠️ 特殊场景**:
- AutoMapper 映射需要公开 setter,可保留 `set`
- ViewModel 绑定需要双向通知,可使用 `set` + `INotifyPropertyChanged`

### 3. 扁平化优先 (Flat Structure)

**优先使用扁平结构**,仅在复杂嵌套有业务必要性时才使用嵌套 DTO。

**✅ 推荐** (扁平化):
```csharp
public class ConsultationDto
{
    public Guid Id { get; set; }
    public string PatientName { get; set; } = string.Empty;  // 扁平化患者信息
    public string DoctorName { get; set; } = string.Empty;   // 扁平化医生信息
}
```

**⚠️ 谨慎使用** (嵌套):
```csharp
public class MedicalCaseDetailDto
{
    public Guid Id { get; set; }
    public ConsultationDto? Consultation { get; set; }  // 嵌套 DTO,仅在确实需要完整子对象时使用
    public PrescriptionDto? Prescription { get; set; }
}
```

**判断标准**:
- 如果只需要显示关联对象的 1-2 个字段 → **扁平化**
- 如果需要完整的子对象操作 (如编辑、传递) → **嵌套 DTO**

### 4. 命名规范

#### 4.1 基础命名模式

| 场景 | 命名后缀 | 示例 | 用途 |
|------|---------|------|------|
| 展示/查询 | `Dto` | `ConsultationDto` | API 响应、列表展示 |
| 创建 | `CreateDto` | `ConsultationCreateDto` | POST 请求 Body |
| 更新 | `UpdateDto` | `ConsultationUpdateDto` | PUT/PATCH 请求 Body |
| 详情 | `DetailDto` | `MedicalCaseDetailDto` | 包含关联对象的完整信息 |

#### 4.2 命名禁忌

**❌ 禁止使用**:
- `Request` / `Response` 后缀 (这是 HTTP 层概念,不是 DTO 职责)
- `Model` 后缀 (容易与 Entity 和 ViewModel 混淆)
- `Info` / `Data` 后缀 (语义模糊)

### 5. 字段设计规范

#### 5.1 必需字段 vs 可选字段

**✅ 推荐**:
```csharp
public class ConsultationCreateDto
{
    [Required]
    public Guid MedicalCaseId { get; set; }  // 必需字段,非 nullable

    [Required]
    [MaxLength(500)]
    public string ChiefComplaint { get; set; } = string.Empty;  // 必需,默认空字符串

    [MaxLength(2000)]
    public string? PresentIllness { get; set; }  // 可选字段,nullable
}
```

**❌ 避免**:
```csharp
public class ConsultationCreateDto
{
    public Guid MedicalCaseId { get; set; } = Guid.Empty;  // ❌ Guid 不应有默认值
    public string ChiefComplaint { get; set; } = null!;    // ❌ 使用 null! 抑制警告,掩盖问题
}
```

#### 5.2 Guid 字段处理

**规则**:
- **必需的 Guid**: `Guid` (非 nullable)
- **可选的 Guid**: `Guid?` (nullable)
- **禁止**: `Guid.Empty` 作为默认值

**✅ 正确**:
```csharp
public class PrescriptionCreateDto
{
    public Guid MedicalCaseId { get; set; }  // 必需,由调用方提供
    public Guid? RefPrescriptionId { get; set; }  // 可选,引用的处方 ID
}
```

#### 5.3 默认值设计

**字符串字段**:
- 必需字段: `= string.Empty`
- 可选字段: `= null` (nullable)

**集合字段**:
```csharp
public List<PrescriptionItemDto> Items { get; set; } = new();  // 空集合,避免 null 引用
```

**枚举字段**:
```csharp
public ConsultationStatus Status { get; set; } = ConsultationStatus.Pending;  // 合理的业务默认值
```

## 📦 DTO 类型详解

### 1. 基础 DTO (`*Dto`)

**用途**: API 查询响应、列表展示

**特征**:
- 包含 Entity 的**核心展示字段**
- 可包含关联对象的**扁平化字段** (如 `PatientName`)
- **不包含**敏感字段 (如密码哈希)
- **不包含**导航属性 (除非确有必要)

**示例**:
```csharp
public class ConsultationDto
{
    public Guid Id { get; set; }
    public Guid MedicalCaseId { get; set; }
    public string PatientName { get; set; } = string.Empty;  // 扁平化
    public string DoctorName { get; set; } = string.Empty;   // 扁平化
    public string ChiefComplaint { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
```

### 2. 创建 DTO (`*CreateDto`)

**用途**: POST 请求 Body

**特征**:
- **不包含** `Id` (由系统生成)
- **不包含** 审计字段 (`CreatedAt`, `CreatedBy` 等,由系统填充)
- **仅包含**创建时必需的业务字段
- **必须**包含关联 ID (如 `MedicalCaseId`)

**示例**:
```csharp
public class ConsultationCreateDto
{
    [Required]
    public Guid MedicalCaseId { get; set; }  // 关联 ID

    [Required]
    [MaxLength(500)]
    public string ChiefComplaint { get; set; } = string.Empty;

    public string? PresentIllness { get; set; }
    // 不包含 Id, CreatedAt, CreatedBy 等系统字段
}
```

### 3. 更新 DTO (`*UpdateDto`)

**用途**: PUT/PATCH 请求 Body

**特征**:
- **不包含** `Id` (在 URL 路径中)
- **不包含** 不可更新字段 (`CreatedAt`, `CreatedBy`, 外键 ID)
- **仅包含**可更新的业务字段
- 字段通常为**可选** (nullable),支持部分更新

**示例**:
```csharp
public class ConsultationUpdateDto
{
    public string? ChiefComplaint { get; set; }
    public string? PresentIllness { get; set; }
    public string? TCMDiagnosis { get; set; }
    // 不包含 Id, MedicalCaseId, CreatedAt 等
}
```

### 4. 详情 DTO (`*DetailDto`)

**用途**: 单个资源的完整信息展示,包含关联对象

**特征**:
- 继承或组合基础 `*Dto`
- 包含**嵌套的关联对象** DTO
- 用于详情页、导出等需要完整数据的场景

**示例**:
```csharp
public class MedicalCaseDetailDto
{
    public Guid Id { get; set; }
    public string PatientName { get; set; } = string.Empty;
    public string DoctorName { get; set; } = string.Empty;

    // 嵌套关联对象
    public ConsultationDto? Consultation { get; set; }
    public PrescriptionDto? Prescription { get; set; }
    public List<DiagnosisDto> Diagnoses { get; set; } = new();
}
```

## 🔄 DTO 与 Entity 映射

### 1. AutoMapper 配置规范

**位置**: `src/Server/Modules/LYBT.Module.*/MappingProfiles/*.cs`

**命名**: `{ModuleName}MappingProfile.cs`

**示例**:
```csharp
public class ConsultationMappingProfile : Profile
{
    public ConsultationMappingProfile()
    {
        // Entity → Dto
        CreateMap<Consultation, ConsultationDto>()
            .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.MedicalCase.PatientName))
            .ForMember(dest => dest.DoctorName, opt => opt.MapFrom(src => src.MedicalCase.DoctorName));

        // CreateDto → Entity
        CreateMap<ConsultationCreateDto, Consultation>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())  // 系统生成
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore());  // 系统填充

        // UpdateDto → Entity
        CreateMap<ConsultationUpdateDto, Consultation>()
            .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null));  // 忽略 null 值
    }
}
```

### 2. 映射原则

**✅ 推荐**:
- **扁平化映射**: Entity 的导航属性 → DTO 的扁平字段
  ```csharp
  .ForMember(dest => dest.PatientName, opt => opt.MapFrom(src => src.MedicalCase.PatientName))
  ```

- **忽略系统字段**: CreateDto/UpdateDto → Entity 时忽略 Id、审计字段
  ```csharp
  .ForMember(dest => dest.Id, opt => opt.Ignore())
  ```

- **条件映射**: UpdateDto → Entity 时忽略 null 值
  ```csharp
  .ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null))
  ```

**❌ 避免**:
- 在 Service 层手动映射 (破坏 DRY 原则)
- 在 DTO 中添加 Entity 引用 (破坏层隔离)

## 🛡️ 验证规范

### 1. Data Annotations (轻量场景)

**适用**: 简单的必需性、长度、格式验证

```csharp
public class ConsultationCreateDto
{
    [Required(ErrorMessage = "医案ID不能为空")]
    public Guid MedicalCaseId { get; set; }

    [Required(ErrorMessage = "主诉不能为空")]
    [MaxLength(500, ErrorMessage = "主诉不能超过500字符")]
    public string ChiefComplaint { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    public string? Email { get; set; }
}
```

### 2. FluentValidation (复杂场景)

**适用**: 跨字段验证、业务规则验证、异步验证

**位置**: `src/Server/Modules/LYBT.Module.*/Validators/*.cs`

```csharp
public class ConsultationCreateDtoValidator : AbstractValidator<ConsultationCreateDto>
{
    private readonly IMedicalCaseRepository _medicalCaseRepository;

    public ConsultationCreateDtoValidator(IMedicalCaseRepository medicalCaseRepository)
    {
        _medicalCaseRepository = medicalCaseRepository;

        RuleFor(x => x.MedicalCaseId)
            .NotEmpty().WithMessage("医案ID不能为空")
            .MustAsync(MedicalCaseExists).WithMessage("关联的医案不存在");

        RuleFor(x => x.ChiefComplaint)
            .NotEmpty().WithMessage("主诉不能为空")
            .MaximumLength(500).WithMessage("主诉不能超过500字符");
    }

    private async Task<bool> MedicalCaseExists(Guid medicalCaseId, CancellationToken cancellationToken)
    {
        var medicalCase = await _medicalCaseRepository.GetByIdAsync(medicalCaseId);
        return medicalCase != null;
    }
}
```

## 📍 DTO 存放位置

```
src/
  └── Shared/
      └── LYBT.Shared.Models/
          └── Contracts/
              ├── Consultation/
              │   ├── ConsultationDto.cs
              │   ├── ConsultationCreateDto.cs
              │   ├── ConsultationUpdateDto.cs
              │   └── ConsultationDetailDto.cs
              ├── MedicalCase/
              │   ├── MedicalCaseDto.cs
              │   ├── MedicalCaseCreateDto.cs
              │   ├── MedicalCaseUpdateDto.cs
              │   └── MedicalCaseDetailDto.cs
              └── Common/
                  ├── PagedResult.cs
                  └── ServiceResult.cs
```

**规则**:
- DTO **必须**放在 `Shared` 项目,供 Server 和 Desktop 共享
- **禁止** 在 Server/Desktop 项目中重复定义同名 DTO
- 按业务模块分目录 (与 Entity 和 Module 对应)

## 🚫 反模式与禁忌

### 1. DTO 包含业务逻辑

**❌ 错误**:
```csharp
public class ConsultationDto
{
    public Guid Id { get; set; }
    public string ChiefComplaint { get; set; } = string.Empty;

    // ❌ DTO 中不应有业务方法
    public bool IsValid()
    {
        return !string.IsNullOrWhiteSpace(ChiefComplaint);
    }
}
```

**✅ 正确**: 业务逻辑应在 Service 或业务规则类中

### 2. DTO 直接继承 Entity

**❌ 错误**:
```csharp
public class ConsultationDto : Consultation  // ❌ 破坏层隔离
{
    public string PatientName { get; set; } = string.Empty;
}
```

**✅ 正确**: DTO 是独立定义的类,通过 AutoMapper 映射

### 3. DTO 包含导航属性

**❌ 错误**:
```csharp
public class ConsultationDto
{
    public Guid Id { get; set; }
    public MedicalCase MedicalCase { get; set; } = null!;  // ❌ Entity 类型
}
```

**✅ 正确**: 使用嵌套 DTO 或扁平化字段

### 4. 使用 `Guid.Empty` 作为默认值

**❌ 错误**:
```csharp
public class ConsultationCreateDto
{
    public Guid MedicalCaseId { get; set; } = Guid.Empty;  // ❌ 掩盖必需性
}
```

**✅ 正确**: 必需的 Guid 不设默认值,让验证器检查

## 📊 检查清单

设计 DTO 时,请确认以下事项:

- [ ] DTO 是否有**明确的单一使用场景** (展示/创建/更新/详情)?
- [ ] 命名是否符合规范 (`*Dto` / `*CreateDto` / `*UpdateDto` / `*DetailDto`)?
- [ ] 是否避免了包含不必要的字段 (如 CreateDto 中的 Id)?
- [ ] 字段类型是否正确 (必需字段非 nullable,可选字段 nullable)?
- [ ] 是否使用了合理的默认值 (字符串 `= string.Empty`,集合 `= new()`)?
- [ ] 是否添加了必要的验证特性 (Data Annotations 或 FluentValidation)?
- [ ] 是否配置了 AutoMapper 映射?
- [ ] 是否遵循扁平化优先原则 (避免不必要的嵌套)?
- [ ] 是否放在了正确的目录 (`Shared.Models/Contracts/{Module}/`)?

## 📚 相关文档

- [Server Module Design Standard](server-module-design-standard.md)
- [Client Unified Design Standard](client/unified-design-standard.md)
- [AutoMapper 官方文档](https://docs.automapper.org/)
- [FluentValidation 官方文档](https://docs.fluentvalidation.net/)

---

**最后更新**: 2025-01-09
**维护者**: LYBTZYZS 开发团队
