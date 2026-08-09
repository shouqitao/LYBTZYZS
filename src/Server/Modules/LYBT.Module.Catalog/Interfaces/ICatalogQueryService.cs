using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Catalog.Interfaces;

/// <summary>
/// 目录只读查询服务泛型接口（A-31-C3b 合并 IHerbService/IFormulaService 孪生）。
/// 读操作直查（写操作走 MediatR Handler，蓝图 §2.2 请求处理边界规则）。
/// </summary>
public interface ICatalogQueryService<TEntity, TListDto, TDetailDto>
{
    /// <summary>分页查询列表（关键字筛选）。</summary>
    Task<Result<PagedResult<TListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, CancellationToken ct);

    /// <summary>按ID获取详情。</summary>
    Task<Result<TDetailDto>> GetByIdAsync(Guid id, CancellationToken ct);
}
