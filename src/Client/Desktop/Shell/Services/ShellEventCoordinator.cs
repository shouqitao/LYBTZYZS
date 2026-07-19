using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Foundation.Security;
using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Extensions;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Infrastructure.Navigation;
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
    private readonly IShellEventServices _services;
    private readonly ILogger<ShellEventCoordinator> _logger;

    private readonly EventSubscriptionManager _eventSubscriptions;

    /// <summary>登录成功后通知 UI 层刷新绑定</summary>
    public event EventHandler? LoginSuccessHandled;

    /// <summary>密码变更后通知 UI 层刷新绑定</summary>
    public event EventHandler? PasswordChangedHandled;

    public ShellEventCoordinator(
        IShellEventServices services,
        IEventAggregator eventAggregator,
        ILogger<ShellEventCoordinator> logger)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _eventSubscriptions = new EventSubscriptionManager(eventAggregator);

        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        _services.LoginCoordinator.LoginSucceeded += OnLoginSucceeded;
        _services.ActivityTracker.SessionExpired += OnSessionExpired;
        _services.LoginState.LogoutRequested += OnLogoutRequested;

        _eventSubscriptions.Subscribe<AuthEvents.PasswordChangedEvent, PasswordChangedPayload>(OnPasswordChanged);
        _eventSubscriptions.Subscribe<AuthEvents.ProfileUpdatedEvent, ProfileUpdatedPayload>(OnProfileUpdated);
        _eventSubscriptions.Subscribe<TokenLifecycleStateChangedEvent, TokenLifecycleStateChangedEventArgs>(
            args => OnTokenLifecycleStateChanged(args).SafeFireAndForget(ex => _logger.LogError(ex, "Token生命周期事件处理异常")));
    }

    private void OnLoginSucceeded(object? sender, LoginSuccessEventArgs args)
    {
        _services.UiDispatcher.InvokeAsync(() =>
        {
            _services.LoginState.ApplyLoginSuccess(args.User);

            _services.Navigation.ClearLoginRegion();
            _services.ActivityTracker.StartTracking();
            _ = _services.TokenLifecycle.StartMonitoringFromStorageAsync();

            _services.NavigationManager.NavigationItems = _services.NavigationManager.BuildNavigationItems(args.User.Role);

            // 背景预加载高频模块
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(2000);
                    await _services.ModuleLoader.PreloadModulesAsync(args.User.Role);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "背景模块预加载失败");
                }
            });

            _logger.LogInformation("登录成功UI更新完成 [用户: {Username}]", args.User.UserName);

            LoginSuccessHandled?.Invoke(this, EventArgs.Empty);
        });
    }

    private void OnSessionExpired(object? sender, EventArgs e)
    {
        _services.LoginState.HandleSessionExpiredAsync()
            .SafeFireAndForget(ex => _logger.LogError(ex, "会话过期处理异常"));
    }

    private void OnPasswordChanged(PasswordChangedPayload payload)
    {
        _logger.LogInformation("收到密码修改成功事件 [用户: {UserName}]，导航到登录界面", payload.UserName);
        _services.UiDispatcher.InvokeAsync(() =>
        {
            _services.LoginState.ApplyPasswordChanged();

            _services.Navigation.ClearContentRegion();
            _services.Navigation.ShowLoginDialog();

            PasswordChangedHandled?.Invoke(this, EventArgs.Empty);
        });
    }

    private void OnProfileUpdated(ProfileUpdatedPayload payload)
    {
        _services.UiDispatcher.InvokeAsync(() =>
        {
            _services.LoginState.ApplyProfileUpdate(payload.UpdatedUser);
        });
    }

    private async Task OnTokenLifecycleStateChanged(TokenLifecycleStateChangedEventArgs args)
    {
        _logger.LogDebug("Token生命周期状态变更: {Previous} -> {Current}", args.PreviousState, args.CurrentState);

        await _services.UiDispatcher.InvokeAsync(async () =>
        {
            switch (args.CurrentState)
            {
                case TokenLifecycleState.Warning:
                    var remainingMinutes = args.RemainingTime?.TotalMinutes ?? 0;
                    _logger.LogDebug("Token即将过期，剩余时间: {RemainingMinutes:F1} 分钟，系统将自动刷新", remainingMinutes);
                    break;

                case TokenLifecycleState.Expired:
                    await _services.LoginState.HandleTokenExpiredAsync();
                    break;
            }
        });
    }

    private void OnLogoutRequested(object? sender, EventArgs e)
    {
        _services.NavigationManager.NavigationItems.Clear();
        _services.Navigation.ClearHistory();
        _services.Navigation.ClearContentRegion();
        _services.Navigation.ShowLoginDialog();
    }

    public void Dispose()
    {
        _services.LoginCoordinator.LoginSucceeded -= OnLoginSucceeded;
        _services.ActivityTracker.SessionExpired -= OnSessionExpired;
        _services.LoginState.LogoutRequested -= OnLogoutRequested;
        _services.ActivityTracker.StopTracking();
        _eventSubscriptions.Dispose();
    }
}
