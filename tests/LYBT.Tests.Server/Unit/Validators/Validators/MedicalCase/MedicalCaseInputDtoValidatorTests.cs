using FluentAssertions;
using FluentValidation.TestHelper;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Primitives.Validation;
using LYBT.Shared.Validators.MedicalCase;
using Xunit;

namespace LYBT.Tests.Server;

/// <summary>
/// MedicalCaseInputDtoValidator 单元测试
/// 验证规则：PatientId(必填) + UserId(必填) + Remark(长度限制)
/// </summary>
public class MedicalCaseInputDtoValidatorTests
{
    private readonly MedicalCaseInputDtoValidator _validator = new();

    #region Valid Input Tests

    [Fact]
    public void Validate_WithValidInput_ShouldPass()
    {
        // Arrange
        var dto = CreateValidMedicalCaseInputDto();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyOptionalRemark_ShouldPass()
    {
        // Arrange
        var dto = CreateValidMedicalCaseInputDto();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region PatientId Validation Tests

    [Fact]
    public void Validate_WithEmptyPatientId_ShouldFail()
    {
        // Arrange
        var dto = CreateValidMedicalCaseInputDto();
        dto.PatientId = Guid.Empty;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PatientId)
            .WithErrorMessage("患者ID不能为空");
    }

    [Fact]
    public void Validate_WithValidPatientId_ShouldPass()
    {
        // Arrange
        var dto = CreateValidMedicalCaseInputDto();
        dto.PatientId = Guid.NewGuid();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.PatientId);
    }

    #endregion

    #region UserId Validation Tests

    [Fact]
    public void Validate_WithEmptyUserId_ShouldFail()
    {
        // Arrange
        var dto = CreateValidMedicalCaseInputDto();
        dto.UserId = Guid.Empty;

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("用户ID不能为空");
    }

    [Fact]
    public void Validate_WithValidUserId_ShouldPass()
    {
        // Arrange
        var dto = CreateValidMedicalCaseInputDto();
        dto.UserId = Guid.NewGuid();

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.UserId);
    }

    #endregion

    #region Error Message Tests

    [Fact]
    public void Validate_ShouldReturnCorrectErrorMessages()
    {
        // Arrange
        var dto = new MedicalCaseInputDto
        {
            PatientId = Guid.Empty,
            UserId = Guid.Empty
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.Errors.Should().HaveCount(2);
        result.Errors.Should().Contain(e => e.ErrorMessage == "患者ID不能为空");
        result.Errors.Should().Contain(e => e.ErrorMessage == "用户ID不能为空");
    }

    #endregion

    #region Helper Methods

    private static MedicalCaseInputDto CreateValidMedicalCaseInputDto()
    {
        return new MedicalCaseInputDto
        {
            PatientId = Guid.NewGuid(),
            UserId = Guid.NewGuid()
        };
    }

    #endregion
}
