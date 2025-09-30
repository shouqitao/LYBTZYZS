using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Services.Auth
{
    /// <summary>
    /// 认证服务接口 - 简化版本
    /// 遵循"适度设计、拒绝过度工程"原则
    /// 提供基本的认证功能
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// 用户是否已登录
        /// </summary>
        bool IsLoggedIn { get; }

        /// <summary>
        /// 用户登录
        /// </summary>
        Task<ServiceResult<LoginResponse>> LoginAsync(LoginRequest request);

        /// <summary>
        /// 用户登出
        /// </summary>
        Task<ServiceResult> LogoutAsync();

        /// <summary>
        /// 获取当前用户信息
        /// </summary>
        Task<UserDto?> GetCurrentUserAsync();

        /// <summary>
        /// 获取当前令牌
        /// </summary>
        string? GetToken();

        /// <summary>
        /// 清除认证信息
        /// </summary>
        void ClearAuthInfo();

        /// <summary>
        /// 检查连接状态
        /// </summary>
        Task<bool> CheckConnectionAsync();
    }
}