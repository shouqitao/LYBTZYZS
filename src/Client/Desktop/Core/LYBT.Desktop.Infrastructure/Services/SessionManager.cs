using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Foundation.Security;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 会话管理器实现 - 包装 AuthenticationService 提供会话功能
    /// Issue #1194 Phase 2: 扩展功能完整实现，支持 RefreshToken、CurrentUserId 等
    /// </summary>
    public class SessionManager : ISessionManager
    {
        private readonly IAuthenticationService _authService;
        private UserDto? _cachedUser;
        private string? _cachedToken;
        private string? _cachedRefreshToken;

        /// <summary>
        /// 会话即将过期事件
        /// </summary>
#pragma warning disable CS0067 // 事件从未使用
        public event EventHandler? SessionExpiring;
#pragma warning restore CS0067

        /// <summary>
        /// 会话已过期事件
        /// </summary>
#pragma warning disable CS0067 // 事件从未使用
        public event EventHandler? SessionExpired;
#pragma warning restore CS0067

        /// <summary>
        /// 会话变化事件
        /// </summary>
        public event EventHandler<SessionChangedEventArgs>? SessionChanged;

        /// <summary>
        /// 构造函数
        /// </summary>
        public SessionManager(IAuthenticationService authService)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
        }

        // ==================== 用户信息属性实现 ====================

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
        /// 当前用户ID
        /// </summary>
        public Guid? CurrentUserId => CurrentUser?.Id;

        /// <summary>
        /// 当前用户名
        /// </summary>
        public string? CurrentUserName => CurrentUser?.UserName;

        // ==================== 认证状态属性实现 ====================

        /// <summary>
        /// 是否已认证
        /// </summary>
        public bool IsAuthenticated => !string.IsNullOrEmpty(CurrentToken);

        /// <summary>
        /// 是否已登录（IsAuthenticated 的别名）
        /// </summary>
        public bool IsLoggedIn => IsAuthenticated;

        // ==================== Token 属性实现 ====================

        /// <summary>
        /// 当前Token（访问令牌）
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
        /// 访问令牌（CurrentToken 的别名）
        /// </summary>
        public string? AccessToken => CurrentToken;

        /// <summary>
        /// 刷新令牌
        /// </summary>
        public string? RefreshToken => _cachedRefreshToken;

        // ==================== 会话管理方法实现 ====================

        /// <summary>
        /// 设置当前用户
        /// </summary>
        public void SetCurrentUser(UserDto user, string token)
        {
            _cachedUser = user ?? throw new ArgumentNullException(nameof(user));
            _cachedToken = token ?? throw new ArgumentNullException(nameof(token));
        }

        /// <summary>
        /// 设置会话信息（支持刷新令牌）
        /// </summary>
        public void SetSession(UserDto user, string accessToken, string? refreshToken = null)
        {
            _cachedUser = user ?? throw new ArgumentNullException(nameof(user));
            _cachedToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
            _cachedRefreshToken = refreshToken;

            // 触发会话变化事件
            SessionChanged?.Invoke(this, new SessionChangedEventArgs(true, user));
        }

        /// <summary>
        /// 设置用户会话（SetSession 的别名，兼容性保留）
        /// </summary>
        public void SetUserSession(UserDto user, string token)
        {
            SetSession(user, token);
        }

        /// <summary>
        /// 更新访问令牌
        /// </summary>
        public void UpdateAccessToken(string accessToken)
        {
            _cachedToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
        }

        /// <summary>
        /// 清除会话
        /// </summary>
        public void ClearSession()
        {
            var wasAuthenticated = IsAuthenticated;

            _cachedUser = null;
            _cachedToken = null;
            _cachedRefreshToken = null;
            _authService.ClearAuthInfo();

            // 触发会话变化事件
            if (wasAuthenticated)
            {
                SessionChanged?.Invoke(this, new SessionChangedEventArgs(false));
            }
        }

        /// <summary>
        /// 清除用户会话（ClearSession 的别名，兼容性保留）
        /// </summary>
        public void ClearUserSession()
        {
            ClearSession();
        }

        // ==================== 权限检查方法实现 ====================

        /// <summary>
        /// 检查权限（基于 UserRole 枚举）
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
        /// 检查权限（基于字符串）
        /// </summary>
        public bool HasPermission(string permission)
        {
            // 简单实现：已登录即有权限
            // 未来可扩展为基于权限字符串的细粒度检查
            return IsAuthenticated && CurrentUser != null;
        }

        /// <summary>
        /// 检查角色（基于字符串）
        /// </summary>
        public bool HasRole(string role)
        {
            if (CurrentUser == null)
            {
                return false;
            }

            return CurrentUser.Role.ToString().Equals(role, StringComparison.OrdinalIgnoreCase);
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
                _ => CurrentUser.Role.ToString()
            };
        }
    }
}
