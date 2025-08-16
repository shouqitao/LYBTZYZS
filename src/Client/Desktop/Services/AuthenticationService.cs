using System;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using Polly;
using Polly.Extensions.Http;
using LYBT.Shared.Interfaces.Services.Business;
using LYBT.Desktop.Core.Services;
using LYBT.Desktop.Core.Models;
using LYBT.Shared.Models.Contracts.Common;
using LoginResponse = LYBT.Shared.Models.Contracts.Auth.LoginResponse;
using LoginRequest = LYBT.Shared.Models.Contracts.Auth.LoginRequest;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
// UltraThink重构: 统一UserInfo和UserDto，使用UserDto作为统一模型
using UserInfo = LYBT.Shared.Models.Contracts.Users.UserDto;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// 身份认证服务 - 遵循UltraThink标准
    /// </summary>
    public class AuthenticationService : 
        LYBT.Shared.Interfaces.Services.Business.IAuthenticationService,
        LYBT.Desktop.Core.Interfaces.Services.IAuthenticationService
    {
        #region 依赖服务
        
        private readonly IAuthApiService _authApiService;
        private readonly ITokenManager _tokenManager;
        private readonly ILogger<AuthenticationService>? _logger;
        private readonly IAsyncPolicy<HttpResponseMessage> _retryPolicy;
        
        #endregion
        
        #region 状态字段
        
        private readonly SemaphoreSlim _authSemaphore = new(1, 1);
        private AuthenticationState _authState;
        
        #endregion
        
        #region 配置常量
        
        private const int MaxRetryAttempts = 3;
        private const int RetryDelaySeconds = 2;
        private const int HealthCheckTimeoutSeconds = 3;
        
        #endregion
        
        #region 构造函数
        
        public AuthenticationService(
            IAuthApiService authApiService,
            ITokenManager tokenManager,
            ILogger<AuthenticationService>? logger = null)
        {
            _authApiService = authApiService ?? throw new ArgumentNullException(nameof(authApiService));
            _tokenManager = tokenManager ?? throw new ArgumentNullException(nameof(tokenManager));
            _logger = logger;
            
            // 初始化重试策略
            _retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    MaxRetryAttempts,
                    retryAttempt => TimeSpan.FromSeconds(Math.Pow(RetryDelaySeconds, retryAttempt)),
                    onRetry: (outcome, timespan, retryCount, context) =>
                    {
                        _logger?.LogWarning("重试第 {RetryCount} 次，等待 {Timespan} 秒", retryCount, timespan.TotalSeconds);
                    });
            
            // 初始化认证状态
            _authState = new AuthenticationState();
        }
        
        #endregion
        
        #region 公共属性
        
        public bool IsLoggedIn => _authState.IsAuthenticated;
        
        #endregion
        
        #region 登录/登出
        
        public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
        {
            if (request == null)
                return ServiceResult<LoginResponse>.Failure("登录请求不能为空");
            
            await _authSemaphore.WaitAsync();
            try
            {
                _logger?.LogInformation("开始登录，用户名: {Username}", request.Username);
                
                
                // 调用API
                var apiResponse = await CallLoginApiWithRetryAsync(request);
                
                if (!apiResponse.IsSuccess)
                {
                    _logger?.LogWarning("登录失败: {Error}", apiResponse.ErrorMessage);
                    return ServiceResult<LoginResponse>.Failure(apiResponse.ErrorMessage ?? "登录失败");
                }
                
                // 处理成功响应
                var loginResponse = ProcessLoginResponse(apiResponse.Data);
                if (loginResponse == null)
                {
                    return ServiceResult<LoginResponse>.Failure("服务器响应格式错误");
                }
                
                // 更新认证状态
                UpdateAuthenticationState(loginResponse);
                
                _logger?.LogInformation($"登录成功，用户ID: {loginResponse.User?.Id}");
                return ServiceResult<LoginResponse>.Success(loginResponse);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "登录过程发生异常");
                return ServiceResult<LoginResponse>.Failure("登录失败: " + ex.Message, null, ex);
            }
            finally
            {
                _authSemaphore.Release();
            }
        }
        
        public async Task<ServiceResult> LogoutAsync()
        {
            await _authSemaphore.WaitAsync();
            try
            {
                _logger?.LogInformation("开始登出，用户ID: {UserId}", _authState.CurrentUser?.Id);
                
                // 尝试调用服务器登出API
                try
                {
                    await _authApiService.LogoutAsync();
                }
                catch (Exception ex)
                {
                    // 服务器登出失败不影响本地登出
                    _logger?.LogWarning(ex, "服务器登出失败，但将继续清除本地状态");
                }
                
                // 清除本地认证状态
                ClearAuthenticationState();
                
                _logger?.LogInformation("登出成功");
                return ServiceResult.Success();
            }
            finally
            {
                _authSemaphore.Release();
            }
        }
        
        #endregion
        
        #region 用户信息
        
        /// <summary>
        /// 获取当前用户信息 - 新Shared接口实现
        /// </summary>
        Task<ServiceResult<UserDto>> LYBT.Shared.Interfaces.Services.Business.IAuthenticationService.GetCurrentUserAsync()
        {
            var currentUser = _authState.CurrentUser;
            if (currentUser == null)
            {
                return Task.FromResult(ServiceResult<UserDto>.Failure("用户未登录"));
            }
            
            return Task.FromResult(ServiceResult<UserDto>.Success(currentUser));
        }

        /// <summary>
        /// 获取当前用户信息 - UI接口实现
        /// </summary>
        Task<UserInfo?> LYBT.Desktop.Core.Interfaces.Services.IAuthenticationService.GetCurrentUserAsync()
        {
            return Task.FromResult(_authState.CurrentUser);
        }

        /// <summary>
        /// 获取当前用户信息 - UI层兼容方法
        /// </summary>
        public Task<UserInfo?> GetCurrentUserForUIAsync()
        {
            return Task.FromResult(_authState.CurrentUser);
        }

        /// <summary>
        /// 检查用户认证状态 - 新Shared接口实现
        /// </summary>
        public Task<ServiceResult<bool>> IsAuthenticatedAsync()
        {
            return Task.FromResult(ServiceResult<bool>.Success(_authState.IsAuthenticated));
        }

        /// <summary>
        /// 验证令牌有效性 - 新Shared接口实现
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateTokenAsync(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    return ServiceResult<bool>.Success(false);
                }

                // 简单验证：检查是否是当前存储的token
                var currentToken = _tokenManager.GetToken();
                if (token == currentToken && _authState.IsAuthenticated)
                {
                    return ServiceResult<bool>.Success(true);
                }

                return ServiceResult<bool>.Success(false);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "验证令牌失败");
                return ServiceResult<bool>.Failure("令牌验证失败: " + ex.Message);
            }
        }

        /// <summary>
        /// 刷新访问令牌 - 新Shared接口实现
        /// </summary>
        public async Task<ServiceResult<LoginResponse>> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                _logger?.LogInformation("开始刷新令牌");
                
                // TODO: 实现刷新令牌逻辑，当前返回当前用户信息
                if (!_authState.IsAuthenticated)
                {
                    return ServiceResult<LoginResponse>.Failure("用户未登录，无法刷新令牌");
                }

                var response = new LoginResponse
                {
                    Token = _tokenManager.GetToken() ?? string.Empty,
                    User = ConvertToBaseUser(_authState.CurrentUser)
                };

                return ServiceResult<LoginResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "刷新令牌失败");
                return ServiceResult<LoginResponse>.Failure("刷新令牌失败: " + ex.Message);
            }
        }
        
        /// <summary>
        /// UI层兼容方法 - 获取Token
        /// </summary>
        public string? GetToken()
        {
            return _tokenManager.GetToken();
        }
        
        public void ClearAuthInfo()
        {
            ClearAuthenticationState();
        }
        
        #endregion
        
        #region 连接检查
        
        public async Task<bool> CheckConnectionAsync()
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(HealthCheckTimeoutSeconds));
                using var httpClient = CreateHttpClient();
                
                var baseUrl = GetApiBaseUrl();
                var healthCheckUrl = $"{baseUrl}/swagger/index.html";
                
                var response = await httpClient.GetAsync(healthCheckUrl, cts.Token);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                _logger?.LogDebug(ex, "API连接检查失败");
                return false;
            }
        }
        
        #endregion
        
        #region 私有方法
        
        
        private async Task<ServiceResult<dynamic>> CallLoginApiWithRetryAsync(LoginRequest request)
        {
            try
            {
                var response = await _authApiService.LoginAsync(request);
                
                // 处理Refit包装的响应
                if (response.IsSuccessStatusCode && response.Content?.Success == true)
                {
                    return ServiceResult<dynamic>.Success(response.Content);
                }
                
                var errorMessage = response.Content?.Message ?? 
                                  response.Error?.Content ?? 
                                  "登录失败";
                                  
                return ServiceResult<dynamic>.Failure(errorMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<dynamic>.Failure("网络请求失败: " + ex.Message, null, ex);
            }
        }
        
        private LoginResponse? ProcessLoginResponse(dynamic apiResponse)
        {
            if (apiResponse?.Data == null)
                return null;
            
            try
            {
                var data = apiResponse.Data;
                
                return new LoginResponse
                {
                    Token = data.Token?.ToString() ?? string.Empty,
                    User = ConvertToUserInfo(data.User)
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "处理登录响应失败");
                return null;
            }
        }
        
        private UserInfo ConvertToUserInfo(dynamic userObj)
        {
            if (userObj == null)
                return new UserInfo();
            
            try
            {
                return new UserInfo
                {
                    Id = Guid.TryParse(userObj.Id?.ToString(), out Guid id) ? id : Guid.Empty,
                    Username = userObj.Username?.ToString() ?? string.Empty,
                    RealName = userObj.RealName?.ToString() ?? string.Empty,
                    PhoneNumber = userObj.PhoneNumber?.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "转换用户信息失败");
                return new UserInfo();
            }
        }
        
        private void UpdateAuthenticationState(LoginResponse response)
        {
            _authState = new AuthenticationState
            {
                IsAuthenticated = true,
                CurrentUser = ConvertToFrontendUserInfo(response.User),
                Token = response.Token,
                AuthenticatedAt = DateTime.Now
            };
            
            _tokenManager.SetToken(response.Token);
        }
        
        private void ClearAuthenticationState()
        {
            _authState = new AuthenticationState();
            _tokenManager.ClearToken();
        }
        
        private UserInfo? ConvertToFrontendUserInfo(BaseUser? authUser)
        {
            if (authUser == null)
                return null;
            
            return new UserInfo
            {
                Id = authUser.Id,
                Username = authUser.Username,
                RealName = authUser.RealName,
                PhoneNumber = authUser.PhoneNumber
            };
        }

        /// <summary>
        /// 转换UserDto到BaseUser - 新接口适配方法
        /// </summary>
        private BaseUser ConvertToBaseUser(UserInfo? userInfo)
        {
            if (userInfo == null)
                return new BaseUser();
            
            return new BaseUser
            {
                Id = userInfo.Id,
                Username = userInfo.Username,
                RealName = userInfo.RealName,
                PhoneNumber = userInfo.PhoneNumber
            };
        }
        
        private HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };
            
            return new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(HealthCheckTimeoutSeconds)
            };
        }
        
        private string GetApiBaseUrl()
        {
            return Core.Configuration.ApiConfiguration.BaseUrl.TrimEnd('/');
        }
        
        #endregion
        
        #region 内部类
        
        /// <summary>
        /// 认证状态
        /// </summary>
        private class AuthenticationState
        {
            public bool IsAuthenticated { get; set; }
            public UserInfo? CurrentUser { get; set; }
            public string? Token { get; set; }
            public DateTime? AuthenticatedAt { get; set; }
        }
        
        #endregion
    }
}