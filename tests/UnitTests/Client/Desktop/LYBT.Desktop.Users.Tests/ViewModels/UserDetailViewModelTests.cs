using FluentAssertions;
using LYBT.Desktop.Users.ViewModels;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;
using Prism.Regions;
using Xunit;

namespace LYBT.Desktop.Users.Tests.ViewModels
{
    /// <summary>
    /// UserDetailViewModel 单元测试
    /// 测试用户详情ViewModel的基本功能（Phase 4B 骨架实现）
    /// </summary>
    public class UserDetailViewModelTests : IDisposable
    {
        private readonly Mock<IEventAggregator> _mockEventAggregator;
        private readonly Mock<ILoggerFactory> _mockLoggerFactory;
        private readonly Mock<ILogger<UserDetailViewModel>> _mockLogger;
        private readonly Mock<IRegionManager> _mockRegionManager;
        private readonly UserDetailViewModel _viewModel;

        public UserDetailViewModelTests()
        {
            // Arrange - Setup Mocks
            _mockEventAggregator = new Mock<IEventAggregator>();
            _mockLoggerFactory = new Mock<ILoggerFactory>();
            _mockLogger = new Mock<ILogger<UserDetailViewModel>>();
            _mockRegionManager = new Mock<IRegionManager>();

            // Setup LoggerFactory to return mock logger
            _mockLoggerFactory
                .Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(_mockLogger.Object);

            // Create ViewModel instance
            _viewModel = new UserDetailViewModel(
                _mockEventAggregator.Object,
                _mockLoggerFactory.Object,
                _mockRegionManager.Object
            );
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_ShouldInitializeViewModel()
        {
            // Assert
            _viewModel.Should().NotBeNull();
            _viewModel.User.Should().BeNull(); // 初始无用户
        }

        [Fact]
        public void Constructor_ShouldInitializeCommands()
        {
            // Assert
            _viewModel.GoBackCommand.Should().NotBeNull();
            _viewModel.EditUserCommand.Should().NotBeNull();
            _viewModel.ResetPasswordCommand.Should().NotBeNull();
        }

        #endregion

        #region User属性测试

        [Fact]
        public void SetUser_ShouldUpdateUserProperty()
        {
            // Arrange
            var testUser = CreateSampleUserDto();

            // Act
            _viewModel.User = testUser;

            // Assert
            _viewModel.User.Should().NotBeNull();
            _viewModel.User.Should().Be(testUser);
            _viewModel.User!.UserName.Should().Be("testuser");
        }

        #endregion

        #region CanExecuteEditUser 测试

        [Fact]
        public void CanExecuteEditUser_WithoutUser_ShouldReturnFalse()
        {
            // Arrange
            _viewModel.User = null;

            // Act
            var method = typeof(UserDetailViewModel)
                .GetMethod("CanExecuteEditUser", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var canExecute = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            canExecute.Should().BeFalse();
        }

        [Fact]
        public void CanExecuteEditUser_WithUserAndNotBusy_ShouldReturnTrue()
        {
            // Arrange
            _viewModel.User = CreateSampleUserDto();
            // IsBusy默认为false

            // Act
            var method = typeof(UserDetailViewModel)
                .GetMethod("CanExecuteEditUser", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var canExecute = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            canExecute.Should().BeTrue();
        }

        [Fact]
        public void CanExecuteEditUser_WhenBusy_ShouldReturnFalse()
        {
            // Arrange
            _viewModel.User = CreateSampleUserDto();
            
            // 使用反射设置IsBusy为true
            var isBusyProperty = typeof(UserDetailViewModel).BaseType!
                .GetProperty("IsBusy", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            isBusyProperty!.SetValue(_viewModel, true);

            // Act
            var method = typeof(UserDetailViewModel)
                .GetMethod("CanExecuteEditUser", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var canExecute = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            canExecute.Should().BeFalse();
        }

        #endregion

        #region CanExecuteResetPassword 测试

        [Fact]
        public void CanExecuteResetPassword_WithoutUser_ShouldReturnFalse()
        {
            // Arrange
            _viewModel.User = null;

            // Act
            var method = typeof(UserDetailViewModel)
                .GetMethod("CanExecuteResetPassword", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var canExecute = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            canExecute.Should().BeFalse();
        }

        [Fact]
        public void CanExecuteResetPassword_WithUserAndNotBusy_ShouldReturnTrue()
        {
            // Arrange
            _viewModel.User = CreateSampleUserDto();

            // Act
            var method = typeof(UserDetailViewModel)
                .GetMethod("CanExecuteResetPassword", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var canExecute = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            canExecute.Should().BeTrue();
        }

        #endregion

        #region ExecuteGoBack 测试

        [Fact]
        public void ExecuteGoBack_ShouldNotThrowException()
        {
            // Act & Assert - 骨架实现，仅验证不抛出异常
            Action act = () => _viewModel.GoBackCommand.Execute();
            act.Should().NotThrow();
        }

        #endregion

        #region Helper Methods

        private UserDto CreateSampleUserDto()
        {
            return new UserDto
            {
                Id = Guid.NewGuid(),
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
