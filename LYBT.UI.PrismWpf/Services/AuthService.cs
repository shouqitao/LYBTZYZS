using LYBT.UI.PrismWpf.Models;
using LYBT.UI.PrismWpf.Services.Api;

namespace LYBT.UI.PrismWpf.Services
{
    /// <summary>
    /// 认证服务接口
    /// </summary>
    public interface IAuthService
    {
        /// <summary>
        /// 用户登录
        /// </summary>
        Task<LoginResponse> LoginAsync(LoginRequest request);

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        UserInfo? CurrentUser { get; }

        /// <summary>
        /// 获取访问令牌
        /// </summary>
        string? AccessToken { get; }

        /// <summary>
        /// 用户注销
        /// </summary>
        Task LogoutAsync();

        /// <summary>
        /// 检查是否已登录
        /// </summary>
        bool IsLoggedIn { get; }

        /// <summary>
        /// 设置令牌
        /// </summary>
        void SetToken(string token);
    }

    /// <summary>
    /// 认证服务实现
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IAuthApi _authApi;
        private UserInfo? _currentUser;
        private string? _accessToken;

        public AuthService(IAuthApi authApi)
        {
            _authApi = authApi;
        }

        public UserInfo? CurrentUser => _currentUser;
        public string? AccessToken => _accessToken;
        public bool IsLoggedIn => _currentUser != null && !string.IsNullOrEmpty(_accessToken);

        public async Task<LoginResponse> LoginAsync(LoginRequest request)
        {
            try
            {
                var response = await _authApi.LoginAsync(request);

                if (response.Success && response.Data != null)
                {
                    _currentUser = response.Data.User;
                    _accessToken = response.Data.Token;

                    return new LoginResponse
                    {
                        Success = true,
                        Token = _accessToken,
                        User = _currentUser
                    };
                }
                else
                {
                    return new LoginResponse
                    {
                        Success = false,
                        Message = response.Message ?? "登录失败"
                    };
                }
            }
            catch (Exception ex)
            {
                return new LoginResponse
                {
                    Success = false,
                    Message = $"网络错误: {ex.Message}"
                };
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                if (IsLoggedIn)
                {
                    await _authApi.LogoutAsync();
                }
            }
            catch
            {
                // 忽略注销错误，强制本地清理
            }
            finally
            {
                _currentUser = null;
                _accessToken = null;
            }
        }

        public void SetToken(string token)
        {
            _accessToken = token;
        }
    }
}