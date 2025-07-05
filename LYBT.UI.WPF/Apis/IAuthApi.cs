using LYBT.Module.Auth.Dtos;
using LYBT.Common.Responses;
using Refit;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Apis {
    /// <summary>
    /// 认证相关 API 接口
    /// </summary>
    public interface IAuthApi {
        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="dto">登录请求</param>
        /// <returns>登录响应</returns>
        [Post("/api/Auth/login")]
        Task<ApiResponse<LoginResponseDto>> LoginAsync([Body] LoginRequestDto dto);

        /// <summary>
        /// 用户登出
        /// </summary>
        /// <param name="dto">登出请求</param>
        /// <returns>登出响应</returns>
        [Post("/api/Auth/logout")]
        Task<ApiResponse<object>> LogoutAsync([Body] LogoutRequestDto dto);
    }
}
