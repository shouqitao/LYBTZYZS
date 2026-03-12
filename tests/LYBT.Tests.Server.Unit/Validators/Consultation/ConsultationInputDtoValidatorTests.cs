using FluentAssertions;
using FluentValidation.TestHelper;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Validators.Consultation;
using Xunit;

namespace LYBT.Tests.Server.Unit.Validators.Consultation;

/// <summary>
/// ConsultationInputDtoValidator 单元测试
/// 验证规则：TcmDiagnosis(必填,最长500)
/// </summary>
public class ConsultationInputDtoValidatorTests
{
    private readonly ConsultationInputDtoValidator _validator = new();

    #region Valid Input Tests

    [Fact]
    public void Validate_WithValidInput_ShouldPass()
    {
        // Arrange
        var dto = new ConsultationInputDto
        {
            TcmDiagnosis = "脾胃虚弱"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyOptionalFields_ShouldPass()
    {
        // Arrange - 仅必填字段
        var dto = new ConsultationInputDto
        {
            TcmDiagnosis = "气虚血瘀"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region TcmDiagnosis Validation Tests

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WithEmptyTcmDiagnosis_ShouldFail(string? tcmDiagnosis)
    {
        // Arrange
        var dto = new ConsultationInputDto
        {
            TcmDiagnosis = tcmDiagnosis!
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TcmDiagnosis)
            .WithErrorMessage("中医诊断不能为空");
    }

    [Fact]
    public void Validate_WithTcmDiagnosisTooLong_ShouldFail()
    {
        // Arrange
        var dto = new ConsultationInputDto
        {
            TcmDiagnosis = new string('诊', 501) // > 500
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TcmDiagnosis)
            .WithErrorMessage("中医诊断长度不能超过500个字符");
    }

    [Fact]
    public void Validate_WithTcmDiagnosisAtMaxLength_ShouldPass()
    {
        // Arrange
        var dto = new ConsultationInputDto
        {
            TcmDiagnosis = new string('诊', 500)
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.TcmDiagnosis);
    }

    [Theory]
    [InlineData("脾胃虚弱")]
    [InlineData("气虚血瘀，痰湿内阻")]
    [InlineData("肝郁脾虚，湿热下注")]
    public void Validate_WithVariousValidDiagnosis_ShouldPass(string diagnosis)
    {
        // Arrange
        var dto = new ConsultationInputDto
        {
            TcmDiagnosis = diagnosis
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion

    #region Error Message Tests

    [Fact]
    public void Validate_ShouldReturnCorrectErrorMessages()
    {
        // Arrange
        var dto = new ConsultationInputDto
        {
            TcmDiagnosis = ""
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.Errors.Should().HaveCount(1);
        result.Errors.Should().Contain(e => e.ErrorMessage == "中医诊断不能为空");
    }

    #endregion
}
