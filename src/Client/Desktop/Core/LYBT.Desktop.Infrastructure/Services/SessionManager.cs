using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Security;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>
    /// 会话管理器实现 - 包装AuthenticationService提供会话功能
    /// optimize-desktop-core: 移除Token相关属性，Token由ITokenStorageService管理
    /// refactor-auth-role-system Phase 1.2: 使用同步方法避免死锁
    /// </summary>
    public class SessionManager : ISessionManager
    {
        private readonly IAuthenticationService _authService;
        private UserDetailDto? _cachedUser;

        // OpenSpec: simplify-auth-architecture - SessionExpiring事件已移除
#pragma warning disable CS0067
        public event EventHandler? SessionExpired;
#pragma warning restore CS0067
        public event EventHandler<SessionChangedEventArgs>? SessionChanged;

        public SessionManager(IAuthenticationService authService) => _authService = authService ?? throw new ArgumentNullException(nameof(authService));

        /// <summary>
        /// 当前用户（使用同步方法避免WPF死锁）
        /// </summary>
        public UserDetailDto? CurrentUser { get { if (_cachedUser == null) _cachedUser = _authService.GetCurrentUser(); return _cachedUser; } }
        public Guid? CurrentUserId => CurrentUser?.Id;
        public string? CurrentUserName => CurrentUser?.UserName;
        public bool IsAuthenticated => !string.IsNullOrEmpty(_authService.GetToken());
        public bool IsLoggedIn => IsAuthenticated;

        public void SetSession(UserDetailDto user, string accessToken, string? refreshToken = null)
        {
            _cachedUser = user ?? throw new ArgumentNullException(nameof(user));
            ArgumentNullException.ThrowIfNull(accessToken);
            SessionChanged?.Invoke(this, new SessionChangedEventArgs(true, user));
        }

        public void ClearSession()
        {
            var wasAuthenticated = IsAuthenticated;
            _cachedUser = null;
            _authService.ClearAuthInfo();
            if (wasAuthenticated) SessionChanged?.Invoke(this, new SessionChangedEventArgs(false));
        }

        public bool HasPermission(UserRole requiredRole) => CurrentUser != null && CurrentUser.Role >= requiredRole;
        public bool HasPermission(string permission) => IsAuthenticated && CurrentUser != null;
        public bool HasRole(string role) => CurrentUser != null && CurrentUser.Role.ToString().Equals(role, StringComparison.OrdinalIgnoreCase);
        public bool IsAdmin() => CurrentUser?.Role is UserRole.Admin or UserRole.SuperAdmin;
        public string GetCurrentUserRoleDisplay() => CurrentUser == null ? "未登录" : CurrentUser.Role switch { UserRole.Admin => "管理员", UserRole.Doctor => "医生", _ => CurrentUser.Role.ToString() };
    }
}
