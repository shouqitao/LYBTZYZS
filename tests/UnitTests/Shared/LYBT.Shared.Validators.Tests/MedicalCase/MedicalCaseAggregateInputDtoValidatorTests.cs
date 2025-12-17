using FluentValidation.TestHelper;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Validators.MedicalCase;
using Xunit;

namespace LYBT.Shared.Validators.Tests.MedicalCase;

/// <summary>
/// MedicalCaseAggregateInputDtoValidator 单元测试
/// OpenSpec: refactor-medicalcase-aggregate-crud (PERSIST-001, PERSIST-002)
/// </summary>
public class MedicalCaseAggregateInputDtoValidatorTests
{
    private readonly MedicalCaseAggregateInputDtoValidator _validator;

    public MedicalCaseAggregateInputDtoValidatorTests()
    {
        _validator = new MedicalCaseAggregateInputDtoValidator();
    }

    #region 基础字段验证

    [Fact]
    public void Validate_WithValidMinimalData_PassesValidation()
    {
        // Arrange
        var dto = new MedicalCaseAggregateInputDto
        {
            Id = Guid.NewGuid()
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithEmptyId_FailsValidation()
    {
        // Arrange
        var dto = new MedicalCaseAggregateInputDto
        {
            Id = Guid.Empty
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Id)
            .WithErrorMessage("医案ID不能为空");
    }

    [Fact]
    public void Validate_WithRemarkTooLong_FailsValidation()
    {
        // Arrange
        var dto = new MedicalCaseAggregateInputDto
        {
            Id = Guid.NewGuid(),
            Remark = new string('a', 1001) // 超过1000字符限制
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Remark);
    }

    [Fact]
    public void Validate_WithEditReasonTooLong_FailsValidation()
    {
        // Arrange
        var dto = new MedicalCaseAggregateInputDto
        {
            Id = Guid.NewGuid(),
            EditReason = new string('a', 1001)
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.EditReason);
    }

    #endregion

    #region 嵌套对象验证

    [Fact]
    public void Validate_WithValidConsultation_PassesValidation()
    {
        // Arrange
        var dto = new MedicalCaseAggregateInputDto
        {
            Id = Guid.NewGuid(),
            Consultation = new ConsultationInputDto
            {
                MedicalCaseId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ChiefComplaint = "头痛"
            }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithValidPrescription_PassesValidation()
    {
        // Arrange
        var dto = new MedicalCaseAggregateInputDto
        {
            Id = Guid.NewGuid(),
            Prescription = new PrescriptionAggregateDto
            {
                NeedsPrescription = true,
                DosageCount = 7,
                Items = new List<PrescriptionItemInputDto>
                {
                    new() { HerbId = Guid.NewGuid(), Dosage = 10 }
                }
            }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNoPrescriptionNeeded_PassesValidation()
    {
        // Arrange - PERSIST-001: 支持"仅诊断无处方"场景
        var dto = new MedicalCaseAggregateInputDto
        {
            Id = Guid.NewGuid(),
            Consultation = new ConsultationInputDto
            {
                MedicalCaseId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ChiefComplaint = "测试"
            },
            Prescription = new PrescriptionAggregateDto
            {
                NeedsPrescription = false,
                Items = new List<PrescriptionItemInputDto>() // 空列表但NeedsPrescription=false
            }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert - 不开处方时，空Items应该通过验证
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WithNullConsultation_PassesValidation()
    {
        // Arrange
        var dto = new MedicalCaseAggregateInputDto
        {
            Id = Guid.NewGuid(),
            Consultation = null
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert - Consultation是可选的
        result.ShouldNotHaveValidationErrorFor(x => x.Consultation);
    }

    [Fact]
    public void Validate_WithNullPrescription_PassesValidation()
    {
        // Arrange
        var dto = new MedicalCaseAggregateInputDto
        {
            Id = Guid.NewGuid(),
            Prescription = null
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert - Prescription是可选的
        result.ShouldNotHaveValidationErrorFor(x => x.Prescription);
    }

    #endregion

    #region 完整场景测试

    [Fact]
    public void Validate_WithCompleteValidData_PassesValidation()
    {
        // Arrange - PERSIST-002: 完整的聚合DTO结构
        var dto = new MedicalCaseAggregateInputDto
        {
            Id = Guid.NewGuid(),
            Remark = "复诊患者",
            EditReason = null,
            Consultation = new ConsultationInputDto
            {
                MedicalCaseId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ChiefComplaint = "头痛三天",
                TCMDiagnosis = "肝阳上亢"
            },
            Prescription = new PrescriptionAggregateDto
            {
                NeedsPrescription = true,
                DosageCount = 7,
                Usage = "每日一剂，早晚分服",
                Advice = "忌辛辣",
                Items = new List<PrescriptionItemInputDto>
                {
                    new() { HerbId = Guid.NewGuid(), Dosage = 15, HerbName = "天麻" },
                    new() { HerbId = Guid.NewGuid(), Dosage = 10, HerbName = "钩藤" }
                }
            }
        };

        // Act
        var result = _validator.TestValidate(dto);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    #endregion
}
