using LYBT.Common.Enums.Users;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

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
        /// 用户角色集合（至少选择一个）
        /// </summary>
        [Required(ErrorMessage = "用户角色不能为空")]
        [MinLength(1, ErrorMessage = "至少指定一个角色")]
        public List<UserRole> Roles { get; set; } = new();

        /// <summary>
        /// 账号启用状态（true=启用，false=禁用）
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 邮箱地址
        /// </summary>
        [EmailAddress(ErrorMessage = "邮箱格式不正确")]
        public string? Email { get; set; }

        /// <summary>
        /// 联系电话
        /// </summary>
        [Phone(ErrorMessage = "联系电话格式不正确")]
        public string? PhoneNumber { get; set; }

    }
}