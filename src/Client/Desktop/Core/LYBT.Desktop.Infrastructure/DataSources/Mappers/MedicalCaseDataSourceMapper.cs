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
    #region MedicalCaseDetailDto ↔ MedicalCase

    /// <summary>
    /// MedicalCaseDetailDto → MedicalCase Entity
    /// </summary>
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.PatientGender))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.PatientAge))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.ConsultationId))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.PrescriptionId))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.Diagnosis))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.HasConsultation))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.HasPrescription))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.PresentIllness))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.Consultation))]
    [MapperIgnoreSource(nameof(MedicalCaseDetailDto.Prescription))]
    [MapperIgnoreTarget(nameof(MedicalCase.Consultation))]
    [MapperIgnoreTarget(nameof(MedicalCase.Prescription))]
    [MapperIgnoreTarget(nameof(MedicalCase.NeedsPrescription))]
    [MapperIgnoreTarget(nameof(MedicalCase.UpdatedBy))]
    [MapperIgnoreTarget(nameof(MedicalCase.RowVersion))]
    [MapperIgnoreTarget(nameof(MedicalCase.IsDeleted))]
    public partial MedicalCase ToEntity(MedicalCaseDetailDto dto);

    /// <summary>
    /// MedicalCase Entity → MedicalCaseDetailDto
    /// </summary>
    [MapperIgnoreSource(nameof(MedicalCase.Consultation))]
    [MapperIgnoreSource(nameof(MedicalCase.Prescription))]
    [MapperIgnoreSource(nameof(MedicalCase.NeedsPrescription))]
    [MapperIgnoreSource(nameof(MedicalCase.IsLocked))]
    [MapperIgnoreSource(nameof(MedicalCase.IsActive))]
    [MapperIgnoreSource(nameof(MedicalCase.IsCompleted))]
    [MapperIgnoreSource(nameof(MedicalCase.UpdatedBy))]
    [MapperIgnoreSource(nameof(MedicalCase.RowVersion))]
    [MapperIgnoreSource(nameof(MedicalCase.IsDeleted))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.PatientGender))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.PatientAge))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.ConsultationId))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.PrescriptionId))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.Diagnosis))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.HasConsultation))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.HasPrescription))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.PresentIllness))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.Consultation))]
    [MapperIgnoreTarget(nameof(MedicalCaseDetailDto.Prescription))]
    public partial MedicalCaseDetailDto ToDetailDto(MedicalCase entity);

    #endregion

    #region MedicalCaseListDto → MedicalCase

    /// <summary>
    /// MedicalCaseListDto → MedicalCase Entity（部分属性）
    /// </summary>
    [MapperIgnoreSource(nameof(MedicalCaseListDto.PatientGender))]
    [MapperIgnoreSource(nameof(MedicalCaseListDto.PatientAge))]
    [MapperIgnoreSource(nameof(MedicalCaseListDto.Diagnosis))]
    [MapperIgnoreSource(nameof(MedicalCaseListDto.HasConsultation))]
    [MapperIgnoreSource(nameof(MedicalCaseListDto.HasPrescription))]
    [MapperIgnoreSource(nameof(MedicalCaseListDto.CompletedAt))]
    [MapperIgnoreTarget(nameof(MedicalCase.NeedsPrescription))]
    [MapperIgnoreTarget(nameof(MedicalCase.CompletedAt))]
    [MapperIgnoreTarget(nameof(MedicalCase.Remark))]
    [MapperIgnoreTarget(nameof(MedicalCase.Consultation))]
    [MapperIgnoreTarget(nameof(MedicalCase.Prescription))]
    [MapperIgnoreTarget(nameof(MedicalCase.UpdatedAt))]
    [MapperIgnoreTarget(nameof(MedicalCase.CreatedBy))]
    [MapperIgnoreTarget(nameof(MedicalCase.UpdatedBy))]
    [MapperIgnoreTarget(nameof(MedicalCase.RowVersion))]
    [MapperIgnoreTarget(nameof(MedicalCase.IsDeleted))]
    public partial MedicalCase ToEntity(MedicalCaseListDto dto);

    #endregion

    #region MedicalCaseInputDto ↔ MedicalCase

    /// <summary>
    /// MedicalCaseInputDto → MedicalCase Entity
    /// </summary>
    [MapperIgnoreSource(nameof(MedicalCaseInputDto.EditReason))]
    [MapperIgnoreSource(nameof(MedicalCaseInputDto.Consultation))]
    [MapperIgnoreSource(nameof(MedicalCaseInputDto.Prescription))]
    [MapperIgnoreTarget(nameof(MedicalCase.PatientName))]
    [MapperIgnoreTarget(nameof(MedicalCase.DoctorName))]
    [MapperIgnoreTarget(nameof(MedicalCase.CaseNumber))]
    [MapperIgnoreTarget(nameof(MedicalCase.CaseStatus))]
    [MapperIgnoreTarget(nameof(MedicalCase.CompletedAt))]
    [MapperIgnoreTarget(nameof(MedicalCase.Consultation))]
    [MapperIgnoreTarget(nameof(MedicalCase.Prescription))]
    [MapperIgnoreTarget(nameof(MedicalCase.CreatedAt))]
    [MapperIgnoreTarget(nameof(MedicalCase.UpdatedAt))]
    [MapperIgnoreTarget(nameof(MedicalCase.CreatedBy))]
    [MapperIgnoreTarget(nameof(MedicalCase.UpdatedBy))]
    [MapperIgnoreTarget(nameof(MedicalCase.RowVersion))]
    [MapperIgnoreTarget(nameof(MedicalCase.IsDeleted))]
    public partial MedicalCase ToEntity(MedicalCaseInputDto dto);

    /// <summary>
    /// MedicalCase Entity → MedicalCaseInputDto
    /// </summary>
    [MapperIgnoreSource(nameof(MedicalCase.PatientName))]
    [MapperIgnoreSource(nameof(MedicalCase.DoctorName))]
    [MapperIgnoreSource(nameof(MedicalCase.CaseNumber))]
    [MapperIgnoreSource(nameof(MedicalCase.CaseStatus))]
    [MapperIgnoreSource(nameof(MedicalCase.CompletedAt))]
    [MapperIgnoreSource(nameof(MedicalCase.Consultation))]
    [MapperIgnoreSource(nameof(MedicalCase.Prescription))]
    [MapperIgnoreSource(nameof(MedicalCase.IsLocked))]
    [MapperIgnoreSource(nameof(MedicalCase.IsActive))]
    [MapperIgnoreSource(nameof(MedicalCase.IsCompleted))]
    [MapperIgnoreSource(nameof(MedicalCase.CreatedAt))]
    [MapperIgnoreSource(nameof(MedicalCase.UpdatedAt))]
    [MapperIgnoreSource(nameof(MedicalCase.CreatedBy))]
    [MapperIgnoreSource(nameof(MedicalCase.UpdatedBy))]
    [MapperIgnoreSource(nameof(MedicalCase.RowVersion))]
    [MapperIgnoreSource(nameof(MedicalCase.IsDeleted))]
    [MapperIgnoreTarget(nameof(MedicalCaseInputDto.EditReason))]
    [MapperIgnoreTarget(nameof(MedicalCaseInputDto.Consultation))]
    [MapperIgnoreTarget(nameof(MedicalCaseInputDto.Prescription))]
    public partial MedicalCaseInputDto ToInputDto(MedicalCase entity);

    #endregion
}
