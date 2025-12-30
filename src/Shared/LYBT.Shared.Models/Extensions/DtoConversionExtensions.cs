using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Shared.Models.Extensions;

/// <summary>
/// DTO转换扩展方法 - 响应DTO到输入DTO的转换
/// OpenSpec: unify-medicalcase-input-dto - 简化MedicalCaseInputDto转换
/// </summary>
public static class DtoConversionExtensions
{
    #region MedicalCase转换

    /// <summary>
    /// MedicalCaseDetailDto转换为MedicalCaseInputDto
    /// OpenSpec: simplify-medicalcase-dataflow - DoctorId→UserId, VisitDate/ConsultationDate删除
    /// OpenSpec: simplify-medicalcase-api - 包含Consultation和Prescription用于聚合保存
    /// </summary>
    public static MedicalCaseInputDto ToInputDto(this MedicalCaseDetailDto dto)
    {
        return new MedicalCaseInputDto
        {
            Id = dto.Id,
            PatientId = dto.PatientId,
            UserId = dto.UserId,
            // VisitDate/ConsultationDate已删除，使用CreatedAt代替
            Remark = dto.Remark,
            Consultation = dto.Consultation?.ToInputDto(),
            Prescription = dto.Prescription?.ToPrescriptionInputDto()
        };
    }

    #endregion

    #region Consultation转换

    /// <summary>
    /// ConsultationDetailDto转换为ConsultationInputDto
    /// </summary>
    public static ConsultationInputDto ToInputDto(this ConsultationDetailDto dto)
    {
        return new ConsultationInputDto
        {
            PresentIllness = dto.PresentIllness,
            TongueDiagnosis = dto.TongueDiagnosis,
            PulseDiagnosis = dto.PulseDiagnosis,
            TcmDiagnosis = dto.TcmDiagnosis
        };
    }

    #endregion

    #region Prescription转换

    /// <summary>
    /// PrescriptionDetailDto转换为PrescriptionInputDto（用于Repository更新）
    /// OpenSpec: simplify-medicalcase-dataflow - Diagnosis/Indication已删除
    /// </summary>
    public static PrescriptionInputDto ToPrescriptionInputDto(this PrescriptionDetailDto dto)
    {
        return new PrescriptionInputDto
        {
            Id = dto.Id,
            // Diagnosis/Indication已删除
            Advice = dto.Advice,
            ReferencedFormulas = dto.ReferencedFormulas,
            Remark = dto.Remark,
            Discount = dto.Discount,
            TotalPrice = dto.TotalPrice,
            DosageCount = dto.DosageCount,
            Usage = dto.Usage,
            Items = dto.Items?.Select(item => new PrescriptionItemInputDto
            {
                Id = item.Id,
                HerbId = item.HerbId,
                HerbName = item.HerbName,
                Unit = item.Unit,
                Dosage = item.Dosage,
                UnitPrice = item.UnitPrice,
                Subtotal = item.Subtotal,
                Usage = item.Usage,
                DecocteMethod = item.DecocteMethod,
                Remark = item.Remark
            }).ToList() ?? []
        };
    }

    #endregion
}
