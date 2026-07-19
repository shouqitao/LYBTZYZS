using MediatR;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Module.Formulas.Application.Mappers;

namespace LYBT.Module.Formulas.Application.Queries;

/// <summary>
/// 获取待验证验方列表查询处理器。
/// </summary>
public class GetPendingValidationQueryHandler(
    IFormulaRepository formulaRepository) : IRequestHandler<GetPendingValidationQuery, Result<List<FormulaDetailDto>>>
{
    private readonly IFormulaRepository _formulaRepository = formulaRepository;

    public async Task<Result<List<FormulaDetailDto>>> Handle(
        GetPendingValidationQuery request, CancellationToken cancellationToken)
    {
        var pendingFormulas = await _formulaRepository.FindWithHerbsAsync(
            f => f.ValidationStatus == FormulaValidationStatus.Draft,
            cancellationToken);

        var dtos = pendingFormulas.Select(FormulaDtoMapper.ToDetailDto).ToList();

        return Result<List<FormulaDetailDto>>.Success(dtos);
    }
}
