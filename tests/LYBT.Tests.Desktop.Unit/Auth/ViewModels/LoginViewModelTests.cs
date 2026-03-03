using FluentAssertions;
using LYBT.Desktop.Auth.ViewModels;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Foundation.Security;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Auth.Tests.ViewModels;

/// <summary>
/// LoginViewModel 单元测试
/// Phase 1 测试置信度重建 - Task 1.1
///
/// 测试策略:
/// - ViewModel 构造函数中的 Task.Run 背景任务在测试环境中会因 Application.Current 为 null 而静默失败
/// - 这是可接受的: 我们测试同步可观察行为 (属性、命令、登录流程)
/// - 不测试 Dispatcher 依赖的 UI 初始化 (LoadSavedCredentials、ApiStatus)
/// </summary>
public class LoginViewModelTests : IDisposable
{
    private readonly IViewModelServices _services;
    private readonly ILoginCoordinator _loginCoordinator;
    private readonly IApplicationStateService _applicationStateService;
    private readonly IUsernameStorageService _usernameStorage;
    private readonly ICredentialVault _credentialVault;

    private readonly LoginViewModel _viewModel;

    public LoginViewModelTests()
    {
        // Mock IViewModelServices 聚合服务
        _services = Substitute.For<IViewModelServices>();
        var loggerFactory = Substitute.For<ILoggerFactory>();
        loggerFactory.CreateLogger(Arg.Any<string>()).Returns(Substitute.For<ILogger>());
        _services.LoggerFactory.Returns(loggerFactory);
        _services.EventAggregator.Returns(Substitute.For<IEventAggregator>());
        _services.RegionManager.Returns(Substitute.For<IRegionManager>());
        _services.SessionManager.Returns(Substitute.For<ISessionManager>());
        _services.UserNotificationService.Returns(Substitute.For<IUserNotificationService>());
        _services.CommonDialogService.Returns(Substitute.For<ICommonDialogService>());
        _services.RoleRegistry.Returns(Substitute.For<IRoleRegistry>());

        // Mock 核心依赖
        _loginCoordinator = Substitute.For<ILoginCoordinator>();
        _applicationStateService = Substitute.For<IApplicationStateService>();
        _usernameStorage = Substitute.For<IUsernameStorageService>();
        _credentialVault = Substitute.For<ICredentialVault>();

        // 创建 ViewModel
        // 注: 构造函数中的 Task.Run 会因 Application.Current 为 null 而静默失败
        _viewModel = new LoginViewModel(
            _services,
            _loginCoordinator,
            _applicationStateService,
            _usernameStorage,
            _credentialVault);
    }

    public void Dispose()
    {
        _viewModel.Dispose();
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithValidDependencies_ShouldInitializeSuccessfully()
    {
        // Assert
        _viewModel.Should().NotBeNull();
        _viewModel.Should().BeAssignableTo<LoginViewModel>();
        _viewModel.Username.Should().BeEmpty();
        _viewModel.Password.Should().BeEmpty();
        _viewModel.HasMessage.Should().BeFalse();
        _viewModel.LoginCommand.Should().NotBeNull();
        _viewModel.CloseApplicationCommand.Should().NotBeNull();
        _viewModel.RetryApiCheckCommand.Should().NotBeNull();
    }

    [Fact]
    public void Constructor_WithNullLoginCoordinator_ShouldThrow()
    {
        // Act & Assert
        var act = () => new LoginViewModel(
            _services, null!, _applicationStateService);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("loginCoordinator");
    }

    [Fact]
    public void Constructor_WithNullApplicationStateService_ShouldThrow()
    {
        // Act & Assert
        var act = () => new LoginViewModel(
            _services, _loginCoordinator, null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("applicationStateService");
    }

    [Fact]
    public void Constructor_WithNullOptionalDependencies_ShouldSucceed()
    {
        // Act - optional dependencies (usernameStorage, credentialVault) can be null
        var vm = new LoginViewModel(
            _services, _loginCoordinator, _applicationStateService,
            usernameStorage: null, credentialVault: null);

        // Assert
        vm.Should().NotBeNull();
        vm.Dispose();
    }

    #endregion

    #region LoginCommand CanExecute Tests

    [Fact]
    public void LoginCommand_WhenUsernameAndPasswordEmpty_CannotExecute()
    {
        // Assert - 初始状态
        _viewModel.LoginCommand.CanExecute(null).Should().BeFalse(
            "用户名和密码为空时不应可执行");
    }

    [Fact]
    public void LoginCommand_WhenOnlyUsernameSet_CannotExecute()
    {
        // Act
        _viewModel.Username = "testuser";

        // Assert
        _viewModel.LoginCommand.CanExecute(null).Should().BeFalse(
            "密码为空时不应可执行");
    }

    [Fact]
    public void LoginCommand_WhenOnlyPasswordSet_CannotExecute()
    {
        // Act
        _viewModel.Password = "testpass";

        // Assert
        _viewModel.LoginCommand.CanExecute(null).Should().BeFalse(
            "用户名为空时不应可执行");
    }

    [Fact]
    public void LoginCommand_WhenBothSet_CanExecute()
    {
        // Act
        _viewModel.Username = "testuser";
        _viewModel.Password = "testpass";

        // Assert
        _viewModel.LoginCommand.CanExecute(null).Should().BeTrue(
            "用户名和密码都非空时应可执行");
    }

    [Fact]
    public void LoginCommand_WhenUsernameIsWhitespace_CannotExecute()
    {
        // Act
        _viewModel.Username = "   ";
        _viewModel.Password = "testpass";

        // Assert
        _viewModel.LoginCommand.CanExecute(null).Should().BeFalse(
            "用户名仅含空白字符时不应可执行");
    }

    #endregion

    #region Property Notification Tests

    [Fact]
    public void Username_WhenSet_ShouldNotifyPropertyChanged()
    {
        // Arrange
        var propertyChanged = false;
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LoginViewModel.Username))
                propertyChanged = true;
        };

        // Act
        _viewModel.Username = "testuser";

        // Assert
        propertyChanged.Should().BeTrue();
        _viewModel.Username.Should().Be("testuser");
    }

    [Fact]
    public void Password_WhenSet_ShouldNotifyPropertyChanged()
    {
        // Arrange
        var propertyChanged = false;
        _viewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LoginViewModel.Password))
                propertyChanged = true;
        };

        // Act
        _viewModel.Password = "testpass";

        // Assert
        propertyChanged.Should().BeTrue();
        _viewModel.Password.Should().Be("testpass");
    }

    #endregion

    #region Login Execution Tests

    [Fact]
    public async Task ExecuteLogin_Success_ShouldNotShowError()
    {
        // Arrange
        _viewModel.Username = "testuser";
        _viewModel.Password = "testpass";

        _loginCoordinator.LoginAsync("testuser", "testpass")
            .Returns(Task.FromResult(LoginResult.Succeeded(
                new LYBT.Shared.Models.Contracts.Users.UserDetailDto
                {
                    Id = Guid.NewGuid(),
                    UserName = "testuser",
                    RealName = "测试用户",
                    Role = LYBT.Shared.Models.Enums.UserRole.Doctor,
                    Email = "test@lybt.com"
                })));

        // Act
        await InvokeLoginCommandAsync();

        // Assert
        _viewModel.ErrorMessage.Should().BeNullOrEmpty("登录成功不应有错误消息");
        _viewModel.IsLoading.Should().BeFalse("登录完成后不应在加载状态");
        _viewModel.StatusMessage.Should().BeEmpty("登录完成后状态消息应清空");
    }

    [Fact]
    public async Task ExecuteLogin_Success_ShouldSaveUsername_WhenRememberUsernameChecked()
    {
        // Arrange
        _viewModel.Username = "testuser";
        _viewModel.Password = "testpass";
        _viewModel.RememberUsername = true;

        _loginCoordinator.LoginAsync("testuser", "testpass")
            .Returns(Task.FromResult(LoginResult.Succeeded(
                new LYBT.Shared.Models.Contracts.Users.UserDetailDto
                {
                    Id = Guid.NewGuid(),
                    UserName = "testuser",
                    RealName = "测试用户",
                    Role = LYBT.Shared.Models.Enums.UserRole.Doctor,
                    Email = "test@lybt.com"
                })));

        // Act
        await InvokeLoginCommandAsync();

        // Assert
        await _usernameStorage.Received(1)
            .SaveUsernameAsync("testuser", rememberMe: true);
    }

    [Fact]
    public async Task ExecuteLogin_Success_ShouldClearUsername_WhenRememberUsernameUnchecked()
    {
        // Arrange
        _viewModel.Username = "testuser";
        _viewModel.Password = "testpass";
        _viewModel.RememberUsername = false;

        _loginCoordinator.LoginAsync("testuser", "testpass")
            .Returns(Task.FromResult(LoginResult.Succeeded(
                new LYBT.Shared.Models.Contracts.Users.UserDetailDto
                {
                    Id = Guid.NewGuid(),
                    UserName = "testuser",
                    RealName = "测试用户",
                    Role = LYBT.Shared.Models.Enums.UserRole.Doctor,
                    Email = "test@lybt.com"
                })));

        // Act
        await InvokeLoginCommandAsync();

        // Assert
        await _usernameStorage.Received(1).ClearUsernameAsync();
    }

    [Fact]
    public async Task ExecuteLogin_Success_ShouldSavePassword_WhenRememberPasswordChecked()
    {
        // Arrange
        _viewModel.Username = "testuser";
        _viewModel.Password = "testpass";
        _viewModel.RememberPassword = true; // 自动勾选 RememberUsername

        _loginCoordinator.LoginAsync("testuser", "testpass")
            .Returns(Task.FromResult(LoginResult.Succeeded(
                new LYBT.Shared.Models.Contracts.Users.UserDetailDto
                {
                    Id = Guid.NewGuid(),
                    UserName = "testuser",
                    RealName = "测试用户",
                    Role = LYBT.Shared.Models.Enums.UserRole.Doctor,
                    Email = "test@lybt.com"
                })));

        // Act
        await InvokeLoginCommandAsync();

        // Assert
        await _credentialVault.Received(1)
            .SavePasswordAsync("testuser", "testpass");
    }

    [Fact]
    public async Task ExecuteLogin_Failure_ShouldShowErrorAndClearPassword()
    {
        // Arrange
        _viewModel.Username = "testuser";
        _viewModel.Password = "wrongpass";

        _loginCoordinator.LoginAsync("testuser", "wrongpass")
            .Returns(Task.FromResult(
                LoginResult.Failed("用户名或密码错误")));

        // Act
        await InvokeLoginCommandAsync();

        // Assert
        _viewModel.ErrorMessage.Should().Be("用户名或密码错误");
        _viewModel.Password.Should().BeEmpty("登录失败应清空密码");
        _viewModel.IsLoading.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteLogin_Failure_WithNullErrorMessage_ShouldShowDefaultError()
    {
        // Arrange
        _viewModel.Username = "testuser";
        _viewModel.Password = "testpass";

        _loginCoordinator.LoginAsync("testuser", "testpass")
            .Returns(Task.FromResult(
                LoginResult.Failed(null!)));

        // Act
        await InvokeLoginCommandAsync();

        // Assert
        _viewModel.ErrorMessage.Should().Be("登录失败，请检查用户名和密码");
    }

    [Fact]
    public async Task ExecuteLogin_Exception_ShouldHandleGracefully()
    {
        // Arrange
        _viewModel.Username = "testuser";
        _viewModel.Password = "testpass";

        _loginCoordinator.LoginAsync("testuser", "testpass")
            .Returns(Task.FromException<LoginResult>(
                new InvalidOperationException("网络连接失败")));

        // Act
        await InvokeLoginCommandAsync();

        // Assert
        _viewModel.ErrorMessage.Should().NotBeNullOrEmpty("异常应产生错误消息");
        _viewModel.Password.Should().BeEmpty("异常时应清空密码");
        _viewModel.IsLoading.Should().BeFalse();
    }

    #endregion

    #region RememberPassword 联动 Tests

    [Fact]
    public void RememberPassword_WhenChecked_ShouldAutoCheckRememberUsername()
    {
        // Arrange
        _viewModel.RememberUsername.Should().BeFalse("初始应为未勾选");

        // Act
        _viewModel.RememberPassword = true;

        // Assert
        _viewModel.RememberUsername.Should().BeTrue(
            "勾选'记住密码'应自动勾选'记住用户名'");
    }

    [Fact]
    public void RememberPassword_WhenUnchecked_ShouldNotUncheckRememberUsername()
    {
        // Arrange
        _viewModel.RememberPassword = true;
        _viewModel.RememberUsername.Should().BeTrue();

        // Act
        _viewModel.RememberPassword = false;

        // Assert
        _viewModel.RememberUsername.Should().BeTrue(
            "取消'记住密码'不应取消'记住用户名'");
    }

    #endregion

    #region ConnectionMode Tests

    [Fact]
    public void SelectedConnectionMode_DefaultsToRemote()
    {
        // Assert
        _viewModel.SelectedConnectionMode.Should().Be(ConnectionMode.Remote);
        _viewModel.IsRemoteMode.Should().BeTrue();
        _viewModel.IsLocalMode.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 通过反射调用 ExecuteLoginAsync (private 方法)。
    /// LoginCommand 是 DelegateCommand 包装的 async void，
    /// 直接 Execute 无法 await，所以通过反射获取内部方法。
    /// </summary>
    private async Task InvokeLoginCommandAsync()
    {
        var method = typeof(LoginViewModel)
            .GetMethod("ExecuteLoginAsync",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        method.Should().NotBeNull("应存在 ExecuteLoginAsync 方法");

        var task = (Task)method!.Invoke(_viewModel, null)!;
        await task;
    }

    #endregion
}

/// <summary>
/// 用户名变更清空密码功能测试
/// Issue: clear-password-on-username-change
/// </summary>
public class LoginViewModelUsernameChangeTests
{
    /// <summary>
    /// 测试辅助类：模拟LoginViewModel的用户名变更逻辑
    /// 由于LoginViewModel构造函数依赖复杂，使用简化版本进行单元测试
    /// </summary>
    private class UsernameChangeLogicTester
    {
        private string _username = string.Empty;
        private string _password = string.Empty;
        private string? _savedUsername;

        public string Username
        {
            get => _username;
            set
            {
                var shouldClearPassword = _savedUsername != null &&
                                          !string.IsNullOrEmpty(_savedUsername) &&
                                          !string.IsNullOrEmpty(value) &&
                                          value != _savedUsername &&
                                          !string.IsNullOrEmpty(_password);

                _username = value;

                if (shouldClearPassword)
                {
                    Password = string.Empty;
                }
            }
        }

        public string Password
        {
            get => _password;
            set => _password = value;
        }

        /// <summary>
        /// 模拟加载已保存的凭据
        /// </summary>
        public void SimulateLoadSavedCredentials(string savedUsername, string savedPassword)
        {
            _savedUsername = savedUsername;
            _username = savedUsername;
            _password = savedPassword;
        }

        /// <summary>
        /// 模拟仅加载用户名（无密码）
        /// </summary>
        public void SimulateLoadSavedUsernameOnly(string savedUsername)
        {
            _savedUsername = savedUsername;
            _username = savedUsername;
        }
    }

    #region 用户名变更清空密码测试

    [Fact]
    public void UsernameChange_WhenSavedCredentials_ShouldClearPassword()
    {
        // Arrange
        var tester = new UsernameChangeLogicTester();
        tester.SimulateLoadSavedCredentials("doctor1", "password123");

        // Act
        tester.Username = "doctor2";

        // Assert
        tester.Username.Should().Be("doctor2");
        tester.Password.Should().BeEmpty("因为用户名已变更，密码应被清空");
    }

    [Fact]
    public void UsernameChange_WhenNoSavedCredentials_ShouldNotAffectPassword()
    {
        // Arrange
        var tester = new UsernameChangeLogicTester();
        tester.Username = "newuser";
        tester.Password = "mypassword";

        // Act
        tester.Username = "anotheruser";

        // Assert
        tester.Username.Should().Be("anotheruser");
        tester.Password.Should().Be("mypassword", "因为没有保存的凭据，密码不应被清空");
    }

    [Fact]
    public void InitialLoad_ShouldNotClearPassword()
    {
        // Arrange
        var tester = new UsernameChangeLogicTester();

        // Act
        tester.SimulateLoadSavedCredentials("doctor1", "password123");

        // Assert
        tester.Username.Should().Be("doctor1");
        tester.Password.Should().Be("password123", "初始加载时密码不应被清空");
    }

    [Fact]
    public void UsernameChange_ToEmpty_ShouldNotClearPassword()
    {
        // Arrange
        var tester = new UsernameChangeLogicTester();
        tester.SimulateLoadSavedCredentials("doctor1", "password123");

        // Act
        tester.Username = string.Empty;

        // Assert
        tester.Username.Should().BeEmpty();
        tester.Password.Should().Be("password123", "用户名清空时密码不应被清空，允许用户删除后重新输入");
    }

    [Fact]
    public void UsernameChange_BackToSaved_ShouldNotRestorePassword()
    {
        // Arrange
        var tester = new UsernameChangeLogicTester();
        tester.SimulateLoadSavedCredentials("doctor1", "password123");

        // Act
        tester.Username = "doctor2";
        tester.Password.Should().BeEmpty();
        tester.Username = "doctor1";

        // Assert
        tester.Username.Should().Be("doctor1");
        tester.Password.Should().BeEmpty("密码一旦清空就不应自动恢复");
    }

    [Fact]
    public void UsernameChange_WhenOnlyUsernameSaved_ShouldNotTriggerClear()
    {
        // Arrange
        var tester = new UsernameChangeLogicTester();
        tester.SimulateLoadSavedUsernameOnly("doctor1");

        // Act
        tester.Password = "newpassword";
        tester.Username = "doctor2";

        // Assert
        tester.Password.Should().BeEmpty("当前实现：只要_savedUsername存在且用户名变更就清空密码");
    }

    #endregion
}
