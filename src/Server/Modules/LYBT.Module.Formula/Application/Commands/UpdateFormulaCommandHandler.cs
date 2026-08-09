using MediatR;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Module.Formulas.Application.Mappers;

namespace LYBT.Module.Formulas.Application.Commands;

/// <summary>
/// 更新验方命令处理器。
/// </summary>
public class UpdateFormulaCommandHandler : IRequestHandler<UpdateFormulaCommand, Result<FormulaDetailDto>>
{
    private readonly IFormulaRepository _formulaRepository;

    public UpdateFormulaCommandHandler(IFormulaRepository formulaRepository)
    {
        _formulaRepository = formulaRepository;
    }

    public async Task<Result<FormulaDetailDto>> Handle(
        UpdateFormulaCommand request, CancellationToken cancellationToken)
    {
        var formula = await _formulaRepository.GetByIdAsync(request.Id, cancellationToken);
        if (formula == null)
            return Result<FormulaDetailDto>.Failure(ErrorCode.FormulaNotFound, ErrorMessages.Get(ErrorCode.FormulaNotFound));

        if (formula.Name != request.Input.Name)
        {
            if (await _formulaRepository.ExistsByNameAsync(request.Input.Name, request.Id, cancellationToken))
                return Result<FormulaDetailDto>.Failure(ErrorCode.FormulaNameExists, $"方剂名称 '{request.Input.Name}' 已存在");
        }

        formula.UpdateProfile(
            request.Input.Name,
            request.Input.Effect,
            request.Input.Indications,
            request.Input.Usage,
            request.Input.Remark,
            request.Input.Property,
            request.Input.Category,
            request.Input.IsShared,
            request.CurrentUserId);

        await _formulaRepository.UpdateAsync(formula, cancellationToken);
        return Result<FormulaDetailDto>.Success(FormulaDtoMapper.ToDetailDto(formula));
    }
}
