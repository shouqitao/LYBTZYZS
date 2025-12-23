using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// Token管理器实现 - 内存级Token存储
    /// OpenSpec: refactor-login-authentication (TKM-001, TKM-002)
    /// 
    /// 特点：
    /// 1. 线程安全：使用lock保护并发访问
    /// 2. 纯内存存储：进程退出即清除
    /// 3. 同步方法：内存操作无需异步
    /// 4. 简洁API：只管理Token字符串和过期时间
    /// </summary>
    public class TokenManager : ITokenManager
    {
        private readonly ILogger<TokenManager> _logger;
        private readonly object _lock = new();

        private string? _accessToken;
        private string? _refreshToken;
        private DateTime? _accessTokenExpiry;

        public TokenManager(ILogger<TokenManager> logger)
        {
            _logger = logger;
            _logger.LogDebug("TokenManager初始化（内存存储模式）");
        }

        /// <inheritdoc/>
        public string? AccessToken
        {
            get
            {
                lock (_lock)
                {
                    return _accessToken;
                }
            }
        }

        /// <inheritdoc/>
        public string? RefreshToken
        {
            get
            {
                lock (_lock)
                {
                    return _refreshToken;
                }
            }
        }

        /// <inheritdoc/>
        public DateTime? AccessTokenExpiry
        {
            get
            {
                lock (_lock)
                {
                    return _accessTokenExpiry;
                }
            }
        }

        /// <inheritdoc/>
        public void SetTokens(string accessToken, string refreshToken, DateTime expiry)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
            {
                throw new ArgumentException("AccessToken不能为空", nameof(accessToken));
            }

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                throw new ArgumentException("RefreshToken不能为空", nameof(refreshToken));
            }

            lock (_lock)
            {
                _accessToken = accessToken;
                _refreshToken = refreshToken;
                _accessTokenExpiry = expiry;
            }

            _logger.LogDebug("Token已设置，过期时间: {Expiry:yyyy-MM-dd HH:mm:ss} UTC", expiry);
        }

        /// <inheritdoc/>
        public void ClearTokens()
        {
            lock (_lock)
            {
                _accessToken = null;
                _refreshToken = null;
                _accessTokenExpiry = null;
            }

            _logger.LogDebug("Token已清除");
        }

        /// <inheritdoc/>
        public bool IsTokenValid()
        {
            lock (_lock)
            {
                if (string.IsNullOrWhiteSpace(_accessToken))
                {
                    return false;
                }

                if (!_accessTokenExpiry.HasValue)
                {
                    return false;
                }

                // 已过期则无效
                return _accessTokenExpiry.Value > DateTime.UtcNow;
            }
        }

        /// <inheritdoc/>
        public bool IsTokenExpiringSoon(TimeSpan threshold)
        {
            lock (_lock)
            {
                if (!_accessTokenExpiry.HasValue)
                {
                    return true; // 无过期时间视为即将过期
                }

                var expiresIn = _accessTokenExpiry.Value - DateTime.UtcNow;
                var isExpiringSoon = expiresIn <= threshold;

                if (isExpiringSoon)
                {
                    _logger.LogDebug("Token即将过期，剩余时间: {ExpiresIn}", expiresIn);
                }

                return isExpiringSoon;
            }
        }
    }
}
