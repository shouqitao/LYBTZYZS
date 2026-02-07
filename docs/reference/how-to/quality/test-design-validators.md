# 测试设计方案 - LYBT.Shared.Validators.Tests

## 1. 模块概述

| 属性 | 值 |
|------|-----|
| **模块路径** | `src/Shared/LYBT.Shared.Validators/` |
| **测试路径** | `tests/UnitTests/Shared/LYBT.Shared.Validators.Tests/` |
| **现有测试数** | 0 |
| **目标测试数** | 120 |
| **新增测试数** | +120 |
| **优先级** | P1 |

---

## 2. 被测组件清单

### 2.1 FluentValidation 验证器 (12个)

| 验证器 | 验证规则数 | 目标测试 |
|--------|-----------|----------|
| LoginRequestValidator | 4 | 6 |
| ChangePasswordRequestValidator | 5 | 8 |
| SuperAdminLoginRequestValidator | 1 | 2 |
| PatientInputDtoValidator | 12 | 15 |
| UserInputDtoValidator | 11 | 14 |
| MedicalCaseInputDtoValidator | 3 | 5 |
| ConsultationInputDtoValidator | 2 | 4 |
| PrescriptionInputDtoValidator | 8+5 | 16 |
| HerbInputDtoValidator | 13 | 15 |
| FormulaInputDtoValidator | 8+8 | 18 |

### 2.2 业务规则验证器 (4个)

| 验证器 | 目标测试 |
|--------|----------|
| PatientBusinessRuleValidator | 10 |
| UserBusinessRuleValidator | 8 |
| PrescriptionBusinessRuleValidator | 12 |
| ValidationContext | 6 |

---

## 3. Auth 验证器测试设计

### 3.1 LoginRequestValidator (6个)

```
Validate_WithValidRequest_ShouldPass
Validate_WithEmptyUsername_ShouldFail
Validate_WithUsernameTooLong_ShouldFail
Validate_WithEmptyPassword_ShouldFail
Validate_WithPasswordTooShort_ShouldFail
Validate_ShouldReturnCorrectErrorMessages
```

### 3.2 ChangePasswordRequestValidator (8个)

```
Validate_WithValidRequest_ShouldPass
Validate_WithEmptyOldPassword_ShouldFail
Validate_WithEmptyNewPassword_ShouldFail
Validate_WithNewPasswordTooShort_ShouldFail
Validate_WithNewPasswordTooLong_ShouldFail
Validate_WithSameOldAndNewPassword_ShouldFail
Validate_WithValidDifferentPasswords_ShouldPass
Validate_ShouldReturnCorrectErrorMessages
```

### 3.3 SuperAdminLoginRequestValidator (2个)

```
Validate_WithValidPassword_ShouldPass
Validate_WithEmptyPassword_ShouldFail
```

---

## 4. Patient 验证器测试设计

### 4.1 PatientInputDtoValidator (15个)

```
Validate_WithValidInput_ShouldPass
Validate_WithEmptyName_ShouldFail
Validate_WithNameTooLong_ShouldFail
Validate_WithInvalidGender_ShouldFail
Validate_WithFutureBirthDate_ShouldFail
Validate_WithValidBirthDate_ShouldPass
Validate_WithInvalidIdNumber_ShouldFail
Validate_With18DigitIdNumber_ShouldPass
Validate_WithInvalidPhoneNumber_ShouldFail
Validate_WithValidPhoneNumber_ShouldPass
Validate_WithAddressTooLong_ShouldFail
Validate_WithAllergyHistoryTooLong_ShouldFail
Validate_WithMedicalHistoryTooLong_ShouldFail
Validate_WithEmptyOptionalFields_ShouldPass
Validate_ShouldReturnCorrectErrorMessages
```

### 4.2 PatientBusinessRuleValidator (10个)

```
ValidateAsync_WithValidPatient_ShouldPass
ValidateAsync_WithEmptyName_ShouldFail
ValidateAsync_WithNameTooLong_ShouldFail
ValidateAsync_WithInvalidGender_ShouldFail
ValidateAsync_WithInvalidPhoneFormat_ShouldFail
ValidateAsync_WithPhoneTooLong_ShouldFail
ValidateAsync_WithAddressTooLong_ShouldFail
ValidateAsync_WithFutureBirthDate_ShouldFail
ValidateAsync_WithUnreasonableAge_ShouldFail
ValidateAsync_WithValidAge_ShouldPass
```

---

## 5. User 验证器测试设计

### 5.1 UserInputDtoValidator (14个)

```
Validate_WithValidNewUser_ShouldPass
Validate_WithEmptyUsername_OnCreate_ShouldFail
Validate_WithEmptyRealName_OnCreate_ShouldFail
Validate_WithRealNameTooLong_ShouldFail
Validate_WithInvalidRole_OnCreate_ShouldFail
Validate_WithPasswordTooShort_ShouldFail
Validate_WithPasswordTooLong_ShouldFail
Validate_WithMismatchedPasswords_ShouldFail
Validate_WithInvalidEmail_ShouldFail
Validate_WithEmailTooLong_ShouldFail
Validate_WithInvalidPhoneNumber_ShouldFail
Validate_WithRemarkTooLong_ShouldFail
Validate_WithEmptyOptionalFields_ShouldPass
Validate_OnUpdate_ShouldNotRequireUsername
```

### 5.2 UserBusinessRuleValidator (8个)

```
ValidateAsync_WithValidUser_ShouldPass
ValidateAsync_WithEmptyUsername_ShouldFail
ValidateAsync_WithReservedUsername_ShouldFail
ValidateAsync_WithPasswordTooShort_ShouldFail
ValidateAsync_AdminCreatingDoctor_ShouldPass
ValidateAsync_DoctorCreatingAdmin_ShouldFail
ValidateAsync_SuperAdminCreatingAny_ShouldPass
ValidateAsync_WithoutContext_ShouldSkipRoleCheck
```

---

## 6. MedicalCase & Consultation 验证器测试设计

### 6.1 MedicalCaseInputDtoValidator (5个)

```
Validate_WithValidInput_ShouldPass
Validate_WithEmptyPatientId_ShouldFail
Validate_WithEmptyUserId_ShouldFail
Validate_WithRemarkTooLong_ShouldFail
Validate_WithEmptyOptionalRemark_ShouldPass
```

### 6.2 ConsultationInputDtoValidator (4个)

```
Validate_WithValidInput_ShouldPass
Validate_WithEmptyTcmDiagnosis_ShouldFail
Validate_WithTcmDiagnosisTooLong_ShouldFail
Validate_WithEmptyOptionalFields_ShouldPass
```

---

## 7. Prescription 验证器测试设计

### 7.1 PrescriptionInputDtoValidator (16个)

```
Validate_WithValidInput_ShouldPass
Validate_WithEmptyMedicalCaseId_OnCreate_ShouldFail
Validate_WithReferencedFormulasTooLong_ShouldFail
Validate_WithAdviceTooLong_ShouldFail
Validate_WithRemarkTooLong_ShouldFail
Validate_WithInvalidDiscount_ShouldFail
Validate_WithValidDiscount_ShouldPass
Validate_WithZeroDosageCount_ShouldFail
Validate_WithDosageCountOver100_ShouldFail
Validate_WithEmptyItems_ShouldFail
Validate_WithValidItems_ShouldPass
Validate_Item_WithEmptyHerbId_ShouldFail
Validate_Item_WithZeroDosage_ShouldFail
Validate_Item_WithDosageOver1000_ShouldFail
Validate_Item_WithUsageTooLong_ShouldFail
Validate_Item_WithRemarkTooLong_ShouldFail
```

### 7.2 PrescriptionBusinessRuleValidator (12个)

```
ValidateAsync_WithValidPrescription_ShouldPass
ValidateAsync_WithReferencedFormulasTooLong_ShouldFail
ValidateAsync_WithAdviceTooLong_ShouldFail
ValidateAsync_WithZeroDosageCount_ShouldFail
ValidateAsync_WithDosageCountOver365_ShouldFail
ValidateAsync_WithEmptyMedicalCaseId_ShouldFail
ValidateAsync_WithEmptyItems_ShouldFail
ValidateAsync_WithItemsOver50_ShouldFail
ValidateAsync_Item_WithEmptyHerbId_ShouldFail
ValidateAsync_Item_WithInvalidDosage_ShouldFail
ValidateSearchParams_WithSqlInjection_ShouldFail
ValidateSearchParams_WithXssCharacters_ShouldFail
```

---

## 8. Herb 验证器测试设计

### 8.1 HerbInputDtoValidator (15个)

```
Validate_WithValidInput_ShouldPass
Validate_WithEmptyName_ShouldFail
Validate_WithNameTooLong_ShouldFail
Validate_WithPinYinCodeTooLong_ShouldFail
Validate_WithCategoryTooLong_ShouldFail
Validate_WithOriginTooLong_ShouldFail
Validate_WithSpecTooLong_ShouldFail
Validate_WithEmptyUnit_ShouldFail
Validate_WithUnitTooLong_ShouldFail
Validate_WithZeroPrice_ShouldFail
Validate_WithNegativePrice_ShouldFail
Validate_WithPriceOver10000_ShouldFail
Validate_WithEffectTooLong_ShouldFail
Validate_WithUsageTooLong_ShouldFail
Validate_WithRemarkTooLong_ShouldFail
```

---

## 9. Formula 验证器测试设计

### 9.1 FormulaInputDtoValidator (18个)

```
Validate_WithValidInput_ShouldPass
Validate_WithEmptyName_ShouldFail
Validate_WithNameTooLong_ShouldFail
Validate_WithEffectTooLong_ShouldFail
Validate_WithDescriptionTooLong_ShouldFail
Validate_WithUsageTooLong_ShouldFail
Validate_WithIndicationsTooLong_ShouldFail
Validate_WithRemarkTooLong_ShouldFail
Validate_WithEmptyHerbs_ShouldFail
Validate_WithValidHerbs_ShouldPass
Validate_HerbItem_WithEmptyHerbName_ShouldFail
Validate_HerbItem_WithHerbNameTooLong_ShouldFail
Validate_HerbItem_WithZeroDosage_ShouldFail
Validate_HerbItem_WithDosageOver1000_ShouldFail
Validate_HerbItem_WithEmptyUnit_ShouldFail
Validate_HerbItem_WithUnitTooLong_ShouldFail
Validate_HerbItem_WithProcessingMethodTooLong_ShouldFail
Validate_HerbItem_WithUsageTooLong_ShouldFail
```

---

## 10. ValidationContext 测试设计 (6个)

```
CanManageRole_SuperAdmin_CanManageAll
CanManageRole_Admin_CanManageDoctor
CanManageRole_Admin_CannotManageAdmin
CanManageRole_Doctor_CannotManageAnyone
IsOwnerOrAdmin_WithOwner_ShouldReturnTrue
IsOwnerOrAdmin_WithAdmin_ShouldReturnTrue
```

---

## 11. 测试数据设计

### 11.1 ValidatorTestData

```csharp
public static class ValidatorTestData
{
    // 有效数据
    public static readonly string ValidUsername = "testuser";
    public static readonly string ValidPassword = "Test@123";
    public static readonly string ValidPhone = "13800138000";
    public static readonly string ValidIdNumber = "110101199001011234";
    public static readonly string ValidEmail = "test@example.com";

    // 无效数据
    public static readonly string TooLongString50 = new string('a', 51);
    public static readonly string TooLongString100 = new string('a', 101);
    public static readonly string TooLongString200 = new string('a', 201);
    public static readonly string TooLongString500 = new string('a', 501);
    public static readonly string TooLongString1000 = new string('a', 1001);
    public static readonly string InvalidPhone = "12345";
    public static readonly string InvalidIdNumber = "123456";
    public static readonly string InvalidEmail = "notanemail";

    // SQL注入测试数据
    public static readonly string SqlInjection = "'; DROP TABLE Users;--";
    public static readonly string XssScript = "<script>alert('xss')</script>";
}
```

### 11.2 TestPatientInputDtoBuilder

```csharp
public static class TestPatientInputDtoBuilder
{
    public static PatientInputDto CreateValid()
    {
        return new PatientInputDto
        {
            Name = "测试患者",
            Gender = Gender.Male,
            BirthDate = DateTime.Today.AddYears(-30),
            PhoneNumber = "13800138000"
        };
    }

    public static PatientInputDto CreateWithName(string name)
    {
        var dto = CreateValid();
        dto.Name = name;
        return dto;
    }
}
```

---

## 12. 测试基类设计

### 12.1 ValidatorTestBase

```csharp
public abstract class ValidatorTestBase<TValidator, TModel>
    where TValidator : AbstractValidator<TModel>, new()
{
    protected readonly TValidator Validator;

    protected ValidatorTestBase()
    {
        Validator = new TValidator();
    }

    protected async Task<ValidationResult> ValidateAsync(TModel model)
    {
        return await Validator.ValidateAsync(model);
    }

    protected void AssertValid(ValidationResult result)
    {
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    protected void AssertInvalid(ValidationResult result, string propertyName)
    {
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == propertyName);
    }

    protected void AssertErrorMessage(ValidationResult result, string expectedMessage)
    {
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains(expectedMessage));
    }
}
```

### 12.2 BusinessRuleValidatorTestBase

```csharp
public abstract class BusinessRuleValidatorTestBase<TValidator, TEntity>
    where TValidator : IBusinessRuleValidator<TEntity>
{
    protected readonly TValidator Validator;
    protected ValidationContext DefaultContext;

    protected BusinessRuleValidatorTestBase(TValidator validator)
    {
        Validator = validator;
        DefaultContext = new ValidationContext
        {
            CurrentUserId = Guid.NewGuid(),
            CurrentUserRole = UserRole.Admin,
            OperationType = BusinessOperation.Create
        };
    }

    protected async Task<ValidationResult> ValidateAsync(TEntity entity)
    {
        return await Validator.ValidateAsync(entity, DefaultContext);
    }
}
```

---

## 13. 验收标准

| 指标 | 目标 |
|------|------|
| Auth 验证器测试数 | 16 |
| Patient 验证器测试数 | 25 |
| User 验证器测试数 | 22 |
| MedicalCase 验证器测试数 | 5 |
| Consultation 验证器测试数 | 4 |
| Prescription 验证器测试数 | 28 |
| Herb 验证器测试数 | 15 |
| Formula 验证器测试数 | 18 |
| ValidationContext 测试数 | 6 |
| 总测试数 | 139 |
| 全部测试通过 | 100% |

---

## 14. 执行计划

| 阶段 | 任务 | 预估时间 |
|------|------|----------|
| 1 | 创建测试基类和测试数据 | 20min |
| 2 | Auth 验证器测试 (16个) | 25min |
| 3 | Patient 验证器测试 (25个) | 40min |
| 4 | User 验证器测试 (22个) | 35min |
| 5 | MedicalCase/Consultation 测试 (9个) | 15min |
| 6 | Prescription 验证器测试 (28个) | 45min |
| 7 | Herb 验证器测试 (15个) | 25min |
| 8 | Formula 验证器测试 (18个) | 30min |
| 9 | ValidationContext 测试 (6个) | 10min |
| 10 | 编译验证和修复 | 15min |
| **总计** | | **~4.5h** |

---

*文档版本: v1.0*
*创建日期: 2026-02-05*
*待代码实现*
