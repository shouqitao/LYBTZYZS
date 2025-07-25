using LYBT.Common.Enums.Users;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

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
        [DisplayName("用户名（唯一，必填）")]
        /// <summary>
        /// UserName 属性。
        /// </summary>
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 真实姓名（必填）
        /// </summary>
        [Required(ErrorMessage = "真实姓名不能为空")]
        [StringLength(20, ErrorMessage = "真实姓名长度不能超过20个字符")]
        [DisplayName("真实姓名（必填）")]
        /// <summary>
        /// RealName 属性。
        /// </summary>
        public string RealName { get; set; } = string.Empty;

        /// <summary>
        /// 用户角色集合（至少选择一个）
        /// </summary>
        [Required(ErrorMessage = "角色不能为空")]
        [MinLength(1, ErrorMessage = "角色不能为空")]
        [DisplayName("用户角色集合（至少选择一个）")]
        /// <summary>
        /// Roles 属性。
        /// </summary>
        public List<UserRole> Roles { get; set; } = new();

        /// <summary>
        /// 账号启用状态（true=启用，false=禁用）
        /// </summary>
        [DisplayName("账号启用状态（true=启用，false=禁用）")]
        /// <summary>
        /// IsActive 属性。
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// 邮箱地址
        /// </summary>
        [EmailAddress(ErrorMessage = "邮箱格式不正确")]
        [DisplayName("邮箱地址")]
        /// <summary>
        /// Email 属性。
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// 联系电话
        /// </summary>
        [Phone(ErrorMessage = "联系电话格式不正确")]
        [DisplayName("联系电话")]
        /// <summary>
        /// PhoneNumber 属性。
        /// </summary>
        public string? PhoneNumber { get; set; }

    }
}
