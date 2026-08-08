using LYBT.Shared.Models.Contracts.Common;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Infrastructure.Extensions;

/// <summary>
/// IQueryable 查询扩展方法
/// 从 BaseRepository 提取的通用查询逻辑
/// </summary>
public static class QueryablePagingExtensions
{
    /// <summary>
    /// 将 IQueryable 转换为分页结果
    /// </summary>
    public static async Task<PagedResult<T>> GetPagedResultAsync<T>(
        this IQueryable<T> query,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>(items, totalCount, pageNumber, pageSize);
    }
}
