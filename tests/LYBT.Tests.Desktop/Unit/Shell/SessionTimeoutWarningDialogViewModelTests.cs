using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Shell.Dialogs.ViewModels;
using LYBT.Tests.Desktop.Infrastructure;
using Microsoft.Extensions.Logging;
using Prism.Services.Dialogs;

namespace LYBT.Tests.Desktop;

/// <summary>
/// 会话超时预警对话框 ViewModel 契约测试（US-AUTH-014）。
/// <para>验证倒计时来源（<see cref="IUserActivityTracker.TimeUntilInactive"/>，非本地自减）、
/// 「续期」重置活动计时 + 尽力刷新令牌后以 OK 关闭、「退出」以 Cancel 关闭、
/// 会话在对话框打开期间过期以 Abort 关闭且不再响应后续事件。</para>
/// </summary>
[Trait("US", "US-AUTH-014")]
public class SessionTimeoutWarningDialogViewModelTests : DesktopTestBase
{
    private readonly IUserActivityTracker _activityTracker = Substitute.For<IUserActivityTracker>();
    private readonly ITokenLifecycleService _tokenLifecycle = Substitute.For<ITokenLifecycleService>();

    public SessionTimeoutWarningDialogViewModelTests()
    {
        _activityTracker.TimeUntilInactive.Returns(TimeSpan.FromSeconds(90));
        _tokenLifecycle.TryRefreshTokenAsync().Returns(Task.FromResult(true));
    }

    private SessionTimeoutWarningDialogViewModel CreateSut() => new(
        Services,
        _activityTracker,
        _tokenLifecycle,
        Substitute.For<ILogger<SessionTimeoutWarningDialogViewModel>>());

    /// <summary>等待异步轮询（PeriodicTimer 每秒一次）落定</summary>
    private static async Task PumpUntilAsync(Func<bool> condition, string failMessage, int timeoutMs = 4000)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        while (!condition())
        {
            if (sw.ElapsedMilliseconds > timeoutMs) break;
            await Task.Delay(10);
        }
        condition().Should().BeTrue(failMessage);
    }

    [Fact]
    public void Dialog_Opened_Shows_Remaining_Inactivity_As_MmSs_Countdown()
    {
        using var sut = CreateSut();

        sut.OnDialogOpened(new DialogParameters());

        sut.RemainingSeconds.Should().Be(90);
        sut.RemainingText.Should().Be("01:30", "倒计时必须按 mm:ss 呈现剩余不活动时间");
        sut.Title.Should().Be("会话即将超时");
        sut.Message.Should().Be("会话即将因长时间无操作而结束，是否继续使用？");
    }

    [Fact]
    public async Task Countdown_Ticks_Refresh_From_Activity_Tracker()
    {
        using var sut = CreateSut();
        sut.OnDialogOpened(new DialogParameters());
        sut.RemainingText.Should().Be("01:30");

        // 追踪器是唯一真相源：剩余时间变化后倒计时须随之刷新（非本地自减）
        _activityTracker.TimeUntilInactive.Returns(TimeSpan.FromSeconds(48));

        await PumpUntilAsync(
            () => sut.RemainingText == "00:48",
            "倒计时应每秒从 IUserActivityTracker.TimeUntilInactive 刷新");
        sut.RemainingSeconds.Should().Be(48);
    }

    [Fact]
    public async Task Countdown_Stops_When_Dialog_Closed()
    {
        using var sut = CreateSut();
        sut.OnDialogOpened(new DialogParameters());
        sut.OnDialogClosed();

        _activityTracker.TimeUntilInactive.Returns(TimeSpan.FromSeconds(5));
        await Task.Delay(1300);

        sut.RemainingText.Should().Be("01:30", "对话框关闭后不得继续刷新倒计时");
    }

    [Fact]
    public async Task Extend_Resets_Activity_Refreshes_Token_And_Closes_With_OK()
    {
        using var sut = CreateSut();
        IDialogResult? closed = null;
        sut.RequestClose += result => closed = result;
        sut.OnDialogOpened(new DialogParameters());

        await sut.ExtendCommand.ExecuteAsync(null);

        _activityTracker.Received(1).ResetActivity();
        await _tokenLifecycle.Received(1).TryRefreshTokenAsync();
        closed.Should().NotBeNull();
        closed!.Result.Should().Be(ButtonResult.OK, "「续期」后应回到正常使用");
    }

    [Fact]
    public async Task Extend_Closes_With_OK_When_Token_Refresh_Returns_False()
    {
        _tokenLifecycle.TryRefreshTokenAsync().Returns(Task.FromResult(false));
        using var sut = CreateSut();
        IDialogResult? closed = null;
        sut.RequestClose += result => closed = result;
        sut.OnDialogOpened(new DialogParameters());

        await sut.ExtendCommand.ExecuteAsync(null);

        _activityTracker.Received(1).ResetActivity();
        closed!.Result.Should().Be(ButtonResult.OK, "刷新令牌失败只记日志，不得阻断续期");
    }

    [Fact]
    public async Task Extend_Closes_With_OK_When_Token_Refresh_Throws()
    {
        _tokenLifecycle.TryRefreshTokenAsync()
            .Returns(Task.FromException<bool>(new InvalidOperationException("token endpoint down")));
        using var sut = CreateSut();
        IDialogResult? closed = null;
        sut.RequestClose += result => closed = result;
        sut.OnDialogOpened(new DialogParameters());

        await sut.ExtendCommand.ExecuteAsync(null);

        _activityTracker.Received(1).ResetActivity();
        closed!.Result.Should().Be(ButtonResult.OK, "刷新令牌抛异常只记日志，不得向 UI 抛出");
    }

    [Fact]
    public async Task Logout_Closes_With_Cancel_Without_Refreshing_Token()
    {
        using var sut = CreateSut();
        IDialogResult? closed = null;
        sut.RequestClose += result => closed = result;
        sut.OnDialogOpened(new DialogParameters());

        sut.LogoutCommand.Execute(null);

        closed.Should().NotBeNull();
        closed!.Result.Should().Be(ButtonResult.Cancel, "「退出」交由监控器转入既有登出链路");
        await _tokenLifecycle.DidNotReceive().TryRefreshTokenAsync();
        _activityTracker.DidNotReceive().ResetActivity();
    }

    [Fact]
    public async Task Session_Expired_While_Open_Closes_With_Abort_And_Ignores_Later_Events()
    {
        using var sut = CreateSut();
        var closedResults = new List<ButtonResult>();
        sut.RequestClose += result => closedResults.Add(result.Result);
        sut.OnDialogOpened(new DialogParameters());

        _activityTracker.SessionExpired += Raise.Event<EventHandler>(_activityTracker, EventArgs.Empty);

        closedResults.Should().ContainSingle()
            .Which.Should().Be(ButtonResult.Abort, "会话已过期时对话框不得停留在登录页之上");

        // 已关闭 → 退订：会话过期事件不得再次触发关闭
        _activityTracker.SessionExpired += Raise.Event<EventHandler>(_activityTracker, EventArgs.Empty);
        closedResults.Should().HaveCount(1);

        // 倒计时亦已停止
        _activityTracker.TimeUntilInactive.Returns(TimeSpan.FromSeconds(1));
        await Task.Delay(1300);
        sut.RemainingText.Should().Be("01:30");
    }

    [Fact]
    public async Task Dispose_While_Open_Stops_Countdown_And_Session_Expiry_Handling()
    {
        var sut = CreateSut();
        var closeCount = 0;
        sut.RequestClose += _ => closeCount++;
        sut.OnDialogOpened(new DialogParameters());

        sut.Dispose();

        _activityTracker.SessionExpired += Raise.Event<EventHandler>(_activityTracker, EventArgs.Empty);
        _activityTracker.TimeUntilInactive.Returns(TimeSpan.FromSeconds(1));
        await Task.Delay(1300);

        closeCount.Should().Be(0, "已释放的对话框不得再响应会话过期");
        sut.RemainingText.Should().Be("01:30", "已释放的对话框不得再刷新倒计时");
    }
}
