using FluentAssertions;
using LYBT.Desktop.Users.Interfaces;
using LYBT.Desktop.Users.ViewModels;
using LYBT.Desktop.Users.ViewModels.Components;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Moq;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Users.Tests.ViewModels
{
    /// <summary>
    /// UserDetailViewModel 单元测试
    /// Issue #2168: 测试CRUD统一架构（Create/Edit/View三种模式）
    /// </summary>
    public class UserDetailViewModelTests : IDisposable
    {
        private readonly Mock<UserCommandHandler> _mockCommandHandler;
        private readonly Mock<IUserRepository> _mockUserRepository;
        private readonly Mock<IEventAggregator> _mockEventAggregator;
        private readonly Mock<ILoggerFactory> _mockLoggerFactory;
        private readonly Mock<ILogger<UserDetailViewModel>> _mockLogger;
        private readonly Mock<ILogger<UserCommandHandler>> _mockCommandLogger;
        private readonly Mock<IRegionManager> _mockRegionManager;
        private readonly UserDetailViewModel _viewModel;

        public UserDetailViewModelTests()
        {
            // Arrange - Setup Mocks
            _mockUserRepository = new Mock<IUserRepository>();
            _mockCommandLogger = new Mock<ILogger<UserCommandHandler>>();
            _mockCommandHandler = new Mock<UserCommandHandler>(_mockUserRepository.Object, _mockCommandLogger.Object);
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
                _mockCommandHandler.Object,
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
            _viewModel.UserId.Should().Be(Guid.Empty); // 初始为空=Create模式
            _viewModel.IsCreateMode.Should().BeTrue();
            _viewModel.IsEditMode.Should().BeTrue(); // 默认为编辑模式
            _viewModel.IsReadOnly.Should().BeFalse();
        }

        [Fact]
        public void Constructor_ShouldInitializeCommands()
        {
            // Assert
            _viewModel.SubmitCommand.Should().NotBeNull();
            _viewModel.CancelCommand.Should().NotBeNull();
            _viewModel.SwitchToEditModeCommand.Should().NotBeNull();
            _viewModel.GoBackCommand.Should().NotBeNull();
        }

        [Fact]
        public void Constructor_ShouldInitializeOptions()
        {
            // Assert
            _viewModel.RoleOptions.Should().NotBeNull();
            _viewModel.RoleOptions.Should().HaveCountGreaterThan(0);
            _viewModel.StatusOptions.Should().NotBeNull();
            _viewModel.StatusOptions.Should().HaveCountGreaterThan(0);
        }

        #endregion

        #region Create模式测试

        [Fact]
        public void CreateMode_ShouldHaveCorrectProperties()
        {
            // Arrange & Act - 默认构造就是Create模式（无参数导航）

            // Assert
            _viewModel.UserId.Should().Be(Guid.Empty);
            _viewModel.IsCreateMode.Should().BeTrue();
            _viewModel.IsEditOrViewMode.Should().BeFalse();
            _viewModel.IsEditMode.Should().BeTrue();
            _viewModel.IsReadOnly.Should().BeFalse();
        }

        [Fact]
        public void CreateMode_FormFields_ShouldBeEmpty()
        {
            // Assert
            _viewModel.UserName.Should().BeEmpty();
            _viewModel.RealName.Should().BeEmpty();
            _viewModel.PhoneNumber.Should().BeNull();
            _viewModel.Email.Should().BeNull();
            _viewModel.SelectedRole.Should().Be(UserRole.Doctor); // 默认值
            _viewModel.Status.Should().Be(CommonStatus.Enabled); // 默认值
        }

        #endregion

        #region Edit/View模式测试

        [Fact]
        public void ProcessNavigationParameters_WithUserId_ShouldEnterEditMode()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var parameters = new NavigationParameters
            {
                { "UserId", userId }
            };

            // Act
            InvokeProcessNavigationParameters(parameters);

            // Assert
            _viewModel.UserId.Should().Be(userId);
            _viewModel.IsCreateMode.Should().BeFalse();
            _viewModel.IsEditOrViewMode.Should().BeTrue();
            _viewModel.IsEditMode.Should().BeTrue(); // 有UserId但无ReadOnly=Edit模式
            _viewModel.IsReadOnly.Should().BeFalse();
        }

        [Fact]
        public void ProcessNavigationParameters_WithUserIdAndReadOnly_ShouldEnterViewMode()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var parameters = new NavigationParameters
            {
                { "UserId", userId },
                { "ReadOnly", true }
            };

            // Act
            InvokeProcessNavigationParameters(parameters);

            // Assert
            _viewModel.UserId.Should().Be(userId);
            _viewModel.IsCreateMode.Should().BeFalse();
            _viewModel.IsEditOrViewMode.Should().BeTrue();
            _viewModel.IsEditMode.Should().BeFalse(); // ReadOnly=true → View模式
            _viewModel.IsReadOnly.Should().BeTrue();
        }

        #endregion

        #region 模式切换测试

        [Fact]
        public void SwitchToEditMode_FromViewMode_ShouldChangeToEditMode()
        {
            // Arrange - 设置为View模式
            var userId = Guid.NewGuid();
            var parameters = new NavigationParameters
            {
                { "UserId", userId },
                { "ReadOnly", true }
            };
            InvokeProcessNavigationParameters(parameters);
            _viewModel.IsReadOnly.Should().BeTrue();

            // Act - 切换到Edit模式
            _viewModel.SwitchToEditModeCommand.Execute();

            // Assert
            _viewModel.IsEditMode.Should().BeTrue();
            _viewModel.IsReadOnly.Should().BeFalse();
        }

        [Fact]
        public void CanSwitchToEditMode_InViewMode_ShouldReturnTrue()
        {
            // Arrange - View模式
            var userId = Guid.NewGuid();
            _viewModel.UserId = userId;
            var isEditModeProperty = typeof(UserDetailViewModel)
                .GetProperty("IsEditMode", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            isEditModeProperty!.SetValue(_viewModel, false); // View模式

            // Act
            var canSwitch = _viewModel.SwitchToEditModeCommand.CanExecute();

            // Assert
            canSwitch.Should().BeTrue();
        }

        [Fact]
        public void CanSwitchToEditMode_InCreateMode_ShouldReturnFalse()
        {
            // Arrange - Create模式（UserId=Empty）
            _viewModel.UserId = Guid.Empty;

            // Act
            var canSwitch = _viewModel.SwitchToEditModeCommand.CanExecute();

            // Assert
            canSwitch.Should().BeFalse(); // Create模式不应该有"切换到编辑"按钮
        }

        #endregion

        #region CanSubmit测试

        [Fact]
        public void CanSubmit_InViewMode_ShouldReturnFalse()
        {
            // Arrange - View模式
            var userId = Guid.NewGuid();
            _viewModel.UserId = userId;
            var isEditModeProperty = typeof(UserDetailViewModel)
                .GetProperty("IsEditMode", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            isEditModeProperty!.SetValue(_viewModel, false);

            _viewModel.UserName = "testuser";
            _viewModel.RealName = "测试用户";

            // Act
            var canSubmit = _viewModel.SubmitCommand.CanExecute();

            // Assert
            canSubmit.Should().BeFalse(); // View模式不能提交
        }

        [Fact]
        public void CanSubmit_WithValidData_ShouldReturnTrue()
        {
            // Arrange
            _viewModel.UserName = "testuser";
            _viewModel.RealName = "测试用户";

            // Act
            var canSubmit = _viewModel.SubmitCommand.CanExecute();

            // Assert
            canSubmit.Should().BeTrue();
        }

        [Fact]
        public void CanSubmit_WithEmptyUserName_ShouldReturnFalse()
        {
            // Arrange
            _viewModel.UserName = "";
            _viewModel.RealName = "测试用户";

            // Act
            var canSubmit = _viewModel.SubmitCommand.CanExecute();

            // Assert
            canSubmit.Should().BeFalse();
        }

        [Fact]
        public void CanSubmit_WithEmptyRealName_ShouldReturnFalse()
        {
            // Arrange
            _viewModel.UserName = "testuser";
            _viewModel.RealName = "";

            // Act
            var canSubmit = _viewModel.SubmitCommand.CanExecute();

            // Assert
            canSubmit.Should().BeFalse();
        }

        #endregion

        #region 表单属性测试

        [Fact]
        public void SetFormProperties_ShouldUpdateProperties()
        {
            // Arrange & Act
            _viewModel.UserName = "testuser";
            _viewModel.RealName = "测试用户";
            _viewModel.PhoneNumber = "13800138000";
            _viewModel.Email = "test@example.com";
            _viewModel.SelectedRole = UserRole.Admin;
            _viewModel.Status = CommonStatus.Disabled;

            // Assert
            _viewModel.UserName.Should().Be("testuser");
            _viewModel.RealName.Should().Be("测试用户");
            _viewModel.PhoneNumber.Should().Be("13800138000");
            _viewModel.Email.Should().Be("test@example.com");
            _viewModel.SelectedRole.Should().Be(UserRole.Admin);
            _viewModel.Status.Should().Be(CommonStatus.Disabled);
        }

        #endregion

        #region ExecuteGoBack 测试

        [Fact]
        public void ExecuteGoBack_ShouldNotThrowException()
        {
            // Act & Assert
            Action act = () => _viewModel.GoBackCommand.Execute();
            act.Should().NotThrow();
        }

        #endregion

        #region Helper Methods

        private void InvokeProcessNavigationParameters(NavigationParameters parameters)
        {
            var method = typeof(UserDetailViewModel)
                .GetMethod("ProcessNavigationParameters",
                    System.Reflection.BindingFlags.NonPublic |
                    System.Reflection.BindingFlags.Instance);
            method!.Invoke(_viewModel, new object[] { parameters });
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
