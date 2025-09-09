using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;
using AuthContracts = LYBT.Shared.Models.Contracts.Auth;

// UltraThink重构: 恢复四层架构清晰分离，UserInfo为UI层，UserDto为传输层
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
        Task<ServiceResult<AuthContracts.LoginResponse>> LoginAsync(AuthContracts.LoginRequest request);

        /// <summary>
        /// 用户登出
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task<ServiceResult> LogoutAsync();

        /// <summary>
        /// 检查是否已登录
        /// </summary>
        /// <returns></returns>
        bool IsLoggedIn { get; }

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task<UserDto?> GetCurrentUserAsync();

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
