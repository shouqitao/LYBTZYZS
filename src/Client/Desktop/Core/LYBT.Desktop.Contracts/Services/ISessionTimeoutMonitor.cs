namespace LYBT.Desktop.Contracts.Services;

/// <summary>
/// 会话超时预警监控器接口（US-AUTH-014）
/// <para>登录成功后启动、登出/会话过期时停止。运行期间每秒轮询 <see cref="IUserActivityTracker.TimeUntilInactive"/>：
/// 剩余不活动时间进入 <c>ClientSession:WarningBeforeTimeoutMinutes</c> 窗口（默认 2 分钟）时弹出倒计时对话框，
/// 每个不活动窗口最多弹一次（用户「续期」后剩余时间回升，自然重新武装）。</para>
/// </summary>
public interface ISessionTimeoutMonitor
{
    /// <summary>
    /// 开始监控（登录成功后调用；重复调用无副作用）
    /// </summary>
    void Start();

    /// <summary>
    /// 停止监控（登出/会话过期时调用；重复调用无副作用）
    /// </summary>
    void Stop();

    /// <summary>
    /// 是否正在监控
    /// </summary>
    bool IsRunning { get; }

    /// <summary>
    /// 请求结束会话事件：用户在预警对话框选择「退出」，或会话在对话框打开期间真正过期。
    /// 订阅方应转入既有登出链路（提示 + 自动登出 + 回登录页）。
    /// </summary>
    event EventHandler? LogoutRequested;
}
