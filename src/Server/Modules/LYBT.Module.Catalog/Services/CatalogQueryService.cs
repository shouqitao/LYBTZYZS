using LYBT.Entities.Common;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Module.Catalog.Services;

/// <summary>
/// 目录只读查询服务实现（A-31-C3b 合并 HerbService/FormulaService 42 行孪生）。
/// 差异点（仓储/映射/错误码）由 DI 工厂注入，同一份实现服务药材与验方两个域。
/// 实体类型仅作为实现泛型参数，不暴露于对外接口（P01b）。
/// </summary>
internal sealed class CatalogQueryService<TEntity, TListDto, TDetailDto> : ICatalogQueryService<TListDto, TDetailDto>
    where TEntity : BaseEntity
{
    private readonly ICatalogRepository<TEntity> _repository;
    private readonly Func<TEntity, TListDto> _toList;
    private readonly Func<TEntity, TDetailDto> _toDetail;
    private readonly ErrorCode _notFoundCode;

    public CatalogQueryService(
        ICatalogRepository<TEntity> repository,
        Func<TEntity, TListDto> toList,
        Func<TEntity, TDetailDto> toDetail,
        ErrorCode notFoundCode)
    {
        _repository = repository;
        _toList = toList;
        _toDetail = toDetail;
        _notFoundCode = notFoundCode;
    }

    public async Task<Result<PagedResult<TListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, Guid? operatorId = null, bool isAdmin = false, CancellationToken ct = default)
    {
        return await GetPagedAsync(page, pageSize, keyword, null, operatorId, isAdmin, ct);
    }

    public async Task<Result<PagedResult<TListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, string? category, Guid? operatorId, bool isAdmin, CancellationToken ct)
    {
        // R-20: 列表页不加载子集合导航
        var result = await _repository.GetPagedAsync(page, pageSize, keyword, category, operatorId, isAdmin, ct, includeChildren: false);
        var dtos = result.Items.Select(_toList).ToList();
        var pagedResult = new PagedResult<TListDto>
        {
            Items = dtos,
            TotalCount = result.TotalCount,
            CurrentPage = result.CurrentPage,
            PageSize = result.PageSize
        };
        return Result<PagedResult<TListDto>>.Success(pagedResult);
    }

    public async Task<Result<List<TDetailDto>>> ExportDetailsAsync(string? keyword = null, string? category = null, Guid? operatorId = null, bool isAdmin = false, CancellationToken ct = default)
    {
        // P2-12: 专用导出查询（不分页，直接 ToListAsync）；R-20: 导出明细需要子集合导航（验方 Herbs）
        var entities = await _repository.GetAllForExportAsync(keyword, category, operatorId, isAdmin, ct, includeChildren: true);
        var dtos = entities.Select(_toDetail).ToList();
        return Result<List<TDetailDto>>.Success(dtos);
    }

    public async Task<Result<List<TListDto>>> ExportListsAsync(string? keyword = null, string? category = null, Guid? operatorId = null, bool isAdmin = false, CancellationToken ct = default)
    {
        // P2-12: 列表导出（药材等无子集合导航需求）
        var entities = await _repository.GetAllForExportAsync(keyword, category, operatorId, isAdmin, ct, includeChildren: false);
        var dtos = entities.Select(_toList).ToList();
        return Result<List<TListDto>>.Success(dtos);
    }

    public async Task<Result<TDetailDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var entity = await _repository.GetByIdAsync(id, ct);
        if (entity == null)
            return Result<TDetailDto>.Failure(_notFoundCode, ErrorMessages.Get(_notFoundCode));
        return Result<TDetailDto>.Success(_toDetail(entity));
    }
}
