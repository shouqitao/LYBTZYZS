using FluentAssertions;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Users.Interfaces;
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
    /// UserEditViewModel 单元测试
    /// 测试用户编辑ViewModel的核心功能
    /// </summary>
    public class UserEditViewModelTests : IDisposable
    {
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IEventAggregator> _mockEventAggregator;
        private readonly Mock<ILoggerFactory> _mockLoggerFactory;
        private readonly Mock<ILogger<UserEditViewModel>> _mockLogger;
        private readonly Mock<IRegionManager> _mockRegionManager;
        private readonly Mock<ISessionManager> _mockSessionManager;
        private readonly Mock<IUserNotificationService> _mockNotificationService;
        private readonly UserEditViewModel _viewModel;

        public UserEditViewModelTests()
        {
            // Arrange - Setup Mocks
            _mockUserRepository = new Mock<IUserRepository>();
            _mockEventAggregator = new Mock<IEventAggregator>();
            _mockLoggerFactory = new Mock<ILoggerFactory>();
            _mockLogger = new Mock<ILogger<UserEditViewModel>>();
            _mockRegionManager = new Mock<IRegionManager>();
            _mockSessionManager = new Mock<ISessionManager>();
            _mockNotificationService = new Mock<IUserNotificationService>();

            // Setup LoggerFactory to return mock logger
            _mockLoggerFactory
                .Setup(x => x.CreateLogger(It.IsAny<string>()))
                .Returns(_mockLogger.Object);

            // Create ViewModel instance
            _viewModel = new UserEditViewModel(
                _mockUserRepository.Object,
                _mockEventAggregator.Object,
                _mockLoggerFactory.Object,
                _mockRegionManager.Object,
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
            _viewModel.RoleOptions.Should().NotBeNull();
            _viewModel.StatusOptions.Should().NotBeNull();
            _viewModel.PageTitle.Should().Be("编辑用户");
            _viewModel.IsUserLoaded.Should().BeFalse();
        }

        [Fact]
        public void Constructor_ShouldInitializeCommands()
        {
            // Assert
            _viewModel.SaveCommand.Should().NotBeNull();
            _viewModel.CancelCommand.Should().NotBeNull();
            _viewModel.ResetCommand.Should().NotBeNull();
            _viewModel.ResetPasswordCommand.Should().NotBeNull();
        }

        #endregion

        #region 用户数据加载测试

        [Fact]
        public async Task LoadUserAsync_WithValidUserId_ShouldLoadUserData()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var testUser = CreateSampleUserDto(userId);

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(testUser);

            // 使用反射设置 UserId
            var userIdProperty = typeof(UserEditViewModel).GetProperty("UserId");
            userIdProperty!.SetValue(_viewModel, userId);

            // Act
            var method = typeof(UserEditViewModel)
                .GetMethod("LoadUserAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)method!.Invoke(_viewModel, null)!;

            // Assert
            _viewModel.IsUserLoaded.Should().BeTrue();
            _viewModel.UserName.Should().Be(testUser.UserName);
            _viewModel.RealName.Should().Be(testUser.RealName);
            _viewModel.PhoneNumber.Should().Be(testUser.PhoneNumber);
            _viewModel.Email.Should().Be(testUser.Email);
            _viewModel.SelectedRole.Should().Be(testUser.Role);
            _viewModel.Status.Should().Be(testUser.Status);
        }

        [Fact]
        public async Task LoadUserAsync_WhenRepositoryReturnsNull_ShouldHandleGracefully()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync((UserDto)null!);

            var userIdProperty = typeof(UserEditViewModel).GetProperty("UserId");
            userIdProperty!.SetValue(_viewModel, userId);

            // Act
            var method = typeof(UserEditViewModel)
                .GetMethod("LoadUserAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)method!.Invoke(_viewModel, null)!;

            // Assert - ExecuteSafelyAsync 会捕获异常，不会直接抛出
            _viewModel.IsUserLoaded.Should().BeFalse();
        }

        #endregion

        #region 表单验证测试

        [Fact]
        public void ValidateRealName_WithValidInput_ShouldPass()
        {
            // Act
            _viewModel.RealName = "张三";

            // Assert
            _viewModel.HasErrors.Should().BeFalse();
        }

        [Fact]
        public void ValidatePhoneNumber_WithInvalidFormat_ShouldFail()
        {
            // Act
            _viewModel.PhoneNumber = "12345678901"; // 无效格式

            // Assert
            _viewModel.HasErrors.Should().BeTrue();
        }

        [Fact]
        public void ValidateEmail_WithInvalidFormat_ShouldFail()
        {
            // Act
            _viewModel.Email = "invalid-email"; // 无@符号

            // Assert
            _viewModel.HasErrors.Should().BeTrue();
        }

        [Fact]
        public void IsFormValid_WithoutUserLoaded_ShouldReturnFalse()
        {
            // Arrange
            _viewModel.RealName = "测试用户";

            // Assert
            _viewModel.IsFormValid.Should().BeFalse(); // IsUserLoaded = false
        }

        #endregion

        #region 变更检测测试

        [Fact]
        public void HasChanges_WithoutUserLoaded_ShouldReturnFalse()
        {
            // Arrange
            _viewModel.RealName = "新姓名";

            // Assert
            _viewModel.HasChanges.Should().BeFalse(); // IsUserLoaded = false
        }

        [Fact]
        public async Task HasChanges_WhenNoChanges_ShouldReturnFalse()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var testUser = CreateSampleUserDto(userId);

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(testUser);

            var userIdProperty = typeof(UserEditViewModel).GetProperty("UserId");
            userIdProperty!.SetValue(_viewModel, userId);

            // 加载用户数据
            var loadMethod = typeof(UserEditViewModel)
                .GetMethod("LoadUserAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)loadMethod!.Invoke(_viewModel, null)!;

            // Act - 无任何更改

            // Assert
            _viewModel.HasChanges.Should().BeFalse();
        }

        [Fact]
        public async Task HasChanges_WhenRealNameChanged_ShouldReturnTrue()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var testUser = CreateSampleUserDto(userId);

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(testUser);

            var userIdProperty = typeof(UserEditViewModel).GetProperty("UserId");
            userIdProperty!.SetValue(_viewModel, userId);

            var loadMethod = typeof(UserEditViewModel)
                .GetMethod("LoadUserAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)loadMethod!.Invoke(_viewModel, null)!;

            // Act
            _viewModel.RealName = "新姓名";

            // Assert
            _viewModel.HasChanges.Should().BeTrue();
        }

        #endregion

        #region 保存命令测试

        [Fact]
        public void CanExecuteSave_WithoutChanges_ShouldReturnFalse()
        {
            // Arrange - 假设已加载用户但无更改
            // 使用反射设置 IsUserLoaded
            var isUserLoadedProperty = typeof(UserEditViewModel)
                .GetProperty("IsUserLoaded", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            isUserLoadedProperty!.SetValue(_viewModel, true);

            _viewModel.RealName = "测试用户";

            // Act
            var method = typeof(UserEditViewModel)
                .GetMethod("CanExecuteSave", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var canSave = (bool)method!.Invoke(_viewModel, null)!;

            // Assert
            canSave.Should().BeFalse(); // HasChanges = false
        }

        #endregion

        #region 重置命令测试

        [Fact]
        public async Task ResetCommand_ShouldRestoreOriginalValues()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var testUser = CreateSampleUserDto(userId);

            _mockUserRepository
                .Setup(x => x.GetByIdAsync(userId))
                .ReturnsAsync(testUser);

            var userIdProperty = typeof(UserEditViewModel).GetProperty("UserId");
            userIdProperty!.SetValue(_viewModel, userId);

            var loadMethod = typeof(UserEditViewModel)
                .GetMethod("LoadUserAsync", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            await (Task)loadMethod!.Invoke(_viewModel, null)!;

            // 修改数据
            _viewModel.RealName = "新姓名";
            _viewModel.PhoneNumber = "13900000000";

            // Act
            var resetMethod = typeof(UserEditViewModel)
                .GetMethod("ExecuteReset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            resetMethod!.Invoke(_viewModel, null);

            // Assert
            _viewModel.RealName.Should().Be(testUser.RealName); // 恢复原值
            _viewModel.PhoneNumber.Should().Be(testUser.PhoneNumber);
            _viewModel.HasChanges.Should().BeFalse();
        }

        #endregion

        #region 取消命令测试

        [Fact]
        public void CancelCommand_ShouldExecute()
        {
            // Arrange
            var mockRegion = new Mock<IRegion>();
            _mockRegionManager.Setup(x => x.Regions["ContentRegion"]).Returns(mockRegion.Object);

            // Act
            var method = typeof(UserEditViewModel)
                .GetMethod("ExecuteCancel", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            method!.Invoke(_viewModel, null);

            // Assert - 验证导航逻辑（简化版，实际会调用 NavigateTo）
            // 此处仅验证方法可执行，不抛异常
            Assert.True(true);
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
