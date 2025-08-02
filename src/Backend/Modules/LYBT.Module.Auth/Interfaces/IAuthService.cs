using LYBT.Shared.Models.Contracts.Auth;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Module.Auth.Interfaces {

    /// <summary>
    /// 登录验证服务接口
    /// </summary>
    public interface IAuthService {

        /// <summary>
        /// 验证用户名和密码
        /// </summary>
        Task<UserDto?> LoginAsync(LoginRequestDto dto);

        /// <summary>
        /// 用户登出
        /// </summary>
        Task<bool> LogoutAsync(LogoutRequestDto dto);

        /// <summary>
        /// 修改 sysadmin 密码
        /// </summary>
        Task<bool> ChangeSysAdminPasswordAsync(ChangeSysAdminPasswordDto dto);
    }
}