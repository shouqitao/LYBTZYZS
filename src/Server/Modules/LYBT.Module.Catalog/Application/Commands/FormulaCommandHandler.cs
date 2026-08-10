using LYBT.Entities.Formulas;
using LYBT.Module.Catalog.Application.Mappers;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 验方命令处理器（A-31-C3b 合并 Create/Update/Delete/Restore/Toggle 五个同构 Handler）。
/// 骨架收敛至 <see cref="CatalogEntityCommandHandlerBase{TEntity,TInput,TDetail}"/>，本类仅提供验方差异点。
/// 注意：Restore 的实体不存在消息为字面量「验方不存在」（与原实现一致，区别于 ErrorMessages.Get）。
/// </summary>
public class FormulaCommandHandler : CatalogEntityCommandHandlerBase<Formula, FormulaInputDto, FormulaDetailDto>
{
    public FormulaCommandHandler(IFormulaRepository formulaRepository)
        : base(formulaRepository)
    {
    }

    protected override string EntityDisplayName => "方剂";

    protected override Formula CreateEntity(FormulaInputDto input, Guid currentUserId)
        => CatalogDtoMapper.ToEntity(input, currentUserId);

    protected override void ApplyUpdate(Formula entity, FormulaInputDto input, Guid currentUserId)
    {
        entity.UpdateProfile(
            input.Name,
            input.Effect,
            input.Indication,
            input.Usage,
            input.Remark,
            input.Property,
            input.Category,
            input.IsShared,
            currentUserId);
    }

    protected override FormulaDetailDto ToDetailDto(Formula entity)
        => CatalogDtoMapper.ToFormulaDetailDto(entity);

    protected override void ApplySoftDelete(Formula entity, Guid operatorId)
        => entity.SoftDelete(operatorId);

    protected override void ApplyRestore(Formula entity, Guid operatorId)
        => entity.Restore(operatorId);

    protected override void ApplyToggleStatus(Formula entity, Guid operatorId)
        => entity.ChangeStatus(
            entity.Status == CommonStatus.Enabled ? CommonStatus.Disabled : CommonStatus.Enabled,
            operatorId);

    protected override ErrorCode NameExistsErrorCode => ErrorCode.FormulaNameExists;
    protected override ErrorCode NotFoundErrorCode => ErrorCode.FormulaNotFound;
    protected override ErrorCode NotDeletedErrorCode => ErrorCode.FormulaNotDeleted;

    protected override string RestoreNotFoundMessage => "验方不存在";

    protected override string RestoreNameConflictMessage(string name) => $"验方名称「{name}」已存在，无法恢复";

    protected override string GetName(Formula entity) => entity.Name;
    protected override string GetName(FormulaInputDto input) => input.Name;
}
