using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Services.Interfaces
{
    /// <summary>
    /// 认证API服务接口 - 统一标准
    /// </summary>
    public interface IAuthApiService
    {
        /// <summary>
        /// 用户登录
        /// </summary>
        [Post("/api/v1/auth/login")]
        Task<Refit.ApiResponse<LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>>> LoginAsync([Body] LoginRequest loginRequest);

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
        Task<Refit.ApiResponse<BaseUser>> GetCurrentUserAsync();

        /// <summary>
        /// 刷新JWT令牌
        /// </summary>
        [Post("/api/v1/auth/refresh-token")]
        Task<Refit.ApiResponse<object>> RefreshTokenAsync();

        /// <summary>
        /// 修改密码
        /// </summary>
        [Post("/api/v1/auth/change-password")]
        Task<Refit.ApiResponse<object>> ChangePasswordAsync([Body] object changePasswordRequest);
    }
}