using LYBT.Entities.Consultations;
using LYBT.Entities.MedicalCases;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Riok.Mapperly.Abstractions;

namespace LYBT.Desktop.LocalData.Mappers;

/// <summary>
/// LocalData 医案映射器 - Entity <-> DTO 转换
/// 处理聚合根 (MedicalCase + Consultation + Prescription) 的映射
/// </summary>
[Mapper]
internal partial class LocalMedicalCaseMapper
{
    #region MedicalCase Entity -> MedicalCaseDetailDto

    /// <summary>
    /// MedicalCase Entity -> MedicalCaseDetailDto (核心映射)
    /// </summary>
    [MapperIgnoreSource(nameof(MedicalCase.Consultation))]
    [MapperIgnoreSource(nameof(MedicalCase.Prescription))]
    [MapperIgnoreSource(nameof(MedicalCase.NeedsPrescription))]
    [MapperIgnoreSource(nameof(MedicalCase.IsLocked))]
    [MapperIgnoreSource(nameof(MedicalCase.IsActive))]
    [MapperIgnoreSource(nameof(MedicalCase.IsCompleted))]
    [MapperIgnoreSource(nameof(MedicalCase.PrintLogs))]
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
    public partial MedicalCaseDetailDto ToDetailDtoCore(MedicalCase entity);

    /// <summary>
    /// 包装方法: 补充聚合子实体和计算属性
    /// </summary>
    public MedicalCaseDetailDto ToDetailDto(MedicalCase entity)
    {
        var dto = ToDetailDtoCore(entity);

        // 补充 Consultation 嵌套 DTO
        if (entity.Consultation != null)
        {
            dto.ConsultationId = entity.Consultation.Id;
            dto.Diagnosis = entity.Consultation.TcmDiagnosis;
            dto.PresentIllness = entity.Consultation.PresentIllness;
            dto.Consultation = ToConsultationDetailDto(entity.Consultation, entity);
        }

        // 补充 Prescription 嵌套 DTO
        if (entity.Prescription != null)
        {
            dto.PrescriptionId = entity.Prescription.Id;
            dto.Prescription = ToPrescriptionDetailDto(entity.Prescription);
        }

        return dto;
    }

    #endregion

    #region Consultation Entity -> ConsultationDetailDto

    /// <summary>
    /// Consultation Entity -> ConsultationDetailDto
    /// 需要从 MedicalCase 父实体获取 PatientId/UserId 等关联字段
    /// </summary>
    private ConsultationDetailDto ToConsultationDetailDto(Consultation consultation, MedicalCase parent)
    {
        return new ConsultationDetailDto
        {
            Id = consultation.Id,
            CreatedAt = consultation.CreatedAt,
            UpdatedAt = consultation.UpdatedAt,
            CreatedBy = consultation.CreatedBy,
            MedicalCaseId = parent.Id,
            PatientId = parent.PatientId,
            UserId = parent.UserId,
            PatientName = parent.PatientName,
            DoctorName = parent.DoctorName,
            PresentIllness = consultation.PresentIllness,
            TongueDiagnosis = consultation.TongueDiagnosis,
            PulseDiagnosis = consultation.PulseDiagnosis,
            TcmDiagnosis = consultation.TcmDiagnosis
        };
    }

    #endregion

    #region Prescription Entity -> PrescriptionDetailDto

    /// <summary>
    /// Prescription Entity -> PrescriptionDetailDto (核心映射)
    /// </summary>
    [MapperIgnoreSource(nameof(Prescription.UpdatedBy))]
    [MapperIgnoreSource(nameof(Prescription.RowVersion))]
    [MapperIgnoreSource(nameof(Prescription.IsDeleted))]
    [MapperIgnoreSource(nameof(Prescription.CreatedBy))]
    // T2-X8-09: Prescription 打印字段已移除，IgnoreSource 不再需要
    [MapperIgnoreTarget(nameof(PrescriptionDetailDto.SingleDosePrice))]
    [MapperIgnoreTarget(nameof(PrescriptionDetailDto.TotalPrice))]
    [MapperIgnoreTarget(nameof(PrescriptionDetailDto.TotalWeight))]
    [MapperIgnoreTarget(nameof(PrescriptionDetailDto.Status))]
    [MapperIgnoreTarget(nameof(PrescriptionDetailDto.DuplicateWarning))]
    [MapperIgnoreTarget(nameof(PrescriptionDetailDto.MissingDrugWarning))]
    private partial PrescriptionDetailDto ToPrescriptionDetailDto(Prescription entity);

    /// <summary>
    /// PrescriptionItem Entity -> PrescriptionItemDto
    /// </summary>
    [MapperIgnoreSource(nameof(PrescriptionItem.PrescriptionId))]
    [MapProperty(nameof(PrescriptionItem.Amount), nameof(PrescriptionItemDto.Subtotal))]
    [MapperIgnoreTarget(nameof(PrescriptionItemDto.TotalPrice))]
    [MapperIgnoreTarget(nameof(PrescriptionItemDto.TotalWeight))]
    [MapperIgnoreTarget(nameof(PrescriptionItemDto.Notes))]
    private partial PrescriptionItemDto ToPrescriptionItemDto(PrescriptionItem entity);

    #endregion

    #region MedicalCaseInputDto -> MedicalCase Entity

    /// <summary>
    /// MedicalCaseInputDto -> MedicalCase Entity
    /// </summary>
    [MapperIgnoreSource(nameof(MedicalCaseInputDto.EditReason))]
    [MapperIgnoreSource(nameof(MedicalCaseInputDto.Consultation))]
    [MapperIgnoreSource(nameof(MedicalCaseInputDto.Prescription))]
    [MapperIgnoreSource(nameof(MedicalCaseInputDto.RegistrationId))]
    [MapperIgnoreTarget(nameof(MedicalCase.PatientName))]
    [MapperIgnoreTarget(nameof(MedicalCase.DoctorName))]
    [MapperIgnoreTarget(nameof(MedicalCase.CaseNumber))]
    [MapperIgnoreTarget(nameof(MedicalCase.CaseStatus))]
    [MapperIgnoreTarget(nameof(MedicalCase.CompletedAt))]
    [MapperIgnoreTarget(nameof(MedicalCase.Consultation))]
    [MapperIgnoreTarget(nameof(MedicalCase.Prescription))]
    [MapperIgnoreTarget(nameof(MedicalCase.PrintLogs))]
    [MapperIgnoreTarget(nameof(MedicalCase.PrintVersion))]
    [MapperIgnoreTarget(nameof(MedicalCase.LastPrintedAt))]
    [MapperIgnoreTarget(nameof(MedicalCase.PrintCount))]
    [MapperIgnoreTarget(nameof(MedicalCase.IsPrinted))]
    [MapperIgnoreTarget(nameof(MedicalCase.CreatedAt))]
    [MapperIgnoreTarget(nameof(MedicalCase.UpdatedAt))]
    [MapperIgnoreTarget(nameof(MedicalCase.CreatedBy))]
    [MapperIgnoreTarget(nameof(MedicalCase.UpdatedBy))]
    [MapperIgnoreTarget(nameof(MedicalCase.RowVersion))]
    [MapperIgnoreTarget(nameof(MedicalCase.IsDeleted))]
    public partial MedicalCase ToEntity(MedicalCaseInputDto dto);

    #endregion
}
