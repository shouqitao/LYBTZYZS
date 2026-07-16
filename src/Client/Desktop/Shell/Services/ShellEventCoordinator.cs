using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Extensions;
using LYBT.Desktop.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using Prism.Events;

namespace LYBT.Desktop.Shell.Services.Login;

/// <summary>
/// Shell 事件协调器
/// 订阅认证相关事件（Token生命周期、会话过期、密码变更、用户资料更新），
/// 协调 ILoginStateManager 及其他管理器响应
/// 从 MainWindowViewModel.InitializeViewModel() 提取
/// </summary>
public class ShellEventCoordinator : IDisposable
{
    private readonly ILoginStateManager _loginStateManager;
    private readonly IUserActivityTracker _userActivityTracker;
    private readonly ILoginCoordinator _loginCoordinator;
    private readonly ITokenLifecycleService _tokenLifecycleService;
    private readonly INavigationCoordinator _navigationCoordinator;
    private readonly MenuManager _menuManager;
    private readonly NavigationManager _navigationManager;
    private readonly IUiThreadDispatcher _uiDispatcher;
    private readonly ILogger<ShellEventCoordinator> _logger;

    private readonly EventSubscriptionManager _eventSubscriptions;

    /// <summary>登录成功后通知 UI 层刷新绑定</summary>
    public event EventHandler? LoginSuccessHandled;

    /// <summary>密码变更后通知 UI 层刷新绑定</summary>
    public event EventHandler? PasswordChangedHandled;

    public ShellEventCoordinator(
        ILoginStateManager loginStateManager,
        IEventAggregator eventAggregator,
        IUserActivityTracker userActivityTracker,
        ILoginCoordinator loginCoordinator,
        ITokenLifecycleService tokenLifecycleService,
        INavigationCoordinator navigationCoordinator,
        MenuManager menuManager,
        NavigationManager navigationManager,
        IUiThreadDispatcher uiDispatcher,
        ILogger<ShellEventCoordinator> logger)
    {
        _loginStateManager = loginStateManager ?? throw new ArgumentNullException(nameof(loginStateManager));
        _userActivityTracker = userActivityTracker ?? throw new ArgumentNullException(nameof(userActivityTracker));
        _loginCoordinator = loginCoordinator ?? throw new ArgumentNullException(nameof(loginCoordinator));
        _tokenLifecycleService = tokenLifecycleService ?? throw new ArgumentNullException(nameof(tokenLifecycleService));
        _navigationCoordinator = navigationCoordinator ?? throw new ArgumentNullException(nameof(navigationCoordinator));
        _menuManager = menuManager ?? throw new ArgumentNullException(nameof(menuManager));
        _navigationManager = navigationManager ?? throw new ArgumentNullException(nameof(navigationManager));
        _uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _eventSubscriptions = new EventSubscriptionManager(eventAggregator);

        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        _loginCoordinator.LoginSucceeded += OnLoginSucceeded;
        _userActivityTracker.SessionExpired += OnSessionExpired;
        _loginStateManager.LogoutRequested += OnLogoutRequested;

        _eventSubscriptions.Subscribe<AuthEvents.PasswordChangedEvent, PasswordChangedPayload>(OnPasswordChanged);
        _eventSubscriptions.Subscribe<AuthEvents.ProfileUpdatedEvent, ProfileUpdatedPayload>(OnProfileUpdated);
        _eventSubscriptions.Subscribe<TokenLifecycleStateChangedEvent, TokenLifecycleStateChangedEventArgs>(
            args => OnTokenLifecycleStateChanged(args).SafeFireAndForget(ex => _logger.LogError(ex, "Token生命周期事件处理异常")));
    }

    private void OnLoginSucceeded(object? sender, LoginSuccessEventArgs args)
    {
        _uiDispatcher.InvokeAsync(() =>
        {
            _loginStateManager.ApplyLoginSuccess(args.User);

            _navigationCoordinator.ClearLoginRegion();
            _userActivityTracker.StartTracking();
            _ = _tokenLifecycleService.StartMonitoringFromStorageAsync();

            _menuManager.RefreshMenuVisibility();
            _navigationManager.NavigationItems = _navigationManager.BuildNavigationItems(args.User.Role);

            _logger.LogInformation("登录成功UI更新完成 [用户: {Username}]", args.User.UserName);

            LoginSuccessHandled?.Invoke(this, EventArgs.Empty);
        });
    }

    private void OnSessionExpired(object? sender, EventArgs e)
    {
        _loginStateManager.HandleSessionExpiredAsync()
            .SafeFireAndForget(ex => _logger.LogError(ex, "会话过期处理异常"));
    }

    private void OnPasswordChanged(PasswordChangedPayload payload)
    {
        _logger.LogInformation("收到密码修改成功事件 [用户: {UserName}]，导航到登录界面", payload.UserName);
        _uiDispatcher.InvokeAsync(() =>
        {
            _loginStateManager.ApplyPasswordChanged();

            _navigationCoordinator.ClearContentRegion();
            _navigationCoordinator.ShowLoginDialog();

            PasswordChangedHandled?.Invoke(this, EventArgs.Empty);
        });
    }

    private void OnProfileUpdated(ProfileUpdatedPayload payload)
    {
        _uiDispatcher.InvokeAsync(() =>
        {
            _loginStateManager.ApplyProfileUpdate(payload.UpdatedUser);
        });
    }

    private async Task OnTokenLifecycleStateChanged(TokenLifecycleStateChangedEventArgs args)
    {
        _logger.LogDebug("Token生命周期状态变更: {Previous} -> {Current}", args.PreviousState, args.CurrentState);

        await _uiDispatcher.InvokeAsync(async () =>
        {
            switch (args.CurrentState)
            {
                case TokenLifecycleState.Warning:
                    var remainingMinutes = args.RemainingTime?.TotalMinutes ?? 0;
                    _logger.LogDebug("Token即将过期，剩余时间: {RemainingMinutes:F1} 分钟，系统将自动刷新", remainingMinutes);
                    break;

                case TokenLifecycleState.Expired:
                    await _loginStateManager.HandleTokenExpiredAsync();
                    break;
            }
        });
    }

    private void OnLogoutRequested(object? sender, EventArgs e)
    {
        _navigationManager.NavigationItems.Clear();
        _navigationCoordinator.ClearHistory();
        _navigationCoordinator.ClearContentRegion();
        _navigationCoordinator.ShowLoginDialog();
    }

    public void Dispose()
    {
        _loginCoordinator.LoginSucceeded -= OnLoginSucceeded;
        _userActivityTracker.SessionExpired -= OnSessionExpired;
        _loginStateManager.LogoutRequested -= OnLogoutRequested;
        _eventSubscriptions.Dispose();
    }
}
