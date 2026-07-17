using System.Threading;
using System.Windows;
using LYBT.Desktop.Shared.UI;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Navigation;

/// <summary>
/// 导航协调器实现 - 统一导航入口
/// 职责：导航路由核心逻辑（~100行）
/// 历史/面包屑/懒加载/Region监控 已提取到独立服务
/// </summary>
public class NavigationCoordinator : INavigationCoordinator
{
    private readonly IRegionManager _regionManager;
    private readonly ISessionManager _sessionManager;
    private readonly IRoleRegistry _roleRegistry;
    private readonly INavigationHistoryService _historyService;
    private readonly IModuleLazyLoader _moduleLazyLoader;
    private readonly IRegionMonitor _regionMonitor;
    private readonly ILogger<NavigationCoordinator> _logger;
    private readonly IUserNotificationService? _userNotificationService;
    private DateTime _lastNavigationTime = DateTime.MinValue;
    private string? _lastNavigationView;
    private const int NavigationDebounceMs = 300;
    private const int NavigationTimeoutSeconds = 10;

    public NavigationCoordinator(
        IRegionManager regionManager,
        ISessionManager sessionManager,
        IRoleRegistry roleRegistry,
        INavigationHistoryService historyService,
        IModuleLazyLoader moduleLazyLoader,
        IRegionMonitor regionMonitor,
        ILogger<NavigationCoordinator> logger,
        IUserNotificationService? userNotificationService = null)
    {
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _roleRegistry = roleRegistry ?? throw new ArgumentNullException(nameof(roleRegistry));
        _historyService = historyService ?? throw new ArgumentNullException(nameof(historyService));
        _moduleLazyLoader = moduleLazyLoader ?? throw new ArgumentNullException(nameof(moduleLazyLoader));
        _regionMonitor = regionMonitor ?? throw new ArgumentNullException(nameof(regionMonitor));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userNotificationService = userNotificationService;
    }

    #region 导航路由核心

    /// <summary>当前视图名称</summary>
    public string? CurrentView
    {
        get
        {
            try
            {
                var region = _regionManager.Regions[RegionNames.ContentRegion];
                var activeView = region?.ActiveViews.FirstOrDefault();
                return activeView?.GetType().Name;
            }
            catch { return null; }
        }
    }

    /// <summary>是否可以后退</summary>
    public bool CanNavigateBack
    {
        get
        {
            try
            {
                var region = _regionManager.Regions[RegionNames.ContentRegion];
                return region?.NavigationService?.Journal?.CanGoBack ?? false;
            }
            catch { return false; }
        }
    }

    /// <summary>是否可以前进</summary>
    public bool CanNavigateForward
    {
        get
        {
            try
            {
                var region = _regionManager.Regions[RegionNames.ContentRegion];
                return region?.NavigationService?.Journal?.CanGoForward ?? false;
            }
            catch { return false; }
        }
    }

    /// <summary>导航历史记录</summary>
    public IReadOnlyList<string> NavigationHistory => _historyService.NavigationHistory;

    /// <summary>当前面包屑列表</summary>
    public IReadOnlyList<BreadcrumbItem> Breadcrumbs => _historyService.Breadcrumbs;

    /// <summary>导航变更事件</summary>
    public event EventHandler<NavigationChangedEventArgs>? NavigationChanged;

    /// <summary>导航到指定视图</summary>
    public void NavigateTo(string viewName, IDictionary<string, object>? parameters = null)
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

            _moduleLazyLoader.EnsureModuleLoaded(viewName);
            var fromView = CurrentView;
            _logger.LogInformation("导航到 {ViewName}", viewName);
            var navParams = ConvertToNavigationParameters(parameters);

                        var tcs = new TaskCompletionSource<bool>();
            var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(NavigationTimeoutSeconds));
            timeoutCts.Token.Register(() => tcs.TrySetResult(false));

            _regionManager.RequestNavigate(RegionNames.ContentRegion, viewName, result =>
            {
                tcs.TrySetResult(result.Result == true);
            
                if (result.Result == true)
                {
                    _historyService.RecordNavigation(fromView, viewName);
                    NavigationChanged?.Invoke(this, new NavigationChangedEventArgs(fromView, viewName, parameters));
                    _logger.LogDebug("导航成功: {FromView} -> {ToView}", fromView, viewName);
                }
                else
                {
                    var errorMessage = result.Error?.Message ?? "未知错误";
                    _logger.LogError("导航失败：{ViewName}，错误：{Error}", viewName, errorMessage);
                    _userNotificationService?.ShowErrorAsync($"无法打开页面：{errorMessage}");
                }
            }, navParams);

            _ = Task.Run(async () =>
            {
                if (!await tcs.Task)
                {
                    _logger.LogWarning("导航超时: {ViewName} ({TimeoutSeconds}s)", viewName, NavigationTimeoutSeconds);
                    _userNotificationService?.ShowWarningAsync($"页面加载超时：{viewName}");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导航到 {ViewName} 时发生异常", viewName);
            _userNotificationService?.ShowErrorAsync($"导航失败：{ex.Message}");
        }
    }

    /// <summary>导航到指定视图（强类型参数）</summary>
    public void NavigateTo<TParams>(string viewName, TParams parameters) where TParams : class
    {
        ArgumentNullException.ThrowIfNull(parameters);
        var dict = new Dictionary<string, object>();
        foreach (var prop in typeof(TParams).GetProperties())
        {
            var value = prop.GetValue(parameters);
            if (value != null)
                dict[prop.Name] = value;
        }
        NavigateTo(viewName, dict);
    }

    /// <summary>导航到当前角色主页</summary>
    public void NavigateToHome()
    {
        var role = _sessionManager.CurrentUser?.Role;
        var homeViewName = role == null
            ? ViewNames.ClinicalHome
            : _roleRegistry.GetHomeViewName(role.Value);

        _logger.LogInformation("导航到主页: {HomeViewName}", homeViewName);
        NavigateTo(homeViewName);
    }

    /// <summary>导航到指定角色主页</summary>
    public void NavigateToHome(UserRole role)
    {
        var homeViewName = _roleRegistry.GetHomeViewName(role);
        _logger.LogInformation("导航到角色主页: Role={Role}, HomeView={HomeViewName}", role, homeViewName);
        NavigateTo(homeViewName);
    }

    /// <summary>导航后退</summary>
    public void NavigateBack()
    {
        try
        {
            var region = _regionManager.Regions[RegionNames.ContentRegion];
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
            _userNotificationService?.ShowErrorAsync($"导航回退失败：{ex.Message}");
        }
    }

    /// <summary>导航前进</summary>
    public void NavigateForward()
    {
        try
        {
            var region = _regionManager.Regions[RegionNames.ContentRegion];
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
            _userNotificationService?.ShowErrorAsync($"导航前进失败：{ex.Message}");
        }
    }

    /// <summary>清除导航历史</summary>
    public void ClearHistory() => _historyService.ClearHistory();

    /// <summary>跳转到指定面包屑</summary>
    public void NavigateToBreadcrumb(BreadcrumbItem item)
    {
        if (item.IsCurrent) return;
        _logger.LogInformation("面包屑导航: 跳转到 {Title} ({ViewName})", item.Title, item.ViewName);
        NavigateTo(item.ViewName);
    }

    #endregion

    #region Region 管理

    /// <summary>显示登录对话框</summary>
    public void ShowLoginDialog()
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _regionManager.RequestNavigate(RegionNames.LoginRegion, ViewNames.Login);
            _logger.LogDebug("显示登录对话框");
        });
    }

    /// <summary>清除登录区域</summary>
    public void ClearLoginRegion()
    {
        if (_regionManager.Regions.ContainsRegionWithName(RegionNames.LoginRegion))
        {
            _regionManager.Regions[RegionNames.LoginRegion].RemoveAll();
            _logger.LogDebug("登录区域已清除");
        }
    }

    /// <summary>清除内容区域</summary>
    public void ClearContentRegion()
    {
        if (_regionManager.Regions.ContainsRegionWithName(RegionNames.ContentRegion))
        {
            _regionManager.Regions[RegionNames.ContentRegion].RemoveAll();
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
    public void SubscribeToRegionCollection() => _regionMonitor.StartMonitoring();

    /// <summary>取消Region集合变化事件订阅</summary>
    public void UnsubscribeFromRegionCollection() => _regionMonitor.StopMonitoring();

    #endregion
}
