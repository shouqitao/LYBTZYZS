using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Extensions
{
    /// <summary>
    /// 医案DTO扩展方法 - 替代AutoMapper
    /// Issue #1152: Desktop端移除AutoMapper依赖
    /// </summary>
    public static class MedicalCaseDtoExtensions
    {
        /// <summary>
        /// 将MedicalCaseInputDto转换为MedicalCaseDto
        /// Epic #1961: 使用统一的 MedicalCaseInputDto
        /// 注意：PatientName/DoctorName等需要在Service层填充
        /// </summary>
        public static MedicalCaseDto ToDto(this MedicalCaseInputDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // OpenSpec: refactor-diagnosis-fields - 移除ChiefComplaint
            return new MedicalCaseDto
            {
                Id = dto.Id ?? Guid.Empty,
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                CaseStatus = MedicalCaseStatus.Active,
                Remark = dto.Remark,
                ConsultationDate = dto.VisitDate,
                // 以下字段需要在Service层设置
                PatientName = string.Empty,
                DoctorName = string.Empty,
                PatientGender = null,
                PatientAge = null,
                ConsultationId = null,
                PrescriptionId = null,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 将MedicalCaseDto转换为MedicalCaseInputDto
        /// Epic #1961: 使用统一的 MedicalCaseInputDto
        /// </summary>
        public static MedicalCaseInputDto ToInputDto(this MedicalCaseDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // OpenSpec: refactor-diagnosis-fields - 移除ChiefComplaint
            return new MedicalCaseInputDto
            {
                Id = dto.Id,
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                VisitDate = dto.ConsultationDate,
                Remark = dto.Remark
            };
        }

        /// <summary>
        /// 将MedicalCaseDetailDto转换为MedicalCaseInputDto
        /// Epic #1961: 使用统一的 MedicalCaseInputDto
        /// </summary>
        // OpenSpec: refactor-diagnosis-fields - 移除ChiefComplaint
        public static MedicalCaseInputDto ToInputDto(this MedicalCaseDetailDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new MedicalCaseInputDto
            {
                Id = dto.Id,
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                VisitDate = dto.ConsultationDate,
                PresentIllnessHistory = dto.PresentIllness,
                Remark = dto.Remark
            };
        }

        /// <summary>
        /// 将MedicalCaseInputDto应用到现有MedicalCaseDto
        /// Epic #1961: 使用统一的 MedicalCaseInputDto
        /// 注意：MedicalCaseDto字段有限，InputDto中的很多字段无法映射
        /// </summary>
        public static void ApplyUpdate(this MedicalCaseDto existing, MedicalCaseInputDto dto)
        {
            if (existing == null)
                throw new ArgumentNullException(nameof(existing));
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // 只更新MedicalCaseDto中实际存在的字段
            // OpenSpec: refactor-diagnosis-fields - 移除ChiefComplaint
            existing.PatientId = dto.PatientId;
            existing.DoctorId = dto.DoctorId;
            existing.ConsultationDate = dto.VisitDate;
            existing.Remark = dto.Remark;
            existing.UpdatedAt = DateTime.UtcNow;
            // 注意：InputDto中的中医诊疗字段在MedicalCaseDto中不存在，无法映射
        }
    }
}
