using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Module.Auth.Interfaces
{

    /// <summary>
    /// 身份认证服务接口 - 只负责身份验证，不涉及用户信息管理
    /// </summary>
    public interface IAuthService
    {

        /// <summary>
        /// 验证用户名和密码，成功返回用户名，失败返回null
        /// </summary>
        Task<string?> VerifyCredentialsAsync(LoginRequest dto);

        /// <summary>
        /// 用户登出
        /// </summary>
        Task<bool> LogoutAsync(LogoutRequest dto);

        /// <summary>
        /// 修改 sysadmin 密码
        /// </summary>
        Task<bool> ChangeSysAdminPasswordAsync(ChangeSysAdminPassword dto);
    }
}