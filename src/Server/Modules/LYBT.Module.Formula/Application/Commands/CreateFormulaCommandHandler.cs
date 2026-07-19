using MediatR;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Primitives.ErrorCodes;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.SharedKernel.Events;
using LYBT.Module.Formulas.Domain;
using LYBT.Module.Formulas.Domain.Events;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Module.Formulas.Application.Mappers;

namespace LYBT.Module.Formulas.Application.Commands;

/// <summary>
/// 创建验方命令处理器。
/// </summary>
public class CreateFormulaCommandHandler : IRequestHandler<CreateFormulaCommand, Result<FormulaDetailDto>>
{
    private readonly IFormulaRepository _formulaRepository;
    private readonly IDomainEventDispatcher _eventDispatcher;

    /// <summary>
    /// 初始化命令处理器。
    /// </summary>
    public CreateFormulaCommandHandler(
        IFormulaRepository formulaRepository,
        IDomainEventDispatcher eventDispatcher)
    {
        _formulaRepository = formulaRepository;
        _eventDispatcher = eventDispatcher;
    }

    /// <inheritdoc/>
    public async Task<Result<FormulaDetailDto>> Handle(
        CreateFormulaCommand request, CancellationToken cancellationToken)
    {
        var dto = request.Input;

        if (await _formulaRepository.ExistsByNameAsync(dto.Name, ct: cancellationToken))
            return Result<FormulaDetailDto>.Failure(ErrorCode.FormulaNameExists, "方剂名称已存在");

        var formula = FormulaDtoMapper.ToEntity(dto, request.CurrentUserId);

        await _formulaRepository.AddAsync(formula, cancellationToken);

        await _eventDispatcher.DispatchAsync(new[]
        {
            new FormulaCreatedEvent(formula.Id, formula.Name, request.CurrentUserId)
        }, cancellationToken);

        return Result<FormulaDetailDto>.Success(FormulaDtoMapper.ToDetailDto(formula));
    }
}


