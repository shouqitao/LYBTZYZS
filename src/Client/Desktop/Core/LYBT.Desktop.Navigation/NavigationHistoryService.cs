using LYBT.Desktop.Shared.UI;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Navigation;

/// <summary>
/// 导航历史服务实现 — 仅管理面包屑和历史记录列表
/// 前进/后退由 Prism IRegionNavigationJournal 管理
/// </summary>
public class NavigationHistoryService : INavigationHistoryService
{
    private const int MaxHistorySize = 20;
    private readonly ILogger<NavigationHistoryService> _logger;
    private readonly List<string> _navigationHistory = new();
    private readonly List<BreadcrumbItem> _breadcrumbs = new();

    public IReadOnlyList<string> NavigationHistory => _navigationHistory.AsReadOnly();
    public IReadOnlyList<BreadcrumbItem> Breadcrumbs => _breadcrumbs.AsReadOnly();

    public NavigationHistoryService(ILogger<NavigationHistoryService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void RecordNavigation(string? fromView, string toView)
    {
        if (_navigationHistory.Count >= MaxHistorySize)
            _navigationHistory.RemoveAt(0);
        _navigationHistory.Add(toView);
        UpdateBreadcrumbs(fromView, toView);
        _logger.LogDebug("导航记录: {From} -> {To}", fromView, toView);
    }

    public void ClearHistory()
    {
        _navigationHistory.Clear();
        _breadcrumbs.Clear();
        _logger.LogDebug("导航历史已清除");
    }

    private void UpdateBreadcrumbs(string? fromView, string toView)
    {
        var toTitle = toView?.Replace("View", "") ?? toView;

        if (fromView == null)
            _breadcrumbs.Clear();

        for (var i = 0; i < _breadcrumbs.Count; i++)
        {
            if (_breadcrumbs[i].IsCurrent)
            {
                _breadcrumbs[i] = new BreadcrumbItem(
                    _breadcrumbs[i].Title, _breadcrumbs[i].ViewName, false);
                break;
            }
        }

        _breadcrumbs.Add(new BreadcrumbItem(
            toTitle ?? toView ?? "Unknown", toView ?? "Unknown", true));
    }
}
