using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.SharedKernel.Common;
using LYBT.Module.Formulas.Domain;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Module.Formulas.Application.Mappers;

namespace LYBT.Module.Formulas.Application.Queries;

/// <summary>
/// 获取验方分页列表查询处理器。
/// </summary>
public class GetFormulasQueryHandler : IRequestHandler<GetFormulasQuery, Result<PagedResult<FormulaListDto>>>
{
    private readonly IFormulaRepository _formulaRepository;

    /// <summary>
    /// 初始化查询处理器。
    /// </summary>
    public GetFormulasQueryHandler(IFormulaRepository formulaRepository)
    {
        _formulaRepository = formulaRepository;
    }

    /// <inheritdoc/>
    public async Task<Result<PagedResult<FormulaListDto>>> Handle(
        GetFormulasQuery request, CancellationToken cancellationToken)
    {
        var result = await _formulaRepository.GetPagedAsync(
            request.Page, request.PageSize, request.Keyword, request.Category, cancellationToken);

        var dtos = result.Items.Select(FormulaDtoMapper.ToListDto).ToList();

        var pagedResult = new PagedResult<FormulaListDto>
        {
            Items = dtos,
            TotalCount = result.TotalCount,
            CurrentPage = result.CurrentPage,
            PageSize = result.PageSize
        };

        return Result<PagedResult<FormulaListDto>>.Success(pagedResult);
    }
}


