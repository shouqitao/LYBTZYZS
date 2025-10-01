using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Json;

namespace LYBT.Desktop.Services.Business
{
    /// <summary>
    /// 认证服务实现 - 连接 Server API 的真实实现
    /// Issue #835: 替换 Mock 实现,支持管理员和医生角色登录/退出
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly ILogger<AuthService> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITokenStorageService _tokenStorage;
        private readonly IConfiguration _configuration;
        private LoginResponse? _currentLoginResponse;

        public AuthService(
            ILogger<AuthService> logger,
            IHttpClientFactory httpClientFactory,
            ITokenStorageService tokenStorage,
            IConfiguration configuration)
        {
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _tokenStorage = tokenStorage;
            _configuration = configuration;
        }

        /// <summary>
        /// 用户登录验证
        /// </summary>
        public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogInformation("开始用户登录: {Username}", request.Username);

                // 创建 HttpClient
                var httpClient = _httpClientFactory.CreateClient();
                var baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5001/";
                httpClient.BaseAddress = new Uri(baseUrl);
                httpClient.Timeout = TimeSpan.FromSeconds(_configuration.GetValue<int>("ApiSettings:TimeoutSeconds", 60));

                // 调用 Server API
                var response = await httpClient.PostAsJsonAsync("/api/v1/auth/login", request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(cancellationToken);

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        _currentLoginResponse = apiResponse.Data;

                        // 保存 Token 到本地(根据 RememberMe 决定是否持久化)
                        await _tokenStorage.SaveAuthenticationAsync(apiResponse.Data, request.RememberMe);

                        _logger.LogInformation("用户登录成功: {Username}, Role: {Role}, RememberMe: {RememberMe}",
                            apiResponse.Data.User.UserName,
                            apiResponse.Data.User.Role,
                            request.RememberMe);

                        return ServiceResult<LoginResponse>.Success(apiResponse.Data);
                    }
                    else
                    {
                        var errorMsg = apiResponse?.Message ?? "登录失败,未知错误";
                        _logger.LogWarning("登录失败: {Username}, 原因: {Error}", request.Username, errorMsg);
                        return ServiceResult<LoginResponse>.Failure(errorMsg);
                    }
                }
                else
                {
                    var errorMsg = response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.Unauthorized => "用户名或密码错误",
                        System.Net.HttpStatusCode.InternalServerError => "服务器错误,请稍后重试",
                        _ => $"登录失败: {response.ReasonPhrase}"
                    };

                    _logger.LogWarning("登录失败: {Username}, StatusCode: {StatusCode}", request.Username, response.StatusCode);
                    return ServiceResult<LoginResponse>.Failure(errorMsg);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "网络请求失败");
                return ServiceResult<LoginResponse>.Failure("无法连接到服务器,请检查网络", ex);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogError(ex, "请求超时");
                return ServiceResult<LoginResponse>.Failure("请求超时,请稍后重试", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登录过程中发生未知错误");
                return ServiceResult<LoginResponse>.Failure("登录失败: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// 用户登出
        /// </summary>
        public async Task<ServiceResult<bool>> LogoutAsync(LogoutRequest request)
        {
            try
            {
                _logger.LogInformation("开始用户登出: {Username}", request.Username);

                // 创建 HttpClient
                var httpClient = _httpClientFactory.CreateClient();
                var baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5001/";
                httpClient.BaseAddress = new Uri(baseUrl);

                // 添加 Authorization Header
                var token = await _tokenStorage.GetTokenAsync();
                if (!string.IsNullOrEmpty(token))
                {
                    httpClient.DefaultRequestHeaders.Authorization =
                        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                }

                // 调用 Server API
                var response = await httpClient.PostAsJsonAsync("/api/v1/auth/logout", request);

                if (response.IsSuccessStatusCode)
                {
                    _currentLoginResponse = null;
                    _logger.LogInformation("用户登出成功: {Username}", request.Username);
                    return ServiceResult<bool>.Success(true);
                }
                else
                {
                    _logger.LogWarning("登出失败: {Username}, StatusCode: {StatusCode}", request.Username, response.StatusCode);
                    // 即使服务器登出失败,也清除本地状态
                    _currentLoginResponse = null;
                    return ServiceResult<bool>.Success(true, "本地登出成功");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登出过程中发生错误");
                // 登出失败也清除本地状态
                _currentLoginResponse = null;
                return ServiceResult<bool>.Success(true, "本地登出成功");
            }
        }

        /// <summary>
        /// 修改sysadmin密码
        /// </summary>
        public Task<ServiceResult<bool>> ChangeSysAdminPasswordAsync(ChangeSysAdminPassword request)
        {
            _logger.LogWarning("ChangeSysAdminPasswordAsync 未实现");
            return Task.FromResult(ServiceResult<bool>.Failure("功能未实现"));
        }

        /// <summary>
        /// 验证用户凭据
        /// </summary>
        public Task<ServiceResult<string>> VerifyCredentialsAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {
            _logger.LogWarning("VerifyCredentialsAsync 未实现");
            return Task.FromResult(ServiceResult<string>.Failure("功能未实现"));
        }

        /// <summary>
        /// 刷新Token
        /// </summary>
        public async Task<ServiceResult<LoginResponse>> RefreshTokenAsync(string refreshToken)
        {
            try
            {
                _logger.LogInformation("开始刷新Token");

                // 创建 HttpClient
                var httpClient = _httpClientFactory.CreateClient();
                var baseUrl = _configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5001/";
                httpClient.BaseAddress = new Uri(baseUrl);

                // 调用 Server API (假设有刷新Token端点)
                var response = await httpClient.PostAsJsonAsync("/api/v1/auth/refresh", new { RefreshToken = refreshToken });

                if (response.IsSuccessStatusCode)
                {
                    var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();

                    if (apiResponse?.Success == true && apiResponse.Data != null)
                    {
                        _currentLoginResponse = apiResponse.Data;
                        _logger.LogInformation("Token刷新成功");
                        return ServiceResult<LoginResponse>.Success(apiResponse.Data);
                    }
                }

                _logger.LogWarning("Token刷新失败");
                return ServiceResult<LoginResponse>.Failure("Token刷新失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "刷新Token时发生错误");
                return ServiceResult<LoginResponse>.Failure("刷新Token失败: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// 验证Token有效性
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateTokenAsync(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return ServiceResult<bool>.Failure("Token为空");
            }

            // 检查本地过期时间
            var isExpired = await _tokenStorage.IsTokenExpiredAsync();
            if (isExpired)
            {
                return ServiceResult<bool>.Failure("Token已过期");
            }

            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 获取用户会话信息
        /// </summary>
        public async Task<ServiceResult<object>> GetSessionInfoAsync(string token)
        {
            var loginResponse = await _tokenStorage.GetLoginResponseAsync();

            if (loginResponse != null)
            {
                return ServiceResult<object>.Success(new
                {
                    loginResponse.User.UserName,
                    loginResponse.User.Role,
                    loginResponse.ExpiresAt
                });
            }

            return ServiceResult<object>.Failure("未找到会话信息");
        }

        /// <summary>
        /// 撤销RefreshToken
        /// </summary>
        public Task<ServiceResult<bool>> RevokeTokenAsync(RevokeTokenRequest request)
        {
            _logger.LogWarning("RevokeTokenAsync 未实现");
            return Task.FromResult(ServiceResult<bool>.Failure("功能未实现"));
        }

        /// <summary>
        /// 保存认证信息到本地 - 供外部调用(如 LoginViewModel)
        /// 注意:LoginAsync 已自动保存 Token,此方法可用于重新保存或更新
        /// </summary>
        public async Task SaveAuthenticationAsync(LoginResponse loginResponse)
        {
            // 默认持久化保存(LoginViewModel 期望的行为)
            await _tokenStorage.SaveAuthenticationAsync(loginResponse, rememberMe: true);
            _currentLoginResponse = loginResponse;
            _logger.LogInformation("认证信息已保存(外部调用)");
        }

        /// <summary>
        /// 修改密码 - 兼容旧接口(ChangePasswordDialogViewModel 使用)
        /// </summary>
        public Task<bool> ChangePasswordAsync(string oldPassword, string newPassword)
        {
            _logger.LogWarning("ChangePasswordAsync(oldPassword, newPassword) 未实现");
            return Task.FromResult(false);
        }
    }
}
