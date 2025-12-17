using FluentAssertions;
using LYBT.Desktop.Consultation.Services; // OpenSpec: standardize-module-structure - Components重命名为Services
using LYBT.Desktop.Infrastructure.Interfaces.Components;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;
using Moq;

namespace LYBT.Desktop.Consultation.Tests.Components
{
    /// <summary>
    /// ConsultationValidator 单元测试
    /// Issue #1779: Consultation模块组件化测试
    /// </summary>
    public class ConsultationValidatorTests
    {
        private readonly Mock<IValidationService> _mockValidationService;
        private readonly Mock<ConsultationDataManager> _mockDataManager;
        private readonly Mock<ILogger<ConsultationValidator>> _mockLogger;
        private readonly ConsultationValidator _validator;

        public ConsultationValidatorTests()
        {
            _mockValidationService = new Mock<IValidationService>();
            _mockDataManager = new Mock<ConsultationDataManager>(
                Mock.Of<LYBT.Desktop.MedicalCase.Interfaces.IMedicalCaseRepository>(),
                Mock.Of<ILogger<ConsultationDataManager>>());
            _mockLogger = new Mock<ILogger<ConsultationValidator>>();

            _validator = new ConsultationValidator(
                _mockValidationService.Object,
                _mockDataManager.Object,
                _mockLogger.Object);
        }

        [Fact]
        public void IsValid_WithValidData_ShouldReturnTrue()
        {
            // Arrange
            // OpenSpec: refactor-diagnosis-fields - 移除ChiefComplaint，TCMDiagnosis是唯一必填字段
            var validConsultation = new ConsultationDto
            {
                TCMDiagnosis = "测试诊断"
            };
            _mockDataManager.Setup(m => m.Current).Returns(validConsultation);
            _mockValidationService.Setup(v => v.IsValid(It.IsAny<ConsultationDto>(), out It.Ref<string>.IsAny))
                .Returns(true);

            // Act
            var result = _validator.IsValid(out var errorMessage);

            // Assert
            result.Should().BeTrue();
            errorMessage.Should().BeEmpty();
        }

        [Fact]
        public void IsValid_WithNullData_ShouldReturnFalse()
        {
            // Arrange
            _mockDataManager.Setup(m => m.Current).Returns((ConsultationDto?)null);

            // Act
            var result = _validator.IsValid(out var errorMessage);

            // Assert
            result.Should().BeFalse();
            errorMessage.Should().NotBeEmpty();
        }

        [Fact]
        public void CanCompleteStep1_WithValidData_ShouldReturnTrue()
        {
            // Arrange
            // OpenSpec: refactor-diagnosis-fields - 移除ChiefComplaint，TCMDiagnosis是唯一必填字段
            var validConsultation = new ConsultationDto
            {
                TCMDiagnosis = "测试诊断"
            };
            _mockDataManager.Setup(m => m.Current).Returns(validConsultation);

            // Act
            var result = _validator.CanCompleteStep1(out var errorMessage);

            // Assert
            result.Should().BeTrue();
            errorMessage.Should().BeEmpty();
        }
    }
}
