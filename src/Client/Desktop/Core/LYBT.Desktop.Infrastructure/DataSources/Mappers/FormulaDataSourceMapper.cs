using LYBT.Entities.Formulas;
using LYBT.Shared.Models.Contracts.Formula;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Infrastructure.DataSources.Mappers;

/// <summary>
/// Formula DTO/Entity 双向映射器
/// OpenSpec: implement-local-mode
/// </summary>
[Mapper]
public partial class FormulaDataSourceMapper
{
    #region FormulaDetailDto ↔ Formula

    /// <summary>
    /// FormulaDetailDto → Formula Entity
    /// </summary>
    [MapperIgnoreSource(nameof(FormulaDetailDto.IsEnabled))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.Indications))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.Description))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.Source))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.Contraindications))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.HerbCount))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.TotalPrice))]
    [MapperIgnoreSource(nameof(FormulaDetailDto.HerbNames))]
    [MapperIgnoreTarget(nameof(Formula.Indication))]
    [MapperIgnoreTarget(nameof(Formula.FormulaType))]
    [MapperIgnoreTarget(nameof(Formula.UserId))]
    [MapperIgnoreTarget(nameof(Formula.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Formula.RowVersion))]
    [MapperIgnoreTarget(nameof(Formula.IsDeleted))]
    public partial Formula ToEntity(FormulaDetailDto dto);

    /// <summary>
    /// Formula Entity → FormulaDetailDto
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
    public partial FormulaDetailDto ToDetailDto(Formula entity);

    #endregion

    #region FormulaListDto → Formula

    /// <summary>
    /// FormulaListDto → Formula Entity（部分属性）
    /// </summary>
    [MapperIgnoreSource(nameof(FormulaListDto.Indications))]
    [MapperIgnoreSource(nameof(FormulaListDto.HerbCount))]
    [MapperIgnoreSource(nameof(FormulaListDto.TotalPrice))]
    [MapperIgnoreTarget(nameof(Formula.Indication))]
    [MapperIgnoreTarget(nameof(Formula.Usage))]
    [MapperIgnoreTarget(nameof(Formula.Remark))]
    [MapperIgnoreTarget(nameof(Formula.Property))]
    [MapperIgnoreTarget(nameof(Formula.FormulaType))]
    [MapperIgnoreTarget(nameof(Formula.UserId))]
    [MapperIgnoreTarget(nameof(Formula.Herbs))]
    [MapperIgnoreTarget(nameof(Formula.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Formula.CreatedBy))]
    [MapperIgnoreTarget(nameof(Formula.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Formula.RowVersion))]
    [MapperIgnoreTarget(nameof(Formula.IsDeleted))]
    public partial Formula ToEntity(FormulaListDto dto);

    #endregion

    #region FormulaInputDto ↔ Formula

    /// <summary>
    /// FormulaInputDto → Formula Entity
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

    /// <summary>
    /// Formula Entity → FormulaInputDto
    /// </summary>
    [MapperIgnoreSource(nameof(Formula.Indication))]
    [MapperIgnoreSource(nameof(Formula.Status))]
    [MapperIgnoreSource(nameof(Formula.ValidationStatus))]
    [MapperIgnoreSource(nameof(Formula.FormulaType))]
    [MapperIgnoreSource(nameof(Formula.UserId))]
    [MapperIgnoreSource(nameof(Formula.CreatedAt))]
    [MapperIgnoreSource(nameof(Formula.UpdatedAt))]
    [MapperIgnoreSource(nameof(Formula.CreatedBy))]
    [MapperIgnoreSource(nameof(Formula.UpdatedBy))]
    [MapperIgnoreSource(nameof(Formula.RowVersion))]
    [MapperIgnoreSource(nameof(Formula.IsDeleted))]
    [MapperIgnoreTarget(nameof(FormulaInputDto.Description))]
    [MapperIgnoreTarget(nameof(FormulaInputDto.Instructions))]
    [MapperIgnoreTarget(nameof(FormulaInputDto.Indications))]
    [MapperIgnoreTarget(nameof(FormulaInputDto.Contraindications))]
    [MapperIgnoreTarget(nameof(FormulaInputDto.Preparation))]
    public partial FormulaInputDto ToInputDto(Formula entity);

    #endregion

    #region FormulaHerbItem 映射

    /// <summary>
    /// FormulaHerbItemDto → FormulaHerbItem Entity
    /// </summary>
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.Preparation))]
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.Processing))]
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.Price))]
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.UnitPrice))]
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.SpecialInstructions))]
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.SortOrder))]
    [MapperIgnoreSource(nameof(FormulaHerbItemDto.Herb))]
    [MapperIgnoreTarget(nameof(FormulaHerbItem.FormulaId))]
    [MapperIgnoreTarget(nameof(FormulaHerbItem.Formula))]
    [MapperIgnoreTarget(nameof(FormulaHerbItem.Remark))]
    public partial FormulaHerbItem ToEntity(FormulaHerbItemDto dto);

    /// <summary>
    /// FormulaHerbItem Entity → FormulaHerbItemDto
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
    /// FormulaHerbItemInputDto → FormulaHerbItem Entity
    /// </summary>
    [MapperIgnoreSource(nameof(FormulaHerbItemInputDto.Preparation))]
    [MapperIgnoreSource(nameof(FormulaHerbItemInputDto.SortOrder))]
    [MapperIgnoreTarget(nameof(FormulaHerbItem.FormulaId))]
    [MapperIgnoreTarget(nameof(FormulaHerbItem.Formula))]
    [MapperIgnoreTarget(nameof(FormulaHerbItem.OriginalHerbName))]
    [MapperIgnoreTarget(nameof(FormulaHerbItem.IsValidated))]
    [MapperIgnoreTarget(nameof(FormulaHerbItem.Remark))]
    public partial FormulaHerbItem ToEntity(FormulaHerbItemInputDto dto);

    /// <summary>
    /// FormulaHerbItem Entity → FormulaHerbItemInputDto
    /// </summary>
    [MapperIgnoreSource(nameof(FormulaHerbItem.FormulaId))]
    [MapperIgnoreSource(nameof(FormulaHerbItem.Formula))]
    [MapperIgnoreSource(nameof(FormulaHerbItem.OriginalHerbName))]
    [MapperIgnoreSource(nameof(FormulaHerbItem.IsValidated))]
    [MapperIgnoreSource(nameof(FormulaHerbItem.Remark))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemInputDto.Preparation))]
    [MapperIgnoreTarget(nameof(FormulaHerbItemInputDto.SortOrder))]
    public partial FormulaHerbItemInputDto ToInputDto(FormulaHerbItem entity);

    #endregion
}
