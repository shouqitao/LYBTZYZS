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
    #region PatientDetailDto ↔ Patient

    /// <summary>
    /// PatientDetailDto → Patient Entity
    /// </summary>
    [MapperIgnoreTarget(nameof(Patient.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Patient.RowVersion))]
    [MapperIgnoreTarget(nameof(Patient.IsDeleted))]
    public partial Patient ToEntity(PatientDetailDto dto);

    /// <summary>
    /// Patient Entity → PatientDetailDto
    /// </summary>
    [MapperIgnoreSource(nameof(Patient.UpdatedBy))]
    [MapperIgnoreSource(nameof(Patient.RowVersion))]
    [MapperIgnoreSource(nameof(Patient.IsDeleted))]
    public partial PatientDetailDto ToDetailDto(Patient entity);

    #endregion

    #region PatientListDto → Patient

    /// <summary>
    /// PatientListDto → Patient Entity（部分属性）
    /// </summary>
    [MapperIgnoreTarget(nameof(Patient.MaritalStatus))]
    [MapperIgnoreTarget(nameof(Patient.IdType))]
    [MapperIgnoreTarget(nameof(Patient.BloodType))]
    [MapperIgnoreTarget(nameof(Patient.IdNumber))]
    [MapperIgnoreTarget(nameof(Patient.AllergyHistory))]
    [MapperIgnoreTarget(nameof(Patient.MedicalHistory))]
    [MapperIgnoreTarget(nameof(Patient.EmergencyContactName))]
    [MapperIgnoreTarget(nameof(Patient.EmergencyContactPhone))]
    [MapperIgnoreTarget(nameof(Patient.EmergencyContactRelation))]
    [MapperIgnoreTarget(nameof(Patient.DisableReason))]
    [MapperIgnoreTarget(nameof(Patient.BirthDate))]
    [MapperIgnoreTarget(nameof(Patient.UpdatedAt))]
    [MapperIgnoreTarget(nameof(Patient.CreatedBy))]
    [MapperIgnoreTarget(nameof(Patient.UpdatedBy))]
    [MapperIgnoreTarget(nameof(Patient.RowVersion))]
    [MapperIgnoreTarget(nameof(Patient.IsDeleted))]
    public partial Patient ToEntity(PatientListDto dto);

    #endregion

    #region PatientInputDto ↔ Patient

    /// <summary>
    /// PatientInputDto → Patient Entity
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

    /// <summary>
    /// Patient Entity → PatientInputDto
    /// </summary>
    [MapperIgnoreSource(nameof(Patient.Age))]
    [MapperIgnoreSource(nameof(Patient.CreatedAt))]
    [MapperIgnoreSource(nameof(Patient.UpdatedAt))]
    [MapperIgnoreSource(nameof(Patient.CreatedBy))]
    [MapperIgnoreSource(nameof(Patient.UpdatedBy))]
    [MapperIgnoreSource(nameof(Patient.RowVersion))]
    [MapperIgnoreSource(nameof(Patient.IsDeleted))]
    [MapperIgnoreSource(nameof(Patient.Status))]
    [MapperIgnoreSource(nameof(Patient.DisableReason))]
    [MapperIgnoreSource(nameof(Patient.LastVisitTime))]
    [MapperIgnoreSource(nameof(Patient.VisitCount))]
    public partial PatientInputDto ToInputDto(Patient entity);

    #endregion
}
