using System.Threading.Tasks;
using Refit;
using LYBT.WPF.Client.Core.Models.Authentication;
using LYBT.WPF.Client.Core.Models.Common;

namespace LYBT.WPF.Client.Services.Interfaces
{
    /// <summary>
    /// 认证API服务接口 - Refit实现
    /// </summary>
    public interface IAuthApiService
    {
        /// <summary>
        /// 用户登录
        /// </summary>
        [Post("/api/v1/Auth/login")]
        Task<Core.Models.Common.ApiResponse<LoginResponse>> LoginAsync([Body] object loginRequest);

        /// <summary>
        /// 用户登出
        /// </summary>
        [Post("/api/v1/Auth/logout")]
        Task<Core.Models.Common.ApiResponse<object>> LogoutAsync([Body] object logoutRequest);

        /// <summary>
        /// 健康检查
        /// </summary>
        [Get("/api/health")]
        Task<string> HealthCheckAsync();
    }
}