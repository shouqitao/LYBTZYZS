namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// Token生命周期服务接口
    /// Issue #1864: 客户端Token生命周期管理
    /// </summary>
    /// <remarks>
    /// 职责：
    /// - 监控Token有效期
    /// - 管理状态机转换(NotAuth->Active->Warning->Expired)
    /// - 发布状态变更事件
    /// - 协调Token自动刷新
    /// </remarks>
    public interface ITokenLifecycleService : IDisposable
    {
        /// <summary>
        /// 当前Token生命周期状态
        /// </summary>
        TokenLifecycleState CurrentState { get; }

        /// <summary>
        /// Token剩余有效时间
        /// </summary>
        TimeSpan? RemainingTime { get; }

        /// <summary>
        /// 启动生命周期监控
        /// </summary>
        /// <param name="tokenExpiresAt">Token过期时间</param>
        void StartMonitoring(DateTime tokenExpiresAt);

        /// <summary>
        /// 停止生命周期监控
        /// </summary>
        void StopMonitoring();

        /// <summary>
        /// 更新Token过期时间（Token刷新后调用）
        /// </summary>
        /// <param name="newExpiresAt">新的过期时间</param>
        void UpdateExpiration(DateTime newExpiresAt);

        /// <summary>
        /// 尝试刷新Token
        /// </summary>
        /// <returns>刷新是否成功</returns>
        Task<bool> TryRefreshTokenAsync();

        /// <summary>
        /// 重置为未认证状态（登出时调用）
        /// </summary>
        void Reset();

        /// <summary>
        /// 警告阈值（剩余时间少于此值时进入Warning状态）
        /// 默认5分钟
        /// </summary>
        TimeSpan WarningThreshold { get; }
    }
}
