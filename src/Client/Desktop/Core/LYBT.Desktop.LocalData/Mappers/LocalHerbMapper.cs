using LYBT.Entities.Herbs;
using LYBT.Shared.Models.Contracts.Herbs;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.LocalData.Mappers;

/// <summary>
/// LocalData 药材映射器 - Entity <-> DTO 转换
/// </summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Both)]
internal partial class LocalHerbMapper
{
    /// <summary>
    /// Herb Entity -> HerbDetailDto
    /// </summary>
    [MapperIgnoreSource(nameof(Herb.UpdatedBy))]
    [MapperIgnoreSource(nameof(Herb.RowVersion))]
    [MapperIgnoreSource(nameof(Herb.IsDeleted))]
    public partial HerbDetailDto ToDetailDto(Herb entity);

    /// <summary>
    /// HerbInputDto -> Herb Entity
    /// </summary>
    [MapperIgnoreTarget(nameof(Herb.Status))]
    [MapperIgnoreTarget(nameof(Herb.CreatedAt))]
    [MapperIgnoreTarget(nameof(Herb.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Herb.CreatedBy))]
    [MapperIgnoreTarget(nameof(Herb.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Herb.RowVersion))]
    [MapperIgnoreTarget(nameof(Herb.IsDeleted))]
    public partial Herb ToEntity(HerbInputDto dto);
}
