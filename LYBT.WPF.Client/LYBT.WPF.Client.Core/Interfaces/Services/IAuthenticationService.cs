using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Models.Authentication;
using LYBT.WPF.Client.Core.Models.Common;

namespace LYBT.WPF.Client.Core.Interfaces.Services
{
    /// <summary>
    /// 认证服务接口
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="request">登录请求</param>
        /// <returns>登录响应</returns>
        Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest request);

        /// <summary>
        /// 用户登出
        /// </summary>
        /// <returns></returns>
        Task<ApiResponse<object>> LogoutAsync();

        /// <summary>
        /// 检查是否已登录
        /// </summary>
        /// <returns></returns>
        bool IsLoggedIn { get; }

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        /// <returns></returns>
        Task<LYBT.WPF.Client.Core.Models.Users.UserInfo?> GetCurrentUserAsync();

        /// <summary>
        /// 获取存储的Token
        /// </summary>
        /// <returns></returns>
        string? GetToken();

        /// <summary>
        /// 清除认证信息
        /// </summary>
        void ClearAuthInfo();
    }
}