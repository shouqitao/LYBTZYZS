using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Services.Auth
{
    /// <summary>
    /// 认证服务实现 - 适配器模式
    /// 将 IAuthService(Shared.Interfaces) 适配为 IAuthenticationService(Desktop.Services)
    /// Issue #835: 连接真实 AuthService
    /// </summary>
    public class AuthenticationService : IAuthenticationService
    {
        private readonly IAuthService _authService;
        private readonly Business.ITokenStorageService _tokenStorage;
        private readonly ILogger<AuthenticationService> _logger;

        public AuthenticationService(
            IAuthService authService,
            Business.ITokenStorageService tokenStorage,
            ILogger<AuthenticationService> logger)
        {
            _authService = authService;
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
        /// 用户登录
        /// </summary>
        public async Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request)
        {
            return await _authService.LoginAsync(request);
        }

        /// <summary>
        /// 用户登出
        /// </summary>
        public async Task<ServiceResult> LogoutAsync()
        {
            try
            {
                // 获取当前用户名
                var loginResponse = await _tokenStorage.GetLoginResponseAsync();
                var username = loginResponse?.User.UserName ?? "unknown";

                // 调用 IAuthService.LogoutAsync(LogoutRequest)
                var logoutRequest = new LogoutRequest
                {
                    Username = username,
                    RefreshToken = loginResponse?.RefreshToken
                };

                var result = await _authService.LogoutAsync(logoutRequest);

                // 清除本地 Token
                await _tokenStorage.ClearAuthenticationAsync();

                if (result.IsSuccess)
                {
                    return ServiceResult.Success("登出成功");
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
        /// 清除认证信息
        /// </summary>
        public void ClearAuthInfo()
        {
            _tokenStorage.ClearAuthenticationAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// 检查连接状态
        /// </summary>
        public async Task<bool> CheckConnectionAsync()
        {
            try
            {
                // 简单检查:验证 Token 是否过期
                var isExpired = await _tokenStorage.IsTokenExpiredAsync();
                return !isExpired;
            }
            catch
            {
                return false;
            }
        }
    }
}
