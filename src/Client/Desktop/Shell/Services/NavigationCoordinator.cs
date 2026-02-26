using System.Collections.Specialized;
using System.Windows;
using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Shell.Services;

/// <summary>
/// 导航协调器实现 - 统一导航入口
/// OpenSpec: unify-navigation-architecture (ADR-3 + ADR-7)
/// 整合NavigationManager、ViewNavigationService、RoleNavigationService功能
/// </summary>
public class NavigationCoordinator : INavigationCoordinator
{
    private readonly IRegionManager _regionManager;
    private readonly ISessionManager _sessionManager;
    private readonly IRoleRegistry _roleRegistry;
    private readonly ILogger<NavigationCoordinator> _logger;
    private readonly IUserNotificationService? _userNotificationService;

    // OpenSpec: unify-navigation-architecture (ADR-7) - 导航历史管理
    private const int MaxHistorySize = 20;
    private readonly List<string> _navigationHistory = new();

    public NavigationCoordinator(
        IRegionManager regionManager,
        ISessionManager sessionManager,
        IRoleRegistry roleRegistry,
        ILogger<NavigationCoordinator> logger,
        IUserNotificationService? userNotificationService = null)
    {
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _roleRegistry = roleRegistry ?? throw new ArgumentNullException(nameof(roleRegistry));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userNotificationService = userNotificationService;
    }

    #region 基础导航 (原有)

    /// <summary>
    /// 当前视图名称
    /// </summary>
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
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// 是否可以后退
    /// </summary>
    public bool CanNavigateBack
    {
        get
        {
            try
            {
                var region = _regionManager.Regions[RegionNames.ContentRegion];
                return region?.NavigationService?.Journal?.CanGoBack ?? false;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// 导航到指定视图
    /// </summary>
    public void NavigateTo(string viewName, IDictionary<string, object>? parameters = null)
    {
        try
        {
            var fromView = CurrentView;
            _logger.LogInformation("导航到 {ViewName}", viewName);

            // 转换通用字典为Prism NavigationParameters
            var navParams = ConvertToNavigationParameters(parameters);

            _regionManager.RequestNavigate(RegionNames.ContentRegion, viewName, result =>
            {
                if (result.Result == true)
                {
                    // OpenSpec: unify-navigation-architecture (ADR-7) - 记录导航历史
                    if (_navigationHistory.Count >= MaxHistorySize)
                    {
                        _navigationHistory.RemoveAt(0);
                    }
                    _navigationHistory.Add(viewName);
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
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导航到 {ViewName} 时发生异常", viewName);
            _userNotificationService?.ShowErrorAsync($"导航失败：{ex.Message}");
        }
    }

    /// <summary>
    /// 将通用字典转换为Prism NavigationParameters
    /// </summary>
    private static NavigationParameters? ConvertToNavigationParameters(IDictionary<string, object>? parameters)
    {
        if (parameters == null || parameters.Count == 0)
            return null;

        var navParams = new NavigationParameters();
        foreach (var kvp in parameters)
        {
            navParams.Add(kvp.Key, kvp.Value);
        }
        return navParams;
    }

    /// <summary>
    /// 导航到当前角色主页
    /// </summary>
    public void NavigateToHome()
    {
        var role = _sessionManager.CurrentUser?.Role;
        string homeViewName;

        if (role == null)
        {
            _logger.LogWarning("当前用户角色为空，使用默认主页视图");
            homeViewName = ViewNames.ClinicalHome;
        }
        else
        {
            homeViewName = _roleRegistry.GetHomeViewName(role.Value);
        }

        _logger.LogInformation("导航到主页: {HomeViewName}", homeViewName);
        NavigateTo(homeViewName);
    }

    /// <summary>
    /// 导航到指定角色主页
    /// </summary>
    /// <param name="role">用户角色</param>
    public void NavigateToHome(UserRole role)
    {
        var homeViewName = _roleRegistry.GetHomeViewName(role);
        _logger.LogInformation("导航到角色主页: Role={Role}, HomeView={HomeViewName}", role, homeViewName);
        NavigateTo(homeViewName);
    }

    /// <summary>
    /// 导航后退
    /// </summary>
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

    #endregion

    #region 历史导航 (从ViewNavigationService整合)

    /// <summary>
    /// 导航历史记录
    /// OpenSpec: unify-navigation-architecture (ADR-7)
    /// </summary>
    public IReadOnlyList<string> NavigationHistory => _navigationHistory.AsReadOnly();

    /// <summary>
    /// 清除导航历史
    /// OpenSpec: unify-navigation-architecture (ADR-7)
    /// </summary>
    public void ClearHistory()
    {
        _navigationHistory.Clear();
        _logger.LogDebug("导航历史已清除");
    }

    /// <summary>
    /// 导航变更事件
    /// OpenSpec: unify-navigation-architecture (ADR-7)
    /// </summary>
    public event EventHandler<NavigationChangedEventArgs>? NavigationChanged;

    #endregion

    #region Region管理 (从NavigationManager整合)

    /// <summary>
    /// 显示登录对话框
    /// OpenSpec: unify-navigation-architecture (ADR-7)
    /// </summary>
    public void ShowLoginDialog()
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            _regionManager.RequestNavigate(RegionNames.LoginRegion, ViewNames.Login);
            _logger.LogDebug("显示登录对话框");
        });
    }

    /// <summary>
    /// 清除登录区域
    /// OpenSpec: unify-navigation-architecture (ADR-7)
    /// </summary>
    public void ClearLoginRegion()
    {
        if (_regionManager.Regions.ContainsRegionWithName(RegionNames.LoginRegion))
        {
            _regionManager.Regions[RegionNames.LoginRegion].RemoveAll();
            _logger.LogDebug("登录区域已清除");
        }
    }

    /// <summary>
    /// 清除内容区域
    /// OpenSpec: unify-navigation-architecture (ADR-7)
    /// </summary>
    public void ClearContentRegion()
    {
        if (_regionManager.Regions.ContainsRegionWithName(RegionNames.ContentRegion))
        {
            _regionManager.Regions[RegionNames.ContentRegion].RemoveAll();
            _logger.LogDebug("内容区域已清除");
        }
    }

    #endregion

    #region 事件订阅 (从NavigationManager整合)

    /// <summary>
    /// 订阅Region集合变化事件（用于导航监控）
    /// OpenSpec: unify-navigation-architecture (ADR-7)
    /// </summary>
    public void SubscribeToRegionCollection()
    {
        _regionManager.Regions.CollectionChanged += OnRegionsCollectionChanged;
        foreach (var region in _regionManager.Regions)
        {
            SubscribeToRegionNavigationEvents(region);
        }
        _logger.LogDebug("Region 导航监控已启用");
    }

    /// <summary>
    /// 取消Region集合变化事件订阅
    /// OpenSpec: unify-navigation-architecture (ADR-7)
    /// </summary>
    public void UnsubscribeFromRegionCollection()
    {
        try
        {
            _regionManager.Regions.CollectionChanged -= OnRegionsCollectionChanged;
            _logger.LogDebug("Region 导航监控已取消");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "取消Region监控失败");
        }
    }

    /// <summary>
    /// Region集合变化事件处理
    /// </summary>
    private void OnRegionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (IRegion region in e.NewItems)
            {
                SubscribeToRegionNavigationEvents(region);
            }
        }
    }

    /// <summary>
    /// 订阅Region导航事件
    /// </summary>
    private void SubscribeToRegionNavigationEvents(IRegion region)
    {
        region.NavigationService.Navigating += (s, e) =>
            _logger.LogDebug("导航中: Region={RegionName}, Target={Uri}", region.Name, e.Uri);
        region.NavigationService.Navigated += (s, e) =>
            _logger.LogDebug("导航完成: Region={RegionName}, Uri={Uri}", region.Name, e.Uri);
        region.NavigationService.NavigationFailed += (s, e) =>
            _logger.LogError(e.Error, "导航失败: {RegionName} -> {Uri}", region.Name, e.Uri);
    }

    #endregion
}
