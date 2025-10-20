using FluentAssertions;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.MedicalCase.ViewModels;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;
using Prism.Regions;
using Xunit;
using CommonStatus = LYBT.Shared.Models.Enums.CommonStatus;

namespace LYBT.Desktop.MedicalCase.Tests.ViewModels
{
    /// <summary>
    /// CompletionViewModel单元测试 - Task #1500
    /// </summary>
    public class CompletionViewModelTests
    {
        private readonly Mock<IMedicalCaseRepository> _mockMedicalCaseRepository;
        private readonly Mock<IRegionManager> _mockRegionManager;
        private readonly Mock<ICommonDialogService> _mockDialogService;
        private readonly Mock<IEventAggregator> _mockEventAggregator;
        private readonly Mock<ILoggerFactory> _mockLoggerFactory;
        private readonly Mock<ILogger<CompletionViewModel>> _mockLogger;

        public CompletionViewModelTests()
        {
            _mockMedicalCaseRepository = new Mock<IMedicalCaseRepository>();
            _mockRegionManager = new Mock<IRegionManager>();
            _mockDialogService = new Mock<ICommonDialogService>();
            _mockEventAggregator = new Mock<IEventAggregator>();
            _mockLoggerFactory = new Mock<ILoggerFactory>();
            _mockLogger = new Mock<ILogger<CompletionViewModel>>();
            _mockLoggerFactory.Setup(x => x.CreateLogger(It.IsAny<string>())).Returns(_mockLogger.Object);
        }

        private CompletionViewModel CreateViewModel()
        {
            return new CompletionViewModel(
                _mockMedicalCaseRepository.Object,
                _mockRegionManager.Object,
                _mockDialogService.Object,
                _mockEventAggregator.Object,
                _mockLoggerFactory.Object);
        }

        #region InitializeAsync Tests

        [Fact]
        public async Task InitializeAsync_WhenMedicalCaseIdIsValid_ShouldUpdateStatusToClosed()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            var existingCase = new MedicalCaseDto
            {
                Id = medicalCaseId,
                CaseNumber = "MC20251019001",
                CaseStatus = MedicalCaseStatus.Active,
                Status = CommonStatus.Enabled
            };
            var updatedCase = new MedicalCaseDto
            {
                Id = medicalCaseId,
                CaseNumber = "MC20251019001",
                CaseStatus = MedicalCaseStatus.Closed,
                Status = CommonStatus.Disabled
            };

            _mockMedicalCaseRepository.Setup(x => x.GetByIdAsync(medicalCaseId))
                .ReturnsAsync(existingCase);
            _mockMedicalCaseRepository.Setup(x => x.UpdateAsync(It.IsAny<MedicalCaseUpdateDto>()))
                .ReturnsAsync(updatedCase);

            var viewModel = CreateViewModel();

            // Act
            await viewModel.InitializeAsync(medicalCaseId);

            // Assert
            _mockMedicalCaseRepository.Verify(x => x.GetByIdAsync(medicalCaseId), Times.Once);
            _mockMedicalCaseRepository.Verify(x => x.UpdateAsync(
                It.Is<MedicalCaseUpdateDto>(dto =>
                    dto.Id == medicalCaseId &&
                    dto.Status == MedicalCaseStatus.Closed.ToString())), Times.Once);
            viewModel.MedicalCaseNumber.Should().Be("MC20251019001");
        }

        [Fact]
        public async Task InitializeAsync_WhenMedicalCaseIdIsEmpty_ShouldNotUpdateStatus()
        {
            // Arrange
            _mockDialogService.Setup(x => x.ShowErrorAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var viewModel = CreateViewModel();

            // Act
            await viewModel.InitializeAsync(Guid.Empty);

            // Assert
            _mockMedicalCaseRepository.Verify(x => x.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
            _mockMedicalCaseRepository.Verify(x => x.UpdateAsync(It.IsAny<MedicalCaseUpdateDto>()), Times.Never);
        }

        [Fact]
        public async Task InitializeAsync_WhenMedicalCaseNotFound_ShouldShowError()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            _mockMedicalCaseRepository.Setup(x => x.GetByIdAsync(medicalCaseId))
                .ReturnsAsync((MedicalCaseDto?)null);
            _mockDialogService.Setup(x => x.ShowErrorAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            var viewModel = CreateViewModel();

            // Act
            await viewModel.InitializeAsync(medicalCaseId);

            // Assert
            _mockMedicalCaseRepository.Verify(x => x.UpdateAsync(It.IsAny<MedicalCaseUpdateDto>()), Times.Never);
        }

        [Fact]
        public async Task InitializeAsync_WhenRepositoryThrows_ShouldHandleException()
        {
            // Arrange
            var medicalCaseId = Guid.NewGuid();
            _mockMedicalCaseRepository.Setup(x => x.GetByIdAsync(medicalCaseId))
                .ThrowsAsync(new Exception("Database error"));

            var viewModel = CreateViewModel();

            // Act & Assert - 不应崩溃
            await viewModel.InitializeAsync(medicalCaseId);

            // 验证日志记录
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.AtLeastOnce);
        }

        #endregion

        #region ContinueConsultationCommand Tests

        [Fact]
        public void ContinueConsultationCommand_WhenExecuted_ShouldNavigateToMedicalCaseFlowViewWithStep1()
        {
            // Arrange
            var viewModel = CreateViewModel();
            NavigationParameters? capturedParameters = null;
            _mockRegionManager.Setup(x => x.RequestNavigate(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NavigationParameters>()))
                .Callback<string, string, NavigationParameters>((region, target, parameters) =>
                {
                    capturedParameters = parameters;
                });

            // Act
            viewModel.ContinueConsultationCommand.Execute();

            // Assert
            _mockRegionManager.Verify(x => x.RequestNavigate(
                "ContentRegion",
                "MedicalCaseFlowView",
                It.IsAny<NavigationParameters>()), Times.Once);
            capturedParameters.Should().NotBeNull();
            capturedParameters!.GetValue<int>("StartStep").Should().Be(1);
        }

        [Fact]
        public void ContinueConsultationCommand_WhenNavigationFails_ShouldHandleException()
        {
            // Arrange
            var viewModel = CreateViewModel();
            _mockRegionManager.Setup(x => x.RequestNavigate(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NavigationParameters>()))
                .Throws(new Exception("Navigation error"));

            // Act & Assert - 不应崩溃
            viewModel.ContinueConsultationCommand.Execute();
        }

        #endregion

        #region ReturnHomeCommand Tests

        [Fact]
        public void ReturnHomeCommand_WhenExecuted_ShouldNavigateToHomeView()
        {
            // Arrange
            var viewModel = CreateViewModel();

            // Act
            viewModel.ReturnHomeCommand.Execute();

            // Assert
            _mockRegionManager.Verify(x => x.RequestNavigate(
                "ContentRegion",
                "HomeView"), Times.Once);
        }

        [Fact]
        public void ReturnHomeCommand_WhenNavigationFails_ShouldHandleException()
        {
            // Arrange
            var viewModel = CreateViewModel();
            _mockRegionManager.Setup(x => x.RequestNavigate(
                It.IsAny<string>(),
                It.IsAny<string>()))
                .Throws(new Exception("Navigation error"));

            // Act & Assert - 不应崩溃
            viewModel.ReturnHomeCommand.Execute();
        }

        #endregion

        #region PrintPrescriptionCommand Tests

        [Fact]
        public async Task PrintPrescriptionCommand_WhenExecuted_ShouldShowInfoDialog()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.MedicalCaseId = Guid.NewGuid();
            _mockDialogService.Setup(x => x.ShowInfoAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            viewModel.PrintPrescriptionCommand.Execute();
            await Task.Delay(50); // 等待async void完成

            // Assert
            _mockDialogService.Verify(x => x.ShowInfoAsync(
                It.Is<string>(s => s.Contains("处方打印功能开发中")),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task PrintPrescriptionCommand_WhenExceptionThrown_ShouldHandleException()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.MedicalCaseId = Guid.NewGuid();
            _mockDialogService.Setup(x => x.ShowInfoAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("Print error"));

            // Act & Assert - 不应崩溃
            viewModel.PrintPrescriptionCommand.Execute();
            await Task.Delay(100); // 等待async void完成

            // 验证日志记录
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.AtLeastOnce);
        }

        #endregion

        #region ViewDetailCommand Tests

        [Fact]
        public async Task ViewDetailCommand_WhenExecuted_ShouldShowInfoDialog()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.MedicalCaseId = Guid.NewGuid();
            _mockDialogService.Setup(x => x.ShowInfoAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            // Act
            viewModel.ViewDetailCommand.Execute();
            await Task.Delay(50); // 等待async void完成

            // Assert
            _mockDialogService.Verify(x => x.ShowInfoAsync(
                It.Is<string>(s => s.Contains("病案详情功能开发中")),
                It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public async Task ViewDetailCommand_WhenExceptionThrown_ShouldHandleException()
        {
            // Arrange
            var viewModel = CreateViewModel();
            viewModel.MedicalCaseId = Guid.NewGuid();
            _mockDialogService.Setup(x => x.ShowInfoAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ThrowsAsync(new Exception("View error"));

            // Act & Assert - 不应崩溃
            viewModel.ViewDetailCommand.Execute();
            await Task.Delay(100); // 等待async void完成

            // 验证日志记录
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.AtLeastOnce);
        }

        #endregion

        #region Property Tests

        [Fact]
        public void MedicalCaseId_ShouldGetAndSet()
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testId = Guid.NewGuid();

            // Act
            viewModel.MedicalCaseId = testId;

            // Assert
            viewModel.MedicalCaseId.Should().Be(testId);
        }

        [Fact]
        public void MedicalCaseNumber_ShouldGetAndSet()
        {
            // Arrange
            var viewModel = CreateViewModel();
            var testNumber = "MC20251019001";

            // Act
            viewModel.MedicalCaseNumber = testNumber;

            // Assert
            viewModel.MedicalCaseNumber.Should().Be(testNumber);
        }

        #endregion
    }
}
