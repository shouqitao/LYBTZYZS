using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Doctors.Dtos {
    /// <summary>
    /// 重置密码 DTO
    /// </summary>
    public class ResetPasswordDto {
        /// <summary>新密码，留空则使用默认值</summary>
        [StringLength(32, MinimumLength = 6)]
        public string? NewPassword { get; set; }
    }
}
