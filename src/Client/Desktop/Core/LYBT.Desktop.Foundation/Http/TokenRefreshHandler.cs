using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Security;
using LYBT.Shared.Configuration.Options.Client;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Prism.Events;

namespace LYBT.Desktop.Foundation.Http
{
    /// <summary>
    /// Token自动刷新处理器 - Issue #1838
    /// OpenSpec: refactor-token-sliding-expiration (AUTH-002)
    /// OpenSpec: refactor-login-authentication (Phase 1.4, 3.2)
    /// OpenSpec: unify-event-system (Phase 2.1)
    /// 检测Access Token即将过期时自动调用RefreshToken端点获取新Token
    /// 仅在用户活跃时执行刷新，实现滑动过期机制
    /// 增强：分级处理刷新失败，支持重试和用户友好错误提示
    /// 通过Prism EventAggregator发布Token刷新事件
    /// </summary>
    public class TokenRefreshHandler : DelegatingHandler, ITokenRefreshHandler
    {
        private readonly ITokenStorageService _tokenStorage;
        private readonly ICredentialVault _credentialVault;
        private readonly IUserActivityState? _userActivityState;
        private readonly ILogger<TokenRefreshHandler> _logger;
        private readonly IConfiguration _configuration;
        private readonly IEventAggregator? _eventAggregator;
        private readonly HttpClient _refreshHttpClient; // 专用HttpClient，避免循环依赖
        private readonly SemaphoreSlim _refreshSemaphore = new SemaphoreSlim(1, 1);

        // Token刷新提前量：提前5分钟刷新，避免临界情况
        private readonly TimeSpan _refreshBeforeExpiry = TimeSpan.FromMinutes(5);

        // Phase 1.4: 重试配置
        private const int MaxRetryAttempts = 3;
        private static readonly TimeSpan[] RetryDelays = new[]
        {
            TimeSpan.FromSeconds(1),
            TimeSpan.FromSeconds(2),
            TimeSpan.FromSeconds(4)
        };

        public TokenRefreshHandler(
            ITokenStorageService tokenStorage,
            ICredentialVault credentialVault,
            IConfiguration configuration,
            ILogger<TokenRefreshHandler> logger,
            IUserActivityState? userActivityState = null,
            IEventAggregator? eventAggregator = null)
        {
            _tokenStorage = tokenStorage ?? throw new ArgumentNullException(nameof(tokenStorage));
            _credentialVault = credentialVault ?? throw new ArgumentNullException(nameof(credentialVault));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _userActivityState = userActivityState; // 可选依赖，启动时可能尚未注册
            _eventAggregator = eventAggregator;

            // unify-configuration-system: 使用强类型配置
            // 创建专用HttpClient用于RefreshToken调用（不包含TokenRefreshHandler，避免循环依赖）
            var apiOptions = new ApiClientOptions();
            _configuration.GetSection(ApiClientOptions.SectionName).Bind(apiOptions);
            var apiBaseUrl = apiOptions.BaseUrl;
            var ignoreSslErrors = apiOptions.IgnoreSslErrors;

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
                        var result = await RefreshTokenAsync();
                        if (result.Success)
                        {
                            _logger.LogInformation("Token刷新成功");

                            // OpenSpec: refactor-token-sliding-expiration (AUTH-002)
                            // 刷新成功后重置用户活动计时器
                            _userActivityState?.ResetActivity();

                            // 发布Prism PubSubEvent
                            await PublishTokenRefreshSucceededEventAsync();
                        }
                        else
                        {
                            _logger.LogWarning("Token刷新失败 [原因: {Reason}]", result.FailureReason);
                            // 事件已在RefreshTokenAsync内部触发
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
        /// 主动刷新Token
        /// OpenSpec: refactor-login-authentication (Phase 1.4)
        /// </summary>
        public async Task<TokenRefreshResult> RefreshTokenAsync()
        {
            var refreshToken = await _tokenStorage.GetRefreshTokenAsync();
            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogWarning("RefreshToken不存在，无法刷新");
                // 发布失败事件
                PublishTokenRefreshFailedEvent(new TokenRefreshFailedEventArgs(
                    TokenRefreshFailureReason.NotLoggedIn,
                    "未登录",
                    "RefreshToken不存在",
                    canRetry: false,
                    requiresReLogin: false));
                return TokenRefreshResult.Failed(TokenRefreshFailureReason.NotLoggedIn, "RefreshToken不存在");
            }

            return await RefreshTokenWithRetryAsync(refreshToken);
        }

        /// <summary>
        /// 带重试的Token刷新
        /// OpenSpec: refactor-login-authentication (Phase 1.4)
        /// </summary>
        private async Task<TokenRefreshResult> RefreshTokenWithRetryAsync(string refreshToken)
        {
            TokenRefreshResult? lastResult = null;

            for (int attempt = 0; attempt < MaxRetryAttempts; attempt++)
            {
                if (attempt > 0)
                {
                    var delay = RetryDelays[Math.Min(attempt - 1, RetryDelays.Length - 1)];
                    _logger.LogInformation("Token刷新重试 [尝试: {Attempt}/{Max}] [延迟: {Delay}ms]",
                        attempt + 1, MaxRetryAttempts, delay.TotalMilliseconds);
                    await Task.Delay(delay);
                }

                lastResult = await ExecuteRefreshAsync(refreshToken);

                if (lastResult.Success)
                {
                    return lastResult;
                }

                // 判断是否可重试
                if (lastResult.FailureReason == TokenRefreshFailureReason.NetworkError ||
                    lastResult.FailureReason == TokenRefreshFailureReason.ServerError)
                {
                    _logger.LogWarning("Token刷新失败（可重试） [原因: {Reason}] [尝试: {Attempt}/{Max}]",
                        lastResult.FailureReason, attempt + 1, MaxRetryAttempts);
                    continue;
                }

                // 不可重试的错误，立即返回
                _logger.LogWarning("Token刷新失败（不可重试） [原因: {Reason}]", lastResult.FailureReason);
                break;
            }

            // T5-P2-05: RefreshToken 失败时尝试 AutoLogin 降级
            if (lastResult != null && IsAutoLoginEligible(lastResult.FailureReason))
            {
                var autoLoginResult = await TryAutoLoginFallbackAsync();
                if (autoLoginResult.Success)
                {
                    _logger.LogInformation("Token刷新失败后 AutoLogin 降级成功");
                    return autoLoginResult;
                }

                _logger.LogWarning("AutoLogin 降级也失败: {Reason}", autoLoginResult.ErrorMessage);
            }

            // 发布失败事件
            if (lastResult != null && lastResult.FailureReason.HasValue)
            {
                var eventArgs = CreateFailedEventArgs(lastResult.FailureReason.Value, lastResult.ErrorMessage ?? "未知错误");
                PublishTokenRefreshFailedEvent(eventArgs);
            }

            return lastResult ?? TokenRefreshResult.Failed(TokenRefreshFailureReason.Unknown, "刷新失败");
        }

        /// <summary>
        /// 执行单次Token刷新
        /// </summary>
        private async Task<TokenRefreshResult> ExecuteRefreshAsync(string refreshToken)
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
                    return await HandleRefreshErrorResponseAsync(response);
                }

                // 3. 解析响应
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
                if (apiResponse == null || !apiResponse.Success || apiResponse.Data == null)
                {
                    var errorMessage = apiResponse?.Message ?? "响应解析失败";
                    return CategorizeApiError(errorMessage);
                }

                // 4. 保存新的Token（保持当前的RememberMe状态）
                var currentLoginResponse = await _tokenStorage.GetLoginResponseAsync();
                var rememberMe = currentLoginResponse != null; // 如果之前有登录信息，则保持持久化

                await _tokenStorage.SaveAuthenticationAsync(apiResponse.Data, rememberMe);

                _logger.LogInformation("Token刷新成功 [NewExpiry: {ExpiresAt}]", apiResponse.Data.ExpiresAt);
                return TokenRefreshResult.Succeeded();
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Token刷新HTTP请求失败");
                return TokenRefreshResult.Failed(TokenRefreshFailureReason.NetworkError, httpEx.Message);
            }
            catch (TaskCanceledException tcEx) when (tcEx.InnerException is TimeoutException)
            {
                _logger.LogError(tcEx, "Token刷新请求超时");
                return TokenRefreshResult.Failed(TokenRefreshFailureReason.NetworkError, "请求超时");
            }
            catch (JsonException jsonEx)
            {
                _logger.LogError(jsonEx, "Token刷新响应JSON解析失败");
                return TokenRefreshResult.Failed(TokenRefreshFailureReason.ServerError, "响应格式错误");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token刷新时发生未预期的异常");
                return TokenRefreshResult.Failed(TokenRefreshFailureReason.Unknown, ex.Message);
            }
        }

        /// <summary>
        /// 处理刷新错误响应
        /// OpenSpec: refactor-login-authentication (Phase 1.4)
        /// </summary>
        private async Task<TokenRefreshResult> HandleRefreshErrorResponseAsync(HttpResponseMessage response)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning("RefreshToken API调用失败 [StatusCode: {StatusCode}] [Error: {Error}]",
                response.StatusCode, errorContent);

            // 根据HTTP状态码分类错误
            return response.StatusCode switch
            {
                HttpStatusCode.Unauthorized => CategorizeUnauthorizedError(errorContent),
                HttpStatusCode.Forbidden => TokenRefreshResult.Failed(TokenRefreshFailureReason.UserDisabled, "账户被禁用"),
                HttpStatusCode.BadRequest => CategorizeApiError(errorContent),
                >= HttpStatusCode.InternalServerError => TokenRefreshResult.Failed(TokenRefreshFailureReason.ServerError, "服务器错误"),
                _ => TokenRefreshResult.Failed(TokenRefreshFailureReason.Unknown, $"HTTP {(int)response.StatusCode}: {errorContent}")
            };
        }

        /// <summary>
        /// 分类401未授权错误
        /// </summary>
        private TokenRefreshResult CategorizeUnauthorizedError(string errorContent)
        {
            var lowerContent = errorContent.ToLowerInvariant();

            if (lowerContent.Contains("expired") || lowerContent.Contains("过期"))
            {
                return TokenRefreshResult.Failed(TokenRefreshFailureReason.RefreshTokenExpired, "RefreshToken已过期");
            }

            if (lowerContent.Contains("revoked") || lowerContent.Contains("撤销") || lowerContent.Contains("invalidated"))
            {
                return TokenRefreshResult.Failed(TokenRefreshFailureReason.RefreshTokenRevoked, "RefreshToken已被撤销");
            }

            if (lowerContent.Contains("invalid") || lowerContent.Contains("无效"))
            {
                return TokenRefreshResult.Failed(TokenRefreshFailureReason.RefreshTokenInvalid, "RefreshToken无效");
            }

            // 默认视为过期
            return TokenRefreshResult.Failed(TokenRefreshFailureReason.RefreshTokenExpired, "认证失败");
        }

        /// <summary>
        /// 分类API业务错误
        /// </summary>
        private TokenRefreshResult CategorizeApiError(string errorMessage)
        {
            var lowerMessage = errorMessage.ToLowerInvariant();

            if (lowerMessage.Contains("disabled") || lowerMessage.Contains("禁用"))
            {
                return TokenRefreshResult.Failed(TokenRefreshFailureReason.UserDisabled, errorMessage);
            }

            if (lowerMessage.Contains("expired") || lowerMessage.Contains("过期"))
            {
                return TokenRefreshResult.Failed(TokenRefreshFailureReason.RefreshTokenExpired, errorMessage);
            }

            if (lowerMessage.Contains("invalid") || lowerMessage.Contains("无效"))
            {
                return TokenRefreshResult.Failed(TokenRefreshFailureReason.RefreshTokenInvalid, errorMessage);
            }

            return TokenRefreshResult.Failed(TokenRefreshFailureReason.Unknown, errorMessage);
        }

        /// <summary>
        /// 创建失败事件参数
        /// </summary>
        private static TokenRefreshFailedEventArgs CreateFailedEventArgs(
            TokenRefreshFailureReason reason, string detailedMessage)
        {
            return reason switch
            {
                TokenRefreshFailureReason.NetworkError => TokenRefreshFailedEventArgs.NetworkError(detailedMessage),
                TokenRefreshFailureReason.RefreshTokenExpired => TokenRefreshFailedEventArgs.RefreshTokenExpired(detailedMessage),
                TokenRefreshFailureReason.RefreshTokenRevoked => TokenRefreshFailedEventArgs.RefreshTokenRevoked(detailedMessage),
                TokenRefreshFailureReason.RefreshTokenInvalid => TokenRefreshFailedEventArgs.RefreshTokenInvalid(detailedMessage),
                TokenRefreshFailureReason.ServerError => TokenRefreshFailedEventArgs.ServerError(detailedMessage),
                TokenRefreshFailureReason.UserDisabled => TokenRefreshFailedEventArgs.UserDisabled(detailedMessage),
                _ => new TokenRefreshFailedEventArgs(reason, "刷新失败，请稍后重试", detailedMessage, canRetry: true, requiresReLogin: false)
            };
        }

        /// <summary>
        /// 发布Token刷新成功事件（Phase 3.2）
        /// </summary>
        private async Task PublishTokenRefreshSucceededEventAsync()
        {
            if (_eventAggregator == null)
                return;

            try
            {
                var loginResponse = await _tokenStorage.GetLoginResponseAsync();
                var payload = new TokenRefreshSucceededPayload
                {
                    NewExpiresAt = loginResponse?.ExpiresAt ?? DateTime.UtcNow.AddHours(1)
                };
                _eventAggregator.GetEvent<AuthEvents.TokenRefreshSucceededEvent>().Publish(payload);

                // US-AUTH-013: 同时发布 SessionExtendedEvent
                _eventAggregator.GetEvent<AuthEvents.SessionExtendedEvent>().Publish(
                    new SessionExtendedPayload { NewExpiresAt = payload.NewExpiresAt });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布Token刷新成功事件失败");
            }
        }

        /// <summary>
        /// 发布Token刷新失败事件（Phase 3.2）
        /// </summary>
        private void PublishTokenRefreshFailedEvent(TokenRefreshFailedEventArgs eventArgs)
        {
            if (_eventAggregator == null)
                return;

            try
            {
                var payload = new TokenRefreshFailedPayload
                {
                    Reason = eventArgs.Reason,
                    UserMessage = eventArgs.UserMessage,
                    DetailedMessage = eventArgs.DetailedMessage,
                    RequiresReLogin = eventArgs.RequiresReLogin,
                    IsRetryable = eventArgs.CanRetry
                };
                _eventAggregator.GetEvent<AuthEvents.TokenRefreshFailedEvent>().Publish(payload);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "发布Token刷新失败事件失败");
            }
        }

        #region T5-P2-05: AutoLogin 降级

        /// <summary>
        /// 判断失败原因是否适合尝试 AutoLogin 降级
        /// 仅 RefreshToken 过期/撤销/无效时尝试，UserDisabled/NotLoggedIn 不降级
        /// </summary>
        private static bool IsAutoLoginEligible(TokenRefreshFailureReason? reason)
        {
            return reason is TokenRefreshFailureReason.RefreshTokenExpired
                or TokenRefreshFailureReason.RefreshTokenRevoked
                or TokenRefreshFailureReason.RefreshTokenInvalid;
        }

        /// <summary>
        /// 尝试使用存储的 AutoLoginToken 进行降级登录
        /// 使用 _refreshHttpClient 直接调用 API，避免循环依赖
        /// </summary>
        private async Task<TokenRefreshResult> TryAutoLoginFallbackAsync()
        {
            try
            {
                // 1. 获取当前用户名
                var loginResponse = await _tokenStorage.GetLoginResponseAsync();
                var username = loginResponse?.User?.UserName;
                if (string.IsNullOrEmpty(username))
                {
                    _logger.LogDebug("AutoLogin 降级跳过: 无法获取用户名");
                    return TokenRefreshResult.Failed(TokenRefreshFailureReason.NotLoggedIn, "无法获取用户名");
                }

                // 2. 从 CredentialVault 获取 AutoLoginToken
                var autoLoginToken = await _credentialVault.GetAutoLoginTokenAsync(username);
                if (string.IsNullOrEmpty(autoLoginToken))
                {
                    _logger.LogDebug("AutoLogin 降级跳过: 无存储的 AutoLoginToken - UserName: {UserName}", username);
                    return TokenRefreshResult.Failed(TokenRefreshFailureReason.NotLoggedIn, "无AutoLoginToken");
                }

                // 3. 调用 POST /api/v1/auth/auto-login
                var request = new AutoLoginRequest
                {
                    UserName = username,
                    AutoLoginToken = autoLoginToken
                };

                var response = await _refreshHttpClient.PostAsJsonAsync("/api/v1/auth/auto-login", request);

                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("AutoLogin 降级 API 调用失败 [StatusCode: {StatusCode}] [Error: {Error}]",
                        response.StatusCode, errorContent);

                    // AutoLoginToken 无效时清除存储
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        await _credentialVault.ClearCredentialsAsync(username);
                        _logger.LogInformation("AutoLoginToken 无效，已清除凭据 - UserName: {UserName}", username);
                    }

                    return TokenRefreshResult.Failed(TokenRefreshFailureReason.RefreshTokenInvalid, "自动登录失败");
                }

                // 4. 解析响应并保存新 Token
                var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
                if (apiResponse?.Success != true || apiResponse.Data == null)
                {
                    return TokenRefreshResult.Failed(TokenRefreshFailureReason.Unknown, apiResponse?.Message ?? "自动登录响应解析失败");
                }

                await _tokenStorage.SaveAuthenticationAsync(apiResponse.Data, rememberMe: true);

                // 5. 更新 AutoLoginToken（服务端可能返回新的）
                if (!string.IsNullOrEmpty(apiResponse.Data.AutoLoginToken))
                {
                    await _credentialVault.SaveAutoLoginTokenAsync(username, apiResponse.Data.AutoLoginToken);
                }

                _logger.LogInformation("AutoLogin 降级成功 [UserName: {UserName}] [NewExpiry: {ExpiresAt}]",
                    username, apiResponse.Data.ExpiresAt);

                // 发布成功事件
                await PublishTokenRefreshSucceededEventAsync();

                return TokenRefreshResult.Succeeded();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AutoLogin 降级异常");
                return TokenRefreshResult.Failed(TokenRefreshFailureReason.Unknown, ex.Message);
            }
        }

        #endregion

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
