using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.Users.Dtos {
    /// <summary>
    /// 用户修改个人信息 DTO
    /// </summary>
    public class ChangeProfileDto {
        /// <summary>用户ID</summary>
        [Required]
        [DisplayName("用户ID")]
/// <summary>
/// UserId 属性。
/// </summary>
        public Guid UserId { get; set; }

        /// <summary>真实姓名</summary>
        [Required]
        [StringLength(20)]
        [DisplayName("真实姓名")]
/// <summary>
/// RealName 属性。
/// </summary>
        public string RealName { get; set; } = string.Empty;

        /// <summary>邮箱地址</summary>
        [EmailAddress]
        [DisplayName("邮箱地址")]
/// <summary>
/// Email 属性。
/// </summary>
        public string? Email { get; set; }

        /// <summary>联系电话</summary>
        [Phone]
        [DisplayName("联系电话")]
/// <summary>
/// PhoneNumber 属性。
/// </summary>
        public string? PhoneNumber { get; set; }
    }
}
