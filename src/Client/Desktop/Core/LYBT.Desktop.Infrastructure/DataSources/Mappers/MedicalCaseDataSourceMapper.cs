using LYBT.Entities.MedicalCases;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Infrastructure.DataSources.Mappers;

/// <summary>
/// MedicalCase DTO/Entity 双向映射器
/// OpenSpec: implement-local-mode
/// </summary>
[Mapper]
public partial class MedicalCaseDataSourceMapper
{
    /// <summary>
    /// MedicalCaseDetailDto → MedicalCase Entity
    /// </summary>
    public partial MedicalCase ToEntity(MedicalCaseDetailDto dto);

    /// <summary>
    /// MedicalCaseListDto → MedicalCase Entity（部分属性）
    /// </summary>
    public partial MedicalCase ToEntity(MedicalCaseListDto dto);

    /// <summary>
    /// MedicalCaseInputDto → MedicalCase Entity
    /// </summary>
    public partial MedicalCase ToEntity(MedicalCaseInputDto dto);

    /// <summary>
    /// MedicalCase Entity → MedicalCaseDetailDto
    /// </summary>
    public partial MedicalCaseDetailDto ToDetailDto(MedicalCase entity);

    /// <summary>
    /// MedicalCase Entity → MedicalCaseInputDto
    /// </summary>
    public partial MedicalCaseInputDto ToInputDto(MedicalCase entity);
}
