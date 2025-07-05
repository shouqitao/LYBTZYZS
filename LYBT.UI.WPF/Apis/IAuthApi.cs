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
        Task<LYBT.Common.Responses.ApiResponse<LoginResponseDto>> LoginAsync([Body] LoginRequestDto dto);

        /// <summary>
        /// 用户登出
        /// </summary>
        /// <param name="dto">登出请求</param>
        /// <returns>登出响应</returns>
        [Post("/api/Auth/logout")]
        Task<LYBT.Common.Responses.ApiResponse<object>> LogoutAsync([Body] LogoutRequestDto dto);

        /// <summary>
        /// 修改 sysadmin 密码
        /// </summary>
        [Post("/api/Auth/changeSysAdminPassword")]
        Task<LYBT.Common.Responses.ApiResponse<object>> ChangeSysAdminPasswordAsync([Body] ChangeSysAdminPasswordDto dto);
    }
}
