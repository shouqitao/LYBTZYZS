using FluentAssertions;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Services; // OpenSpec: standardize-module-structure - Components重命名为Services
using LYBT.Desktop.MedicalCase.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Regions;
using Prism.Services.Dialogs;
using Xunit;

namespace LYBT.Desktop.MedicalCase.Tests.Components
{
    /// <summary>
    /// MedicalCaseNavigationHandler单元测试
    /// OpenSpec: refactor-viewmodel-layer - Phase 5.2.5
    /// </summary>
    public class MedicalCaseNavigationHandlerTests
    {
        private readonly Mock<IRegionManager> _mockRegionManager;
        private readonly Mock<IDialogService> _mockDialogService;
        private readonly Mock<ICommonDialogService> _mockCommonDialogService;
        private readonly Mock<ILogger<MedicalCaseNavigationHandler>> _mockLogger;
        private readonly MedicalCaseNavigationHandler _sut;

        public MedicalCaseNavigationHandlerTests()
        {
            _mockRegionManager = new Mock<IRegionManager>();
            _mockDialogService = new Mock<IDialogService>();
            _mockCommonDialogService = new Mock<ICommonDialogService>();
            _mockLogger = new Mock<ILogger<MedicalCaseNavigationHandler>>();

            _sut = new MedicalCaseNavigationHandler(
                _mockRegionManager.Object,
                _mockDialogService.Object,
                _mockCommonDialogService.Object,
                _mockLogger.Object);
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenRegionManagerIsNull()
        {
            // Act & Assert
            var act = () => new MedicalCaseNavigationHandler(
                null!,
                _mockDialogService.Object,
                _mockCommonDialogService.Object,
                _mockLogger.Object);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("regionManager");
        }

        [Fact]
        public void Constructor_ShouldThrowArgumentNullException_WhenLoggerIsNull()
        {
            // Act & Assert
            var act = () => new MedicalCaseNavigationHandler(
                _mockRegionManager.Object,
                _mockDialogService.Object,
                _mockCommonDialogService.Object,
                null!);
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Fact]
        public void Constructor_ShouldAllowNullDialogService()
        {
            // Act
            var handler = new MedicalCaseNavigationHandler(
                _mockRegionManager.Object,
                null,
                _mockCommonDialogService.Object,
                _mockLogger.Object);

            // Assert
            handler.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_ShouldAllowNullCommonDialogService()
        {
            // Act
            var handler = new MedicalCaseNavigationHandler(
                _mockRegionManager.Object,
                _mockDialogService.Object,
                null,
                _mockLogger.Object);

            // Assert
            handler.Should().NotBeNull();
        }

        #endregion

        #region ExecuteBackAsync - Management Mode Tests

        [Fact]
        public async Task ExecuteBackAsync_ManagementReadOnly_ShouldNavigateDirectly()
        {
            // Act
            await _sut.ExecuteBackAsync(WorkspaceMode.Management, isReadOnly: true);

            // Assert
            _mockRegionManager.Verify(
                x => x.RequestNavigate("ContentRegion", "MedicalCaseManagementView"),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteBackAsync_ManagementEdit_WhenUserConfirmsYes_ShouldSaveAndNavigate()
        {
            // Arrange
            var saveCalled = false;
            var isEditingSet = false;
            _sut.SaveDraftCallback = () => { saveCalled = true; return Task.CompletedTask; };
            _sut.SetIsEditingCallback = value => { isEditingSet = !value; };
            _sut.CheckAndGetAuditReasonCallback = () => Task.FromResult<string?>("");

            _mockDialogService.Setup(x => x.ShowDialog(
                "UnsavedChangesDialog",
                It.IsAny<IDialogParameters>(),
                It.IsAny<Action<IDialogResult>>()))
                .Callback<string, IDialogParameters, Action<IDialogResult>>((name, param, callback) =>
                {
                    var result = new Mock<IDialogResult>();
                    result.Setup(r => r.Result).Returns(ButtonResult.Yes);
                    callback(result.Object);
                });

            // Act
            await _sut.ExecuteBackAsync(WorkspaceMode.Management, isReadOnly: false);

            // Assert
            saveCalled.Should().BeTrue();
            isEditingSet.Should().BeTrue();
            _mockRegionManager.Verify(
                x => x.RequestNavigate("ContentRegion", "MedicalCaseManagementView"),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteBackAsync_ManagementEdit_WhenUserConfirmsNo_ShouldNavigateWithoutSave()
        {
            // Arrange
            var saveCalled = false;
            _sut.SaveDraftCallback = () => { saveCalled = true; return Task.CompletedTask; };

            _mockDialogService.Setup(x => x.ShowDialog(
                "UnsavedChangesDialog",
                It.IsAny<IDialogParameters>(),
                It.IsAny<Action<IDialogResult>>()))
                .Callback<string, IDialogParameters, Action<IDialogResult>>((name, param, callback) =>
                {
                    var result = new Mock<IDialogResult>();
                    result.Setup(r => r.Result).Returns(ButtonResult.No);
                    callback(result.Object);
                });

            // Act
            await _sut.ExecuteBackAsync(WorkspaceMode.Management, isReadOnly: false);

            // Assert
            saveCalled.Should().BeFalse();
            _mockRegionManager.Verify(
                x => x.RequestNavigate("ContentRegion", "MedicalCaseManagementView"),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteBackAsync_ManagementEdit_WhenUserCancels_ShouldNotNavigate()
        {
            // Arrange
            _mockDialogService.Setup(x => x.ShowDialog(
                "UnsavedChangesDialog",
                It.IsAny<IDialogParameters>(),
                It.IsAny<Action<IDialogResult>>()))
                .Callback<string, IDialogParameters, Action<IDialogResult>>((name, param, callback) =>
                {
                    var result = new Mock<IDialogResult>();
                    result.Setup(r => r.Result).Returns(ButtonResult.Cancel);
                    callback(result.Object);
                });

            // Act
            await _sut.ExecuteBackAsync(WorkspaceMode.Management, isReadOnly: false);

            // Assert
            _mockRegionManager.Verify(
                x => x.RequestNavigate(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        #endregion

        #region ExecuteBackAsync - Clinical Mode Tests

        [Fact]
        public async Task ExecuteBackAsync_Clinical_WhenUserSelectsSaveDraft_ShouldSaveAndNavigate()
        {
            // Arrange
            var saveCalled = false;
            _sut.SaveDraftCallback = () => { saveCalled = true; return Task.CompletedTask; };

            _mockCommonDialogService.Setup(x => x.ShowTripleChoiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ReturnsAsync(TripleChoiceResult.Yes);

            // Act
            await _sut.ExecuteBackAsync(WorkspaceMode.Clinical, isReadOnly: false);

            // Assert
            saveCalled.Should().BeTrue();
            _mockRegionManager.Verify(
                x => x.RequestNavigate("ContentRegion", "PatientSelectionView"),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteBackAsync_Clinical_WhenUserSelectsCancelCase_ShouldCancelAndNavigate()
        {
            // Arrange
            var cancelCalled = false;
            _sut.CancelCaseCallback = () => { cancelCalled = true; return Task.CompletedTask; };

            _mockCommonDialogService.Setup(x => x.ShowTripleChoiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ReturnsAsync(TripleChoiceResult.No);

            // Act
            await _sut.ExecuteBackAsync(WorkspaceMode.Clinical, isReadOnly: false);

            // Assert
            cancelCalled.Should().BeTrue();
            _mockRegionManager.Verify(
                x => x.RequestNavigate("ContentRegion", "PatientSelectionView"),
                Times.Once);
        }

        [Fact]
        public async Task ExecuteBackAsync_Clinical_WhenUserSelectsStay_ShouldNotNavigate()
        {
            // Arrange
            _mockCommonDialogService.Setup(x => x.ShowTripleChoiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ReturnsAsync(TripleChoiceResult.Cancel);

            // Act
            await _sut.ExecuteBackAsync(WorkspaceMode.Clinical, isReadOnly: false);

            // Assert
            _mockRegionManager.Verify(
                x => x.RequestNavigate(It.IsAny<string>(), It.IsAny<string>()),
                Times.Never);
        }

        #endregion

        #region HandleLeaveRequestAsync Tests

        [Fact]
        public async Task HandleLeaveRequestAsync_WhenSaveDraft_ShouldReturnAllowLeaveWithSaveDraftChoice()
        {
            // Arrange
            _mockCommonDialogService.Setup(x => x.ShowTripleChoiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ReturnsAsync(TripleChoiceResult.Yes);

            // Act
            var result = await _sut.HandleLeaveRequestAsync();

            // Assert
            result.CanLeave.Should().BeTrue();
            result.Choice.Should().Be(LeaveConsultationChoice.SaveDraft);
        }

        [Fact]
        public async Task HandleLeaveRequestAsync_WhenCancelCase_ShouldReturnAllowLeaveWithCancelCaseChoice()
        {
            // Arrange
            _mockCommonDialogService.Setup(x => x.ShowTripleChoiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ReturnsAsync(TripleChoiceResult.No);

            // Act
            var result = await _sut.HandleLeaveRequestAsync();

            // Assert
            result.CanLeave.Should().BeTrue();
            result.Choice.Should().Be(LeaveConsultationChoice.CancelCase);
        }

        [Fact]
        public async Task HandleLeaveRequestAsync_WhenStay_ShouldReturnCancelLeave()
        {
            // Arrange
            _mockCommonDialogService.Setup(x => x.ShowTripleChoiceAsync(
                It.IsAny<string>(),
                It.IsAny<string>()))
                .ReturnsAsync(TripleChoiceResult.Cancel);

            // Act
            var result = await _sut.HandleLeaveRequestAsync();

            // Assert
            result.CanLeave.Should().BeFalse();
            result.Choice.Should().Be(LeaveConsultationChoice.Stay);
        }

        [Fact]
        public async Task HandleLeaveRequestAsync_WhenCommonDialogServiceIsNull_ShouldReturnStay()
        {
            // Arrange
            var handler = new MedicalCaseNavigationHandler(
                _mockRegionManager.Object,
                _mockDialogService.Object,
                null,
                _mockLogger.Object);

            // Act
            var result = await handler.HandleLeaveRequestAsync();

            // Assert
            result.CanLeave.Should().BeFalse();
            result.Choice.Should().Be(LeaveConsultationChoice.Stay);
        }

        #endregion

        #region HandleManagementLeaveRequestAsync Tests

        [Fact]
        public async Task HandleManagementLeaveRequestAsync_WhenDialogServiceIsNull_ShouldReturnFalse()
        {
            // Arrange
            var handler = new MedicalCaseNavigationHandler(
                _mockRegionManager.Object,
                null,
                _mockCommonDialogService.Object,
                _mockLogger.Object);

            // Act
            var result = await handler.HandleManagementLeaveRequestAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task HandleManagementLeaveRequestAsync_WhenUserCancelsAudit_ShouldReturnFalse()
        {
            // Arrange
            _sut.CheckAndGetAuditReasonCallback = () => Task.FromResult<string?>(null);

            _mockDialogService.Setup(x => x.ShowDialog(
                "UnsavedChangesDialog",
                It.IsAny<IDialogParameters>(),
                It.IsAny<Action<IDialogResult>>()))
                .Callback<string, IDialogParameters, Action<IDialogResult>>((name, param, callback) =>
                {
                    var result = new Mock<IDialogResult>();
                    result.Setup(r => r.Result).Returns(ButtonResult.Yes);
                    callback(result.Object);
                });

            // Act
            var result = await _sut.HandleManagementLeaveRequestAsync();

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task HandleManagementLeaveRequestAsync_WhenAuditReasonProvided_ShouldSetEditReason()
        {
            // Arrange
            string? capturedReason = null;
            _sut.CheckAndGetAuditReasonCallback = () => Task.FromResult<string?>("测试审计原因");
            _sut.SetEditReasonCallback = reason => capturedReason = reason;
            _sut.SaveDraftCallback = () => Task.CompletedTask;
            _sut.SetIsEditingCallback = _ => { };

            _mockDialogService.Setup(x => x.ShowDialog(
                "UnsavedChangesDialog",
                It.IsAny<IDialogParameters>(),
                It.IsAny<Action<IDialogResult>>()))
                .Callback<string, IDialogParameters, Action<IDialogResult>>((name, param, callback) =>
                {
                    var result = new Mock<IDialogResult>();
                    result.Setup(r => r.Result).Returns(ButtonResult.Yes);
                    callback(result.Object);
                });

            // Act
            await _sut.HandleManagementLeaveRequestAsync();

            // Assert
            capturedReason.Should().Be("测试审计原因");
        }

        #endregion

        #region Callback Tests

        [Fact]
        public void Callbacks_ShouldBeSettableAndNullable()
        {
            // Assert initial state
            _sut.SaveDraftCallback.Should().BeNull();
            _sut.CancelCaseCallback.Should().BeNull();
            _sut.CheckAndGetAuditReasonCallback.Should().BeNull();
            _sut.SetEditReasonCallback.Should().BeNull();
            _sut.SetIsEditingCallback.Should().BeNull();

            // Act
            _sut.SaveDraftCallback = () => Task.CompletedTask;
            _sut.CancelCaseCallback = () => Task.CompletedTask;

            // Assert
            _sut.SaveDraftCallback.Should().NotBeNull();
            _sut.CancelCaseCallback.Should().NotBeNull();
        }

        #endregion
    }
}
