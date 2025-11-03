using FluentAssertions;
using LYBT.Desktop.Consultation.Components;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Consultation.Tests.Components
{
    /// <summary>
    /// ConsultationCommandHandler 单元测试
    /// Issue #1779: Consultation模块组件化测试
    /// </summary>
    public class ConsultationCommandHandlerTests
    {
        private readonly Mock<ConsultationDataManager> _mockDataManager;
        private readonly Mock<ConsultationValidator> _mockValidator;
        private readonly Mock<IMedicalCaseRepository> _mockRepository;
        private readonly Mock<ILogger<ConsultationCommandHandler>> _mockLogger;
        private readonly Mock<IRegionManager> _mockRegionManager;
        private readonly Mock<IEventAggregator> _mockEventAggregator;
        private readonly ConsultationCommandHandler _commandHandler;

        public ConsultationCommandHandlerTests()
        {
            _mockDataManager = new Mock<ConsultationDataManager>(
                Mock.Of<IMedicalCaseRepository>(),
                Mock.Of<ILogger<ConsultationDataManager>>());

            _mockValidator = new Mock<ConsultationValidator>(
                Mock.Of<LYBT.Desktop.Infrastructure.Interfaces.Components.IValidationService>(),
                _mockDataManager.Object,
                Mock.Of<ILogger<ConsultationValidator>>());

            _mockRepository = new Mock<IMedicalCaseRepository>();
            _mockLogger = new Mock<ILogger<ConsultationCommandHandler>>();
            _mockRegionManager = new Mock<IRegionManager>();
            _mockEventAggregator = new Mock<IEventAggregator>();

            _commandHandler = new ConsultationCommandHandler(
                _mockDataManager.Object,
                _mockValidator.Object,
                _mockRepository.Object,
                _mockLogger.Object,
                _mockRegionManager.Object,
                _mockEventAggregator.Object);
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
            var mockConsultation = new ConsultationDto
            {
                ChiefComplaint = "测试主诉",
                TCMDiagnosis = "测试诊断"
            };
            _mockDataManager.Setup(m => m.Current).Returns(mockConsultation);

            // Act
            _commandHandler.ClearForm();

            // Assert
            _mockDataManager.Verify(m => m.UpdateField(nameof(ConsultationDto.ChiefComplaint), string.Empty), Times.Once);
            _mockDataManager.Verify(m => m.UpdateField(nameof(ConsultationDto.TCMDiagnosis), string.Empty), Times.Once);
        }

        [Fact]
        public async Task CompleteStep1Async_WithValidData_ShouldReturnTrue()
        {
            // Arrange
            var testId = Guid.NewGuid();
            _mockDataManager.SetupGet(m => m.MedicalCaseId).Returns(testId);
            _mockValidator.Setup(v => v.CanCompleteStep1(out It.Ref<string>.IsAny)).Returns(true);
            _mockRepository.Setup(r => r.CompleteStep1Async(testId, It.IsAny<CompleteStep1Request>()))
                .ReturnsAsync(new ConsultationStepDto { Step1CompletedAt = DateTime.Now });

            // Act
            var result = await _commandHandler.CompleteStep1Async(prescriptionEnabled: true);

            // Assert
            result.Should().BeTrue();
            _mockRepository.Verify(r => r.CompleteStep1Async(testId, It.IsAny<CompleteStep1Request>()), Times.Once);
        }
    }
}
