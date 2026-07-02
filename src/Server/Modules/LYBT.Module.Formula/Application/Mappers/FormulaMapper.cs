using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Module.Formulas.Domain;

namespace LYBT.Module.Formulas.Application.Mappers;

/// <summary>
/// 验方数据映射器。静态类，用于 Domain 实体与 DTO 之间的转换。
/// </summary>
public static class FormulaDtoMapper
{
    /// <summary>
    /// FormulaInputDto 转换为 Formula 实体（创建）。
    /// </summary>
    public static Formula ToEntity(FormulaInputDto dto, Guid? createdBy = null) => Formula.Create(
        dto.Name,
        dto.Effect,
        dto.Indications,
        dto.Usage,
        dto.Remark,
        dto.Property,
        dto.Category,
        formulaType: Shared.Models.Enums.FormulaType.Experience,
        dto.IsShared,
        userId: createdBy,
        createdBy);

    /// <summary>
    /// Formula 实体转换为 FormulaListDto（列表查询）。
    /// </summary>
    public static FormulaListDto ToListDto(Formula entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Effect = entity.Effect,
        Indications = entity.Indication,
        Category = entity.Category,
        IsShared = entity.IsShared,
        ValidationStatus = entity.ValidationStatus,
        Status = entity.Status,
        HerbCount = entity.HerbCount,
        TotalPrice = 0,
        CreatedAt = entity.CreatedAt
    };

    /// <summary>
    /// Formula 实体转换为 FormulaDetailDto（详情查询）。
    /// </summary>
    public static FormulaDetailDto ToDetailDto(Formula entity) => new()
    {
        Id = entity.Id,
        Name = entity.Name,
        Effect = entity.Effect,
        Indications = entity.Indication,
        Usage = entity.Usage,
        Remark = entity.Remark,
        Property = entity.Property,
        Status = entity.Status,
        IsShared = entity.IsShared,
        ValidationStatus = entity.ValidationStatus,
        Category = entity.Category ?? "验方",
        CreatedAt = entity.CreatedAt,
        UpdatedAt = entity.UpdatedAt,
        CreatedBy = entity.CreatedBy,
        HerbCount = entity.HerbCount,
        TotalPrice = 0,
        Herbs = entity.Herbs?.Select(ToHerbItemDto).ToList() ?? new List<FormulaHerbItemDto>()
    };

    /// <summary>
    /// FormulaHerbItem 实体转换为 FormulaHerbItemDto。
    /// </summary>
    public static FormulaHerbItemDto ToHerbItemDto(FormulaHerbItem entity) => new()
    {
        Id = entity.Id,
        HerbId = entity.HerbId,
        HerbName = entity.HerbName,
        Dosage = entity.Dosage,
        Unit = entity.Unit,
        Usage = entity.Usage,
        Preparation = entity.ProcessingMethod,
        ProcessingMethod = entity.ProcessingMethod,
        DecocteMethod = entity.DecocteMethod,
        OriginalHerbName = entity.OriginalHerbName,
        IsValidated = entity.IsValidated
    };
}


