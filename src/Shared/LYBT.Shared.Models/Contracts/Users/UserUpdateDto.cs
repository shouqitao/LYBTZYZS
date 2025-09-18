using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Users
{
    /// <summary>
    /// 用户更新DTO - P4-Fix临时创建用于测试编译通过
    /// 最小化字段定义，仅包含测试所需属性
    /// </summary>
    public class UserUpdateDto
    {
        /// <summary>用户ID</summary>
        [Required(ErrorMessage = "用户ID不能为空")]
        public Guid Id { get; set; }

        /// <summary>真实姓名</summary>
        [StringLength(50, ErrorMessage = "真实姓名长度不能超过50个字符")]
        [DisplayName("真实姓名")]
        public string? RealName { get; set; }

        /// <summary>手机号码</summary>
        [StringLength(20, ErrorMessage = "手机号码长度不能超过20个字符")]
        [DisplayName("手机号码")]
        public string? PhoneNumber { get; set; }

        /// <summary>用户角色</summary>
        [DisplayName("用户角色")]
        public UserRole? Role { get; set; }
    }
}