using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Services.Auth;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 会话管理器实现 - 包装 AuthenticationService 提供会话功能
    /// Issue #1114 Phase 2: 快速修复 DI 解析问题
    /// </summary>
    public class SessionManager : ISessionManager
    {
        private readonly IAuthenticationService _authService;
        private UserDto? _cachedUser;
        private string? _cachedToken;

        /// <summary>
        /// 会话即将过期事件
        /// </summary>
        public event EventHandler? SessionExpiring;

        /// <summary>
        /// 会话已过期事件
        /// </summary>
        public event EventHandler? SessionExpired;

        /// <summary>
        /// 构造函数
        /// </summary>
        public SessionManager(IAuthenticationService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        /// <summary>
        /// 当前用户
        /// </summary>
        public UserDto? CurrentUser
        {
            get
            {
                if (_cachedUser == null)
                {
                    // 从 AuthenticationService 异步获取用户（同步调用）
                    _cachedUser = _authService.GetCurrentUserAsync()
                        .GetAwaiter()
                        .GetResult();
                }
                return _cachedUser;
            }
        }

        /// <summary>
        /// 是否已认证
        /// </summary>
        public bool IsAuthenticated => !string.IsNullOrEmpty(CurrentToken);

        /// <summary>
        /// 当前Token
        /// </summary>
        public string? CurrentToken
        {
            get
            {
                if (_cachedToken == null)
                {
                    _cachedToken = _authService.GetToken();
                }
                return _cachedToken;
            }
        }

        /// <summary>
        /// 设置当前用户
        /// </summary>
        public void SetCurrentUser(UserDto user, string token)
        {
            _cachedUser = user ?? throw new ArgumentNullException(nameof(user));
            _cachedToken = token ?? throw new ArgumentNullException(nameof(token));
        }

        /// <summary>
        /// 清除会话
        /// </summary>
        public void ClearSession()
        {
            _cachedUser = null;
            _cachedToken = null;
            _authService.ClearAuthInfo();
        }

        /// <summary>
        /// 检查权限
        /// </summary>
        public bool HasPermission(UserRole requiredRole)
        {
            if (CurrentUser == null)
            {
                return false;
            }

            // 角色枚举值越大权限越高
            return CurrentUser.Role >= requiredRole;
        }

        /// <summary>
        /// 是否为管理员
        /// </summary>
        public bool IsAdmin()
        {
            return CurrentUser?.Role == UserRole.Admin;
        }

        /// <summary>
        /// 获取当前用户角色显示名称
        /// </summary>
        public string GetCurrentUserRoleDisplay()
        {
            if (CurrentUser == null)
            {
                return "未登录";
            }

            return CurrentUser.Role switch
            {
                UserRole.Admin => "管理员",
                UserRole.Doctor => "医生",
                UserRole.Receptionist => "前台",
                UserRole.Pharmacist => "药剂师",
                _ => CurrentUser.Role.ToString()
            };
        }
    }
}
