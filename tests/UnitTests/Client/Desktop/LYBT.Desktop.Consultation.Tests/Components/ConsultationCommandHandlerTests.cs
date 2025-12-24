using FluentAssertions;
using LYBT.Desktop.Consultation.Services; // OpenSpec: standardize-module-structure - Components重命名为Services
using LYBT.Desktop.Infrastructure.Interfaces.Components;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Regions;

namespace LYBT.Desktop.Consultation.Tests.Components
{
    /// <summary>
    /// ConsultationCommandHandler 单元测试
    /// Issue #1779: Consultation模块组件化测试
    /// OpenSpec: simplify-medicalcase-api - 使用IMedicalCaseDataManager
    /// </summary>
    public class ConsultationCommandHandlerTests
    {
        private readonly Mock<IMedicalCaseAggregateService> _mockDataManager;
        private readonly Mock<ConsultationValidator> _mockValidator;
        private readonly Mock<ILogger<ConsultationCommandHandler>> _mockLogger;
        private readonly Mock<IRegionManager> _mockRegionManager;
        private readonly ConsultationCommandHandler _commandHandler;

        public ConsultationCommandHandlerTests()
        {
            _mockDataManager = new Mock<IMedicalCaseAggregateService>();

            _mockValidator = new Mock<ConsultationValidator>(
                Mock.Of<IValidationService>(),
                _mockDataManager.Object,
                Mock.Of<ILogger<ConsultationValidator>>());

            _mockLogger = new Mock<ILogger<ConsultationCommandHandler>>();
            _mockRegionManager = new Mock<IRegionManager>();

            _commandHandler = new ConsultationCommandHandler(
                _mockDataManager.Object,
                _mockValidator.Object,
                _mockLogger.Object,
                _mockRegionManager.Object);
        }

        [Fact]
        public async Task SaveAsync_WithValidData_ShouldReturnTrue()
        {
            // Arrange
            _mockValidator.Setup(v => v.IsValid(out It.Ref<string>.IsAny)).Returns(true);
            _mockDataManager.Setup(m => m.SaveAsync()).ReturnsAsync(true);

            // Act
            var result = await _commandHandler.SaveAsync(validate: true);

            // Assert
            result.Should().BeTrue();
            _mockValidator.Verify(v => v.IsValid(out It.Ref<string>.IsAny), Times.Once);
            _mockDataManager.Verify(m => m.SaveAsync(), Times.Once);
        }

        [Fact]
        public void ClearForm_ShouldClearAllFields()
        {
            // Arrange
            // OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
            // OpenSpec: simplify-medicalcase-api - 使用CurrentConsultation属性
            var mockConsultation = new ConsultationDetailDto
            {
                PresentIllness = "测试现病史",
                TongueDiagnosis = "测试舌诊",
                PulseDiagnosis = "测试脉诊",
                TCMDiagnosis = "测试诊断"
            };
            _mockDataManager.Setup(m => m.CurrentConsultation).Returns(mockConsultation);

            // Act
            _commandHandler.ClearForm();

            // Assert - 验证4个核心字段被清空（直接修改属性而非调用UpdateField）
            mockConsultation.PresentIllness.Should().BeNull();
            mockConsultation.TongueDiagnosis.Should().BeNull();
            mockConsultation.PulseDiagnosis.Should().BeNull();
            mockConsultation.TCMDiagnosis.Should().BeEmpty();
        }
    }
}
