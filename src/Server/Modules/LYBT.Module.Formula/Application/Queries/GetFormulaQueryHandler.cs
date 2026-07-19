using MediatR;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Formulas.Domain;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Module.Formulas.Application.Mappers;

namespace LYBT.Module.Formulas.Application.Queries;

/// <summary>
/// 根据ID获取验方详情查询处理器。
/// </summary>
public class GetFormulaQueryHandler : IRequestHandler<GetFormulaQuery, Result<FormulaDetailDto>>
{
    private readonly IFormulaRepository _formulaRepository;

    /// <summary>
    /// 初始化查询处理器。
    /// </summary>
    public GetFormulaQueryHandler(IFormulaRepository formulaRepository)
    {
        _formulaRepository = formulaRepository;
    }

    /// <inheritdoc/>
    public async Task<Result<FormulaDetailDto>> Handle(
        GetFormulaQuery request, CancellationToken cancellationToken)
    {
        var formula = await _formulaRepository.GetByIdAsync(request.Id, cancellationToken);

        if (formula == null)
            return Result<FormulaDetailDto>.Failure(ErrorCode.FormulaNotFound, "方剂不存在");

        return Result<FormulaDetailDto>.Success(FormulaDtoMapper.ToDetailDto(formula));
    }
}


