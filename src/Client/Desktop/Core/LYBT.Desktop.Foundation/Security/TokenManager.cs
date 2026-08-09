using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// Token管理器实现 - 内存级Token存储
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

        private string? _accessToken = null;
        private string? _refreshToken = null;
        private DateTime? _accessTokenExpiry = null;

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
    }
}
