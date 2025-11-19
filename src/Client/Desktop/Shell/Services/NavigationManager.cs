using System.Windows;
using LYBT.Desktop.Infrastructure.Constants;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Shell.Services;

/// <summary>
/// 导航管理器 - 负责Region导航、角色导航、页面跳转
/// Issue #1790: 从MainWindowViewModel提取导航逻辑（~150行）
/// </summary>
public class NavigationManager
{
    private readonly IRegionManager _regionManager;
    private readonly ILogger<NavigationManager> _logger;

    public NavigationManager(
        IRegionManager regionManager,
        ILogger<NavigationManager> logger)
    {
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// 显示登录界面
    /// </summary>
    public void ShowLoginDialog()
    {
        Application.Current.Dispatcher.InvokeAsync(() =>
        {
            if (_regionManager != null)
            {
                System.Diagnostics.Debug.WriteLine("📱 ShowLoginDialog: 导航到登录视图");
                _regionManager.RequestNavigate(RegionNames.LoginRegion, "LoginView");
            }
        });
    }

    /// <summary>
    /// 清除登录区域
    /// </summary>
    public void ClearLoginRegion()
    {
        if (_regionManager.Regions.ContainsRegionWithName(RegionNames.LoginRegion))
        {
            _regionManager.Regions[RegionNames.LoginRegion].RemoveAll();
        }
    }

    /// <summary>
    /// 清除内容区域
    /// </summary>
    public void ClearContentRegion()
    {
        if (_regionManager.Regions.ContainsRegionWithName(RegionNames.ContentRegion))
        {
            _regionManager.Regions[RegionNames.ContentRegion].RemoveAll();
        }
    }

    /// <summary>
    /// 导航到控件示例页面
    /// </summary>
    public void NavigateToControlExamples()
    {
        try
        {
            _regionManager.RequestNavigate(RegionNames.ContentRegion, "ControlExamplesView");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"打开控件示例页面失败: {ex.Message}");
            throw new InvalidOperationException($"打开控件示例页面失败: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// 快速导航到患者管理并触发新增
    /// Issue #1790: 从ExecuteQuickAddPatientAsync提取
    /// </summary>
    public void NavigateToAddPatient()
    {
        var navigationParams = new NavigationParameters();
        navigationParams.Add("Action", "AddNew");
        _regionManager.RequestNavigate(RegionNames.ContentRegion, "PatientManagementView", navigationParams);
    }

    /// <summary>
    /// 快速导航到医案流程视图
    /// Issue #1790: 从ExecuteQuickStartConsultationAsync提取
    /// </summary>
    public void NavigateToMedicalCaseFlow()
    {
        _regionManager.RequestNavigate(RegionNames.ContentRegion, "MedicalCaseFlowView");
    }

    /// <summary>
    /// 订阅Region集合变化事件
    /// Issue #877: Region导航监控
    /// </summary>
    public void SubscribeToRegionCollection()
    {
        _regionManager.Regions.CollectionChanged += OnRegionsCollectionChanged;

        // 订阅现有Region的导航事件
        foreach (var region in _regionManager.Regions)
        {
            SubscribeToRegionNavigationEvents(region);
        }

        _logger.LogDebug("Region 导航监控已启用");
    }

    /// <summary>
    /// 取消Region集合变化事件订阅
    /// Issue #877: Region导航监控清理
    /// </summary>
    public void UnsubscribeFromRegionCollection()
    {
        try
        {
            _regionManager.Regions.CollectionChanged -= OnRegionsCollectionChanged;
            System.Diagnostics.Debug.WriteLine(" [NavigationManager] Region 导航监控已取消");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"❌ [NavigationManager] 取消Region监控失败: {ex.Message}");
        }
    }

    /// <summary>
    /// Region集合变化事件处理 - Issue #877
    /// </summary>
    private void OnRegionsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (IRegion region in e.NewItems)
            {
                SubscribeToRegionNavigationEvents(region);
                System.Diagnostics.Debug.WriteLine($"🔔 新 Region 已注册并监控: {region.Name}");
            }
        }
    }

    /// <summary>
    /// 订阅Region导航事件 - Issue #877
    /// </summary>
    private void SubscribeToRegionNavigationEvents(IRegion region)
    {
        region.NavigationService.Navigating += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine($" 导航中: Region={region.Name}, Target={e.Uri}");
        };

        region.NavigationService.Navigated += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine($" 导航完成: Region={region.Name}, Uri={e.Uri}");
        };

        region.NavigationService.NavigationFailed += (s, e) =>
        {
            System.Diagnostics.Debug.WriteLine($"❌ 导航失败: Region={region.Name}, Uri={e.Uri}, Error={e.Error?.Message}");
            _logger.LogError(e.Error, "Region 导航失败: {RegionName} -> {Uri}", region.Name, e.Uri);
        };
    }
}
