using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Module.Catalog.Interfaces;

/// <summary>
/// 目录只读查询服务泛型接口（A-31-C3b 合并 IHerbService/IFormulaService 孪生）。
/// 读操作直查（写操作走 MediatR Handler，蓝图 §2.2 请求处理边界规则）。
/// 接口不暴露实体类型（P01b：UI 层不得依赖 Entities），实体差异由 DI 工厂绑定。
/// </summary>
public interface ICatalogQueryService<TListDto, TDetailDto>
{
    /// <summary>分页查询列表（关键字筛选，兼容旧调用）。</summary>
    Task<Result<PagedResult<TListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, Guid? operatorId = null, bool isAdmin = false, CancellationToken ct = default);

    /// <summary>分页查询列表（关键字/分类筛选）。</summary>
    Task<Result<PagedResult<TListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, string? category, Guid? operatorId, bool isAdmin, CancellationToken ct);

    /// <summary>按ID获取详情。</summary>
    Task<Result<TDetailDto>> GetByIdAsync(Guid id, CancellationToken ct);

    /// <summary>导出详情列表（含关联明细，如验方 Herbs；上限 10000）。</summary>
    Task<Result<List<TDetailDto>>> ExportDetailsAsync(string? keyword = null, string? category = null, Guid? operatorId = null, bool isAdmin = false, CancellationToken ct = default);
}
