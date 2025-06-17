using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Doctors.Dtos {
    /// <summary>
    /// 重置密码 DTO
    /// </summary>
    public class ResetPasswordDto {
        /// <summary>新密码</summary>
        [Required(ErrorMessage = "新密码不能为空")]
        [StringLength(32, MinimumLength = 6)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
