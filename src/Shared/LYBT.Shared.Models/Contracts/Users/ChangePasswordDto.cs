using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Users
{
    // OpenSpec: dto-architecture-specification
    // UserDto空继承别名已删除,统一使用UserDetailDto
    // 参见 docs/architecture/dto-architecture-specification.md

    /// <summary>
    /// 修改密码DTO
    /// </summary>
    public class ChangePasswordDto
    {
        /// <summary>用户ID</summary>
        [Required(ErrorMessage = "用户ID不能为空")]
        [DisplayName("用户ID")]
        public Guid UserId { get; set; }

        /// <summary>原密码</summary>
        [Required(ErrorMessage = "原密码不能为空")]
        [StringLength(128, MinimumLength = 6, ErrorMessage = "密码长度必须在6-128个字符之间")]
        [DisplayName("原密码")]
        public string OldPassword { get; set; } = string.Empty;

        /// <summary>新密码</summary>
        [Required(ErrorMessage = "新密码不能为空")]
        [StringLength(128, MinimumLength = 6, ErrorMessage = "密码长度必须在6-128个字符之间")]
        [DisplayName("新密码")]
        public string NewPassword { get; set; } = string.Empty;

        /// <summary>确认新密码</summary>
        [Required(ErrorMessage = "确认密码不能为空")]
        [Compare("NewPassword", ErrorMessage = "两次输入的密码不一致")]
        [DisplayName("确认新密码")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
