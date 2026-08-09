using LYBT.Module.Formulas.Application.Mappers;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Module.Formulas.Services;

/// <summary>
/// 验方服务实现 — 读操作直查（写操作已收敛至 MediatR Handler，见蓝图 §2.2）。
/// </summary>
internal class FormulaService : IFormulaService
{
    private readonly IFormulaRepository _formulaRepository;

    public FormulaService(IFormulaRepository formulaRepository)
    {
        _formulaRepository = formulaRepository;
    }

    public async Task<Result<PagedResult<FormulaListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, CancellationToken ct)
    {
        var result = await _formulaRepository.GetPagedAsync(page, pageSize, keyword, null, ct);
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

    public async Task<Result<FormulaDetailDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var formula = await _formulaRepository.GetByIdAsync(id, ct);
        if (formula == null)
            return Result<FormulaDetailDto>.Failure(ErrorCode.FormulaNotFound, ErrorMessages.Get(ErrorCode.FormulaNotFound));
        return Result<FormulaDetailDto>.Success(FormulaDtoMapper.ToDetailDto(formula));
    }
}
