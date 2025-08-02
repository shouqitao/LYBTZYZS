using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Users
{
    /// <summary>
    /// 创建用户DTO
    /// </summary>
    public class CreateUserDto
    {
        /// <summary>用户名</summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(32, ErrorMessage = "用户名长度不能超过32个字符")]
        public string UserName { get; set; } = string.Empty;

        /// <summary>真实姓名</summary>
        [Required(ErrorMessage = "真实姓名不能为空")]
        [StringLength(50, ErrorMessage = "真实姓名长度不能超过50个字符")]
        public string RealName { get; set; } = string.Empty;

        /// <summary>角色</summary>
        [Required(ErrorMessage = "角色不能为空")]
        public string Role { get; set; } = string.Empty;

        /// <summary>邮箱</summary>
        [EmailAddress(ErrorMessage = "邮箱格式不正确")]
        public string? Email { get; set; }

        /// <summary>电话号码</summary>
        [StringLength(20, ErrorMessage = "电话号码长度不能超过20个字符")]
        public string? PhoneNumber { get; set; }

        /// <summary>备注</summary>
        public string? Remark { get; set; }
    }
}