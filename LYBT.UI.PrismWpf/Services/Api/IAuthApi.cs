using Refit;
using LYBT.UI.PrismWpf.Models;
using LYBT.Common.Responses;

namespace LYBT.UI.PrismWpf.Services.Api
{
    /// <summary>
    /// 认证API接口
    /// </summary>
    public interface IAuthApi
    {
        /// <summary>
        /// 用户登录
        /// </summary>
        [Post("/api/Auth/login")]
        Task<ApiResponse<LoginTokenResponse>> LoginAsync([Body] LoginRequest request);

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        [Get("/api/Auth/current")]
        [Headers("Authorization: Bearer")]
        Task<ApiResponse<UserInfo>> GetCurrentUserAsync();

        /// <summary>
        /// 用户注销
        /// </summary>
        [Post("/api/Auth/logout")]
        [Headers("Authorization: Bearer")]
        Task<ApiResponse<object>> LogoutAsync();
    }

    /// <summary>
    /// 登录令牌响应
    /// </summary>
    public class LoginTokenResponse
    {
        /// <summary>
        /// 访问令牌
        /// </summary>
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// 用户信息
        /// </summary>
        public UserInfo User { get; set; } = new();
    }
}