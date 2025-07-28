using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Users {

    /// <summary>
    /// 管理员重置密码 DTO
    /// </summary>
    public class ResetPasswordDto {

        /// <summary>
        /// 新密码（必填）
        /// </summary>
        [Required(ErrorMessage = "新密码不能为空")]
        [StringLength(32, MinimumLength = 6)]
        [DisplayName("新密码（必填）")]
        public string NewPassword { get; set; } = string.Empty;
    }
}