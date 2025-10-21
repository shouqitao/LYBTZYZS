using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Extensions
{
    /// <summary>
    /// 问诊DTO扩展方法 - 替代AutoMapper
    /// Issue #1152: Desktop端移除AutoMapper依赖
    /// </summary>
    public static class ConsultationDtoExtensions
    {
        /// <summary>
        /// 将ConsultationCreateDto转换为ConsultationDto
        /// Issue #1152: 替代AutoMapper
        /// </summary>
        public static ConsultationDto ToDto(this ConsultationCreateDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new ConsultationDto
            {
                MedicalCaseId = dto.MedicalCaseId,
                PatientId = dto.PatientId,
                UserId = dto.UserId,
                PatientName = dto.PatientName,
                DoctorName = dto.DoctorName,
                ChiefComplaint = dto.ChiefComplaint,
                PresentIllness = dto.PresentIllness,
                Inspection = dto.Inspection,
                AuscultationOlfaction = dto.AuscultationOlfaction,
                Inquiry = dto.Inquiry,
                Palpation = dto.Palpation,
                TCMDiagnosis = dto.TCMDiagnosis,
                TreatmentPrinciple = dto.TreatmentPrinciple,
                MedicalAdvice = dto.MedicalAdvice,
                // Issue #1562 Phase 2: 移除StartTime/EndTime/ConsultationStatus
                Remark = dto.Remark,
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 将ConsultationUpdateDto应用到现有ConsultationDto
        /// Issue #1152: 替代AutoMapper
        /// </summary>
        public static void ApplyUpdate(this ConsultationDto existing, ConsultationUpdateDto dto)
        {
            if (existing == null)
                throw new ArgumentNullException(nameof(existing));
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            existing.ChiefComplaint = dto.ChiefComplaint;
            existing.PresentIllness = dto.PresentIllness;
            existing.Inspection = dto.Inspection;
            existing.AuscultationOlfaction = dto.AuscultationOlfaction;
            existing.Inquiry = dto.Inquiry;
            existing.Palpation = dto.Palpation;
            existing.TCMDiagnosis = dto.TCMDiagnosis;
            existing.TreatmentPrinciple = dto.TreatmentPrinciple;
            existing.MedicalAdvice = dto.MedicalAdvice; // Issue #1562 Phase 2
            existing.Remark = dto.Remark;
            existing.UpdatedAt = DateTime.UtcNow;
        }
    }
}
