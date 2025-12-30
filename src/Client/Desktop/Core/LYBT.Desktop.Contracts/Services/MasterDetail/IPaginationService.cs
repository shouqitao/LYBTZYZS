namespace LYBT.Desktop.Contracts.Services.MasterDetail;

/// <summary>
/// 分页服务接口
/// OpenSpec: unify-desktop-architecture (Phase 1.2)
/// 管理列表数据的分页状态和导航
/// </summary>
public interface IPaginationService
{
    /// <summary>
    /// 每页条数
    /// </summary>
    int PageSize { get; set; }

    /// <summary>
    /// 当前页码（从1开始）
    /// </summary>
    int CurrentPage { get; set; }

    /// <summary>
    /// 总记录数
    /// </summary>
    int TotalCount { get; set; }

    /// <summary>
    /// 总页数
    /// </summary>
    int TotalPages { get; }

    /// <summary>
    /// 是否有上一页
    /// </summary>
    bool HasPreviousPage { get; }

    /// <summary>
    /// 是否有下一页
    /// </summary>
    bool HasNextPage { get; }

    /// <summary>
    /// 跳转到指定页
    /// </summary>
    /// <param name="page">目标页码</param>
    Task GoToPageAsync(int page);

    /// <summary>
    /// 跳转到首页
    /// </summary>
    Task GoToFirstPageAsync();

    /// <summary>
    /// 跳转到末页
    /// </summary>
    Task GoToLastPageAsync();

    /// <summary>
    /// 上一页
    /// </summary>
    Task PreviousPageAsync();

    /// <summary>
    /// 下一页
    /// </summary>
    Task NextPageAsync();

    /// <summary>
    /// 分页状态变化事件
    /// </summary>
    event EventHandler? PageChanged;
}
