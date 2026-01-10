using LYBT.Desktop.Contracts.Roles;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Shell.Services;

/// <summary>
/// 导航协调器实现 - 统一导航入口
/// OpenSpec: unify-navigation-architecture (ADR-3)
/// 整合NavigationManager和RoleNavigationService功能
/// </summary>
public class NavigationCoordinator : INavigationCoordinator
{
    private readonly IRegionManager _regionManager;
    private readonly ISessionManager _sessionManager;
    private readonly IRoleRegistry _roleRegistry;
    private readonly ILogger<NavigationCoordinator> _logger;
    private readonly IUserNotificationService? _userNotificationService;

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
            _logger.LogInformation("导航到 {ViewName}", viewName);

            // 转换通用字典为Prism NavigationParameters
            var navParams = ConvertToNavigationParameters(parameters);

            _regionManager.RequestNavigate(RegionNames.ContentRegion, viewName, result =>
            {
                if (result.Result != true)
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
}
