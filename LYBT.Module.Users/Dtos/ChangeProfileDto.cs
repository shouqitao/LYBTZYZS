using System;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Users.Dtos {
    /// <summary>
    /// 用户修改个人信息 DTO
    /// </summary>
    public class ChangeProfileDto {
        /// <summary>用户ID</summary>
        [Required]
        public Guid UserId { get; set; }

        /// <summary>真实姓名</summary>
        [Required]
        [StringLength(20)]
        public string RealName { get; set; } = string.Empty;

        /// <summary>邮箱地址</summary>
        [EmailAddress]
        public string? Email { get; set; }

        /// <summary>联系电话</summary>
        [Phone]
        public string? PhoneNumber { get; set; }
    }
}
