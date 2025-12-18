using FluentAssertions;
using LYBT.Desktop.Consultation.Services; // OpenSpec: standardize-module-structure - Components重命名为Services
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;
using Moq;

namespace LYBT.Desktop.Consultation.Tests.Components
{
    /// <summary>
    /// ConsultationDataManager 单元测试
    /// Issue #1779: Consultation模块组件化测试
    /// </summary>
    public class ConsultationDataManagerTests
    {
        private readonly Mock<IMedicalCaseRepository> _mockRepository;
        private readonly Mock<ILogger<ConsultationDataManager>> _mockLogger;
        private readonly ConsultationDataManager _dataManager;

        public ConsultationDataManagerTests()
        {
            _mockRepository = new Mock<IMedicalCaseRepository>();
            _mockLogger = new Mock<ILogger<ConsultationDataManager>>();
            _dataManager = new ConsultationDataManager(
                _mockRepository.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task InitializeAsync_WithValidId_ShouldLoadConsultation()
        {
            // Arrange
            var testId = Guid.NewGuid();
            var mockDetail = new MedicalCaseDetailDto
            {
                Id = testId,
                Consultation = new ConsultationDetailDto
                {
                    // OpenSpec: refactor-diagnosis-fields - ChiefComplaint已移除
                    TCMDiagnosis = "测试诊断"
                }
            };
            _mockRepository.Setup(r => r.GetByIdWithDetailsAsync(testId))
                .ReturnsAsync(mockDetail);

            // Act
            await _dataManager.InitializeAsync(testId);

            // Assert
            _dataManager.Current.Should().NotBeNull();
            _dataManager.Current!.TCMDiagnosis.Should().Be("测试诊断");
        }

        [Fact]
        public async Task SaveAsync_WithChanges_ShouldCallRepository()
        {
            // Arrange
            var testId = Guid.NewGuid();
            var mockDetail = new MedicalCaseDetailDto
            {
                Id = testId,
                Consultation = new ConsultationDetailDto
                {
                    // OpenSpec: refactor-diagnosis-fields - ChiefComplaint已移除
                    TCMDiagnosis = "原始诊断"
                }
            };
            _mockRepository.Setup(r => r.GetByIdWithDetailsAsync(testId))
                .ReturnsAsync(mockDetail);
            await _dataManager.InitializeAsync(testId);

            _dataManager.UpdateField(nameof(ConsultationDetailDto.TCMDiagnosis), "新诊断");

            _mockRepository.Setup(r => r.UpdateConsultationAsync(It.IsAny<Guid>(), It.IsAny<ConsultationInputDto>()))
                .ReturnsAsync(new ConsultationDetailDto());

            // Act
            var result = await _dataManager.SaveAsync();

            // Assert
            result.Should().BeTrue();
            _mockRepository.Verify(r => r.UpdateConsultationAsync(testId, It.IsAny<ConsultationInputDto>()), Times.Once);
        }

        [Fact]
        public async Task HasChanges_AfterFieldUpdate_ShouldReturnTrue()
        {
            // Arrange
            var testId = Guid.NewGuid();
            var mockDetail = new MedicalCaseDetailDto
            {
                Id = testId,
                Consultation = new ConsultationDetailDto
                {
                    // OpenSpec: refactor-diagnosis-fields - ChiefComplaint已移除
                    TCMDiagnosis = "原始诊断"
                }
            };
            _mockRepository.Setup(r => r.GetByIdWithDetailsAsync(testId))
                .ReturnsAsync(mockDetail);
            await _dataManager.InitializeAsync(testId);

            // Act
            _dataManager.UpdateField(nameof(ConsultationDetailDto.TCMDiagnosis), "新诊断");

            // Assert
            _dataManager.HasChanges.Should().BeTrue();
        }
    }
}
