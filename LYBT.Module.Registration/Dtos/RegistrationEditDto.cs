using LYBT.Common.Enums;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Registration.Dtos {

    /// <summary>
    /// 编辑挂号 DTO
    /// </summary>
    public class RegistrationEditDto {

        /// <summary>挂号ID</summary>
        [Required(ErrorMessage = "挂号ID不能为空")]
        public Guid Id { get; set; }

        /// <summary>挂号类型</summary>
        [Required(ErrorMessage = "挂号类型不能为空")]
        public RegistrationType RegistrationType { get; set; } = RegistrationType.General;

        /// <summary>备注</summary>
        public string Remark { get; set; } = string.Empty;
    }
}