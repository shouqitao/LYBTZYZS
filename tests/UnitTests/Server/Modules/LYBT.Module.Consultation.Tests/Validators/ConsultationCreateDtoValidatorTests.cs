using FluentValidation.TestHelper;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Validators.Consultation;
using Xunit;

namespace LYBT.Module.Consultations.Tests.Validators;

/// <summary>
/// ConsultationInputDtoValidator 单元测试
/// Issue #864 - Phase 2.4: Consultation 模块测试
/// </summary>
public class ConsultationInputDtoValidatorTests
{
    private readonly ConsultationInputDtoValidator _validator;

    public ConsultationInputDtoValidatorTests()
    {
        _validator = new ConsultationInputDtoValidator();
    }

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        // Arrange
        // OpenSpec: refactor-diagnosis-fields - 移除ChiefComplaint
        var dto = new ConsultationInputDto
        {
            TcmDiagnosis = "外感风寒" // 唯一必填字段
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    /// <summary>
    /// Issue #2231: PatientId不再验证必填
    /// 原因：Consultation实体没有PatientId字段，它通过MedicalCase关联获取
    /// </summary>
    [Fact]
    public void Validate_WithEmptyPatientId_PassesValidation()
    {
        // Arrange
        // OpenSpec: refactor-diagnosis-fields - 移除ChiefComplaint
        var dto = new ConsultationInputDto
        {
            TcmDiagnosis = "测试诊断"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert - Issue #2231: PatientId不再要求必填
        result.ShouldNotHaveValidationErrorFor(x => x.PatientId);
    }

    // OpenSpec: refactor-diagnosis-fields - ChiefComplaint已移除，测试TcmDiagnosis长度验证
    [Fact]
    public void Validate_WithTcmDiagnosisTooLong_FailsValidation()
    {
        // Arrange
        var dto = new ConsultationInputDto
        {
            TcmDiagnosis = new string('a', 501) // 超过 500 字符限制
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TcmDiagnosis);
    }
}
