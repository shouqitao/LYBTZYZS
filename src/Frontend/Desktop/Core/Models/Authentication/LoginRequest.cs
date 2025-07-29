using System.ComponentModel.DataAnnotations;

namespace LYBT.WPF.Client.Core.Models.Authentication
{
    /// <summary>
    /// 登录请求模型
    /// </summary>
    public class LoginRequest
    {
        /// <summary>用户名</summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(32, ErrorMessage = "用户名长度不能超过32个字符")]
        public string Username { get; set; } = string.Empty;

        /// <summary>密码</summary>
        [Required(ErrorMessage = "密码不能为空")]
        public string Password { get; set; } = string.Empty;

        /// <summary>记住我</summary>
        public bool RememberMe { get; set; }

        /// <summary>客户端IP</summary>
        public string? ClientIp { get; set; }

        /// <summary>用户代理</summary>
        public string? UserAgent { get; set; }

        /// <summary>登录类型</summary>
        public string? LoginType { get; set; } = "Password";
    }
}