using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Foundation.Security
{
    /// <summary>
    /// 认证服务接口 - 简化版本
    /// 遵循"适度设计、拒绝过度工程"原则
    /// 提供基本的认证功能
    /// </summary>
    public interface IAuthenticationService
    {
        /// <summary>
        /// 异步检查用户是否已登录
        /// </summary>
        Task<bool> IsLoggedInAsync();

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

        /// <summary>
        /// 修改密码
        /// </summary>
        /// <param name="currentPassword">当前密码</param>
        /// <param name="newPassword">新密码</param>
        /// <returns>修改是否成功</returns>
        Task<bool> ChangePasswordAsync(string currentPassword, string newPassword);
    }
}
