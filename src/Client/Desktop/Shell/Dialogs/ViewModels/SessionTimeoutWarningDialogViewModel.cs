using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.ViewModels.Base;
using Microsoft.Extensions.Logging;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Shell.Dialogs.ViewModels
{
    /// <summary>
    /// 会话超时预警对话框视图模型（US-AUTH-014）
    ///
    /// 对话框打开期间每秒轮询 <see cref="IUserActivityTracker.TimeUntilInactive"/> 刷新 mm:ss 倒计时：
    /// - 「续期」：重置活动计时 + 尽力刷新令牌（失败仅记日志）→ 以 <see cref="ButtonResult.OK"/> 关闭；
    /// - 「退出」：以 <see cref="ButtonResult.Cancel"/> 关闭，由监控器转入既有登出链路；
    /// - 会话在对话框打开期间真正过期（<see cref="IUserActivityTracker.SessionExpired"/>）→ 以
    ///   <see cref="ButtonResult.Abort"/> 关闭，不得停留在登录页之上。
    /// </summary>
    public partial class SessionTimeoutWarningDialogViewModel : DialogViewModelBase
    {
        private readonly IUserActivityTracker _activityTracker;
        private readonly ITokenLifecycleService _tokenLifecycle;
        private readonly ILogger<SessionTimeoutWarningDialogViewModel> _logger;

        private readonly PeriodicTimer _countdownTimer = new(TimeSpan.FromSeconds(1));
        private CancellationTokenSource? _countdownCts;
        private Task? _countdownTask;

        #region 可观察属性

        /// <summary>
        /// 剩余不活动秒数（向上取整，避免还有时间却显示 00:00）
        /// </summary>
        [ObservableProperty]
        private int _remainingSeconds;

        /// <summary>
        /// 剩余时间文本（mm:ss）
        /// </summary>
        [ObservableProperty]
        private string _remainingText = "00:00";

        /// <summary>
        /// 提示消息
        /// </summary>
        [ObservableProperty]
        private string _message = "会话即将因长时间无操作而结束，是否继续使用？";

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="services">ViewModel服务聚合</param>
        /// <param name="activityTracker">用户活动追踪服务（剩余不活动时间来源）</param>
        /// <param name="tokenLifecycle">Token生命周期服务（「续期」尽力刷新令牌）</param>
        /// <param name="logger">日志服务</param>
        public SessionTimeoutWarningDialogViewModel(
            IViewModelServices services,
            IUserActivityTracker activityTracker,
            ITokenLifecycleService tokenLifecycle,
            ILogger<SessionTimeoutWarningDialogViewModel> logger)
            : base(services)
        {
            _activityTracker = activityTracker ?? throw new ArgumentNullException(nameof(activityTracker));
            _tokenLifecycle = tokenLifecycle ?? throw new ArgumentNullException(nameof(tokenLifecycle));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            Title = "会话即将超时";

            // 会话在对话框打开期间过期 → 对话框必须立即关闭（不得停留在登录页之上）
            _activityTracker.SessionExpired += OnSessionExpired;
        }

        #endregion

        #region 对话框生命周期

        /// <summary>
        /// 对话框打开：立即刷新一次倒计时并启动每秒轮询
        /// </summary>
        protected override void OnDialogOpenedCore(IDialogParameters? parameters)
        {
            StartCountdown();
            _logger.LogInformation("[SESSION] 会话超时预警对话框已打开（剩余 {RemainingText}）", RemainingText);
        }

        /// <summary>
        /// 对话框关闭：停止倒计时并退订会话过期事件
        /// </summary>
        protected override void OnDialogClosedCore()
        {
            StopCountdown();
            UnsubscribeSessionExpired();
        }

        /// <summary>
        /// 释放：停止倒计时、释放计时器并退订会话过期事件
        /// </summary>
        protected override void OnDisposing()
        {
            StopCountdown();
            DisposeCountdownTimer();
            UnsubscribeSessionExpired();
            base.OnDisposing();
        }

        #endregion

        #region 命令

        /// <summary>
        /// 续期命令：重置活动计时 + 尽力刷新令牌（失败仅记日志）→ 关闭对话框
        /// </summary>
        [RelayCommand]
        private async Task ExtendAsync()
        {
            try
            {
                _activityTracker.ResetActivity();

                var refreshed = await _tokenLifecycle.TryRefreshTokenAsync();
                if (!refreshed)
                {
                    _logger.LogWarning("[SESSION] 续期时刷新令牌未成功（仅记日志，不阻断会话）");
                }
            }
            catch (Exception ex)
            {
                // 续期尽力而为：任何失败都不阻断会话，也不向 UI 抛异常
                _logger.LogWarning(ex, "[SESSION] 续期失败（仅记日志，不阻断会话）");
            }

            CloseDialog(ButtonResult.OK);
        }

        /// <summary>
        /// 退出命令：关闭对话框并请求登出（由 <see cref="ISessionTimeoutMonitor"/> 转入既有登出链路）
        /// </summary>
        [RelayCommand]
        private void Logout()
        {
            _logger.LogInformation("[SESSION] 用户在会话超时预警对话框选择退出");
            CloseDialog(ButtonResult.Cancel);
        }

        #endregion

        #region 倒计时

        private void StartCountdown()
        {
            if (_countdownCts is not null)
            {
                return;
            }

            var cts = new CancellationTokenSource();
            _countdownCts = cts;

            // 打开即刷新，避免首秒显示占位值
            RefreshCountdown();

            // PeriodicTimer 只允许一个未完成的 WaitForNextTickAsync：新循环挂在旧循环之后启动，
            // 确保上一次取消已被观察到（同 RegistrationListViewModel 的刷新循环约定）。
            var previous = _countdownTask;
            _countdownTask = previous is null || previous.IsCompleted
                ? RunCountdownLoopAsync(cts)
                : previous.ContinueWith(_ => RunCountdownLoopAsync(cts), TaskScheduler.Default).Unwrap();
        }

        /// <summary>
        /// 停止倒计时（可再次 StartCountdown）。只取消、不释放 CTS：轮询循环可能仍在等待该 token，
        /// CTS 由循环自身在退出时释放。
        /// </summary>
        private void StopCountdown()
        {
            var cts = _countdownCts;
            if (cts is null)
            {
                return;
            }

            _countdownCts = null;
            cts.Cancel();
        }

        /// <summary>释放倒计时计时器：须等轮询循环退出后再释放，否则与 WaitForNextTickAsync 竞态</summary>
        private void DisposeCountdownTimer()
        {
            var loop = _countdownTask;
            _countdownTask = null;

            if (loop is null || loop.IsCompleted)
            {
                _countdownTimer.Dispose();
                return;
            }

            _ = loop.ContinueWith(_ => _countdownTimer.Dispose(), TaskScheduler.Default);
        }

        /// <summary>
        /// 倒计时循环：每秒在 UI 线程刷新一次剩余时间。
        /// </summary>
        private async Task RunCountdownLoopAsync(CancellationTokenSource cts)
        {
            try
            {
                while (ReferenceEquals(_countdownCts, cts) && await _countdownTimer.WaitForNextTickAsync(cts.Token))
                {
                    await UiDispatcher.InvokeAsync(RefreshCountdown);
                }
            }
            catch (OperationCanceledException)
            {
                // 正常停止路径（OnDialogClosedCore / OnDisposing）
            }
            finally
            {
                cts.Dispose();
            }
        }

        /// <summary>从活动追踪服务读取剩余时间并刷新倒计时显示</summary>
        private void RefreshCountdown()
        {
            var remaining = _activityTracker.TimeUntilInactive;
            var seconds = (int)Math.Ceiling(remaining.TotalSeconds);

            RemainingSeconds = seconds > 0 ? seconds : 0;
            RemainingText = TimeSpan.FromSeconds(RemainingSeconds).ToString(@"mm\:ss");
        }

        #endregion

        #region 会话过期

        /// <summary>
        /// 会话已过期（活动追踪服务触发）→ 以 <see cref="ButtonResult.Abort"/> 关闭对话框
        /// </summary>
        private void OnSessionExpired(object? sender, EventArgs e)
        {
            _logger.LogWarning("[SESSION] 会话在预警对话框打开期间已过期，关闭对话框");
            StopCountdown();
            // 终态：立即退订，避免重复触发关闭（OnDialogClosedCore 再次退订为幂等操作）
            UnsubscribeSessionExpired();
            CloseDialog(ButtonResult.Abort);
        }

        private void UnsubscribeSessionExpired()
        {
            _activityTracker.SessionExpired -= OnSessionExpired;
        }

        #endregion
    }
}
