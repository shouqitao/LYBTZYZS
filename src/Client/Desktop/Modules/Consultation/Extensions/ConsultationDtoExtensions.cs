using System;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Modules.Consultation.Extensions
{
    /// <summary>
    /// ConsultationDto扩展方法 - UltraThink v2.0 DTO转换
    /// </summary>
    public static class ConsultationDtoExtensions
    {
        /// <summary>
        /// 将ConsultationDetailDto转换为ConsultationDto
        /// UltraThink v2.0: 提供统一的DTO转换逻辑
        /// </summary>
        /// <param name="detailDto">详情DTO</param>
        /// <returns>基础DTO</returns>
        public static ConsultationDto ToConsultationDto(this ConsultationDetailDto detailDto)
        {
            if (detailDto == null)
                throw new ArgumentNullException(nameof(detailDto));

            return new ConsultationDto
            {
                Id = detailDto.Id,
                MedicalCaseId = detailDto.MedicalCaseId,
                PatientId = detailDto.PatientId,
                UserId = detailDto.DoctorId,
                DoctorName = detailDto.DoctorName,
                ConsultationTime = detailDto.ConsultationTime,
                ChiefComplaint = detailDto.ChiefComplaint,
                PresentIllness = detailDto.PresentIllness,
                Inspection = detailDto.Inspection,
                AuscultationOlfaction = detailDto.AuscultationOlfaction,
                Inquiry = detailDto.Inquiry,
                Palpation = detailDto.Palpation,
                TongueInspection = detailDto.TongueInspection,
                PulseCondition = detailDto.PulseCondition,
                DifferentiationAnalysis = detailDto.PatternDifferentiation,
                TCMDiagnosis = detailDto.TCMDiagnosis ?? string.Empty,
                Diagnosis = detailDto.Diagnosis,
                TreatmentPrinciple = detailDto.TreatmentPrinciple,
                MedicalAdvice = detailDto.MedicalAdvice,
                Remark = detailDto.Remark,
                Status = (CommonStatus)detailDto.Status,
                CreateTime = detailDto.CreateTime,
                UpdateTime = detailDto.UpdateTime
            };
        }
    }
}