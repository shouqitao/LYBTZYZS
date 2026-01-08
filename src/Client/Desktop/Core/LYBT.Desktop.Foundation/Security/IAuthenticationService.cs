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
        /// 获取当前用户信息 (异步)
        /// </summary>
        Task<UserDetailDto?> GetCurrentUserAsync();

        /// <summary>
        /// 获取当前用户信息 (同步，用于属性访问)
        /// refactor-auth-role-system Phase 1.2
        /// </summary>
        UserDetailDto? GetCurrentUser();

        /// <summary>
        /// 获取当前令牌
        /// </summary>
        string? GetToken();

        /// <summary>
        /// 验证Token并返回详细信息 - Issue #1824
        /// </summary>
        Task<ServiceResult<ValidateTokenResponse>> ValidateTokenAsync(string token);

        /// <summary>
        /// 清除认证信息
        /// </summary>
        void ClearAuthInfo();

        /// <summary>
        /// 检查连接状态
        /// </summary>
        Task<bool> CheckConnectionAsync();

        // Issue #2262: ChangePasswordAsync已移除
        // 职责分离：密码修改统一使用IUserRepository.ChangePasswordAsync
        // Auth服务负责认证，User服务负责用户管理（包括密码修改）

        /// <summary>
        /// 使用AutoLoginToken自动登录
        /// OpenSpec: refactor-login-authentication (CVT-001)
        /// </summary>
        /// <param name="request">自动登录请求</param>
        /// <returns>登录响应</returns>
        Task<ServiceResult<LoginResponse>> LoginWithAutoTokenAsync(AutoLoginRequest request);
    }
}
