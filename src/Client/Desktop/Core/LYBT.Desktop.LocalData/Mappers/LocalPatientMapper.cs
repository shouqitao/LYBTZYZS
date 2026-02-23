using LYBT.Entities.Patients;
using LYBT.Shared.Models.Contracts.Patients;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.LocalData.Mappers;

/// <summary>
/// LocalData 患者映射器 - Entity <-> DTO 转换
/// </summary>
[Mapper]
internal partial class LocalPatientMapper
{
    /// <summary>
    /// Patient Entity -> PatientDetailDto
    /// </summary>
    [MapperIgnoreSource(nameof(Patient.UpdatedBy))]
    [MapperIgnoreSource(nameof(Patient.RowVersion))]
    [MapperIgnoreSource(nameof(Patient.IsDeleted))]
    public partial PatientDetailDto ToDetailDto(Patient entity);

    /// <summary>
    /// PatientInputDto -> Patient Entity
    /// </summary>
    [MapperIgnoreTarget(nameof(Patient.CreatedAt))]
    [MapperIgnoreTarget(nameof(Patient.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Patient.CreatedBy))]
    [MapperIgnoreTarget(nameof(Patient.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Patient.RowVersion))]
    [MapperIgnoreTarget(nameof(Patient.IsDeleted))]
    [MapperIgnoreTarget(nameof(Patient.Status))]
    [MapperIgnoreTarget(nameof(Patient.DisableReason))]
    [MapperIgnoreTarget(nameof(Patient.LastVisitTime))]
    [MapperIgnoreTarget(nameof(Patient.VisitCount))]
    public partial Patient ToEntity(PatientInputDto dto);
}
