using LYBT.Desktop.Contracts.Api;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// 认证服务实现 - ADR-002合规版本
    /// Desktop端Infrastructure Service，直接调用HTTP API（IAuthApi）
    /// 不依赖Server端Service接口，符合架构决策
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IAuthApi _authApi;
        private readonly ITokenStorageService _tokenStorage;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(
            IAuthApi authApi,
            ITokenStorageService tokenStorage,
            ILogger<AuthenticationService> logger)
        {
            _authApi = authApi;
            _tokenStorage = tokenStorage;
            _logger = logger;
        }

        /// <summary>
        /// 异步检查用户是否已登录
        /// </summary>
        public async Task<bool> IsLoggedInAsync()
        {
            var token = await _tokenStorage.GetTokenAsync();
            return !string.IsNullOrEmpty(token);
        }

        /// <summary>
        /// 用户登录 - 调用HTTP API
        /// </summary>
        public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
        {
            try
            {
                var apiResponse = await _authApi.LoginAsync(request);

                if (apiResponse.Success && apiResponse.Data != null)
                {
                    return ServiceResult<LoginResponse>.Success(apiResponse.Data, apiResponse.Message);
                }
                else
                {
                    return ServiceResult<LoginResponse>.Failure(apiResponse.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登录失败");
                return ServiceResult<LoginResponse>.Failure($"登录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 用户登出 - 调用HTTP API
        /// </summary>
        public async Task<ServiceResult> LogoutAsync()
        {
            try
            {
                // 获取当前用户名
                var loginResponse = await _tokenStorage.GetLoginResponseAsync();
                var username = loginResponse?.User.UserName ?? "unknown";

                // 调用 IAuthApi.LogoutAsync(LogoutRequest)
                var logoutRequest = new LogoutRequest
                {
                    Username = username,
                    RefreshToken = loginResponse?.RefreshToken
                };

                var apiResponse = await _authApi.LogoutAsync(logoutRequest);

                // 清除本地 Token
                await _tokenStorage.ClearAuthenticationAsync();

                if (apiResponse.Success)
                {
                    return ServiceResult.Success(apiResponse.Message);
                }
                else
                {
                    // 即使服务器登出失败,本地 Token 已清除,视为成功
                    return ServiceResult.Success("本地登出成功");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "登出失败");
                // 即使异常,也清除本地 Token
                await _tokenStorage.ClearAuthenticationAsync();
                return ServiceResult.Success("本地登出成功");
            }
        }

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        public async Task<UserDto?> GetCurrentUserAsync()
        {
            var loginResponse = await _tokenStorage.GetLoginResponseAsync();
            return loginResponse?.User;
        }

        /// <summary>
        /// 获取当前令牌
        /// </summary>
        public string? GetToken()
        {
            return _tokenStorage.GetTokenAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// 验证Token并返回详细信息 - Issue #1824
        /// </summary>
        public async Task<ServiceResult<ValidateTokenResponse>> ValidateTokenAsync(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    return ServiceResult<ValidateTokenResponse>.Failure("Token不能为空");
                }

                var request = new ValidateTokenRequest { Token = token };
                var apiResponse = await _authApi.ValidateTokenAsync(request);

                if (apiResponse.Success && apiResponse.Data != null)
                {
                    _logger.LogInformation("Token验证成功: {Username}", apiResponse.Data.Username);
                    return ServiceResult<ValidateTokenResponse>.Success(apiResponse.Data, apiResponse.Message);
                }
                else
                {
                    _logger.LogWarning("Token验证失败: {Message}", apiResponse.Message);
                    return ServiceResult<ValidateTokenResponse>.Failure(apiResponse.Message ?? "Token验证失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token验证发生异常");
                return ServiceResult<ValidateTokenResponse>.Failure($"Token验证失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 清除认证信息
        /// </summary>
        public void ClearAuthInfo()
        {
            _tokenStorage.ClearAuthenticationAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// 检查连接状态 - 调用健康检查API
        /// </summary>
        public async Task<bool> CheckConnectionAsync()
        {
            try
            {
                // 先检查本地Token是否过期
                var isExpired = await _tokenStorage.IsTokenExpiredAsync();
                if (isExpired)
                {
                    return false;
                }

                // 调用健康检查API验证服务可用性
                var healthResponse = await _authApi.HealthCheckAsync();
                return healthResponse != null && healthResponse.Status == "Healthy";
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 修改密码 - 调用HTTP API
        /// </summary>
        public async Task<bool> ChangePasswordAsync(string currentPassword, string newPassword)
        {
            try
            {
                var request = new ChangeSysAdminPassword
                {
                    OldPassword = currentPassword,
                    NewPassword = newPassword
                };

                var apiResponse = await _authApi.ChangeSysAdminPasswordAsync(request);

                if (apiResponse.Success)
                {
                    _logger.LogInformation("密码修改成功");
                    return true;
                }
                else
                {
                    _logger.LogWarning("密码修改失败: {Message}", apiResponse.Message);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改密码时发生异常");
                return false;
            }
        }
    }
}
