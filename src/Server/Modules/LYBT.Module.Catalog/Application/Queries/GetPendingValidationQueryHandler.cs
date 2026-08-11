using LYBT.Module.Catalog.Application.Mappers;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using MediatR;

namespace LYBT.Module.Catalog.Application.Queries;

/// <summary>
/// 获取待验证验方列表查询处理器。
/// </summary>
public class GetPendingValidationQueryHandler(
    IFormulaRepository formulaRepository) : IRequestHandler<GetPendingValidationQuery, Result<PagedResult<FormulaDetailDto>>>
{
    private readonly IFormulaRepository _formulaRepository = formulaRepository;

    public async Task<Result<PagedResult<FormulaDetailDto>>> Handle(
        GetPendingValidationQuery request, CancellationToken cancellationToken)
    {
        var pendingFormulas = await _formulaRepository.FindWithHerbsAsync(
            f => f.ValidationStatus == FormulaValidationStatus.Draft,
            cancellationToken);

        // P3 (US-FORM-007): 待验证列表分页（原全量返回）
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);
        var totalCount = pendingFormulas.Count;
        var dtos = pendingFormulas
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(CatalogDtoMapper.ToFormulaDetailDto)
            .ToList();

        return Result<PagedResult<FormulaDetailDto>>.Success(new PagedResult<FormulaDetailDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            CurrentPage = page,
            PageSize = pageSize
        });
    }
}
