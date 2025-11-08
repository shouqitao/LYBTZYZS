using System.Net.Http;
using LYBT.Desktop.Foundation.Security;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Http
{
    /// <summary>
    /// HTTP 请求消息处理器 - 自动添加认证令牌
    /// </summary>
    public class AuthorizationMessageHandler : DelegatingHandler
    {
        private readonly ITokenStorageService _tokenStorage;
        private readonly ILogger<AuthorizationMessageHandler> _logger;

        public AuthorizationMessageHandler(
            ITokenStorageService tokenStorage,
            ILogger<AuthorizationMessageHandler> logger)
        {
            _tokenStorage = tokenStorage ?? throw new ArgumentNullException(nameof(tokenStorage));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // Issue #1906修复：跳过匿名端点（如/health），不检查Token也不发出警告
            var requestPath = request.RequestUri?.AbsolutePath ?? string.Empty;
            if (IsAnonymousEndpoint(requestPath))
            {
                _logger.LogDebug("跳过匿名端点的Token检查: {Url}", request.RequestUri);
                return await base.SendAsync(request, cancellationToken);
            }

            // 获取存储的令牌
            var token = await _tokenStorage.GetTokenAsync();

            // 如果有令牌，添加到请求头
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                _logger.LogDebug("添加 Authorization header 到请求: {Url}", request.RequestUri);
            }
            else
            {
                _logger.LogWarning("未找到认证令牌，请求未添加 Authorization header: {Url}", request.RequestUri);
            }

            return await base.SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// 判断是否为匿名端点（不需要Token的端点）
        /// </summary>
        private static bool IsAnonymousEndpoint(string path)
        {
            // 匿名端点列表（支持版本化路由 /api/v1/...）
            var anonymousEndpoints = new[]
            {
                "/health",              // 健康检测
                "/api/auth/login",      // 登录（无版本号）
                "/api/v1/auth/login",   // 登录（v1版本）
                "/api/auth/refresh",    // 刷新Token（无版本号）
                "/api/v1/auth/refresh"  // 刷新Token（v1版本）
            };

            return anonymousEndpoints.Any(endpoint =>
                path.Equals(endpoint, StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(endpoint, StringComparison.OrdinalIgnoreCase));
        }
    }
}
