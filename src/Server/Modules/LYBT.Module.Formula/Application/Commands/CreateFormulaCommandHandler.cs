using MediatR;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Module.Formulas.Application.Mappers;

namespace LYBT.Module.Formulas.Application.Commands;

/// <summary>
/// 创建验方命令处理器。
/// </summary>
public class CreateFormulaCommandHandler : IRequestHandler<CreateFormulaCommand, Result<FormulaDetailDto>>
{
    private readonly IFormulaRepository _formulaRepository;

    /// <summary>
    /// 初始化命令处理器。
    /// </summary>
    public CreateFormulaCommandHandler(
        IFormulaRepository formulaRepository)
    {
        _formulaRepository = formulaRepository;
    }

    /// <inheritdoc/>
    public async Task<Result<FormulaDetailDto>> Handle(
        CreateFormulaCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Input;

        if (await _formulaRepository.ExistsByNameAsync(dto.Name, ct: cancellationToken))
            return Result<FormulaDetailDto>.Failure(ErrorCode.FormulaNameExists, ErrorMessages.Get(ErrorCode.FormulaNameExists));

        var formula = FormulaDtoMapper.ToEntity(dto, request.CurrentUserId);

        await _formulaRepository.AddAsync(formula, cancellationToken);

        return Result<FormulaDetailDto>.Success(FormulaDtoMapper.ToDetailDto(formula));
    }
}


