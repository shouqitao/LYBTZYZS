using FluentAssertions;
using LYBT.Desktop.Contracts.Api; // Issue #2164: 添加Api接口引用
using LYBT.Desktop.Infrastructure.Interfaces.Components;
using LYBT.Desktop.MedicalCase.Services; // OpenSpec: standardize-module-structure - Components重命名为Services
using LYBT.Desktop.MedicalCase.Interfaces;
// [已移除] using LYBT.Desktop.MedicalCase.Models; - ConsultationStep枚举已删除
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;
using Prism.Regions;
using Xunit;

namespace LYBT.Desktop.MedicalCase.Tests.Components
{
    /// <summary>
    /// MedicalCaseCommandHandler单元测试 - Issue #1778
    /// </summary>
    public class MedicalCaseCommandHandlerTests
    {
        private readonly Mock<MedicalCaseDataManager> _mockDataManager;
        private readonly Mock<MedicalCaseValidator> _mockValidator;
        private readonly Mock<ILogger<MedicalCaseCommandHandler>> _mockLogger;
        private readonly Mock<IRegionManager> _mockRegionManager;
        private readonly Mock<IEventAggregator> _mockEventAggregator;
        private readonly MedicalCaseCommandHandler _sut;

        public MedicalCaseCommandHandlerTests()
        {
            // Issue #2164: 添加IMedicalCaseApi参数
            _mockDataManager = new Mock<MedicalCaseDataManager>(
                MockBehavior.Loose,
                Mock.Of<IMedicalCaseRepository>(),
                Mock.Of<IMedicalCaseApi>(),
                Mock.Of<ILogger<MedicalCaseDataManager>>());

            _mockValidator = new Mock<MedicalCaseValidator>(
                MockBehavior.Loose,
                Mock.Of<IValidationService>(),
                _mockDataManager.Object,
                Mock.Of<ILogger<MedicalCaseValidator>>());

            _mockLogger = new Mock<ILogger<MedicalCaseCommandHandler>>();
            _mockRegionManager = new Mock<IRegionManager>();
            _mockEventAggregator = new Mock<IEventAggregator>();

            // Issue #2164: 构造函数只需要4个参数（移除IEventAggregator）
            _sut = new MedicalCaseCommandHandler(
                _mockDataManager.Object,
                _mockValidator.Object,
                _mockLogger.Object,
                _mockRegionManager.Object);
        }

        #region SaveAsync Tests

        [Fact]
        public async Task SaveAsync_ShouldSaveWithValidation_WhenValidateBeforeSaveTrue()
        {
            // Arrange
            _mockValidator.Setup(x => x.IsValid(out It.Ref<string>.IsAny))
                .Returns((out string error) => { error = string.Empty; return true; });
            _mockDataManager.Setup(x => x.SaveAsync()).ReturnsAsync(true);

            // Act
            var result = await _sut.SaveAsync(validateBeforeSave: true);

            // Assert
            result.Should().BeTrue();
            _mockValidator.Verify(x => x.IsValid(out It.Ref<string>.IsAny), Times.Once);
            _mockDataManager.Verify(x => x.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task SaveAsync_ShouldSkipValidation_WhenValidateBeforeSaveFalse()
        {
            // Arrange
            _mockDataManager.Setup(x => x.SaveAsync()).ReturnsAsync(true);

            // Act
            var result = await _sut.SaveAsync(validateBeforeSave: false);

            // Assert
            result.Should().BeTrue();
            _mockValidator.Verify(x => x.IsValid(out It.Ref<string>.IsAny), Times.Never);
            _mockDataManager.Verify(x => x.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task SaveAsync_ShouldReturnFalse_WhenValidationFails()
        {
            // Arrange
            _mockValidator.Setup(x => x.IsValid(out It.Ref<string>.IsAny))
                .Returns((out string error) => { error = "验证失败"; return false; });

            // Act
            var result = await _sut.SaveAsync(validateBeforeSave: true);

            // Assert
            result.Should().BeFalse();
            _mockDataManager.Verify(x => x.SaveAsync(), Times.Never);
        }

        [Fact]
        public async Task SaveAsync_ShouldReturnFalse_WhenDataManagerSaveFails()
        {
            // Arrange
            _mockValidator.Setup(x => x.IsValid(out It.Ref<string>.IsAny))
                .Returns((out string error) => { error = string.Empty; return true; });
            _mockDataManager.Setup(x => x.SaveAsync()).ReturnsAsync(false);

            // Act
            var result = await _sut.SaveAsync(validateBeforeSave: true);

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_ShouldCallDataManager_WhenInvoked()
        {
            // Arrange
            _mockDataManager.Setup(x => x.DeleteAsync()).ReturnsAsync(true);

            // Act
            var result = await _sut.DeleteAsync();

            // Assert
            result.Should().BeTrue();
            _mockDataManager.Verify(x => x.DeleteAsync(), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_ShouldReturnFalse_WhenDataManagerDeleteFails()
        {
            // Arrange
            _mockDataManager.Setup(x => x.DeleteAsync()).ReturnsAsync(false);

            // Act
            var result = await _sut.DeleteAsync();

            // Assert
            result.Should().BeFalse();
        }

        #endregion

        #region ReloadAsync Tests

        [Fact]
        public async Task ReloadAsync_ShouldCallDataManager_WhenInvoked()
        {
            // Arrange
            _mockDataManager.Setup(x => x.ReloadAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _sut.ReloadAsync();

            // Assert
            result.Should().BeTrue();
            _mockDataManager.Verify(x => x.ReloadAsync(), Times.Once);
        }

        #endregion

        // [已移除] Workflow Validation Tests (CanCompleteStep1, CanMarkForPrescription, CanCreatePrescription, ValidateStepAsync)
        // 三步流程已取消，相关验证逻辑已移除

        #region Prescription Management Tests

        [Fact]
        public async Task CreatePrescriptionAsync_ShouldCreateAndReturnTrue_WhenSuccessful()
        {
            // Arrange
            // OpenSpec: unify-medicalcase-input-dto - PrescriptionInputDto仅需MedicalCaseId
            var createDto = new PrescriptionInputDto
            {
                MedicalCaseId = Guid.NewGuid(),
                Diagnosis = "风寒感冒",
                DosageCount = 3
            };

            var createdPrescription = new PrescriptionDetailDto
            {
                Id = Guid.NewGuid(),
                Indication = createDto.Diagnosis,
                DosageCount = createDto.DosageCount
            };

            _mockDataManager.Setup(x => x.CreatePrescriptionAsync(createDto))
                .ReturnsAsync(createdPrescription);

            // Act
            var result = await _sut.CreatePrescriptionAsync(createDto);

            // Assert
            result.Should().BeTrue();
            _mockDataManager.Verify(x => x.CreatePrescriptionAsync(createDto), Times.Once);
        }

        [Fact]
        public async Task CreatePrescriptionAsync_ShouldReturnFalse_WhenDtoIsNull()
        {
            // Act
            var result = await _sut.CreatePrescriptionAsync(null!);

            // Assert
            result.Should().BeFalse();
            _mockDataManager.Verify(x => x.CreatePrescriptionAsync(It.IsAny<PrescriptionInputDto>()), Times.Never);
        }

        [Fact]
        public async Task CreatePrescriptionAsync_ShouldReturnFalse_WhenCreationFails()
        {
            // Arrange
            // OpenSpec: unify-medicalcase-input-dto - PrescriptionInputDto仅需MedicalCaseId
            var createDto = new PrescriptionInputDto
            {
                MedicalCaseId = Guid.NewGuid()
            };

            _mockDataManager.Setup(x => x.CreatePrescriptionAsync(createDto))
                .ReturnsAsync((PrescriptionDetailDto?)null);

            // Act
            var result = await _sut.CreatePrescriptionAsync(createDto);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public async Task UpdatePrescriptionAsync_ShouldCallSave_WhenPrescriptionExists()
        {
            // Arrange
            _mockDataManager.Setup(x => x.CurrentPrescription).Returns(new PrescriptionDetailDto { Id = Guid.NewGuid() });
            _mockValidator.Setup(x => x.IsValid(out It.Ref<string>.IsAny))
                .Returns((out string error) => { error = string.Empty; return true; });
            _mockDataManager.Setup(x => x.SaveAsync()).ReturnsAsync(true);

            // Act
            var result = await _sut.UpdatePrescriptionAsync();

            // Assert
            result.Should().BeTrue();
            _mockDataManager.Verify(x => x.SaveAsync(), Times.Once);
        }

        [Fact]
        public async Task UpdatePrescriptionAsync_ShouldReturnFalse_WhenPrescriptionIsNull()
        {
            // Arrange
            _mockDataManager.Setup(x => x.CurrentPrescription).Returns((PrescriptionDetailDto?)null);

            // Act
            var result = await _sut.UpdatePrescriptionAsync();

            // Assert
            result.Should().BeFalse();
            _mockDataManager.Verify(x => x.SaveAsync(), Times.Never);
        }

        [Fact]
        public async Task DeletePrescriptionAsync_ShouldCallDataManager_WhenInvoked()
        {
            // Arrange
            _mockDataManager.Setup(x => x.DeletePrescriptionAsync()).ReturnsAsync(true);

            // Act
            var result = await _sut.DeletePrescriptionAsync();

            // Assert
            result.Should().BeTrue();
            _mockDataManager.Verify(x => x.DeletePrescriptionAsync(), Times.Once);
        }

        #endregion

        #region Navigation Tests

        [Fact]
        public async Task NavigateToPatientHistoryAsync_ShouldNavigateWithParameters()
        {
            // Arrange
            var patientId = Guid.NewGuid();

            // Act
            var result = await _sut.NavigateToPatientHistoryAsync(patientId);

            // Assert
            result.Should().BeTrue();
            // OpenSpec: refactor-medicalcase-management - 使用新的Master-Detail视图
            _mockRegionManager.Verify(x => x.RequestNavigate(
                "ContentRegion",
                "MedicalCaseMasterDetailView",
                It.Is<NavigationParameters>(p => p.ContainsKey("PatientId"))),
                Times.Once);
        }

        [Fact]
        public async Task NavigateToMedicalCaseListAsync_ShouldNavigateToList()
        {
            // Act
            var result = await _sut.NavigateToMedicalCaseListAsync();

            // Assert
            result.Should().BeTrue();
            // OpenSpec: refactor-medicalcase-management - 使用新的Master-Detail视图
            _mockRegionManager.Verify(x => x.RequestNavigate(
                "ContentRegion",
                "MedicalCaseMasterDetailView"),
                Times.Once);
        }

        #endregion

        #region ICommandHandler Tests

        [Fact]
        public void RegisterCommand_ShouldAllowCommandRegistration()
        {
            // Arrange
            var commandName = "TestCommand";
            Func<object?, Task<bool>> handler = async (param) => { await Task.Delay(1); return true; };

            // Act
            _sut.RegisterCommand(commandName, handler);

            // Assert - 不抛异常即为成功
            true.Should().BeTrue();
        }

        [Fact]
        public void RegisterCanExecute_ShouldAllowCanExecuteRegistration()
        {
            // Arrange
            var commandName = "TestCommand";
            Func<bool> canExecute = () => true;

            // Act
            _sut.RegisterCanExecute(commandName, canExecute);

            // Assert - 不抛异常即为成功
            true.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_ShouldExecuteRegisteredCommand()
        {
            // Arrange
            var commandName = "TestCommand";
            var executed = false;
            Func<object?, Task<bool>> handler = async (param) =>
            {
                await Task.Delay(1);
                executed = true;
                return true;
            };

            _sut.RegisterCommand(commandName, handler);

            // Act
            var result = await _sut.ExecuteAsync(commandName);

            // Assert
            result.Should().BeTrue();
            executed.Should().BeTrue();
        }

        [Fact]
        public async Task ExecuteAsync_ShouldReturnFalse_WhenCommandNotFound()
        {
            // Act
            var result = await _sut.ExecuteAsync("NonExistentCommand");

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void CanExecute_ShouldReturnTrue_WhenNoHandlerRegistered()
        {
            // Act
            var result = _sut.CanExecute("AnyCommand");

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void CanExecute_ShouldReturnHandlerResult_WhenHandlerRegistered()
        {
            // Arrange
            var commandName = "TestCommand";
            _sut.RegisterCanExecute(commandName, () => false);

            // Act
            var result = _sut.CanExecute(commandName);

            // Assert
            result.Should().BeFalse();
        }

        #endregion
    }
}
