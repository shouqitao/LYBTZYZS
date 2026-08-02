using LYBT.Module.Herbs.Application.Mappers;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Module.Herbs.Services;

/// <summary>
/// 药材服务实现 — 封装简单 CRUD 操作，替代 trivial MediatR Handler。
/// </summary>
internal class HerbService : IHerbService
{
    private readonly IHerbRepository _herbRepository;

    public HerbService(IHerbRepository herbRepository)
    {
        _herbRepository = herbRepository;
    }

    public async Task<Result<PagedResult<HerbListDto>>> GetPagedAsync(int page, int pageSize, string? keyword, CancellationToken ct)
    {
        var result = await _herbRepository.GetPagedAsync(page, pageSize, keyword, null, ct);
        var dtos = result.Items.Select(HerbDtoMapper.ToListDto).ToList();
        var pagedResult = new PagedResult<HerbListDto>
        {
            Items = dtos,
            TotalCount = result.TotalCount,
            CurrentPage = result.CurrentPage,
            PageSize = result.PageSize
        };
        return Result<PagedResult<HerbListDto>>.Success(pagedResult);
    }

    public async Task<Result<HerbDetailDto>> GetByIdAsync(Guid id, CancellationToken ct)
    {
        var herb = await _herbRepository.GetByIdAsync(id, ct);
        if (herb == null)
            return Result<HerbDetailDto>.Failure(ErrorCode.HerbNotFound, "药材不存在");
        return Result<HerbDetailDto>.Success(HerbDtoMapper.ToDetailDto(herb));
    }

    public async Task<Result<HerbDetailDto>> UpdateAsync(Guid id, HerbInputDto dto, Guid operatorId, CancellationToken ct)
    {
        var herb = await _herbRepository.GetByIdAsync(id, ct);
        if (herb == null)
            return Result<HerbDetailDto>.Failure(ErrorCode.HerbNotFound, "药材不存在");

        if (herb.Name != dto.Name)
        {
            if (await _herbRepository.ExistsByNameAsync(dto.Name, id, ct))
                return Result<HerbDetailDto>.Failure(ErrorCode.HerbNameExists, $"药材名称 '{dto.Name}' 已存在");
        }

        herb.UpdateProfile(
            dto.Name,
            dto.Unit,
            dto.Price,
            dto.PinYinCode,
            dto.Category,
            dto.Properties,
            dto.Origin,
            dto.Spec,
            dto.CostPrice,
            dto.Effect,
            dto.Usage,
            dto.Remark,
            operatorId);

        await _herbRepository.UpdateAsync(herb, ct);
        return Result<HerbDetailDto>.Success(HerbDtoMapper.ToDetailDto(herb));
    }

    public async Task<Result<HerbDetailDto>> ToggleStatusAsync(Guid id, Guid operatorId, CancellationToken ct)
    {
        var herb = await _herbRepository.GetByIdAsync(id, ct);
        if (herb == null)
            return Result<HerbDetailDto>.Failure(ErrorCode.HerbNotFound, "药材不存在");

        herb.ChangeStatus(
            herb.Status == CommonStatus.Enabled ? CommonStatus.Disabled : CommonStatus.Enabled,
            operatorId);

        await _herbRepository.UpdateAsync(herb, ct);
        return Result<HerbDetailDto>.Success(HerbDtoMapper.ToDetailDto(herb));
    }

    public async Task<Result<HerbDetailDto>> RestoreAsync(Guid id, Guid operatorId, CancellationToken ct)
    {
        var herb = await _herbRepository.GetByIdIncludingDeletedAsync(id, ct);
        if (herb == null)
            return Result<HerbDetailDto>.Failure(ErrorCode.HerbNotFound, "药材不存在");

        if (!herb.IsDeleted)
            return Result<HerbDetailDto>.Failure(ErrorCode.HerbNotFound, "药材未被删除，无需恢复");

        var nameExists = await _herbRepository.ExistsByNameAsync(herb.Name, herb.Id, ct);
        if (nameExists)
            return Result<HerbDetailDto>.Failure(ErrorCode.HerbNameExists, $"药材名称「{herb.Name}」已存在，无法恢复");

        herb.Restore(operatorId);
        await _herbRepository.UpdateAsync(herb, ct);
        return Result<HerbDetailDto>.Success(HerbDtoMapper.ToDetailDto(herb));
    }

    public async Task<Result<BatchOperationResultDto>> BatchEnableAsync(List<Guid> ids, CancellationToken ct)
    {
        var result = new BatchOperationResultDto { TotalCount = ids.Count };
        foreach (var id in ids)
        {
            var herb = await _herbRepository.GetByIdAsync(id, ct);
            if (herb == null)
            {
                result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Reason = "药材不存在" });
                result.FailureCount++;
                continue;
            }
            try
            {
                herb.ChangeStatus(CommonStatus.Enabled, Guid.Empty);
                await _herbRepository.UpdateAsync(herb, ct);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Name = herb.Name, Reason = ex.Message });
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
            var herb = await _herbRepository.GetByIdAsync(id, ct);
            if (herb == null)
            {
                result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Reason = "药材不存在" });
                result.FailureCount++;
                continue;
            }
            try
            {
                herb.ChangeStatus(CommonStatus.Disabled, Guid.Empty);
                await _herbRepository.UpdateAsync(herb, ct);
                result.SuccessCount++;
            }
            catch (Exception ex)
            {
                result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Name = herb.Name, Reason = ex.Message });
                result.FailureCount++;
            }
        }
        result.Message = $"批量禁用完成: 成功{result.SuccessCount}个, 失败{result.FailureCount}个";
        return Result<BatchOperationResultDto>.Success(result);
    }
}
