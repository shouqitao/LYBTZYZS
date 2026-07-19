using MediatR;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Formulas.Domain;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Module.Formulas.Application.Mappers;

namespace LYBT.Module.Formulas.Application.Commands;

/// <summary>
/// 更新验方命令处理器。
/// </summary>
public class UpdateFormulaCommandHandler : IRequestHandler<UpdateFormulaCommand, Result<FormulaDetailDto>>
{
    private readonly IFormulaRepository _formulaRepository;

    /// <summary>
    /// 初始化命令处理器。
    /// </summary>
    public UpdateFormulaCommandHandler(IFormulaRepository formulaRepository)
    {
        _formulaRepository = formulaRepository;
    }

    /// <inheritdoc/>
    public async Task<Result<FormulaDetailDto>> Handle(
        UpdateFormulaCommand request, CancellationToken cancellationToken)
    {
        var formula = await _formulaRepository.GetByIdAsync(request.Id, cancellationToken);
        if (formula == null)
            return Result<FormulaDetailDto>.Failure(ErrorCode.FormulaNotFound, "方剂不存在");

        var dto = request.Input;

        if (formula.Name != dto.Name)
        {
            if (await _formulaRepository.ExistsByNameAsync(dto.Name, request.Id, ct: cancellationToken))
                return Result<FormulaDetailDto>.Failure(ErrorCode.FormulaNameExists, $"方剂名称 '{dto.Name}' 已存在");
        }

        formula.UpdateProfile(
            dto.Name,
            dto.Effect,
            dto.Indications,
            dto.Usage,
            dto.Remark,
            dto.Property,
            dto.Category,
            dto.IsShared,
            request.CurrentUserId);

        await _formulaRepository.UpdateAsync(formula, cancellationToken);

        return Result<FormulaDetailDto>.Success(FormulaDtoMapper.ToDetailDto(formula));
    }
}


