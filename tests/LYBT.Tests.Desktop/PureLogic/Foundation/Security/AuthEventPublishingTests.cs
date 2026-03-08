using FluentAssertions;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.Security;
using LYBT.Desktop.Foundation.Security;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Prism.Events;
using Xunit;

namespace LYBT.Tests.Desktop.PureLogic.Foundation.Security;

/// <summary>
/// US-AUTH-013: 认证事件发布测试
/// 验证 LoginStartedEvent / LogoutStartedEvent / SessionExtendedEvent 在正确时机发布
/// </summary>
public class AuthEventPublishingTests
{
    private readonly IEventAggregator _eventAggregator = new EventAggregator();

    #region LoginStartedEvent Tests

    [Fact]
    public async Task LoginAsync_ShouldPublish_LoginStartedEvent()
    {
        // Arrange
        LoginStartedPayload? received = null;
        _eventAggregator.GetEvent<AuthEvents.LoginStartedEvent>()
            .Subscribe(p => received = p);

        var authApi = Substitute.For<IAuthApi>();
        authApi.LoginAsync(Arg.Any<LoginRequest>())
            .Returns(ApiResponse<LoginResponse>.CreateSuccess(new LoginResponse
            {
                Token = "test-token",
                RefreshToken = "test-refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            }));

        var sut = CreateAuthenticationService(authApi);

        // Act
        await sut.LoginAsync(new LoginRequest { UserName = "doctor1", Password = "pass" });

        // Assert
        received.Should().NotBeNull("LoginStartedEvent should be published before login");
        received!.UserName.Should().Be("doctor1");
        received.IsAutoLogin.Should().BeFalse();
    }

    [Fact]
    public async Task LoginWithAutoTokenAsync_ShouldPublish_LoginStartedEvent_WithAutoLoginFlag()
    {
        // Arrange
        LoginStartedPayload? received = null;
        _eventAggregator.GetEvent<AuthEvents.LoginStartedEvent>()
            .Subscribe(p => received = p);

        var authApi = Substitute.For<IAuthApi>();
        authApi.LoginWithAutoTokenAsync(Arg.Any<AutoLoginRequest>())
            .Returns(ApiResponse<LoginResponse>.CreateSuccess(new LoginResponse
            {
                Token = "test-token",
                RefreshToken = "test-refresh",
                ExpiresAt = DateTime.UtcNow.AddHours(1)
            }));

        var sut = CreateAuthenticationService(authApi);

        // Act
        await sut.LoginWithAutoTokenAsync(new AutoLoginRequest
        {
            UserName = "doctor1",
            AutoLoginToken = "auto-token"
        });

        // Assert
        received.Should().NotBeNull("LoginStartedEvent should be published for auto-login");
        received!.UserName.Should().Be("doctor1");
        received.IsAutoLogin.Should().BeTrue();
    }

    [Fact]
    public async Task LoginAsync_WhenApiFails_ShouldStillPublish_LoginStartedEvent()
    {
        // Arrange
        LoginStartedPayload? received = null;
        _eventAggregator.GetEvent<AuthEvents.LoginStartedEvent>()
            .Subscribe(p => received = p);

        var authApi = Substitute.For<IAuthApi>();
        authApi.LoginAsync(Arg.Any<LoginRequest>())
            .Returns(ApiResponse<LoginResponse>.CreateFail("Invalid credentials"));

        var sut = CreateAuthenticationService(authApi);

        // Act
        await sut.LoginAsync(new LoginRequest { UserName = "doctor1", Password = "wrong" });

        // Assert - event should be published BEFORE the API call, so even on failure
        received.Should().NotBeNull("LoginStartedEvent should be published even when login fails");
    }

    #endregion

    #region LogoutStartedEvent Tests

    [Fact]
    public async Task LogoutAsync_ShouldPublish_LogoutStartedEvent()
    {
        // Arrange
        LogoutStartedPayload? received = null;
        _eventAggregator.GetEvent<AuthEvents.LogoutStartedEvent>()
            .Subscribe(p => received = p);

        var tokenStorage = Substitute.For<ITokenStorageService>();
        tokenStorage.GetLoginResponseAsync().Returns(new LoginResponse
        {
            Token = "test-token",
            RefreshToken = "test-refresh",
            ExpiresAt = DateTime.UtcNow.AddHours(1),
            User = new() { UserName = "doctor1" }
        });

        var sut = CreateLogoutService(tokenStorage);

        // Act
        await sut.LogoutAsync();

        // Assert
        received.Should().NotBeNull("LogoutStartedEvent should be published at logout start");
        received!.UserName.Should().Be("doctor1");
    }

    #endregion

    #region Helpers

    private AuthenticationService CreateAuthenticationService(IAuthApi? authApi = null)
    {
        return new AuthenticationService(
            authApi ?? Substitute.For<IAuthApi>(),
            Substitute.For<ITokenStorageService>(),
            Substitute.For<ITokenValidator>(),
            Substitute.For<ICredentialVault>(),
            Substitute.For<ILogger<AuthenticationService>>(),
            _eventAggregator);
    }

    private LogoutService CreateLogoutService(ITokenStorageService? tokenStorage = null)
    {
        var stateMachine = Substitute.For<IAuthenticationStateMachine>();
        stateMachine.Fire(Arg.Any<AuthEvent>(), Arg.Any<string?>()).Returns(true);

        return new LogoutService(
            Substitute.For<ILogger<LogoutService>>(),
            tokenStorage ?? Substitute.For<ITokenStorageService>(),
            Substitute.For<IAuthApi>(),
            stateMachine,
            _eventAggregator);
    }

    #endregion
}
