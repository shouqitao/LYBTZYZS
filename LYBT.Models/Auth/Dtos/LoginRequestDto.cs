using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.Auth.Dtos {

    /// <summary>
    /// 用户登录请求 DTO
    /// </summary>
    public class LoginRequestDto {

        /// <summary>用户名</summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(32, ErrorMessage = "用户名长度不能超过32个字符")]
        [DisplayName("用户名")]
/// <summary>
/// Username 属性。
/// </summary>
        public string Username { get; set; } = string.Empty;

        /// <summary>密码</summary>
        [Required(ErrorMessage = "密码不能为空")]
        [DisplayName("密码")]
/// <summary>
/// Password 属性。
/// </summary>
        public string Password { get; set; } = string.Empty;

        /// <summary>客户端IP</summary>
        [DisplayName("客户端IP")]
/// <summary>
/// ClientIp 属性。
/// </summary>
        public string? ClientIp { get; set; }

        /// <summary>用户</summary>
        [DisplayName("UserAgent")]
/// <summary>
/// UserAgent 属性。
/// </summary>
        public string? UserAgent { get; set; }

        /// <summary>登录类型（Password, WeChat, OAuth等）</summary>
        [DisplayName("登录类型（Password, WeChat, OAuth等）")]
/// <summary>
/// LoginType 属性。
/// </summary>
        public string? LoginType { get; set; } = "Password";
    }
}
