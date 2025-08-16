using LYBT.Shared.Models.Contracts.Common;
using System.Threading.Tasks;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Contracts.Auth;


// UltraThink重构: 统一UserInfo和UserDto，使用UserDto作为统一模型
using UserInfo = LYBT.Shared.Models.Contracts.Users.UserDto;

namespace LYBT.Desktop.Core.Interfaces.Services
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
        Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request);

        /// <summary>
        /// 用户登出
        /// </summary>
        /// <returns></returns>
        Task<ServiceResult> LogoutAsync();

        /// <summary>
        /// 检查是否已登录
        /// </summary>
        /// <returns></returns>
        bool IsLoggedIn { get; }

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        /// <returns></returns>
        Task<UserInfo?> GetCurrentUserAsync();

        /// <summary>
        /// 获取存储的Token
        /// </summary>
        /// <returns></returns>
        string? GetToken();

        /// <summary>
        /// 清除认证信息
        /// </summary>
        void ClearAuthInfo();

        /// <summary>
        /// 检查API连接状态
        /// </summary>
        /// <returns>API是否在线</returns>
        Task<bool> CheckConnectionAsync();
    }
}