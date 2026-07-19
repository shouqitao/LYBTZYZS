using System.Collections.Specialized;
using Prism.Regions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Infrastructure.Navigation;

/// <summary>
/// Region 集合监控实现
/// </summary>
public class RegionMonitor : IRegionMonitor
{
    private readonly IRegionManager _regionManager;
    private readonly ILogger<RegionMonitor> _logger;

    public RegionMonitor(IRegionManager regionManager, ILogger<RegionMonitor> logger)
    {
        _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void StartMonitoring()
    {
        _regionManager.Regions.CollectionChanged += OnRegionsCollectionChanged;
        foreach (var region in _regionManager.Regions)
            SubscribeToRegionNavigationEvents(region);
        _logger.LogDebug("Region 导航监控已启用");
    }

    public void StopMonitoring()
    {
        try
        {
            _regionManager.Regions.CollectionChanged -= OnRegionsCollectionChanged;
            _logger.LogDebug("Region 导航监控已取消");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "取消 Region 监控失败");
        }
    }

    private void OnRegionsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.NewItems != null)
        {
            foreach (IRegion region in e.NewItems)
                SubscribeToRegionNavigationEvents(region);
        }
    }

    private void SubscribeToRegionNavigationEvents(IRegion region)
    {
        region.NavigationService.Navigating += (s, e) =>
            _logger.LogDebug("导航中: Region={RegionName}, Target={Uri}", region.Name, e.Uri);
        region.NavigationService.Navigated += (s, e) =>
            _logger.LogDebug("导航完成: Region={RegionName}, Uri={Uri}", region.Name, e.Uri);
        region.NavigationService.NavigationFailed += (s, e) =>
            _logger.LogError(e.Error, "导航失败: {RegionName} -> {Uri}", region.Name, e.Uri);
    }

    public void Dispose()
    {
        StopMonitoring();
    }
}
