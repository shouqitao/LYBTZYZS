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
    ///
    /// Issue #1864: Token认证安全重构 - 集成客户端JWT自验证
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IAuthApi _authApi;
        private readonly ITokenStorageService _tokenStorage;
        private readonly ITokenValidator _tokenValidator;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(
            IAuthApi authApi,
            ITokenStorageService tokenStorage,
            ITokenValidator tokenValidator,
            ILogger<AuthenticationService> logger)
        {
            _authApi = authApi;
            _tokenStorage = tokenStorage;
            _tokenValidator = tokenValidator;
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
                return ServiceResult<LoginResponse>.Failure("登录失败，请稍后重试");
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

                // 检查Token是否已过期，过期则跳过API调用（避免401异常）
                var isExpired = await _tokenStorage.IsTokenExpiredAsync();
                if (isExpired)
                {
                    _logger.LogInformation("Token已过期，跳过服务端登出API调用");
                    await _tokenStorage.ClearAuthenticationAsync();
                    return ServiceResult.Success("本地登出成功");
                }

                // 调用 IAuthApi.LogoutAsync(LogoutRequest)
                var logoutRequest = new LogoutRequest
                {
                    UserName = username,
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
        public async Task<UserDetailDto?> GetCurrentUserAsync()
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
        /// 验证Token并返回详细信息
        /// Issue #1864: 使用客户端JWT自验证，移除Server API依赖
        /// </summary>
        public async Task<ServiceResult<ValidateTokenResponse>> ValidateTokenAsync(string token)
        {
            try
            {
                if (string.IsNullOrEmpty(token))
                {
                    return ServiceResult<ValidateTokenResponse>.Failure("Token不能为空");
                }

                // Issue #1864: 使用本地Token验证器，移除Server API依赖
                var validationResult = await _tokenValidator.ValidateTokenAsync(token);

                if (validationResult.IsValid && validationResult.UserInfo != null)
                {
                    var userInfo = validationResult.UserInfo;
                    var response = new ValidateTokenResponse
                    {
                        IsValid = true,
                        UserId = null, // Issue #1864: UserId在Token中是Guid，但DTO定义为int?，暂设为null
                        Username = userInfo.UserName,
                        Role = userInfo.Role,
                        ExpiresAt = ExtractTokenExpiration(token), // 提取Token过期时间
                        ErrorMessage = null
                    };

                    _logger.LogInformation("Token本地验证成功: {Username} (UserType: {UserType})",
                        userInfo.UserName, userInfo.UserType);
                    return ServiceResult<ValidateTokenResponse>.Success(response, "Token验证成功");
                }
                else
                {
                    _logger.LogWarning("Token本地验证失败: {ErrorMessage}", validationResult.ErrorMessage);
                    return ServiceResult<ValidateTokenResponse>.Failure(
                        validationResult.ErrorMessage ?? "Token验证失败");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Token验证发生异常");
                return ServiceResult<ValidateTokenResponse>.Failure("Token验证失败，请稍后重试");
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

        /// <summary>
        /// 修改系统管理员密码 (Issue #1892)
        /// </summary>
        public async Task<ServiceResult<bool>> ChangeSysAdminPasswordAsync(ChangeSysAdminPassword request)
        {
            try
            {
                _logger.LogInformation("修改系统管理员密码");

                var apiResponse = await _authApi.ChangeSysAdminPasswordAsync(request);

                if (apiResponse.Success)
                {
                    _logger.LogInformation("系统管理员密码修改成功");
                    return ServiceResult<bool>.Success(true, apiResponse.Message ?? "密码修改成功");
                }
                else
                {
                    var errorMsg = apiResponse.Message ?? "密码修改失败";
                    _logger.LogWarning("系统管理员密码修改失败: {Message}", errorMsg);
                    return ServiceResult<bool>.Failure(errorMsg);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "修改系统管理员密码时发生异常");
                return ServiceResult<bool>.Failure("修改密码失败，请稍后重试");
            }
        }

        /// <summary>
        /// 使用AutoLoginToken自动登录
        /// OpenSpec: refactor-login-authentication (CVT-001)
        /// </summary>
        public async Task<ServiceResult<LoginResponse>> LoginWithAutoTokenAsync(AutoLoginRequest request)
        {
            try
            {
                _logger.LogInformation("使用AutoLoginToken登录 - UserName: {UserName}", request.UserName);

                var apiResponse = await _authApi.LoginWithAutoTokenAsync(request);

                if (apiResponse.Success && apiResponse.Data != null)
                {
                    _logger.LogInformation("AutoLoginToken登录成功 - UserName: {UserName}", request.UserName);
                    return ServiceResult<LoginResponse>.Success(apiResponse.Data, apiResponse.Message);
                }
                else
                {
                    _logger.LogWarning("AutoLoginToken登录失败 - UserName: {UserName}, Message: {Message}",
                        request.UserName, apiResponse.Message);
                    return ServiceResult<LoginResponse>.Failure(apiResponse.Message);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AutoLoginToken登录异常 - UserName: {UserName}", request.UserName);
                return ServiceResult<LoginResponse>.Failure("自动登录失败，请手动登录");
            }
        }

        /// <summary>
        /// 从JWT Token中提取过期时间
        /// Issue #1864: 辅助方法，用于本地Token验证
        /// </summary>
        private DateTime? ExtractTokenExpiration(string token)
        {
            try
            {
                var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
                var jwtToken = tokenHandler.ReadJwtToken(token);
                return jwtToken.ValidTo;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "提取Token过期时间失败");
                return null;
            }
        }
    }
}
