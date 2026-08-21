using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Models.Common;

/// <summary>
/// 分页结果映射（T3.2）
/// 统一 PagedResult 组装，避免 MedicalCaseQueryService 5 处重复
/// </summary>
public static class PagedResultMapper
{
    public static PagedResult<TDto> FromEntities<TEntity, TDto>(
        List<TEntity> entities,
        List<TDto> dtos,
        int totalCount,
        int page,
        int pageSize)
        where TEntity : class
        where TDto : class
        => new PagedResult<TDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        };

    public static PagedResult<T> FromPaged<T>(PagedResult<T> source, List<T> items) where T : class
        => new PagedResult<T>
        {
            Items = items,
            TotalCount = source.TotalCount,
            CurrentPage = source.CurrentPage,
            PageSize = source.PageSize
        };
}
