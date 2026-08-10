using LYBT.Entities.Herbs;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 批量启用药材命令处理器（A-31-C3b 统一收敛至 CatalogBatchStatusHandlerBase）。
/// </summary>
public class BatchEnableHerbsCommandHandler : CatalogBatchStatusHandlerBase<Herb, BatchEnableHerbsCommand>
{
    public BatchEnableHerbsCommandHandler(IHerbRepository herbRepository)
        : base(herbRepository, CommonStatus.Enabled, "启用", "药材不存在")
    {
    }

    protected override void ApplyStatusChange(Herb herb)
        => herb.ChangeStatus(TargetStatus, Guid.Empty);

    protected override string? GetEntityName(Herb herb) => herb.Name;
}
