using LYBT.Entities.Formulas;
using LYBT.Shared.Models.Contracts.Formula;
using Riok.Mapperly.Abstractions;

namespace LYBT.Module.Formulas.Application.Mappers;

/// <summary>
/// 验方数据映射器。Mapperly 编译时生成（A-18 P1-4 由手写静态类改造）。
/// 纯属性复制方法（ToListDto）由 Mapperly 生成；工厂/复杂逻辑方法保留手写（行为等价）。
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target, AutoUserMappings = false)]
public static partial class FormulaDtoMapper
{
    /// <summary>
    /// FormulaInputDto 转换为 Formula 实体（创建）。
    /// 保留手写：走领域工厂 Formula.Create（校验 + Trim + 审计字段），Mapperly 无法表达。
    /// 不带 [UserMapping] 标记：AutoUserMappings=false 下不被 Mapperly 发现（带额外参数签名不受支持）。
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
    /// Mapperly 生成：Indication→Indications 重命名；TotalPrice 无源字段忽略（默认 0 等价）。
    /// </summary>
    [MapProperty(nameof(Formula.Indication), nameof(FormulaListDto.Indications))]
    [MapperIgnoreTarget(nameof(FormulaListDto.TotalPrice))]
    public static partial FormulaListDto ToListDto(Formula entity);

    /// <summary>
    /// Formula 实体转换为 FormulaDetailDto（详情查询）。
    /// 保留手写：Category 空值回退"验方"（DTO getter 兜底不一致）、TotalPrice 恒 0、Herbs 嵌套映射。
    /// </summary>
    [UserMapping(Default = false)]
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
    /// 保留手写：Preparation/Processing 属性名映射 Mapperly 无法自动推断。
    /// </summary>
    [UserMapping(Default = false)]
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
