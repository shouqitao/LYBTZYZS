using LYBT.Module.Patients.Dtos;
using LYBT.Module.Patients.Models;

namespace LYBT.Module.Patients.Extensions {

    /// <summary>
    /// 病人模型与 DTO 映射扩展方法（用于模型转换）
    /// </summary>
    public static class PatientMappingExtensions {

        /// <summary>
        /// 将 PatientModel 转换为 PatientEditDto（用于编辑页面）
        /// </summary>
        public static PatientEditDto ToEditDto(this PatientModel model) {
            return new PatientEditDto {
                Id = model.Id,
                Name = model.Name,
                Gender = model.Gender,
                Age = model.Age,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                IDNumber = model.IDNumber
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
                Age = model.Age,
                PhoneNumber = model.PhoneNumber,
                Address = model.Address,
                PinyinCode = model.PinyinCode
            };
        }

        /// <summary>
        /// 将 PatientEditDto 转换为 PatientModel（用于保存/更新）
        /// </summary>
        public static PatientModel ToModel(this PatientEditDto dto) {
            return new PatientModel {
                Id = dto.Id,
                Name = dto.Name,
                Gender = dto.Gender,
                Age = dto.Age,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,
                IDNumber = dto.IDNumber
            };
        }
    }
}