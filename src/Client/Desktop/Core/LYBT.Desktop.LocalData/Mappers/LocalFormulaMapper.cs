using LYBT.Entities.Formulas;
using LYBT.Shared.Models.Contracts.Formula;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.LocalData.Mappers;

/// <summary>
/// LocalData 验方映射器 - Entity <-> DTO 转换
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Both)]
internal partial class LocalFormulaMapper
{
    #region Formula Entity <-> DTO

    /// <summary>
    /// Formula Entity -> FormulaDetailDto
    /// </summary>
    [MapperIgnoreSource(nameof(Formula.Indication))]
    [MapperIgnoreSource(nameof(Formula.FormulaType))]
    [MapperIgnoreSource(nameof(Formula.UserId))]
    [MapperIgnoreSource(nameof(Formula.UpdatedBy))]
    [MapperIgnoreSource(nameof(Formula.RowVersion))]
    [MapperIgnoreSource(nameof(Formula.IsDeleted))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.IsEnabled))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Indications))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Description))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Source))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.Contraindications))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.HerbCount))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.TotalPrice))]
    [MapperIgnoreTarget(nameof(FormulaDetailDto.HerbNames))]
    public partial FormulaDetailDto ToDetailDtoCore(Formula entity);

    /// <summary>
    /// 包装方法: 补充计算属性
    /// </summary>
    public FormulaDetailDto ToDetailDto(Formula entity)
    {
        var dto = ToDetailDtoCore(entity);
        dto.HerbCount = entity.Herbs?.Count ?? 0;
        dto.TotalPrice = 0; // 总价格由 Service 层计算
        return dto;
    }

    /// <summary>
    /// FormulaInputDto -> Formula Entity
    /// </summary>
    [MapperIgnoreSource(nameof(FormulaInputDto.Description))]
    [MapperIgnoreSource(nameof(FormulaInputDto.Instructions))]
    [MapperIgnoreSource(nameof(FormulaInputDto.Indications))]
    [MapperIgnoreSource(nameof(FormulaInputDto.Contraindications))]
    [MapperIgnoreSource(nameof(FormulaInputDto.Preparation))]
    [MapperIgnoreTarget(nameof(Formula.Indication))]
    [MapperIgnoreTarget(nameof(Formula.Status))]
    [MapperIgnoreTarget(nameof(Formula.ValidationStatus))]
    [MapperIgnoreTarget(nameof(Formula.FormulaType))]
    [MapperIgnoreTarget(nameof(Formula.UserId))]
    [MapperIgnoreTarget(nameof(Formula.CreatedAt))]
    [MapperIgnoreTarget(nameof(Formula.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Formula.CreatedBy))]
    [MapperIgnoreTarget(nameof(Formula.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Formula.RowVersion))]
    [MapperIgnoreTarget(nameof(Formula.IsDeleted))]
    public partial Formula ToEntity(FormulaInputDto dto);

    #endregion

    #region FormulaHerbItem 映射

    /// <summary>
    /// FormulaHerbItem Entity -> FormulaHerbItemDto
    /// </summary>
    [MapperIgnoreSource(nameof(FormulaHerbItem.FormulaId))]
    [MapperIgnoreSource(nameof(FormulaHerbItem.Formula))]
    [MapperIgnoreSource(nameof(FormulaHerbItem.Remark))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.Preparation))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.Processing))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.Price))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.UnitPrice))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.SpecialInstructions))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.SortOrder))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemDto.Herb))]
    public partial FormulaHerbItemDto ToDto(FormulaHerbItem entity);

    /// <summary>
    /// FormulaHerbItemInputDto -> FormulaHerbItem Entity
    /// </summary>
    [MapperIgnoreSource(nameof(FormulaHerbItemInputDto.Preparation))]
    [MapperIgnoreSource(nameof(FormulaHerbItemInputDto.SortOrder))]
    [MapperIgnoreTarget(nameof(FormulaHerbItem.FormulaId))]
    [MapperIgnoreTarget(nameof(FormulaHerbItem.Formula))]
    [MapperIgnoreTarget(nameof(FormulaHerbItem.OriginalHerbName))]
    [MapperIgnoreTarget(nameof(FormulaHerbItem.IsValidated))]
    [MapperIgnoreTarget(nameof(FormulaHerbItem.Remark))]
    public partial FormulaHerbItem ToEntity(FormulaHerbItemInputDto dto);

    #endregion
}
