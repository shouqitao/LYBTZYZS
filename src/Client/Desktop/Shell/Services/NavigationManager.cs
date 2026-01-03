using System.Windows;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Infrastructure.Constants;
using LYBT.Shared.ExceptionHandling.Mappers;
using Microsoft.Extensions.Logging;
using Prism.Regions;

namespace LYBT.Desktop.Shell.Services;

/// <summary>导航管理器 - 负责Region导航、角色导航、页面跳转</summary>
public class NavigationManager
{
    private readonly IRegionManager _regionManager;
    private readonly ILogger<NavigationManager> _logger;
    private readonly IUserNotificationService? _userNotificationService;

    public NavigationManager(
        IRegionManager regionManager,
        ILogger<NavigationManager> logger,
        IUserNotificationService? userNotificationService = null)
    {
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _userNotificationService = userNotificationService;
    }

    /// <summary>显示登录界面</summary>
    public void ShowLoginDialog()
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (_regionManager != null)
                _regionManager.RequestNavigate(RegionNames.LoginRegion, "LoginView");
        });
    }

    /// <summary>清除登录区域</summary>
    public void ClearLoginRegion()
    {
        if (_regionManager.Regions.ContainsRegionWithName(RegionNames.LoginRegion))
            _regionManager.Regions[RegionNames.LoginRegion].RemoveAll();
    }

    /// <summary>清除内容区域</summary>
    public void ClearContentRegion()
    {
        if (_regionManager.Regions.ContainsRegionWithName(RegionNames.ContentRegion))
            _regionManager.Regions[RegionNames.ContentRegion].RemoveAll();
    }

    /// <summary>导航到控件示例页面</summary>
    public void NavigateToControlExamples()
    {
        try { _regionManager.RequestNavigate(RegionNames.ContentRegion, "ControlExamplesView"); }
        catch (Exception ex) { throw new InvalidOperationException(ClientErrorMessageMapper.GetSafeOperationFailureMessage("打开控件示例页面", ex), ex); }
    }

    /// <summary>快速导航到患者管理并触发新增</summary>
    /// <remarks>OpenSpec: refactor-admin-workspace - 导航到角色台管理视图</remarks>
    public void NavigateToAddPatient()
    {
        var navigationParams = new NavigationParameters { { "Action", "AddNew" } };
        _regionManager.RequestNavigate(RegionNames.ContentRegion, "PatientManagementView", navigationParams);
    }

    /// <summary>快速导航到医案工作区视图</summary>
    public void NavigateToMedicalCaseFlow() => _regionManager.RequestNavigate(RegionNames.ContentRegion, "MedicalCaseWorkspaceView");

    /// <summary>poc-drawer-layout: 通用导航到指定视图</summary>
    /// <param name="viewName">视图名称</param>
    public void NavigateTo(string viewName)
    {
        try
        {
            _logger.LogInformation("导航到 {ViewName}", viewName);
            _regionManager.RequestNavigate(RegionNames.ContentRegion, viewName, result =>
            {
                if (result.Result != true)
                {
                    var errorMessage = result.Error?.Message ?? "未知错误";
                    _logger.LogError("导航失败：{ViewName}，错误：{Error}", viewName, errorMessage);

                    // OpenSpec: refactor-auth-role-system - 添加用户友好的错误提示
                    _userNotificationService?.ShowErrorAsync($"无法打开页面：{errorMessage}");
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "导航到 {ViewName} 时发生异常", viewName);

            // OpenSpec: refactor-auth-role-system - 添加用户友好的错误提示
            _userNotificationService?.ShowErrorAsync($"导航失败：{ex.Message}");
        }
    }

    /// <summary>订阅Region集合变化事件</summary>
    public void SubscribeToRegionCollection()
    {
        _regionManager.Regions.CollectionChanged += OnRegionsCollectionChanged;
        foreach (var region in _regionManager.Regions)
            SubscribeToRegionNavigationEvents(region);
        _logger.LogDebug("Region 导航监控已启用");
    }

    /// <summary>取消Region集合变化事件订阅</summary>
    public void UnsubscribeFromRegionCollection()
    {
        try { _regionManager.Regions.CollectionChanged -= OnRegionsCollectionChanged; }
        catch (Exception ex) { _logger.LogWarning(ex, "取消Region监控失败"); }
    }

    /// <summary>Region集合变化事件处理</summary>
    private void OnRegionsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
            foreach (IRegion region in e.NewItems)
                SubscribeToRegionNavigationEvents(region);
    }

    /// <summary>订阅Region导航事件</summary>
    private void SubscribeToRegionNavigationEvents(IRegion region)
    {
        region.NavigationService.Navigating += (s, e) => _logger.LogDebug("导航中: Region={RegionName}, Target={Uri}", region.Name, e.Uri);
        region.NavigationService.Navigated += (s, e) => _logger.LogDebug("导航完成: Region={RegionName}, Uri={Uri}", region.Name, e.Uri);
        region.NavigationService.NavigationFailed += (s, e) => _logger.LogError(e.Error, "导航失败: {RegionName} -> {Uri}", region.Name, e.Uri);
    }
}
