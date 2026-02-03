using LYBT.Entities.Patients;
using LYBT.Shared.Models.Contracts.Patients;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.Infrastructure.DataSources.Mappers;

/// <summary>
/// 患者数据源映射器 - DTO ↔ Entity
/// OpenSpec: implement-local-mode
/// 使用 Mapperly 编译时映射
/// </summary>
[Mapper]
public partial class PatientDataSourceMapper
{
    /// <summary>
    /// PatientDetailDto → Patient Entity
    /// </summary>
    public partial Patient ToEntity(PatientDetailDto dto);

    /// <summary>
    /// PatientListDto → Patient Entity（部分属性）
    /// </summary>
    public partial Patient ToEntity(PatientListDto dto);

    /// <summary>
    /// PatientInputDto → Patient Entity
    /// </summary>
    public partial Patient ToEntity(PatientInputDto dto);

    /// <summary>
    /// Patient Entity → PatientDetailDto
    /// </summary>
    public partial PatientDetailDto ToDetailDto(Patient entity);

    /// <summary>
    /// Patient Entity → PatientInputDto
    /// </summary>
    public partial PatientInputDto ToInputDto(Patient entity);
}
