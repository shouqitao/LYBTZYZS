using LYBT.Desktop.Contracts.UI;

namespace LYBT.Desktop.Infrastructure.Navigation;

/// <summary>
/// 导航历史服务接口 — 管理导航历史和面包屑
/// </summary>
public interface INavigationHistoryService
{
    /// <summary>导航历史记录</summary>
    IReadOnlyList<string> NavigationHistory { get; }

    /// <summary>当前面包屑列表</summary>
    IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; }

    /// <summary>记录一次导航</summary>
    void RecordNavigation(string? fromView, string toView);

    /// <summary>清除历史</summary>
    void ClearHistory();
}
