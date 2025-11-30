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
        /// 将ConsultationInputDto转换为ConsultationDto
        /// Issue #1152: 替代AutoMapper
        /// </summary>
        public static ConsultationDto ToDto(this ConsultationInputDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new ConsultationDto
            {
                MedicalCaseId = dto.MedicalCaseId ?? throw new ArgumentException("MedicalCaseId不能为空", nameof(dto)),
                PatientId = dto.PatientId ?? throw new ArgumentException("PatientId不能为空", nameof(dto)),
                UserId = dto.UserId ?? throw new ArgumentException("UserId不能为空", nameof(dto)),
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
                // OpenSpec: clarify-cancel-consultation-logic - 诊断不需要独立备注
                // MedicalCaseRemark在服务端保存到MedicalCase.Remark
                // DD-002: 移除Status字段，Consultation状态从聚合根MedicalCase派生
                Remark = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 将ConsultationDto转换为ConsultationInputDto
        /// Issue #1778: 组件化架构需要
        /// </summary>
        public static ConsultationInputDto ToInputDto(this ConsultationDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new ConsultationInputDto
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
                // OpenSpec: clarify-cancel-consultation-logic
                // ConsultationDto.Remark映射到MedicalCaseRemark
                MedicalCaseRemark = dto.Remark
            };
        }

        /// <summary>
        /// 将ConsultationInputDto应用到现有ConsultationDto
        /// Issue #1152: 替代AutoMapper
        /// </summary>
        public static void ApplyUpdate(this ConsultationDto existing, ConsultationInputDto dto)
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
            // OpenSpec: clarify-cancel-consultation-logic - 不从InputDto更新Consultation.Remark
            // MedicalCaseRemark在服务端保存到MedicalCase.Remark
            existing.UpdatedAt = DateTime.UtcNow;
        }
    }
}
