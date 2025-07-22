using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.Users.Dtos {

    /// <summary>
    /// 用户修改密码 DTO
    /// </summary>
    public class ChangePasswordDto {

        /// <summary>
        /// 用户ID
        /// </summary>
        [Required]
        [DisplayName("用户ID")]
/// <summary>
/// UserId 属性。
/// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// 原密码
        /// </summary>
        [Required]
        [StringLength(32, MinimumLength = 6)]
        [DisplayName("原密码")]
/// <summary>
/// OldPassword 属性。
/// </summary>
        public string OldPassword { get; set; } = string.Empty;

        /// <summary>
        /// 新密码
        /// </summary>
        [Required]
        [StringLength(32, MinimumLength = 6)]
        [DisplayName("新密码")]
/// <summary>
/// NewPassword 属性。
/// </summary>
        public string NewPassword { get; set; } = string.Empty;
    }
}
