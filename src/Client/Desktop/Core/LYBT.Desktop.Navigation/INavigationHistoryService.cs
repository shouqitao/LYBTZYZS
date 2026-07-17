using LYBT.Desktop.Shared.UI;

namespace LYBT.Desktop.Navigation;

/// <summary>
/// 导航历史服务接口 — 管理导航历史、前进栈、面包屑
/// </summary>
public interface INavigationHistoryService
{
    /// <summary>导航历史记录</summary>
    IReadOnlyList<string> NavigationHistory { get; }

    /// <summary>是否可以前进</summary>
    bool CanNavigateForward { get; }

    /// <summary>当前面包屑列表</summary>
    IReadOnlyList<BreadcrumbItem> Breadcrumbs { get; }

    /// <summary>记录一次导航</summary>
    void RecordNavigation(string? fromView, string toView);

    /// <summary>从前进栈弹出</summary>
    string? PopForwardStack();

    /// <summary>推入前进栈</summary>
    void PushForwardStack(string viewName);

    /// <summary>清除历史</summary>
    void ClearHistory();
}
