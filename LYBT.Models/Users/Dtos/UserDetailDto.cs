using LYBT.Common.Enums.Users;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.Users.Dtos {
    /// <summary>
    /// 用户详情 DTO，用于用户资料查看与编辑（不包含密码）
    /// </summary>
    public class UserDetailDto {
        /// <summary>用户唯一标识（主键，Guid 类型，必填）</summary>
        [Required(ErrorMessage = "用户ID不能为空")]
        [DisplayName("用户唯一标识（主键，Guid 类型，必填）")]
/// <summary>
/// Id 属性。
/// </summary>
        public Guid Id { get; set; }

        /// <summary>真实姓名</summary>
        [Required(ErrorMessage = "真实姓名不能为空")]
        [StringLength(20, ErrorMessage = "真实姓名长度不能超过20个字符")]
        [DisplayName("真实姓名")]
/// <summary>
/// RealName 属性。
/// </summary>
        public string RealName { get; set; } = string.Empty;

        /// <summary>多个用户角色（至少一个）</summary>
        [Required(ErrorMessage = "角色不能为空")]
        [MinLength(1, ErrorMessage = "角色不能为空")]
        [DisplayName("多个用户角色（至少一个）")]
/// <summary>
/// Roles 属性。
/// </summary>
        public List<UserRole> Roles { get; set; } = new();

        /// <summary>账号启用状态（true=启用，false=禁用，必填）</summary>
        [Required(ErrorMessage = "账号启用状态不能为空")]
        [DisplayName("账号启用状态（true=启用，false=禁用，必填）")]
/// <summary>
/// IsActive 属性。
/// </summary>
        public bool IsActive { get; set; }

        /// <summary>邮箱地址</summary>
        [EmailAddress(ErrorMessage = "邮箱格式不正确")]
        [DisplayName("邮箱地址")]
/// <summary>
/// Email 属性。
/// </summary>
        public string? Email { get; set; }

        /// <summary>联系电话</summary>
        [Phone(ErrorMessage = "联系电话格式不正确")]
        [DisplayName("联系电话")]
/// <summary>
/// PhoneNumber 属性。
/// </summary>
        public string? PhoneNumber { get; set; }
    }
}
