using LYBT.Common.Enums.Registration;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Registration {

    /// <summary>
    /// 编辑挂号 DTO
    /// </summary>
    public class RegistrationEditDto {

        /// <summary>挂号ID</summary>
        [Required(ErrorMessage = "挂号ID不能为空")]
        [DisplayName("挂号ID")]
        public Guid Id { get; set; }

        /// <summary>挂号类型</summary>
        [Required(ErrorMessage = "挂号类型不能为空")]
        [DisplayName("挂号类型")]
        public RegistrationType RegistrationType { get; set; } = RegistrationType.Normal;

        /// <summary>医生ID</summary>
        [DisplayName("医生ID")]
        public string DoctorId { get; set; } = string.Empty;

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string Remark { get; set; } = string.Empty;
    }
}