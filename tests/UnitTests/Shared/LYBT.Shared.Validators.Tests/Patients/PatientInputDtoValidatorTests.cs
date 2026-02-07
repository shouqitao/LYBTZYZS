using FluentAssertions;
using FluentValidation.TestHelper;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Primitives.Validation;
using LYBT.Shared.Validators.Patients;
using Xunit;

namespace LYBT.Shared.Validators.Tests.Patients;

/// <summary>
/// PatientInputDtoValidator 单元测试
/// 验证规则：Name(必填,最长100) + Gender(枚举) + BirthDate(<=今天) + IdNumber(18位) + PhoneNumber(11位) + 各种长度限制
/// </summary>
public class PatientInputDtoValidatorTests
{
    private readonly PatientInputDtoValidator _validator = new();

    #region Valid Input Tests

    [Fact]
    public void Validate_WithValidInput_ShouldPass()
    {
        // Arrange
        var dto = CreateValidPatientInputDto();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithMinimalValidInput_ShouldPass()
    {
        // Arrange - 仅必填字段
        var dto = new PatientInputDto
        {
            Name = "张三"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyOptionalFields_ShouldPass()
    {
        // Arrange
        var dto = new PatientInputDto
        {
            Name = "张三",
            IdNumber = null,
            PhoneNumber = null,
            Address = null,
            AllergyHistory = null,
            MedicalHistory = null
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Name Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyName_ShouldFail(string? name)
    {
        // Arrange
        var dto = CreateValidPatientInputDto();
        dto.Name = name!;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage("姓名不能为空");
    }

    [Fact]
    public void Validate_WithNameTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidPatientInputDto();
        dto.Name = new string('张', ValidationConstants.NameMaxLength + 1);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name)
            .WithErrorMessage($"姓名长度不能超过{ValidationConstants.NameMaxLength}个字符");
    }

    [Fact]
    public void Validate_WithNameAtMaxLength_ShouldPass()
    {
        // Arrange
        var dto = CreateValidPatientInputDto();
        dto.Name = new string('张', ValidationConstants.NameMaxLength);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Name);
    }

    #endregion

    #region Gender Validation Tests

    [Theory]
    [InlineData(Gender.Unknown)]
    [InlineData(Gender.Male)]
    [InlineData(Gender.Female)]
    public void Validate_WithValidGender_ShouldPass(Gender gender)
    {
        // Arrange
        var dto = CreateValidPatientInputDto();
        dto.Gender = gender;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Gender);
    }

    [Fact]
    public void Validate_WithInvalidGender_ShouldFail()
    {
        // Arrange
        var dto = CreateValidPatientInputDto();
        dto.Gender = (Gender)999;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Gender)
            .WithErrorMessage("性别值无效");
    }

    #endregion

    #region BirthDate Validation Tests

    [Fact]
    public void Validate_WithFutureBirthDate_ShouldFail()
    {
        // Arrange
        var dto = CreateValidPatientInputDto();
        dto.BirthDate = DateTime.Today.AddDays(1);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.BirthDate)
            .WithErrorMessage("出生日期不能晚于当前日期");
    }

    [Fact]
    public void Validate_WithValidBirthDate_ShouldPass()
    {
        // Arrange
        var dto = CreateValidPatientInputDto();
        dto.BirthDate = DateTime.Today.AddYears(-30);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.BirthDate);
    }

    [Fact]
    public void Validate_WithTodayBirthDate_ShouldPass()
    {
        // Arrange
        var dto = CreateValidPatientInputDto();
        dto.BirthDate = DateTime.Today;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.BirthDate);
    }

    [Fact]
    public void Validate_WithNullBirthDate_ShouldPass()
    {
        // Arrange
        var dto = CreateValidPatientInputDto();
        dto.BirthDate = null;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.BirthDate);
    }

    #endregion

    #region IdNumber Validation Tests

    [Fact]
    public void Validate_WithInvalidIdNumber_ShouldFail()
    {
        // Arrange
        var dto = CreateValidPatientInputDto();
        dto.IdNumber = "123456"; // 6位，不是18位

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.IdNumber)
            .WithErrorMessage("身份证号格式不正确（应为18位）");
    }

    [Theory]
    [InlineData("110101199001011234")]  // 标准18位
    [InlineData("11010119900101123X")]  // 最后一位为X
    [InlineData("11010119900101123x")]  // 最后一位为x（小写）
    public void Validate_With18DigitIdNumber_ShouldPass(string idNumber)
    {
        // Arrange
        var dto = CreateValidPatientInputDto();
        dto.IdNumber = idNumber;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.IdNumber);
    }

    [Theory]
    [InlineData("1234567890123456789")] // 19位
    [InlineData("12345678901234567")]   // 17位
    [InlineData("abcdefghijklmnopqr")]  // 字母
    public void Validate_WithInvalidIdNumberFormat_ShouldFail(string idNumber)
    {
        // Arrange
        var dto = CreateValidPatientInputDto();
        dto.IdNumber = idNumber;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.IdNumber);
    }

    #endregion

    #region PhoneNumber Validation Tests

    [Fact]
    public void Validate_WithInvalidPhoneNumber_ShouldFail()
    {
        // Arrange
        var dto = CreateValidPatientInputDto();
        dto.PhoneNumber = "12345"; // 太短

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber)
            .WithErrorMessage("手机号格式不正确");
    }

    [Theory]
    [InlineData("13800138000")]
    [InlineData("15912345678")]
    [InlineData("18888888888")]
    [InlineData("19999999999")]
    public void Validate_WithValidPhoneNumber_ShouldPass(string phoneNumber)
    {
        // Arrange
        var dto = CreateValidPatientInputDto();
        dto.PhoneNumber = phoneNumber;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PhoneNumber);
    }

    [Theory]
    [InlineData("12345678901")] // 1开头但第二位是2
    [InlineData("02345678901")] // 不是1开头
    [InlineData("1380013800")]  // 10位
    [InlineData("138001380000")] // 12位
    public void Validate_WithInvalidPhoneFormat_ShouldFail(string phoneNumber)
    {
        // Arrange
        var dto = CreateValidPatientInputDto();
        dto.PhoneNumber = phoneNumber;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PhoneNumber);
    }

    #endregion

    #region Length Limit Tests

    [Fact]
    public void Validate_WithAddressTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidPatientInputDto();
        dto.Address = new string('北', ValidationConstants.AddressMaxLength + 1);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Address)
            .WithErrorMessage($"地址长度不能超过{ValidationConstants.AddressMaxLength}个字符");
    }

    [Fact]
    public void Validate_WithAllergyHistoryTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidPatientInputDto();
        dto.AllergyHistory = new string('过', ValidationConstants.RemarkMaxLength + 1);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.AllergyHistory)
            .WithErrorMessage($"过敏史长度不能超过{ValidationConstants.RemarkMaxLength}个字符");
    }

    [Fact]
    public void Validate_WithMedicalHistoryTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidPatientInputDto();
        dto.MedicalHistory = new string('病', ValidationConstants.LongRemarkMaxLength + 1);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.MedicalHistory)
            .WithErrorMessage($"既往病史长度不能超过{ValidationConstants.LongRemarkMaxLength}个字符");
    }

    [Fact]
    public void Validate_WithEmergencyContactNameTooLong_ShouldFail()
    {
        // Arrange
        var dto = CreateValidPatientInputDto();
        dto.EmergencyContactName = new string('李', ValidationConstants.NameMaxLength + 1);

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EmergencyContactName);
    }

    #endregion

    #region Error Message Tests

    [Fact]
    public void Validate_ShouldReturnCorrectErrorMessages()
    {
        // Arrange
        var dto = new PatientInputDto
        {
            Name = "",
            Gender = (Gender)999,
            BirthDate = DateTime.Today.AddDays(1)
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.Errors.Should().HaveCountGreaterOrEqualTo(3);
        result.Errors.Should().Contain(e => e.ErrorMessage == "姓名不能为空");
        result.Errors.Should().Contain(e => e.ErrorMessage == "性别值无效");
        result.Errors.Should().Contain(e => e.ErrorMessage == "出生日期不能晚于当前日期");
    }

    #endregion

    #region Helper Methods

    private static PatientInputDto CreateValidPatientInputDto()
    {
        return new PatientInputDto
        {
            Name = "张三",
            Gender = Gender.Male,
            BirthDate = DateTime.Today.AddYears(-30),
            PhoneNumber = "13800138000",
            IdNumber = "110101199001011234",
            Address = "北京市朝阳区"
        };
    }

    #endregion
}
