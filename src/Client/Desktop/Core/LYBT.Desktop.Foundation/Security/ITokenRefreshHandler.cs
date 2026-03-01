namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// Token刷新处理器接口
    /// OpenSpec: refactor-login-authentication (Phase 1.4)
    /// OpenSpec: unify-event-system (Phase 2.1)
    /// 提供主动刷新方法，事件通过IEventAggregator发布
    /// </summary>
    /// <remarks>
    /// Token刷新事件通过Prism PubSubEvent发布:
    /// - AuthEvents.TokenRefreshSucceededEvent: 刷新成功
    /// - AuthEvents.TokenRefreshFailedEvent: 刷新失败
    /// </remarks>
    public interface ITokenRefreshHandler
    {
        /// <summary>
        /// 主动刷新Token
        /// </summary>
        /// <returns>刷新结果</returns>
        Task<TokenRefreshResult> RefreshTokenAsync();
    }

    /// <summary>
    /// Token刷新失败事件参数
    /// </summary>
    public class TokenRefreshFailedEventArgs : EventArgs
    {
        /// <summary>
        /// 失败原因
        /// </summary>
        public TokenRefreshFailureReason Reason { get; }

        /// <summary>
        /// 用户友好的错误消息
        /// </summary>
        public string UserMessage { get; }

        /// <summary>
        /// 详细错误信息（用于日志）
        /// </summary>
        public string DetailedMessage { get; }

        /// <summary>
        /// 是否可重试
        /// </summary>
        public bool CanRetry { get; }

        /// <summary>
        /// 是否需要重新登录
        /// </summary>
        public bool RequiresReLogin { get; }

        public TokenRefreshFailedEventArgs(
            TokenRefreshFailureReason reason,
            string userMessage,
            string detailedMessage,
            bool canRetry,
            bool requiresReLogin)
        {
            Reason = reason;
            UserMessage = userMessage;
            DetailedMessage = detailedMessage;
            CanRetry = canRetry;
            RequiresReLogin = requiresReLogin;
        }

        /// <summary>
        /// 创建网络错误事件参数
        /// </summary>
        public static TokenRefreshFailedEventArgs NetworkError(string detailedMessage) =>
            new(TokenRefreshFailureReason.NetworkError,
                "网络连接失败，请检查网络后重试",
                detailedMessage,
                canRetry: true,
                requiresReLogin: false);

        /// <summary>
        /// 创建RefreshToken过期事件参数
        /// </summary>
        public static TokenRefreshFailedEventArgs RefreshTokenExpired(string detailedMessage) =>
            new(TokenRefreshFailureReason.RefreshTokenExpired,
                "登录已过期，请重新登录",
                detailedMessage,
                canRetry: false,
                requiresReLogin: true);

        /// <summary>
        /// 创建RefreshToken撤销事件参数
        /// </summary>
        public static TokenRefreshFailedEventArgs RefreshTokenRevoked(string detailedMessage) =>
            new(TokenRefreshFailureReason.RefreshTokenRevoked,
                "登录凭证已失效，请重新登录",
                detailedMessage,
                canRetry: false,
                requiresReLogin: true);

        /// <summary>
        /// 创建RefreshToken无效事件参数
        /// </summary>
        public static TokenRefreshFailedEventArgs RefreshTokenInvalid(string detailedMessage) =>
            new(TokenRefreshFailureReason.RefreshTokenInvalid,
                "登录凭证无效，请重新登录",
                detailedMessage,
                canRetry: false,
                requiresReLogin: true);

        /// <summary>
        /// 创建服务器错误事件参数
        /// </summary>
        public static TokenRefreshFailedEventArgs ServerError(string detailedMessage) =>
            new(TokenRefreshFailureReason.ServerError,
                "服务暂时不可用，请稍后重试",
                detailedMessage,
                canRetry: true,
                requiresReLogin: false);

        /// <summary>
        /// 创建用户禁用事件参数
        /// </summary>
        public static TokenRefreshFailedEventArgs UserDisabled(string detailedMessage) =>
            new(TokenRefreshFailureReason.UserDisabled,
                "您的账户已被禁用，请联系管理员",
                detailedMessage,
                canRetry: false,
                requiresReLogin: true);
    }

    /// <summary>
    /// Token刷新结果
    /// </summary>
    public class TokenRefreshResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// 失败原因（仅当Success=false时有效）
        /// </summary>
        public TokenRefreshFailureReason? FailureReason { get; }

        /// <summary>
        /// 错误消息
        /// </summary>
        public string? ErrorMessage { get; }

        private TokenRefreshResult(bool success, TokenRefreshFailureReason? failureReason, string? errorMessage)
        {
            Success = success;
            FailureReason = failureReason;
            ErrorMessage = errorMessage;
        }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static TokenRefreshResult Succeeded() => new(true, null, null);

        /// <summary>
        /// 创建失败结果
        /// </summary>
        public static TokenRefreshResult Failed(TokenRefreshFailureReason reason, string errorMessage) =>
            new(false, reason, errorMessage);
    }
}
