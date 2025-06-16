using System;
using System.ComponentModel.DataAnnotations;
using LYBT.Common.Enums;

namespace LYBT.Module.Users.Dtos {
    /// <summary>
    /// 新增用户请求 DTO，用于创建用户（带校验注解）
    /// </summary>
    public class UserCreateDto {
        /// <summary>
        /// 用户名（唯一，必填）
        /// </summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(32, MinimumLength = 2, ErrorMessage = "用户名长度需在2-32个字符之间")]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 真实姓名（必填）
        /// </summary>
        [Required(ErrorMessage = "真实姓名不能为空")]
        [StringLength(20, ErrorMessage = "真实姓名长度不能超过20个字符")]
        public string RealName { get; set; } = string.Empty;

        /// <summary>
        /// 用户角色（必填）
        /// </summary>
        [Required(ErrorMessage = "用户角色不能为空")]
        [EnumDataType(typeof(UserRole), ErrorMessage = "用户角色无效")]
        public UserRole Role { get; set; } = UserRole.Doctor;

        /// <summary>
        /// 账号启用状态（true=启用，false=禁用）
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 初始密码（必填）
        /// </summary>
        [Required(ErrorMessage = "密码不能为空")]
        [StringLength(32, MinimumLength = 6, ErrorMessage = "密码长度必须在6-32个字符之间")]
        public string Password { get; set; } = string.Empty;
    }
}
