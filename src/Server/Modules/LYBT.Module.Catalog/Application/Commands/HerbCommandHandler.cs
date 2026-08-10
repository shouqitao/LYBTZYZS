using LYBT.Entities.Herbs;
using LYBT.Module.Catalog.Application.Mappers;
using LYBT.Module.Catalog.Interfaces;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Primitives.ErrorCodes;

namespace LYBT.Module.Catalog.Application.Commands;

/// <summary>
/// 药材命令处理器（A-31-C3b 合并 Create/Update/Delete/Restore/Toggle 五个同构 Handler）。
/// 骨架收敛至 <see cref="CatalogEntityCommandHandlerBase{TEntity,TInput,TDetail}"/>，本类仅提供药材差异点。
/// </summary>
public class HerbCommandHandler : CatalogEntityCommandHandlerBase<Herb, HerbInputDto, HerbDetailDto>
{
    public HerbCommandHandler(IHerbRepository herbRepository)
        : base(herbRepository)
    {
    }

    protected override string EntityDisplayName => "药材";

    protected override Herb CreateEntity(HerbInputDto input, Guid currentUserId)
        => CatalogDtoMapper.ToEntity(input, currentUserId);

    protected override void ApplyUpdate(Herb entity, HerbInputDto input, Guid currentUserId)
    {
        entity.UpdateProfile(
            input.Name,
            input.Unit,
            input.Price,
            input.PinYinCode,
            input.Category,
            input.Properties,
            input.Origin,
            input.Spec,
            input.CostPrice,
            input.Effect,
            input.Usage,
            input.Remark,
            currentUserId);
    }

    protected override HerbDetailDto ToDetailDto(Herb entity)
        => CatalogDtoMapper.ToHerbDetailDto(entity);

    protected override void ApplySoftDelete(Herb entity, Guid operatorId)
        => entity.SoftDelete(operatorId);

    protected override void ApplyRestore(Herb entity, Guid operatorId)
        => entity.Restore(operatorId);

    protected override void ApplyToggleStatus(Herb entity, Guid operatorId)
        => entity.ChangeStatus(
            entity.Status == CommonStatus.Enabled ? CommonStatus.Disabled : CommonStatus.Enabled,
            operatorId);

    protected override ErrorCode NameExistsErrorCode => ErrorCode.HerbNameExists;
    protected override ErrorCode NotFoundErrorCode => ErrorCode.HerbNotFound;
    protected override ErrorCode NotDeletedErrorCode => ErrorCode.HerbNotDeleted;

    protected override string GetName(Herb entity) => entity.Name;
    protected override string GetName(HerbInputDto input) => input.Name;
}
