using FluentAssertions;
using FluentValidation.TestHelper;
using LYBT.Module.Patients.Validators;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Module.Patients.Tests.Validators;

/// <summary>
/// PatientUpdateDtoValidator 单元测试
/// Issue #865 - Phase 2.1: Patients 模块测试补充
/// 测试 PatientUpdateDto 的所有验证规则
/// </summary>
public class PatientUpdateDtoValidatorTests
{
    private readonly PatientUpdateDtoValidator _validator;

    public PatientUpdateDtoValidatorTests()
    {
        _validator = new PatientUpdateDtoValidator();
    }

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        // Arrange
        var dto = new PatientUpdateDto
        {
            Id = Guid.NewGuid(),
            Name = "张三",
            Gender = Gender.Male,
            PhoneNumber = "13812345678",
            IdNumber = "110101199001011234"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #region Name 验证测试

    [Fact]
    public void Validate_WithEmptyName_FailsValidation()
    {
        // Arrange
        var dto = new PatientUpdateDto
        {
            Id = Guid.NewGuid(),
            Name = "",
            Gender = Gender.Male
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("姓名不能为空");
    }

    [Fact]
    public void Validate_WithNullName_FailsValidation()
    {
        // Arrange
        var dto = new PatientUpdateDto
        {
            Id = Guid.NewGuid(),
            Name = null!,
            Gender = Gender.Male
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WithNameExceeding50Characters_FailsValidation()
    {
        // Arrange
        var dto = new PatientUpdateDto
        {
            Id = Guid.NewGuid(),
            Name = new string('名', 51),
            Gender = Gender.Male
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("姓名长度不能超过50个字符");
    }

    [Fact]
    public void Validate_WithNameExactly50Characters_PassesValidation()
    {
        // Arrange
        var dto = new PatientUpdateDto
        {
            Id = Guid.NewGuid(),
            Name = new string('名', 50),
            Gender = Gender.Male
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region PhoneNumber 验证测试

    [Fact]
    public void Validate_WithValidPhoneNumber_PassesValidation()
    {
        // Arrange
        var dto = new PatientUpdateDto
        {
            Id = Guid.NewGuid(),
            Name = "李四",
            PhoneNumber = "13912345678"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Fact]
    public void Validate_WithNullPhoneNumber_PassesValidation()
    {
        // Arrange
        var dto = new PatientUpdateDto
        {
            Id = Guid.NewGuid(),
            Name = "王五",
            PhoneNumber = null
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Fact]
    public void Validate_WithEmptyPhoneNumber_PassesValidation()
    {
        // Arrange
        var dto = new PatientUpdateDto
        {
            Id = Guid.NewGuid(),
            Name = "赵六",
            PhoneNumber = ""
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Theory]
    [InlineData("13812345678")]  // 移动
    [InlineData("15912345678")]  // 移动
    [InlineData("18812345678")]  // 移动
    [InlineData("13012345678")]  // 联通
    [InlineData("14512345678")]  // 联通
    [InlineData("17712345678")]  // 电信
    [InlineData("19912345678")]  // 电信
    public void Validate_WithVariousValidPhoneNumbers_PassesValidation(string phoneNumber)
    {
        // Arrange
        var dto = new PatientUpdateDto
        {
            Id = Guid.NewGuid(),
            Name = "孙七",
            PhoneNumber = phoneNumber
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Theory]
    [InlineData("12812345678")]  // 不以1开头后跟3-9
    [InlineData("1381234567")]   // 少于11位
    [InlineData("138123456789")] // 多于11位
    [InlineData("abcdefghijk")]  // 非数字
    [InlineData("138-1234-5678")]// 包含分隔符
    public void Validate_WithInvalidPhoneNumber_FailsValidation(string phoneNumber)
    {
        // Arrange
        var dto = new PatientUpdateDto
        {
            Id = Guid.NewGuid(),
            Name = "周八",
            PhoneNumber = phoneNumber
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
            .WithErrorMessage("手机号格式不正确");
    }

    #endregion

    #region IdNumber 验证测试

    [Fact]
    public void Validate_WithValid18DigitIdNumber_PassesValidation()
    {
        // Arrange
        var dto = new PatientUpdateDto
        {
            Id = Guid.NewGuid(),
            Name = "吴九",
            IdNumber = "110101199001011234"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.IdNumber);
    }

    [Fact]
    public void Validate_WithValid15DigitIdNumber_PassesValidation()
    {
        // Arrange
        var dto = new PatientUpdateDto
        {
            Id = Guid.NewGuid(),
            Name = "郑十",
            IdNumber = "110101900101123"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.IdNumber);
    }

    [Fact]
    public void Validate_WithValid17DigitPlusX_IdNumber_PassesValidation()
    {
        // Arrange
        var dto = new PatientUpdateDto
        {
            Id = Guid.NewGuid(),
            Name = "钱十一",
            IdNumber = "11010119900101123X"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.IdNumber);
    }

    [Fact]
    public void Validate_WithNullIdNumber_PassesValidation()
    {
        // Arrange
        var dto = new PatientUpdateDto
        {
            Id = Guid.NewGuid(),
            Name = "冯十二",
            IdNumber = null
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.IdNumber);
    }

    [Fact]
    public void Validate_WithEmptyIdNumber_PassesValidation()
    {
        // Arrange
        var dto = new PatientUpdateDto
        {
            Id = Guid.NewGuid(),
            Name = "陈十三",
            IdNumber = ""
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.IdNumber);
    }

    [Theory]
    [InlineData("1234567890")]        // 太短
    [InlineData("12345678901234567")] // 16位，不符合规则
    [InlineData("abcdefghijklmnopq")] // 非数字
    [InlineData("110101-19900101-1234")] // 包含分隔符
    public void Validate_WithInvalidIdNumber_FailsValidation(string idNumber)
    {
        // Arrange
        var dto = new PatientUpdateDto
        {
            Id = Guid.NewGuid(),
            Name = "褚十四",
            IdNumber = idNumber
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.IdNumber)
            .WithErrorMessage("身份证号格式不正确");
    }

    #endregion

    #region 综合场景测试

    [Fact]
    public void Validate_WithAllFieldsValid_PassesValidation()
    {
        // Arrange
        var dto = new PatientUpdateDto
        {
            Id = Guid.NewGuid(),
            Name = "完整信息患者",
            Gender = Gender.Female,
            PhoneNumber = "13912345678",
            IdNumber = "110101199501011234",
            Address = "北京市朝阳区",
            AllergyHistory = "青霉素过敏"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithOnlyRequiredFields_PassesValidation()
    {
        // Arrange
        var dto = new PatientUpdateDto
        {
            Id = Guid.NewGuid(),
            Name = "最小信息患者"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithMultipleInvalidFields_ReturnsAllErrors()
    {
        // Arrange
        var dto = new PatientUpdateDto
        {
            Id = Guid.NewGuid(),
            Name = "",
            PhoneNumber = "invalid-phone",
            IdNumber = "invalid-id"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
        result.ShouldHaveValidationErrorFor(x => x.IdNumber);
    }

    #endregion
}
