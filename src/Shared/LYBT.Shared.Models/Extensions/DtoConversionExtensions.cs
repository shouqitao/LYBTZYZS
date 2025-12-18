using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Shared.Models.Extensions;

/// <summary>
/// DTO转换扩展方法 - 响应DTO到输入DTO的转换
/// OpenSpec: refactor-dto-simplification - 保留必要的转换方法
/// </summary>
public static class DtoConversionExtensions
{
    #region MedicalCase转换

    /// <summary>
    /// MedicalCaseDetailDto转换为MedicalCaseInputDto
    /// </summary>
    public static MedicalCaseInputDto ToInputDto(this MedicalCaseDetailDto dto)
    {
        var inputDto = new MedicalCaseInputDto
        {
            Id = dto.Id,
            PatientId = dto.PatientId,
            DoctorId = dto.DoctorId,
            VisitDate = dto.ConsultationDate,
            Remark = dto.Remark
        };
        inputDto.PresentIllnessHistory = dto.PresentIllness;
        return inputDto;
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
            TCMDiagnosis = dto.TCMDiagnosis
        };
    }

    #endregion

    #region Prescription转换

    /// <summary>
    /// PrescriptionDetailDto转换为PrescriptionUpdateDto（用于Repository更新）
    /// </summary>
    public static PrescriptionUpdateDto ToPrescriptionUpdateDto(this PrescriptionDetailDto dto)
    {
        return new PrescriptionUpdateDto
        {
            PrescriptionNumber = dto.PrescriptionNumber,
            Diagnosis = dto.Indication ?? string.Empty,
            Advice = dto.Advice,
            Remark = dto.Remark,
            Discount = dto.Discount,
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
            }).ToList()
        };
    }

    /// <summary>
    /// PrescriptionDetailDto转换为PrescriptionEditDto（用于更新）
    /// </summary>
    public static PrescriptionEditDto ToUpdateDto(this PrescriptionDetailDto dto)
    {
        return new PrescriptionEditDto
        {
            Id = dto.Id,
            Diagnosis = dto.Indication,
            DosageCount = dto.DosageCount,
            Usage = dto.Usage,
            Advice = dto.Advice,
            TotalPrice = dto.TotalPrice,
            Discount = dto.Discount,
            Remark = dto.Remark,
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
            }).ToList() ?? new List<PrescriptionItemInputDto>()
        };
    }

    #endregion
}
