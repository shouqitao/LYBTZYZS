namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// Token刷新失败事件参数
    /// </summary>
    public class TokenRefreshFailedEventArgs : EventArgs
    {
        public TokenRefreshFailureReason Reason { get; }
        public string UserMessage { get; }
        public string DetailedMessage { get; }
        public bool CanRetry { get; }
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

        public static TokenRefreshFailedEventArgs NetworkError(string detailedMessage) =>
            new(TokenRefreshFailureReason.NetworkError,
                "网络连接失败，请检查网络后重试",
                detailedMessage,
                canRetry: true,
                requiresReLogin: false);

        public static TokenRefreshFailedEventArgs RefreshTokenExpired(string detailedMessage) =>
            new(TokenRefreshFailureReason.RefreshTokenExpired,
                "登录已过期，请重新登录",
                detailedMessage,
                canRetry: false,
                requiresReLogin: true);

        public static TokenRefreshFailedEventArgs RefreshTokenRevoked(string detailedMessage) =>
            new(TokenRefreshFailureReason.RefreshTokenRevoked,
                "登录凭证已失效，请重新登录",
                detailedMessage,
                canRetry: false,
                requiresReLogin: true);

        public static TokenRefreshFailedEventArgs RefreshTokenInvalid(string detailedMessage) =>
            new(TokenRefreshFailureReason.RefreshTokenInvalid,
                "登录凭证无效，请重新登录",
                detailedMessage,
                canRetry: false,
                requiresReLogin: true);

        public static TokenRefreshFailedEventArgs ServerError(string detailedMessage) =>
            new(TokenRefreshFailureReason.ServerError,
                "服务暂时不可用，请稍后重试",
                detailedMessage,
                canRetry: true,
                requiresReLogin: false);

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
        public bool Success { get; }
        public TokenRefreshFailureReason? FailureReason { get; }
        public string? ErrorMessage { get; }

        private TokenRefreshResult(bool success, TokenRefreshFailureReason? failureReason, string? errorMessage)
        {
            Success = success;
            FailureReason = failureReason;
            ErrorMessage = errorMessage;
        }

        public static TokenRefreshResult Succeeded() => new(true, null, null);

        public static TokenRefreshResult Failed(TokenRefreshFailureReason reason, string errorMessage) =>
            new(false, reason, errorMessage);
    }
}
