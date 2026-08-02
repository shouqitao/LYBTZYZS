using LYBT.Module.Formulas.Application.Mappers;
using LYBT.Module.Formulas.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Module.Formulas.Services;

/// <summary>
/// 验方服务实现 — 封装简单 CRUD 操作，替代 trivial MediatR Handler。
/// </summary>
internal class FormulaService : IFormulaService
{
    private readonly IFormulaRepository _formulaRepository;

    public FormulaService(IFormulaRepository formulaRepository)
    {
        _formulaRepository = formulaRepository;
    }

    public async Task<Result<PagedResult<FormulaListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, CancellationToken ct)
    {
        var result = await _formulaRepository.GetPagedAsync(page, pageSize, keyword, null, ct);
        var dtos = result.Items.Select(FormulaDtoMapper.ToListDto).ToList();
        var pagedResult = new PagedResult<FormulaListDto>
        {
            Items = dtos,
            TotalCount = result.TotalCount,
            CurrentPage = result.CurrentPage,
            PageSize = result.PageSize
        };
        return Result<PagedResult<FormulaListDto>>.Success(pagedResult);
    }

    public async Task<Result<FormulaDetailDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var formula = await _formulaRepository.GetByIdAsync(id, ct);
        if (formula == null)
            return Result<FormulaDetailDto>.Failure(ErrorCode.FormulaNotFound, "方剂不存在");
        return Result<FormulaDetailDto>.Success(FormulaDtoMapper.ToDetailDto(formula));
    }

    public async Task<Result<FormulaDetailDto>> UpdateAsync(Guid id, FormulaInputDto dto, Guid operatorId, CancellationToken ct)
    {
        var formula = await _formulaRepository.GetByIdAsync(id, ct);
        if (formula == null)
            return Result<FormulaDetailDto>.Failure(ErrorCode.FormulaNotFound, "方剂不存在");

        if (formula.Name != dto.Name)
        {
            if (await _formulaRepository.ExistsByNameAsync(dto.Name, id, ct))
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
            operatorId);

        await _formulaRepository.UpdateAsync(formula, ct);
        return Result<FormulaDetailDto>.Success(FormulaDtoMapper.ToDetailDto(formula));
    }

    public async Task<Result<FormulaDetailDto>> ToggleStatusAsync(Guid id, Guid operatorId, CancellationToken ct)
    {
        var formula = await _formulaRepository.GetByIdAsync(id, ct);
        if (formula == null)
            return Result<FormulaDetailDto>.Failure(ErrorCode.FormulaNotFound, "方剂不存在");

        var newStatus = formula.Status == CommonStatus.Enabled ? CommonStatus.Disabled : CommonStatus.Enabled;
        formula.ChangeStatus(newStatus, operatorId);
        await _formulaRepository.UpdateAsync(formula, ct);
        return Result<FormulaDetailDto>.Success(FormulaDtoMapper.ToDetailDto(formula));
    }

    public async Task<Result<FormulaDetailDto>> RestoreAsync(Guid id, Guid operatorId, CancellationToken ct)
    {
        var formula = await _formulaRepository.GetByIdIncludingDeletedAsync(id, ct);
        if (formula == null)
            return Result<FormulaDetailDto>.Failure(ErrorCode.FormulaNotFound, "验方不存在");

        if (!formula.IsDeleted)
            return Result<FormulaDetailDto>.Failure(ErrorCode.FormulaNotFound, "验方未被删除，无需恢复");

        var nameExists = await _formulaRepository.ExistsByNameAsync(formula.Name, formula.Id, ct);
        if (nameExists)
            return Result<FormulaDetailDto>.Failure(ErrorCode.FormulaNameExists, $"验方名称「{formula.Name}」已存在，无法恢复");

        formula.Restore(operatorId);
        await _formulaRepository.UpdateAsync(formula, ct);
        return Result<FormulaDetailDto>.Success(FormulaDtoMapper.ToDetailDto(formula));
    }

    public async Task<Result<BatchOperationResultDto>> BatchEnableAsync(List<Guid> ids, CancellationToken ct)
    {
        var result = new BatchOperationResultDto { TotalCount = ids.Count };
        foreach (var id in ids)
        {
            var formula = await _formulaRepository.GetByIdAsync(id, ct);
            if (formula == null)
            {
                result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Reason = "验方不存在" });
                result.FailureCount++;
                continue;
            }
            try
            {
                formula.ChangeStatus(CommonStatus.Enabled, Guid.Empty);
                await _formulaRepository.UpdateAsync(formula, ct);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Name = formula.Name, Reason = ex.Message });
                result.FailureCount++;
            }
        }
        result.Message = $"批量启用完成: 成功{result.SuccessCount}个, 失败{result.FailureCount}个";
        return Result<BatchOperationResultDto>.Success(result);
    }

    public async Task<Result<BatchOperationResultDto>> BatchDisableAsync(List<Guid> ids, CancellationToken ct)
    {
        var result = new BatchOperationResultDto { TotalCount = ids.Count };
        foreach (var id in ids)
        {
            var formula = await _formulaRepository.GetByIdAsync(id, ct);
            if (formula == null)
            {
                result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Reason = "验方不存在" });
                result.FailureCount++;
                continue;
            }
            try
            {
                formula.ChangeStatus(CommonStatus.Disabled, Guid.Empty);
                await _formulaRepository.UpdateAsync(formula, ct);
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
