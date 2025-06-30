using LYBT.Common.Enums.Users;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Users.Dtos {

    /// <summary>
    /// 编辑用户请求 DTO，用于修改用户资料（带校验注解）
    /// </summary>
    public class UserEditDto {

        /// <summary>
        /// 用户唯一标识（主键，Guid 类型，必填）
        /// </summary>
        [Required(ErrorMessage = "用户ID不能为空")]
        public Guid Id { get; set; }

        /// <summary>
        /// 真实姓名
        /// </summary>
        [Required(ErrorMessage = "真实姓名不能为空")]
        [StringLength(20, ErrorMessage = "真实姓名长度不能超过20个字符")]
        public string RealName { get; set; } = string.Empty;

        /// <summary>
        /// 用户角色（管理员、医生等，枚举类型，必填）
        /// </summary>
        [Required(ErrorMessage = "用户角色不能为空")]
        [EnumDataType(typeof(UserRole), ErrorMessage = "用户角色无效")]
        public UserRole Role { get; set; } = UserRole.DiagnosingDoctor;

        /// <summary>
        /// 账号启用状态（true=启用，false=禁用，必填）
        /// </summary>
        [Required(ErrorMessage = "账号启用状态不能为空")]
        public bool IsActive { get; set; }

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

        /// <summary>
        /// 新密码（可选，留空表示不修改。修改密码时必须符合长度要求）
        /// </summary>
        [StringLength(32, MinimumLength = 6, ErrorMessage = "密码长度必须在6-32个字符之间")]
        public string? Password { get; set; }
    }
}