using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// 认证服务实现：提供模拟的登录验证逻辑
    /// </summary>
    public class AuthService : IAuthService {
        public bool Login(string userName, string password) {
            // 模拟一个简单的认证逻辑
            // 例如：用户名为 "admin" 且密码为 "123" 时认为验证通过
            if (userName == "admin" && password == "123")
                return true;
            else
                return false;
        }
    }
}
