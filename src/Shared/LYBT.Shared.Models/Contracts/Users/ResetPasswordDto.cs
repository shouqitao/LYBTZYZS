using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Users
{
    // OpenSpec: dto-architecture-specification
    // UserDto空继承别名已删除,统一使用UserDetailDto
    // 参见 docs/architecture/dto-architecture-specification.md

    /// <summary>
    /// 重置密码DTO
    /// </summary>
    public class ResetPasswordDto
    {
        /// <summary>用户ID</summary>
        [Required(ErrorMessage = "用户ID不能为空")]
        [DisplayName("用户ID")]
        public Guid UserId { get; set; }

        /// <summary>新密码</summary>
        [Required(ErrorMessage = "新密码不能为空")]
        [StringLength(128, MinimumLength = 6, ErrorMessage = "密码长度必须在6-128个字符之间")]
        [DisplayName("新密码")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>确认密码</summary>
        [Required(ErrorMessage = "确认密码不能为空")]
        [Compare("NewPassword", ErrorMessage = "两次输入的密码不一致")]
        [DisplayName("确认密码")]
        public string ConfirmPassword { get; set; } = string.Empty;

        /// <summary>重置原因</summary>
        [StringLength(500, ErrorMessage = "重置原因长度不能超过500个字符")]
        [DisplayName("重置原因")]
        public string? Reason { get; set; }

        /// <summary>是否强制用户下次登录时修改密码</summary>
        [DisplayName("强制修改密码")]
        public bool ForceChangePassword { get; set; } = true;
    }
}
