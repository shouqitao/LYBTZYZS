using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Shell.Dialogs.Views;
using LYBT.Shared.Configuration.Options.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Shell.Services.Session;

/// <summary>
/// 会话超时预警监控器实现（US-AUTH-014）
/// <para>随登录成功启动，每秒轮询 <see cref="IUserActivityTracker.TimeUntilInactive"/>；
/// 剩余不活动时间进入 <c>ClientSession:WarningBeforeTimeoutMinutes</c> 窗口时，在 UI 线程弹出
/// <see cref="SessionTimeoutWarningDialog"/> 倒计时对话框（<c>_dialogOpen</c> 守卫保证每个不活动窗口只弹一次）。</para>
/// <para>对话框返回「退出」(Cancel) 或会话过期 (Abort) 时触发 <see cref="LogoutRequested"/>；
/// 「续期」(OK) 后剩余时间回升，窗口条件自然失效，下一个不活动窗口重新武装。</para>
/// </summary>
public class SessionTimeoutMonitor : ISessionTimeoutMonitor, IDisposable
{
    /// <summary>预警对话框注册名（<c>RegisterDialog</c> 的注册名 = 视图类名）</summary>
    private const string DialogName = nameof(SessionTimeoutWarningDialog);

    /// <summary>轮询间隔（秒）— 倒计时与窗口判定均按秒粒度</summary>
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(1);

    private readonly IUserActivityTracker _activityTracker;
    private readonly IDialogService _dialogService;
    private readonly IUiThreadDispatcher _uiDispatcher;
    private readonly ClientSessionOptions _sessionOptions;
    private readonly ILogger<SessionTimeoutMonitor> _logger;

    private readonly PeriodicTimer _timer = new(PollInterval);

    private CancellationTokenSource? _cts;
    private Task? _loopTask;
    private int _dialogOpen;
    private bool _disposed;

    /// <inheritdoc />
    public event EventHandler? LogoutRequested;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="activityTracker">用户活动追踪服务（剩余不活动时间来源）</param>
    /// <param name="dialogService">Prism 对话框服务</param>
    /// <param name="uiDispatcher">UI 线程调度器（对话框必须在 UI 线程显示）</param>
    /// <param name="sessionOptions">客户端会话配置（预警窗口分钟数）</param>
    /// <param name="logger">日志服务</param>
    public SessionTimeoutMonitor(
        IUserActivityTracker activityTracker,
        IDialogService dialogService,
        IUiThreadDispatcher uiDispatcher,
        IOptions<ClientSessionOptions> sessionOptions,
        ILogger<SessionTimeoutMonitor> logger)
    {
        _activityTracker = activityTracker ?? throw new ArgumentNullException(nameof(activityTracker));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _sessionOptions = sessionOptions?.Value ?? new ClientSessionOptions();
    }

    /// <inheritdoc />
    public bool IsRunning => _cts is not null;

    /// <inheritdoc />
    public void Start()
    {
        if (_disposed)
        {
            _logger.LogWarning("尝试启动已释放的SessionTimeoutMonitor");
            return;
        }

        if (_cts is not null)
        {
            return;
        }

        var cts = new CancellationTokenSource();
        _cts = cts;

        // PeriodicTimer 只允许一个未完成的 WaitForNextTickAsync：上一次取消若尚未被旧循环观察到，
        // 新循环立刻等待会抛 InvalidOperationException（计时器仍处于等待中状态）。
        // 故新循环挂在旧循环之后启动，确保旧等待已被观察到。
        var previous = _loopTask;
        _loopTask = previous is null || previous.IsCompleted
            ? RunPollLoopAsync(cts)
            : previous.ContinueWith(_ => RunPollLoopAsync(cts), TaskScheduler.Default).Unwrap();

        _logger.LogInformation("[SESSION] 会话超时预警监控已启动（预警窗口 {WarningMinutes} 分钟）",
            _sessionOptions.WarningBeforeTimeoutMinutes);
    }

    /// <inheritdoc />
    public void Stop()
    {
        var cts = _cts;
        if (cts is null)
        {
            return;
        }

        _cts = null;
        cts.Cancel();
        Interlocked.Exchange(ref _dialogOpen, 0);

        _logger.LogInformation("[SESSION] 会话超时预警监控已停止");
    }

    /// <summary>
    /// 轮询循环（PeriodicTimer 每秒一次）。CTS 由本循环在退出时释放，保证 token 不会被「释放后使用」。
    /// </summary>
    private async Task RunPollLoopAsync(CancellationTokenSource cts)
    {
        try
        {
            // 仅当自身仍是当前轮询循环时继续：Stop 后 _cts 置空，旧循环随即退出，不与新一轮 wait 冲突
            while (ReferenceEquals(_cts, cts) && await _timer.WaitForNextTickAsync(cts.Token))
            {
                Poll();
            }
        }
        catch (OperationCanceledException)
        {
            // 正常停止路径（Stop / Dispose）
        }
        finally
        {
            cts.Dispose();
        }
    }

    /// <summary>
    /// 单次窗口判定：剩余不活动时间 &gt; 0 且 ≤ 预警窗口，且当前没有预警对话框打开 → 显示对话框。
    /// </summary>
    private void Poll()
    {
        if (Volatile.Read(ref _dialogOpen) == 1)
        {
            return;
        }

        // WarningBeforeTimeoutMinutes = 0 → 关闭预警
        var warningWindow = TimeSpan.FromMinutes(_sessionOptions.WarningBeforeTimeoutMinutes);
        if (warningWindow <= TimeSpan.Zero)
        {
            return;
        }

        var remaining = _activityTracker.TimeUntilInactive;
        if (remaining <= TimeSpan.Zero || remaining > warningWindow)
        {
            return;
        }

        // 竞态兜底：对话框已在其它路径打开时不重复弹出
        if (Interlocked.CompareExchange(ref _dialogOpen, 1, 0) != 0)
        {
            return;
        }

        _logger.LogInformation("[SESSION] 会话剩余不活动时间 {RemainingSeconds:F0} 秒，弹出超时预警对话框",
            remaining.TotalSeconds);

        try
        {
            _ = _uiDispatcher.InvokeAsync(ShowWarningDialog);
        }
        catch (Exception ex)
        {
            // 调度失败 → 重新武装，下个轮询重试
            Interlocked.Exchange(ref _dialogOpen, 0);
            _logger.LogError(ex, "[SESSION] 调度会话超时预警对话框失败");
        }
    }

    /// <summary>在 UI 线程显示预警对话框（Prism 模态窗口）</summary>
    private void ShowWarningDialog()
    {
        try
        {
            _dialogService.ShowDialog(DialogName, new DialogParameters(), OnWarningDialogClosed);
        }
        catch (Exception ex)
        {
            Interlocked.Exchange(ref _dialogOpen, 0);
            _logger.LogError(ex, "[SESSION] 显示会话超时预警对话框失败");
        }
    }

    /// <summary>
    /// 预警对话框关闭回调：解除「已弹出」守卫；「退出」(Cancel) / 会话过期 (Abort) → 请求登出。
    /// </summary>
    private void OnWarningDialogClosed(IDialogResult result)
    {
        Interlocked.Exchange(ref _dialogOpen, 0);

        if (result?.Result is not (ButtonResult.Cancel or ButtonResult.Abort))
        {
            // 「续期」(OK)：活动计时已重置，剩余时间回升 → 下个不活动窗口自然重新武装
            return;
        }

        // 会话过期路径（ShellEventCoordinator.OnSessionExpired → Stop）已接管登出时不得重复触发
        if (!IsRunning)
        {
            return;
        }

        _logger.LogInformation("[SESSION] 会话超时预警对话框返回 {Result}，请求结束会话", result.Result);
        Stop();

        try
        {
            LogoutRequested?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[SESSION] LogoutRequested 事件处理器异常");
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Stop();

        // 计时器须等轮询循环退出后再释放（否则与 WaitForNextTickAsync 竞态）
        var loop = _loopTask;
        _loopTask = null;

        if (loop is null || loop.IsCompleted)
        {
            _timer.Dispose();
            return;
        }

        _ = loop.ContinueWith(_ => _timer.Dispose(), TaskScheduler.Default);
    }
}
