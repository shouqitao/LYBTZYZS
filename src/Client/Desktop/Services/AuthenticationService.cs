using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.Models;
using LYBT.Desktop.Core.Services;

using LYBT.Shared.Interfaces.Api;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
using LoginRequest = LYBT.Shared.Models.Contracts.Auth.LoginRequest;
using LoginResponse = LYBT.Shared.Models.Contracts.Auth.LoginResponse;

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// 身份认证服务 - 遵循UltraThink标准
    /// </summary>
    public class AuthenticationService : 
        LYBT.Desktop.Core.Interfaces.Services.IAuthenticationService
    {
        #region 依赖服务
        
        private readonly IAuthApi _authApiService;
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
            IAuthApi authApiService,
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
                return ServiceResult<LoginResponse>.Failure("登录失败: " + ex.Message, ex);
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
        public Task<ServiceResult<UserDto>> GetCurrentUserAsync()
        {
            var currentUser = _authState.CurrentUser;
            if (currentUser == null)
            {
                return Task.FromResult(ServiceResult<UserDto>.Failure("用户未登录"));
            }
            
            var userDto = ConvertToUserDto(currentUser);
            return Task.FromResult(ServiceResult<UserDto>.Success(userDto));
        }

        /// <summary>
        /// 获取当前用户信息 - UI接口实现
        /// </summary>
        Task<UserDto?> LYBT.Desktop.Core.Interfaces.Services.IAuthenticationService.GetCurrentUserAsync()
        {
            return Task.FromResult(_authState.CurrentUser);
        }

        /// <summary>
        /// 获取当前用户信息 - UI层兼容方法
        /// </summary>
        public Task<UserDto?> GetCurrentUserForUIAsync()
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
        public Task<ServiceResult<bool>> ValidateTokenAsync(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    return Task.FromResult(ServiceResult<bool>.Success(false));
                }

                // 简单验证：检查是否是当前存储的token
                var currentToken = _tokenManager.GetToken();
                if (token == currentToken && _authState.IsAuthenticated)
                {
                    return Task.FromResult(ServiceResult<bool>.Success(true));
                }

                return Task.FromResult(ServiceResult<bool>.Success(false));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "验证令牌失败");
                return Task.FromResult(ServiceResult<bool>.Failure("令牌验证失败: " + ex.Message));
            }
        }

        /// <summary>
        /// 刷新访问令牌 - 新Shared接口实现
        /// </summary>
        public Task<ServiceResult<LoginResponse>> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                _logger?.LogInformation("开始刷新令牌");
                
                // TODO: 实现刷新令牌逻辑，当前返回当前用户信息
                if (!_authState.IsAuthenticated)
                {
                    return Task.FromResult(ServiceResult<LoginResponse>.Failure("用户未登录，无法刷新令牌"));
                }

                var response = new LoginResponse
                {
                    Token = _tokenManager.GetToken() ?? string.Empty,
                    User = _authState.CurrentUser ?? new UserDto()
                };

                return Task.FromResult(ServiceResult<LoginResponse>.Success(response));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "刷新令牌失败");
                return Task.FromResult(ServiceResult<LoginResponse>.Failure("刷新令牌失败: " + ex.Message));
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
                var apiResponse = await _authApiService.LoginAsync(request);
                
                // 处理API响应格式
                if (apiResponse.Success && apiResponse.Data != null)
                {
                    return ServiceResult<dynamic>.Success(apiResponse.Data);
                }
                
                var errorMessage = apiResponse.Message ?? "登录失败";
                                  
                return ServiceResult<dynamic>.Failure(errorMessage);
            }
            catch (Exception ex)
            {
                return ServiceResult<dynamic>.Failure("登录失败: " + ex.Message, ex);
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
                    User = ConvertToUserDto(data.User)
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "处理登录响应失败");
                return null;
            }
        }
        
        private UserDto ConvertToUserDto(dynamic userObj)
        {
            if (userObj == null)
                return new UserDto();
            
            try
            {
                return new UserDto
                {
                    Id = Guid.TryParse(userObj.Id?.ToString(), out Guid id) ? id : Guid.Empty,
                    Username = userObj.Username?.ToString() ?? string.Empty,
                    RealName = userObj.RealName?.ToString() ?? string.Empty,
                    PhoneNumber = userObj.PhoneNumber?.ToString(),
                    Email = userObj.Email?.ToString(),
                    Role = userObj.Role?.ToString() ?? "User",
                    Status = Enum.TryParse<LYBT.Shared.Models.Enums.CommonStatus>(userObj.Status?.ToString(), out LYBT.Shared.Models.Enums.CommonStatus status) ? status : LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                    PinYinCode = userObj.PinYinCode?.ToString()
                };
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "转换用户信息失败");
                return new UserDto();
            }
        }
        
        private void UpdateAuthenticationState(LoginResponse response)
        {
            _authState = new AuthenticationState
            {
                IsAuthenticated = true,
                CurrentUser = response.User,
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
        
        private UserDto? ConvertToFrontendUserInfo(UserDto? authUser)
        {
            // 直接返回UserDto，不需要转换
            return authUser;
        }

        /// <summary>
        /// 简化UserDto转换 - 直接使用UserDto
        /// </summary>
        private UserDto PassThroughUserDto(UserDto userInfo)
        {
            return userInfo; // 直接返回，不需要转换
        }

        /// <summary>
        /// 转换UserDto到UserDto - 简化的直通方法（BaseUser已删除）
        /// </summary>
        private UserDto ConvertToBaseUser(UserDto? userInfo)
        {
            if (userInfo == null)
                return new UserDto();
            
            // 直接返回UserDto，不需要转换
            return userInfo;
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
            public UserDto? CurrentUser { get; set; }
            public string? Token { get; set; }
            public DateTime? AuthenticatedAt { get; set; }
        }
        
        #endregion
    }
}