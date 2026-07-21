using System.Threading;
using System.Windows;
using LYBT.Desktop.Contracts.UI;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Infrastructure.Navigation;

/// <summary>
/// 导航协调器实现 - 统一导航入口
/// 职责：导航路由核心逻辑（~100行）
/// 历史/面包屑/懒加载/Region监控 已提取到独立服务
/// </summary>
public class NavigationCoordinator : INavigationCoordinator
{
    private readonly INavigationServices _services;
    private readonly ILogger<NavigationCoordinator> _logger;
    private DateTime _lastNavigationTime = DateTime.MinValue;
    private string? _lastNavigationView;
    private const int NavigationDebounceMs = 300;
    private const int NavigationTimeoutSeconds = 10;

    public NavigationCoordinator(
        INavigationServices services,
        ILogger<NavigationCoordinator> logger)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #region 导航路由核心

    /// <summary>当前视图名称</summary>
    public string? CurrentView
    {
        get
        {
            try
            {
                var region = _services.RegionManager.Regions[RegionNames.ContentRegion];
                var activeView = region?.ActiveViews.FirstOrDefault();
                return activeView?.GetType().Name;
            }
            catch (Exception ex) { _logger.LogDebug(ex, "获取当前视图名称失败"); return null; }
        }
    }

    /// <summary>是否可以后退</summary>
    public bool CanNavigateBack
    {
        get
        {
            try
            {
                var region = _services.RegionManager.Regions[RegionNames.ContentRegion];
                return region?.NavigationService?.Journal?.CanGoBack ?? false;
            }
            catch (Exception ex) { _logger.LogDebug(ex, "检查导航后退状态失败"); return false; }
        }
    }

    /// <summary>是否可以前进</summary>
    public bool CanNavigateForward
    {
        get
        {
            try
            {
                var region = _services.RegionManager.Regions[RegionNames.ContentRegion];
                return region?.NavigationService?.Journal?.CanGoForward ?? false;
            }
            catch (Exception ex) { _logger.LogDebug(ex, "检查导航前进状态失败"); return false; }
        }
    }

    /// <summary>导航历史记录</summary>
    public IReadOnlyList<string> NavigationHistory => _services.HistoryService.NavigationHistory;

    /// <summary>当前面包屑列表</summary>
    public IReadOnlyList<BreadcrumbItem> Breadcrumbs => _services.HistoryService.Breadcrumbs;

    /// <summary>导航变更事件</summary>
    public event EventHandler<NavigationChangedEventArgs>? NavigationChanged;

    /// <summary>导航到指定视图</summary>
    public async Task NavigateTo(string viewName, IDictionary<string, object>? parameters = null)
    {
        try
        {
            // 防抖：300ms 内不重复导航到同一视图
            if (viewName == _lastNavigationView &&
                viewName == CurrentView &&
                (DateTime.UtcNow - _lastNavigationTime).TotalMilliseconds < NavigationDebounceMs)
            {
                _logger.LogDebug("导航防抖：忽略重复请求 {ViewName}", viewName);
                return;
            }

            _lastNavigationTime = DateTime.UtcNow;
            _lastNavigationView = viewName;

            await _services.ModuleLazyLoader.EnsureModuleLoadedAsync(viewName);
            var fromView = CurrentView;
            _logger.LogInformation("导航到 {ViewName}", viewName);
            var navParams = ConvertToNavigationParameters(parameters);

            var tcs = new TaskCompletionSource<bool>();
            var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(NavigationTimeoutSeconds));
            var timeoutRegistration = timeoutCts.Token.Register(() => tcs.TrySetResult(false));

            _services.RegionManager.RequestNavigate(RegionNames.ContentRegion, viewName, result =>
            {
                tcs.TrySetResult(result.Result == true);

                if (result.Result == true)
                {
                    _services.HistoryService.RecordNavigation(fromView, viewName);
                    NavigationChanged?.Invoke(this, new NavigationChangedEventArgs(fromView, viewName, parameters));
                    _logger.LogDebug("导航成功: {FromView} -> {ToView}", fromView, viewName);
                }
                else
                {
                    var errorMessage = result.Error?.Message ?? "未知错误";
                    _logger.LogError("导航失败：{ViewName}，错误：{Error}", viewName, errorMessage);
                    _services.UserNotificationService?.ShowErrorAsync($"无法打开页面：{errorMessage}");
                }
            }, navParams);

            _ = Task.Run(async () =>
            {
                try
                {
                    if (!await tcs.Task)
                    {
                        _logger.LogWarning("导航超时: {ViewName} ({TimeoutSeconds}s)", viewName, NavigationTimeoutSeconds);
                        _services.UserNotificationService?.ShowWarningAsync($"页面加载超时：{viewName}");
                    }
                }
                finally
                {
                    timeoutCts.Dispose();
                    timeoutRegistration.Dispose();
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导航到 {ViewName} 时发生异常", viewName);
            _services.UserNotificationService?.ShowErrorAsync("导航失败，请重试");
        }
    }

    /// <summary>导航到指定视图（强类型参数）</summary>
    public async Task NavigateTo<TParams>(string viewName, TParams parameters) where TParams : class
    {
        ArgumentNullException.ThrowIfNull(parameters);
        var dict = new Dictionary<string, object>();
        foreach (var prop in typeof(TParams).GetProperties())
        {
            var value = prop.GetValue(parameters);
            if (value != null)
                dict[prop.Name] = value;
        }
        await NavigateTo(viewName, dict);
    }

    /// <summary>导航到当前角色主页</summary>
    public async Task NavigateToHome()
    {
        var role = _services.SessionManager.CurrentUser?.Role;
        var homeViewName = role == null
            ? ViewNames.ClinicalHome
            : _services.RoleRegistry.GetHomeViewName(role.Value);

        _logger.LogInformation("导航到主页: {HomeViewName}", homeViewName);
        await NavigateTo(homeViewName);
    }

    /// <summary>导航到指定角色主页</summary>
    public async Task NavigateToHome(UserRole role)
    {
        var homeViewName = _services.RoleRegistry.GetHomeViewName(role);
        _logger.LogInformation("导航到角色主页: Role={Role}, HomeView={HomeViewName}", role, homeViewName);
        await NavigateTo(homeViewName);
    }

    /// <summary>导航后退</summary>
    public void NavigateBack()
    {
        try
        {
            var region = _services.RegionManager.Regions[RegionNames.ContentRegion];
            if (region?.NavigationService?.Journal?.CanGoBack == true)
            {
                region.NavigationService.Journal.GoBack();
                _logger.LogDebug("导航回退成功");
            }
            else
            {
                _logger.LogWarning("无法回退，导航历史为空");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导航回退失败");
            _services.UserNotificationService?.ShowErrorAsync("导航回退失败，请重试");
        }
    }

    /// <summary>导航前进</summary>
    public void NavigateForward()
    {
        try
        {
            var region = _services.RegionManager.Regions[RegionNames.ContentRegion];
            if (region?.NavigationService?.Journal?.CanGoForward == true)
            {
                region.NavigationService.Journal.GoForward();
                _logger.LogDebug("导航前进成功");
            }
            else
            {
                _logger.LogWarning("无法前进，无前进历史");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导航前进失败");
            _services.UserNotificationService?.ShowErrorAsync("导航前进失败，请重试");
        }
    }

    /// <summary>清除导航历史</summary>
    public void ClearHistory() => _services.HistoryService.ClearHistory();

    #endregion

    #region Region 管理

    /// <summary>显示登录对话框</summary>
    public void ShowLoginDialog()
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _services.RegionManager.RequestNavigate(RegionNames.LoginRegion, ViewNames.Login);
            _logger.LogDebug("显示登录对话框");
        });
    }

    /// <summary>清除登录区域</summary>
    public void ClearLoginRegion()
    {
        if (_services.RegionManager.Regions.ContainsRegionWithName(RegionNames.LoginRegion))
        {
            _services.RegionManager.Regions[RegionNames.LoginRegion].RemoveAll();
            _logger.LogDebug("登录区域已清除");
        }
    }

    /// <summary>清除内容区域</summary>
    public void ClearContentRegion()
    {
        if (_services.RegionManager.Regions.ContainsRegionWithName(RegionNames.ContentRegion))
        {
            _services.RegionManager.Regions[RegionNames.ContentRegion].RemoveAll();
            _logger.LogDebug("内容区域已清除");
        }
    }

    #endregion

    #region 辅助方法

    private static NavigationParameters? ConvertToNavigationParameters(IDictionary<string, object>? parameters)
    {
        if (parameters == null || parameters.Count == 0)
            return null;

        var navParams = new NavigationParameters();
        foreach (var kvp in parameters)
            navParams.Add(kvp.Key, kvp.Value);
        return navParams;
    }

    #endregion

    #region Region 监控（委托给 IRegionMonitor）

    /// <summary>订阅Region集合变化事件</summary>
    public void SubscribeToRegionCollection() => _services.RegionMonitor.StartMonitoring();

    /// <summary>取消Region集合变化事件订阅</summary>
    public void UnsubscribeFromRegionCollection() => _services.RegionMonitor.StopMonitoring();

    #endregion
}
