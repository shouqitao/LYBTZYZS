using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Doctors {

    /// <summary>
    /// 医生创建 DTO（简化版）
    /// </summary>
    public class DoctorCreateDto {

        [Required(ErrorMessage = "用户ID不能为空")]
        [DisplayName("用户ID")]
        public Guid UserId { get; set; }

        [Required(ErrorMessage = "医生姓名不能为空")]
        [StringLength(50, ErrorMessage = "姓名长度不能超过50个字符")]
        [DisplayName("医生姓名")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "专长不能为空")]
        [StringLength(200, ErrorMessage = "专长描述不能超过200个字符")]
        [DisplayName("专长")]
        public string Specialty { get; set; } = string.Empty;

        [Required(ErrorMessage = "挂号费不能为空")]
        [Range(0, 9999.99, ErrorMessage = "挂号费必须在0-9999.99之间")]
        [DisplayName("挂号费")]
        public decimal RegistrationFee { get; set; }

        [Required(ErrorMessage = "执业证书号不能为空")]
        [StringLength(50, ErrorMessage = "执业证书号不能超过50个字符")]
        [DisplayName("执业证书号")]
        public string LicenseNumber { get; set; } = string.Empty;

        [StringLength(20, ErrorMessage = "联系电话不能超过20个字符")]
        [DisplayName("联系电话")]
        public string? ContactNumber { get; set; }

        [StringLength(500, ErrorMessage = "简介不能超过500个字符")]
        [DisplayName("简介")]
        public string? Introduction { get; set; }
    }
}