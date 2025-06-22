using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// 定义认证服务接口
    /// </summary>
    public interface IAuthService {
        /// <summary>
        /// 验证用户名和密码，模拟登录
        /// </summary>
        bool Login(string userName, string password);
    }
}
