using LYBT.Entities.Formulas;
using LYBT.Module.Catalog.Application.Mappers;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;
using MediatR;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 验方命令处理器（A-31-C3b 合并 Create/Update/Delete/Restore/Toggle 五个同构 Handler）。
/// </summary>
public class FormulaCommandHandler :
    IRequestHandler<CreateEntityCommand<FormulaInputDto, FormulaDetailDto>, Result<FormulaDetailDto>>,
    IRequestHandler<UpdateEntityCommand<FormulaInputDto, FormulaDetailDto>, Result<FormulaDetailDto>>,
    IRequestHandler<DeleteEntityCommand<Formula>, Result>,
    IRequestHandler<RestoreEntityCommand<Formula, FormulaDetailDto>, Result<FormulaDetailDto>>,
    IRequestHandler<ToggleEntityStatusCommand<Formula, FormulaDetailDto>, Result<FormulaDetailDto>>
{
    private readonly IFormulaRepository _formulaRepository;

    public FormulaCommandHandler(IFormulaRepository formulaRepository)
    {
        _formulaRepository = formulaRepository;
    }

    public async Task<Result<FormulaDetailDto>> Handle(
        CreateEntityCommand<FormulaInputDto, FormulaDetailDto> request, CancellationToken cancellationToken)
    {
        var dto = request.Input;

        if (await _formulaRepository.ExistsByNameAsync(dto.Name, ct: cancellationToken))
            return Result<FormulaDetailDto>.Failure(ErrorCode.FormulaNameExists, ErrorMessages.Get(ErrorCode.FormulaNameExists));

        var formula = CatalogDtoMapper.ToEntity(dto, request.CurrentUserId);

        await _formulaRepository.AddAsync(formula, cancellationToken);

        return Result<FormulaDetailDto>.Success(CatalogDtoMapper.ToFormulaDetailDto(formula));
    }

    public async Task<Result<FormulaDetailDto>> Handle(
        UpdateEntityCommand<FormulaInputDto, FormulaDetailDto> request, CancellationToken cancellationToken)
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
        return Result<FormulaDetailDto>.Success(CatalogDtoMapper.ToFormulaDetailDto(formula));
    }

    public async Task<Result> Handle(
        DeleteEntityCommand<Formula> request, CancellationToken cancellationToken)
    {
        var formula = await _formulaRepository.GetByIdAsync(request.Id, cancellationToken);
        if (formula == null)
            return Result.Failure(ErrorCode.FormulaNotFound, ErrorMessages.Get(ErrorCode.FormulaNotFound));

        formula.SoftDelete(request.CurrentUserId);

        await _formulaRepository.UpdateAsync(formula, cancellationToken);

        return Result.Success();
    }

    public async Task<Result<FormulaDetailDto>> Handle(
        RestoreEntityCommand<Formula, FormulaDetailDto> request, CancellationToken cancellationToken)
    {
        var formula = await _formulaRepository.GetByIdIncludingDeletedAsync(request.Id, cancellationToken);
        if (formula == null)
            return Result<FormulaDetailDto>.Failure(ErrorCode.FormulaNotFound, "验方不存在");

        if (!formula.IsDeleted)
            return Result<FormulaDetailDto>.Failure(ErrorCode.FormulaNotFound, ErrorMessages.Get(ErrorCode.FormulaNotDeleted));

        var nameExists = await _formulaRepository.ExistsByNameAsync(formula.Name, formula.Id, cancellationToken);
        if (nameExists)
            return Result<FormulaDetailDto>.Failure(ErrorCode.FormulaNameExists, $"验方名称「{formula.Name}」已存在，无法恢复");

        formula.Restore(request.CurrentUserId);
        await _formulaRepository.UpdateAsync(formula, cancellationToken);
        return Result<FormulaDetailDto>.Success(CatalogDtoMapper.ToFormulaDetailDto(formula));
    }

    public async Task<Result<FormulaDetailDto>> Handle(
        ToggleEntityStatusCommand<Formula, FormulaDetailDto> request, CancellationToken cancellationToken)
    {
        var formula = await _formulaRepository.GetByIdAsync(request.Id, cancellationToken);
        if (formula == null)
            return Result<FormulaDetailDto>.Failure(ErrorCode.FormulaNotFound, ErrorMessages.Get(ErrorCode.FormulaNotFound));

        var newStatus = formula.Status == CommonStatus.Enabled ? CommonStatus.Disabled : CommonStatus.Enabled;
        formula.ChangeStatus(newStatus, request.CurrentUserId);
        await _formulaRepository.UpdateAsync(formula, cancellationToken);
        return Result<FormulaDetailDto>.Success(CatalogDtoMapper.ToFormulaDetailDto(formula));
    }
}
