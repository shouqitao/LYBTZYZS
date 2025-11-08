using FluentAssertions;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Desktop.Users.ViewModels;
using LYBT.Desktop.Users.ViewModels.Components;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Users.Tests.ViewModels
{
    /// <summary>
    /// UserProfileDialogViewModel 单元测试
    /// 测试个人资料编辑对话框的功能
    /// </summary>
    public class UserProfileDialogViewModelTests : IDisposable
    {
        private readonly Mock<UserCommandHandler> _mockCommandHandler;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IEventAggregator> _mockEventAggregator;
        private readonly Mock<ILoggerFactory> _mockLoggerFactory;
        private readonly Mock<ILogger<UserProfileDialogViewModel>> _mockLogger;
        private readonly Mock<ILogger<UserCommandHandler>> _mockCommandLogger;
        private readonly Mock<IRegionManager> _mockRegionManager;
        private readonly Mock<ISessionManager> _mockSessionManager;
        private readonly Mock<IAuthenticationService> _mockAuthService;
        private readonly Mock<IUserNotificationService> _mockNotificationService;
        private readonly UserProfileDialogViewModel _viewModel;

        public UserProfileDialogViewModelTests()
        {
            // Arrange - Setup Mocks
            _mockUserRepository = new Mock<IUserRepository>();
            _mockCommandLogger = new Mock<ILogger<UserCommandHandler>>();
            _mockCommandHandler = new Mock<UserCommandHandler>(_mockUserRepository.Object, _mockCommandLogger.Object);
            _mockEventAggregator = new Mock<IEventAggregator>();
            _mockLoggerFactory = new Mock<ILoggerFactory>();
            _mockLogger = new Mock<ILogger<UserProfileDialogViewModel>>();
            _mockRegionManager = new Mock<IRegionManager>();
            _mockSessionManager = new Mock<ISessionManager>();
            _mockAuthService = new Mock<IAuthenticationService>();
            _mockNotificationService = new Mock<IUserNotificationService>();

            // Setup LoggerFactory to return mock logger
            _mockLoggerFactory
                .Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(_mockLogger.Object);

            // Create ViewModel instance (Issue #1887-1892: 独立的个人资料对话框)
            _viewModel = new UserProfileDialogViewModel(
                _mockCommandHandler.Object,
                _mockSessionManager.Object,
                _mockEventAggregator.Object,
                _mockLoggerFactory.Object,
                _mockRegionManager.Object,
                _mockNotificationService.Object
            );
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_ShouldInitializeViewModel()
        {
            // Assert
            _viewModel.Should().NotBeNull();
            _viewModel.Title.Should().Be("个人资料"); // Issue #1892: 默认为非sysadmin模式
            // _viewModel.HasAvatar.Should().BeFalse(); // Avatar功能未实现
            // _viewModel.IsSysAdmin.Should().BeFalse(); // IsSysAdmin属性已移除 // Issue #1892: 默认非sysadmin
        }

        [Fact]
        public void Constructor_ShouldInitializeCommands()
        {
            // Assert
            // _viewModel.SelectAvatarCommand.Should().NotBeNull(); // Avatar命令未实现
            // _viewModel.RemoveAvatarCommand.Should().NotBeNull(); // Avatar命令未实现
            _viewModel.SaveCommand.Should().NotBeNull();
            _viewModel.CancelCommand.Should().NotBeNull();
        }

        #endregion

        #region OnDialogOpened 测试

        [Fact]
        public void OnDialogOpened_WithValidCurrentUser_ShouldLoadUserProfile()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var currentUser = CreateSampleUserDto(userId);

            // Mock ISessionManager.CurrentUser
            _mockSessionManager
                .Setup(x => x.CurrentUser)
                .Returns(currentUser);

            _mockCommandHandler
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((true, currentUser, (string?)null));

            // Issue #1892: 添加IsSysAdmin参数（false = 普通用户模式）
            var parameters = new DialogParameters
            {
                { "IsSysAdmin", false }
            };

            // Act
            _viewModel.OnDialogOpened(parameters);

            // Wait a moment for async loading
            Thread.Sleep(100);

            // Assert - 验证CommandHandler被调用
            _mockCommandHandler.Verify(x => x.GetByIdAsync(userId), Times.AtLeastOnce);
        }

        [Fact]
        public void OnDialogOpened_WithoutCurrentUser_ShouldSetError()
        {
            // Arrange
            // Mock ISessionManager.CurrentUser 返回 null
            _mockSessionManager
                .Setup(x => x.CurrentUser)
                .Returns((UserDto?)null);

            // Issue #1892: 添加IsSysAdmin参数
            var parameters = new DialogParameters
            {
                { "IsSysAdmin", false }
            };

            // Act
            _viewModel.OnDialogOpened(parameters);

            // Assert
            _viewModel.HasError.Should().BeTrue();
            _viewModel.ErrorMessage.Should().Contain("用户信息");
        }

        #endregion

        #region LoadUserProfileAsync 测试

        [Fact]
        public async Task LoadUserProfileAsync_WithValidUserId_ShouldSetUserInfo()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var testUser = CreateSampleUserDto(userId);

            _mockCommandHandler
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((true, testUser, (string?)null));

            // 使用反射设置 _currentUserId
            var currentUserIdField = typeof(UserProfileDialogViewModel)
                .GetField("_currentUserId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            currentUserIdField!.SetValue(_viewModel, userId);

            // Act - 使用反射调用私有方法
            var method = typeof(UserProfileDialogViewModel)
                .GetMethod("LoadUserProfileAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)method!.Invoke(_viewModel, null)!;

            // Assert
            _viewModel.UserName.Should().Be(testUser.UserName);
            _viewModel.RealName.Should().Be(testUser.RealName);
            _viewModel.Email.Should().Be(testUser.Email);
            _viewModel.PhoneNumber.Should().Be(testUser.PhoneNumber);
            _viewModel.HasError.Should().BeFalse();
        }

        [Fact]
        public async Task LoadUserProfileAsync_WhenRepositoryReturnsNull_ShouldSetError()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockCommandHandler
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((false, (UserDto?)null, "加载个人资料失败")); // Issue #1892: 错误消息包含"失败"

            var currentUserIdField = typeof(UserProfileDialogViewModel)
                .GetField("_currentUserId", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            currentUserIdField!.SetValue(_viewModel, userId);

            // Act
            var method = typeof(UserProfileDialogViewModel)
                .GetMethod("LoadUserProfileAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)method!.Invoke(_viewModel, null)!;

            // Assert
            _viewModel.HasError.Should().BeTrue();
            _viewModel.ErrorMessage.Should().Contain("失败");
        }

        #endregion

        /*
        #region UpdateAvatarInitial 测试 - Avatar功能未实现，暂时注释

        [Fact]
        public void UpdateAvatarInitial_WithUsernameAndNoAvatar_ShouldSetInitial()
        {
            // Arrange
            _viewModel.HasAvatar = false;

            // Act - 设置Username会触发UpdateAvatarInitial
            _viewModel.UserName = "testuser";

            // Assert
            _viewModel.AvatarInitial.Should().Be("T"); // 首字母大写
        }

        #endregion
        */

        /*
        #region RemoveAvatar 测试 - Avatar功能未实现，暂时注释

        [Fact]
        public void RemoveAvatarCommand_ShouldClearAvatarAndSetInitial()
        {
            // Arrange
            _viewModel.UserName = "testuser";
            _viewModel.HasAvatar = true; // 先设置有头像

            // Act
            _viewModel.RemoveAvatarCommand.Execute();

            // Assert
            _viewModel.HasAvatar.Should().BeFalse();
            _viewModel.AvatarSource.Should().BeNull();
            _viewModel.AvatarInitial.Should().Be("T");
        }

        #endregion
        */

        #region ValidateInput 测试

        [Fact]
        public void ValidateInput_WithEmptyRealName_ShouldFail()
        {
            // Arrange
            _viewModel.RealName = string.Empty;
            _viewModel.PhoneNumber = "13800138000";

            // Act
            var method = typeof(UserProfileDialogViewModel)
                .GetMethod("ValidateInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isValid = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            isValid.Should().BeFalse();
            _viewModel.HasError.Should().BeTrue();
            _viewModel.ErrorMessage.Should().Contain("真实姓名");
        }

        [Fact]
        public void ValidateInput_WithInvalidPhoneNumber_ShouldFail()
        {
            // Arrange
            _viewModel.RealName = "张三";
            _viewModel.PhoneNumber = "22345678901"; // 不以1开头（第一位是2）

            // Act
            var method = typeof(UserProfileDialogViewModel)
                .GetMethod("ValidateInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isValid = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            isValid.Should().BeFalse();
            _viewModel.ErrorMessage.Should().Contain("手机号码");
        }

        [Fact]
        public void ValidateInput_WithValidInput_ShouldPass()
        {
            // Arrange
            _viewModel.RealName = "张三";
            _viewModel.PhoneNumber = "13800138000";

            // Act
            var method = typeof(UserProfileDialogViewModel)
                .GetMethod("ValidateInput", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var isValid = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            isValid.Should().BeTrue();
            _viewModel.HasError.Should().BeFalse();
        }

        #endregion

        #region CanSave 测试

        [Fact]
        public void CanSave_WithRealNameFilled_ShouldReturnTrue()
        {
            // Arrange
            _viewModel.RealName = "张三";

            // Act
            var method = typeof(UserProfileDialogViewModel)
                .GetMethod("CanSave", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var canSave = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            canSave.Should().BeTrue();
        }

        [Fact]
        public void CanSave_WithEmptyRealName_ShouldReturnFalse()
        {
            // Arrange
            _viewModel.RealName = string.Empty;

            // Act
            var method = typeof(UserProfileDialogViewModel)
                .GetMethod("CanSave", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var canSave = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            canSave.Should().BeFalse();
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
