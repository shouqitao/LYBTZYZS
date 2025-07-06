using LYBT.Models.Patients;
using LYBT.Module.Patients.Dtos;

namespace LYBT.Module.Patients.Extensions {
    /// <summary>
    /// 病人模型与 DTO 映射扩展方法（用于模型转换）
    /// </summary>
    public static class PatientMappingExtensions {
        /// <summary>
        /// 将 PatientModel 转换为 PatientDetailDto（用于详情/编辑页面）
        /// </summary>
        public static PatientDetailDto ToDetailDto(this PatientModel model) {
            return new PatientDetailDto {
                Id = model.Id,
                Name = model.Name,
                Gender = model.Gender,
                Age = model.Age ?? 0,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                IDNumber = model.IDNumber,
                PinyinCode = model.PinyinCode,
                IsSpecial = model.IsSpecial
                // 可补充其他字段
            };
        }

        /// <summary>
        /// 将 PatientModel 转换为 PatientDto（用于列表展示）
        /// </summary>
        public static PatientDto ToDto(this PatientModel model) {
            return new PatientDto {
                Id = model.Id,
                Name = model.Name,
                Gender = model.Gender,
                Age = model.Age ?? 0,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                PinyinCode = model.PinyinCode,
                IsSpecial = model.IsSpecial
                // 可补充其他字段
            };
        }

        /// <summary>
        /// 将 PatientDetailDto 转换为 PatientModel（用于保存/更新）
        /// </summary>
        public static PatientModel ToModel(this PatientDetailDto dto) {
            return new PatientModel {
                Id = dto.Id,
                Name = dto.Name,
                Gender = dto.Gender,
                Age = dto.Age,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,
                IDNumber = dto.IDNumber,
                PinyinCode = dto.PinyinCode,
                IsSpecial = dto.IsSpecial
                // 可补充其他字段
            };
        }
    }
}