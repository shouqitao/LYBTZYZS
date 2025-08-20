using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Modules.Auth.Api
{
    /// <summary>
    /// 认证API客户端接口 - UltraThink v2.0统一标准
    /// 使用简化的API响应格式，与其他模块保持一致
    /// </summary>
    public interface IAuthApi
    {
        /// <summary>
        /// 用户登录
        /// </summary>
        [Post("/api/v1/auth/login")]
        Task<Refit.ApiResponse<LoginResponse>> LoginAsync([Body] LoginRequest loginRequest);

        /// <summary>
        /// 用户登出
        /// </summary>
        [Post("/api/v1/auth/logout")]
        Task<Refit.ApiResponse<object>> LogoutAsync();

        /// <summary>
        /// 健康检查
        /// </summary>
        [Get("/api/health")]
        Task<Refit.ApiResponse<string>> HealthCheckAsync();

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        [Get("/api/v1/auth/current-user")]
        Task<Refit.ApiResponse<UserDto>> GetCurrentUserAsync();

        /// <summary>
        /// 刷新JWT令牌
        /// </summary>
        [Post("/api/v1/auth/refresh-token")]
        Task<Refit.ApiResponse<LoginResponse>> RefreshTokenAsync();

        /// <summary>
        /// 修改密码
        /// </summary>
        [Post("/api/v1/auth/change-password")]
        Task<Refit.ApiResponse<object>> ChangePasswordAsync([Body] ChangePasswordRequest changePasswordRequest);
    }
}