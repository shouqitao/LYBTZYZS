using System.Collections.Concurrent;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Shell.Services.Session;
using LYBT.Shared.Configuration.Options.Client;
using LYBT.Tests.Desktop.Infrastructure.TestDoubles;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prism.Services.Dialogs;

namespace LYBT.Tests.Desktop.Unit;

/// <summary>
/// 会话超时预警监控器契约测试（US-AUTH-014）。
/// <para>验证：剩余不活动时间进入 <c>ClientSession:WarningBeforeTimeoutMinutes</c> 窗口才弹窗、
/// 每个不活动窗口只弹一次（「续期」后剩余时间回升自然重新武装）、
/// 窗口外/预警关闭（0 分钟）不弹窗、「退出」(Cancel) 与过期 (Abort) 触发 LogoutRequested、
/// 已停止（登出/过期路径接管）时不再重复触发登出。</para>
/// </summary>
[Trait("US", "US-AUTH-014")]
public class SessionTimeoutMonitorTests
{
    /// <summary>Prism 对话框注册名 = 视图类名（App.RegisterTypes 的 RegisterDialog）</summary>
    private const string DialogName = "SessionTimeoutWarningDialog";

    private readonly IUserActivityTracker _activityTracker = Substitute.For<IUserActivityTracker>();
    private readonly IDialogService _dialogService = Substitute.For<IDialogService>();
    private readonly ConcurrentQueue<Action<IDialogResult>> _dialogCallbacks = new();

    public SessionTimeoutMonitorTests()
    {
        // 默认：剩余不活动时间远大于预警窗口
        _activityTracker.TimeUntilInactive.Returns(TimeSpan.FromMinutes(30));

        _dialogService
            .When(service => service.ShowDialog(
                Arg.Any<string>(),
                Arg.Any<IDialogParameters>(),
                Arg.Any<Action<IDialogResult>>()))
            .Do(callInfo => _dialogCallbacks.Enqueue(callInfo.Arg<Action<IDialogResult>>()));
    }

    private SessionTimeoutMonitor CreateSut(int warningMinutes = 2) => new(
        _activityTracker,
        _dialogService,
        new TestUiThreadDispatcher(),
        Options.Create(new ClientSessionOptions { WarningBeforeTimeoutMinutes = warningMinutes }),
        Substitute.For<ILogger<SessionTimeoutMonitor>>());

    private void AssertDialogShownTimes(int expected)
        => _dialogService.Received(expected).ShowDialog(
            DialogName,
            Arg.Any<IDialogParameters>(),
            Arg.Any<Action<IDialogResult>>());

    /// <summary>第 <paramref name="index"/> 次弹出的预警对话框关闭回调（0 起）</summary>
    private Action<IDialogResult> DialogCallbackAt(int index) => _dialogCallbacks.ElementAt(index);

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
    public async Task Within_Warning_Window_Shows_Dialog_Once_Per_Inactivity_Window()
    {
        using var sut = CreateSut();
        _activityTracker.TimeUntilInactive.Returns(TimeSpan.FromSeconds(90));

        sut.Start();

        await PumpUntilAsync(
            () => _dialogCallbacks.Count == 1,
            "剩余不活动时间进入预警窗口后应弹出会话超时预警对话框");
        AssertDialogShownTimes(1);

        // 对话框仍打开（未返回结果）→ 同一不活动窗口内不得重复弹出
        await Task.Delay(1300);
        AssertDialogShownTimes(1);
    }

    [Fact]
    public async Task Outside_Warning_Window_Does_Not_Show_Dialog()
    {
        using var sut = CreateSut();
        _activityTracker.TimeUntilInactive.Returns(TimeSpan.FromMinutes(10));

        sut.Start();
        await Task.Delay(1500);

        AssertDialogShownTimes(0);
    }

    [Fact]
    public async Task Warning_Disabled_By_Zero_Minutes_Does_Not_Show_Dialog()
    {
        using var sut = CreateSut(warningMinutes: 0);
        _activityTracker.TimeUntilInactive.Returns(TimeSpan.FromSeconds(30));

        sut.Start();
        await Task.Delay(1500);

        AssertDialogShownTimes(0);
    }

    [Fact]
    public async Task Extend_Rearms_Warning_For_Next_Inactivity_Window()
    {
        using var sut = CreateSut();
        var logoutRequested = 0;
        sut.LogoutRequested += (_, _) => logoutRequested++;
        _activityTracker.TimeUntilInactive.Returns(TimeSpan.FromSeconds(90));

        sut.Start();
        await PumpUntilAsync(() => _dialogCallbacks.Count == 1, "应先弹出预警对话框");

        // 「续期」：活动计时重置 → 剩余时间回升，窗口条件失效且不得重弹
        _activityTracker.TimeUntilInactive.Returns(TimeSpan.FromMinutes(30));
        DialogCallbackAt(0)(new DialogResult(ButtonResult.OK));
        await Task.Delay(1300);
        AssertDialogShownTimes(1);
        logoutRequested.Should().Be(0, "「续期」不得请求登出");

        // 再次进入预警窗口 → 自然重新武装
        _activityTracker.TimeUntilInactive.Returns(TimeSpan.FromSeconds(60));
        await PumpUntilAsync(() => _dialogCallbacks.Count == 2, "新的不活动窗口应重新弹出预警");
        AssertDialogShownTimes(2);
    }

    [Fact]
    public async Task Dialog_Cancel_Raises_LogoutRequested_And_Stops_Monitor()
    {
        using var sut = CreateSut();
        var logoutRequested = 0;
        sut.LogoutRequested += (_, _) => logoutRequested++;
        _activityTracker.TimeUntilInactive.Returns(TimeSpan.FromSeconds(90));

        sut.Start();
        await PumpUntilAsync(() => _dialogCallbacks.Count == 1, "应先弹出预警对话框");

        DialogCallbackAt(0)(new DialogResult(ButtonResult.Cancel));

        logoutRequested.Should().Be(1, "用户选择「退出」应请求结束会话");
        sut.IsRunning.Should().BeFalse("请求登出后监控应停止");
    }

    [Fact]
    public async Task Dialog_Abort_On_Expiry_Raises_LogoutRequested()
    {
        using var sut = CreateSut();
        var logoutRequested = 0;
        sut.LogoutRequested += (_, _) => logoutRequested++;
        _activityTracker.TimeUntilInactive.Returns(TimeSpan.FromSeconds(90));

        sut.Start();
        await PumpUntilAsync(() => _dialogCallbacks.Count == 1, "应先弹出预警对话框");

        DialogCallbackAt(0)(new DialogResult(ButtonResult.Abort));

        logoutRequested.Should().Be(1, "会话在对话框打开期间过期应请求结束会话");
        sut.IsRunning.Should().BeFalse();
    }

    [Fact]
    public async Task Dialog_Abort_After_Stop_Does_Not_Duplicate_Logout()
    {
        using var sut = CreateSut();
        var logoutRequested = 0;
        sut.LogoutRequested += (_, _) => logoutRequested++;
        _activityTracker.TimeUntilInactive.Returns(TimeSpan.FromSeconds(90));

        sut.Start();
        await PumpUntilAsync(() => _dialogCallbacks.Count == 1, "应先弹出预警对话框");

        // 会话过期路径（ShellEventCoordinator.OnSessionExpired）已接管并停止监控 → 不得重复登出
        sut.Stop();
        DialogCallbackAt(0)(new DialogResult(ButtonResult.Abort));

        logoutRequested.Should().Be(0);
    }

    [Fact]
    public async Task Stop_Stops_Polling_And_Start_Is_Idempotent()
    {
        using var sut = CreateSut();
        _activityTracker.TimeUntilInactive.Returns(TimeSpan.FromSeconds(90));

        sut.Start();
        sut.Start(); // 幂等：重复启动不得产生第二个轮询循环
        await PumpUntilAsync(() => _dialogCallbacks.Count == 1, "应先弹出预警对话框");

        sut.Stop();
        sut.IsRunning.Should().BeFalse();

        DialogCallbackAt(0)(new DialogResult(ButtonResult.OK));
        await Task.Delay(1300);
        AssertDialogShownTimes(1);
    }
}
