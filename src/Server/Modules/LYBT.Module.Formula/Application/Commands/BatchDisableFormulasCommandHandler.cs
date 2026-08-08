using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Formulas.Interfaces;

namespace LYBT.Module.Formulas.Application.Commands;

/// <summary>
/// 批量禁用验方命令处理器。
/// </summary>
public class BatchDisableFormulasCommandHandler : IRequestHandler<BatchDisableFormulasCommand, Result<BatchOperationResultDto>>
{
    private readonly IFormulaRepository _formulaRepository;

    public BatchDisableFormulasCommandHandler(IFormulaRepository formulaRepository)
    {
        _formulaRepository = formulaRepository;
    }

    public async Task<Result<BatchOperationResultDto>> Handle(
        BatchDisableFormulasCommand request, CancellationToken cancellationToken)
    {
        var result = new BatchOperationResultDto { TotalCount = request.Ids.Count };
        foreach (var id in request.Ids)
        {
            var formula = await _formulaRepository.GetByIdAsync(id, cancellationToken);
            if (formula == null)
            {
                result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Reason = "验方不存在" });
                result.FailureCount++;
                continue;
            }
            try
            {
                formula.ChangeStatus(CommonStatus.Disabled, Guid.Empty);
                await _formulaRepository.UpdateAsync(formula, cancellationToken);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Name = formula.Name, Reason = ex.Message });
                result.FailureCount++;
            }
        }
        result.Message = $"批量禁用完成: 成功{result.SuccessCount}个, 失败{result.FailureCount}个";
        return Result<BatchOperationResultDto>.Success(result);
    }
}
