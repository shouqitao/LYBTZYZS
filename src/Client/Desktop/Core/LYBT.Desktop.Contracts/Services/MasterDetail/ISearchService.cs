namespace LYBT.Desktop.Contracts.Services.MasterDetail;

/// <summary>
/// 搜索服务接口
/// OpenSpec: unify-desktop-architecture (Phase 1.2)
/// 管理列表数据的搜索功能
/// </summary>
public interface ISearchService
{
    /// <summary>
    /// 搜索文本
    /// </summary>
    string? SearchText { get; set; }

    /// <summary>
    /// 是否正在搜索
    /// </summary>
    bool IsSearching { get; }

    /// <summary>
    /// 搜索延迟时间（毫秒）
    /// </summary>
    int SearchDelayMs { get; set; }

    /// <summary>
    /// 执行搜索
    /// </summary>
    /// <param name="searchText">搜索文本</param>
    Task SearchAsync(string? searchText);

    /// <summary>
    /// 清除搜索
    /// </summary>
    void ClearSearch();

    /// <summary>
    /// 搜索完成事件
    /// </summary>
    event EventHandler? SearchCompleted;
}
