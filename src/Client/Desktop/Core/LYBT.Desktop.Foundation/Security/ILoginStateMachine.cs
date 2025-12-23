namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// 登录状态机接口
    /// OpenSpec: refactor-login-authentication (Phase 2.1)
    /// OpenSpec: unify-event-system (Phase 2.2)
    /// 管理登录流程的状态转换，确保状态一致性
    /// </summary>
    /// <remarks>
    /// 状态变更事件通过Prism PubSubEvent发布:
    /// - AuthEvents.LoginStateChangedEvent
    /// </remarks>
    public interface ILoginStateMachine
    {
        /// <summary>
        /// 当前状态
        /// </summary>
        LoginState CurrentState { get; }

        /// <summary>
        /// 尝试触发状态转换
        /// </summary>
        /// <param name="trigger">触发器</param>
        /// <returns>转换是否成功</returns>
        bool Fire(LoginTrigger trigger);

        /// <summary>
        /// 检查是否可以触发指定转换
        /// </summary>
        /// <param name="trigger">触发器</param>
        /// <returns>是否可以转换</returns>
        bool CanFire(LoginTrigger trigger);

        /// <summary>
        /// 重置到初始状态
        /// </summary>
        void Reset();

        /// <summary>
        /// 是否处于已登录状态
        /// </summary>
        bool IsLoggedIn { get; }

        /// <summary>
        /// 是否处于过渡状态（登录中、登出中、刷新中）
        /// </summary>
        bool IsTransitioning { get; }
    }

    /// <summary>
    /// 登录状态变更事件参数
    /// </summary>
    public class LoginStateChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 之前的状态
        /// </summary>
        public LoginState PreviousState { get; }

        /// <summary>
        /// 当前状态
        /// </summary>
        public LoginState CurrentState { get; }

        /// <summary>
        /// 触发转换的触发器
        /// </summary>
        public LoginTrigger Trigger { get; }

        /// <summary>
        /// 转换发生的时间
        /// </summary>
        public DateTime Timestamp { get; }

        public LoginStateChangedEventArgs(
            LoginState previousState,
            LoginState currentState,
            LoginTrigger trigger)
        {
            PreviousState = previousState;
            CurrentState = currentState;
            Trigger = trigger;
            Timestamp = DateTime.UtcNow;
        }
    }
}
