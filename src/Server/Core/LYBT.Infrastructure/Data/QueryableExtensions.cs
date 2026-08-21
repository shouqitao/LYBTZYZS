using LYBT.Entities.Common;

namespace LYBT.Infrastructure.Data;

/// <summary>
/// 查询扩展（T3.2）
/// 统一软删除谓词，避免 !e.IsDeleted 散落 304 行
/// </summary>
public static class QueryableExtensions
{
    /// <summary>过滤未删除（WhereActive）— 替代 !e.IsDeleted</summary>
    public static IQueryable<T> WhereActive<T>(this IQueryable<T> query) where T : class, ISoftDeletable
        => query.Where(e => !e.IsDeleted);

    /// <summary>包含已删除（IgnoreQueryFilters + WhereActive 互补）</summary>
    public static IQueryable<T> WhereIncludingDeleted<T>(this IQueryable<T> query) where T : class, ISoftDeletable
        => query.IgnoreQueryFilters();
}
