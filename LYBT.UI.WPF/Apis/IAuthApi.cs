using LYBT.Module.Auth.Dtos;
using Refit;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Apis {
    /// <summary>
    /// 认证相关的 API 接口
    /// </summary>
    public interface IAuthApi {
        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="dto">登录请求数据</param>
        /// <returns>登录响应数据</returns>
        [Post("/api/Auth/login")]
        Task<LoginResponseDto> LoginAsync([Body] LoginRequestDto dto);

        /// <summary>
        /// 用户登出
        /// </summary>
        /// <param name="dto">登出请求数据</param>
        /// <returns>登出响应</returns>
        [Post("/api/Auth/logout")]
        Task<Module.Auth.Dtos.ApiResponse<object>> LogoutAsync([Body] LogoutRequestDto dto);
    }
}
