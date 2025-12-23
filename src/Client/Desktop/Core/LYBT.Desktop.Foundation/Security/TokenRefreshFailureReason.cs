namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// Token刷新失败原因枚举
    /// OpenSpec: refactor-login-authentication (Phase 1.4)
    /// 用于分类Token刷新失败的不同场景，以便采取不同的处理策略
    /// </summary>
    public enum TokenRefreshFailureReason
    {
        /// <summary>
        /// 未知错误
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// 网络错误（连接失败、超时等）
        /// 处理策略：可重试，使用指数退避
        /// </summary>
        NetworkError = 1,

        /// <summary>
        /// RefreshToken已过期
        /// 处理策略：导航到登录页，需要重新登录
        /// </summary>
        RefreshTokenExpired = 2,

        /// <summary>
        /// RefreshToken已被撤销（用户在其他设备登出、管理员强制登出等）
        /// 处理策略：导航到登录页，需要重新登录
        /// </summary>
        RefreshTokenRevoked = 3,

        /// <summary>
        /// RefreshToken无效（格式错误、不存在等）
        /// 处理策略：导航到登录页，需要重新登录
        /// </summary>
        RefreshTokenInvalid = 4,

        /// <summary>
        /// 服务端错误（500系列错误）
        /// 处理策略：可重试，但需要提示用户服务暂时不可用
        /// </summary>
        ServerError = 5,

        /// <summary>
        /// 用户账户已禁用
        /// 处理策略：导航到登录页，显示账户被禁用提示
        /// </summary>
        UserDisabled = 6,

        /// <summary>
        /// 未登录状态（无Token可刷新）
        /// 处理策略：正常情况，无需处理
        /// </summary>
        NotLoggedIn = 7
    }
}
