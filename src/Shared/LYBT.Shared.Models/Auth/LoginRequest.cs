using System.ComponentModel;

namespace LYBT.Shared.Models.Auth
{
    /// <summary>
    /// 登录请求数据 - 前后端共享
    /// </summary>
    public class LoginRequest
    {
        /// <summary>用户名</summary>
        [DisplayName("用户名")]
        public string Username { get; set; } = string.Empty;

        /// <summary>密码</summary>
        [DisplayName("密码")]
        public string Password { get; set; } = string.Empty;

        /// <summary>记住我</summary>
        [DisplayName("记住我")]
        public bool RememberMe { get; set; }

        /// <summary>客户端IP</summary>
        [DisplayName("客户端IP")]
        public string? ClientIp { get; set; }

        /// <summary>用户代理</summary>
        [DisplayName("用户代理")]
        public string? UserAgent { get; set; }

        /// <summary>登录类型</summary>
        [DisplayName("登录类型")]
        public string LoginType { get; set; } = "Password";
    }
}