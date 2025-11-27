using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Security;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Http
{
    /// <summary>
    /// Token自动刷新处理器 - Issue #1838
    /// OpenSpec: refactor-token-sliding-expiration (AUTH-002)
    /// 检测Access Token即将过期时自动调用RefreshToken端点获取新Token
    /// 仅在用户活跃时执行刷新，实现滑动过期机制
    /// </summary>
    public class TokenRefreshHandler : DelegatingHandler
    {
        private readonly ITokenStorageService _tokenStorage;
        private readonly IUserActivityState? _userActivityState;
        private readonly ILogger<TokenRefreshHandler> _logger;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _refreshHttpClient; // 专用HttpClient，避免循环依赖
        private readonly SemaphoreSlim _refreshSemaphore = new SemaphoreSlim(1, 1);

        // Token刷新提前量：提前5分钟刷新，避免临界情况
        private readonly TimeSpan _refreshBeforeExpiry = TimeSpan.FromMinutes(5);

        public TokenRefreshHandler(
            ITokenStorageService tokenStorage,
            IConfiguration configuration,
            ILogger<TokenRefreshHandler> logger,
            IUserActivityState? userActivityState = null)
        {
            _tokenStorage = tokenStorage ?? throw new ArgumentNullException(nameof(tokenStorage));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userActivityState = userActivityState; // 可选依赖，启动时可能尚未注册

            // 创建专用HttpClient用于RefreshToken调用（不包含TokenRefreshHandler，避免循环依赖）
            var apiBaseUrl = _configuration["Lybt:Client:Api:BaseUrl"] ?? "https://localhost:5001";
            var ignoreSslErrors = _configuration.GetValue<bool>("Lybt:Client:Api:IgnoreSslErrors", false);

            var httpHandler = new HttpClientHandler();
            if (ignoreSslErrors)
            {
                httpHandler.ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true;
            }

            _refreshHttpClient = new HttpClient(httpHandler)
            {
                BaseAddress = new Uri(apiBaseUrl),
                Timeout = TimeSpan.FromSeconds(30)
            };
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            // 1. 获取当前登录信息，检查Token过期状态
            var loginResponse = await _tokenStorage.GetLoginResponseAsync();
            if (loginResponse == null)
            {
                // 未登录，直接放行请求
                return await base.SendAsync(request, cancellationToken);
            }

            // 2. 检查Token是否即将过期（提前5分钟）
            var expiresAt = loginResponse.ExpiresAt;
            var timeUntilExpiry = expiresAt - DateTime.UtcNow;

            if (timeUntilExpiry <= _refreshBeforeExpiry)
            {
                // OpenSpec: refactor-token-sliding-expiration (AUTH-002)
                // 3. 检查用户活跃状态，仅在用户活跃时刷新Token（滑动过期机制）
                if (_userActivityState != null && !_userActivityState.IsUserActive)
                {
                    _logger.LogInformation("用户不活跃，跳过Token刷新（滑动过期机制）");
                    return await base.SendAsync(request, cancellationToken);
                }

                _logger.LogInformation("Access Token即将过期（剩余 {Minutes} 分钟），准备刷新", timeUntilExpiry.TotalMinutes);

                // 4. 使用SemaphoreSlim防止并发刷新
                await _refreshSemaphore.WaitAsync(cancellationToken);
                try
                {
                    // 再次检查是否需要刷新（可能被其他线程刷新过了）
                    var currentLoginResponse = await _tokenStorage.GetLoginResponseAsync();
                    if (currentLoginResponse == null || currentLoginResponse.ExpiresAt - DateTime.UtcNow <= _refreshBeforeExpiry)
                    {
                        // 5. 调用RefreshToken API
                        var refreshToken = await _tokenStorage.GetRefreshTokenAsync();
                        if (string.IsNullOrEmpty(refreshToken))
                        {
                            _logger.LogWarning("RefreshToken不存在，无法自动刷新");
                            return await base.SendAsync(request, cancellationToken);
                        }

                        var success = await RefreshTokenAsync(refreshToken);
                        if (success)
                        {
                            _logger.LogInformation("Token刷新成功");

                            // OpenSpec: refactor-token-sliding-expiration (AUTH-002)
                            // 刷新成功后重置用户活动计时器
                            _userActivityState?.ResetActivity();
                        }
                        else
                        {
                            _logger.LogWarning("Token刷新失败，可能需要重新登录");
                        }
                    }
                    else
                    {
                        _logger.LogDebug("Token已被其他请求刷新，无需重复刷新");
                    }
                }
                finally
                {
                    _refreshSemaphore.Release();
                }
            }

            // 6. 继续原始请求
            return await base.SendAsync(request, cancellationToken);
        }

        /// <summary>
        /// 调用Refresh API获取新Token并保存
        /// Issue #1838: 使用专用HttpClient直接调用，避免与Refit客户端的循环依赖
        /// </summary>
        private async Task<bool> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                // 1. 构造请求体
                var requestBody = new RefreshTokenRequest
                {
                    RefreshToken = refreshToken
                };

                // 2. 调用 POST /api/v1/auth/refresh 端点
                var response = await _refreshHttpClient.PostAsJsonAsync("/api/v1/auth/refresh", requestBody);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("RefreshToken API调用失败 [StatusCode: {StatusCode}] [Error: {Error}]",
                        response.StatusCode, errorContent);
                    return false;
                }

                // 3. 解析响应
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
                if (apiResponse == null || !apiResponse.Success || apiResponse.Data == null)
                {
                    _logger.LogWarning("RefreshToken API响应解析失败或返回失败状态");
                    return false;
                }

                // 4. 保存新的Token（保持当前的RememberMe状态）
                var currentLoginResponse = await _tokenStorage.GetLoginResponseAsync();
                var rememberMe = currentLoginResponse != null; // 如果之前有登录信息，则保持持久化

                await _tokenStorage.SaveAuthenticationAsync(apiResponse.Data, rememberMe);

                _logger.LogInformation("Token刷新成功 [NewExpiry: {ExpiresAt}]", apiResponse.Data.ExpiresAt);
                return true;
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Token刷新响应JSON解析失败");
                return false;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Token刷新HTTP请求失败");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token刷新时发生未预期的异常");
                return false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _refreshSemaphore?.Dispose();
                _refreshHttpClient?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
