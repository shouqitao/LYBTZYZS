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
    /// <summary>
    /// HerbDetailDto → Herb Entity
    /// </summary>
    public partial Herb ToEntity(HerbDetailDto dto);

    /// <summary>
    /// HerbListDto → Herb Entity（部分属性）
    /// </summary>
    public partial Herb ToEntity(HerbListDto dto);

    /// <summary>
    /// HerbInputDto → Herb Entity
    /// </summary>
    public partial Herb ToEntity(HerbInputDto dto);

    /// <summary>
    /// Herb Entity → HerbDetailDto
    /// </summary>
    public partial HerbDetailDto ToDetailDto(Herb entity);

    /// <summary>
    /// Herb Entity → HerbInputDto
    /// </summary>
    public partial HerbInputDto ToInputDto(Herb entity);
}
