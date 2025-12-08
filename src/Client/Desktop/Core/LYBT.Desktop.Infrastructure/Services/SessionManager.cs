using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Infrastructure.Services
{
    /// <summary>会话管理器实现 - 包装AuthenticationService提供会话功能</summary>
    public class SessionManager : ISessionManager
    {
        private readonly IAuthenticationService _authService;
        private UserDto? _cachedUser;
        private string? _cachedToken;
        private string? _cachedRefreshToken;

#pragma warning disable CS0067
        public event EventHandler? SessionExpiring;
        public event EventHandler? SessionExpired;
#pragma warning restore CS0067
        public event EventHandler<SessionChangedEventArgs>? SessionChanged;

        public SessionManager(IAuthenticationService authService) => _authService = authService ?? throw new ArgumentNullException(nameof(authService));

        public UserDto? CurrentUser { get { if (_cachedUser == null) _cachedUser = _authService.GetCurrentUserAsync().GetAwaiter().GetResult(); return _cachedUser; } }
        public Guid? CurrentUserId => CurrentUser?.Id;
        public string? CurrentUserName => CurrentUser?.UserName;
        public bool IsAuthenticated => !string.IsNullOrEmpty(CurrentToken);
        public bool IsLoggedIn => IsAuthenticated;
        public string? CurrentToken { get { if (_cachedToken == null) _cachedToken = _authService.GetToken(); return _cachedToken; } }
        public string? AccessToken => CurrentToken;
        public string? RefreshToken => _cachedRefreshToken;

        public void SetCurrentUser(UserDto user, string token) { _cachedUser = user ?? throw new ArgumentNullException(nameof(user)); _cachedToken = token ?? throw new ArgumentNullException(nameof(token)); }

        public void SetSession(UserDto user, string accessToken, string? refreshToken = null)
        {
            _cachedUser = user ?? throw new ArgumentNullException(nameof(user));
            _cachedToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));
            _cachedRefreshToken = refreshToken;
            SessionChanged?.Invoke(this, new SessionChangedEventArgs(true, user));
        }

        public void SetUserSession(UserDto user, string token) => SetSession(user, token);
        public void UpdateAccessToken(string accessToken) => _cachedToken = accessToken ?? throw new ArgumentNullException(nameof(accessToken));

        public void ClearSession()
        {
            var wasAuthenticated = IsAuthenticated;
            _cachedUser = null;
            _cachedToken = null;
            _cachedRefreshToken = null;
            _authService.ClearAuthInfo();
            if (wasAuthenticated) SessionChanged?.Invoke(this, new SessionChangedEventArgs(false));
        }

        public void ClearUserSession() => ClearSession();
        public bool HasPermission(UserRole requiredRole) => CurrentUser != null && CurrentUser.Role >= requiredRole;
        public bool HasPermission(string permission) => IsAuthenticated && CurrentUser != null;
        public bool HasRole(string role) => CurrentUser != null && CurrentUser.Role.ToString().Equals(role, StringComparison.OrdinalIgnoreCase);
        public bool IsAdmin() => CurrentUser?.Role is UserRole.Admin or UserRole.SuperAdmin;
        public string GetCurrentUserRoleDisplay() => CurrentUser == null ? "未登录" : CurrentUser.Role switch { UserRole.Admin => "管理员", UserRole.Doctor => "医生", _ => CurrentUser.Role.ToString() };
    }
}
