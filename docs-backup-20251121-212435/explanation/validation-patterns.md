# FluentValidation验证模式

> **文档类型**: Explanation
> **目标读者**: 开发人员
> **前置阅读**: [Server端架构](architecture/server/README.md)
> **完成日期**: 2025-11-09（Epic #1934）

---

## 1. 概述

本文档介绍LYBTZYZS项目中使用的FluentValidation验证模式，特别是**条件验证**模式，用于实现创建和更新操作的统一DTO验证。

**核心模式**：
- ✅ 条件验证（`.When()` 谓词）
- ✅ InputDto统一模式（单DTO用于创建和更新）
- ✅ 创建/更新场景区分（通过`Id`字段判断）

**技术栈**：
- FluentValidation 11.x
- .NET 8.0
- ASP.NET Core 8.0

---

## 2. 条件验证模式

### 2.1 核心概念

**问题背景**：
- 创建和更新操作通常需要不同的验证规则
- 传统方式：创建两个独立的DTO（CreateDto、UpdateDto）
- 缺点：代码重复、维护成本高

**解决方案**：
- 使用单一InputDto，通过`Id`字段区分创建和更新
- 使用FluentValidation的`.When()`谓词实现条件验证
- 创建时：`Id == null || Id == Guid.Empty`
- 更新时：`Id`有值

### 2.2 实现示例

#### UserInputDtoValidator（标准示例）

**场景**：用户启用/禁用功能需要更新用户状态，但不需要更新用户名。

**实现代码**：
```csharp
using FluentValidation;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Shared.Validators.Users
{
    public class UserInputDtoValidator : AbstractValidator<UserInputDto>
    {
        public UserInputDtoValidator()
        {
            // 用户名：创建时必填（Id为null），更新时可选
            RuleFor(x => x.UserName)
                .NotEmpty()
                .When(x => x.Id == null || x.Id == Guid.Empty);
        }
    }
}
```

**代码位置**：
- `src/Shared/LYBT.Shared.Validators/Users/UserInputDtoValidator.cs`

**关键点**：
1. **`.NotEmpty()`**：验证规则（不能为空）
2. **`.When(x => x.Id == null || x.Id == Guid.Empty)`**：条件谓词（仅在创建时应用）
3. **`x.Id`**：判断依据（null或Empty表示创建，有值表示更新）

**验证行为**：
| 操作 | Id值 | UserName必填? | 验证结果 |
|-----|------|--------------|---------|
| 创建 | null | 是 | 未填写→验证失败 |
| 创建 | Guid.Empty | 是 | 未填写→验证失败 |
| 更新 | 有效Guid | 否 | 未填写→验证通过 |

### 2.3 应用场景

#### 场景1：用户启用/禁用（Epic #1934修复）

**问题描述**：
- 启用/禁用用户时，只需更新`Status`字段
- 不需要更新`UserName`字段
- 但原有验证器要求`UserName`必填，导致400错误

**解决方案**：
```csharp
// 修改前（错误）：
RuleFor(x => x.UserName).NotEmpty(); // 创建和更新都必填

// 修改后（正确）：
RuleFor(x => x.UserName)
    .NotEmpty()
    .When(x => x.Id == null || x.Id == Guid.Empty); // 仅创建时必填
```

**修复结果**：
- ✅ 创建用户：UserName必填（验证通过）
- ✅ 更新状态：UserName可选（验证通过，400错误已修复）

#### 场景2：患者信息更新（潜在应用）

**场景描述**：
- 创建患者时，`Name`和`IdNumber`必填
- 更新患者时，允许部分字段更新（如只更新手机号）

**实现示例**：
```csharp
public class PatientInputDtoValidator : AbstractValidator<PatientInputDto>
{
    public PatientInputDtoValidator()
    {
        // 患者姓名：创建时必填，更新时可选
        RuleFor(x => x.Name)
            .NotEmpty()
            .When(x => x.Id == null || x.Id == Guid.Empty);

        // 身份证号：创建时必填，更新时可选
        RuleFor(x => x.IdNumber)
            .NotEmpty()
            .When(x => x.Id == null || x.Id == Guid.Empty);

        // 手机号码：始终可选，但需验证格式
        RuleFor(x => x.PhoneNumber)
            .Matches(@"^1[3-9]\d{9}$")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}
```

**注意**：目前患者模块使用DataAnnotations验证（`[Required]`），未来可迁移到FluentValidation。

---

## 3. InputDto统一模式

### 3.1 设计理念

**核心思想**：
- 使用单一InputDto同时支持创建和更新操作
- 通过`Id`字段区分操作类型
- 避免CreateDto和UpdateDto的代码重复

**模式结构**：
```csharp
public class XxxInputDto
{
    // 标识字段（可选，创建时为null，更新时有值）
    public Guid? Id { get; set; }

    // 业务字段
    public string Name { get; set; }
    public string? Description { get; set; }
    // ... 其他字段
}
```

### 3.2 实现示例

#### UserInputDto

```csharp
namespace LYBT.Shared.Models.Contracts.Users
{
    public class UserInputDto
    {
        /// <summary>
        /// 用户ID（更新时必填，创建时为null）
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// 用户名（创建时必填，更新时可选）
        /// </summary>
        public string UserName { get; set; }

        /// <summary>
        /// 状态（启用/禁用）
        /// </summary>
        public CommonStatus Status { get; set; }

        // ... 其他字段
    }
}
```

#### PatientInputDto

```csharp
namespace LYBT.Shared.Models.Contracts.Patients
{
    public class PatientInputDto
    {
        /// <summary>
        /// 患者ID（更新时必填，创建时为null）
        /// </summary>
        public Guid? Id { get; set; }

        /// <summary>
        /// 患者姓名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 性别
        /// </summary>
        public Gender Gender { get; set; }

        /// <summary>
        /// 出生日期
        /// </summary>
        public DateTime? BirthDate { get; set; }

        /// <summary>
        /// 身份证号
        /// </summary>
        public string? IdNumber { get; set; }

        // ... 其他字段
    }
}
```

### 3.3 Service层处理

**CreateAsync方法**：
```csharp
public async Task<UserDto> CreateAsync(UserInputDto input)
{
    // 创建时，Id应为null或Empty
    if (input.Id.HasValue && input.Id.Value != Guid.Empty)
    {
        throw new InvalidOperationException("创建用户时Id必须为null或Empty");
    }

    // 验证（FluentValidation会检查UserName必填）
    var validationResult = await _validator.ValidateAsync(input);
    if (!validationResult.IsValid)
    {
        throw new ValidationException(validationResult.Errors);
    }

    // 创建实体
    var user = new User
    {
        Id = Guid.NewGuid(), // 生成新ID
        UserName = input.UserName,
        Status = input.Status,
        // ... 其他字段
    };

    _context.Users.Add(user);
    await _context.SaveChangesAsync();

    return _mapper.Map<UserDto>(user);
}
```

**UpdateAsync方法**：
```csharp
public async Task<UserDto> UpdateAsync(UserInputDto input)
{
    // 更新时，Id必须有值
    if (!input.Id.HasValue || input.Id.Value == Guid.Empty)
    {
        throw new InvalidOperationException("更新用户时Id不能为null或Empty");
    }

    // 验证（FluentValidation会跳过UserName必填检查）
    var validationResult = await _validator.ValidateAsync(input);
    if (!validationResult.IsValid)
    {
        throw new ValidationException(validationResult.Errors);
    }

    // 获取现有实体
    var user = await _context.Users.FindAsync(input.Id.Value);
    if (user == null)
    {
        throw new NotFoundException($"用户不存在: {input.Id.Value}");
    }

    // 选择性更新字段（仅更新非null字段）
    if (!string.IsNullOrEmpty(input.UserName))
    {
        user.UserName = input.UserName;
    }

    user.Status = input.Status;
    // ... 其他字段

    await _context.SaveChangesAsync();

    return _mapper.Map<UserDto>(user);
}
```

---

## 4. 条件验证进阶

### 4.1 复杂条件验证

**场景**：字段A必填，当字段B满足特定条件时。

**示例**：
```csharp
public class PrescriptionInputDtoValidator : AbstractValidator<PrescriptionInputDto>
{
    public PrescriptionInputDtoValidator()
    {
        // 处方名称：创建时必填
        RuleFor(x => x.Name)
            .NotEmpty()
            .When(x => x.Id == null || x.Id == Guid.Empty);

        // 诊断结果：创建时且类型为"成药处方"时必填
        RuleFor(x => x.Diagnosis)
            .NotEmpty()
            .When(x => (x.Id == null || x.Id == Guid.Empty)
                    && x.PrescriptionType == PrescriptionType.Patent);

        // 草药列表：类型为"草药处方"时必填
        RuleFor(x => x.Herbs)
            .NotEmpty()
            .When(x => x.PrescriptionType == PrescriptionType.Herbal);
    }
}
```

### 4.2 反向条件验证

**场景**：更新时必填，创建时可选。

**示例**：
```csharp
public class OrderInputDtoValidator : AbstractValidator<OrderInputDto>
{
    public OrderInputDtoValidator()
    {
        // 创建时：订单号自动生成，无需填写
        // 更新时：订单号必填（用于查找）
        RuleFor(x => x.OrderNumber)
            .NotEmpty()
            .When(x => x.Id.HasValue && x.Id.Value != Guid.Empty);
    }
}
```

### 4.3 链式条件验证

**场景**：多个验证规则依次应用。

**示例**：
```csharp
public class ProductInputDtoValidator : AbstractValidator<ProductInputDto>
{
    public ProductInputDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("产品名称不能为空")
            .When(x => x.Id == null || x.Id == Guid.Empty)
            .MaximumLength(100).WithMessage("产品名称不能超过100字符")
            .Matches(@"^[\u4e00-\u9fa5a-zA-Z0-9]+$").WithMessage("产品名称只能包含中文、字母和数字");
    }
}
```

---

## 5. 最佳实践

### 5.1 验证器设计原则

1. **单一职责**：每个验证器只负责一个DTO的验证
2. **明确条件**：使用`.When()`时，条件表达式应清晰易懂
3. **错误信息**：使用`.WithMessage()`提供友好的错误提示
4. **性能考虑**：避免在验证器中进行数据库查询（使用异步验证器除外）

### 5.2 命名约定

- 验证器类名：`{DtoName}Validator`
- 示例：`UserInputDtoValidator`, `PatientInputDtoValidator`
- 位置：`LYBT.Shared.Validators/{ModuleName}/`

### 5.3 代码组织

**推荐结构**：
```
LYBT.Shared.Validators/
├── Users/
│   ├── UserInputDtoValidator.cs
│   └── UserLoginDtoValidator.cs
├── Patients/
│   ├── PatientInputDtoValidator.cs
│   └── PatientSearchDtoValidator.cs
├── Prescriptions/
│   ├── PrescriptionInputDtoValidator.cs
│   └── PrescriptionItemDtoValidator.cs
└── Common/
    └── BaseValidator.cs
```

### 5.4 测试验证器

**单元测试示例**：
```csharp
public class UserInputDtoValidatorTests
{
    private readonly UserInputDtoValidator _validator;

    public UserInputDtoValidatorTests()
    {
        _validator = new UserInputDtoValidator();
    }

    [Fact]
    public void Should_RequireUserName_WhenCreating()
    {
        // Arrange
        var dto = new UserInputDto
        {
            Id = null, // 创建场景
            UserName = null, // 未填写
            Status = CommonStatus.Enabled
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "UserName");
    }

    [Fact]
    public void Should_NotRequireUserName_WhenUpdating()
    {
        // Arrange
        var dto = new UserInputDto
        {
            Id = Guid.NewGuid(), // 更新场景
            UserName = null, // 未填写
            Status = CommonStatus.Disabled
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue(); // 更新时UserName可选
    }

    [Fact]
    public void Should_AcceptUserName_WhenCreating()
    {
        // Arrange
        var dto = new UserInputDto
        {
            Id = null, // 创建场景
            UserName = "testuser", // 已填写
            Status = CommonStatus.Enabled
        };

        // Act
        var result = _validator.Validate(dto);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
```

---

## 6. 常见问题

### Q1: `.When()`和`.Unless()`有什么区别?

**答案**：
- `.When(predicate)`：当条件为true时，应用验证规则
- `.Unless(predicate)`：当条件为false时，应用验证规则
- 等价关系：`.When(x => condition)` ≡ `.Unless(x => !condition)`

**示例**：
```csharp
// 使用 .When()
RuleFor(x => x.Name)
    .NotEmpty()
    .When(x => x.Id == null);

// 等价的 .Unless()
RuleFor(x => x.Name)
    .NotEmpty()
    .Unless(x => x.Id != null);
```

### Q2: 如何验证集合字段?

**答案**：使用`RuleForEach()`验证集合中的每个元素。

**示例**：
```csharp
public class PrescriptionInputDtoValidator : AbstractValidator<PrescriptionInputDto>
{
    public PrescriptionInputDtoValidator()
    {
        // 验证草药列表不为空
        RuleFor(x => x.Herbs)
            .NotEmpty()
            .When(x => x.PrescriptionType == PrescriptionType.Herbal);

        // 验证集合中的每个草药项
        RuleForEach(x => x.Herbs)
            .SetValidator(new HerbItemDtoValidator())
            .When(x => x.Herbs != null);
    }
}
```

### Q3: 如何处理异步验证（如数据库查询）?

**答案**：使用`MustAsync()`进行异步验证。

**示例**：
```csharp
public class UserInputDtoValidator : AbstractValidator<UserInputDto>
{
    private readonly IUserRepository _userRepository;

    public UserInputDtoValidator(IUserRepository userRepository)
    {
        _userRepository = userRepository;

        // 用户名唯一性验证（异步）
        RuleFor(x => x.UserName)
            .NotEmpty()
            .When(x => x.Id == null || x.Id == Guid.Empty)
            .MustAsync(async (userName, cancellation) =>
            {
                var exists = await _userRepository.ExistsByUserNameAsync(userName);
                return !exists; // 不存在则验证通过
            })
            .WithMessage("用户名已存在");
    }
}
```

**注意**：
- 异步验证会影响性能，应谨慎使用
- 考虑在Service层进行业务验证，而不是在DTO验证器中

### Q4: 创建和更新能否使用不同的验证器?

**答案**：可以，但不推荐。

**不推荐方式**（分离验证器）：
```csharp
public class UserCreateDtoValidator : AbstractValidator<UserCreateDto> { }
public class UserUpdateDtoValidator : AbstractValidator<UserUpdateDto> { }
```

**推荐方式**（统一验证器 + 条件验证）：
```csharp
public class UserInputDtoValidator : AbstractValidator<UserInputDto>
{
    public UserInputDtoValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty()
            .When(x => x.Id == null || x.Id == Guid.Empty);
    }
}
```

**原因**：
- ✅ 减少代码重复
- ✅ 统一验证逻辑
- ✅ 降低维护成本

---

## 7. 技术参考

### 7.1 FluentValidation官方文档

- 官方网站：https://docs.fluentvalidation.net/
- 条件验证：https://docs.fluentvalidation.net/en/latest/conditions.html
- 异步验证：https://docs.fluentvalidation.net/en/latest/async.html

### 7.2 项目实现参考

**已实现的验证器**：
- `UserInputDtoValidator.cs` - 用户输入验证（标准示例）
- `MedicalCaseInputDtoValidator.cs` - 病案输入验证（Epic #1961实现）

**待实现的验证器**：
- `PatientInputDtoValidator.cs` - 患者输入验证（当前使用DataAnnotations）
- `PrescriptionInputDtoValidator.cs` - 处方输入验证

### 7.3 相关文档

- [Server端三层架构](architecture/server/README.md) - Service层验证集成
- [患者管理CRUD操作指南](../how-to/client/patient-management.md) - 患者模块使用
- [API设计最佳实践](api-design-best-practices.md) - DTO设计规范

---

## 8. 附录：快速参考

### 8.1 常用验证规则

| 规则 | 说明 | 示例 |
|-----|------|------|
| `NotEmpty()` | 不能为空 | `RuleFor(x => x.Name).NotEmpty()` |
| `NotNull()` | 不能为null | `RuleFor(x => x.BirthDate).NotNull()` |
| `Length(min, max)` | 长度范围 | `RuleFor(x => x.Name).Length(1, 50)` |
| `MaximumLength(max)` | 最大长度 | `RuleFor(x => x.Description).MaximumLength(500)` |
| `Matches(regex)` | 正则匹配 | `RuleFor(x => x.Phone).Matches(@"^1[3-9]\d{9}$")` |
| `EmailAddress()` | 邮箱格式 | `RuleFor(x => x.Email).EmailAddress()` |
| `GreaterThan(value)` | 大于某值 | `RuleFor(x => x.Age).GreaterThan(0)` |
| `LessThanOrEqualTo(value)` | 小于等于 | `RuleFor(x => x.Age).LessThanOrEqualTo(120)` |
| `Must(predicate)` | 自定义验证 | `RuleFor(x => x.Status).Must(s => s != CommonStatus.Unknown)` |

### 8.2 条件验证快速参考

```csharp
// 创建时必填
RuleFor(x => x.Field)
    .NotEmpty()
    .When(x => x.Id == null || x.Id == Guid.Empty);

// 更新时必填
RuleFor(x => x.Field)
    .NotEmpty()
    .When(x => x.Id.HasValue && x.Id.Value != Guid.Empty);

// 字段A有值时，字段B必填
RuleFor(x => x.FieldB)
    .NotEmpty()
    .When(x => !string.IsNullOrEmpty(x.FieldA));

// 类型为X时，字段必填
RuleFor(x => x.Field)
    .NotEmpty()
    .When(x => x.Type == SomeType.X);

// 多条件组合
RuleFor(x => x.Field)
    .NotEmpty()
    .When(x => (x.Id == null || x.Id == Guid.Empty)
            && x.Type == SomeType.X);
```

---

**文档版本**: v1.0
**最后更新**: 2025-11-09
**相关Issue**: Epic #1934（用户启用/禁用Bug修复）
**提交哈希**: 3864741dc
