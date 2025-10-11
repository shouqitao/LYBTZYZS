using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Extensions
{
    /// <summary>
    /// 患者DTO扩展方法 - 替代AutoMapper
    /// Issue #1152: Desktop端移除AutoMapper依赖
    /// </summary>
    public static class PatientDtoExtensions
    {
        /// <summary>
        /// 将PatientCreateDto转换为PatientDto（用于创建预览）
        /// </summary>
        public static PatientDto ToDto(this PatientCreateDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new PatientDto
            {
                // 基本信息
                Name = dto.Name,
                Gender = dto.Gender,
                BirthDate = dto.BirthDate,
                IdNumber = dto.IdNumber,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,

                // 健康信息
                AllergyHistory = dto.AllergyHistory,
                MaritalStatus = dto.MaritalStatus,
                IdType = dto.IdType,
                BloodType = dto.BloodType,

                // 紧急联系人
                EmergencyContactName = dto.EmergencyContactName,
                EmergencyContactPhone = dto.EmergencyContactPhone,
                EmergencyContactRelation = dto.EmergencyContactRelation,

                // 系统字段（新建时的默认值）
                Status = CommonStatus.Enabled,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                VisitCount = 0
                // Id, LastVisitTime, DisableReason, PinYinCode 保持默认null/0值
            };
        }

        /// <summary>
        /// 将PatientUpdateDto的字段应用到现有PatientDto（用于更新）
        /// </summary>
        public static void ApplyUpdate(this PatientDto existing, PatientUpdateDto dto)
        {
            if (existing == null)
                throw new ArgumentNullException(nameof(existing));
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            // 基本信息
            existing.Name = dto.Name;
            existing.Gender = dto.Gender;
            existing.BirthDate = dto.BirthDate;
            existing.IdNumber = dto.IdNumber;
            existing.PhoneNumber = dto.PhoneNumber;
            existing.Address = dto.Address;

            // 健康信息
            existing.AllergyHistory = dto.AllergyHistory;
            existing.MaritalStatus = dto.MaritalStatus;
            existing.IdType = dto.IdType;
            existing.BloodType = dto.BloodType;

            // 紧急联系人
            existing.EmergencyContactName = dto.EmergencyContactName;
            existing.EmergencyContactPhone = dto.EmergencyContactPhone;
            existing.EmergencyContactRelation = dto.EmergencyContactRelation;

            // 更新时间戳
            existing.UpdatedAt = DateTime.UtcNow;

            // 不更新：Id, Status, CreatedAt, VisitCount, LastVisitTime, DisableReason, PinYinCode
        }

        /// <summary>
        /// 将PatientDto转换为PatientUpdateDto（用于编辑表单）
        /// </summary>
        public static PatientUpdateDto ToUpdateDto(this PatientDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            return new PatientUpdateDto
            {
                Id = dto.Id,

                // 基本信息
                Name = dto.Name,
                Gender = dto.Gender,
                BirthDate = dto.BirthDate,
                IdNumber = dto.IdNumber,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,

                // 健康信息
                AllergyHistory = dto.AllergyHistory,
                MaritalStatus = dto.MaritalStatus,
                IdType = dto.IdType,
                BloodType = dto.BloodType,

                // 紧急联系人
                EmergencyContactName = dto.EmergencyContactName,
                EmergencyContactPhone = dto.EmergencyContactPhone,
                EmergencyContactRelation = dto.EmergencyContactRelation,

                // Status字段
                Status = dto.Status
            };
        }
    }
}
