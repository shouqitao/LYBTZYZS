using FluentAssertions;
using LYBT.Desktop.MedicalCase.Components;
// [已移除] using LYBT.Desktop.MedicalCase.Models; - ConsultationStep枚举已删除
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;
using Xunit;

namespace LYBT.Desktop.MedicalCase.Tests.Components
{
    /// <summary>
    /// MedicalCaseEventCoordinator单元测试
    /// OpenSpec: refactor-viewmodel-layer - Task 2.4.4
    /// </summary>
    public class MedicalCaseEventCoordinatorTests
    {
        private readonly Mock<IEventAggregator> _mockEventAggregator;
        private readonly Mock<ILogger<MedicalCaseEventCoordinator>> _mockLogger;
        private readonly MedicalCaseEventCoordinator _sut;

        public MedicalCaseEventCoordinatorTests()
        {
            _mockEventAggregator = new Mock<IEventAggregator>();
            _mockLogger = new Mock<ILogger<MedicalCaseEventCoordinator>>();

            _sut = new MedicalCaseEventCoordinator(
                _mockEventAggregator.Object,
                _mockLogger.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenEventAggregatorIsNull()
        {
            // Act & Assert
            var act = () => new MedicalCaseEventCoordinator(null!, _mockLogger.Object);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("eventAggregator");
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
        {
            // Act & Assert
            var act = () => new MedicalCaseEventCoordinator(_mockEventAggregator.Object, null!);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void Constructor_ShouldCreateInstance_WhenDependenciesProvided()
        {
            // Assert
            _sut.Should().NotBeNull();
        }

        #endregion

        #region PublishMedicalCaseSaved Tests

        [Fact]
        public void PublishMedicalCaseSaved_ShouldNotThrow_WhenCalled()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            // Act
            var act = () => _sut.PublishMedicalCaseSaved(medicalCaseId);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void PublishMedicalCaseSaved_ShouldLogInformation()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            // Act
            _sut.PublishMedicalCaseSaved(medicalCaseId);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MedicalCaseSavedEvent")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region PublishMedicalCaseCompleted Tests

        [Fact]
        public void PublishMedicalCaseCompleted_ShouldNotThrow_WhenCalled()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            // Act
            var act = () => _sut.PublishMedicalCaseCompleted(medicalCaseId);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void PublishMedicalCaseCompleted_ShouldLogInformation()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();

            // Act
            _sut.PublishMedicalCaseCompleted(medicalCaseId);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("MedicalCaseCompletedEvent")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        // [已移除] PublishConsultationStepChanged Tests - 三步流程已取消

        #region PublishPrescriptionCreated Tests

        [Fact]
        public void PublishPrescriptionCreated_ShouldNotThrow_WhenCalled()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var prescriptionId = Guid.NewGuid();

            // Act
            var act = () => _sut.PublishPrescriptionCreated(medicalCaseId, prescriptionId);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void PublishPrescriptionCreated_ShouldLogInformation()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var prescriptionId = Guid.NewGuid();

            // Act
            _sut.PublishPrescriptionCreated(medicalCaseId, prescriptionId);

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("PrescriptionCreatedEvent")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Subscribe Tests

        [Fact]
        public void SubscribeToPatientSelected_ShouldNotThrow_WhenCalled()
        {
            // Arrange
            Action<Guid> action = _ => { };

            // Act
            var act = () => _sut.SubscribeToPatientSelected(action);

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void SubscribeToMedicalCaseSaved_ShouldNotThrow_WhenCalled()
        {
            // Arrange
            Action<Guid> action = _ => { };

            // Act
            var act = () => _sut.SubscribeToMedicalCaseSaved(action);

            // Assert
            act.Should().NotThrow();
        }

        // [已移除] SubscribeToConsultationStepChanged Test - 三步流程已取消

        #endregion

        #region UnsubscribeAll Tests

        [Fact]
        public void UnsubscribeAll_ShouldNotThrow_WhenNoSubscriptions()
        {
            // Act
            var act = () => _sut.UnsubscribeAll();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void UnsubscribeAll_ShouldLogDebug()
        {
            // Act
            _sut.UnsubscribeAll();

            // Assert
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("取消所有事件订阅")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_ShouldNotThrow_WhenCalled()
        {
            // Act
            var act = () => _sut.Dispose();

            // Assert
            act.Should().NotThrow();
        }

        [Fact]
        public void Dispose_ShouldCallUnsubscribeAll()
        {
            // Act
            _sut.Dispose();

            // Assert - UnsubscribeAll logs debug message
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("取消所有事件订阅")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public void Dispose_ShouldBeIdempotent()
        {
            // Act
            _sut.Dispose();
            _sut.Dispose();

            // Assert - UnsubscribeAll should only be called once
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Debug,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("取消所有事件订阅")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        #endregion
    }
}
