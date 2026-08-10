using LYBT.Entities.Formulas;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 批量启用验方命令处理器（A-31-C3b 统一收敛至 CatalogBatchStatusHandlerBase）。
/// </summary>
public class BatchEnableFormulasCommandHandler : CatalogBatchStatusHandlerBase<Formula, BatchEnableFormulasCommand>
{
    public BatchEnableFormulasCommandHandler(IFormulaRepository formulaRepository)
        : base(formulaRepository, CommonStatus.Enabled, "启用", "验方不存在")
    {
    }

    protected override void ApplyStatusChange(Formula formula)
        => formula.ChangeStatus(TargetStatus, Guid.Empty);

    protected override string? GetEntityName(Formula formula) => formula.Name;
}
