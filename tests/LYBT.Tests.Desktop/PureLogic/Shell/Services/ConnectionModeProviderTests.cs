using FluentAssertions;
using LYBT.Desktop.Contracts;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.LocalData.Context;
using LYBT.Desktop.Shell.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.PureLogic.Shell.Services;

/// <summary>
/// ConnectionModeProvider 单元测试 (SYNC-D03)
/// 验证运行时模式切换的核心逻辑:
/// - 切换流程 (验证 -> 清理 UI -> 切换 -> 通知)
/// - 阻断条件 (活跃医案 / 验证失败 / 重复切换)
/// - 事件通知
/// </summary>
public class ConnectionModeProviderTests
{
    private readonly ILogger<ConnectionModeProvider> _logger;
    private readonly IModeSwitchValidator _validator;
    private readonly IActiveConsultationService _activeConsultation;
    private readonly INavigationCoordinator _navigation;

    public ConnectionModeProviderTests()
    {
        _logger = Substitute.For<ILogger<ConnectionModeProvider>>();
        _validator = Substitute.For<IModeSwitchValidator>();
        _activeConsultation = Substitute.For<IActiveConsultationService>();
        _navigation = Substitute.For<INavigationCoordinator>();
    }

    #region 初始化

    [Fact]
    public void Constructor_SetsInitialMode()
    {
        IConnectionModeProvider sut = CreateProvider(ConnectionMode.Local);

        sut.CurrentMode.Should().Be(ConnectionMode.Local);
        sut.IsLocal.Should().BeTrue();
        sut.IsRemote.Should().BeFalse();
    }

    [Fact]
    public void Constructor_Remote_SetsCorrectProperties()
    {
        IConnectionModeProvider sut = CreateProvider(ConnectionMode.Remote);

        sut.CurrentMode.Should().Be(ConnectionMode.Remote);
        sut.IsRemote.Should().BeTrue();
        sut.IsLocal.Should().BeFalse();
        sut.IsSwitching.Should().BeFalse();
    }

    #endregion

    #region 成功切换

    [Fact]
    public async Task SwitchMode_RemoteToLocal_Succeeds()
    {
        IConnectionModeProvider sut = CreateProvider(ConnectionMode.Remote);
        SetupValidatorSuccess();

        var result = await sut.SwitchModeAsync(ConnectionMode.Local);

        result.Success.Should().BeTrue();
        sut.CurrentMode.Should().Be(ConnectionMode.Local);
        sut.IsLocal.Should().BeTrue();
    }

    [Fact]
    public async Task SwitchMode_LocalToRemote_Succeeds()
    {
        IConnectionModeProvider sut = CreateProvider(ConnectionMode.Local);
        SetupValidatorSuccess();

        var result = await sut.SwitchModeAsync(ConnectionMode.Remote);

        result.Success.Should().BeTrue();
        sut.CurrentMode.Should().Be(ConnectionMode.Remote);
        sut.IsRemote.Should().BeTrue();
    }

    [Fact]
    public async Task SwitchMode_ClearsUIBeforeSwitching()
    {
        var sut = CreateProvider(ConnectionMode.Remote);
        SetupValidatorSuccess();

        await sut.SwitchModeAsync(ConnectionMode.Local);

        _navigation.Received(1).ClearContentRegion();
        _navigation.Received(1).ClearHistory();
    }

    [Fact]
    public async Task SwitchMode_NavigatesToHomeAfterSwitch()
    {
        var sut = CreateProvider(ConnectionMode.Remote);
        SetupValidatorSuccess();

        await sut.SwitchModeAsync(ConnectionMode.Local);

        _navigation.Received(1).NavigateToHome();
    }

    [Fact]
    public async Task SwitchMode_FiresModeChangedEvent()
    {
        var sut = CreateProvider(ConnectionMode.Remote);
        SetupValidatorSuccess();

        ConnectionModeChangedEventArgs? receivedArgs = null;
        sut.ModeChanged += (_, args) => receivedArgs = args;

        await sut.SwitchModeAsync(ConnectionMode.Local);

        receivedArgs.Should().NotBeNull();
        receivedArgs!.PreviousMode.Should().Be(ConnectionMode.Remote);
        receivedArgs.CurrentMode.Should().Be(ConnectionMode.Local);
    }

    [Fact]
    public async Task SwitchMode_IsSwitchingIsFalseBefore_And_After()
    {
        var sut = CreateProvider(ConnectionMode.Remote);
        SetupValidatorSuccess();

        sut.IsSwitching.Should().BeFalse();

        await sut.SwitchModeAsync(ConnectionMode.Local);

        sut.IsSwitching.Should().BeFalse();
    }

    #endregion

    #region 阻断条件

    [Fact]
    public async Task SwitchMode_SameMode_ReturnsSuccessWithoutAction()
    {
        var sut = CreateProvider(ConnectionMode.Remote);

        var result = await sut.SwitchModeAsync(ConnectionMode.Remote);

        result.Success.Should().BeTrue();
        _navigation.DidNotReceive().ClearContentRegion();
        _navigation.DidNotReceive().NavigateToHome();
    }

    [Fact]
    public async Task SwitchMode_ActiveConsultation_ReturnsFailure()
    {
        var sut = CreateProvider(ConnectionMode.Remote);
        _activeConsultation.HasActiveConsultation.Returns(true);

        var result = await sut.SwitchModeAsync(ConnectionMode.Local);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("活跃医案");
        sut.CurrentMode.Should().Be(ConnectionMode.Remote, "mode should not change on failure");
    }

    [Fact]
    public async Task SwitchMode_ValidatorFails_ReturnsFailure()
    {
        var sut = CreateProvider(ConnectionMode.Local);
        _activeConsultation.HasActiveConsultation.Returns(false);
        _validator.ValidateLocalToRemoteSwitchAsync(Arg.Any<CancellationToken>())
            .Returns(ModeSwitchValidationResult.Failed("本地有 3 个未完成的医案", 3));

        var result = await sut.SwitchModeAsync(ConnectionMode.Remote);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("未完成的医案");
        sut.CurrentMode.Should().Be(ConnectionMode.Local);
    }

    [Fact]
    public async Task SwitchMode_ValidatorFails_DoesNotClearUI()
    {
        var sut = CreateProvider(ConnectionMode.Local);
        _activeConsultation.HasActiveConsultation.Returns(false);
        _validator.ValidateLocalToRemoteSwitchAsync(Arg.Any<CancellationToken>())
            .Returns(ModeSwitchValidationResult.Failed("blocked"));

        await sut.SwitchModeAsync(ConnectionMode.Remote);

        _navigation.DidNotReceive().ClearContentRegion();
        _navigation.DidNotReceive().NavigateToHome();
    }

    #endregion

    #region 验证器路由

    [Fact]
    public async Task SwitchMode_LocalToRemote_CallsLocalToRemoteValidator()
    {
        var sut = CreateProvider(ConnectionMode.Local);
        SetupValidatorSuccess();

        await sut.SwitchModeAsync(ConnectionMode.Remote);

        await _validator.Received(1).ValidateLocalToRemoteSwitchAsync(Arg.Any<CancellationToken>());
        await _validator.DidNotReceive().ValidateRemoteToLocalSwitchAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SwitchMode_RemoteToLocal_CallsRemoteToLocalValidator()
    {
        var sut = CreateProvider(ConnectionMode.Remote);
        SetupValidatorSuccess();

        await sut.SwitchModeAsync(ConnectionMode.Local);

        await _validator.Received(1).ValidateRemoteToLocalSwitchAsync(Arg.Any<CancellationToken>());
        await _validator.DidNotReceive().ValidateLocalToRemoteSwitchAsync(Arg.Any<CancellationToken>());
    }

    #endregion

    #region CancellationToken

    [Fact]
    public async Task SwitchMode_Cancelled_ReturnsFailure()
    {
        var sut = CreateProvider(ConnectionMode.Remote);
        _activeConsultation.HasActiveConsultation.Returns(false);

        using var cts = new CancellationTokenSource();
        _validator.ValidateRemoteToLocalSwitchAsync(Arg.Any<CancellationToken>())
            .Returns(async callInfo =>
            {
                await cts.CancelAsync();
                callInfo.Arg<CancellationToken>().ThrowIfCancellationRequested();
                return ModeSwitchValidationResult.Valid;
            });

        var result = await sut.SwitchModeAsync(ConnectionMode.Local, cts.Token);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("取消");
        sut.CurrentMode.Should().Be(ConnectionMode.Remote);
    }

    #endregion

    #region 异常处理

    [Fact]
    public async Task SwitchMode_ValidatorThrows_ReturnsFailure()
    {
        var sut = CreateProvider(ConnectionMode.Remote);
        _activeConsultation.HasActiveConsultation.Returns(false);
        _validator.ValidateRemoteToLocalSwitchAsync(Arg.Any<CancellationToken>())
            .Returns<ModeSwitchValidationResult>(_ => throw new InvalidOperationException("unexpected error"));

        var result = await sut.SwitchModeAsync(ConnectionMode.Local);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("unexpected error");
        sut.CurrentMode.Should().Be(ConnectionMode.Remote);
        sut.IsSwitching.Should().BeFalse();
    }

    #endregion

    #region Helpers

    private ConnectionModeProvider CreateProvider(ConnectionMode initialMode)
    {
        // Mock 接口而非具体类，避免构造函数签名变更导致测试断裂
        var databaseInitializer = Substitute.For<LYBT.Desktop.Contracts.Initialization.IDatabaseInitializer>();

        // 设置 EnsureInitializedAsync 返回已完成的 Task
        databaseInitializer.EnsureInitializedAsync(Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        return new ConnectionModeProvider(
            initialMode,
            _logger,
            _validator,
            _activeConsultation,
            _navigation,
            databaseInitializer);
    }

    private void SetupValidatorSuccess()
    {
        _activeConsultation.HasActiveConsultation.Returns(false);
        _validator.ValidateLocalToRemoteSwitchAsync(Arg.Any<CancellationToken>())
            .Returns(ModeSwitchValidationResult.Valid);
        _validator.ValidateRemoteToLocalSwitchAsync(Arg.Any<CancellationToken>())
            .Returns(ModeSwitchValidationResult.Valid);
    }

    #endregion
}
