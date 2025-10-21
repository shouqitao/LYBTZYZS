using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Auth
{

    /// <summary>
    /// 用户登录请求 - 前后端共享API契约
    /// </summary>
    public class LoginRequest
    {

        /// <summary>用户名</summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(32, ErrorMessage = "用户名长度不能超过32个字符")]
        [DisplayName("用户名")]
        public string UserName { get; set; } = string.Empty;

        /// <summary>密码</summary>
        [Required(ErrorMessage = "密码不能为空")]
        [DisplayName("密码")]
        public string Password { get; set; } = string.Empty;

        /// <summary>客户端IP</summary>
        [DisplayName("客户端IP")]
        public string? ClientIp { get; set; }

        /// <summary>用户</summary>
        [DisplayName("UserAgent")]
        public string? UserAgent { get; set; }

        /// <summary>登录类型（Password, WeChat, OAuth等）</summary>
        [DisplayName("登录类型（Password, WeChat, OAuth等）")]
        public string? LoginType { get; set; } = "Password";

        /// <summary>记住我</summary>
        [DisplayName("记住我")]
        public bool RememberMe { get; set; } = false;

        /// <summary>设备ID（用于多设备管理）</summary>
        [DisplayName("设备ID")]
        public string? DeviceId { get; set; }

        /// <summary>设备名称（用于多设备管理）</summary>
        [DisplayName("设备名称")]
        public string? DeviceName { get; set; }
    }
}
