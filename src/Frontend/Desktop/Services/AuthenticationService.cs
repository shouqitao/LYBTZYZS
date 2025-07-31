using System;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Services;
using LYBT.WPF.Client.Core.Models.Authentication;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Services.Interfaces;
using UserInfo = LYBT.WPF.Client.Core.Models.Users.UserInfo;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 真实的身份认证服务实现
    /// </summary>
    public class AuthenticationService : LYBT.WPF.Client.Core.Interfaces.Services.IAuthenticationService
    {
        private readonly IAuthApiService _authApiService;
        private readonly ITokenManager _tokenManager;
        private bool _isLoggedIn = false;
        private UserInfo? _currentUser;

        public AuthenticationService(IAuthApiService authApiService, ITokenManager tokenManager)
        {
            _authApiService = authApiService;
            _tokenManager = tokenManager;
        }

        public bool IsLoggedIn => _isLoggedIn;

        public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request)
        {
            try
            {
                // 创建后端API格式的登录请求
                var loginDto = new
                {
                    username = request.Username,
                    password = request.Password,
                    rememberMe = request.RememberMe,
                    clientIp = request.ClientIp,
                    userAgent = request.UserAgent,
                    loginType = request.LoginType
                };

                var response = await _authApiService.LoginAsync(loginDto);
                
                if (response.IsSuccess && response.Data != null)
                {
                    _isLoggedIn = true;
                    _tokenManager.SetToken(response.Data.Token);
                    _currentUser = response.Data.User;
                }

                return response;
            }
            catch (Exception ex)
            {
                return new ApiResponse<LoginResponse>
                {
                    IsSuccess = false,
                    Message = $"登录过程中发生错误: {ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<object>> LogoutAsync()
        {
            try
            {
                var logoutDto = new
                {
                    token = _tokenManager.GetToken()
                };

                var response = await _authApiService.LogoutAsync(logoutDto);
                
                // 无论API调用是否成功，都清除本地登录状态
                ClearAuthInfo();
                
                return response.IsSuccess ? response : new ApiResponse<object>
                {
                    IsSuccess = true,
                    Message = "已清除本地登录状态"
                };
            }
            catch (Exception ex)
            {
                // 即使API调用失败，也清除本地状态
                ClearAuthInfo();
                return new ApiResponse<object>
                {
                    IsSuccess = true,
                    Message = $"登出完成，但API调用失败: {ex.Message}"
                };
            }
        }

        public async Task<UserInfo?> GetCurrentUserAsync()
        {
            if (!_isLoggedIn || _currentUser == null)
                return null;

            // 可以考虑从API刷新用户信息
            // var response = await _apiService.GetAsync<UserInfo>("users/current");
            // if (response.Success && response.Data != null)
            // {
            //     _currentUser = response.Data;
            // }

            return _currentUser;
        }

        public string? GetToken()
        {
            return _tokenManager.GetToken();
        }

        public void ClearAuthInfo()
        {
            _isLoggedIn = false;
            _tokenManager.ClearToken();
            _currentUser = null;
        }
    }
}