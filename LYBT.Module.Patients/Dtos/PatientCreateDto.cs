using LYBT.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Patients.Dtos {
    /// <summary>
    /// 创建病人输入 DTO
    /// </summary>
    public class PatientCreateDto {
        [Required(ErrorMessage = "姓名不能为空")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "性别不能为空")]
        public Gender Gender { get; set; } = Gender.Unknown;

        [Required(ErrorMessage = "年龄不能为空")]
        [Range(0, 150, ErrorMessage = "年龄范围应在 0~150 之间")]
        public int Age { get; set; } = 0;

        public string AllergyHistory { get; set; } = string.Empty;

        [Required(ErrorMessage = "民族不能为空")]
        public string Ethnicity { get; set; } = string.Empty;

        [Required(ErrorMessage = "家庭住址不能为空")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "手机号码不能为空")]
        [Phone(ErrorMessage = "手机号码格式不正确")]
        public string PhoneNumber { get; set; } = string.Empty;

        public string Education { get; set; } = string.Empty;
        public string Profession { get; set; } = string.Empty;
        public string IDType { get; set; } = string.Empty;
        public string IDNumber { get; set; } = string.Empty;
        public string MaritalStatus { get; set; } = string.Empty;
    }
}
