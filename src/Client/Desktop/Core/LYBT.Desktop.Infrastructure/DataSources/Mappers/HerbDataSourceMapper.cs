using LYBT.Entities.Herbs;
using LYBT.Shared.Models.Contracts.Herbs;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Infrastructure.DataSources.Mappers;

/// <summary>
/// Herb DTO/Entity 双向映射器
/// OpenSpec: implement-local-mode
/// </summary>
[Mapper]
public partial class HerbDataSourceMapper
{
    #region HerbDetailDto ↔ Herb

    /// <summary>
    /// HerbDetailDto → Herb Entity
    /// </summary>
    [MapperIgnoreSource(nameof(HerbDetailDto.Properties))]
    [MapperIgnoreTarget(nameof(Herb.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Herb.RowVersion))]
    [MapperIgnoreTarget(nameof(Herb.IsDeleted))]
    public partial Herb ToEntity(HerbDetailDto dto);

    /// <summary>
    /// Herb Entity → HerbDetailDto
    /// </summary>
    [MapperIgnoreSource(nameof(Herb.UpdatedBy))]
    [MapperIgnoreSource(nameof(Herb.RowVersion))]
    [MapperIgnoreSource(nameof(Herb.IsDeleted))]
    [MapperIgnoreTarget(nameof(HerbDetailDto.Properties))]
    public partial HerbDetailDto ToDetailDto(Herb entity);

    #endregion

    #region HerbListDto → Herb

    /// <summary>
    /// HerbListDto → Herb Entity（部分属性）
    /// </summary>
    [MapperIgnoreTarget(nameof(Herb.CostPrice))]
    [MapperIgnoreTarget(nameof(Herb.Effect))]
    [MapperIgnoreTarget(nameof(Herb.Usage))]
    [MapperIgnoreTarget(nameof(Herb.Remark))]
    [MapperIgnoreTarget(nameof(Herb.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Herb.CreatedBy))]
    [MapperIgnoreTarget(nameof(Herb.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Herb.RowVersion))]
    [MapperIgnoreTarget(nameof(Herb.IsDeleted))]
    public partial Herb ToEntity(HerbListDto dto);

    #endregion

    #region HerbInputDto ↔ Herb

    /// <summary>
    /// HerbInputDto → Herb Entity
    /// </summary>
    [MapperIgnoreTarget(nameof(Herb.Status))]
    [MapperIgnoreTarget(nameof(Herb.CreatedAt))]
    [MapperIgnoreTarget(nameof(Herb.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Herb.CreatedBy))]
    [MapperIgnoreTarget(nameof(Herb.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Herb.RowVersion))]
    [MapperIgnoreTarget(nameof(Herb.IsDeleted))]
    public partial Herb ToEntity(HerbInputDto dto);

    /// <summary>
    /// Herb Entity → HerbInputDto
    /// </summary>
    [MapperIgnoreSource(nameof(Herb.Status))]
    [MapperIgnoreSource(nameof(Herb.CreatedAt))]
    [MapperIgnoreSource(nameof(Herb.UpdatedAt))]
    [MapperIgnoreSource(nameof(Herb.CreatedBy))]
    [MapperIgnoreSource(nameof(Herb.UpdatedBy))]
    [MapperIgnoreSource(nameof(Herb.RowVersion))]
    [MapperIgnoreSource(nameof(Herb.IsDeleted))]
    public partial HerbInputDto ToInputDto(Herb entity);

    #endregion
}
