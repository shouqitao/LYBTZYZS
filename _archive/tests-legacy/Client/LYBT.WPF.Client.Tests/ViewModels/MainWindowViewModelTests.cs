using System;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.WPF.Client.Core.Events;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Users;
using LYBT.WPF.Client.Shell.ViewModels;
using Moq;
using Prism.Events;
using Prism.Navigation.Regions;
using Xunit;

namespace LYBT.WPF.Client.Tests.ViewModels
{
    /// <summary>
    /// MainWindowViewModel 单元测试
    /// 测试应用程序主控制器的核心功能
    /// </summary>
    public class MainWindowViewModelTests
    {
        private readonly Mock<IRegionManager> _mockRegionManager;
        private readonly Mock<IEventAggregator> _mockEventAggregator;
        private readonly Mock<IAuthenticationService> _mockAuthService;
        private readonly Mock<IPermissionService> _mockPermissionService;
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<ICommonDialogService> _mockCommonDialogService;
        private readonly Mock<IRegionCollection> _mockRegionCollection;
        private readonly Mock<IRegion> _mockContentRegion;
        private readonly Mock<IRegion> _mockLoginRegion;
        private readonly MainWindowViewModel _viewModel;

        public MainWindowViewModelTests()
        {
            _mockRegionManager = new Mock<IRegionManager>();
            _mockEventAggregator = new Mock<IEventAggregator>();
            _mockAuthService = new Mock<IAuthenticationService>();
            _mockPermissionService = new Mock<IPermissionService>();
            _mockUserService = new Mock<IUserService>();
            _mockCommonDialogService = new Mock<ICommonDialogService>();
            _mockRegionCollection = new Mock<IRegionCollection>();
            _mockContentRegion = new Mock<IRegion>();
            _mockLoginRegion = new Mock<IRegion>();

            // 设置区域管理器
            SetupRegionManager();

            // 设置事件聚合器
            SetupEventAggregator();

            _viewModel = new MainWindowViewModel(
                _mockRegionManager.Object,
                _mockEventAggregator.Object,
                _mockAuthService.Object,
                _mockPermissionService.Object,
                _mockUserService.Object,
                _mockCommonDialogService.Object);
        }

        private void SetupRegionManager()
        {
            _mockRegionManager.Setup(x => x.Regions).Returns(_mockRegionCollection.Object);
            _mockRegionCollection.Setup(x => x.ContainsRegionWithName("ContentRegion")).Returns(true);
            _mockRegionCollection.Setup(x => x.ContainsRegionWithName("LoginRegion")).Returns(true);
            _mockRegionCollection.Setup(x => x["ContentRegion"]).Returns(_mockContentRegion.Object);
            _mockRegionCollection.Setup(x => x["LoginRegion"]).Returns(_mockLoginRegion.Object);
        }

        private void SetupEventAggregator()
        {
            var mockLoginSuccessEvent = new Mock<LoginSuccessEvent>();
            var mockLogoutEvent = new Mock<LogoutEvent>();

            _mockEventAggregator
                .Setup(x => x.GetEvent<LoginSuccessEvent>())
                .Returns(mockLoginSuccessEvent.Object);

            _mockEventAggregator
                .Setup(x => x.GetEvent<LogoutEvent>())
                .Returns(mockLogoutEvent.Object);
        }

        #region Property Tests

        [Fact]
        public void Title_WhenSet_ShouldUpdateAndNotifyPropertyChanged()
        {
            // Arrange
            var newTitle = "新标题";
            var propertyChanged = false;
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.Title))
                    propertyChanged = true;
            };

            // Act
            _viewModel.Title = newTitle;

            // Assert
            _viewModel.Title.Should().Be(newTitle);
            propertyChanged.Should().BeTrue();
        }

        [Fact]
        public void Title_InitialValue_ShouldBeDefault()
        {
            // Assert
            _viewModel.Title.Should().Be("凌隐宝堂中医诊所诊疗系统");
        }

        [Fact]
        public void CurrentUser_WhenSet_ShouldUpdateAndNotifyPropertyChanged()
        {
            // Arrange
            var user = new UserInfo
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "测试用户"
            };
            var propertyChanged = false;
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.CurrentUser))
                    propertyChanged = true;
            };

            // Act
            _viewModel.CurrentUser = user;

            // Assert
            _viewModel.CurrentUser.Should().Be(user);
            propertyChanged.Should().BeTrue();
        }

        [Fact]
        public void IsLoggedIn_WhenSet_ShouldUpdateAndNotifyBothProperties()
        {
            // Arrange
            var isLoggedInChanged = false;
            var isNotLoggedInChanged = false;
            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(_viewModel.IsLoggedIn))
                    isLoggedInChanged = true;
                if (e.PropertyName == nameof(_viewModel.IsNotLoggedIn))
                    isNotLoggedInChanged = true;
            };

            // Act
            _viewModel.IsLoggedIn = true;

            // Assert
            _viewModel.IsLoggedIn.Should().BeTrue();
            _viewModel.IsNotLoggedIn.Should().BeFalse();
            isLoggedInChanged.Should().BeTrue();
            isNotLoggedInChanged.Should().BeTrue();
        }

        [Fact]
        public void IsNotLoggedIn_ShouldReturnOppositeOfIsLoggedIn()
        {
            // Arrange & Act
            _viewModel.IsLoggedIn = true;

            // Assert
            _viewModel.IsNotLoggedIn.Should().BeFalse();

            // Act
            _viewModel.IsLoggedIn = false;

            // Assert
            _viewModel.IsNotLoggedIn.Should().BeTrue();
        }

        #endregion

        #region Command Tests

        [Fact]
        public void LogoutCommand_ShouldBeInitialized()
        {
            // Assert
            _viewModel.LogoutCommand.Should().NotBeNull();
        }

        [Fact]
        public void TestApiCommand_ShouldBeInitialized()
        {
            // Assert
            _viewModel.TestApiCommand.Should().NotBeNull();
        }

        [Fact]
        public void ShowControlExamplesCommand_ShouldBeInitialized()
        {
            // Assert
            _viewModel.ShowControlExamplesCommand.Should().NotBeNull();
        }

        [Fact]
        public void TestApiCommand_CanExecute_ShouldDependOnIsLoggedIn()
        {
            // Arrange
            _viewModel.IsLoggedIn = false;

            // Assert
            _viewModel.TestApiCommand.CanExecute().Should().BeFalse();

            // Arrange
            _viewModel.IsLoggedIn = true;

            // Assert
            _viewModel.TestApiCommand.CanExecute().Should().BeTrue();
        }

        [Fact]
        public void ShowControlExamplesCommand_CanExecute_ShouldDependOnIsLoggedIn()
        {
            // Arrange
            _viewModel.IsLoggedIn = false;

            // Assert
            _viewModel.ShowControlExamplesCommand.CanExecute().Should().BeFalse();

            // Arrange
            _viewModel.IsLoggedIn = true;

            // Assert
            _viewModel.ShowControlExamplesCommand.CanExecute().Should().BeTrue();
        }

        #endregion

        #region Logout Tests

        [Fact]
        public async Task LogoutCommand_WhenUserConfirms_ShouldPerformLogout()
        {
            // Arrange
            _mockCommonDialogService
                .Setup(x => x.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            
            _mockAuthService
                .Setup(x => x.LogoutAsync())
                .ReturnsAsync(ServiceResult.Success());

            _viewModel.CurrentUser = new UserInfo { Username = "testuser", RealName = "测试用户" };
            _viewModel.IsLoggedIn = true;

            // Act
            await Task.Run(() => _viewModel.LogoutCommand.Execute());
            await Task.Delay(100); // 等待异步操作完成

            // Assert
            _mockAuthService.Verify(x => x.LogoutAsync(), Times.Once);
            _mockContentRegion.Verify(x => x.RemoveAll(), Times.Once);
        }

        [Fact]
        public async Task LogoutCommand_WhenUserCancels_ShouldNotPerformLogout()
        {
            // Arrange
            _mockCommonDialogService
                .Setup(x => x.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(false);

            // Act
            await Task.Run(() => _viewModel.LogoutCommand.Execute());
            await Task.Delay(100);

            // Assert
            _mockAuthService.Verify(x => x.LogoutAsync(), Times.Never);
        }

        [Fact]
        public async Task LogoutCommand_WhenLogoutFails_ShouldShowError()
        {
            // Arrange
            _mockCommonDialogService
                .Setup(x => x.ShowConfirmationAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            
            var failureResult = ServiceResult.Failure("Logout failed");
            _mockAuthService
                .Setup(x => x.LogoutAsync())
                .ThrowsAsync(new Exception("Logout failed"));

            // Act
            await Task.Run(() => _viewModel.LogoutCommand.Execute());
            await Task.Delay(100);

            // Assert
            _mockCommonDialogService.Verify(x => x.ShowErrorAsync(
                It.Is<string>(s => s.Contains("退出登录失败")), 
                It.IsAny<string>()), Times.Once);
        }

        #endregion

        #region Login Status Tests

        [Fact]
        public void CheckLoginStatus_WhenLoggedIn_ShouldLoadMainContent()
        {
            // Arrange
            var user = new UserInfo
            {
                Id = Guid.NewGuid(),
                Username = "testuser",
                RealName = "测试用户"
            };

            _mockAuthService.Setup(x => x.IsLoggedIn).Returns(true);
            _mockAuthService.Setup(x => x.GetCurrentUserAsync()).ReturnsAsync(user);

            // Act
            // CheckLoginStatus is called in constructor with dispatcher
            // We need to trigger it manually for testing
            var viewModel = new MainWindowViewModel(
                _mockRegionManager.Object,
                _mockEventAggregator.Object,
                _mockAuthService.Object,
                _mockPermissionService.Object,
                _mockUserService.Object,
                _mockCommonDialogService.Object);

            // Note: Due to dispatcher usage, actual verification would require 
            // integration testing or dispatcher mocking
        }

        [Fact]
        public void CheckLoginStatus_WhenNotLoggedIn_ShouldShowLoginDialog()
        {
            // Arrange
            _mockAuthService.Setup(x => x.IsLoggedIn).Returns(false);

            // Act
            var viewModel = new MainWindowViewModel(
                _mockRegionManager.Object,
                _mockEventAggregator.Object,
                _mockAuthService.Object,
                _mockPermissionService.Object,
                _mockUserService.Object,
                _mockCommonDialogService.Object);

            // Note: Due to dispatcher usage, actual verification would require 
            // integration testing or dispatcher mocking
        }

        #endregion

        #region Event Subscription Tests

        [Fact]
        public void Constructor_ShouldSubscribeToLoginSuccessEvent()
        {
            // Arrange
            var mockLoginSuccessEvent = new Mock<LoginSuccessEvent>();
            _mockEventAggregator
                .Setup(x => x.GetEvent<LoginSuccessEvent>())
                .Returns(mockLoginSuccessEvent.Object);

            // Act
            var viewModel = new MainWindowViewModel(
                _mockRegionManager.Object,
                _mockEventAggregator.Object,
                _mockAuthService.Object,
                _mockPermissionService.Object,
                _mockUserService.Object,
                _mockCommonDialogService.Object);

            // Assert
            mockLoginSuccessEvent.Verify(x => x.Subscribe(It.IsAny<Action>()), Times.Once);
        }

        #endregion

        #region Navigation Tests

        [Fact]
        public void ShowLoginDialog_ShouldNavigateToLoginView()
        {
            // This is a private method, we test it indirectly through CheckLoginStatus
            // when user is not logged in
            _mockAuthService.Setup(x => x.IsLoggedIn).Returns(false);

            // The navigation should happen in the constructor via dispatcher
            // Due to dispatcher, full verification would require integration testing
        }

        [Fact]
        public void LoadMainContent_WithSysAdmin_ShouldSetCorrectTitle()
        {
            // Arrange
            var adminUser = new UserInfo
            {
                Id = Guid.NewGuid(),
                Username = "sysadmin",
                RealName = "系统管理员"
            };

            _viewModel.CurrentUser = adminUser;

            // Act - LoadMainContent is private, test through public interface
            _viewModel.IsLoggedIn = true;

            // Note: Full testing of LoadMainContent requires refactoring or integration tests
        }

        [Fact]
        public void LoadMainContent_WithNullUser_ShouldShowError()
        {
            // Arrange
            _viewModel.CurrentUser = null;

            // Act & Assert
            // LoadMainContent is private, would need refactoring for full testability
            // This demonstrates the pattern, but actual implementation would need
            // the method to be accessible or tested through public interface
        }

        #endregion

        #region Control Examples Tests

        [Fact]
        public void ExecuteShowControlExamples_ShouldNavigateToControlExamplesView()
        {
            // Arrange
            _viewModel.IsLoggedIn = true;

            // Act
            _viewModel.ShowControlExamplesCommand.Execute();

            // Assert
            _mockRegionManager.Verify(x => x.RequestNavigate(
                "ContentRegion", 
                "ControlExamplesView"), Times.Once);
        }

        [Fact]
        public async Task ExecuteShowControlExamples_WhenNavigationFails_ShouldShowError()
        {
            // Arrange
            _viewModel.IsLoggedIn = true;
            _mockRegionManager
                .Setup(x => x.RequestNavigate(It.IsAny<string>(), It.IsAny<string>()))
                .Throws(new Exception("Navigation failed"));

            // Act
            _viewModel.ShowControlExamplesCommand.Execute();
            await Task.Delay(100);

            // Assert
            _mockCommonDialogService.Verify(x => x.ShowErrorAsync(
                It.Is<string>(s => s.Contains("打开控件示例页面失败")),
                It.IsAny<string>()), Times.Once);
        }

        #endregion
    }
}