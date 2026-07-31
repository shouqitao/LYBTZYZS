using MediatR;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Module.Herbs.Interfaces;

namespace LYBT.Module.Herbs.Application.Commands;

public class BatchEnableHerbsCommandHandler : IRequestHandler<BatchEnableHerbsCommand, Result<BatchOperationResultDto>>
{
    private readonly IHerbRepository _herbRepository;

    public BatchEnableHerbsCommandHandler(IHerbRepository herbRepository)
    {
        _herbRepository = herbRepository;
    }

    public async Task<Result<BatchOperationResultDto>> Handle(
        BatchEnableHerbsCommand request, CancellationToken cancellationToken)
    {
        var result = new BatchOperationResultDto { TotalCount = request.Ids.Count };

        foreach (var id in request.Ids)
        {
            var herb = await _herbRepository.GetByIdAsync(id, cancellationToken);
            if (herb == null)
            {
                result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Reason = "药材不存在" });
                result.FailureCount++;
                continue;
            }

            try
            {
                herb.ChangeStatus(CommonStatus.Enabled, Guid.Empty);
                await _herbRepository.UpdateAsync(herb, cancellationToken);
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
}

public class BatchDisableHerbsCommandHandler : IRequestHandler<BatchDisableHerbsCommand, Result<BatchOperationResultDto>>
{
    private readonly IHerbRepository _herbRepository;

    public BatchDisableHerbsCommandHandler(IHerbRepository herbRepository)
    {
        _herbRepository = herbRepository;
    }

    public async Task<Result<BatchOperationResultDto>> Handle(
        BatchDisableHerbsCommand request, CancellationToken cancellationToken)
    {
        var result = new BatchOperationResultDto { TotalCount = request.Ids.Count };

        foreach (var id in request.Ids)
        {
            var herb = await _herbRepository.GetByIdAsync(id, cancellationToken);
            if (herb == null)
            {
                result.FailedItems.Add(new BatchOperationFailureItem { Id = id, Reason = "药材不存在" });
                result.FailureCount++;
                continue;
            }

            try
            {
                herb.ChangeStatus(CommonStatus.Disabled, Guid.Empty);
                await _herbRepository.UpdateAsync(herb, cancellationToken);
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
