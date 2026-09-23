using LYBT.Tests.Desktop.Infrastructure;
using FluentAssertions;
using LYBT.Desktop.Contracts.Results;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Shell.ViewModels;
using LYBT.Shared.Models.Contracts.Users;
using NSubstitute;
using Prism.Events;
using Xunit;

namespace LYBT.Tests.Desktop.Unit;

/// <summary>
/// I-11 回归测试：改密成功后必须发布 <see cref="AuthEvents.PasswordChangedEvent"/>。
/// 该事件由 ShellEventCoordinator.OnPasswordChanged 订阅（清理内容区 + 回登录页，Issue #1906
/// 「改密后强制重新登录」）；发布点曾随旧 AuthenticationService.ChangePasswordAsync 一并丢失，
/// 导致该行为静默失效。
/// </summary>
public class AccountSettingsPasswordChangedEventTests : DesktopTestBase
{
    /// <summary>
    /// 构造 SUT。事件聚合器用**真实** <see cref="EventAggregator"/>（替身的 GetEvent 返回 null，
    /// 无法验证「事件是否真的发布」），故由调用方建好并订阅后再传入。
    /// </summary>
    private AccountSettingsViewModel CreateSut(IUserService userService, IEventAggregator eventAggregator)
    {
        Services.EventAggregator.Returns(eventAggregator);
        return new AccountSettingsViewModel(
            Services,
            Substitute.For<IAuthenticationService>(),
            userService,
            Substitute.For<INavigationCoordinator>());
    }

    private static void PreparePasswordFields(AccountSettingsViewModel sut, string oldPassword)
    {
        sut.CurrentUser = new UserDetailDto { Id = Guid.NewGuid(), UserName = "doctor1" };
        sut.OldPassword = oldPassword;
        sut.NewPassword = "NewPass@456";
        sut.ConfirmPassword = "NewPass@456";
    }

    [Fact]
    public async Task ChangePassword_Success_PublishesPasswordChangedEvent()
    {
        // Arrange
        var eventAggregator = new EventAggregator();
        PasswordChangedPayload? received = null;
        eventAggregator.GetEvent<AuthEvents.PasswordChangedEvent>()
            .Subscribe(payload => received = payload);

        var userService = Substitute.For<IUserService>();
        userService.ChangePasswordAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(CommandResult<bool>.Succeeded(true));

        var sut = CreateSut(userService, eventAggregator);
        PreparePasswordFields(sut, "OldPass@123");

        // Act
        await sut.ChangePasswordCommand.ExecuteAsync(null);

        // Assert
        received.Should().NotBeNull("改密成功必须发布 PasswordChangedEvent，否则改密后不会回到登录页");
        received!.UserName.Should().Be("doctor1");
        received.RequiresReLogin.Should().BeTrue();
    }

    [Fact]
    public async Task ChangePassword_Failure_DoesNotPublishPasswordChangedEvent()
    {
        // Arrange
        var eventAggregator = new EventAggregator();
        var published = false;
        eventAggregator.GetEvent<AuthEvents.PasswordChangedEvent>()
            .Subscribe(_ => published = true);

        var userService = Substitute.For<IUserService>();
        userService.ChangePasswordAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(CommandResult<bool>.Failed("当前密码错误"));

        var sut = CreateSut(userService, eventAggregator);
        PreparePasswordFields(sut, "WrongPass@1");

        // Act
        await sut.ChangePasswordCommand.ExecuteAsync(null);

        // Assert
        published.Should().BeFalse("改密失败不应触发回登录页");
    }
}
