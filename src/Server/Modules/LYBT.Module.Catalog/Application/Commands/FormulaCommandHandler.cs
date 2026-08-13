using LYBT.Entities.Formulas;
using LYBT.Entities.Herbs;
using LYBT.Module.Catalog.Application.Mappers;
using LYBT.Module.Catalog.Interfaces;
using Microsoft.Extensions.Logging;
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
    private readonly IHerbRepository _herbRepository;
    private readonly ILogger _logger;

    public FormulaCommandHandler(
        IFormulaRepository formulaRepository,
        IHerbRepository herbRepository,
        ILogger<FormulaCommandHandler> logger)
        : base(formulaRepository)
    {
        _herbRepository = herbRepository;
        _logger = logger;
    }

    protected override ErrorCode ValidationErrorCode => ErrorCode.FormulaValidationFailed; // 422（herbId 引用校验失败）

    protected override string EntityDisplayName => "方剂";

    protected override Formula CreateEntity(FormulaInputDto input, Guid currentUserId)
    {
        var formula = CatalogDtoMapper.ToEntity(input, currentUserId);
        // T5-2 #14 (US-FORM-003): 创建时持久化药材组成（原 Mapper 丢弃 Herbs）
        formula.ReplaceHerbs(MapHerbs(input, formula.Id));
        return formula;
    }

    /// <summary>
    /// 保存前引用校验（2026-08-13 真机缺口修复）: herbs 的 HerbId 必须存在且未删除——
    /// 空 herbs 由 Validator 拦截（400）；HerbId 不存在/已删除 → 明确错误（422）
    /// </summary>
    protected override async Task<string?> ValidateBeforeSaveAsync(FormulaInputDto input, CancellationToken ct)
    {
        if (input.Herbs == null || input.Herbs.Count == 0)
            return "验方必须包含至少一味中药材";

        var herbIds = input.Herbs.Where(h => h.HerbId.HasValue).Select(h => h.HerbId!.Value).Distinct().ToList();
        var existing = new List<Herb>();
        foreach (var herbId in herbIds)
        {
            var herb = await _herbRepository.GetByIdAsync(herbId, ct);
            if (herb != null)
                existing.Add(herb);
        }
        var missing = herbIds.Except(existing.Select(h => h.Id)).ToList();
        if (missing.Count > 0)
            return $"验方包含不存在的药材（ID: {string.Join(", ", missing)}）——请检查后重试";

        // 已删除药材（软删）视为不存在
        var deleted = existing.Where(h => h.IsDeleted).Select(h => h.Id).ToList();
        if (deleted.Count > 0)
            return $"验方包含已删除的药材（ID: {string.Join(", ", deleted)}）——请恢复药材或移除该组成";

        return null;
    }

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

        // T5-2 #14 (US-FORM-004): 更新时替换药材组成（原 UpdateProfile 丢弃 Herbs）
        entity.ReplaceHerbs(MapHerbs(input, entity.Id));
        // T5-2 #15 (US-FORM-010 FLAW-F1): Validated 验方更新后若任一药材未验证 → 降级 Draft
        // P1-3（US-LOG-000 2026-08-13）: 业务决策日志——降级发生时 Warning
        var wasValidated = entity.ValidationStatus == FormulaValidationStatus.Validated;
        entity.DegradeToDraftIfAnyHerbUnvalidated();
        if (wasValidated && entity.ValidationStatus != FormulaValidationStatus.Validated)
            _logger.LogWarning("[FORMULA] 验方 {Id} 更新后存在未验证药材——验证状态降级为 Draft（US-FORM-010）", entity.Id);
    }

    /// <summary>
    /// 将输入 DTO 药材映射为实体集合（T5-2 #14）。
    /// </summary>
    private static List<FormulaHerbItem> MapHerbs(FormulaInputDto input, Guid formulaId)
        => input.Herbs.Select(h => FormulaHerbItem.Create(
                formulaId,
                h.HerbName,
                h.Dosage,
                h.Unit,
                h.HerbId,
                h.HerbName,
                h.Preparation,
                null,
                h.ProcessingMethod,
                h.DecocteMethod))
            .ToList();

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
