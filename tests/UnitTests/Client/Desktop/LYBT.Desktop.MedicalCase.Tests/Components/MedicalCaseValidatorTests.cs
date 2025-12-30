using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.Interfaces.Components;
using LYBT.Desktop.MedicalCase.Services; // OpenSpec: standardize-module-structure - Components重命名为Services
using LYBT.Desktop.MedicalCase.Interfaces;
// [已移除] using LYBT.Desktop.MedicalCase.Models; - ConsultationStep枚举已删除
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LYBT.Desktop.MedicalCase.Tests.Components
{
    /// <summary>
    /// MedicalCaseValidator单元测试 - Issue #1778
    /// </summary>
    public class MedicalCaseValidatorTests
    {
        private readonly Mock<IValidationService> _mockValidationService;
        private readonly Mock<MedicalCaseAggregateService> _mockDataManager;
        private readonly Mock<ILogger<MedicalCaseValidator>> _mockLogger;
        private readonly MedicalCaseValidator _sut;

        public MedicalCaseValidatorTests()
        {
            _mockValidationService = new Mock<IValidationService>();
            _mockDataManager = new Mock<MedicalCaseAggregateService>(
                MockBehavior.Loose,
                Mock.Of<IMedicalCaseRepository>(),
                Mock.Of<IMedicalCaseApi>(),
                Mock.Of<ILogger<MedicalCaseAggregateService>>());
            _mockLogger = new Mock<ILogger<MedicalCaseValidator>>();

            _sut = new MedicalCaseValidator(
                _mockValidationService.Object,
                _mockDataManager.Object,
                _mockLogger.Object);
        }

        #region ValidateAsync Tests

        [Fact]
        public async Task ValidateAsync_ShouldReturnValid_WhenAllDataValid()
        {
            // Arrange
            var medicalCase = CreateValidMedicalCaseDto();
            var consultation = CreateValidConsultationDetailDto();
            var prescription = CreateValidPrescriptionDetailDto();

            _mockDataManager.Setup(x => x.Current).Returns(medicalCase);
            _mockDataManager.Setup(x => x.CurrentConsultation).Returns(consultation);
            _mockDataManager.Setup(x => x.CurrentPrescription).Returns(prescription);

            _mockValidationService.Setup(x => x.ValidateAsync(It.IsAny<MedicalCaseInputDto>()))
                .ReturnsAsync(new ValidationResult());
            _mockValidationService.Setup(x => x.ValidateAsync(It.IsAny<ConsultationInputDto>()))
                .ReturnsAsync(new ValidationResult());
            _mockValidationService.Setup(x => x.ValidateAsync(It.IsAny<PrescriptionInputDto>()))
                .ReturnsAsync(new ValidationResult());

            // Act
            var result = await _sut.ValidateAsync();

            // Assert
            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
        }

        [Fact]
        public async Task ValidateAsync_ShouldReturnInvalid_WhenMedicalCaseIsNull()
        {
            // Arrange
            _mockDataManager.Setup(x => x.Current).Returns((MedicalCaseDetailDto?)null);

            // Act
            var result = await _sut.ValidateAsync();

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.ErrorMessage == "病案数据不能为空");
        }

        [Fact]
        public async Task ValidateAsync_ShouldReturnInvalid_WhenMedicalCaseValidationFails()
        {
            // Arrange
            var medicalCase = CreateValidMedicalCaseDto();
            _mockDataManager.Setup(x => x.Current).Returns(medicalCase);

            // OpenSpec: unify-medicalcase-input-dto - 使用PatientId字段（ChiefComplaint已移至Consultation）
            var validationError = new ValidationFailure("PatientId", "患者ID不能为空");
            _mockValidationService.Setup(x => x.ValidateAsync(It.IsAny<MedicalCaseInputDto>()))
                .ReturnsAsync(new ValidationResult(new[] { validationError }));

            // Act
            var result = await _sut.ValidateAsync();

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "PatientId");
        }

        [Fact]
        public async Task ValidateAsync_ShouldValidateConsultation_WhenConsultationExists()
        {
            // Arrange
            var medicalCase = CreateValidMedicalCaseDto();
            var consultation = CreateValidConsultationDetailDto();

            _mockDataManager.Setup(x => x.Current).Returns(medicalCase);
            _mockDataManager.Setup(x => x.CurrentConsultation).Returns(consultation);

            _mockValidationService.Setup(x => x.ValidateAsync(It.IsAny<MedicalCaseInputDto>()))
                .ReturnsAsync(new ValidationResult());
            _mockValidationService.Setup(x => x.ValidateAsync(It.IsAny<ConsultationInputDto>()))
                .ReturnsAsync(new ValidationResult());

            // Act
            await _sut.ValidateAsync();

            // Assert
            _mockValidationService.Verify(x => x.ValidateAsync(It.IsAny<ConsultationInputDto>()), Times.Once);
        }

        [Fact]
        public async Task ValidateAsync_ShouldValidatePrescription_WhenPrescriptionExists()
        {
            // Arrange
            var medicalCase = CreateValidMedicalCaseDto();
            var prescription = CreateValidPrescriptionDetailDto();

            _mockDataManager.Setup(x => x.Current).Returns(medicalCase);
            _mockDataManager.Setup(x => x.CurrentPrescription).Returns(prescription);

            _mockValidationService.Setup(x => x.ValidateAsync(It.IsAny<MedicalCaseInputDto>()))
                .ReturnsAsync(new ValidationResult());
            _mockValidationService.Setup(x => x.ValidateAsync(It.IsAny<PrescriptionInputDto>()))
                .ReturnsAsync(new ValidationResult());

            // Act
            await _sut.ValidateAsync();

            // Assert
            _mockValidationService.Verify(x => x.ValidateAsync(It.IsAny<PrescriptionInputDto>()), Times.Once);
        }

        #endregion

        #region IsValid Tests

        [Fact]
        public void IsValid_ShouldReturnTrue_WhenAllDataValid()
        {
            // Arrange
            var medicalCase = CreateValidMedicalCaseDto();
            var consultation = CreateValidConsultationDetailDto();

            _mockDataManager.Setup(x => x.Current).Returns(medicalCase);
            _mockDataManager.Setup(x => x.CurrentConsultation).Returns(consultation);

            _mockValidationService.Setup(x => x.IsValid(It.IsAny<MedicalCaseInputDto>(), out It.Ref<string>.IsAny))
                .Returns((object obj, out string error) => { error = string.Empty; return true; });
            _mockValidationService.Setup(x => x.IsValid(It.IsAny<ConsultationInputDto>(), out It.Ref<string>.IsAny))
                .Returns((object obj, out string error) => { error = string.Empty; return true; });

            // Act
            var result = _sut.IsValid(out var errorMessage);

            // Assert
            result.Should().BeTrue();
            errorMessage.Should().BeEmpty();
        }

        [Fact]
        public void IsValid_ShouldReturnFalse_WhenMedicalCaseIsNull()
        {
            // Arrange
            _mockDataManager.Setup(x => x.Current).Returns((MedicalCaseDetailDto?)null);

            // Act
            var result = _sut.IsValid(out var errorMessage);

            // Assert
            result.Should().BeFalse();
            errorMessage.Should().Be("病案数据不能为空");
        }

        #endregion

        #region ValidatePropertyAsync Tests

        [Fact]
        public async Task ValidatePropertyAsync_ShouldReturnPropertyErrors_WhenPropertyInvalid()
        {
            // Arrange
            var medicalCase = CreateValidMedicalCaseDto();
            _mockDataManager.Setup(x => x.Current).Returns(medicalCase);

            // OpenSpec: simplify-medicalcase-dataflow - DoctorId→UserId
            var errors = new[]
            {
                new ValidationFailure("UserId", "医生ID不能为空"),
                new ValidationFailure("PatientId", "患者ID不能为空")
            };

            _mockValidationService.Setup(x => x.ValidateAsync(It.IsAny<MedicalCaseInputDto>()))
                .ReturnsAsync(new ValidationResult(errors));

            // Act
            var result = await _sut.ValidatePropertyAsync("UserId");

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.PropertyName == "UserId");
            result.Errors.Should().NotContain(e => e.PropertyName == "PatientId");
        }

        #endregion

        // [已移除] 三步流程相关测试 (CanCompleteStep1, CanMarkForPrescription, CanCreatePrescription, ValidateStepAsync)
        // 三步流程已取消，相关验证逻辑已移除

        #region Helper Methods

        /// <summary>
        /// 创建有效的MedicalCaseDetailDto测试数据
        /// OpenSpec: simplify-medicalcase-dataflow - DoctorId→UserId
        /// </summary>
        private MedicalCaseDetailDto CreateValidMedicalCaseDto()
        {
            return new MedicalCaseDetailDto
            {
                Id = Guid.NewGuid(),
                CaseNumber = "MC-2025-001",
                Diagnosis = "感冒发热", // ChiefComplaint已移至Consultation
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),  // OpenSpec: DoctorId→UserId
                CaseStatus = (MedicalCaseStatus)CaseStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 创建有效的ConsultationDetailDto测试数据
        /// OpenSpec: unify-medicalcase-input-dto - ChiefComplaint, TreatmentPrinciple已从ConsultationDetailDto移除
        /// </summary>
        private ConsultationDetailDto CreateValidConsultationDetailDto()
        {
            return new ConsultationDetailDto
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                PresentIllness = "现病史记录", // 替代ChiefComplaint
                TcmDiagnosis = "风寒感冒",
                TongueDiagnosis = "舌淡苔白",
                PulseDiagnosis = "脉浮紧",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 创建有效的PrescriptionDetailDto测试数据
        /// OpenSpec: simplify-medicalcase-dataflow - PatientId, UserId, Indication已从PrescriptionDetailDto移除
        /// </summary>
        private PrescriptionDetailDto CreateValidPrescriptionDetailDto()
        {
            return new PrescriptionDetailDto
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                // PatientId, UserId, Indication已移除（Indication打印时从Consultation.TcmDiagnosis获取）
                DosageCount = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        #endregion
    }
}
