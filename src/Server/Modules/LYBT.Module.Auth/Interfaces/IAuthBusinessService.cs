using LYBT.Entities.Users;
using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Auth.Interfaces
{
    /// <summary>
    /// 认证业务服务接口
    /// UltraThink架构 - Business层接口抽象
    /// </summary>
    public interface IAuthBusinessService
    {
        /// <summary>
        /// 完整登录流程处理
        /// </summary>
        Task<ServiceResult<LoginResponse>> ProcessLoginAsync(LoginRequest request);

        /// <summary>
        /// 用户登出处理
        /// </summary>
        Task<ServiceResult<bool>> ProcessLogoutAsync(LogoutRequest request);

        /// <summary>
        /// 验证用户密码
        /// </summary>
        Task<ServiceResult<bool>> ValidatePasswordAsync(User user, string password);

        /// <summary>
        /// 修改系统管理员密码
        /// </summary>
        Task<ServiceResult<bool>> ChangeSysAdminPasswordAsync(string newPassword);

        /// <summary>
        /// 验证用户凭据
        /// </summary>
        Task<ServiceResult<string>> VerifyCredentialsAsync(LoginRequest request);
    }
}
