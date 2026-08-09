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
        private readonly object _lock = new();
        private UserDetailDto? _cachedUser;
        public event EventHandler? SessionExpired;
        public event EventHandler<SessionChangedEventArgs>? SessionChanged;

        public SessionManager(IAuthenticationService authService) => _authService = authService ?? throw new ArgumentNullException(nameof(authService));

        /// <summary>
        /// 当前用户（使用同步方法避免WPF死锁）
        /// </summary>
        public UserDetailDto? CurrentUser
        {
            get
            {
                lock (_lock)
                {
                    if (_cachedUser == null)
                        _cachedUser = _authService.GetCurrentUser();
                    return _cachedUser;
                }
            }
        }
        public Guid? CurrentUserId => CurrentUser?.Id;
        public string? CurrentUserName => CurrentUser?.UserName;
        public bool IsAuthenticated => !string.IsNullOrEmpty(_authService.GetToken());
        public bool IsLoggedIn => IsAuthenticated;

        public void ClearSession()
        {
            var wasAuthenticated = IsAuthenticated;
            lock (_lock)
            {
                _cachedUser = null;
            }
            _authService.ClearAuthInfo();
            
            if (wasAuthenticated)
            {
                // 先触发SessionChanged，再触发SessionExpired
                // 这样订阅者可以先处理会话状态变更，再处理过期逻辑
                SessionChanged?.Invoke(this, new SessionChangedEventArgs(false));
                SessionExpired?.Invoke(this, EventArgs.Empty);
            }
        }

        public bool HasPermission(UserRole requiredRole) => CurrentUser != null && CurrentUser.Role >= requiredRole;
        public bool HasPermission(string permission) => IsAuthenticated && CurrentUser != null;
    }
}
