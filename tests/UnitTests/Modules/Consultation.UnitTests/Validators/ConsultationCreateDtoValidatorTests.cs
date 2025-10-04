using FluentValidation.TestHelper;
using LYBT.Module.Consultation.Validators;
using LYBT.Shared.Models.Contracts.Consultation;
using Xunit;

namespace LYBT.Module.Consultation.Tests.Validators;

/// <summary>
/// ConsultationCreateDtoValidator 单元测试
/// Issue #864 - Phase 2.4: Consultation 模块测试
/// </summary>
public class ConsultationCreateDtoValidatorTests
{
    private readonly ConsultationCreateDtoValidator _validator;

    public ConsultationCreateDtoValidatorTests()
    {
        _validator = new ConsultationCreateDtoValidator();
    }

    [Fact]
    public void Validate_WithValidData_PassesValidation()
    {
        // Arrange
        var dto = new ConsultationCreateDto
        {
            MedicalCaseId = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ChiefComplaint = "头痛",
            StartTime = DateTime.Now.AddDays(-1)
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyPatientId_FailsValidation()
    {
        // Arrange
        var dto = new ConsultationCreateDto
        {
            MedicalCaseId = Guid.NewGuid(),
            PatientId = Guid.Empty,
            UserId = Guid.NewGuid(),
            ChiefComplaint = "测试"
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.PatientId);
    }

    [Fact]
    public void Validate_WithChiefComplaintTooLong_FailsValidation()
    {
        // Arrange
        var dto = new ConsultationCreateDto
        {
            MedicalCaseId = Guid.NewGuid(),
            PatientId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            ChiefComplaint = new string('a', 501) // 超过 500 字符限制
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ChiefComplaint);
    }
}
