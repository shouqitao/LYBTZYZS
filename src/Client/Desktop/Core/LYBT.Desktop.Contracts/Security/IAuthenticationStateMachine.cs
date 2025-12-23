namespace LYBT.Desktop.Contracts.Security;

/// <summary>
/// 统一认证状态机接口
/// OpenSpec: refactor-auth-role-system (Phase 1.1)
/// 替代原有的 ILoginStateMachine 和 LoginFlowState 双状态机架构
/// </summary>
public interface IAuthenticationStateMachine
{
    /// <summary>
    /// 当前认证状态
    /// </summary>
    AuthState CurrentState { get; }

    /// <summary>
    /// 是否已认证（登录成功）
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// 是否处于过渡状态（登录中、登出中、刷新中等）
    /// </summary>
    bool IsTransitioning { get; }

    /// <summary>
    /// 当前状态的显示消息（用于UI进度显示）
    /// </summary>
    string? StatusMessage { get; }

    /// <summary>
    /// 尝试触发状态转换
    /// </summary>
    /// <param name="evt">触发事件</param>
    /// <param name="statusMessage">可选的状态消息（用于UI显示）</param>
    /// <returns>转换是否成功</returns>
    bool Fire(AuthEvent evt, string? statusMessage = null);

    /// <summary>
    /// 异步触发状态转换
    /// 用于需要等待状态变更完成的场景
    /// </summary>
    /// <param name="evt">触发事件</param>
    /// <param name="statusMessage">可选的状态消息</param>
    /// <returns>转换是否成功</returns>
    Task<bool> FireAsync(AuthEvent evt, string? statusMessage = null);

    /// <summary>
    /// 检查是否可以触发指定转换
    /// </summary>
    /// <param name="evt">触发事件</param>
    /// <returns>是否可以转换</returns>
    bool CanFire(AuthEvent evt);

    /// <summary>
    /// 重置到初始状态
    /// </summary>
    void Reset();

    /// <summary>
    /// 获取当前状态允许的所有触发事件
    /// </summary>
    IEnumerable<AuthEvent> GetPermittedEvents();

    /// <summary>
    /// 状态变更事件
    /// </summary>
    event EventHandler<AuthStateChangedEventArgs>? StateChanged;
}
