using LYBT.Common.Enums.Users;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// 认证服务接口，提供登录、登出、Token管理、自动登录等功能
    /// </summary>
    public interface IAuthService {
        /// <summary>
        /// JWT Token
        /// </summary>
        string Token { get; }

        /// <summary>
        /// 当前登录用户ID
        /// </summary>
        Guid UserId { get; }

        /// <summary>
        /// 是否已记住用户信息
        /// </summary>
        bool HasRemembered { get; }

        /// <summary>
        /// 记住的用户名
        /// </summary>
        string RememberedUserName { get; }

        /// <summary>
        /// 记住的密码
        /// </summary>
        string RememberedPassword { get; }

        /// <summary>
        /// 用户登录
        /// </summary>
        /// <param name="userName">用户名</param>
        /// <param name="password">密码</param>
        /// <returns>登录结果：(成功标志, 用户角色列表, 错误消息, Token)</returns>
        Task<(bool success, IList<UserRole> roles, string errorMessage, string token)> LoginAsync(string userName, string password);

        /// <summary>
        /// 用户登出
        /// </summary>
        /// <returns>登出是否成功</returns>
        Task<bool> LogoutAsync();

        /// <summary>
        /// 清除自动登录信息
        /// </summary>
        void ClearAutoLoginInfo();

        /// <summary>
        /// 修改 sysadmin 密码
        /// </summary>
        Task<bool> ChangeSysAdminPasswordAsync(string oldPassword, string newPassword);
    }
}
