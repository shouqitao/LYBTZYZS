using System;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Users.Dtos {
    /// <summary>
    /// 用户修改密码 DTO
    /// </summary>
    public class ChangePasswordDto {
        /// <summary>
        /// 用户ID
        /// </summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>
        /// 原密码
        /// </summary>
        [Required]
        [StringLength(32, MinimumLength = 6)]
        public string OldPassword { get; set; } = string.Empty;

        /// <summary>
        /// 新密码
        /// </summary>
        [Required]
        [StringLength(32, MinimumLength = 6)]
        public string NewPassword { get; set; } = string.Empty;
    }
}
