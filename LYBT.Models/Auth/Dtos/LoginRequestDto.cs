using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Auth.Dtos {

    /// <summary>
    /// 用户登录请求 DTO
    /// </summary>
    public class LoginRequestDto {

        /// <summary>用户名</summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(32, ErrorMessage = "用户名长度不能超过32个字符")]
        public string Username { get; set; } = string.Empty;

        /// <summary>密码</summary>
        [Required(ErrorMessage = "密码不能为空")]
        public string Password { get; set; } = string.Empty;

        /// <summary>客户端IP</summary>
        public string? ClientIp { get; set; }

        /// <summary>UserAgent</summary>
        public string? UserAgent { get; set; }

        /// <summary>登录类型（Password, WeChat, OAuth等）</summary>
        public string? LoginType { get; set; } = "Password";
    }
}