# 测试设计方案 - LYBT.Shared.Models.Tests

## 1. 模块概述

| 属性 | 值 |
|------|-----|
| **模块路径** | `src/Shared/LYBT.Shared.Models/` |
| **测试路径** | `tests/UnitTests/Shared/LYBT.Shared.Models.Tests/` |
| **现有测试数** | 4 |
| **目标测试数** | 45 |
| **新增测试数** | +41 |
| **优先级** | P2 |

---

## 2. 被测组件清单

### 2.1 Common DTO

| 类 | 现有测试 | 目标测试 | 新增 |
|----|----------|----------|------|
| PagedQueryBaseDto | 4 | 4 | 0 |
| Result<T> | 0 | 8 | +8 |
| ServiceResult | 0 | 5 | +5 |
| PagedResult<T> | 0 | 4 | +4 |
| ApiResponse<T> | 0 | 4 | +4 |

### 2.2 枚举

| 枚举 | 现有测试 | 目标测试 | 新增 |
|------|----------|----------|------|
| UserRole | 0 | 4 | +4 |
| AuthErrorCode | 0 | 3 | +3 |
| Gender | 0 | 3 | +3 |
| MedicalCaseStatus | 0 | 4 | +4 |
| FormulaType | 0 | 2 | +2 |

### 2.3 Extensions

| 类 | 现有测试 | 目标测试 | 新增 |
|----|----------|----------|------|
| EnumExtensions | 0 | 4 | +4 |
| DtoConversionExtensions | 0 | 4 | +4 |

---

## 3. Result<T> 测试设计 (8个)

```
Success_WithData_ShouldReturnSuccessResult
Success_ShouldSetIsSuccessTrue
Failure_WithMessage_ShouldReturnFailureResult
Failure_ShouldSetIsSuccessFalse
Failure_WithMultipleErrors_ShouldContainAllErrors
Failure_ShouldContainErrorMessage
ImplicitConversion_FromData_ShouldCreateSuccess
ImplicitConversion_ToBoolean_ShouldReturnIsSuccess
```

---

## 4. ServiceResult 测试设计 (5个)

```
Success_ShouldReturnSuccessResult
Failure_WithMessage_ShouldReturnFailure
Failure_WithErrorCode_ShouldContainCode
IsSuccess_WithSuccessResult_ShouldReturnTrue
IsSuccess_WithFailureResult_ShouldReturnFalse
```

---

## 5. PagedResult<T> 测试设计 (4个)

```
Constructor_ShouldInitializeProperties
TotalPages_ShouldCalculateCorrectly
HasNextPage_OnLastPage_ShouldReturnFalse
HasPreviousPage_OnFirstPage_ShouldReturnFalse
```

---

## 6. ApiResponse<T> 测试设计 (4个)

```
Success_ShouldSetSuccessTrue
Success_ShouldContainData
Failure_ShouldSetSuccessFalse
Failure_ShouldContainMessage
```

---

## 7. UserRole 测试设计 (4个)

```
UserRole_Values_ShouldHaveCorrectOrder
UserRole_SuperAdmin_ShouldHaveHighestValue
UserRole_Doctor_ShouldBeLessThanAdmin
UserRole_Comparison_ShouldWorkCorrectly
```

---

## 8. AuthErrorCode 测试设计 (3个)

```
AuthErrorCode_AuthenticationErrors_ShouldBeIn1xxRange
AuthErrorCode_TokenErrors_ShouldBeIn2xxRange
AuthErrorCode_SessionErrors_ShouldBeIn3xxRange
```

---

## 9. Gender 测试设计 (3个)

```
Gender_Unknown_ShouldBeDefault
Gender_Values_ShouldBeValid
Gender_ToString_ShouldReturnCorrectValue
```

---

## 10. MedicalCaseStatus 测试设计 (4个)

```
MedicalCaseStatus_Draft_ShouldBeDefault
MedicalCaseStatus_Transitions_ShouldBeValid
MedicalCaseStatus_Completed_ShouldNotTransitionToDraft
MedicalCaseStatus_Values_ShouldBeInOrder
```

---

## 11. FormulaType 测试设计 (2个)

```
FormulaType_Classic_ShouldBe1
FormulaType_Experience_ShouldBe2
```

---

## 12. EnumExtensions 测试设计 (4个)

```
GetDescription_WithDescriptionAttribute_ShouldReturnDescription
GetDescription_WithoutAttribute_ShouldReturnEnumName
ToEnumItem_ShouldCreateCorrectItem
GetAllItems_ShouldReturnAllValues
```

---

## 13. DtoConversionExtensions 测试设计 (4个)

```
ToPagedResult_ShouldConvertCorrectly
ToServiceResult_FromSuccess_ShouldCreateSuccess
ToServiceResult_FromFailure_ShouldCreateFailure
ToApiResponse_ShouldConvertCorrectly
```

---

## 14. 测试数据设计

### 14.1 TestEnumData

```csharp
public static class TestEnumData
{
    public static IEnumerable<object[]> UserRoleData => new[]
    {
        new object[] { UserRole.Receptionist, 0 },
        new object[] { UserRole.Doctor, 1 },
        new object[] { UserRole.Admin, 10 },
        new object[] { UserRole.SuperAdmin, 100 }
    };

    public static IEnumerable<object[]> AuthErrorCodeRangeData => new[]
    {
        new object[] { AuthErrorCode.InvalidCredentials, 100, 199 },
        new object[] { AuthErrorCode.TokenExpired, 200, 299 },
        new object[] { AuthErrorCode.SessionExpired, 300, 399 }
    };

    public static IEnumerable<object[]> MedicalCaseStatusData => new[]
    {
        new object[] { MedicalCaseStatus.Draft, 0 },
        new object[] { MedicalCaseStatus.Active, 1 },
        new object[] { MedicalCaseStatus.Completed, 2 },
        new object[] { MedicalCaseStatus.Cancelled, 3 }
    };
}
```

---

## 15. Mock 策略

此模块主要是 DTO 和枚举测试，不需要复杂的 Mock 设置。测试主要验证：

1. **工厂方法**: 验证 Success/Failure 等静态方法
2. **属性计算**: 验证 TotalPages, HasNextPage 等计算属性
3. **枚举值**: 验证枚举值和顺序
4. **扩展方法**: 验证转换逻辑

```csharp
public class ResultTests
{
    [Fact]
    public void Success_WithData_ShouldReturnSuccessResult()
    {
        // Arrange
        var data = new UserDto { Id = Guid.NewGuid() };

        // Act
        var result = Result<UserDto>.Success(data);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be(data);
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Failure_WithMessage_ShouldReturnFailureResult()
    {
        // Arrange
        var errorMessage = "操作失败";

        // Act
        var result = Result<UserDto>.Failure(errorMessage);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Errors.Should().Contain(errorMessage);
    }
}

public class UserRoleTests
{
    [Theory]
    [MemberData(nameof(TestEnumData.UserRoleData), MemberType = typeof(TestEnumData))]
    public void UserRole_Values_ShouldHaveCorrectValues(UserRole role, int expectedValue)
    {
        // Assert
        ((int)role).Should().Be(expectedValue);
    }

    [Fact]
    public void UserRole_SuperAdmin_ShouldHaveHighestValue()
    {
        // Assert
        ((int)UserRole.SuperAdmin).Should().BeGreaterThan((int)UserRole.Admin);
        ((int)UserRole.Admin).Should().BeGreaterThan((int)UserRole.Doctor);
    }
}
```

---

## 16. 验收标准

| 指标 | 目标 |
|------|------|
| Common DTO 测试数 | 25 |
| 枚举测试数 | 16 |
| Extensions 测试数 | 8 |
| 总测试数 | 49 |
| Result 模式覆盖 | 100% |
| 枚举值覆盖 | 100% |

---

## 17. 执行计划

| 阶段 | 任务 | 预估时间 |
|------|------|----------|
| 1 | Result<T> 测试 (8个) | 20min |
| 2 | ServiceResult 测试 (5个) | 12min |
| 3 | PagedResult 测试 (4个) | 10min |
| 4 | ApiResponse 测试 (4个) | 10min |
| 5 | 枚举测试 (16个) | 30min |
| 6 | Extensions 测试 (8个) | 20min |
| 7 | 编译验证和修复 | 10min |
| **总计** | | **~2h** |

---

*文档版本: v1.0*
*创建日期: 2026-02-05*
*待代码实现*
