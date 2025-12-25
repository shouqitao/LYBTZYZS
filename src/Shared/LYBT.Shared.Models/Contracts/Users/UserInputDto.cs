using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;  // UserRole

namespace LYBT.Shared.Models.Contracts.Users
{
    // OpenSpec: dto-architecture-specification
    // UserDto空继承别名已删除,统一使用UserDetailDto
    // 参见 docs/architecture/dto-architecture-specification.md

    /// <summary>
    /// 用户输入DTO - 统一创建和更新
    /// Phase 3: 合并UserCreateDto和UserUpdateDto
    /// Issue #1262: 密码改为可选,Server端使用默认值
    /// </summary>
    public class UserInputDto
    {
        /// <summary>用户ID(更新时必填,创建时为null)</summary>
        [DisplayName("用户ID")]
        public Guid? Id { get; set; }

        /// <summary>用户名(创建时必填,更新时不可改)</summary>
        [StringLength(32, MinimumLength = 3, ErrorMessage = "用户名长度必须在3-32个字符之间")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "用户名只能包含字母、数字和下划线")]
        [DisplayName("用户名")]
        public string? UserName { get; set; }

        /// <summary>
        /// 密码(创建时可选,更新时禁止)
        /// Issue #1262: 如果不提供密码,Server端将使用配置的默认密码
        /// </summary>
        [StringLength(128, MinimumLength = 6, ErrorMessage = "密码长度必须在6-128个字符之间")]
        [DisplayName("密码")]
        public string? Password { get; set; }

        /// <summary>
        /// 确认密码(创建时可选,更新时禁止)
        /// Issue #1262: 仅当提供密码时需要确认
        /// </summary>
        [Compare("Password", ErrorMessage = "两次输入的密码不一致")]
        [DisplayName("确认密码")]
        public string? ConfirmPassword { get; set; }

        /// <summary>真实姓名(创建时必填,更新时可选)</summary>
        [StringLength(50, ErrorMessage = "真实姓名长度不能超过50个字符")]
        [DisplayName("真实姓名")]
        public string? RealName { get; set; }

        /// <summary>拼音码(可手动修正多音字错误)</summary>
        [StringLength(50, ErrorMessage = "拼音码长度不能超过50个字符")]
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>手机号码</summary>
        [Phone(ErrorMessage = "电话号码格式不正确")]
        [StringLength(20, ErrorMessage = "手机号码长度不能超过20个字符")]
        [DisplayName("手机号码")]
        public string? PhoneNumber { get; set; }

        /// <summary>邮箱地址</summary>
        [EmailAddress(ErrorMessage = "邮箱格式不正确")]
        [StringLength(100, ErrorMessage = "邮箱长度不能超过100个字符")]
        [DisplayName("邮箱地址")]
        public string? Email { get; set; }

        /// <summary>用户角色(创建时必填,更新时可选)</summary>
        [DisplayName("用户角色")]
        public UserRole? Role { get; set; } = UserRole.Doctor;

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        // OpenSpec: sync-entity-dto-fields - Status字段已移除
        // InputDto不应包含Status字段，状态变更应通过专用API进行
    }
}
