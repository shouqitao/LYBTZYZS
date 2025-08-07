using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Users {

    /// <summary>
    /// 用户更新DTO - 前后端共享API契约
    /// 用于更新用户信息的请求模型
    /// </summary>
    public class UserUpdateDto {

        /// <summary>用户ID</summary>
        [Required(ErrorMessage = "用户ID不能为空")]
        [DisplayName("用户ID")]
        public Guid Id { get; set; }

        /// <summary>用户名</summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(32, MinimumLength = 3, ErrorMessage = "用户名长度必须在3-32个字符之间")]
        [RegularExpression(@"^[a-zA-Z0-9_]+$", ErrorMessage = "用户名只能包含字母、数字和下划线")]
        [DisplayName("用户名")]
        public string Username { get; set; } = string.Empty;

        /// <summary>真实姓名</summary>
        [Required(ErrorMessage = "真实姓名不能为空")]
        [StringLength(50, ErrorMessage = "真实姓名长度不能超过50个字符")]
        [DisplayName("真实姓名")]
        public string RealName { get; set; } = string.Empty;

        /// <summary>用户角色</summary>
        [Required(ErrorMessage = "用户角色不能为空")]
        [DisplayName("用户角色")]
        public string Role { get; set; } = "User";

        /// <summary>电话号码</summary>
        [Phone(ErrorMessage = "电话号码格式不正确")]
        [StringLength(20, ErrorMessage = "电话号码长度不能超过20个字符")]
        [DisplayName("电话号码")]
        public string? PhoneNumber { get; set; }

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}