using LYBT.Common.Enums.Users;

namespace LYBT.UI.WPF.Services {

    /// <summary>
    /// 定义认证服务接口
    /// </summary>
    public interface IAuthService {

        /// <summary>
        /// 验证用户名和密码，返回对应的用户角色。登录失败返回 null
        /// </summary>
        UserRole? Login(string userName, string password);
    }
}