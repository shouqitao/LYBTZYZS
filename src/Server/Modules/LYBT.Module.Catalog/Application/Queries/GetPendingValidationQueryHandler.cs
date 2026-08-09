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
    IFormulaRepository formulaRepository) : IRequestHandler<GetPendingValidationQuery, Result<List<FormulaDetailDto>>>
{
    private readonly IFormulaRepository _formulaRepository = formulaRepository;

    public async Task<Result<List<FormulaDetailDto>>> Handle(
        GetPendingValidationQuery request, CancellationToken cancellationToken)
    {
        var pendingFormulas = await _formulaRepository.FindWithHerbsAsync(
            f => f.ValidationStatus == FormulaValidationStatus.Draft,
            cancellationToken);

        var dtos = pendingFormulas.Select(CatalogDtoMapper.ToFormulaDetailDto).ToList();

        return Result<List<FormulaDetailDto>>.Success(dtos);
    }
}
