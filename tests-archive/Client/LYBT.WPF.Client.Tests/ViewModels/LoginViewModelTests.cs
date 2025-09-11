using System;
using System.Threading.Tasks;
using FluentAssertions;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Modules.Authentication.ViewModels;
using LYBT.WPF.Client.Services;
using LYBT.Shared.Models.Auth;
using LYBT.WPF.Client.Core.Events;
using LYBT.WPF.Client.Core.Models;
using Moq;
using Prism.Events;
using Xunit;
using System.ComponentModel;

namespace LYBT.WPF.Client.Tests.ViewModels
{
    /// <summary>
    /// LoginViewModel 单元测试
    /// 测试登录视图模型的核心功能，包括属性绑定、命令执行、事件处理和业务逻辑
    /// </summary>
    public class LoginViewModelTests : IDisposable
    {
        private readonly Mock<IAuthenticationService> _mockAuthService;
        private readonly Mock<ICredentialService> _mockCredentialService;
        private readonly Mock<IEventAggregator> _mockEventAggregator;
        private readonly Mock<LoginSuccessEvent> _mockLoginSuccessEvent;
        private readonly Mock<LogoutEvent> _mockLogoutEvent;
        private readonly LoginViewModel _viewModel;
        private readonly List<string> _propertyChangedEvents;

        public LoginViewModelTests()
        {
            _mockAuthService = new Mock<IAuthenticationService>();
            _mockCredentialService = new Mock<ICredentialService>();
            _mockEventAggregator = new Mock<IEventAggregator>();
            _mockLoginSuccessEvent = new Mock<LoginSuccessEvent>();
            _mockLogoutEvent = new Mock<LogoutEvent>();
            _propertyChangedEvents = new List<string>();

            // 设置事件聚合器的返回值
            _mockEventAggregator.Setup(x => x.GetEvent<LoginSuccessEvent>())
                .Returns(_mockLoginSuccessEvent.Object);
            _mockEventAggregator.Setup(x => x.GetEvent<LogoutEvent>())
                .Returns(_mockLogoutEvent.Object);

            // 创建 ViewModel 实例
            _viewModel = new LoginViewModel(
                _mockEventAggregator.Object,
                _mockAuthService.Object,
                _mockCredentialService.Object);

            // 订阅属性变更事件
            _viewModel.PropertyChanged += (s, e) => _propertyChangedEvents.Add(e.PropertyName ?? "");
        }

        public void Dispose()
        {
            _viewModel?.Dispose();
        }

        #region 构造函数测试

        [Fact]
        public void Constructor_WithNullAuthService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new LoginViewModel(
                _mockEventAggregator.Object,
                null!,
                _mockCredentialService.Object));
        }

        [Fact]
        public void Constructor_WithNullCredentialService_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new LoginViewModel(
                _mockEventAggregator.Object,
                _mockAuthService.Object,
                null!));
        }

        [Fact]
        public void Constructor_WithValidParameters_InitializesCorrectly()
        {
            // Act & Assert
            _viewModel.Should().NotBeNull();
            _viewModel.LoginCommand.Should().NotBeNull();
            _viewModel.Username.Should().BeEmpty();
            _viewModel.Password.Should().BeEmpty();
            _viewModel.RememberMe.Should().BeFalse();
            _viewModel.IsLoading.Should().BeFalse();
        }

        [Fact]
        public void Constructor_SubscribesToLogoutEvent()
        {
            // Assert
            _mockLogoutEvent.Verify(x => x.Subscribe(It.IsAny<Action>(), ThreadOption.UIThread), Times.Once);
        }

        [Fact]
        public void Constructor_LoadsSavedCredentials()
        {
            // Assert
            _mockCredentialService.Verify(x => x.LoadCredentials(), Times.AtLeastOnce);
        }

        #endregion

        #region 属性测试

        [Fact]
        public void Username_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            _propertyChangedEvents.Clear();
            const string testUsername = "testuser";

            // Act
            _viewModel.Username = testUsername;

            // Assert
            _viewModel.Username.Should().Be(testUsername);
            _propertyChangedEvents.Should().Contain(nameof(LoginViewModel.Username));
        }

        [Fact]
        public void Username_SetValue_RaisesCanExecuteChangedOnLoginCommand()
        {
            // Arrange
            var canExecuteChangedCount = 0;
            _viewModel.LoginCommand.CanExecuteChanged += (s, e) => canExecuteChangedCount++;

            // Act
            _viewModel.Username = "testuser";

            // Assert
            canExecuteChangedCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public void Password_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            _propertyChangedEvents.Clear();
            const string testPassword = "testpassword";

            // Act
            _viewModel.Password = testPassword;

            // Assert
            _viewModel.Password.Should().Be(testPassword);
            _propertyChangedEvents.Should().Contain(nameof(LoginViewModel.Password));
        }

        [Fact]
        public void Password_SetValue_RaisesCanExecuteChangedOnLoginCommand()
        {
            // Arrange
            var canExecuteChangedCount = 0;
            _viewModel.LoginCommand.CanExecuteChanged += (s, e) => canExecuteChangedCount++;

            // Act
            _viewModel.Password = "testpassword";

            // Assert
            canExecuteChangedCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public void RememberMe_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            _propertyChangedEvents.Clear();

            // Act
            _viewModel.RememberMe = true;

            // Assert
            _viewModel.RememberMe.Should().BeTrue();
            _propertyChangedEvents.Should().Contain(nameof(LoginViewModel.RememberMe));
        }

        [Fact]
        public void HasSavedPassword_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            _propertyChangedEvents.Clear();

            // Act
            _viewModel.HasSavedPassword = true;

            // Assert
            _viewModel.HasSavedPassword.Should().BeTrue();
            _propertyChangedEvents.Should().Contain(nameof(LoginViewModel.HasSavedPassword));
        }

        [Fact]
        public void IsApiOnline_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            _propertyChangedEvents.Clear();

            // Act
            _viewModel.IsApiOnline = true;

            // Assert
            _viewModel.IsApiOnline.Should().BeTrue();
            _propertyChangedEvents.Should().Contain(nameof(LoginViewModel.IsApiOnline));
        }

        [Fact]
        public void IsApiOnline_SetValue_RaisesCanExecuteChangedOnLoginCommand()
        {
            // Arrange
            var canExecuteChangedCount = 0;
            _viewModel.LoginCommand.CanExecuteChanged += (s, e) => canExecuteChangedCount++;

            // Act
            _viewModel.IsApiOnline = true;

            // Assert
            canExecuteChangedCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public void ApiStatus_SetValue_RaisesPropertyChanged()
        {
            // Arrange
            _propertyChangedEvents.Clear();
            const string testStatus = "✅ API连接正常";

            // Act
            _viewModel.ApiStatus = testStatus;

            // Assert
            _viewModel.ApiStatus.Should().Be(testStatus);
            _propertyChangedEvents.Should().Contain(nameof(LoginViewModel.ApiStatus));
        }

        #endregion

        #region LoginCommand CanExecute 测试

        [Fact]
        public void LoginCommand_CanExecute_WhenIsLoading_ReturnsFalse()
        {
            // Arrange
            _viewModel.Username = "testuser";
            _viewModel.Password = "testpassword";
            _viewModel.IsApiOnline = true;
            _viewModel.IsLoading = true;

            // Act
            var canExecute = _viewModel.LoginCommand.CanExecute();

            // Assert
            canExecute.Should().BeFalse();
        }

        [Fact]
        public void LoginCommand_CanExecute_WhenUsernameEmpty_ReturnsFalse()
        {
            // Arrange
            _viewModel.Username = "";
            _viewModel.Password = "testpassword";
            _viewModel.IsApiOnline = true;
            _viewModel.IsLoading = false;

            // Act
            var canExecute = _viewModel.LoginCommand.CanExecute();

            // Assert
            canExecute.Should().BeFalse();
        }

        [Fact]
        public void LoginCommand_CanExecute_WhenPasswordEmpty_ReturnsFalse()
        {
            // Arrange
            _viewModel.Username = "testuser";
            _viewModel.Password = "";
            _viewModel.IsApiOnline = true;
            _viewModel.IsLoading = false;

            // Act
            var canExecute = _viewModel.LoginCommand.CanExecute();

            // Assert
            canExecute.Should().BeFalse();
        }

        [Fact]
        public void LoginCommand_CanExecute_WhenApiOffline_ReturnsFalse()
        {
            // Arrange
            _viewModel.Username = "testuser";
            _viewModel.Password = "testpassword";
            _viewModel.IsApiOnline = false;
            _viewModel.IsLoading = false;

            // Act
            var canExecute = _viewModel.LoginCommand.CanExecute();

            // Assert
            canExecute.Should().BeFalse();
        }

        [Fact]
        public void LoginCommand_CanExecute_WhenAllConditionsMet_ReturnsTrue()
        {
            // Arrange
            _viewModel.Username = "testuser";
            _viewModel.Password = "testpassword";
            _viewModel.IsApiOnline = true;
            _viewModel.IsLoading = false;

            // Act
            var canExecute = _viewModel.LoginCommand.CanExecute();

            // Assert
            canExecute.Should().BeTrue();
        }

        [Theory]
        [InlineData("", "password")]  // 空用户名
        [InlineData("   ", "password")]  // 空白用户名
        [InlineData("user", "")]  // 空密码
        [InlineData("user", "   ")]  // 空白密码
        public void LoginCommand_CanExecute_WithInvalidInput_ReturnsFalse(string username, string password)
        {
            // Arrange
            _viewModel.Username = username;
            _viewModel.Password = password;
            _viewModel.IsApiOnline = true;
            _viewModel.IsLoading = false;

            // Act
            var canExecute = _viewModel.LoginCommand.CanExecute();

            // Assert
            canExecute.Should().BeFalse();
        }

        #endregion

        #region LoginCommand Execute 成功场景测试

        [Fact]
        public async Task LoginCommand_Execute_WithValidCredentials_CallsAuthService()
        {
            // Arrange
            _viewModel.Username = "testuser";
            _viewModel.Password = "testpassword";
            _viewModel.RememberMe = true;

            var loginResponse = new ServiceResult<LoginResponse>
            {
                IsSuccess = true,
                Data = new LoginResponse
                {
                    Token = "test-token",
                    User = new UserInfo { Username = "testuser", Id = Guid.NewGuid() }
                }
            };

            _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
                .ReturnsAsync(loginResponse);

            // Act
            await Task.Run(() => _viewModel.LoginCommand.Execute());
            await Task.Delay(100); // 等待异步操作完成

            // Assert
            _mockAuthService.Verify(x => x.LoginAsync(It.Is<LoginRequest>(r =>
                r.Username == "testuser" &&
                r.Password == "testpassword" &&
                r.RememberMe == true &&
                r.LoginType == "Password")), Times.Once);
        }

        [Fact]
        public async Task LoginCommand_Execute_WithSuccessfulLogin_SavesCredentials()
        {
            // Arrange
            _viewModel.Username = "testuser";
            _viewModel.Password = "testpassword";
            _viewModel.RememberMe = true;

            var loginResponse = new ServiceResult<LoginResponse>
            {
                IsSuccess = true,
                Data = new LoginResponse
                {
                    Token = "test-token",
                    User = new UserInfo { Username = "testuser", Id = Guid.NewGuid() }
                }
            };

            _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
                .ReturnsAsync(loginResponse);

            // Act
            await Task.Run(() => _viewModel.LoginCommand.Execute());
            await Task.Delay(100); // 等待异步操作完成

            // Assert
            _mockCredentialService.Verify(x => x.SaveCredentials("testuser", "testpassword", true), Times.Once);
        }

        [Fact]
        public async Task LoginCommand_Execute_WithSuccessfulLogin_PublishesLoginSuccessEvent()
        {
            // Arrange
            _viewModel.Username = "testuser";
            _viewModel.Password = "testpassword";

            var loginResponse = new ServiceResult<LoginResponse>
            {
                IsSuccess = true,
                Data = new LoginResponse
                {
                    Token = "test-token",
                    User = new UserInfo { Username = "testuser", Id = Guid.NewGuid() }
                }
            };

            _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
                .ReturnsAsync(loginResponse);

            // Act
            await Task.Run(() => _viewModel.LoginCommand.Execute());
            await Task.Delay(1100); // 等待异步操作完成（包括1秒延迟）

            // Assert
            _mockLoginSuccessEvent.Verify(x => x.Publish(), Times.Once);
        }

        [Fact]
        public async Task LoginCommand_Execute_WithSysadminUser_ShowsAdminMessage()
        {
            // Arrange
            _viewModel.Username = "sysadmin";
            _viewModel.Password = "password";

            var loginResponse = new ServiceResult<LoginResponse>
            {
                IsSuccess = true,
                Data = new LoginResponse
                {
                    Token = "test-token",
                    User = new UserInfo { Username = "sysadmin", Id = Guid.NewGuid() }
                }
            };

            _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
                .ReturnsAsync(loginResponse);

            // Act
            await Task.Run(() => _viewModel.LoginCommand.Execute());
            await Task.Delay(100); // 等待异步操作完成

            // Assert
            _viewModel.StatusMessage.Should().Contain("超级管理员登录成功");
        }

        [Fact]
        public async Task LoginCommand_Execute_WithRegularUser_ShowsUserMessage()
        {
            // Arrange
            _viewModel.Username = "regularuser";
            _viewModel.Password = "password";

            var loginResponse = new ServiceResult<LoginResponse>
            {
                IsSuccess = true,
                Data = new LoginResponse
                {
                    Token = "test-token",
                    User = new UserInfo { Username = "regularuser", Id = Guid.NewGuid() }
                }
            };

            _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
                .ReturnsAsync(loginResponse);

            // Act
            await Task.Run(() => _viewModel.LoginCommand.Execute());
            await Task.Delay(100); // 等待异步操作完成

            // Assert
            _viewModel.StatusMessage.Should().Contain("用户登录成功");
        }

        [Fact]
        public async Task LoginCommand_Execute_SetsLoadingState()
        {
            // Arrange
            _viewModel.Username = "testuser";
            _viewModel.Password = "testpassword";
            var loadingStates = new List<bool>();

            _viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(LoginViewModel.IsLoading))
                {
                    loadingStates.Add(_viewModel.IsLoading);
                }
            };

            var loginResponse = new ServiceResult<LoginResponse>
            {
                IsSuccess = true,
                Data = new LoginResponse
                {
                    Token = "test-token",
                    User = new UserInfo { Username = "testuser", Id = Guid.NewGuid() }
                }
            };

            _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
                .ReturnsAsync(loginResponse);

            // Act
            await Task.Run(() => _viewModel.LoginCommand.Execute());
            await Task.Delay(100); // 等待异步操作完成

            // Assert
            loadingStates.Should().HaveCount(2);
            loadingStates[0].Should().BeTrue();  // 开始时设置为 true
            loadingStates[1].Should().BeFalse(); // 结束时设置为 false
        }

        #endregion

        #region LoginCommand Execute 失败场景测试

        [Fact]
        public async Task LoginCommand_Execute_WithEmptyUsername_ShowsErrorMessage()
        {
            // Arrange
            _viewModel.Username = "";
            _viewModel.Password = "password";

            // Act
            await Task.Run(() => _viewModel.LoginCommand.Execute());

            // Assert
            _viewModel.ErrorMessage.Should().Be("请输入用户名");
        }

        [Fact]
        public async Task LoginCommand_Execute_WithEmptyPassword_ShowsErrorMessage()
        {
            // Arrange
            _viewModel.Username = "testuser";
            _viewModel.Password = "";

            // Act
            await Task.Run(() => _viewModel.LoginCommand.Execute());

            // Assert
            _viewModel.ErrorMessage.Should().Be("请输入密码");
        }

        [Fact]
        public async Task LoginCommand_Execute_WithFailedLogin_ShowsErrorMessage()
        {
            // Arrange
            _viewModel.Username = "testuser";
            _viewModel.Password = "wrongpassword";

            var loginResponse = new ServiceResult<LoginResponse>
            {
                IsSuccess = false,
                ErrorMessage = "用户名或密码错误"
            };

            _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
                .ReturnsAsync(loginResponse);

            // Act
            await Task.Run(() => _viewModel.LoginCommand.Execute());
            await Task.Delay(100); // 等待异步操作完成

            // Assert
            _viewModel.ErrorMessage.Should().Be("用户名或密码错误");
        }

        [Fact]
        public async Task LoginCommand_Execute_WithFailedLoginAndNoErrorMessage_ShowsDefaultError()
        {
            // Arrange
            _viewModel.Username = "testuser";
            _viewModel.Password = "wrongpassword";

            var loginResponse = new ServiceResult<LoginResponse>
            {
                IsSuccess = false,
                ErrorMessage = null
            };

            _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
                .ReturnsAsync(loginResponse);

            // Act
            await Task.Run(() => _viewModel.LoginCommand.Execute());
            await Task.Delay(100); // 等待异步操作完成

            // Assert
            _viewModel.ErrorMessage.Should().Be("登录失败，请检查用户名和密码");
        }

        [Fact]
        public async Task LoginCommand_Execute_WithException_HandlesError()
        {
            // Arrange
            _viewModel.Username = "testuser";
            _viewModel.Password = "password";

            _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
                .ThrowsAsync(new Exception("网络连接错误"));

            // Act
            await Task.Run(() => _viewModel.LoginCommand.Execute());
            await Task.Delay(100); // 等待异步操作完成

            // Assert
            _viewModel.ErrorMessage.Should().Contain("登录: 网络连接错误");
            _viewModel.IsLoading.Should().BeFalse();
        }

        #endregion

        #region 凭据加载测试

        [Fact]
        public void LoadSavedCredentials_WithValidCredentials_UpdatesProperties()
        {
            // Arrange
            var savedCredentials = new SavedCredentials
            {
                Username = "saveduser",
                Password = "savedpassword",
                RememberMe = true
            };

            _mockCredentialService.Setup(x => x.LoadCredentials())
                .Returns(savedCredentials);

            // 创建新的 ViewModel 来测试构造函数中的加载逻辑
            var newViewModel = new LoginViewModel(
                _mockEventAggregator.Object,
                _mockAuthService.Object,
                _mockCredentialService.Object);

            // Assert
            newViewModel.Username.Should().Be("saveduser");
            newViewModel.Password.Should().Be("savedpassword");
            newViewModel.RememberMe.Should().BeTrue();
            newViewModel.HasSavedPassword.Should().BeTrue();

            newViewModel.Dispose();
        }

        [Fact]
        public void LoadSavedCredentials_WithNoCredentials_SetsDefaultValues()
        {
            // Arrange
            _mockCredentialService.Setup(x => x.LoadCredentials())
                .Returns((SavedCredentials?)null);

            // 创建新的 ViewModel 来测试构造函数中的加载逻辑
            var newViewModel = new LoginViewModel(
                _mockEventAggregator.Object,
                _mockAuthService.Object,
                _mockCredentialService.Object);

            // Assert
            newViewModel.HasSavedPassword.Should().BeFalse();

            newViewModel.Dispose();
        }

        [Fact]
        public void LoadSavedCredentials_WithException_HandlesGracefully()
        {
            // Arrange
            _mockCredentialService.Setup(x => x.LoadCredentials())
                .Throws(new Exception("File access error"));

            // 创建新的 ViewModel 不应该抛出异常
            var newViewModel = new LoginViewModel(
                _mockEventAggregator.Object,
                _mockAuthService.Object,
                _mockCredentialService.Object);

            // Assert
            newViewModel.HasSavedPassword.Should().BeFalse();

            newViewModel.Dispose();
        }

        #endregion

        #region LogoutEvent 处理测试

        [Fact]
        public void OnLogout_ClearsErrorsAndStatus()
        {
            // Arrange - 设置事件回调捕获
            Action? logoutCallback = null;
            var mockLogoutEvent = new Mock<LogoutEvent>();
            mockLogoutEvent.Setup(x => x.Subscribe(It.IsAny<Action>(), ThreadOption.UIThread))
                .Callback<Action, ThreadOption>((action, _) => logoutCallback = action);

            var mockEventAggregator = new Mock<IEventAggregator>();
            mockEventAggregator.Setup(x => x.GetEvent<LogoutEvent>())
                .Returns(mockLogoutEvent.Object);
            mockEventAggregator.Setup(x => x.GetEvent<LoginSuccessEvent>())
                .Returns(_mockLoginSuccessEvent.Object);

            // 创建ViewModel
            var testViewModel = new LoginViewModel(
                mockEventAggregator.Object,
                _mockAuthService.Object,
                _mockCredentialService.Object);

            testViewModel.ErrorMessage = "Test error";
            testViewModel.StatusMessage = "Test status";

            // Act - 触发登出事件
            logoutCallback?.Invoke();

            // Assert
            testViewModel.ErrorMessage.Should().BeEmpty();
            testViewModel.StatusMessage.Should().BeEmpty();
            testViewModel.HasError.Should().BeFalse();

            testViewModel.Dispose();
        }

        [Fact]
        public void OnLogout_ReloadsCredentials()
        {
            // Arrange
            Action? logoutCallback = null;
            var mockCredentialService = new Mock<ICredentialService>();
            var mockLogoutEvent = new Mock<LogoutEvent>();

            mockLogoutEvent.Setup(x => x.Subscribe(It.IsAny<Action>(), ThreadOption.UIThread))
                .Callback<Action, ThreadOption>((action, _) => logoutCallback = action);

            var mockEventAggregator = new Mock<IEventAggregator>();
            mockEventAggregator.Setup(x => x.GetEvent<LogoutEvent>())
                .Returns(mockLogoutEvent.Object);
            mockEventAggregator.Setup(x => x.GetEvent<LoginSuccessEvent>())
                .Returns(_mockLoginSuccessEvent.Object);

            var testViewModel = new LoginViewModel(
                mockEventAggregator.Object,
                _mockAuthService.Object,
                mockCredentialService.Object);

            // Act
            logoutCallback?.Invoke();

            // Assert
            mockCredentialService.Verify(x => x.LoadCredentials(), Times.AtLeastOnce);

            testViewModel.Dispose();
        }

        #endregion

        #region API连接检测测试

        [Fact]
        public async Task CheckApiConnection_WithSuccessfulConnection_UpdatesStatus()
        {
            // Arrange
            _mockAuthService.Setup(x => x.CheckConnectionAsync())
                .ReturnsAsync(true);

            // 创建新的ViewModel以触发初始连接检测
            using var testViewModel = new LoginViewModel(
                _mockEventAggregator.Object,
                _mockAuthService.Object,
                _mockCredentialService.Object);

            // 等待初始连接检测完成
            await Task.Delay(300);

            // Assert
            testViewModel.IsApiOnline.Should().BeTrue();
            testViewModel.ApiStatus.Should().Be("✅ API连接正常");
        }

        [Fact]
        public async Task CheckApiConnection_WithFailedConnection_UpdatesStatus()
        {
            // Arrange
            _mockAuthService.Setup(x => x.CheckConnectionAsync())
                .ReturnsAsync(false);

            // 创建新的ViewModel以触发初始连接检测
            using var testViewModel = new LoginViewModel(
                _mockEventAggregator.Object,
                _mockAuthService.Object,
                _mockCredentialService.Object);

            // 等待初始连接检测完成
            await Task.Delay(300);

            // Assert
            testViewModel.IsApiOnline.Should().BeFalse();
            testViewModel.ApiStatus.Should().Be("❌ API服务不可用");
        }

        [Fact]
        public async Task CheckApiConnection_WithException_HandlesError()
        {
            // Arrange
            _mockAuthService.Setup(x => x.CheckConnectionAsync())
                .ThrowsAsync(new Exception("连接超时"));

            // 创建新的ViewModel以触发初始连接检测
            using var testViewModel = new LoginViewModel(
                _mockEventAggregator.Object,
                _mockAuthService.Object,
                _mockCredentialService.Object);

            // 等待初始连接检测完成
            await Task.Delay(300);

            // Assert
            testViewModel.IsApiOnline.Should().BeFalse();
            testViewModel.ApiStatus.Should().Contain("❌ 连接失败: 连接超时");
        }

        #endregion

        #region IsLoading状态变化测试

        [Fact]
        public void IsLoading_StateChange_RaisesCanExecuteChangedOnLoginCommand()
        {
            // Arrange
            var canExecuteChangedCount = 0;
            _viewModel.LoginCommand.CanExecuteChanged += (s, e) => canExecuteChangedCount++;

            // Act
            _viewModel.IsLoading = true;
            _viewModel.IsLoading = false;

            // Assert - 只需要验证有事件被触发，不用要求具体次数
            canExecuteChangedCount.Should().BeGreaterThan(0);
        }

        #endregion

        #region 资源清理测试

        [Fact]
        public void Dispose_DisposesResources()
        {
            // Arrange
            var testViewModel = new LoginViewModel(
                _mockEventAggregator.Object,
                _mockAuthService.Object,
                _mockCredentialService.Object);

            // Act
            testViewModel.Dispose();

            // Assert - 不应该抛出异常
            // 这主要测试 Dispose 方法是否正确实现
        }

        #endregion

        #region 边界条件测试

        [Theory]
        [InlineData("user", "   ")]  // 密码只有空白字符
        [InlineData("   ", "pass")]  // 用户名只有空白字符
        [InlineData("user", "\t")]   // 密码包含制表符
        [InlineData("\n", "pass")]   // 用户名包含换行符
        public void LoginCommand_Execute_WithWhitespaceInput_ShowsAppropriateError(string username, string password)
        {
            // Arrange
            _viewModel.Username = username;
            _viewModel.Password = password;

            // Act
            _viewModel.LoginCommand.Execute();

            // Assert
            if (string.IsNullOrWhiteSpace(username))
            {
                _viewModel.ErrorMessage.Should().Be("请输入用户名");
            }
            else if (string.IsNullOrWhiteSpace(password))
            {
                _viewModel.ErrorMessage.Should().Be("请输入密码");
            }
        }

        [Fact]
        public async Task LoginCommand_Execute_MultipleRapidCalls_HandledCorrectly()
        {
            // Arrange
            _viewModel.Username = "testuser";
            _viewModel.Password = "testpassword";

            var loginResponse = new ServiceResult<LoginResponse>
            {
                IsSuccess = true,
                Data = new LoginResponse
                {
                    Token = "test-token",
                    User = new UserInfo { Username = "testuser", Id = Guid.NewGuid() }
                }
            };

            _mockAuthService.Setup(x => x.LoginAsync(It.IsAny<LoginRequest>()))
                .ReturnsAsync(loginResponse);

            // Act - 快速多次调用
            _viewModel.LoginCommand.Execute();
            _viewModel.LoginCommand.Execute();
            _viewModel.LoginCommand.Execute();

            await Task.Delay(100); // 等待异步操作

            // Assert - 由于IsLoading状态，应该只调用一次
            _mockAuthService.Verify(x => x.LoginAsync(It.IsAny<LoginRequest>()), Times.Once);
        }

        #endregion
    }
}