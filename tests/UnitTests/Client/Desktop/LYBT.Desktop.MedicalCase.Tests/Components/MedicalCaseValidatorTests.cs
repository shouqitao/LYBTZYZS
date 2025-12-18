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
        private readonly Mock<MedicalCaseDataManager> _mockDataManager;
        private readonly Mock<ILogger<MedicalCaseValidator>> _mockLogger;
        private readonly MedicalCaseValidator _sut;

        public MedicalCaseValidatorTests()
        {
            _mockValidationService = new Mock<IValidationService>();
            _mockDataManager = new Mock<MedicalCaseDataManager>(
                MockBehavior.Loose,
                Mock.Of<IMedicalCaseRepository>(),
                Mock.Of<IMedicalCaseApi>(),
                Mock.Of<ILogger<MedicalCaseDataManager>>());
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
            _mockValidationService.Setup(x => x.ValidateAsync(It.IsAny<PrescriptionUpdateDto>()))
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

            var validationError = new ValidationFailure("ChiefComplaint", "主诉不能为空");
            _mockValidationService.Setup(x => x.ValidateAsync(It.IsAny<MedicalCaseInputDto>()))
                .ReturnsAsync(new ValidationResult(new[] { validationError }));

            // Act
            var result = await _sut.ValidateAsync();

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "ChiefComplaint");
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
            _mockValidationService.Setup(x => x.ValidateAsync(It.IsAny<PrescriptionUpdateDto>()))
                .ReturnsAsync(new ValidationResult());

            // Act
            await _sut.ValidateAsync();

            // Assert
            _mockValidationService.Verify(x => x.ValidateAsync(It.IsAny<PrescriptionUpdateDto>()), Times.Once);
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

            var errors = new[]
            {
                new ValidationFailure("ChiefComplaint", "主诉不能为空"),
                new ValidationFailure("PatientId", "患者ID不能为空")
            };

            _mockValidationService.Setup(x => x.ValidateAsync(It.IsAny<MedicalCaseInputDto>()))
                .ReturnsAsync(new ValidationResult(errors));

            // Act
            var result = await _sut.ValidatePropertyAsync("ChiefComplaint");

            // Assert
            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.PropertyName == "ChiefComplaint");
            result.Errors.Should().NotContain(e => e.PropertyName == "PatientId");
        }

        #endregion

        // [已移除] 三步流程相关测试 (CanCompleteStep1, CanMarkForPrescription, CanCreatePrescription, ValidateStepAsync)
        // 三步流程已取消，相关验证逻辑已移除

        #region Helper Methods

        private MedicalCaseDetailDto CreateValidMedicalCaseDto()
        {
            return new MedicalCaseDetailDto
            {
                Id = Guid.NewGuid(),
                CaseNumber = "MC-2025-001",
                ChiefComplaint = "感冒发热",
                PatientId = Guid.NewGuid(),
                DoctorId = Guid.NewGuid(),
                CaseStatus = (MedicalCaseStatus)CaseStatus.Active,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private ConsultationDetailDto CreateValidConsultationDetailDto()
        {
            return new ConsultationDetailDto
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                ChiefComplaint = "感冒发热",
                TCMDiagnosis = "风寒感冒",
                TreatmentPrinciple = "辛温解表",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        private PrescriptionDetailDto CreateValidPrescriptionDetailDto()
        {
            return new PrescriptionDetailDto
            {
                Id = Guid.NewGuid(),
                MedicalCaseId = Guid.NewGuid(),
                PatientId = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                Indication = "风寒感冒",
                DosageCount = 3,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        #endregion
    }
}
