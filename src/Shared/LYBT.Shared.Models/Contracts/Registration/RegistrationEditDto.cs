using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Registration {

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
        public RegistrationType RegistrationType { get; set; } = RegistrationType.Regular;

        /// <summary>医生ID</summary>
        [DisplayName("医生ID")]
        public Guid DoctorId { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string Remark { get; set; } = string.Empty;
    }
}