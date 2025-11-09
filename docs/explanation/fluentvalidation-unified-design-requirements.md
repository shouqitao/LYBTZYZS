# FluentValidation统一设计需求分析

> **文档类型**: Requirements Discussion
> **状态**: 📝 需求讨论
> **目标读者**: 开发团队、架构师
> **创建日期**: 2025-11-09
> **优先级**: P1（高优先级）

---

## 1. 背景与动机

### 1.1 当前状态

项目中**8个模块使用FluentValidation**，共计**12个验证器**：

| 模块 | 验证器数量 | 验证模式 | 文件名 |
|-----|----------|---------|--------|
| **Auth** | 3 | 统一验证 | LoginRequestValidator, ChangePasswordRequestValidator, SuperAdminLoginRequestValidator |
| **Users** | 1 | ✅ **条件验证** | UserInputDtoValidator |
| **Patients** | 1 | 统一验证 | PatientInputDtoValidator |
| **Consultation** | 1 | 统一验证 | ConsultationInputDtoValidator |
| **Formula** | 2 | 统一验证 + 嵌套 | FormulaInputDtoValidator, FormulaHerbItemInputDtoValidator |
| **Herbs** | 1 | 统一验证 | HerbInputDtoValidator |
| **MedicalCase** | 2 | ❌ **分离验证器** | MedicalCaseCreateDtoValidator, MedicalCaseUpdateDtoValidator |
| **Prescriptions** | 3 | ❌ **分离验证器** + 嵌套 | PrescriptionCreateDtoValidator, PrescriptionEditDtoValidator, PrescriptionItemInputDtoValidator |

### 1.2 问题识别

#### 问题1：验证模式不统一

**3种验证模式并存**：

1. **条件验证模式**（推荐）：1个模块
   - Users模块：使用`UserInputDtoValidator` + `.When(x => x.Id == null)`
   - 单一InputDto，通过条件验证区分创建/更新

2. **统一验证模式**：4个模块
   - Patients, Consultation, Formula, Herbs
   - 单一验证器，所有场景使用相同规则

3. **分离验证器模式**（不推荐）：2个模块
   - MedicalCase：`MedicalCaseCreateDtoValidator` + `MedicalCaseUpdateDtoValidator`
   - Prescriptions：`PrescriptionCreateDtoValidator` + `PrescriptionEditDtoValidator`
   - 创建和更新使用不同的DTO和验证器

**影响**：
- ❌ 代码重复：分离验证器模式导致验证规则重复
- ❌ 维护成本高：需要同时维护两个验证器
- ❌ 不一致的API设计：部分模块使用CreateDto/UpdateDto，部分使用InputDto

#### 问题2：分离验证器的具体问题

**MedicalCase模块**：
```csharp
// 当前实现（不推荐）
MedicalCaseCreateDto + MedicalCaseCreateDtoValidator
MedicalCaseUpdateDto + MedicalCaseUpdateDtoValidator

// 问题：
// 1. 两个DTO字段高度重复（90%相同）
// 2. 两个验证器规则高度重复
// 3. Service层需要处理两个不同的DTO类型
```

**Prescriptions模块**：
```csharp
// 当前实现（不推荐）
PrescriptionCreateDto + PrescriptionCreateDtoValidator
PrescriptionEditDto + PrescriptionEditDtoValidator

// 问题同上
```

#### 问题3：条件验证模式未充分利用

**当前状态**：
- 仅Users模块使用条件验证（`.When()` 谓词）
- 其他需要区分创建/更新的模块未采用此模式

**潜在收益**：
- ✅ 减少DTO数量（8个 → 4个，减少50%）
- ✅ 减少验证器数量（5个 → 2个，减少60%）
- ✅ 统一API设计模式
- ✅ 降低维护成本

#### 问题4：缺乏统一的验证规范

**命名不一致**：
- ✅ 推荐：`PatientInputDtoValidator`, `UserInputDtoValidator`
- ❌ 不推荐：`MedicalCaseCreateDtoValidator`, `PrescriptionEditDtoValidator`

**组织结构不清晰**：
- 缺乏嵌套验证器的命名规范（如：`FormulaHerbItemInputDtoValidator`）
- 缺乏验证常量的集中管理

**测试覆盖不足**：
- 大部分验证器缺乏单元测试
- 缺乏条件验证场景的测试用例

### 1.3 业务证据

**Epic #1934用户启用/禁用Bug**：
- **问题**：更新用户状态时，验证器要求UserName必填，导致400错误
- **原因**：未使用条件验证区分创建/更新场景
- **解决方案**：引入条件验证（`.When(x => x.Id == null)`）
- **效果**：✅ Bug修复，验证逻辑更合理

**结论**：条件验证模式已证明其价值，应推广到其他模块。

---

## 2. 目标设计

### 2.1 核心原则

1. **统一使用条件验证模式**
   - 所有CRUD操作使用单一InputDto
   - 通过`.When()` 谓词区分创建/更新场景
   - 减少DTO和验证器数量

2. **统一命名规范**
   - 验证器：`{ModuleName}InputDtoValidator`
   - 嵌套验证器：`{ParentName}{ItemName}InputDtoValidator`
   - 示例：`PatientInputDtoValidator`, `FormulaHerbItemInputDtoValidator`

3. **统一组织结构**
   - 路径：`LYBT.Shared.Validators/{ModuleName}/`
   - 验证常量：`LYBT.Shared.Models.Constants.ValidationConstants`

4. **完整测试覆盖**
   - 每个验证器必须有单元测试
   - 条件验证场景必须有测试用例（创建/更新）

### 2.2 标准验证器模板

#### 基础模板（无条件验证）

```csharp
namespace LYBT.Shared.Validators.{ModuleName}
{
    /// <summary>
    /// {模块名}输入DTO验证器
    /// </summary>
    public class {ModuleName}InputDtoValidator : AbstractValidator<{ModuleName}InputDto>
    {
        public {ModuleName}InputDtoValidator()
        {
            // 必填字段
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("名称不能为空")
                .MaximumLength(100).WithMessage("名称长度不能超过100个字符");

            // 可选字段（有值时验证格式）
            RuleFor(x => x.Email)
                .EmailAddress().WithMessage("邮箱格式不正确")
                .When(x => !string.IsNullOrEmpty(x.Email));
        }
    }
}
```

#### 条件验证模板（创建/更新区分）

```csharp
namespace LYBT.Shared.Validators.{ModuleName}
{
    /// <summary>
    /// {模块名}输入DTO验证器
    /// 支持创建和更新场景，通过Id字段区分
    /// </summary>
    public class {ModuleName}InputDtoValidator : AbstractValidator<{ModuleName}InputDto>
    {
        public {ModuleName}InputDtoValidator()
        {
            // 创建时必填，更新时可选
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("名称不能为空")
                .When(x => x.Id == null || x.Id == Guid.Empty);

            // 所有场景都需验证（如果有值）
            RuleFor(x => x.Name)
                .MaximumLength(100).WithMessage("名称长度不能超过100个字符")
                .When(x => !string.IsNullOrEmpty(x.Name));

            // 更新时必填（用于查找）
            RuleFor(x => x.Id)
                .NotEmpty().WithMessage("更新时Id不能为空")
                .When(x => x.Id.HasValue && x.Id.Value != Guid.Empty);
        }
    }
}
```

#### 嵌套集合验证模板

```csharp
namespace LYBT.Shared.Validators.{ModuleName}
{
    public class {ModuleName}InputDtoValidator : AbstractValidator<{ModuleName}InputDto>
    {
        public {ModuleName}InputDtoValidator()
        {
            // 集合验证
            RuleFor(x => x.Items)
                .NotEmpty().WithMessage("明细不能为空")
                .Must(items => items != null && items.Any())
                .WithMessage("必须包含至少一项");

            // 集合中每个元素的验证
            RuleForEach(x => x.Items)
                .SetValidator(new {ModuleName}ItemInputDtoValidator())
                .When(x => x.Items != null);
        }
    }

    /// <summary>
    /// {模块名}明细项验证器
    /// </summary>
    public class {ModuleName}ItemInputDtoValidator : AbstractValidator<{ModuleName}ItemInputDto>
    {
        public {ModuleName}ItemInputDtoValidator()
        {
            RuleFor(x => x.Quantity)
                .GreaterThan(0).WithMessage("数量必须大于0");
        }
    }
}
```

### 2.3 目标架构

```
LYBT.Shared.Validators/
├── Auth/
│   ├── LoginRequestValidator.cs
│   ├── ChangePasswordRequestValidator.cs
│   └── SuperAdminLoginRequestValidator.cs
├── Users/
│   └── UserInputDtoValidator.cs ✅ 条件验证
├── Patients/
│   └── PatientInputDtoValidator.cs
├── Consultation/
│   └── ConsultationInputDtoValidator.cs
├── Formula/
│   ├── FormulaInputDtoValidator.cs
│   └── FormulaHerbItemInputDtoValidator.cs（嵌套）
├── Herbs/
│   └── HerbInputDtoValidator.cs
├── MedicalCase/
│   └── MedicalCaseInputDtoValidator.cs ⚠️ 需重构
├── Prescriptions/
│   ├── PrescriptionInputDtoValidator.cs ⚠️ 需重构
│   └── PrescriptionItemInputDtoValidator.cs（嵌套）
└── Common/
    └── ValidationConstants.cs（验证常量）
```

**目标**：
- ❌ 删除：`MedicalCaseCreateDtoValidator`, `MedicalCaseUpdateDtoValidator`
- ❌ 删除：`PrescriptionCreateDtoValidator`, `PrescriptionEditDtoValidator`
- ✅ 新增：`MedicalCaseInputDtoValidator`（条件验证）
- ✅ 新增：`PrescriptionInputDtoValidator`（条件验证）
- 验证器总数：12个 → 10个（减少16.7%）

---

## 3. 技术方案

### 3.1 MedicalCase模块迁移方案

#### 当前实现

**DTO定义**：
```csharp
// MedicalCaseCreateDto.cs
public class MedicalCaseCreateDto
{
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public string? ChiefComplaint { get; set; }
    public string? PresentIllnessHistory { get; set; }
}

// MedicalCaseUpdateDto.cs
public class MedicalCaseUpdateDto
{
    public Guid Id { get; set; } // 唯一区别
    public Guid PatientId { get; set; }
    public Guid DoctorId { get; set; }
    public string? ChiefComplaint { get; set; }
    public string? PresentIllnessHistory { get; set; }
}
```

**验证器**：
```csharp
// MedicalCaseCreateDtoValidator.cs
public class MedicalCaseCreateDtoValidator : AbstractValidator<MedicalCaseCreateDto>
{
    public MedicalCaseCreateDtoValidator()
    {
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorId).NotEmpty();
        RuleFor(x => x.ChiefComplaint).MaximumLength(1000).When(...);
        RuleFor(x => x.PresentIllnessHistory).MaximumLength(2000).When(...);
    }
}

// MedicalCaseUpdateDtoValidator.cs
public class MedicalCaseUpdateDtoValidator : AbstractValidator<MedicalCaseUpdateDto>
{
    public MedicalCaseUpdateDtoValidator()
    {
        // 几乎完全相同的验证规则（代码重复）
        RuleFor(x => x.PatientId).NotEmpty();
        RuleFor(x => x.DoctorId).NotEmpty();
        // ...
    }
}
```

#### 目标实现

**统一DTO**：
```csharp
// MedicalCaseInputDto.cs
public class MedicalCaseInputDto
{
    /// <summary>
    /// 病案ID（更新时必填，创建时为null）
    /// </summary>
    public Guid? Id { get; set; }

    /// <summary>
    /// 患者ID
    /// </summary>
    public Guid PatientId { get; set; }

    /// <summary>
    /// 医生ID
    /// </summary>
    public Guid DoctorId { get; set; }

    /// <summary>
    /// 主诉
    /// </summary>
    public string? ChiefComplaint { get; set; }

    /// <summary>
    /// 现病史
    /// </summary>
    public string? PresentIllnessHistory { get; set; }
}
```

**条件验证器**：
```csharp
// MedicalCaseInputDtoValidator.cs
public class MedicalCaseInputDtoValidator : AbstractValidator<MedicalCaseInputDto>
{
    public MedicalCaseInputDtoValidator()
    {
        // PatientId：始终必填
        RuleFor(x => x.PatientId)
            .NotEmpty().WithMessage("患者ID不能为空");

        // DoctorId：始终必填
        RuleFor(x => x.DoctorId)
            .NotEmpty().WithMessage("医生ID不能为空");

        // ChiefComplaint：可选字段，有值时验证长度
        RuleFor(x => x.ChiefComplaint)
            .MaximumLength(1000).WithMessage("主诉长度不能超过1000个字符")
            .When(x => !string.IsNullOrEmpty(x.ChiefComplaint));

        // PresentIllnessHistory：可选字段，有值时验证长度
        RuleFor(x => x.PresentIllnessHistory)
            .MaximumLength(2000).WithMessage("现病史长度不能超过2000个字符")
            .When(x => !string.IsNullOrEmpty(x.PresentIllnessHistory));

        // 创建/更新场景区分（如果需要）
        // 目前MedicalCase的创建和更新验证规则完全相同，暂不需要条件验证
        // 如果未来需要，可添加：
        // RuleFor(x => x.SomeField)
        //     .NotEmpty()
        //     .When(x => x.Id == null || x.Id == Guid.Empty);
    }
}
```

#### Service层调整

**当前**：
```csharp
public async Task<MedicalCaseDto> CreateAsync(MedicalCaseCreateDto dto)
{
    var validationResult = await _createValidator.ValidateAsync(dto);
    // ...
}

public async Task<MedicalCaseDto> UpdateAsync(Guid id, MedicalCaseUpdateDto dto)
{
    var validationResult = await _updateValidator.ValidateAsync(dto);
    // ...
}
```

**目标**：
```csharp
public async Task<MedicalCaseDto> CreateAsync(MedicalCaseInputDto input)
{
    // 验证Id必须为null
    if (input.Id.HasValue && input.Id.Value != Guid.Empty)
    {
        throw new InvalidOperationException("创建时Id必须为null");
    }

    var validationResult = await _validator.ValidateAsync(input);
    // ...
}

public async Task<MedicalCaseDto> UpdateAsync(MedicalCaseInputDto input)
{
    // 验证Id必须有值
    if (!input.Id.HasValue || input.Id.Value == Guid.Empty)
    {
        throw new InvalidOperationException("更新时Id不能为null");
    }

    var validationResult = await _validator.ValidateAsync(input);
    // ...
}
```

### 3.2 Prescriptions模块迁移方案

**当前实现**：
- `PrescriptionCreateDto` + `PrescriptionCreateDtoValidator`
- `PrescriptionEditDto` + `PrescriptionEditDtoValidator`
- `PrescriptionItemInputDto` + `PrescriptionItemInputDtoValidator`（嵌套，保留）

**目标实现**：
- `PrescriptionInputDto` + `PrescriptionInputDtoValidator`（统一）
- `PrescriptionItemInputDto` + `PrescriptionItemInputDtoValidator`（嵌套，保留）

**迁移步骤同MedicalCase**。

### 3.3 验证常量统一管理

**当前问题**：
- 验证规则中硬编码了大量魔法数字（如：`MaximumLength(1000)`）
- 缺乏集中管理

**解决方案**：
```csharp
// LYBT.Shared.Models.Constants/ValidationConstants.cs
namespace LYBT.Shared.Models.Constants
{
    /// <summary>
    /// 验证常量
    /// </summary>
    public static class ValidationConstants
    {
        // 长度限制
        public const int NameMaxLength = 100;
        public const int ShortTextMaxLength = 200;
        public const int RemarkMaxLength = 1000;
        public const int LongRemarkMaxLength = 2000;
        public const int AddressMaxLength = 200;
        public const int PhoneMaxLength = 20;

        // 数值范围
        public const int AgeMinValue = 0;
        public const int AgeMaxValue = 150;
        public const int QuantityMinValue = 0;
        public const int QuantityMaxValue = 1000;
        public const int DosageCountMinValue = 1;
        public const int DosageCountMaxValue = 100;

        // 正则表达式
        public const string IdCardRegex = @"^\d{17}[\dXx]$";
        public const string PhoneRegex = @"^1[3-9]\d{9}$";
        public const string EmailRegex = @"^[\w-\.]+@([\w-]+\.)+[\w-]{2,4}$";
    }
}
```

**使用示例**：
```csharp
RuleFor(x => x.Name)
    .NotEmpty()
    .MaximumLength(ValidationConstants.NameMaxLength)
    .WithMessage($"姓名长度不能超过{ValidationConstants.NameMaxLength}个字符");
```

---

## 4. Phase拆分与实施计划

### Phase 1: MedicalCase模块重构（3-5天）

**目标**：将MedicalCase模块从分离验证器迁移到条件验证模式

**任务清单**：
1. ✅ 创建`MedicalCaseInputDto`（合并Create/Update）
2. ✅ 创建`MedicalCaseInputDtoValidator`（条件验证）
3. ✅ 更新`MedicalCaseService.CreateAsync/UpdateAsync`
4. ✅ 更新`MedicalCaseController`
5. ✅ 删除`MedicalCaseCreateDto`和`MedicalCaseUpdateDto`
6. ✅ 删除`MedicalCaseCreateDtoValidator`和`MedicalCaseUpdateDtoValidator`
7. ✅ 更新AutoMapper配置
8. ✅ 编写单元测试（创建/更新场景）
9. ✅ 编译验证（0 errors, 0 warnings）
10. ✅ 功能测试（创建/更新病案）

**验收标准**：
- MedicalCase模块只有1个InputDto和1个验证器
- 创建和更新功能正常工作
- 所有测试通过

### Phase 2: Prescriptions模块重构（3-5天）

**目标**：将Prescriptions模块从分离验证器迁移到条件验证模式

**任务清单**：
1. ✅ 创建`PrescriptionInputDto`（合并Create/Edit）
2. ✅ 创建`PrescriptionInputDtoValidator`（条件验证）
3. ✅ 更新`PrescriptionService.CreateAsync/UpdateAsync`
4. ✅ 更新`PrescriptionController`
5. ✅ 删除`PrescriptionCreateDto`和`PrescriptionEditDto`
6. ✅ 删除`PrescriptionCreateDtoValidator`和`PrescriptionEditDtoValidator`
7. ✅ 保留`PrescriptionItemInputDtoValidator`（嵌套验证器）
8. ✅ 更新AutoMapper配置
9. ✅ 编写单元测试（创建/更新场景）
10. ✅ 编译验证（0 errors, 0 warnings）
11. ✅ 功能测试（创建/更新处方）

**验收标准**：
- Prescriptions模块只有1个InputDto和2个验证器（Input + Item）
- 创建和更新功能正常工作
- 嵌套集合验证正常
- 所有测试通过

### Phase 3: 验证常量统一管理（2-3天）

**目标**：将硬编码的验证规则提取到`ValidationConstants`

**任务清单**：
1. ✅ 创建`ValidationConstants.cs`
2. ✅ 提取所有验证器中的魔法数字
3. ✅ 更新所有验证器使用常量
4. ✅ 编译验证（0 errors, 0 warnings）
5. ✅ 回归测试（所有验证器）

**验收标准**：
- 所有验证器使用`ValidationConstants`
- 无硬编码的魔法数字
- 所有测试通过

### Phase 4: 测试补充与文档更新（2-3天）

**目标**：补充测试用例，更新文档

**任务清单**：
1. ✅ 为所有验证器补充单元测试
2. ✅ 补充条件验证场景测试（创建/更新）
3. ✅ 补充嵌套集合验证测试
4. ✅ 更新`validation-patterns.md`文档
5. ✅ 更新API文档（如果需要）
6. ✅ 更新架构文档

**验收标准**：
- 测试覆盖率 ≥ 80%
- 所有验证器有完整测试
- 文档完整更新

### Phase 5: 代码审查与优化（1-2天）

**目标**：代码审查，性能优化

**任务清单**：
1. ✅ 代码审查（命名、规范、注释）
2. ✅ 性能测试（验证器性能）
3. ✅ 优化建议实施
4. ✅ 最终验收

**验收标准**：
- 代码审查通过
- 性能测试通过
- 所有验收标准满足

---

## 5. 风险评估

### 5.1 技术风险

| 风险 | 影响 | 概率 | 缓解措施 |
|-----|------|------|---------|
| DTO变更导致编译错误 | 中 | 高 | 分Phase实施，每次只改1个模块 |
| Service层逻辑需要调整 | 中 | 中 | 详细的迁移方案，充分测试 |
| AutoMapper配置需要更新 | 低 | 高 | 简单的配置调整 |
| 现有功能受影响 | 高 | 低 | 充分的功能测试和回归测试 |

### 5.2 业务风险

| 风险 | 影响 | 概率 | 缓解措施 |
|-----|------|------|---------|
| 迁移导致业务中断 | 高 | 低 | 在测试环境充分验证 |
| 用户体验受影响 | 中 | 低 | API向后兼容，前端无需修改 |

### 5.3 时间风险

| 风险 | 影响 | 概率 | 缓解措施 |
|-----|------|------|---------|
| 工作量估算不准确 | 中 | 中 | 预留20%缓冲时间 |
| 测试时间不足 | 高 | 中 | 优先保证测试质量 |

---

## 6. 验收标准

### 6.1 功能验收

- ✅ MedicalCase模块使用`MedicalCaseInputDto`和`MedicalCaseInputDtoValidator`
- ✅ Prescriptions模块使用`PrescriptionInputDto`和`PrescriptionInputDtoValidator`
- ✅ 所有验证器使用`ValidationConstants`
- ✅ 创建和更新功能正常工作
- ✅ 嵌套集合验证正常工作

### 6.2 质量验收

- ✅ 编译：0 errors, 0 warnings
- ✅ 测试覆盖率：≥ 80%
- ✅ 所有单元测试通过
- ✅ 所有功能测试通过
- ✅ 代码审查通过

### 6.3 文档验收

- ✅ `validation-patterns.md`更新（新增MedicalCase/Prescriptions示例）
- ✅ API文档更新（如果需要）
- ✅ 架构文档更新
- ✅ 迁移指南创建

### 6.4 性能验收

- ✅ 验证器性能测试通过
- ✅ API响应时间无明显增加（±5%）

---

## 7. 投资回报率（ROI）

### 7.1 成本估算

| 项目 | 工作量 | 说明 |
|-----|-------|------|
| Phase 1 (MedicalCase) | 3-5天 | DTO重构、验证器重构、Service/Controller调整、测试 |
| Phase 2 (Prescriptions) | 3-5天 | 同上 |
| Phase 3 (ValidationConstants) | 2-3天 | 常量提取、验证器更新 |
| Phase 4 (测试与文档) | 2-3天 | 测试补充、文档更新 |
| Phase 5 (审查与优化) | 1-2天 | 代码审查、性能优化 |
| **总计** | **11-18天** | 预留20%缓冲 → **14-22天** |

### 7.2 收益估算

**短期收益**：
- ✅ 减少DTO数量：4个（MedicalCase 2个 + Prescriptions 2个）
- ✅ 减少验证器数量：2个
- ✅ 减少代码行数：~500行（重复验证规则）
- ✅ 提高代码一致性：所有模块遵循统一模式

**长期收益**：
- ✅ 降低维护成本：统一模式，易于理解和修改
- ✅ 提高开发效率：新模块可直接复用模板
- ✅ 减少Bug风险：统一验证逻辑，减少遗漏
- ✅ 提升代码质量：规范化、可测试性提高

### 7.3 ROI分析

**投入**：14-22天开发时间

**回报**：
- 年度维护成本降低：估计节省10-15天/年（统一模式易维护）
- 新功能开发加速：估计节省2-3天/模块（复用模板）
- Bug修复效率提升：估计节省5-8天/年（统一验证逻辑）

**ROI = (年度节省时间 - 初始投入) / 初始投入**
- 年度节省时间：10 + 2×2（假设2个新模块）+ 5 = 19天（保守估计）
- ROI = (19 - 18) / 18 = **5.6%**（第一年）
- ROI = 19 / 18 = **105.6%**（第二年及以后）

**结论**：第一年即可回本，第二年开始产生正收益，长期ROI可观。

---

## 8. 后续优化方向

### 8.1 异步验证

**场景**：需要数据库查询的验证（如唯一性检查）

**示例**：
```csharp
RuleFor(x => x.Name)
    .NotEmpty()
    .MustAsync(async (name, cancellation) =>
    {
        var exists = await _repository.ExistsByNameAsync(name);
        return !exists;
    })
    .WithMessage("名称已存在");
```

**优先级**：P2（中优先级）

### 8.2 自定义验证扩展方法

**场景**：复用常见的验证规则

**示例**：
```csharp
public static class ValidationExtensions
{
    public static IRuleBuilderOptions<T, string> ChineseMobilePhone<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .Matches(ValidationConstants.PhoneRegex)
            .WithMessage("手机号格式不正确");
    }
}

// 使用
RuleFor(x => x.PhoneNumber)
    .ChineseMobilePhone()
    .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
```

**优先级**：P3（低优先级）

### 8.3 验证错误国际化

**场景**：支持多语言错误消息

**优先级**：P3（低优先级，MVP阶段不需要）

---

## 9. 相关文档

- **[FluentValidation验证模式](validation-patterns.md)** - 条件验证详细说明
- **[患者管理CRUD操作](../how-to/client/patient-management.md)** - 患者模块使用
- **[Server端架构](architecture/server/README.md)** - Service层集成

---

## 10. 决策记录

| 决策项 | 决策结果 | 理由 | 日期 |
|-------|---------|------|------|
| 验证模式选择 | 条件验证模式（InputDto + .When()） | 减少代码重复，统一API设计 | 2025-11-09 |
| 验证器命名 | {ModuleName}InputDtoValidator | 统一命名规范 | 2025-11-09 |
| 嵌套验证器 | 保留（Formula, Prescriptions） | 集合验证需要 | 2025-11-09 |
| 验证常量管理 | 集中管理（ValidationConstants） | 避免魔法数字，易维护 | 2025-11-09 |
| 实施策略 | 渐进式（5 Phases） | 降低风险，可控制 | 2025-11-09 |

---

**文档版本**: v1.0
**最后更新**: 2025-11-09
**下一步行动**: 创建GitHub Issue，启动Phase 1实施
**预计完成时间**: 2025-11-30（3周）
