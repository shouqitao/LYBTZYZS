using LYBT.Entities.Formulas;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 批量禁用验方命令处理器（A-31-C3b 统一收敛至 CatalogBatchStatusHandlerBase）。
/// </summary>
public class BatchDisableFormulasCommandHandler : CatalogBatchStatusHandlerBase<Formula, BatchDisableFormulasCommand>
{
    public BatchDisableFormulasCommandHandler(IFormulaRepository formulaRepository)
        : base(formulaRepository, CommonStatus.Disabled, "禁用", "验方不存在")
    {
    }

    protected override void ApplyStatusChange(Formula formula)
        => formula.ChangeStatus(TargetStatus, Guid.Empty);

    protected override string? GetEntityName(Formula formula) => formula.Name;
}
