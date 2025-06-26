using LYBT.Common.Enums.Users;
using System.Collections.Generic;

namespace LYBT.UI.WPF.Services {

    /// <summary>
    /// 定义认证服务接口
    /// </summary>
    public interface IAuthService {

        /// <summary>
    /// 验证用户名和密码，返回对应的用户角色列表。登录失败返回 null
        /// </summary>
        IList<UserRole>? Login(string userName, string password);
    }
}