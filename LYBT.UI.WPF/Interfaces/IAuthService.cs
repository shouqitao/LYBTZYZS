using LYBT.Common.Enums.Users;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// 认证服务接口，支持异步登录、Token管理、自动登录
    /// </summary>
    public interface IAuthService {
        Task<(bool success, IList<UserRole> roles, string errorMessage, string token)> LoginAsync(string userName, string password);

        string Token { get; }
        bool HasRemembered { get; }
        void ClearAutoLoginInfo();
        string RememberedUserName { get; }
        string RememberedPassword { get; }

        Guid CurrentUserId { get; }
    }
}
