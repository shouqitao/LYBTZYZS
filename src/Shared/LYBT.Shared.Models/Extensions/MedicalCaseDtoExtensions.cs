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
        /// 将MedicalCaseCreateDto转换为MedicalCaseDto
        /// Issue #1152: 替代AutoMapper
        /// 字段映射: Status→CaseStatus
        /// 注意：PatientName/DoctorName等需要在Service层填充
        /// </summary>
        public static MedicalCaseDto ToDto(this MedicalCaseCreateDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new MedicalCaseDto
            {
                CaseNumber = dto.CaseNumber,
                ChiefComplaint = dto.ChiefComplaint,
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                CaseStatus = dto.Status,  // Status → CaseStatus
                Remark = dto.Remark,
                ConsultationDate = DateTime.Now,
                // 以下字段需要在Service层设置
                PatientName = string.Empty,
                DoctorName = string.Empty,
                PatientGender = null,
                PatientAge = null,
                ConsultationId = null,
                PrescriptionId = null,
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 将MedicalCaseDto转换为MedicalCaseUpdateDto
        /// Issue #1778: 组件化架构需要
        /// </summary>
        public static MedicalCaseUpdateDto ToUpdateDto(this MedicalCaseDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new MedicalCaseUpdateDto
            {
                Id = dto.Id,
                ChiefComplaint = dto.ChiefComplaint,
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                Remark = dto.Remark
            };
        }

        /// <summary>
        /// 将MedicalCaseDetailDto转换为MedicalCaseUpdateDto
        /// Issue #1778: 组件化架构需要
        /// </summary>
        public static MedicalCaseUpdateDto ToUpdateDto(this MedicalCaseDetailDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new MedicalCaseUpdateDto
            {
                Id = dto.Id,
                ChiefComplaint = dto.ChiefComplaint,
                PatientId = dto.PatientId,
                DoctorId = dto.DoctorId,
                Remark = dto.Remark
            };
        }

        /// <summary>
        /// 将MedicalCaseUpdateDto应用到现有MedicalCaseDto
        /// Issue #1152: 替代AutoMapper
        /// 注意：MedicalCaseDto字段有限，UpdateDto中的很多字段无法映射
        /// </summary>
        public static void ApplyUpdate(this MedicalCaseDto existing, MedicalCaseUpdateDto dto)
        {
            if (existing == null)
                throw new ArgumentNullException(nameof(existing));
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // 只更新MedicalCaseDto中实际存在的字段
            existing.ChiefComplaint = dto.ChiefComplaint;
            existing.PatientId = dto.PatientId;
            existing.DoctorId = dto.DoctorId;
            existing.Remark = dto.Remark;
            existing.UpdatedAt = DateTime.UtcNow;
            // 注意：UpdateDto中的PresentIllness、PastHistory、DiagnosisResult、TreatmentPlan等字段
            // 在MedicalCaseDto中不存在，无法映射
        }
    }
}
