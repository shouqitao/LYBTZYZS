using MediatR;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Module.Formulas.Application.Mappers;

namespace LYBT.Module.Formulas.Application.Commands;

public class ToggleFormulaStatusCommandHandler
    : IRequestHandler<ToggleFormulaStatusCommand, Result<FormulaDetailDto>>
{
    private readonly IFormulaRepository _formulaRepository;

    public ToggleFormulaStatusCommandHandler(IFormulaRepository formulaRepository)
    {
        _formulaRepository = formulaRepository;
    }

    public async Task<Result<FormulaDetailDto>> Handle(
        ToggleFormulaStatusCommand request, CancellationToken cancellationToken)
    {
        var formula = await _formulaRepository.GetByIdAsync(request.Id, cancellationToken);
        if (formula == null)
            return Result<FormulaDetailDto>.Failure(ErrorCode.FormulaNotFound, "方剂不存在");

        var newStatus = formula.Status == CommonStatus.Enabled
            ? CommonStatus.Disabled
            : CommonStatus.Enabled;
        formula.ChangeStatus(newStatus, request.OperatorId);

        await _formulaRepository.UpdateAsync(formula, cancellationToken);

        return Result<FormulaDetailDto>.Success(FormulaDtoMapper.ToDetailDto(formula));
    }
}


