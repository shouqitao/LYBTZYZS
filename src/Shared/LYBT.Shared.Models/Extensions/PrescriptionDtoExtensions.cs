using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Extensions
{
    /// <summary>
    /// 处方DTO扩展方法 - 替代AutoMapper
    /// Issue #1152: Desktop端移除AutoMapper依赖
    /// </summary>
    public static class PrescriptionDtoExtensions
    {
        /// <summary>
        /// 将PrescriptionCreateDto转换为PrescriptionDto
        /// Issue #1152: 替代AutoMapper
        /// 字段映射: DoctorId→UserId, Quantity→DosageCount
        /// </summary>
        public static PrescriptionDto ToDto(this PrescriptionCreateDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new PrescriptionDto
            {
                PatientId = dto.PatientId,
                UserId = dto.DoctorId,  // DoctorId → UserId
                DosageCount = dto.Quantity,  // Quantity → DosageCount
                Usage = dto.Usage,
                Discount = 1.0m,
                Advice = dto.Advice,
                FormulaSource = dto.FormulaSource,
                Remark = dto.Remark,
                MedicalCaseId = Guid.Empty,  // 需要在Service层设置
                Indication = null,  // 需要在Service层设置
                Items = new List<PrescriptionItemDto>(),  // 需要在Service层单独处理
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 将PrescriptionDto转换为PrescriptionUpdateDto
        /// Issue #1778: 组件化架构需要
        /// </summary>
        public static PrescriptionUpdateDto ToUpdateDto(this PrescriptionDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new PrescriptionUpdateDto
            {
                PrescriptionNumber = dto.PrescriptionNumber,
                Diagnosis = dto.Indication ?? string.Empty, // Indication → Diagnosis
                DosageCount = dto.DosageCount,
                Usage = dto.Usage,
                Discount = dto.Discount,
                Advice = dto.Advice,
                Remark = dto.Remark
            };
        }

        /// <summary>
        /// 将PrescriptionUpdateDto应用到现有PrescriptionDto
        /// Issue #1152: 替代AutoMapper
        /// </summary>
        public static void ApplyUpdate(this PrescriptionDto existing, PrescriptionUpdateDto dto)
        {
            if (existing == null)
                throw new ArgumentNullException(nameof(existing));
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            existing.Advice = dto.Advice;
            existing.DosageCount = dto.DosageCount;
            existing.Usage = dto.Usage;
            existing.Discount = dto.Discount;
            existing.Remark = dto.Remark;
            existing.UpdatedAt = DateTime.UtcNow;
            // Items 需要在Service层单独处理
        }
    }
}
