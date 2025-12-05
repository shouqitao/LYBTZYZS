namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// Token生命周期状态
    /// Issue #1864: 客户端Token生命周期管理
    /// </summary>
    public enum TokenLifecycleState
    {
        /// <summary>
        /// 未认证状态（无Token或Token无效）
        /// </summary>
        NotAuthenticated,

        /// <summary>
        /// 活跃状态（Token有效）
        /// </summary>
        Active,

        /// <summary>
        /// 警告状态（Token即将过期，剩余时间少于阈值）
        /// </summary>
        Warning,

        /// <summary>
        /// 已过期状态（Token已过期）
        /// </summary>
        Expired
    }
}
