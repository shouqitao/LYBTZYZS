# ADR-001: FluentValidation作为统一验证框架

**日期**: 2025-10-26（追溯决策记录）
**状态**: Accepted
**决策者**: 项目架构团队
**标签**: #架构 #验证 #技术选型

---

## 📋 元数据

| 属性 | 值 |
|------|------|
| **ADR编号** | ADR-001 |
| **创建日期** | 2025-10-26 |
| **最后更新** | 2025-10-26 |
| **状态** | Accepted（追溯记录） |
| **决策者** | 项目架构团队 |
| **影响范围** | Server端全系统 |
| **相关Issue** | 无（早期技术选型） |
| **取代ADR** | 无 |

---

## 🎯 背景（Context）

### 问题描述

Server端需要统一的数据验证框架，用于：
1. **DTO验证**：验证API请求数据的完整性和合法性
2. **业务规则验证**：实现复杂的业务规则检查（如患者手机号唯一性、医案状态转换合法性）
3. **错误消息管理**：提供清晰、可本地化的错误提示
4. **验证逻辑复用**：避免在Controller、Service层重复编写验证代码

### 当前状态（选型前）

有三种可选验证方案：
1. **DataAnnotations**：ASP.NET Core内置验证框架
2. **手动验证**：在Service层手动编写if-else验证逻辑
3. **FluentValidation**：第三方验证框架，提供流式API

### 问题影响

如果不统一验证框架，会导致：
- **验证逻辑分散**：部分在DataAnnotations，部分在Service层手动验证
- **可维护性差**：验证规则修改需要多处同步
- **错误消息不一致**：不同地方使用不同的错误提示格式
- **测试困难**：手动验证逻辑难以进行单元测试

---

## ✅ 决策（Decision）

**选择FluentValidation作为Server端统一验证框架**：

### 核心原则

1. **所有DTO必须配套Validator**：CreateXxxDto、UpdateXxxDto等
2. **验证器独立测试**：每个Validator都有对应的单元测试
3. **与ASP.NET Core集成**：自动在Controller层触发验证
4. **支持依赖注入**：可在Validator中注入Repository进行数据库查询验证

### 技术实现

**项目结构**：
```
LYBT.Application/
├── DTOs/
│   ├── CreatePatientDto.cs
│   └── UpdatePatientDto.cs
├── Validators/
│   ├── CreatePatientDtoValidator.cs
│   └── UpdatePatientDtoValidator.cs
└── ServiceCollectionExtensions.cs（注册验证器）
```

**验证器示例**：
```csharp
public class CreatePatientDtoValidator : AbstractValidator<CreatePatientDto>
{
    private readonly IPatientRepository _patientRepository;

    public CreatePatientDtoValidator(IPatientRepository patientRepository)
    {
        _patientRepository = patientRepository;

        // 基础字段验证
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("患者姓名不能为空")
            .MaximumLength(50).WithMessage("患者姓名不能超过50个字符");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("手机号不能为空")
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确");

        // 业务规则验证（依赖数据库）
        RuleFor(x => x.Phone)
            .MustAsync(async (dto, phone, cancellation) =>
            {
                return !await _patientRepository.ExistsByPhoneAsync(phone, dto.Id);
            })
            .WithMessage("手机号已存在");
    }
}
```

**ASP.NET Core集成**：
```csharp
// Program.cs or ServiceCollectionExtensions.cs
services.AddValidatorsFromAssembly(typeof(CreatePatientDtoValidator).Assembly);
services.AddFluentValidationAutoValidation();
services.AddFluentValidationClientsideAdapters();
```

**自动触发验证**：
```csharp
[HttpPost]
public async Task<IActionResult> CreatePatient([FromBody] CreatePatientDto dto)
{
    // FluentValidation自动验证，验证失败返回422状态码
    // 无需手动调用validator.ValidateAsync()
    var patient = await _patientService.CreateAsync(dto);
    return Ok(patient);
}
```

---

## 📊 后果（Consequences）

### 优点（Pros）

- ✅ **流式API可读性强**：`RuleFor(x => x.Name).NotEmpty().MaximumLength(50)`比DataAnnotations清晰
- ✅ **支持依赖注入**：可在Validator中注入Repository进行数据库验证
- ✅ **测试友好**：验证逻辑独立，易于单元测试
- ✅ **错误消息可定制**：支持`.WithMessage()`定制中文错误消息
- ✅ **支持异步验证**：`MustAsync`用于数据库查询验证
- ✅ **复杂规则支持**：`When`、`Unless`、`Must`支持条件验证和自定义规则
- ✅ **与ASP.NET Core无缝集成**：自动触发验证，返回422状态码

### 缺点（Cons）

- ❌ **第三方依赖**：需要引入NuGet包（FluentValidation、FluentValidation.AspNetCore）
- ❌ **学习成本**：团队需要学习FluentValidation的API
- ❌ **代码量增加**：每个DTO需要单独的Validator类
- ❌ **性能开销**：相比DataAnnotations，FluentValidation略慢（但差异极小，<1ms）

### 风险与缓解措施

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| FluentValidation停止维护 | 未来升级困难 | 项目活跃度高（5k+ stars），社区活跃，风险低 |
| 验证逻辑过于复杂 | 可维护性下降 | 制定Validator编写规范，保持验证逻辑简洁 |
| 数据库验证性能问题 | API响应变慢 | 使用缓存减少数据库查询，仅在必要时使用MustAsync |

---

## 🔄 替代方案（Alternatives Considered）

### 方案A: DataAnnotations（ASP.NET Core内置）

**描述**: 使用[Required]、[MaxLength]等特性标记DTO属性

**示例**：
```csharp
public class CreatePatientDto
{
    [Required(ErrorMessage = "患者姓名不能为空")]
    [MaxLength(50, ErrorMessage = "患者姓名不能超过50个字符")]
    public string Name { get; set; }

    [Required(ErrorMessage = "手机号不能为空")]
    [RegularExpression(@"^1[3-9]\d{9}$", ErrorMessage = "手机号格式不正确")]
    public string Phone { get; set; }
}
```

**优点**:
- ✅ 无需第三方依赖，ASP.NET Core内置
- ✅ 学习成本低，团队熟悉度高
- ✅ 代码量少，验证规则直接标记在DTO上

**缺点**:
- ❌ **不支持依赖注入**：无法注入Repository进行数据库验证
- ❌ **可读性差**：复杂规则需要写在Attribute参数中，代码冗长
- ❌ **不支持异步验证**：无法执行数据库查询验证
- ❌ **不支持条件验证**：无法实现"当字段A为X时，字段B必须为Y"的逻辑

**为什么未采纳**: 缺少依赖注入和异步验证能力，无法满足业务规则验证需求（如患者手机号唯一性检查）

---

### 方案B: 手动验证（Service层编写if-else）

**描述**: 在Service层手动编写验证逻辑

**示例**：
```csharp
public async Task<Patient> CreateAsync(CreatePatientDto dto)
{
    // 手动验证
    if (string.IsNullOrWhiteSpace(dto.Name))
        throw new ValidationException("患者姓名不能为空");

    if (dto.Name.Length > 50)
        throw new ValidationException("患者姓名不能超过50个字符");

    if (await _patientRepository.ExistsByPhoneAsync(dto.Phone))
        throw new ValidationException("手机号已存在");

    // 业务逻辑
    var patient = _mapper.Map<Patient>(dto);
    return await _patientRepository.AddAsync(patient);
}
```

**优点**:
- ✅ 无需第三方依赖
- ✅ 灵活度高，可编写任意验证逻辑

**缺点**:
- ❌ **代码分散**：验证逻辑与业务逻辑混在一起
- ❌ **可维护性差**：验证规则修改需要找到Service中的if-else
- ❌ **难以测试**：验证逻辑无法独立测试
- ❌ **错误消息不一致**：不同Service可能使用不同的错误提示格式

**为什么未采纳**: 可维护性和可测试性差，违反单一职责原则（Service应专注业务逻辑，不应承担验证职责）

---

### 方案C: 混合方案（DataAnnotations + 手动验证）

**描述**: 简单验证用DataAnnotations，复杂验证用手动验证

**优点**:
- ✅ 兼顾简洁性和灵活性

**缺点**:
- ❌ **验证逻辑分散**：部分在DTO，部分在Service
- ❌ **不统一**：不同DTO使用不同验证方式，增加认知负担

**为什么未采纳**: 不统一的验证方式会导致长期维护混乱

---

## 🏗️ 架构例外（Architecture Exceptions）

**无架构例外**：FluentValidation符合三层架构原则，验证器位于Application层，职责清晰。

---

## 📚 参考资料（References）

- **官方文档**: [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- **NuGet包**:
  - `FluentValidation` (11.x)
  - `FluentValidation.AspNetCore` (11.x)
- **架构文档**: `docs/explanation/architecture/server/README.md`
- **业务规则**: `docs/explanation/business-rules.md` (DC-001、DC-002、DC-003)
- **代码位置**: `src/LYBT.Application/Validators/`

---

## 📝 实施计划（Implementation Plan）

### Phase 1: 基础设施搭建（已完成）
- [x] 引入FluentValidation NuGet包
- [x] 配置ASP.NET Core集成（Program.cs）
- [x] 创建Validators目录结构

### Phase 2: 核心模块Validator实现（已完成）
- [x] Patients模块（CreatePatientDtoValidator、UpdatePatientDtoValidator）
- [x] MedicalCase模块（CreateMedicalCaseDtoValidator）
- [x] Consultation模块（CreateConsultationDtoValidator）
- [x] Prescription模块（CreatePrescriptionDtoValidator）

### Phase 3: 验证器单元测试（部分完成）
- [x] CreatePatientDtoValidator测试
- [ ] 其他Validator测试（待补充）

### Phase 4: 文档和规范（本ADR）
- [x] 创建ADR-001记录技术选型
- [ ] 编写Validator编写规范文档

---

## ✅ 验收标准（Acceptance Criteria）

- [x] FluentValidation已集成到ASP.NET Core
- [x] 所有核心模块DTO都有对应Validator
- [x] API验证失败返回422状态码和清晰的错误消息
- [x] 编译通过（0 errors, 0 warnings）
- [ ] 验证器单元测试覆盖率 ≥60%（待补充）

---

## 📅 更新日志（Change Log）

| 日期 | 版本 | 变更内容 | 作者 |
|------|------|----------|------|
| 2025-10-26 | v1.0 | 追溯创建ADR-001，记录FluentValidation选型决策 | Claude/项目团队 |

---

**创建者**: Claude Code（基于项目现状追溯记录）
**审核者**: 待人工审核
**批准者**: 项目架构团队（早期已批准，本ADR追溯记录）

---

## 💡 最佳实践建议

### Validator编写规范

1. **命名规范**：`{Dto名称}Validator`（如`CreatePatientDtoValidator`）
2. **一个DTO一个Validator**：避免一个Validator验证多个DTO
3. **依赖注入最小化**：只注入必要的Repository，避免注入Service
4. **验证逻辑简洁**：复杂业务规则在Service层实现，Validator只做数据合法性检查
5. **错误消息清晰**：使用中文，提供具体的错误原因和修复建议
6. **异步验证谨慎使用**：`MustAsync`会增加数据库查询，仅在必要时使用

### 示例：完整的Validator

```csharp
public class CreatePatientDtoValidator : AbstractValidator<CreatePatientDto>
{
    private readonly IPatientRepository _patientRepository;

    public CreatePatientDtoValidator(IPatientRepository patientRepository)
    {
        _patientRepository = patientRepository;

        // 基础字段验证
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("患者姓名不能为空")
            .MaximumLength(50).WithMessage("患者姓名不能超过50个字符");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("手机号不能为空")
            .Matches(@"^1[3-9]\d{9}$").WithMessage("手机号格式不正确");

        RuleFor(x => x.BirthDate)
            .LessThan(DateTime.Now).WithMessage("出生日期不能晚于今天")
            .GreaterThan(DateTime.Now.AddYears(-150)).WithMessage("出生日期不能早于150年前");

        // 条件验证
        When(x => !string.IsNullOrWhiteSpace(x.IdCard), () =>
        {
            RuleFor(x => x.IdCard)
                .Matches(@"^\d{17}[\dXx]$").WithMessage("身份证号格式不正确");
        });

        // 业务规则验证（依赖数据库）
        RuleFor(x => x.Phone)
            .MustAsync(async (dto, phone, cancellation) =>
            {
                return !await _patientRepository.ExistsByPhoneAsync(phone, dto.Id);
            })
            .WithMessage("手机号已存在，请使用其他手机号");
    }
}
```

### 测试示例

```csharp
public class CreatePatientDtoValidatorTests
{
    private readonly CreatePatientDtoValidator _validator;
    private readonly Mock<IPatientRepository> _mockRepository;

    public CreatePatientDtoValidatorTests()
    {
        _mockRepository = new Mock<IPatientRepository>();
        _validator = new CreatePatientDtoValidator(_mockRepository.Object);
    }

    [Fact]
    public async Task Validate_WhenNameIsEmpty_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreatePatientDto { Name = "" };

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreatePatientDto.Name));
    }

    [Fact]
    public async Task Validate_WhenPhoneExists_ShouldHaveValidationError()
    {
        // Arrange
        var dto = new CreatePatientDto { Phone = "13800138000" };
        _mockRepository.Setup(r => r.ExistsByPhoneAsync("13800138000", null))
            .ReturnsAsync(true);

        // Act
        var result = await _validator.ValidateAsync(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("手机号已存在"));
    }
}
```
