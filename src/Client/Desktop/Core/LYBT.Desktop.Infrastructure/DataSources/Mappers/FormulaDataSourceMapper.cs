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
    /// <summary>
    /// FormulaDetailDto → Formula Entity
    /// </summary>
    public partial Formula ToEntity(FormulaDetailDto dto);

    /// <summary>
    /// FormulaListDto → Formula Entity（部分属性）
    /// </summary>
    public partial Formula ToEntity(FormulaListDto dto);

    /// <summary>
    /// FormulaInputDto → Formula Entity
    /// </summary>
    public partial Formula ToEntity(FormulaInputDto dto);

    /// <summary>
    /// Formula Entity → FormulaDetailDto
    /// </summary>
    public partial FormulaDetailDto ToDetailDto(Formula entity);

    /// <summary>
    /// Formula Entity → FormulaInputDto
    /// </summary>
    public partial FormulaInputDto ToInputDto(Formula entity);
}
