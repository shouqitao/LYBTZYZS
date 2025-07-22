using LYBT.Common.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.Registration.Dtos {

    /// <summary>
    /// 编辑挂号 DTO
    /// </summary>
    public class RegistrationEditDto {

        /// <summary>挂号ID</summary>
        [Required(ErrorMessage = "挂号ID不能为空")]
        [DisplayName("挂号ID")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>挂号类型</summary>
        [Required(ErrorMessage = "挂号类型不能为空")]
        [DisplayName("挂号类型")]
/// <summary>
/// RegistrationType 属性。
/// </summary>
        public RegistrationType RegistrationType { get; set; } = RegistrationType.General;

        /// <summary>医生ID</summary>
        [DisplayName("医生ID")]
/// <summary>
/// DoctorId 属性。
/// </summary>
        public string DoctorId { get; set; } = string.Empty;

        /// <summary>备注</summary>
        [DisplayName("备注")]
/// <summary>
/// Remark 属性。
/// </summary>
        public string Remark { get; set; } = string.Empty;
    }
}
