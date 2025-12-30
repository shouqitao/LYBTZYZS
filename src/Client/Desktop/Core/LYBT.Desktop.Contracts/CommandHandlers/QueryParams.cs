namespace LYBT.Desktop.Contracts.CommandHandlers;

/// <summary>
/// CommandHandler查询参数
/// OpenSpec: unify-desktop-architecture (Phase 1.4)
/// 统一所有列表查询的参数格式
/// </summary>
public record QueryParams
{
    /// <summary>
    /// 搜索文本
    /// </summary>
    public string? SearchText { get; init; }

    /// <summary>
    /// 当前页码（从1开始）
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// 每页条数
    /// </summary>
    public int PageSize { get; init; } = 20;

    /// <summary>
    /// 排序字段
    /// </summary>
    public string? SortBy { get; init; }

    /// <summary>
    /// 是否降序排序
    /// </summary>
    public bool SortDescending { get; init; }

    /// <summary>
    /// 额外过滤条件
    /// </summary>
    public Dictionary<string, object>? Filters { get; init; }

    /// <summary>
    /// 创建默认查询参数
    /// </summary>
    public static QueryParams Default => new();

    /// <summary>
    /// 创建搜索查询参数
    /// </summary>
    public static QueryParams Search(string searchText) => new() { SearchText = searchText };

    /// <summary>
    /// 创建分页查询参数
    /// </summary>
    public static QueryParams Paged(int page, int pageSize) => new() { Page = page, PageSize = pageSize };

    /// <summary>
    /// 添加过滤条件
    /// </summary>
    public QueryParams WithFilter(string key, object value)
    {
        var filters = Filters ?? new Dictionary<string, object>();
        filters[key] = value;
        return this with { Filters = filters };
    }
}
