using LYBT.Shared.Models.Contracts.Consultation;

namespace LYBT.Shared.Models.Extensions
{
    /// <summary>
    /// 问诊DTO扩展方法 - 替代AutoMapper
    /// Issue #1152: Desktop端移除AutoMapper依赖
    /// OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
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
                // 诊断核心字段（精简版）
                PresentIllness = dto.PresentIllness,
                TongueDiagnosis = dto.TongueDiagnosis,
                PulseDiagnosis = dto.PulseDiagnosis,
                TCMDiagnosis = dto.TCMDiagnosis,
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
                // 诊断核心字段（精简版）
                PresentIllness = dto.PresentIllness,
                TongueDiagnosis = dto.TongueDiagnosis,
                PulseDiagnosis = dto.PulseDiagnosis,
                TCMDiagnosis = dto.TCMDiagnosis
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

            // 诊断核心字段（精简版）
            existing.PresentIllness = dto.PresentIllness;
            existing.TongueDiagnosis = dto.TongueDiagnosis;
            existing.PulseDiagnosis = dto.PulseDiagnosis;
            existing.TCMDiagnosis = dto.TCMDiagnosis;
            existing.UpdatedAt = DateTime.UtcNow;
        }
    }
}
