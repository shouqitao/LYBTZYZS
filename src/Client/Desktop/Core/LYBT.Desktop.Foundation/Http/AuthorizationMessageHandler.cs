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
    }
}
