using System.Threading.Tasks;
using LYBT.Module.Auth.Dtos;
using LYBT.Module.Users.Dtos;

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
    }
}
