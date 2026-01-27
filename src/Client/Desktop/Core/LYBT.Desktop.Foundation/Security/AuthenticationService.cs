using LYBT.Desktop.Contracts.Api;
using LYBT.Shared.ExceptionHandling.Mappers;
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
        private readonly ICredentialVault _credentialVault;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(
            IAuthApi authApi,
            ITokenStorageService tokenStorage,
            ITokenValidator tokenValidator,
            ICredentialVault credentialVault,
            ILogger<AuthenticationService> logger)
        {
            _authApi = authApi;
            _tokenStorage = tokenStorage;
            _tokenValidator = tokenValidator;
            _credentialVault = credentialVault;
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
                return ServiceResult<LoginResponse>.Failure(ClientErrorMessageMapper.GetSafeOperationFailureMessage("登录", ex));
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
                    // OpenSpec: simplify-auth-architecture - 登出时保留AutoLoginToken
                    // 用户主动登出不清除自动登录凭据，只有取消勾选"自动登录"时才清除
                    return ServiceResult.Success("本地登出成功");
                }

                // 调用 IAuthApi.LogoutAsync(LogoutRequest)
                var logoutRequest = new LogoutRequest
                {
                    UserName = username,
                    RefreshToken = loginResponse?.RefreshToken
                };

                var apiResponse = await _authApi.LogoutAsync(logoutRequest);

                // 清除本地 Token（JWT会话Token）
                await _tokenStorage.ClearAuthenticationAsync();
                
                // OpenSpec: simplify-auth-architecture - 登出时保留AutoLoginToken
                // 用户主动登出不清除自动登录凭据，只有取消勾选"自动登录"时才清除

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
                // 即使异常,也清除本地 Token（但保留AutoLoginToken）
                await _tokenStorage.ClearAuthenticationAsync();
                return ServiceResult.Success("本地登出成功");
            }
        }

        /// <summary>
        /// 获取当前用户信息 (异步)
        /// </summary>
        public async Task<UserDetailDto?> GetCurrentUserAsync()
        {
            var loginResponse = await _tokenStorage.GetLoginResponseAsync();
            return loginResponse?.User;
        }

        /// <summary>
        /// 获取当前用户信息 (同步，用于属性访问)
        /// refactor-auth-role-system Phase 1.2
        /// </summary>
        public UserDetailDto? GetCurrentUser()
        {
            var loginResponse = _tokenStorage.GetLoginResponse();
            return loginResponse?.User;
        }

        /// <summary>
        /// 获取当前令牌
        /// refactor-auth-role-system Phase 1.2: 使用同步方法避免死锁
        /// </summary>
        public string? GetToken()
        {
            return _tokenStorage.GetToken();
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
                return ServiceResult<ValidateTokenResponse>.Failure(ClientErrorMessageMapper.GetSafeOperationFailureMessage("Token验证", ex));
            }
        }

        /// <summary>
        /// 清除认证信息
        /// refactor-auth-role-system Phase 1.2: 使用同步方法避免死锁
        /// </summary>
        public void ClearAuthInfo()
        {
            _tokenStorage.ClearAuthentication();
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

        // Issue #2262: ChangePasswordAsync已移除
        // 职责分离：密码修改统一使用IUserRepository.ChangePasswordAsync
        // Auth服务负责认证，User服务负责用户管理（包括密码修改）

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
                return ServiceResult<LoginResponse>.Failure(ClientErrorMessageMapper.GetSafeOperationFailureMessage("自动登录", ex));
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
