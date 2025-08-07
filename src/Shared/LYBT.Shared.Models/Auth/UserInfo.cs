using System.ComponentModel;
// using LYBT.Shared.Models.Enums; // UserRole已删除

namespace LYBT.Shared.Models.Auth {

    /// <summary>
    /// 用户信息 - API契约
    /// </summary>
    public class UserInfo {

        /// <summary>用户ID</summary>
        [DisplayName("用户ID")]
        public Guid Id { get; set; }

        /// <summary>用户名</summary>
        [DisplayName("用户名")]
        public string Username { get; set; } = string.Empty;

        /// <summary>真实姓名</summary>
        [DisplayName("真实姓名")]
        public string RealName { get; set; } = string.Empty;

        /// <summary>角色</summary>
        [DisplayName("角色")]// 
public string Role { get; set; } = "User";

        /// <summary>邮箱</summary>
        [DisplayName("邮箱")]
        public string? Email { get; set; }

        /// <summary>手机号</summary>
        [DisplayName("手机号")]
        public string? PhoneNumber { get; set; }

        /// <summary>是否激活</summary>
        [DisplayName("是否激活")]
        public bool IsActive { get; set; } = true;
    }
}