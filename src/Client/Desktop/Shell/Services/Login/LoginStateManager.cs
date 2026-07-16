using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.Services.Toast;
using LYBT.Shared.Models.Contracts.Users;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Events;

namespace LYBT.Desktop.Shell.Services.Login;

/// <summary>
/// 登录状态管理器实现
/// 从 MainWindowViewModel 提取的登录状态管理逻辑
/// </summary>
public class LoginStateManager : ILoginStateManager
{
    private const string DefaultTitle = "凌隐宝堂中医诊所诊疗系统";

    private readonly IUserActivityTracker _userActivityTracker;
    private readonly ITokenLifecycleService _tokenLifecycleService;
    private readonly ILoginCoordinator _loginCoordinator;
    private readonly IEventAggregator _eventAggregator;
    private readonly IToastService? _toastService;
    private readonly ILogger<LoginStateManager> _logger;

    public bool IsLoggedIn { get; private set; }
    public bool IsNotLoggedIn => !IsLoggedIn;
    public UserDetailDto? CurrentUser { get; private set; }
    public string Title { get; private set; } = DefaultTitle;

    public string CurrentUserDisplayName =>
        IsLoggedIn && CurrentUser != null
            ? (string.IsNullOrEmpty(CurrentUser.RealName) ? CurrentUser.UserName : CurrentUser.RealName)
            : string.Empty;

    public string CurrentUserInitial =>
        CurrentUser != null && !string.IsNullOrEmpty(CurrentUser.UserName)
            ? CurrentUser.UserName[..1].ToUpper()
            : "?";

    public string CurrentUserRoleDisplay =>
        CurrentUser?.Role switch
        {
            UserRole.SuperAdmin => "超级管理员",
            UserRole.Admin => "管理员",
            UserRole.Doctor => "医生",
            UserRole.Receptionist => "前台",
            _ => string.Empty
        };

    public event EventHandler? LoginStateChanged;
    public event EventHandler? LogoutRequested;

    public LoginStateManager(
        IUserActivityTracker userActivityTracker,
        ITokenLifecycleService tokenLifecycleService,
        ILoginCoordinator loginCoordinator,
        IEventAggregator eventAggregator,
        ILogger<LoginStateManager> logger,
        IToastService? toastService = null)
    {
        _userActivityTracker = userActivityTracker ?? throw new ArgumentNullException(nameof(userActivityTracker));
        _tokenLifecycleService = tokenLifecycleService ?? throw new ArgumentNullException(nameof(tokenLifecycleService));
        _loginCoordinator = loginCoordinator ?? throw new ArgumentNullException(nameof(loginCoordinator));
        _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _toastService = toastService;
    }

    public void ApplyLoginSuccess(UserDetailDto user)
    {
        IsLoggedIn = true;
        CurrentUser = user;

        bool isAdmin = user.UserName?.Equals(SystemConstants.SuperAdminUsername, StringComparison.OrdinalIgnoreCase) == true
                       || user.Role == UserRole.Admin;
        var userDisplayName = string.IsNullOrEmpty(user.RealName) ? user.UserName : user.RealName;
        Title = $"凌隐宝堂中医诊所诊疗系统 - {userDisplayName} ({(isAdmin ? "管理员" : "医生")})";

        LoginStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ApplyPasswordChanged()
    {
        _logger.LogInformation("密码修改成功，重置登录状态");
        IsLoggedIn = false;
        CurrentUser = null;
        Title = DefaultTitle;
        LoginStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void ApplyProfileUpdate(UserDetailDto updatedUser)
    {
        CurrentUser = updatedUser;
        _logger.LogInformation("已同步用户资料更新 [用户: {UserName}]", updatedUser.UserName);
        LoginStateChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task PerformLogoutAsync()
    {
        _userActivityTracker.StopTracking();
        _tokenLifecycleService.Reset();
        IsLoggedIn = false;
        CurrentUser = null;
        Title = DefaultTitle;

        var serverLogoutCompleted = false;
        try
        {
            await _loginCoordinator.LogoutAsync();
            serverLogoutCompleted = true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "登出处理异常");
        }

        _eventAggregator.GetEvent<AuthEvents.LogoutCompletedEvent>().Publish(new LogoutCompletedPayload
        {
            LocalLogoutCompleted = true,
            ServerLogoutCompleted = serverLogoutCompleted
        });

        LoginStateChanged?.Invoke(this, EventArgs.Empty);
        LogoutRequested?.Invoke(this, EventArgs.Empty);
    }

    public async Task HandleTokenExpiredAsync()
    {
        _logger.LogWarning("Token已过期，执行自动登出");

        if (_toastService != null)
            await Task.Run(() => _toastService.ShowSuccess("您的登录凭证已过期，请重新登录。"));

        _tokenLifecycleService.Reset();
        await PerformLogoutAsync();
    }

    public async Task HandleSessionExpiredAsync()
    {
        _logger.LogWarning("用户会话因不活跃已过期，执行自动登出");

        if (_toastService != null)
            await Task.Run(() => _toastService.ShowSuccess("您的会话因长时间未操作已过期，请重新登录。"));

        await PerformLogoutAsync();
    }

    public void Dispose()
    {
        _tokenLifecycleService.Dispose();
    }
}
