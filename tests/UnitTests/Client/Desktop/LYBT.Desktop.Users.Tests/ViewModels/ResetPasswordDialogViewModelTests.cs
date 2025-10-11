using FluentAssertions;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Users.Repositories;
using LYBT.Desktop.Users.ViewModels;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;
using Xunit;

namespace LYBT.Desktop.Users.Tests.ViewModels
{
    /// <summary>
    /// ResetPasswordDialogViewModel 单元测试
    /// 测试管理员重置用户密码的功能
    /// </summary>
    public class ResetPasswordDialogViewModelTests : IDisposable
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IEventAggregator> _mockEventAggregator;
        private readonly Mock<ILoggerFactory> _mockLoggerFactory;
        private readonly Mock<ILogger<ResetPasswordDialogViewModel>> _mockLogger;
        private readonly Mock<IRegionManager> _mockRegionManager;
        private readonly Mock<ISessionManager> _mockSessionManager;
        private readonly Mock<IUserNotificationService> _mockNotificationService;
        private readonly ResetPasswordDialogViewModel _viewModel;

        public ResetPasswordDialogViewModelTests()
        {
            // Arrange - Setup Mocks
            _mockUserRepository = new Mock<IUserRepository>();
            _mockEventAggregator = new Mock<IEventAggregator>();
            _mockLoggerFactory = new Mock<ILoggerFactory>();
            _mockLogger = new Mock<ILogger<ResetPasswordDialogViewModel>>();
            _mockRegionManager = new Mock<IRegionManager>();
            _mockSessionManager = new Mock<ISessionManager>();
            _mockNotificationService = new Mock<IUserNotificationService>();

            // Setup LoggerFactory to return mock logger
            _mockLoggerFactory
                .Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(_mockLogger.Object);

            // Create ViewModel instance
            _viewModel = new ResetPasswordDialogViewModel(
                _mockEventAggregator.Object,
                _mockLoggerFactory.Object,
                _mockRegionManager.Object,
                _mockUserRepository.Object,
                _mockSessionManager.Object,
                _mockNotificationService.Object
            );
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_ShouldInitializeViewModel()
        {
            // Assert
            _viewModel.Should().NotBeNull();
            _viewModel.Title.Should().Be("重置密码");
            _viewModel.RequirePasswordChange.Should().BeTrue(); // 默认要求修改密码
            _viewModel.SendNotification.Should().BeFalse(); // 默认不发送通知
        }

        [Fact]
        public void Constructor_ShouldInitializeCommands()
        {
            // Assert
            _viewModel.GeneratePasswordCommand.Should().NotBeNull();
            _viewModel.ConfirmCommand.Should().NotBeNull();
            _viewModel.CancelCommand.Should().NotBeNull();
        }

        #endregion

        #region OnDialogOpened 测试

        [Fact]
        public void OnDialogOpened_WithValidUserId_ShouldLoadUserInfo()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var testUser = CreateSampleUserDto(userId);

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(testUser);

            var parameters = new DialogParameters
            {
                { "UserId", userId }
            };

            // Act
            _viewModel.OnDialogOpened(parameters);

            // Wait a moment for async loading
            Thread.Sleep(100);

            // Assert
            // 注意：LoadUserInfoAsync是异步的，这里只能验证参数被接收
            // 实际的Username设置在异步完成后才会生效
            _mockUserRepository.Verify(x => x.GetByIdAsync(userId), Times.AtLeastOnce);
        }

        [Fact]
        public void OnDialogOpened_WithoutUserId_ShouldSetError()
        {
            // Arrange
            var parameters = new DialogParameters(); // 无UserId参数

            // Act
            _viewModel.OnDialogOpened(parameters);

            // Assert
            _viewModel.HasError.Should().BeTrue();
            _viewModel.ErrorMessage.Should().Contain("参数");
        }

        [Fact]
        public void OnDialogOpened_WithOptionalUsername_ShouldSetUsername()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var username = "testuser";

            var parameters = new DialogParameters
            {
                { "UserId", userId },
                { "Username", username }
            };

            // Act
            _viewModel.OnDialogOpened(parameters);

            // Assert
            _viewModel.Username.Should().Be(username);
        }

        #endregion

        #region LoadUserInfoAsync 测试

        [Fact]
        public async Task LoadUserInfoAsync_WithValidUserId_ShouldSetUsername()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var testUser = CreateSampleUserDto(userId);

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(testUser);

            // Act - 使用反射调用私有方法
            var method = typeof(ResetPasswordDialogViewModel)
                .GetMethod("LoadUserInfoAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)method!.Invoke(_viewModel, new object[] { userId })!;

            // Assert
            _viewModel.Username.Should().Be(testUser.UserName);
            _viewModel.HasError.Should().BeFalse();
        }

        [Fact]
        public async Task LoadUserInfoAsync_WhenRepositoryReturnsNull_ShouldSetError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((UserDto)null!);

            // Act
            var method = typeof(ResetPasswordDialogViewModel)
                .GetMethod("LoadUserInfoAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)method!.Invoke(_viewModel, new object[] { userId })!;

            // Assert
            _viewModel.HasError.Should().BeTrue();
            _viewModel.ErrorMessage.Should().Contain("无法加载");
        }

        #endregion

        #region GenerateRandomPassword 测试

        [Fact]
        public void GeneratePasswordCommand_ShouldGenerateValid12CharPassword()
        {
            // Act
            _viewModel.GeneratePasswordCommand.Execute();

            // Assert
            _viewModel.NewPassword.Should().NotBeNullOrEmpty();
            _viewModel.NewPassword.Length.Should().Be(12);
            _viewModel.ConfirmPassword.Should().Be(_viewModel.NewPassword);
        }

        [Fact]
        public void GeneratePasswordCommand_ShouldIncludeAllCharacterTypes()
        {
            // Act
            _viewModel.GeneratePasswordCommand.Execute();

            var password = _viewModel.NewPassword;

            // Assert - 密码应包含所有字符类型
            password.Should().MatchRegex(@"[a-z]", "应包含小写字母");
            password.Should().MatchRegex(@"[A-Z]", "应包含大写字母");
            password.Should().MatchRegex(@"\d", "应包含数字");
            password.Should().MatchRegex(@"[!@#$%^&*()_+\-=\[\]{}]", "应包含特殊字符");
        }

        #endregion

        #region ValidatePasswords 测试

        [Fact]
        public void ValidatePasswords_WithEmptyNewPassword_ShouldFail()
        {
            // Arrange
            _viewModel.NewPassword = string.Empty;
            _viewModel.ConfirmPassword = "SomePassword123!";

            // Act
            var method = typeof(ResetPasswordDialogViewModel)
                .GetMethod("ValidatePasswords", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isValid = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            isValid.Should().BeFalse();
            _viewModel.HasError.Should().BeTrue();
            _viewModel.ErrorMessage.Should().Contain("请输入新密码");
        }

        [Fact]
        public void ValidatePasswords_WithShortPassword_ShouldFail()
        {
            // Arrange
            _viewModel.NewPassword = "Short1!"; // 7字符，少于8
            _viewModel.ConfirmPassword = "Short1!";

            // Act
            var method = typeof(ResetPasswordDialogViewModel)
                .GetMethod("ValidatePasswords", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isValid = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            isValid.Should().BeFalse();
            _viewModel.ErrorMessage.Should().Contain("至少8个字符");
        }

        [Fact]
        public void ValidatePasswords_WithEmptyConfirmPassword_ShouldFail()
        {
            // Arrange
            _viewModel.NewPassword = "ValidPass123!";
            _viewModel.ConfirmPassword = string.Empty;

            // Act
            var method = typeof(ResetPasswordDialogViewModel)
                .GetMethod("ValidatePasswords", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isValid = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            isValid.Should().BeFalse();
            _viewModel.ErrorMessage.Should().Contain("请确认密码");
        }

        [Fact]
        public void ValidatePasswords_WithMismatchedPasswords_ShouldFail()
        {
            // Arrange
            _viewModel.NewPassword = "Password123!";
            _viewModel.ConfirmPassword = "DifferentPass123!";

            // Act
            var method = typeof(ResetPasswordDialogViewModel)
                .GetMethod("ValidatePasswords", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isValid = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            isValid.Should().BeFalse();
            _viewModel.ErrorMessage.Should().Contain("不一致");
        }

        [Fact]
        public void ValidatePasswords_WithValidInput_ShouldPass()
        {
            // Arrange
            _viewModel.NewPassword = "ValidPass123!";
            _viewModel.ConfirmPassword = "ValidPass123!";

            // Act
            var method = typeof(ResetPasswordDialogViewModel)
                .GetMethod("ValidatePasswords", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isValid = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            isValid.Should().BeTrue();
            _viewModel.HasError.Should().BeFalse();
        }

        #endregion

        #region CanConfirm 测试

        [Fact]
        public void CanConfirm_WithBothPasswordsFilled_ShouldReturnTrue()
        {
            // Arrange
            _viewModel.NewPassword = "Password123!";
            _viewModel.ConfirmPassword = "Password123!";

            // Act
            var method = typeof(ResetPasswordDialogViewModel)
                .GetMethod("CanConfirm", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var canConfirm = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            canConfirm.Should().BeTrue();
        }

        [Fact]
        public void CanConfirm_WithEmptyPassword_ShouldReturnFalse()
        {
            // Arrange
            _viewModel.NewPassword = string.Empty;
            _viewModel.ConfirmPassword = "Password123!";

            // Act
            var method = typeof(ResetPasswordDialogViewModel)
                .GetMethod("CanConfirm", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var canConfirm = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            canConfirm.Should().BeFalse();
        }

        #endregion

        #region Helper Methods

        private UserDto CreateSampleUserDto(Guid? userId = null)
        {
            return new UserDto
            {
                Id = userId ?? Guid.NewGuid(),
                UserName = "testuser",
                RealName = "测试用户",
                Role = UserRole.Doctor,
                Status = CommonStatus.Enabled,
                Email = "test@example.com",
                PhoneNumber = "13800138000",
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            _viewModel?.Dispose();
        }

        #endregion
    }
}
