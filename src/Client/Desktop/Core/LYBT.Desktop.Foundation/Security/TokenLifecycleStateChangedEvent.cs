using Prism.Events;

namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// Token生命周期状态变更事件
    /// Issue #1864: 客户端Token生命周期管理
    /// </summary>
    public class TokenLifecycleStateChangedEvent : PubSubEvent<TokenLifecycleStateChangedEventArgs>
    {
    }

    /// <summary>
    /// Token生命周期状态变更事件参数
    /// </summary>
    public class TokenLifecycleStateChangedEventArgs
    {
        public TokenLifecycleStateChangedEventArgs(
            TokenLifecycleState previousState,
            TokenLifecycleState currentState,
            TimeSpan? remainingTime = null)
        {
            PreviousState = previousState;
            CurrentState = currentState;
            RemainingTime = remainingTime;
            Timestamp = DateTime.UtcNow;
        }

        /// <summary>
        /// 之前的状态
        /// </summary>
        public TokenLifecycleState PreviousState { get; }

        /// <summary>
        /// 当前状态
        /// </summary>
        public TokenLifecycleState CurrentState { get; }

        /// <summary>
        /// Token剩余有效时间（仅在Active/Warning状态下有值）
        /// </summary>
        public TimeSpan? RemainingTime { get; }

        /// <summary>
        /// 状态变更时间戳
        /// </summary>
        public DateTime Timestamp { get; }

        /// <summary>
        /// 是否需要用户交互（Warning状态时为true）
        /// </summary>
        public bool RequiresUserInteraction => CurrentState == TokenLifecycleState.Warning;

        /// <summary>
        /// 是否需要重新登录（Expired状态时为true）
        /// </summary>
        public bool RequiresReLogin => CurrentState == TokenLifecycleState.Expired;
    }
}
