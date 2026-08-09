using LYBT.Entities.Herbs;
using LYBT.Infrastructure.BatchOperations;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using MediatR;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 批量禁用药材命令处理器（A-31-C3b 统一收敛至 BatchOperationHandlerBase）。
/// </summary>
public class BatchDisableHerbsCommandHandler
    : BatchOperationHandlerBase<Herb>,
      IRequestHandler<BatchDisableHerbsCommand, Result<BatchOperationResultDto>>
{
    private readonly IHerbRepository _herbRepository;

    public BatchDisableHerbsCommandHandler(IHerbRepository herbRepository)
    {
        _herbRepository = herbRepository;
    }

    public Task<Result<BatchOperationResultDto>> Handle(
        BatchDisableHerbsCommand request, CancellationToken cancellationToken)
        => ExecuteBatchAsync(request.Ids, Guid.Empty, cancellationToken);

    protected override Task<Herb?> GetByIdAsync(Guid id, CancellationToken ct)
        => _herbRepository.GetByIdAsync(id, ct);

    protected override Task UpdateAsync(Herb herb, CancellationToken ct)
        => _herbRepository.UpdateAsync(herb, ct);

    protected override Task ApplyOperationAsync(Herb herb, Guid operatorId, CancellationToken ct)
    {
        herb.ChangeStatus(CommonStatus.Disabled, Guid.Empty);
        return Task.CompletedTask;
    }

    protected override string EntityNotFoundMessage => "药材不存在";
    protected override string OperationName => "禁用";
    protected override string? GetEntityName(Herb herb) => herb.Name;
}
