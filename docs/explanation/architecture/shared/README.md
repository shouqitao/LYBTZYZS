# 共享架构指南

**版本**：v5.1 Validators迁移版（Epic #1773）
**更新时间**：2025-11-03
**对应代码层**：LYBT.Shared  

## 🏗️ 共享架构设计

凌隐宝堂中医诊所共享架构是连接Server端和Client端的桥梁，提供跨层共享的基础设施、数据模型、业务接口和技术标准。

```
LYBT.Shared (共享层)
├── Models/             # 数据模型和实体
├── Validators/         # ✨ FluentValidation验证规则（Epic #1773新增）
├── Infrastructure/     # 基础设施组件
├── Utilities/          # 工具类和扩展
├── Constants/          # 常量定义
└── Enums/             # 枚举类型
```

## 📐 核心组件详解

### 1. Models - 数据模型层

> **⚠️ 架构说明**：当前MVP阶段，Models采用**按业务模块组织DTO**结构，不使用平坦的DTOs/目录。

**职责**：定义数据传输对象、枚举、常量、异常类、扩展方法

**实际目录结构**（src/Shared/LYBT.Shared.Models/）：

```
Common/              # 通用DTO和基类
  ├── BatchIdsDto.cs           # 批量ID操作DTO
  ├── EnumItem.cs              # 枚举项DTO
  ├── PagedResult.cs           # 分页结果
  └── StatusDto.cs             # 状态DTO基类

Constants/           # 常量定义
  ├── ErrorMessageKeys.cs      # 错误消息键
  └── ValidationConstants.cs   # 验证常量

Contracts/           # DTO按业务模块组织（核心架构）
  ├── Auth/                    # 认证模块DTOs
  ├── Consultation/            # 诊断模块DTOs
  ├── Patients/                # 患者模块DTOs
  │   ├── PatientDtos.cs              # PatientDto, PatientDetailDto
  │   ├── PatientOperationDtos.cs     # 操作相关DTOs
  │   └── PatientStatisticsDtos.cs    # 统计相关DTOs
  ├── Prescriptions/           # 处方模块DTOs
  ├── MedicalCase/             # 病案模块DTOs
  └── ...

Core/                # 核心基类
  └── BaseAuthSession.cs       # 认证会话基类

Enums/               # 枚举定义
  ├── Gender.cs                # 性别枚举
  ├── MedicalCaseEnums.cs      # 病案相关枚举（Status, Type等）
  ├── UserRole.cs              # 用户角色
  ├── PrescriptionStatus.cs    # 处方状态
  └── ...（共9个枚举文件）

Exceptions/          # 异常类定义
  └── BusinessException.cs     # 业务异常基类

Extensions/          # 扩展方法
  ├── Application/             # 应用初始化扩展
  └── ServiceCollection/       # 服务集合扩展
```

**设计原则**：
- ✅ **按业务模块组织**：Contracts/Patients/而不是平坦的DTOs/目录
- ✅ **按功能分组**：PatientDtos.cs（基础）、PatientOperationDtos.cs（操作）、PatientStatisticsDtos.cs（统计）
- ✅ **清晰的命名空间**：`LYBT.Shared.Models.Contracts.Patients`
- ✅ **避免过度拆分**：相关DTOs放在同一个文件中（如PatientDto和PatientDetailDto）

**实际代码示例**：

```csharp
// Contracts/Patients/PatientDtos.cs
namespace LYBT.Shared.Models.Contracts.Patients
{
    /// &lt;summary&gt;
    /// 患者信息DTO - UltraThink v2.0简化版
    /// &lt;/summary&gt;
    public class PatientDto : StatusDto
    {
        public string Name { get; set; } = string.Empty;
        public Gender Gender { get; set; }
        public DateTime? BirthDate { get; set; }
        
        /// &lt;summary&gt;年龄（基于出生日期的计算属性）&lt;/summary&gt;
        public int? Age
        {
            get
            {
                if (BirthDate == null) return null;
                var today = DateTime.Today;
                var age = today.Year - BirthDate.Value.Year;
                if (BirthDate.Value.Date &gt; today.AddYears(-age)) age--;
                return age;
            }
        }
    }
}

// Common/PagedResult.cs - 通用分页结果
public class PagedResult&lt;T&gt;
{
    public List&lt;T&gt; Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int CurrentPage { get; set; }
    public int PageSize { get; set; }
    public int TotalPages =&gt; (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage =&gt; CurrentPage &lt; TotalPages;
    public bool HasPreviousPage =&gt; CurrentPage &gt; 1;
}

// Enums/Gender.cs - 枚举定义
public enum Gender
{
    Unknown = 0,
    Male = 1,
    Female = 2
}
```

**关键差异说明**：
- ❌ **文档描述**：Entities/, DTOs/, Requests/, Responses/, ViewModels/（平坦结构）
- ✅ **实际实现**：Contracts/{Module}/（按业务模块组织）+ Common/（通用）+ Constants/（常量）+ Enums/（枚举）
- **原因**：实际架构更符合MVP原则（够用即好），避免过度分层

---

### 2. Validators - 验证规则层（Epic #1773新增）

> **✨ 新增项目**（2025-11-01）：统一前后端验证规则，实现**一次定义、两端共享**，消除验证规则不一致问题。

**职责**：
- 定义FluentValidation验证规则
- 为InputDto提供验证器实现
- 前后端共享验证逻辑
- 数据格式验证（不包含业务规则验证）

**实际目录结构**（src/Shared/LYBT.Shared.Validators/）：

```
LYBT.Shared.Validators/
├── Auth/
│   ├── LoginRequestValidator.cs
│   ├── ChangePasswordRequestValidator.cs
│   └── SuperAdminLoginRequestValidator.cs
├── Consultation/
│   └── ConsultationInputDtoValidator.cs
├── Formula/
│   └── FormulaInputDtoValidator.cs
├── Herbs/
│   └── HerbInputDtoValidator.cs
├── MedicalCase/
│   ├── MedicalCaseCreateDtoValidator.cs
│   └── MedicalCaseUpdateDtoValidator.cs
├── Patients/
│   └── PatientInputDtoValidator.cs
├── Prescriptions/
│   ├── PrescriptionCreateDtoValidator.cs
│   └── PrescriptionEditDtoValidator.cs
└── Users/
    └── UserInputDtoValidator.cs
```

**设计原则**：

1. **一次定义、两端共享**
   - 验证规则在Shared.Validators定义
   - Server端和Client端同时使用
   - 保证前后端验证规则100%一致

2. **按模块组织**
   - 与Models/Contracts保持一致的目录结构
   - 易于定位和维护

3. **InputDto专属**
   - 只为InputDto（用于Create/Update操作）提供验证器
   - Dto（用于Read操作）不需要验证

4. **业务规则分离**
   - 只包含数据格式验证（字符串长度、必填项、格式等）
   - 不包含业务规则验证（如"患者是否存在"）
   - 业务规则由Service层或Domain层处理

#### 2.1 Validator示例

**PatientInputDtoValidator.cs**（患者输入验证器）：

```csharp
using FluentValidation;
using LYBT.Shared.Models.Contracts.Patients;

namespace LYBT.Shared.Validators.Patients
{
    /// <summary>
    /// 患者输入DTO验证器
    /// Epic #1773: 前后端共享验证规则
    /// </summary>
    public class PatientInputDtoValidator : AbstractValidator<PatientInputDto>
    {
        public PatientInputDtoValidator()
        {
            // 姓名验证
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("患者姓名不能为空")
                .MaximumLength(50).WithMessage("患者姓名长度不能超过50个字符");

            // 性别验证
            RuleFor(x => x.Gender)
                .IsInEnum().WithMessage("性别值无效");

            // 手机号验证（可选）
            RuleFor(x => x.PhoneNumber)
                .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确")
                .When(x => !string.IsNullOrEmpty(x.PhoneNumber));

            // 身份证号验证（可选）
            RuleFor(x => x.IdNumber)
                .Length(18).WithMessage("身份证号必须为18位")
                .Matches(@"^\d{17}[\dXx]$").WithMessage("身份证号格式不正确")
                .When(x => !string.IsNullOrEmpty(x.IdNumber));

            // 年龄验证（可选）
            RuleFor(x => x.Age)
                .InclusiveBetween(0, 150).WithMessage("年龄必须在0-150之间")
                .When(x => x.Age.HasValue);

            // 紧急联系人电话验证（如果提供了姓名则必须提供电话）
            RuleFor(x => x.EmergencyContactPhone)
                .NotEmpty().WithMessage("请提供紧急联系人电话")
                .Matches(@"^1[3-9]\d{9}$").WithMessage("紧急联系人电话格式不正确")
                .When(x => !string.IsNullOrEmpty(x.EmergencyContactName));
        }
    }
}
```

**特点说明**：
- ✅ **声明式**：使用FluentValidation的Fluent API定义验证规则
- ✅ **可读性**：规则清晰明了，易于理解和维护
- ✅ **条件验证**：使用`When()`实现条件验证（仅当字段有值时验证）
- ✅ **多语言支持**：中文错误消息，便于用户理解

#### 2.2 Server端集成（ASP.NET Core Pipeline）

**Module注册**（PatientsModule.cs）：

```csharp
using FluentValidation;
using FluentValidation.AspNetCore;
using LYBT.Shared.Validators.Patients;

public class PatientsModule : IModule
{
    public void ConfigureServices(IServiceCollection services)
    {
        // ⭐ 注册FluentValidation
        services.AddFluentValidationAutoValidation();          // 自动验证
        services.AddFluentValidationClientsideAdapters();      // 客户端适配器

        // ⭐ 注册Shared.Validators的Validators
        services.AddValidatorsFromAssemblyContaining<PatientInputDtoValidator>();

        // 其他服务注册...
    }
}
```

**Controller自动验证**：

```csharp
[ApiController]
[Route("api/patients")]
public class PatientsController : BaseApiController
{
    private readonly IPatientService _patientService;

    [HttpPost]
    public async Task<ActionResult<PatientDto>> CreatePatient(
        [FromBody] PatientInputDto inputDto)  // ⭐ 自动验证
    {
        // ⭐ inputDto已通过PatientInputDtoValidator验证
        // 如验证失败，ASP.NET Core自动返回400 Bad Request + 错误详情
        var patient = await _patientService.CreatePatientAsync(inputDto);
        return Ok(patient);
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<PatientDto>> UpdatePatient(
        Guid id,
        [FromBody] PatientInputDto inputDto)  // ⭐ 自动验证
    {
        // ⭐ inputDto已通过验证
        var patient = await _patientService.UpdatePatientAsync(inputDto);
        return Ok(patient);
    }
}
```

**验证失败响应示例**：

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "errors": {
    "Name": [
      "患者姓名不能为空"
    ],
    "PhoneNumber": [
      "手机号格式不正确"
    ]
  }
}
```

#### 2.3 Client端集成（Component验证）

**PatientValidator.cs**（Client端验证组件）：

```csharp
using FluentValidation;
using FluentValidation.Results;
using LYBT.Shared.Validators.Patients;

namespace LYBT.Desktop.Patients.ViewModels.Components
{
    /// <summary>
    /// 患者验证器 - 组件化架构
    /// 集成FluentValidation Validators提供组件级验证接口
    /// Epic #1773 Task 4: Patients模块组件化改造
    /// </summary>
    public class PatientValidator
    {
        private readonly IValidator<PatientInputDto> _patientInputValidator;  // ⭐ 来自Shared.Validators
        private readonly ILogger<PatientValidator> _logger;

        public PatientValidator(
            IValidator<PatientInputDto> patientInputValidator,
            ILogger<PatientValidator> logger)
        {
            _patientInputValidator = patientInputValidator;
            _logger = logger;
        }

        /// <summary>
        /// 验证患者输入DTO
        /// </summary>
        public async Task<ValidationResult> ValidatePatientInputAsync(PatientInputDto inputDto)
        {
            if (inputDto == null)
            {
                return new ValidationResult(new[] { new ValidationFailure("Patient", "患者数据为空") });
            }

            _logger.LogDebug("开始验证患者输入: {PatientName}", inputDto.Name);
            var result = await _patientInputValidator.ValidateAsync(inputDto);

            if (!result.IsValid)
            {
                _logger.LogWarning("患者输入验证失败，错误数量: {ErrorCount}", result.Errors.Count);
            }

            return result;
        }

        /// <summary>
        /// 检查验证结果是否有效
        /// </summary>
        public bool IsValid(ValidationResult result, out string errorMessage)
        {
            if (result.IsValid)
            {
                errorMessage = string.Empty;
                return true;
            }

            errorMessage = string.Join("; ", result.Errors.Select(e => e.ErrorMessage));
            return false;
        }
    }
}
```

**ViewModel使用示例**：

```csharp
private async void HandlePatientSaved()
{
    try
    {
        // ⭐ 验证数据（委托给Validator组件）
        if (Patient != null)
        {
            var inputDto = _validator.ConvertToInputDto(Patient);
            var validationResult = await _validator.ValidatePatientInputAsync(inputDto);

            if (!_validator.IsValid(validationResult, out string errorMessage))
            {
                await ShowErrorMessageAsync($"数据验证失败: {errorMessage}");
                return;  // ⭐ 验证失败，终止保存
            }
        }

        // ⭐ 验证通过，保存数据
        var success = await _dataManager.SaveAsync();
        if (success)
        {
            await ShowSuccessMessageAsync("患者信息保存成功");
        }
    }
    catch (Exception ex)
    {
        await ShowErrorMessageAsync($"保存失败: {ex.Message}");
    }
}
```

**DI注册**（PatientsModule.cs）：

```csharp
public void RegisterTypes(IContainerRegistry containerRegistry)
{
    // ⭐ 注册Shared.Validators的Validator
    containerRegistry.RegisterSingleton<IValidator<PatientInputDto>, PatientInputDtoValidator>();

    // ⭐ 注册Component（使用Shared.Validators）
    containerRegistry.Register<PatientValidator>();

    // 其他注册...
}
```

#### 2.4 迁移历史

**迁移前**（Phase 1 - 2025-10-31之前）：

```
Server端：
  src/Server/Modules/LYBT.Module.Patients/
    └── Validators/
        └── PatientInputDtoValidator.cs  ⚠️ Server端专属

Client端：
  - 无统一验证
  - 部分使用DataAnnotations
  - 验证规则分散在ViewModel
```

**迁移后**（Phase 2 - Epic #1773，2025-11-01）：

```
Shared层：
  src/Shared/LYBT.Shared.Validators/
    └── Patients/
        └── PatientInputDtoValidator.cs  ✅ 前后端共享

Server端：
  src/Server/Modules/LYBT.Module.Patients/
    ├── Validators/ （❌ 已移除）
    └── PatientsModule.cs（添加Shared.Validators引用）

Client端：
  src/Client/Desktop/Modules/LYBT.Desktop.Patients/
    ├── ViewModels/Components/
    │   └── PatientValidator.cs（集成Shared.Validators）
    └── PatientsModule.cs（添加Shared.Validators引用）
```

**迁移收益**：
- ✅ **验证规则一致性**：前后端使用相同的Validator，验证规则100%一致
- ✅ **减少重复代码**：验证规则只需定义一次
- ✅ **维护成本降低**：验证规则修改只需一处变更
- ✅ **测试简化**：Validator可独立测试，测试用例可前后端复用

#### 2.5 架构约束

**只验证数据格式，不验证业务规则**：

```csharp
// ✅ 正确示例（数据格式验证）
RuleFor(x => x.Name)
    .NotEmpty().WithMessage("患者姓名不能为空")
    .MaximumLength(50).WithMessage("患者姓名长度不能超过50个字符");

RuleFor(x => x.PhoneNumber)
    .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确");

// ❌ 错误示例（业务规则验证 - 不应在Validator中）
RuleFor(x => x.PatientId)
    .MustAsync(async (id, _) => await PatientExists(id))  // ❌ 业务规则
    .WithMessage("患者不存在");

RuleFor(x => x.PhoneNumber)
    .MustAsync(async (phone, _) => !await PhoneNumberExists(phone))  // ❌ 业务规则
    .WithMessage("手机号已被使用");
```

**业务规则验证应在Service层处理**：

```csharp
// ✅ 正确做法：业务规则在Service层验证
public async Task<PatientDto> CreatePatientAsync(PatientInputDto inputDto)
{
    // 1. FluentValidation自动完成数据格式验证（ASP.NET Core Pipeline）

    // 2. 业务规则验证（Service层）
    if (await _patientRepository.PhoneNumberExistsAsync(inputDto.PhoneNumber))
    {
        throw new BusinessException("手机号已被使用");
    }

    // 3. 创建患者
    var patient = await _patientRepository.CreateAsync(inputDto);
    return patient;
}
```

**架构原因**：
- Shared.Validators不应依赖Repository或Service（避免循环依赖）
- 业务规则可能需要访问数据库或外部服务
- 业务规则可能随业务变化而变化，应与数据格式验证分离

#### 2.6 优势总结

**验证规则一致性**：
- ✅ 前后端使用相同的Validator
- ✅ 验证规则100%一致
- ✅ 避免"前端验证通过，后端验证失败"的问题

**开发效率提升**：
- ✅ 验证规则只需定义一次
- ✅ 减少50%的验证代码
- ✅ 新增字段只需修改一处

**可维护性提升**：
- ✅ 验证规则集中管理
- ✅ 修改验证规则只需一处变更
- ✅ 易于定位和修复验证问题

**测试简化**：
- ✅ Validator可独立测试
- ✅ 测试用例可前后端复用
- ✅ 减少重复测试代码

---

### 3. Interfaces - 接口定义层（已移除）

> **⚠️ 项目状态**：Shared.Interfaces项目已于2025-10-31被彻底移除（Issue #1728）。
> 该决策基于**MVP架构原则**：Server和Client端分别定义各自的接口，避免过早抽象。

**历史背景与移除原因**（MVP架构原则）：

当前v5.0架构采用**去中心化接口定义**模式，每个端定义自己的接口：

```
Server端接口定义：
  src/Server/Core/LYBT.Server.Core.Interfaces/
    ├── Services/         # 业务服务接口（IPatientService等）
    ├── Repositories/     # 仓储接口（IPatientRepository等）
    └── Common/           # 通用接口

Client端接口定义：
  src/Client/Shared/LYBT.Client.Shared.Interfaces/
    ├── Services/         # 客户端服务接口
    └── ViewModels/       # ViewModel接口

Shared.Interfaces已移除：
  src/Shared/LYBT.Shared.Interfaces/  ❌ 已删除（Issue #1728）
```

**设计优势**：
- ✅ **避免过早抽象**：Server和Client的接口需求不同，不强制共享
- ✅ **依赖方向清晰**：Server依赖Server.Core.Interfaces，Client依赖Client.Shared.Interfaces
- ✅ **职责明确**：每个端管理自己的接口定义
- ✅ **符合MVP原则**：只在真正需要跨端共享接口时才引入到Shared层

**演进触发条件**（参见ADR-005）：
- 出现真正需要跨端共享的接口（如通用验证接口IValidationService）
- 达到接口共享阈值（>5个跨端接口）
- 如需重新引入Shared.Interfaces项目，需创建新Issue并记录ADR

**历史决策记录**：Shared.Interfaces空项目曾是**有意的架构选择**（ADR-005），现已演进为完全移除。

### 3. Components - 跨端组件层

> **⚠️ 项目说明**：当前项目名称为**LYBT.Shared.Components**（不是Infrastructure），包含少量跨端共享组件。

**职责**：提供Desktop/Avalonia跨端共享的业务组件（当前专注于中药相关功能）

**实际目录结构**（src/Shared/LYBT.Shared.Components/）：

```
Components/
  ├── HerbCalculatorBase.cs       # 中药计算基类
  ├── HerbValidatorBase.cs        # 中药验证基类
  └── IHerbItem.cs                # 中药项接口
```

**组件说明**：

1. **HerbCalculatorBase** - 中药剂量计算抽象基类
   - 提供中药配方的剂量计算逻辑
   - 支持Desktop和Avalonia端共享

2. **HerbValidatorBase** - 中药验证抽象基类
   - 提供中药配伍禁忌验证
   - 支持Desktop和Avalonia端共享

3. **IHerbItem** - 中药项通用接口
   - 定义中药项的基本属性
   - 支持Desktop和Avalonia端共享

**设计原则**（MVP阶段）：
- ✅ **仅包含真正需要跨端共享的组件**（当前仅3个中药相关组件）
- ✅ **避免过度抽象**：不预先创建可能用不上的Infrastructure组件
- ✅ **按需演进**：当出现新的跨端共享需求时再添加新组件
- ❌ **不创建空目录**：没有Data/, Caching/, Logging/等未使用的目录

**实际代码示例**：

```csharp
// Components/IHerbItem.cs - 中药项接口
namespace LYBT.Shared.Components
{
    /// &lt;summary&gt;
    /// 中药项通用接口 - Desktop/Avalonia跨端共享
    /// &lt;/summary&gt;
    public interface IHerbItem
    {
        string Name { get; set; }           // 中药名称
        decimal Dosage { get; set; }        // 剂量（克）
        string Unit { get; set; }           // 单位
    }
}

// Components/HerbCalculatorBase.cs - 中药计算基类
public abstract class HerbCalculatorBase
{
    /// &lt;summary&gt;
    /// 计算处方总剂量
    /// &lt;/summary&gt;
    public abstract decimal CalculateTotalDosage(IEnumerable&lt;IHerbItem&gt; herbs);
    
    /// &lt;summary&gt;
    /// 计算单味药占比
    /// &lt;/summary&gt;
    public abstract decimal CalculateProportion(IHerbItem herb, IEnumerable&lt;IHerbItem&gt; herbs);
}
```

**关键差异说明**：
- ❌ **文档描述**：Infrastructure/（Data/, Caching/, Logging/, Security/, Validation/）
- ✅ **实际实现**：Components/（仅3个中药相关组件）
- **原因**：MVP阶段避免过早抽象，仅实现真正需要的跨端共享功能

**演进触发条件**（参见ADR-005）：
- 出现更多跨端共享需求（>5个组件）
- 需要通用的Data/Caching/Logging组件时
- 当前Components/可能演进为Infrastructure/的子目录之一

---

## 📐 DTO设计原则

> **⚠️ 核心架构原则**：本节记录Epic #1736 DTO优化的设计理念和最佳实践，确保整个项目的DTO设计保持一致性。

### DTO设计演进历史

**Epic #1736 (2025-10-31 - 2025-11-01)**: 五阶段DTO优化，从MVP超前设计回归到简单实用

```
Phase 1: 删除MVP超前设计DTO（22个）
  ↓
Phase 2: 移除DTO中的业务逻辑和计算属性
  ↓
Phase 3: 合并Create/Update DTOs为统一InputDto ⭐ 核心优化
  ↓
Phase 4: 清理DTO属性别名
  ↓
Phase 5: 修复PrescriptionDetailDto继承设计
```

---

### 核心设计理念

#### 1. InputDto统一模式（Phase 3核心优化）

**设计理念**：合并CreateDto和UpdateDto为统一的InputDto，简化API设计和维护成本。

**❌ 旧模式（过度设计）**：
```csharp
// 需要维护两个几乎相同的DTO
public class PatientCreateDto
{
    public string Name { get; set; }
    public Gender Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    // ... 20个属性
}

public class PatientUpdateDto  // 95%字段与CreateDto重复
{
    public Guid Id { get; set; }  // 唯一差异
    public string Name { get; set; }
    public Gender Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    // ... 20个属性
}
```

**✅ 新模式（InputDto统一）**：
```csharp
/// <summary>
/// 患者输入DTO - 统一创建和更新
/// Epic #1736 Phase 3: 合并Create/Update DTOs
/// </summary>
public class PatientInputDto
{
    /// <summary>患者ID（更新时必填，创建时为null）</summary>
    [DisplayName("患者ID")]
    public Guid? Id { get; set; }

    /// <summary>患者姓名</summary>
    [Required(ErrorMessage = "患者姓名不能为空")]
    [StringLength(50, ErrorMessage = "患者姓名长度不能超过50个字符")]
    [DisplayName("患者姓名")]
    public string Name { get; set; } = string.Empty;

    /// <summary>性别</summary>
    [Required(ErrorMessage = "性别不能为空")]
    [DisplayName("性别")]
    public Gender Gender { get; set; }

    /// <summary>出生日期</summary>
    [DisplayName("出生日期")]
    public DateTime? BirthDate { get; set; }

    // ... 其他业务属性
}
```

**使用场景**：
```csharp
// 场景1: 创建患者（Id为null）
var createInput = new PatientInputDto
{
    Id = null,  // 创建时为null
    Name = "张三",
    Gender = Gender.Male,
    BirthDate = new DateTime(1980, 5, 15)
};
await patientService.CreateAsync(createInput);

// 场景2: 更新患者（Id必填）
var updateInput = new PatientInputDto
{
    Id = Guid.Parse("..."),  // 更新时必填
    Name = "张三（已修改）",
    Gender = Gender.Male,
    BirthDate = new DateTime(1980, 5, 15)
};
await patientService.UpdateAsync(updateInput.Id.Value, updateInput);
```

**Service层处理逻辑**：
```csharp
public class PatientService : IPatientService
{
    // 创建方法：接收InputDto，Id必须为null
    public async Task<ServiceResult<PatientDto>> CreateAsync(PatientInputDto input)
    {
        if (input.Id.HasValue)
            return ServiceResult<PatientDto>.Error("创建时不应提供ID");

        var entity = _mapper.Map<Patient>(input);
        entity.Id = Guid.NewGuid();  // Service层生成ID

        var created = await _repository.AddAsync(entity);
        return ServiceResult<PatientDto>.Success(_mapper.Map<PatientDto>(created));
    }

    // 更新方法：接收Guid和InputDto，InputDto.Id可选
    public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientInputDto input)
    {
        var existing = await _repository.GetByIdAsync(id);
        if (existing == null)
            return ServiceResult<PatientDto>.Error("患者不存在");

        _mapper.Map(input, existing);  // 仅映射业务属性，Id不变

        var updated = await _repository.UpdateAsync(existing);
        return ServiceResult<PatientDto>.Success(_mapper.Map<PatientDto>(updated));
    }
}
```

**Validator统一模式**：
```csharp
/// <summary>
/// PatientInputDto验证器 - 同时用于创建和更新
/// Epic #1736 Phase 3: 删除独立的UpdateValidator
/// </summary>
public class PatientInputDtoValidator : AbstractValidator<PatientInputDto>
{
    public PatientInputDtoValidator()
    {
        // 通用验证规则（创建和更新共享）
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("患者姓名不能为空")
            .MaximumLength(50).WithMessage("患者姓名长度不能超过50个字符");

        RuleFor(x => x.Gender)
            .IsInEnum().WithMessage("性别值无效");

        RuleFor(x => x.BirthDate)
            .LessThan(DateTime.Today).WithMessage("出生日期不能晚于今天")
            .When(x => x.BirthDate.HasValue);

        // 创建时的特殊验证
        RuleFor(x => x.Id)
            .Null().WithMessage("创建时不应提供ID")
            .When(x => x.Id.HasValue, ApplyConditionTo.CurrentValidator);
    }
}
```

**优势总结**：
- ✅ **代码量减少50%**：无需维护两个几乎相同的DTO
- ✅ **验证逻辑统一**：无需维护两个独立的Validator
- ✅ **API简化**：创建和更新使用相同的DTO结构
- ✅ **维护成本降低**：字段修改只需更新一处

---

#### 2. Dto vs DetailDto vs InputDto

**三种DTO的职责划分**：

| DTO类型 | 用途 | 场景 | 示例 |
|---------|------|------|------|
| **Dto** | 基础数据传输 | 列表查询、关联查询 | `PatientDto`, `ConsultationDto` |
| **DetailDto** | 详情查询（扩展版） | 单个详情查询、包含计算字段/关联数据 | `PrescriptionDetailDto`, `MedicalCaseDetailDto` |
| **InputDto** | 输入数据（创建/更新） | API请求体、表单提交 | `PatientInputDto`, `HerbInputDto` |

**使用指南**：

**2.1 基础Dto - 列表和关联查询**：
```csharp
/// <summary>
/// 患者信息DTO - 基础版
/// 用于列表查询、下拉选择、关联查询等场景
/// </summary>
public class PatientDto : StatusDto
{
    public string Name { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public DateTime? BirthDate { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;

    // ⚠️ 不包含计算属性（如Age）
    // ⚠️ 不包含关联数据（如MedicalCases列表）
}

// 使用场景：
// 1. GET /api/patients?pageIndex=1&pageSize=20  → List<PatientDto>
// 2. GET /api/medicalcases/{id}  → MedicalCaseDto { PatientDto Patient }
```

**2.2 DetailDto - 详情查询（扩展版）**：
```csharp
/// <summary>
/// 处方详情DTO - 扩展版本
/// 继承PrescriptionDto基础字段，添加运行时计算的警告信息
/// Epic #1736 Phase 5: 简化继承设计，去除new关键字
/// </summary>
public class PrescriptionDetailDto : PrescriptionDto
{
    /// <summary>重复用药警告（运行时计算）</summary>
    [DisplayName("重复用药警告")]
    public string? DuplicateWarning { get; set; }

    /// <summary>缺药警告（运行时计算）</summary>
    [DisplayName("缺药警告")]
    public string? MissingDrugWarning { get; set; }

    /// <summary>格式化的处方编号（用于UI展示）</summary>
    [DisplayName("处方编号")]
    public string? PrescriptionNo { get; set; }
}

// 使用场景：
// GET /api/prescriptions/{id}  → PrescriptionDetailDto（包含警告信息）
```

**2.3 InputDto - 创建和更新**：
```csharp
/// <summary>
/// 中药材输入DTO - 统一创建和更新
/// Epic #1736 Phase 3: 合并HerbCreateDto和HerbUpdateDto
/// </summary>
public class HerbInputDto
{
    /// <summary>药材ID（更新时必填，创建时为null）</summary>
    [DisplayName("药材ID")]
    public Guid? Id { get; set; }

    /// <summary>药材名称</summary>
    [Required(ErrorMessage = "药材名称不能为空")]
    [StringLength(100)]
    [DisplayName("药材名称")]
    public string Name { get; set; } = string.Empty;

    /// <summary>单价</summary>
    [Required]
    [Range(0, 999999.99)]
    [DisplayName("单价")]
    public decimal Price { get; set; }

    // ... 其他业务属性
}

// 使用场景：
// POST /api/herbs  → Body: HerbInputDto (Id=null)
// PUT /api/herbs/{id}  → Body: HerbInputDto (Id可选)
```

**何时使用DetailDto而非Dto**：
- ✅ 需要运行时计算字段（如警告信息、统计数据）
- ✅ 需要预加载关联数据（如订单详情包含订单项列表）
- ✅ 需要格式化字段（如格式化的编号、显示用文本）
- ❌ 如果只是多几个简单字段，直接扩展基础Dto即可

---

#### 3. DTO属性设计规范（Phase 2 & Phase 4）

**Phase 2: 移除DTO中的业务逻辑和计算属性**

**❌ 错误示例（DTO包含业务逻辑）**：
```csharp
public class PrescriptionDto
{
    public List<PrescriptionItemDto> Items { get; set; }

    // ❌ 错误：DTO不应包含业务计算逻辑
    public decimal TotalPrice
    {
        get
        {
            return Items.Sum(x => x.Quantity * x.UnitPrice) * (1 - Discount);
        }
    }
}
```

**✅ 正确示例（Service层计算）**：
```csharp
// DTO仅存储数据
public class PrescriptionDto : StatusDto
{
    public List<PrescriptionItemDto> Items { get; set; }
    public decimal Discount { get; set; }

    /// <summary>总价格（由Service层计算并设置）</summary>
    [DisplayName("总价格")]
    public decimal TotalPrice { get; set; }
}

// Service层负责计算
public class PrescriptionService
{
    public async Task<PrescriptionDto> GetByIdAsync(Guid id)
    {
        var entity = await _repository.GetByIdAsync(id);
        var dto = _mapper.Map<PrescriptionDto>(entity);

        // Service层计算总价格
        dto.TotalPrice = dto.Items.Sum(x => x.Quantity * x.UnitPrice) * (1 - dto.Discount);

        return dto;
    }
}
```

**Phase 4: 清理DTO属性别名**

**❌ 错误示例（属性别名混淆）**：
```csharp
public class PrescriptionItemDto
{
    public string? Remark { get; set; }

    // ❌ 错误：属性别名造成混淆
    public string? Notes { get => Remark; set => Remark = value; }
}
```

**✅ 正确示例（统一属性名）**：
```csharp
public class PrescriptionItemDto
{
    /// <summary>备注</summary>
    [DisplayName("备注")]
    [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
    public string? Remark { get; set; }

    // ✅ 如需兼容旧代码，明确标注为兼容性别名
    /// <summary>备注(兼容旧代码)</summary>
    [DisplayName("备注")]
    [Obsolete("请使用Remark属性")]
    public string? Notes { get => Remark; set => Remark = value; }
}
```

---

### 实际案例：MedicalCase模块DTO优化

**Issue #1738 (2025-11-01)**: 清理MedicalCase模块重复DTO，统一使用Shared层

**优化前（5个重复DTO）**：
```
Module.MedicalCase/Dtos/
├── ConsultationDetailDto.cs          ❌ 与Shared层ConsultationDto重复
├── UpdateConsultationRequest.cs      ❌ 应使用ConsultationInputDto
├── CreatePrescriptionRequest.cs      ❌ 应使用PrescriptionCreateDto
├── UpdatePrescriptionRequest.cs      ❌ 应使用PrescriptionEditDto
└── PrescriptionItemDto.cs            ❌ 与Shared层完全相同
```

**优化后（统一使用Shared层）**：
```csharp
// MedicalCaseController.cs - 更新后的API签名
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;

[HttpPut("{id}/consultation")]
public async Task<ActionResult<ApiResponse<MedicalCaseEntity>>> UpdateConsultation(
    Guid id,
    [FromBody] ConsultationInputDto request)  // ✅ 使用Shared层InputDto

[HttpPost("{id}/prescriptions")]
public async Task<ActionResult<ApiResponse<PrescriptionEntity>>> CreatePrescription(
    Guid id,
    [FromBody] PrescriptionCreateDto request)  // ✅ 使用Shared层CreateDto

[HttpPut("{id}/prescriptions/{prescriptionId}")]
public async Task<ActionResult<ApiResponse<PrescriptionEntity>>> UpdatePrescription(
    Guid id,
    Guid prescriptionId,
    [FromBody] PrescriptionEditDto request)  // ✅ 使用Shared层EditDto

[HttpGet("{medicalCaseId}/consultations")]
[ProducesResponseType(typeof(ApiResponse<List<ConsultationDto>>), 200)]
public async Task<ActionResult<ApiResponse<List<ConsultationDto>>>> GetConsultationList(
    Guid medicalCaseId)  // ✅ 使用Shared层Dto
```

**优化效益**：
- ✅ 删除5个重复DTO文件
- ✅ 确保跨模块DTO一致性
- ✅ 简化维护成本
- ✅ 符合三层对齐架构原则

---

### DTO设计检查清单

在设计新DTO或重构现有DTO时，请确认以下要点：

#### ✅ 结构设计
- [ ] DTO按业务模块组织在`Contracts/{Module}/`目录
- [ ] 相关DTOs放在同一个文件中（如PatientDto和PatientDetailDto）
- [ ] 使用清晰的命名空间：`LYBT.Shared.Models.Contracts.{Module}`

#### ✅ InputDto模式
- [ ] 优先使用InputDto统一创建和更新，避免Create/Update分离
- [ ] InputDto包含可选的`Id`属性（创建时null，更新时必填）
- [ ] 创建和更新共享相同的Validator

#### ✅ 职责划分
- [ ] 基础Dto用于列表和关联查询（不含计算属性）
- [ ] DetailDto用于详情查询（可含运行时计算字段）
- [ ] InputDto用于创建和更新（包含验证规则）

#### ✅ 属性规范
- [ ] DTO仅存储数据，不包含业务逻辑
- [ ] 计算属性由Service层填充（如TotalPrice、Age）
- [ ] 避免属性别名，使用统一的属性名
- [ ] 使用`[DisplayName]`和`[Description]`标注属性

#### ✅ 继承规范
- [ ] DetailDto继承基础Dto，添加扩展字段
- [ ] 避免使用`new`关键字隐藏基类属性（Epic #1736 Phase 5）
- [ ] 明确标注继承关系和扩展理由

#### ✅ 验证规范
- [ ] 使用FluentValidation定义验证规则
- [ ] InputDto的Validator同时用于创建和更新
- [ ] 验证规则与DataAnnotations保持同步（如StringLength）

---

### 相关Issue和ADR

**Epic #1736**: DTO优化Phase 1-5
- Phase 1: 删除22个MVP超前设计DTO
- Phase 2: 移除DTO中的业务逻辑和计算属性
- Phase 3: 合并Create/Update DTOs为统一InputDto ⭐
- Phase 4: 清理DTO属性别名
- Phase 5: 修复PrescriptionDetailDto继承设计

**Issue #1738**: 清理MedicalCase模块重复DTO

**相关ADR**:
- ADR-005: 长期演进触发条件（DTO设计复杂度指标）
- ADR-001: 使用FluentValidation进行数据验证

---

### 4. Utilities - 工具类层

> **⚠️ 项目说明**：当前Utilities包含**少量跨端共享的工具类**，主要是启动初始化和缓存扩展。

**实际目录结构**（src/Shared/LYBT.Shared.Utilities/）：

```
Utilities/
  ├── Configuration/            # 配置相关（空目录，保留结构）
  ├── Extensions/               # 扩展方法
  │   ├── Application/
  │   │   └── ApplicationInitializationExtensions.cs  # 应用启动初始化扩展
  │   └── ServiceCollection/
  │       └── CacheExtensions.cs                      # 缓存服务注册扩展
  ├── Helpers/                  # 辅助类（空目录，保留结构）
  └── Security/                 # 安全相关（空目录，保留结构）
```

**现有工具类**（仅2个）：

**4.1 ApplicationInitializationExtensions.cs** - 应用启动初始化扩展：
```csharp
// 用途：提供应用启动时的初始化扩展方法
// 位置：Extensions/Application/ApplicationInitializationExtensions.cs
public static class ApplicationInitializationExtensions
{
    // 初始化应用（具体实现见代码）
}
```

**4.2 CacheExtensions.cs** - 缓存服务注册扩展：
```csharp
// 用途：提供IServiceCollection的缓存服务注册扩展
// 位置：Extensions/ServiceCollection/CacheExtensions.cs
public static class CacheExtensions
{
    public static IServiceCollection AddMemoryCacheServices(this IServiceCollection services)
    {
        services.AddMemoryCache();
        return services;
    }
}
```

**设计原则**（MVP阶段）：

- ✅ **仅包含真正需要跨端共享的工具类**（当前仅2个扩展类）
- ✅ **避免过度设计**：不预先创建StringExtensions、DateTimeExtensions等可能用不上的工具类
- ✅ **按需添加**：未来如有真正需要跨端共享的工具方法，再添加对应类

**注意事项**：

1. **空目录保留原因**：Configuration/、Helpers/、Security/目录当前为空，但保留目录结构以便未来扩展
2. **工具类最小化**：当前仅ApplicationInitializationExtensions和CacheExtensions，符合"够用即好"原则
3. **端特定工具类**：
   - Server端特定工具类 → 放在Server端项目
   - Client端特定工具类 → 放在Client端项目
   - 仅真正跨端共享的 → 放在Shared.Utilities

**演进触发条件**（参见ADR-005）：
- 当出现3个以上端都需要使用的相同工具方法时 → 提取到Shared.Utilities
- 当前MVP阶段不主动创建"可能未来会用到"的工具类

### 5. Constants - 常量定义层

> **⚠️ 项目说明**：当前Constants包含**少量验证和错误消息相关的常量**，不包含文档中描述的SystemConstants和BusinessConstants。

**职责**：定义验证规则常量、错误消息键

**实际目录结构**（src/Shared/LYBT.Shared.Models/Constants/）：

```
Constants/
  ├── ErrorMessageKeys.cs      # 错误消息键定义
  └── ValidationConstants.cs   # 验证常量定义
```

**实际代码示例**：

**5.1 ValidationConstants.cs** - 验证规则常量：
```csharp
// 用途：定义统一的验证规则常量
// 位置：Constants/ValidationConstants.cs
namespace LYBT.Shared.Models.Constants
{
    public static class ValidationConstants
    {
        // 患者验证
        public const int PATIENT_NAME_MAX_LENGTH = 50;
        public const int PATIENT_PHONE_LENGTH = 11;
        public const int PATIENT_IDCARD_LENGTH = 18;
        
        // 处方验证
        public const int PRESCRIPTION_NAME_MAX_LENGTH = 100;
        public const decimal MIN_HERB_DOSAGE = 0.1m;
        public const decimal MAX_HERB_DOSAGE = 1000m;
        
        // 分页验证
        public const int MIN_PAGE_SIZE = 1;
        public const int MAX_PAGE_SIZE = 100;
        public const int DEFAULT_PAGE_SIZE = 20;
    }
}
```

**5.2 ErrorMessageKeys.cs** - 错误消息键：
```csharp
// 用途：定义统一的错误消息键（用于国际化）
// 位置：Constants/ErrorMessageKeys.cs
namespace LYBT.Shared.Models.Constants
{
    public static class ErrorMessageKeys
    {
        // 通用错误
        public const string VALIDATION_FAILED = "validation.failed";
        public const string NOT_FOUND = "not.found";
        public const string UNAUTHORIZED = "unauthorized";
        
        // 患者相关
        public const string PATIENT_NAME_REQUIRED = "patient.name.required";
        public const string PATIENT_PHONE_INVALID = "patient.phone.invalid";
        
        // 处方相关
        public const string PRESCRIPTION_EMPTY = "prescription.empty";
        public const string HERB_DOSAGE_INVALID = "herb.dosage.invalid";
    }
}
```

**设计原则**（MVP阶段）：
- ✅ **仅包含真正需要的常量**：验证规则和错误消息键
- ✅ **避免过度设计**：不预先创建SystemConstants、BusinessConstants等大而全的常量类
- ✅ **按需添加**：未来如需其他常量类型，再添加对应文件

**注意事项**：
1. **业务枚举值**：使用Enums/目录定义（如Gender、MedicalCaseStatus等），不使用字符串常量
2. **配置值**：使用appsettings.json或环境变量，不硬编码在Constants中
3. **最小化原则**：当前仅2个常量文件，符合MVP"够用即好"原则

### 6. Enums - 枚举类型层
**职责**：定义业务枚举类型、系统枚举类型

**代码示例**：
```csharp
// Enums/Gender.cs
public enum Gender
{
    [Description("男")]
    Male = 1,
    
    [Description("女")]
    Female = 2
}

// Enums/MedicalCaseStatus.cs
public enum MedicalCaseStatus
{
    [Description("新建")]
    New = 1,
    
    [Description("进行中")]
    InProgress = 2,
    
    [Description("已完成")]
    Completed = 3,
    
    [Description("已取消")]
    Cancelled = 4
}

// Enums/PrescriptionStatus.cs
public enum PrescriptionStatus
{
    [Description("草稿")]
    Draft = 1,
    
    [Description("已确认")]
    Confirmed = 2,
    
    [Description("已发药")]
    Dispensed = 3,
    
    [Description("已完成")]
    Completed = 4,
    
    [Description("已取消")]
    Cancelled = 5
}

// Enums/ConsultationType.cs
public enum ConsultationType
{
    [Description("初诊")]
    FirstVisit = 1,
    
    [Description("复诊")]
    FollowUp = 2,
    
    [Description("急诊")]
    Emergency = 3
}

// Enums/PaymentMethod.cs
public enum PaymentMethod
{
    [Description("现金")]
    Cash = 1,
    
    [Description("银行卡")]
    CreditCard = 2,
    
    [Description("支付宝")]
    Alipay = 3,
    
    [Description("微信支付")]
    WechatPay = 4,
    
    [Description("医保")]
    Insurance = 5
}

// Enums/Permission.cs
public enum Permission
{
    [Description("患者管理")]
    PatientManage = 1,
    
    [Description("医案管理")]
    MedicalCaseManage = 2,
    
    [Description("诊疗管理")]
    ConsultationManage = 3,
    
    [Description("处方管理")]
    PrescriptionManage = 4,
    
    [Description("药材管理")]
    HerbManage = 5,
    
    [Description("验方管理")]
    FormulaManage = 6,
    
    [Description("用户管理")]
    UserManage = 7,
    
    [Description("系统管理")]
    SystemManage = 8
}
```

## 🔧 跨层数据传输

### 1. 统一响应格式
```csharp
// Models/Responses/ApiResult.cs
public class ApiResult<T>
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public T Data { get; set; }
    public int Code { get; set; }
    public DateTime Timestamp { get; set; }
    public List<string> Errors { get; set; }
    
    public static ApiResult<T> Success(T data, string message = "操作成功")
    {
        return new ApiResult<T>
        {
            Success = true,
            Message = message,
            Data = data,
            Code = 200,
            Timestamp = DateTime.UtcNow
        };
    }
    
    public static ApiResult<T> Error(string message, int code = 400, List<string> errors = null)
    {
        return new ApiResult<T>
        {
            Success = false,
            Message = message,
            Code = code,
            Timestamp = DateTime.UtcNow,
            Errors = errors ?? new List<string>()
        };
    }
    
    public static ApiResult<T> ValidationError(List<ValidationError> validationErrors)
    {
        var errors = validationErrors.Select(e => e.ErrorMessage).ToList();
        return new ApiResult<T>
        {
            Success = false,
            Message = "数据验证失败",
            Code = 422,
            Timestamp = DateTime.UtcNow,
            Errors = errors
        };
    }
}
```

### 2. 分页响应格式
```csharp
// Models/Responses/PagedResult.cs
public class PagedResult<T>
{
    public IEnumerable<T> Data { get; set; }
    public int PageIndex { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage { get; set; }
    public bool HasNextPage { get; set; }
    
    public static PagedResult<T> Create(IEnumerable<T> data, int pageIndex, int pageSize, int totalCount)
    {
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);
        
        return new PagedResult<T>
        {
            Data = data,
            PageIndex = pageIndex,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = pageIndex > 1,
            HasNextPage = pageIndex < totalPages
        };
    }
}
```

### 3. 统一异常处理
```csharp
// Infrastructure/Exceptions/BusinessException.cs
public class BusinessException : Exception
{
    public int ErrorCode { get; }
    public string ErrorDetails { get; }
    
    public BusinessException(string message, int errorCode = 400, string errorDetails = null)
        : base(message)
    {
        ErrorCode = errorCode;
        ErrorDetails = errorDetails;
    }
    
    public BusinessException(string message, Exception innerException, int errorCode = 400, string errorDetails = null)
        : base(message, innerException)
    {
        ErrorCode = errorCode;
        ErrorDetails = errorDetails;
    }
}

// Infrastructure/Exceptions/ValidationException.cs
public class ValidationException : BusinessException
{
    public List<ValidationError> ValidationErrors { get; }
    
    public ValidationException(List<ValidationError> validationErrors)
        : base("数据验证失败", 422)
    {
        ValidationErrors = validationErrors;
    }
}

// Infrastructure/Exceptions/NotFoundException.cs
public class NotFoundException : BusinessException
{
    public NotFoundException(string entityName, object id)
        : base($"{entityName} with id {id} not found", 404)
    {
    }
    
    public NotFoundException(string message)
        : base(message, 404)
    {
    }
}
```

## 🔗 技术决策记录 (ADR)

### ADR-001: 使用FluentValidation进行数据验证
**状态**: 已接受  
**日期**: 2025-10-15  

**决策**：使用FluentValidation库进行数据验证，而不是DataAnnotations。

**理由**：
- 更灵活的验证规则定义
- 更好的性能
- 支持复杂的验证逻辑
- 更清晰的错误消息

**后果**：
- 需要额外的依赖
- 验证规则需要单独维护
- 学习成本较高

### ADR-002: 使用AutoMapper进行对象映射
**状态**: 已接受  
**日期**: 2025-10-15  

**决策**：使用AutoMapper库进行对象映射，而不是手动映射。

**理由**：
- 减少样板代码
- 提高开发效率
- 减少映射错误
- 支持复杂映射逻辑

**后果**：
- 运行时性能开销
- 需要配置映射规则
- 调试复杂度增加

### ADR-003: 使用MediatR实现命令查询分离
**状态**: 已拒绝  
**日期**: 2025-10-15  

**决策**：不使用MediatR，保持传统的服务层架构。

**理由**：
- 项目规模相对较小
- 避免过度设计
- 减少学习成本
- 保持代码简洁

**后果**：
- 代码耦合度可能较高
- 扩展性受限
- 测试复杂度较高

## 📋 最佳实践

### 1. 命名约定
- **接口**: 以I开头，如IPatientService
- **实现类**: 以具体名称开头，如PatientService
- **实体类**: 使用业务名词，如Patient、MedicalCase
- **DTO类**: 以Dto结尾，如PatientDto
- **请求类**: 以Request结尾，如PatientCreateRequest
- **响应类**: 以Response结尾，如PatientResponse

### 2. 代码组织
- **单一职责**: 每个类只负责一个功能
- **开闭原则**: 对扩展开放，对修改封闭
- **依赖倒置**: 依赖抽象，不依赖具体实现
- **接口隔离**: 使用小而专一的接口

### 3. 性能优化
- **延迟加载**: 使用延迟加载减少内存占用
- **缓存策略**: 合理使用缓存提高性能
- **异步编程**: I/O操作使用async/await
- **批量操作**: 减少数据库访问次数

### 4. 安全考虑
- **输入验证**: 所有输入都必须验证
- **SQL注入防护**: 使用参数化查询
- **敏感数据**: 敏感数据加密存储
- **权限控制**: 实现细粒度权限控制

## 🔗 相关文档

- **[架构总览](../README.md)** - 三层对齐架构设计原理
- **[Server端架构](../server/README.md)** - 服务端三层架构实现
- **[Client端架构](../client/README.md)** - WPF五层架构实现
- **[共享开发指南](../../development/shared/README.md)** - 共享层开发规范
- **[模块设计指南](../module-design-guide.md)** - 业务模块化设计标准

---

**文档维护**：架构组 | **最后更新**：2025-10-15  
**适用版本**：v5.0 对齐架构版 | **审核状态**：已审核