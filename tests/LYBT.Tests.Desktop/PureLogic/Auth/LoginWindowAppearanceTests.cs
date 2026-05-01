using FluentAssertions;
using LYBT.Desktop.Auth.ViewModels;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Application;
using LYBT.Desktop.Foundation.HealthCheck;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Contracts.Roles;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Prism.Events;
using Prism.Regions;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.Auth;

/// <summary>
/// AUTH-012: Login window appearance defaults.
/// Documents that the LoginViewModel initializes with expected UI state.
/// These are thin tests that reference already-covered behavior in LoginViewModelTests.cs.
/// </summary>
public class LoginWindowAppearanceTests
{
    private readonly IViewModelServices _viewModelServices;
    private readonly ILoginCoordinator _loginCoordinator;
    private readonly IApplicationStateService _applicationStateService;
    private readonly IUsernameStorageService _usernameStorage;
    private readonly ICredentialVault _credentialVault;

    public LoginWindowAppearanceTests()
    {
        var loggerFactory = Substitute.For<ILoggerFactory>();
        var eventAggregator = Substitute.For<IEventAggregator>();
        var regionManager = Substitute.For<IRegionManager>();
        var sessionManager = Substitute.For<ISessionManager>();
        var userNotificationService = Substitute.For<IUserNotificationService>();
        var commonDialogService = Substitute.For<ICommonDialogService>();
        var roleRegistry = Substitute.For<IRoleRegistry>();

        _viewModelServices = Substitute.For<IViewModelServices>();
        _viewModelServices.LoggerFactory.Returns(loggerFactory);
        _viewModelServices.EventAggregator.Returns(eventAggregator);
        _viewModelServices.RegionManager.Returns(regionManager);
        _viewModelServices.SessionManager.Returns(sessionManager);
        _viewModelServices.UserNotificationService.Returns(userNotificationService);
        _viewModelServices.CommonDialogService.Returns(commonDialogService);
        _viewModelServices.RoleRegistry.Returns(roleRegistry);

        _loginCoordinator = Substitute.For<ILoginCoordinator>();
        _applicationStateService = Substitute.For<IApplicationStateService>();
        _usernameStorage = Substitute.For<IUsernameStorageService>();
        _credentialVault = Substitute.For<ICredentialVault>();

        _applicationStateService.IsApiHealthy.Returns(true);
        _applicationStateService.ConnectionStatus.Returns("Connected");
    }

    private LoginViewModel CreateSut()
    {
        return new LoginViewModel(
            _viewModelServices,
            _loginCoordinator,
            _applicationStateService,
            _usernameStorage,
            _credentialVault);
    }

    #region US-AUTH-012: Login window appearance defaults

    [Fact]
    public void US_AUTH_012_RememberUsername_DefaultsToFalse()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.RememberUsername.Should().BeFalse(
            "US-AUTH-012: remember-username checkbox should be unchecked by default");
    }

    [Fact]
    public void US_AUTH_012_RememberPassword_DefaultsToFalse()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.RememberPassword.Should().BeFalse(
            "US-AUTH-012: remember-password checkbox should be unchecked by default");
    }

    [Fact]
    public void US_AUTH_012_UsernameAndPassword_DefaultToEmpty()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.Username.Should().BeEmpty(
            "US-AUTH-012: username field should be empty on fresh login screen");
        sut.Password.Should().BeEmpty(
            "US-AUTH-012: password field should be empty on fresh login screen");
    }

    #endregion
}
