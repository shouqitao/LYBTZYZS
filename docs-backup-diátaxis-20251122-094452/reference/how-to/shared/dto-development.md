# DTO开发指南

> **DTO (Data Transfer Object) 开发实战手册**
> 从需求到实现 | 跨端统一 | 类型安全 | 验证增强
> **版本**: v1.0 | **更新**: 2025-10-30

---

## 📋 目录

1. [开发流程总览](#开发流程总览)
2. [环境准备](#环境准备)
3. [创建查询DTO](#创建查询dto)
4. [创建创建DTO](#创建创建dto)
5. [创建更新DTO](#创建更新dto)
6. [创建搜索DTO](#创建搜索dto)
7. [DataAnnotations验证](#dataannotations验证)
8. [FluentValidation验证](#fluentvalidation验证)
9. [AutoMapper映射配置](#automapper映射配置)
10. [接口实现模式](#接口实现模式)
11. [完整实战案例](#完整实战案例)
12. [常见问题与陷阱](#常见问题与陷阱)
13. [检查清单](#检查清单)

---

## 开发流程总览

### DTO创建五步法

```
Step 1: 需求分析
    ↓ 确定实体字段和操作类型
Step 2: 创建查询DTO
    ↓ 继承StatusDto，包含所有业务字段
Step 3: 创建输入DTO
    ↓ CreateDto和UpdateDto（共享基础类）
Step 4: 添加验证规则
    ↓ DataAnnotations + FluentValidation
Step 5: 配置AutoMapper
    ↓ Entity ↔ DTO双向映射
```

### 关键决策点

在开始创建DTO之前，需要明确以下问题：

| 问题 | 选择 | 示例 |
|------|------|------|
| **需要哪些CRUD操作？** | 查询/创建/更新/删除/搜索 | 患者管理需要完整CRUD |
| **是否需要状态管理？** | 是：继承`StatusDto`，否：继承`TimestampDto` | 患者需要状态（启用/禁用） |
| **有哪些必填字段？** | 标记`[Required]` | 患者姓名、性别 |
| **有哪些可选字段？** | 使用`?`标记 | 出生日期`DateTime?` |
| **是否需要实现接口？** | 如`IHerbItem`用于跨端计算 | 处方药材项实现`IHerbItem` |

---

## 环境准备

### 1. 项目结构

**DTO定义位置**：`LYBT.Shared.Models/Contracts/{模块名}/`

```
LYBT.Shared.Models/
└── Contracts/
    ├── Common/            # 通用基类和接口
    │   ├── DtoBase.cs     # BaseDto, TimestampDto, StatusDto
    │   ├── PagedResult.cs
    │   └── ApiResponse.cs
    ├── Patients/          # 患者模块DTO
    │   └── PatientDtos.cs # PatientDto, PatientCreateDto, PatientUpdateDto
    ├── Herbs/             # 药材模块DTO
    │   └── HerbDtos.cs
    └── MedicalCases/      # 医案模块DTO
        └── MedicalCaseDtos.cs
```

**命名规范**：
- ✅ 文件名使用复数：`PatientDtos.cs`（不是`PatientDto.cs`）
- ✅ 相关DTO放在同一文件：`PatientDto`、`PatientCreateDto`、`PatientUpdateDto`
- ✅ 模块名作为命名空间：`LYBT.Shared.Models.Contracts.Patients`

### 2. 引用依赖

```xml
<!-- LYBT.Shared.Models.csproj -->
<ItemGroup>
    <!-- 验证框架 -->
    <PackageReference Include="System.ComponentModel.Annotations" Version="8.0.0" />
    <PackageReference Include="FluentValidation" Version="11.x" />

    <!-- JSON序列化 -->
    <PackageReference Include="System.Text.Json" Version="8.0.0" />
</ItemGroup>
```

### 3. 必需的using语句

```csharp
using System.ComponentModel;                       // [DisplayName]
using System.ComponentModel.DataAnnotations;      // [Required], [StringLength]
using LYBT.Shared.Models.Contracts.Common;        // BaseDto, StatusDto
using LYBT.Shared.Models.Enums;                   // Gender, CommonStatus
using LYBT.Shared.Models.Constants;               // ValidationConstants
```

---

## 创建查询DTO

### 目的
查询DTO用于从Server端返回完整的实体数据（包括ID、时间戳、状态）。

### 步骤

#### Step 1: 确定继承基类

| 基类 | 包含字段 | 使用场景 |
|------|---------|---------|
| `BaseDto` | `Id` | 简单枚举项（不需要时间戳和状态） |
| `TimestampDto` | `Id` + `CreatedAt` + `UpdatedAt` | 需要审计时间但不需要状态 |
| `StatusDto` | `Id` + `CreatedAt` + `UpdatedAt` + `Status` | 需要状态管理（如Patient、Herb） |

**最常用**：`StatusDto`（涵盖大部分业务实体）

#### Step 2: 定义业务字段

**示例：PatientDto（患者信息DTO）**

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

    /// <summary>年龄（基于出生日期的计算属性）</summary>
    [DisplayName("年龄")]
    public int? Age
    {
        get
        {
            if (BirthDate == null) return null;
            var today = DateTime.Today;
            var age = today.Year - BirthDate.Value.Year;
            if (BirthDate.Value.Date > today.AddYears(-age)) age--;
            return age;
        }
    }

    /// <summary>身份证号</summary>
    [DisplayName("身份证号")]
    public string? IdNumber { get; set; }

    /// <summary>手机号码</summary>
    [DisplayName("手机号码")]
    public string? PhoneNumber { get; set; }

    /// <summary>地址</summary>
    [DisplayName("地址")]
    public string? Address { get; set; }

    /// <summary>过敏史</summary>
    [DisplayName("过敏史")]
    public string? AllergyHistory { get; set; }

    /// <summary>最后就诊时间</summary>
    [DisplayName("最后就诊时间")]
    public DateTime? LastVisitTime { get; set; }

    /// <summary>就诊次数</summary>
    [DisplayName("就诊次数")]
    public int VisitCount { get; set; }
}
```

#### Step 3: 字段设计要点

**✅ 推荐做法**：

| 规则 | 说明 | 示例 |
|------|------|------|
| **必填字段** | 使用非空类型 | `string Name` |
| **可选字段** | 使用可空类型 | `string? IdNumber`, `DateTime? BirthDate` |
| **计算属性** | 使用只读属性（无setter） | `int? Age { get; }` |
| **枚举字段** | 使用强类型枚举 | `Gender Gender`（不用`int`） |
| **所有字段** | 添加`[DisplayName]`标记 | `[DisplayName("患者姓名")]` |
| **隐藏敏感字段** | 不包含`PasswordHash`等 | ❌ 不要暴露密码、Token |

**❌ 避免做法**：

```csharp
// ❌ 错误示例1：包含敏感字段
public class UserDto : StatusDto
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty; // ❌ 不应暴露
}

// ❌ 错误示例2：包含Entity导航属性
public class PatientDto : StatusDto
{
    public string Name { get; set; } = string.Empty;
    public List<MedicalCase> MedicalCases { get; set; } // ❌ 应该用List<MedicalCaseDto>
}

// ❌ 错误示例3：使用int表示枚举
public class PatientDto : StatusDto
{
    public string Name { get; set; } = string.Empty;
    public int Gender { get; set; } // ❌ 应该用Gender枚举
}
```

---

## 创建创建DTO

### 目的
创建DTO用于从Client端提交新实体的数据（不包含ID，由Server端生成）。

### 步骤

#### Step 1: 提取共享字段（推荐）

为了避免创建DTO和更新DTO之间的重复代码，建议先创建一个共享的基础输入DTO。

```csharp
/// <summary>
/// 患者输入基础DTO - 提取创建和更新的共同字段
/// </summary>
public abstract class PatientInputBaseDto
{
    /// <summary>患者姓名</summary>
    [Required(ErrorMessage = "患者姓名不能为空")]
    [StringLength(50, ErrorMessage = "患者姓名长度不能超过50个字符")]
    [DisplayName("患者姓名")]
    public string Name { get; set; } = string.Empty;

    /// <summary>性别</summary>
    [DisplayName("性别")]
    public Gender Gender { get; set; } = Gender.Unknown;

    /// <summary>出生日期</summary>
    [DisplayName("出生日期")]
    public DateTime? BirthDate { get; set; }

    /// <summary>身份证号</summary>
    [StringLength(18, ErrorMessage = "身份证号长度不能超过18个字符")]
    [RegularExpression(@"^\d{15}$|^\d{17}[\dXx]$", ErrorMessage = "身份证号格式不正确")]
    [DisplayName("身份证号")]
    public string? IdNumber { get; set; }

    /// <summary>手机号码</summary>
    [StringLength(11, ErrorMessage = "手机号码长度不能超过11个字符")]
    [DisplayName("手机号码")]
    public string? PhoneNumber { get; set; }

    /// <summary>地址</summary>
    [StringLength(200, ErrorMessage = "地址长度不能超过200个字符")]
    [DisplayName("地址")]
    public string? Address { get; set; }

    /// <summary>过敏史</summary>
    [StringLength(500, ErrorMessage = "过敏史长度不能超过500个字符")]
    [DisplayName("过敏史")]
    public string? AllergyHistory { get; set; }

    /// <summary>状态</summary>
    [DisplayName("状态")]
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;
}
```

#### Step 2: 继承基础DTO

```csharp
/// <summary>
/// 患者创建DTO - 继承输入基础DTO
/// </summary>
public class PatientCreateDto : PatientInputBaseDto
{
    // 继承所有字段，无需额外定义
    // ID、CreatedAt、UpdatedAt由Server端自动生成
}
```

#### Step 3: 验证规则要点

| 规则类型 | 使用工具 | 示例 |
|---------|---------|------|
| **必填验证** | `[Required]` | `[Required(ErrorMessage = "患者姓名不能为空")]` |
| **长度验证** | `[StringLength]` | `[StringLength(50, ErrorMessage = "长度不能超过50个字符")]` |
| **格式验证** | `[RegularExpression]` | 身份证号、手机号格式 |
| **范围验证** | `[Range]` | `[Range(0, 150, ErrorMessage = "年龄必须在0-150之间")]` |

**验证常量统一管理**：

```csharp
// LYBT.Shared.Models/Constants/ValidationConstants.cs
public static class ValidationConstants
{
    public const int NameMaxLength = 50;
    public const int PhoneMaxLength = 11;
    public const int AddressMaxLength = 200;
    public const int RemarkMaxLength = 500;

    public const string IdCardRegex = @"^\d{15}$|^\d{17}[\dXx]$";
    public const string PhoneRegex = @"^1[3-9]\d{9}$";
}

// 使用方式
[StringLength(ValidationConstants.NameMaxLength,
    ErrorMessage = "患者姓名长度不能超过{1}个字符")]
public string Name { get; set; } = string.Empty;
```

---

## 创建更新DTO

### 目的
更新DTO用于从Client端提交实体的更新数据（必须包含ID用于标识）。

### 步骤

#### Step 1: 继承基础DTO并实现IIdentifiable接口

```csharp
/// <summary>
/// 患者更新DTO - 继承输入基础DTO并实现ID接口
/// </summary>
public class PatientUpdateDto : PatientInputBaseDto, IIdentifiable<Guid>
{
    /// <summary>患者ID</summary>
    [Required(ErrorMessage = "患者ID不能为空")]
    [DisplayName("患者ID")]
    public Guid Id { get; set; }
}
```

#### Step 2: 关键点说明

| 关键点 | 说明 |
|--------|------|
| **必须包含ID** | 用于标识要更新的实体 |
| **ID必须验证** | `[Required]`确保ID不为空 |
| **不包含CreatedAt** | 创建时间不可修改 |
| **UpdatedAt由Server端设置** | Client端不需要传递 |

**✅ 正确的更新DTO**：

```csharp
public class PatientUpdateDto : PatientInputBaseDto, IIdentifiable<Guid>
{
    [Required(ErrorMessage = "患者ID不能为空")]
    public Guid Id { get; set; }

    // ✅ 包含所有可更新的业务字段（继承自PatientInputBaseDto）
    // ❌ 不包含CreatedAt（由系统维护）
    // ❌ 不包含UpdatedAt（由系统自动设置）
}
```

---

## 创建搜索DTO

### 目的
搜索DTO用于提供搜索条件和分页参数。

### 步骤

#### Step 1: 继承PagedQueryBaseDto

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

    /// <summary>性别</summary>
    [DisplayName("性别")]
    public Gender? Gender { get; set; }

    /// <summary>状态</summary>
    [DisplayName("状态")]
    public CommonStatus? Status { get; set; }

    /// <summary>创建日期范围-开始</summary>
    [DisplayName("创建日期范围-开始")]
    public DateTime? CreateStartDate { get; set; }

    /// <summary>创建日期范围-结束</summary>
    [DisplayName("创建日期范围-结束")]
    public DateTime? CreateEndDate { get; set; }

    /// <summary>最小年龄</summary>
    [DisplayName("最小年龄")]
    public int? MinAge { get; set; }

    /// <summary>最大年龄</summary>
    [DisplayName("最大年龄")]
    public int? MaxAge { get; set; }
}
```

#### Step 2: 搜索DTO设计要点

| 规则 | 说明 | 示例 |
|------|------|------|
| **所有字段可空** | 使用`?`标记 | `string? Name`, `Gender? Gender` |
| **日期范围** | 使用StartDate/EndDate命名 | `CreateStartDate`, `CreateEndDate` |
| **数值范围** | 使用Min/Max前缀 | `MinAge`, `MaxAge` |
| **继承分页基类** | `PagedQueryBaseDto` | 自动获得`PageIndex`, `PageSize` |

**PagedQueryBaseDto定义**：

```csharp
/// <summary>
/// 分页查询基础DTO
/// </summary>
public abstract class PagedQueryBaseDto
{
    /// <summary>页码（从1开始）</summary>
    [Range(1, int.MaxValue, ErrorMessage = "页码必须大于0")]
    [DisplayName("页码")]
    public int PageIndex { get; set; } = 1;

    /// <summary>每页大小</summary>
    [Range(1, 100, ErrorMessage = "每页大小必须在1-100之间")]
    [DisplayName("每页大小")]
    public int PageSize { get; set; } = 10;
}
```

---

## DataAnnotations验证

### 常用验证特性

#### 1. Required - 必填验证

```csharp
/// <summary>患者姓名</summary>
[Required(ErrorMessage = "患者姓名不能为空")]
[DisplayName("患者姓名")]
public string Name { get; set; } = string.Empty;
```

**注意**：`ErrorMessage`必须提供中文提示。

#### 2. StringLength - 长度验证

```csharp
/// <summary>患者姓名</summary>
[StringLength(50, MinimumLength = 2,
    ErrorMessage = "患者姓名长度必须在{2}-{1}个字符之间")]
[DisplayName("患者姓名")]
public string Name { get; set; } = string.Empty;
```

**参数说明**：
- `{1}` = `MaximumLength`（50）
- `{2}` = `MinimumLength`（2）

#### 3. RegularExpression - 格式验证

```csharp
/// <summary>身份证号</summary>
[RegularExpression(@"^\d{15}$|^\d{17}[\dXx]$",
    ErrorMessage = "身份证号格式不正确")]
[DisplayName("身份证号")]
public string? IdNumber { get; set; }

/// <summary>手机号码</summary>
[RegularExpression(@"^1[3-9]\d{9}$",
    ErrorMessage = "手机号码格式不正确")]
[DisplayName("手机号码")]
public string? PhoneNumber { get; set; }
```

#### 4. Range - 范围验证

```csharp
/// <summary>年龄</summary>
[Range(0, 150, ErrorMessage = "年龄必须在{1}-{2}之间")]
[DisplayName("年龄")]
public int Age { get; set; }
```

#### 5. Compare - 字段比较

```csharp
/// <summary>密码</summary>
[Required(ErrorMessage = "密码不能为空")]
[DisplayName("密码")]
public string Password { get; set; } = string.Empty;

/// <summary>确认密码</summary>
[Compare(nameof(Password), ErrorMessage = "两次密码输入不一致")]
[DisplayName("确认密码")]
public string ConfirmPassword { get; set; } = string.Empty;
```

### 验证特性组合示例

```csharp
/// <summary>
/// 患者创建DTO - 完整验证示例
/// </summary>
public class PatientCreateDto
{
    /// <summary>患者姓名</summary>
    [Required(ErrorMessage = "患者姓名不能为空")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "患者姓名长度必须在2-50个字符之间")]
    [DisplayName("患者姓名")]
    public string Name { get; set; } = string.Empty;

    /// <summary>性别</summary>
    [Required(ErrorMessage = "性别不能为空")]
    [DisplayName("性别")]
    public Gender Gender { get; set; }

    /// <summary>手机号码</summary>
    [StringLength(11, ErrorMessage = "手机号码长度不能超过11个字符")]
    [RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "手机号码格式不正确")]
    [DisplayName("手机号码")]
    public string? PhoneNumber { get; set; }

    /// <summary>年龄</summary>
    [Range(0, 150, ErrorMessage = "年龄必须在0-150之间")]
    [DisplayName("年龄")]
    public int? Age { get; set; }
}
```

---

## FluentValidation验证

### 何时使用FluentValidation

| 场景 | 工具 |
|------|------|
| **简单验证** | DataAnnotations |
| **条件验证** | FluentValidation |
| **跨字段验证** | FluentValidation |
| **异步验证** | FluentValidation |
| **自定义错误消息** | FluentValidation |

### 创建验证器

#### Step 1: 创建验证器类

**文件位置**：`LYBT.Shared.Models/Validators/{模块名}/`

```csharp
using FluentValidation;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Constants;

namespace LYBT.Shared.Models.Validators.Patients
{
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
                .MaximumLength(ValidationConstants.NameMaxLength)
                .WithMessage("患者姓名长度不能超过{MaxLength}个字符");

            // 性别验证
            RuleFor(x => x.Gender)
                .IsInEnum().WithMessage("性别值无效");

            // 手机号码验证（可选，但格式必须正确）
            RuleFor(x => x.PhoneNumber)
                .Matches(ValidationConstants.PhoneRegex)
                .WithMessage("手机号码格式不正确")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            // 身份证号码验证（可选，但格式必须正确）
            RuleFor(x => x.IdNumber)
                .Matches(ValidationConstants.IdCardRegex)
                .WithMessage("身份证号码格式不正确")
                .When(x => !string.IsNullOrEmpty(x.IdNumber));

            // 出生日期验证（不能是未来日期）
            RuleFor(x => x.BirthDate)
                .LessThanOrEqualTo(DateTime.Today)
                .WithMessage("出生日期不能是未来日期")
                .When(x => x.BirthDate.HasValue);

            // 地址长度验证
            RuleFor(x => x.Address)
                .MaximumLength(ValidationConstants.AddressMaxLength)
                .WithMessage("地址长度不能超过{MaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Address));
        }
    }
}
```

#### Step 2: 常用验证规则

| 规则方法 | 说明 | 示例 |
|---------|------|------|
| `NotEmpty()` | 不为空 | `RuleFor(x => x.Name).NotEmpty()` |
| `NotNull()` | 不为null | `RuleFor(x => x.Gender).NotNull()` |
| `MaximumLength(n)` | 最大长度 | `RuleFor(x => x.Name).MaximumLength(50)` |
| `Matches(regex)` | 正则匹配 | `RuleFor(x => x.Phone).Matches(@"^1[3-9]\d{9}$")` |
| `IsInEnum()` | 枚举有效性 | `RuleFor(x => x.Gender).IsInEnum()` |
| `LessThanOrEqualTo()` | 小于等于 | `RuleFor(x => x.BirthDate).LessThanOrEqualTo(DateTime.Today)` |
| `When(condition)` | 条件验证 | `.When(x => x.PhoneNumber != null)` |

#### Step 3: 跨字段验证

```csharp
/// <summary>
/// 医案创建DTO验证器 - 跨字段验证示例
/// </summary>
public class MedicalCaseCreateDtoValidator : AbstractValidator<MedicalCaseCreateDto>
{
    public MedicalCaseCreateDtoValidator()
    {
        // 创建日期验证（不能早于患者出生日期）
        RuleFor(x => x)
            .Must(dto => ValidateVisitDate(dto.VisitDate, dto.PatientBirthDate))
            .WithMessage("就诊日期不能早于患者出生日期")
            .When(x => x.VisitDate.HasValue && x.PatientBirthDate.HasValue);

        // 处方剂数验证（如果有处方，剂数必须>0）
        RuleFor(x => x.PrescriptionQuantity)
            .GreaterThan(0)
            .WithMessage("处方剂数必须大于0")
            .When(x => x.HasPrescription);
    }

    private bool ValidateVisitDate(DateTime? visitDate, DateTime? birthDate)
    {
        if (!visitDate.HasValue || !birthDate.HasValue) return true;
        return visitDate.Value >= birthDate.Value;
    }
}
```

#### Step 4: 异步验证（数据库查询）

```csharp
/// <summary>
/// 患者创建DTO验证器 - 异步验证示例
/// </summary>
public class PatientCreateDtoValidator : AbstractValidator<PatientCreateDto>
{
    private readonly IPatientRepository _patientRepository;

    public PatientCreateDtoValidator(IPatientRepository patientRepository)
    {
        _patientRepository = patientRepository;

        // 手机号码唯一性验证（异步）
        RuleFor(x => x.PhoneNumber)
            .MustAsync(async (phoneNumber, cancellation) =>
            {
                if (string.IsNullOrEmpty(phoneNumber)) return true;
                var existing = await _patientRepository.GetByPhoneNumberAsync(phoneNumber);
                return existing == null;
            })
            .WithMessage("手机号码已被使用")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

        // 身份证号唯一性验证（异步）
        RuleFor(x => x.IdNumber)
            .MustAsync(async (idNumber, cancellation) =>
            {
                if (string.IsNullOrEmpty(idNumber)) return true;
                var existing = await _patientRepository.GetByIdNumberAsync(idNumber);
                return existing == null;
            })
            .WithMessage("身份证号已被使用")
            .When(x => !string.IsNullOrEmpty(x.IdNumber));
    }
}
```

### 注册验证器

**在Startup.cs或ModuleExtensions.cs中注册**：

```csharp
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

public static class SharedModelsModule
{
    public static IServiceCollection AddSharedModelsValidators(
        this IServiceCollection services)
    {
        // 注册所有验证器（自动扫描程序集）
        services.AddValidatorsFromAssemblyContaining<PatientCreateDtoValidator>();

        // 或者手动注册
        services.AddScoped<IValidator<PatientCreateDto>, PatientCreateDtoValidator>();
        services.AddScoped<IValidator<PatientUpdateDto>, PatientUpdateDtoValidator>();

        return services;
    }
}
```

---

## AutoMapper映射配置

### 创建映射配置

#### Step 1: 创建Profile类

**文件位置**：`LYBT.Module.{模块名}/Mapping/{实体名}MappingProfile.cs`

```csharp
using AutoMapper;
using LYBT.Entities.Models;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Module.Patients.Mapping
{
    /// <summary>
    /// 患者映射配置
    /// </summary>
    public class PatientMappingProfile : Profile
    {
        public PatientMappingProfile()
        {
            // ========== Entity → Dto（查询） ==========
            CreateMap<Patient, PatientDto>()
                .ReverseMap(); // 双向映射

            // ========== CreateDto → Entity（创建） ==========
            CreateMap<PatientCreateDto, Patient>()
                .ForMember(dest => dest.Id, opt => opt.Ignore()) // ID由系统生成
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // 时间由系统设置
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());

            // ========== UpdateDto → Entity（更新） ==========
            CreateMap<PatientUpdateDto, Patient>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore()) // 创建时间不可修改
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.Now)); // 自动设置更新时间
        }
    }
}
```

#### Step 2: 映射配置要点

| 场景 | 配置方法 | 说明 |
|------|---------|------|
| **双向映射** | `.ReverseMap()` | Entity ↔ Dto |
| **忽略字段** | `.ForMember(dest => dest.Id, opt => opt.Ignore())` | ID由系统生成 |
| **自动设置时间** | `.ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.Now))` | 更新时间 |
| **条件映射** | `.ForAllMembers(opts => opts.Condition((src, dest, srcMember) => srcMember != null))` | 忽略null值 |
| **字段名映射** | `.ForMember(dest => dest.BirthDate, opt => opt.MapFrom(src => src.DateOfBirth))` | 字段名不一致 |

#### Step 3: 复杂映射示例

**嵌套DTO映射**：

```csharp
/// <summary>
/// 医案映射配置 - 嵌套DTO映射
/// </summary>
public class MedicalCaseMappingProfile : Profile
{
    public MedicalCaseMappingProfile()
    {
        // ========== Entity → Dto（包含嵌套对象） ==========
        CreateMap<MedicalCase, MedicalCaseDto>()
            .ForMember(dest => dest.Consultation,
                opt => opt.MapFrom(src => src.Consultation)) // 嵌套Consultation
            .ForMember(dest => dest.Prescription,
                opt => opt.MapFrom(src => src.Prescription)) // 嵌套Prescription
            .ForMember(dest => dest.PatientName,
                opt => opt.MapFrom(src => src.Patient.Name)); // 导航属性展开

        // ========== Consultation Entity → ConsultationDetailDto ==========
        CreateMap<Consultation, ConsultationDetailDto>();

        // ========== Prescription Entity → PrescriptionDetailDto ==========
        CreateMap<Prescription, PrescriptionDetailDto>()
            .ForMember(dest => dest.Items,
                opt => opt.MapFrom(src => src.PrescriptionItems)); // 集合映射
    }
}
```

### 注册AutoMapper

**在Startup.cs或ModuleExtensions.cs中注册**：

```csharp
using Microsoft.Extensions.DependencyInjection;

public static class PatientsModule
{
    public static IServiceCollection AddPatientsModule(
        this IServiceCollection services)
    {
        // 注册AutoMapper（自动扫描Profile）
        services.AddAutoMapper(typeof(PatientMappingProfile).Assembly);

        return services;
    }
}
```

### 使用AutoMapper

**在Service层中使用**：

```csharp
public class PatientService : IPatientService
{
    private readonly IMapper _mapper;
    private readonly IPatientRepository _repository;

    public PatientService(IMapper mapper, IPatientRepository repository)
    {
        _mapper = mapper;
        _repository = repository;
    }

    // 查询：Entity → Dto
    public async Task<PatientDto?> GetByIdAsync(Guid id)
    {
        var patient = await _repository.GetByIdAsync(id);
        return _mapper.Map<PatientDto>(patient);
    }

    // 创建：CreateDto → Entity
    public async Task<PatientDto> CreateAsync(PatientCreateDto dto)
    {
        var patient = _mapper.Map<Patient>(dto);
        patient.Id = Guid.NewGuid();
        patient.CreatedAt = DateTime.Now;

        await _repository.AddAsync(patient);

        return _mapper.Map<PatientDto>(patient);
    }

    // 更新：UpdateDto → Entity
    public async Task<PatientDto> UpdateAsync(Guid id, PatientUpdateDto dto)
    {
        var patient = await _repository.GetByIdAsync(id);
        if (patient == null) throw new NotFoundException("患者不存在");

        _mapper.Map(dto, patient); // 将dto数据映射到现有patient
        patient.UpdatedAt = DateTime.Now;

        await _repository.UpdateAsync(patient);

        return _mapper.Map<PatientDto>(patient);
    }
}
```

---

## 接口实现模式

### IHerbItem接口实现

**使用场景**：处方药材项需要实现`IHerbItem`接口，以便使用跨端共享的`HerbCalculatorBase`和`HerbValidatorBase`。

#### Step 1: 查看接口定义

```csharp
// LYBT.Shared.Components/IHerbItem.cs
public interface IHerbItem
{
    int HerbId { get; set; }
    string HerbName { get; set; }
    decimal Dosage { get; set; }
    string Unit { get; set; }
    decimal Quantity { get; set; }
    decimal UnitPrice { get; set; }
}
```

#### Step 2: 在DTO中实现接口

```csharp
/// <summary>
/// 处方药材项DTO - 实现IHerbItem接口
/// </summary>
public class PrescriptionItemDto : IHerbItem
{
    /// <summary>药材ID</summary>
    [DisplayName("药材ID")]
    public int HerbId { get; set; }

    /// <summary>药材名称</summary>
    [Required(ErrorMessage = "药材名称不能为空")]
    [DisplayName("药材名称")]
    public string HerbName { get; set; } = string.Empty;

    /// <summary>剂量（单剂）</summary>
    [Range(0.1, 1000, ErrorMessage = "剂量必须在0.1-1000之间")]
    [DisplayName("剂量")]
    public decimal Dosage { get; set; }

    /// <summary>剂量单位</summary>
    [DisplayName("剂量单位")]
    public string Unit { get; set; } = "g";

    /// <summary>数量（剂数）</summary>
    [Range(1, 100, ErrorMessage = "数量必须在1-100之间")]
    [DisplayName("数量")]
    public decimal Quantity { get; set; }

    /// <summary>单价（元/克）</summary>
    [DisplayName("单价")]
    public decimal UnitPrice { get; set; }

    // ========== 扩展字段（非IHerbItem要求） ==========

    /// <summary>处方项ID</summary>
    [DisplayName("处方项ID")]
    public Guid Id { get; set; }

    /// <summary>小计（计算属性）</summary>
    [DisplayName("小计")]
    public decimal Subtotal => Dosage * Quantity * UnitPrice;
}
```

#### Step 3: 使用HerbCalculatorBase

```csharp
// Server端Service使用
public class PrescriptionService
{
    private readonly HerbCalculatorBase<PrescriptionItemDto> _calculator;

    public PrescriptionService()
    {
        _calculator = new PrescriptionCalculator(); // 继承HerbCalculatorBase
    }

    public decimal CalculatePrescriptionTotal(List<PrescriptionItemDto> items)
    {
        // 计算总剂量
        var totalDosage = _calculator.CalculateTotalDosage(items);

        // 计算总价
        var totalPrice = _calculator.CalculateTotalPrice(items);

        // 计算估算总价（考虑数量）
        var estimatedTotal = _calculator.CalculateEstimatedTotalPrice(items);

        return estimatedTotal;
    }
}

// PrescriptionCalculator实现
public class PrescriptionCalculator : HerbCalculatorBase<PrescriptionItemDto>
{
    // 继承所有计算方法，无需额外实现
}
```

---

## 完整实战案例

### 案例：创建药材（Herb）完整CRUD DTO

#### Step 1: 创建文件

**文件位置**：`LYBT.Shared.Models/Contracts/Herbs/HerbDtos.cs`

#### Step 2: 定义查询DTO

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Constants;

namespace LYBT.Shared.Models.Contracts.Herbs
{
    /// <summary>
    /// 药材信息DTO
    /// </summary>
    public class HerbDto : StatusDto
    {
        /// <summary>药材名称</summary>
        [DisplayName("药材名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>分类</summary>
        [DisplayName("分类")]
        public string? Category { get; set; }

        /// <summary>功效</summary>
        [DisplayName("功效")]
        public string? Effects { get; set; }

        /// <summary>单价（元/克）</summary>
        [DisplayName("单价")]
        public decimal? UnitPrice { get; set; }

        /// <summary>默认计量单位</summary>
        [DisplayName("默认计量单位")]
        public string? DefaultUnit { get; set; }

        /// <summary>常用剂量</summary>
        [DisplayName("常用剂量")]
        public string? DefaultDosage { get; set; }

        /// <summary>拼音首字母（快速检索）</summary>
        [DisplayName("拼音首字母")]
        public string? PinyinAbbreviation { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Notes { get; set; }
    }
}
```

#### Step 3: 定义输入基础DTO

```csharp
/// <summary>
/// 药材输入基础DTO - 提取创建和更新的共同字段
/// </summary>
public abstract class HerbInputBaseDto
{
    /// <summary>药材名称</summary>
    [Required(ErrorMessage = "药材名称不能为空")]
    [StringLength(ValidationConstants.NameMaxLength,
        ErrorMessage = "药材名称长度不能超过{1}个字符")]
    [DisplayName("药材名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>分类</summary>
    [StringLength(50, ErrorMessage = "分类长度不能超过50个字符")]
    [DisplayName("分类")]
    public string? Category { get; set; }

    /// <summary>功效</summary>
    [StringLength(ValidationConstants.RemarkMaxLength,
        ErrorMessage = "功效长度不能超过{1}个字符")]
    [DisplayName("功效")]
    public string? Effects { get; set; }

    /// <summary>单价（元/克）</summary>
    [Range(0.01, 10000, ErrorMessage = "单价必须在0.01-10000之间")]
    [DisplayName("单价")]
    public decimal? UnitPrice { get; set; }

    /// <summary>默认计量单位</summary>
    [StringLength(10, ErrorMessage = "计量单位长度不能超过10个字符")]
    [DisplayName("默认计量单位")]
    public string? DefaultUnit { get; set; }

    /// <summary>常用剂量</summary>
    [StringLength(20, ErrorMessage = "常用剂量长度不能超过20个字符")]
    [DisplayName("常用剂量")]
    public string? DefaultDosage { get; set; }

    /// <summary>备注</summary>
    [StringLength(ValidationConstants.RemarkMaxLength,
        ErrorMessage = "备注长度不能超过{1}个字符")]
    [DisplayName("备注")]
    public string? Notes { get; set; }

    /// <summary>状态</summary>
    [DisplayName("状态")]
    public CommonStatus Status { get; set; } = CommonStatus.Enabled;
}
```

#### Step 4: 定义创建DTO

```csharp
/// <summary>
/// 药材创建DTO
/// </summary>
public class HerbCreateDto : HerbInputBaseDto
{
    // 继承所有字段，无需额外定义
}
```

#### Step 5: 定义更新DTO

```csharp
/// <summary>
/// 药材更新DTO
/// </summary>
public class HerbUpdateDto : HerbInputBaseDto, IIdentifiable<Guid>
{
    /// <summary>药材ID</summary>
    [Required(ErrorMessage = "药材ID不能为空")]
    [DisplayName("药材ID")]
    public Guid Id { get; set; }
}
```

#### Step 6: 定义搜索DTO

```csharp
/// <summary>
/// 药材搜索DTO
/// </summary>
public class HerbSearchDto : PagedQueryBaseDto
{
    /// <summary>药材名称（模糊搜索）</summary>
    [DisplayName("药材名称")]
    public string? Name { get; set; }

    /// <summary>分类</summary>
    [DisplayName("分类")]
    public string? Category { get; set; }

    /// <summary>拼音首字母（模糊搜索）</summary>
    [DisplayName("拼音首字母")]
    public string? PinyinAbbreviation { get; set; }

    /// <summary>状态</summary>
    [DisplayName("状态")]
    public CommonStatus? Status { get; set; }

    /// <summary>最小价格</summary>
    [DisplayName("最小价格")]
    public decimal? MinPrice { get; set; }

    /// <summary>最大价格</summary>
    [DisplayName("最大价格")]
    public decimal? MaxPrice { get; set; }
}
```

#### Step 7: 创建FluentValidation验证器

**文件位置**：`LYBT.Shared.Models/Validators/Herbs/HerbCreateDtoValidator.cs`

```csharp
using FluentValidation;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Constants;

namespace LYBT.Shared.Models.Validators.Herbs
{
    /// <summary>
    /// 药材创建DTO验证器
    /// </summary>
    public class HerbCreateDtoValidator : AbstractValidator<HerbCreateDto>
    {
        public HerbCreateDtoValidator()
        {
            // 药材名称验证
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("药材名称不能为空")
                .MaximumLength(ValidationConstants.NameMaxLength)
                .WithMessage("药材名称长度不能超过{MaxLength}个字符");

            // 单价验证（可选，但必须>0）
            RuleFor(x => x.UnitPrice)
                .GreaterThan(0).WithMessage("单价必须大于0")
                .When(x => x.UnitPrice.HasValue);

            // 功效长度验证
            RuleFor(x => x.Effects)
                .MaximumLength(ValidationConstants.RemarkMaxLength)
                .WithMessage("功效长度不能超过{MaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Effects));

            // 备注长度验证
            RuleFor(x => x.Notes)
                .MaximumLength(ValidationConstants.RemarkMaxLength)
                .WithMessage("备注长度不能超过{MaxLength}个字符")
                .When(x => !string.IsNullOrEmpty(x.Notes));
        }
    }
}
```

#### Step 8: 创建AutoMapper映射配置

**文件位置**：`LYBT.Module.Herbs/Mapping/HerbMappingProfile.cs`

```csharp
using AutoMapper;
using LYBT.Entities.Models;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Module.Herbs.Mapping
{
    /// <summary>
    /// 药材映射配置
    /// </summary>
    public class HerbMappingProfile : Profile
    {
        public HerbMappingProfile()
        {
            // ========== Entity → Dto（查询） ==========
            CreateMap<Herb, HerbDto>()
                .ReverseMap();

            // ========== CreateDto → Entity（创建） ==========
            CreateMap<HerbCreateDto, Herb>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.PinyinAbbreviation,
                    opt => opt.MapFrom(src => GeneratePinyinAbbreviation(src.Name)));

            // ========== UpdateDto → Entity（更新） ==========
            CreateMap<HerbUpdateDto, Herb>()
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.Now))
                .ForMember(dest => dest.PinyinAbbreviation,
                    opt => opt.MapFrom(src => GeneratePinyinAbbreviation(src.Name)));
        }

        /// <summary>
        /// 生成拼音首字母
        /// </summary>
        private static string GeneratePinyinAbbreviation(string name)
        {
            // 实际实现：使用NPinyin库生成拼音首字母
            // 这里仅为示例
            return name.Substring(0, Math.Min(name.Length, 3)).ToUpper();
        }
    }
}
```

#### Step 9: 在Service层使用

```csharp
public class HerbService : IHerbService
{
    private readonly IMapper _mapper;
    private readonly IHerbRepository _repository;
    private readonly IValidator<HerbCreateDto> _createValidator;
    private readonly IValidator<HerbUpdateDto> _updateValidator;

    public HerbService(
        IMapper mapper,
        IHerbRepository repository,
        IValidator<HerbCreateDto> createValidator,
        IValidator<HerbUpdateDto> updateValidator)
    {
        _mapper = mapper;
        _repository = repository;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<HerbDto> CreateAsync(HerbCreateDto dto)
    {
        // FluentValidation验证
        var validationResult = await _createValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // DTO → Entity
        var herb = _mapper.Map<Herb>(dto);
        herb.Id = Guid.NewGuid();
        herb.CreatedAt = DateTime.Now;

        // 保存
        await _repository.AddAsync(herb);

        // Entity → DTO
        return _mapper.Map<HerbDto>(herb);
    }

    public async Task<HerbDto> UpdateAsync(Guid id, HerbUpdateDto dto)
    {
        // FluentValidation验证
        var validationResult = await _updateValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 查询现有实体
        var herb = await _repository.GetByIdAsync(id);
        if (herb == null) throw new NotFoundException("药材不存在");

        // DTO → Entity（映射更新）
        _mapper.Map(dto, herb);
        herb.UpdatedAt = DateTime.Now;

        // 保存
        await _repository.UpdateAsync(herb);

        // Entity → DTO
        return _mapper.Map<HerbDto>(herb);
    }
}
```

---

## 常见问题与陷阱

### 问题1：DTO字段名与Entity不一致

**问题描述**：
```csharp
// Entity中使用DateOfBirth
public class Patient
{
    public DateTime? DateOfBirth { get; set; }
}

// DTO中使用BirthDate
public class PatientDto
{
    public DateTime? BirthDate { get; set; }
}
```

**解决方案**：
```csharp
// 在AutoMapper映射配置中显式指定
CreateMap<Patient, PatientDto>()
    .ForMember(dest => dest.BirthDate, opt => opt.MapFrom(src => src.DateOfBirth));
```

**最佳实践**：
- ✅ 统一命名：Entity和DTO使用相同的字段名（推荐）
- ✅ 如必须不同，使用AutoMapper显式映射

### 问题2：计算属性导致的映射错误

**问题描述**：
```csharp
public class PatientDto
{
    public DateTime? BirthDate { get; set; }

    // 只读计算属性（无setter）
    public int? Age
    {
        get
        {
            if (BirthDate == null) return null;
            var today = DateTime.Today;
            var age = today.Year - BirthDate.Value.Year;
            if (BirthDate.Value.Date > today.AddYears(-age)) age--;
            return age;
        }
    }
}
```

**问题**：AutoMapper尝试映射Age但失败（因为没有setter）

**解决方案**：
```csharp
// 在AutoMapper配置中忽略计算属性
CreateMap<Patient, PatientDto>()
    .ForMember(dest => dest.Age, opt => opt.Ignore());
```

### 问题3：忘记添加FluentValidation验证器

**问题描述**：
创建了验证器类但忘记注册到DI容器，导致验证不生效。

**解决方案**：
```csharp
// 在Startup.cs或ModuleExtensions.cs中注册
services.AddValidatorsFromAssemblyContaining<PatientCreateDtoValidator>();
```

### 问题4：CreateDto包含ID字段

**错误示例**：
```csharp
// ❌ 创建DTO不应包含ID（ID由Server端生成）
public class PatientCreateDto
{
    public Guid Id { get; set; } // ❌ 错误
    public string Name { get; set; } = string.Empty;
}
```

**正确做法**：
```csharp
// ✅ 创建DTO不包含ID
public class PatientCreateDto
{
    public string Name { get; set; } = string.Empty;
    // ID由Server端生成
}

// ✅ 在Service层创建时分配ID
public async Task<PatientDto> CreateAsync(PatientCreateDto dto)
{
    var patient = _mapper.Map<Patient>(dto);
    patient.Id = Guid.NewGuid(); // Server端生成ID
    patient.CreatedAt = DateTime.Now;

    await _repository.AddAsync(patient);

    return _mapper.Map<PatientDto>(patient);
}
```

### 问题5：UpdateDto不包含ID

**错误示例**：
```csharp
// ❌ 更新DTO必须包含ID用于标识
public class PatientUpdateDto
{
    public string Name { get; set; } = string.Empty;
    // ❌ 缺少ID字段
}
```

**正确做法**：
```csharp
// ✅ 更新DTO必须包含ID并实现IIdentifiable接口
public class PatientUpdateDto : PatientInputBaseDto, IIdentifiable<Guid>
{
    [Required(ErrorMessage = "患者ID不能为空")]
    public Guid Id { get; set; }
}
```

### 问题6：嵌套DTO包含Entity导航属性

**错误示例**：
```csharp
// ❌ DTO中包含Entity导航属性
public class MedicalCaseDto
{
    public Guid Id { get; set; }
    public List<Consultation> Consultations { get; set; } // ❌ 应该用ConsultationDto
}
```

**正确做法**：
```csharp
// ✅ 使用DTO表示关联关系
public class MedicalCaseDto
{
    public Guid Id { get; set; }
    public ConsultationDetailDto? Consultation { get; set; } // ✅ 使用DTO
    public PrescriptionDetailDto? Prescription { get; set; } // ✅ 使用DTO
}
```

### 问题7：SearchDto字段不可空

**错误示例**：
```csharp
// ❌ 搜索DTO字段必须全部可空
public class PatientSearchDto : PagedQueryBaseDto
{
    public string Name { get; set; } = string.Empty; // ❌ 应该是string?
    public Gender Gender { get; set; } // ❌ 应该是Gender?
}
```

**正确做法**：
```csharp
// ✅ 所有搜索条件字段都使用可空类型
public class PatientSearchDto : PagedQueryBaseDto
{
    public string? Name { get; set; } // ✅ 可空
    public Gender? Gender { get; set; } // ✅ 可空
    public CommonStatus? Status { get; set; } // ✅ 可空
}
```

---

## 检查清单

### DTO创建检查清单

#### 查询DTO（{Entity}Dto）

- [ ] 继承正确的基类（`StatusDto`、`TimestampDto`或`BaseDto`）
- [ ] 包含所有业务字段
- [ ] 必填字段使用非空类型（`string`）
- [ ] 可选字段使用可空类型（`string?`, `DateTime?`）
- [ ] 计算属性只有getter（无setter）
- [ ] 所有字段添加`[DisplayName]`标记
- [ ] 不包含敏感字段（PasswordHash、Token）
- [ ] 不包含Entity导航属性（使用DTO代替）
- [ ] 使用强类型枚举（不用`int`）

#### 创建DTO（{Entity}CreateDto）

- [ ] 继承输入基础DTO（如`PatientInputBaseDto`）
- [ ] 不包含ID字段（ID由Server端生成）
- [ ] 不包含CreatedAt、UpdatedAt字段
- [ ] 必填字段添加`[Required]`验证
- [ ] 字符串字段添加`[StringLength]`验证
- [ ] 格式字段添加`[RegularExpression]`验证
- [ ] 数值字段添加`[Range]`验证
- [ ] 使用`ValidationConstants`统一管理验证常量

#### 更新DTO（{Entity}UpdateDto）

- [ ] 继承输入基础DTO（如`PatientInputBaseDto`）
- [ ] 实现`IIdentifiable<Guid>`接口
- [ ] 包含`Id`字段并添加`[Required]`验证
- [ ] 不包含CreatedAt字段（创建时间不可修改）
- [ ] 不包含UpdatedAt字段（由Server端自动设置）

#### 搜索DTO（{Entity}SearchDto）

- [ ] 继承`PagedQueryBaseDto`
- [ ] 所有搜索条件字段使用可空类型
- [ ] 日期范围使用StartDate/EndDate命名
- [ ] 数值范围使用Min/Max前缀
- [ ] 添加`[DisplayName]`标记

### 验证配置检查清单

#### DataAnnotations验证

- [ ] 必填字段添加`[Required]`
- [ ] 字符串字段添加`[StringLength]`
- [ ] 格式字段添加`[RegularExpression]`
- [ ] 数值字段添加`[Range]`
- [ ] 所有验证都提供中文ErrorMessage
- [ ] 使用ValidationConstants统一常量

#### FluentValidation验证器

- [ ] 创建验证器类（继承`AbstractValidator<T>`）
- [ ] 所有必填字段使用`.NotEmpty()`
- [ ] 可选字段使用`.When()`条件验证
- [ ] 跨字段验证使用`.Must()`或`.MustAsync()`
- [ ] 所有验证规则提供中文错误消息
- [ ] 在DI容器中注册验证器

### AutoMapper配置检查清单

- [ ] 创建MappingProfile类
- [ ] Entity → Dto使用`.ReverseMap()`
- [ ] CreateDto → Entity忽略ID和时间戳
- [ ] UpdateDto → Entity忽略CreatedAt
- [ ] UpdateDto → Entity自动设置UpdatedAt
- [ ] 计算属性使用`.Ignore()`
- [ ] 字段名不一致使用`.MapFrom()`
- [ ] 嵌套对象正确映射
- [ ] 在DI容器中注册Profile

### 文件组织检查清单

- [ ] DTO文件放在`LYBT.Shared.Models/Contracts/{模块名}/`
- [ ] 文件名使用复数形式（`PatientDtos.cs`）
- [ ] 相关DTO放在同一文件
- [ ] 验证器放在`LYBT.Shared.Models/Validators/{模块名}/`
- [ ] AutoMapper Profile放在`LYBT.Module.{模块名}/Mapping/`
- [ ] 命名空间正确（`LYBT.Shared.Models.Contracts.{模块名}`）

---

## 参考资料

### 相关文档

- [DTO设计标准](../../explanation/architecture/shared/dto-design-standard.md) - DTO设计原则和规范
- [Shared.Models README](../../../src/Shared/LYBT.Shared.Models/README.md) - 完整的DTO项目文档
- [三层对齐架构](../../explanation/architecture/README.md) - 架构总览
- [跨端架构](../../explanation/architecture/shared/README.md) - 跨端共享原则

### 代码示例

- `src/Shared/LYBT.Shared.Models/Contracts/Patients/PatientDtos.cs` - 完整的CRUD DTO示例
- `src/Shared/LYBT.Shared.Models/Contracts/Common/DtoBase.cs` - DTO基类定义
- `src/Shared/LYBT.Shared.Models/Validators/Patients/PatientCreateDtoValidator.cs` - FluentValidation示例
- `src/Server/Modules/LYBT.Module.Patients/Mapping/PatientMappingProfile.cs` - AutoMapper配置示例

### 外部资源

- [AutoMapper官方文档](https://docs.automapper.org/) - AutoMapper完整参考
- [FluentValidation官方文档](https://docs.fluentvalidation.net/) - FluentValidation完整参考
- [DataAnnotations MSDN](https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations) - DataAnnotations API参考

---

**文档版本**: v1.0
**最后更新**: 2025-10-30
**维护者**: 项目架构组

🤖 Generated with [Claude Code](https://claude.com/claude-code)

Co-Authored-By: Claude <noreply@anthropic.com>
