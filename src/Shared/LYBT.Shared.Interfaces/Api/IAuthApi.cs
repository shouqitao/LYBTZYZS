using System.Threading.Tasks;
using Refit;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Interfaces.Api
{
    /// <summary>
    /// 认证API客户端接口 - UltraThink统一API客户端管理器标准
    /// 保持与现有项目兼容的响应格式和类型系统
    /// </summary>
    public interface IAuthApi
    {
        /// <summary>
        /// 用户登录
        /// </summary>
        [Post("/api/v1/auth/login")]
        Task<LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>> LoginAsync([Body] LoginRequest loginRequest);

        /// <summary>
        /// 用户登出
        /// </summary>
        [Post("/api/v1/auth/logout")]
        Task<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>> LogoutAsync();

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        [Get("/api/v1/auth/current-user")]
        Task<LYBT.Shared.Models.Contracts.Common.ApiResponse<UserDto>> GetCurrentUserAsync();

        /// <summary>
        /// 刷新JWT令牌
        /// </summary>
        [Post("/api/v1/auth/refresh-token")]
        Task<LYBT.Shared.Models.Contracts.Common.ApiResponse<LoginResponse>> RefreshTokenAsync();

        /// <summary>
        /// 修改密码
        /// </summary>
        [Post("/api/v1/auth/change-password")]
        Task<LYBT.Shared.Models.Contracts.Common.ApiResponse<object>> ChangePasswordAsync([Body] ChangePasswordRequest changePasswordRequest);

        /// <summary>
        /// 健康检查
        /// </summary>
        [Get("/api/v1/health/alive")]
        Task<string> HealthCheckAsync();
    }
}