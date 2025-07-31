using System.Threading.Tasks;
using Refit;
using LYBT.WPF.Client.Core.Models.Authentication;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.Shared.Models.Common;

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
        Task<LYBT.Shared.Models.Common.ApiResponse<LYBT.Shared.Models.Auth.LoginResponse>> LoginAsync([Body] object loginRequest);

        /// <summary>
        /// 用户登出
        /// </summary>
        [Post("/api/v1/Auth/logout")]
        Task<LYBT.Shared.Models.Common.ApiResponse<object>> LogoutAsync([Body] object logoutRequest);

        /// <summary>
        /// 健康检查
        /// </summary>
        [Get("/api/health")]
        Task<string> HealthCheckAsync();

        /// <summary>
        /// 模拟登录 - 不依赖数据库
        /// </summary>
        [Post("/api/v1/Auth/mockLogin")]
        Task<LYBT.Shared.Models.Common.ApiResponse<LYBT.Shared.Models.Auth.LoginResponse>> MockLoginAsync([Body] object loginRequest);
    }
}