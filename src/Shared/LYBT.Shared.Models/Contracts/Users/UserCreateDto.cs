using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Users
{
    /// <summary>
    /// 用户创建DTO - P4-Fix临时创建用于测试编译通过
    /// 最小化字段定义，仅包含测试所需属性
    /// </summary>
    public class UserCreateDto
    {
        /// <summary>用户名</summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(50, ErrorMessage = "用户名长度不能超过50个字符")]
        [DisplayName("用户名")]
        public string Username { get; set; } = string.Empty;

        /// <summary>真实姓名</summary>
        [Required(ErrorMessage = "真实姓名不能为空")]
        [StringLength(50, ErrorMessage = "真实姓名长度不能超过50个字符")]
        [DisplayName("真实姓名")]
        public string RealName { get; set; } = string.Empty;

        /// <summary>手机号码</summary>
        [StringLength(20, ErrorMessage = "手机号码长度不能超过20个字符")]
        [DisplayName("手机号码")]
        public string? PhoneNumber { get; set; }

        /// <summary>用户角色</summary>
        [Required(ErrorMessage = "用户角色不能为空")]
        [DisplayName("用户角色")]
        public UserRole Role { get; set; } = UserRole.Doctor;

        /// <summary>密码</summary>
        [Required(ErrorMessage = "密码不能为空")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "密码长度必须在6-100个字符之间")]
        [DisplayName("密码")]
        public string Password { get; set; } = string.Empty;
    }
}