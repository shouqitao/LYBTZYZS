using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Auth
{

    /// <summary>
    /// 登录请求数据 - 前后端共享API契约
    /// 统一认证接口的请求模型，包含完整验证规则
    /// </summary>
    public class LoginRequest
    {

        /// <summary>用户名</summary>
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(32, ErrorMessage = "用户名长度不能超过32个字符")]
        [DisplayName("用户名")]
        public string Username { get; set; } = string.Empty;

        /// <summary>密码</summary>
        [Required(ErrorMessage = "密码不能为空")]
        [DisplayName("密码")]
        public string Password { get; set; } = string.Empty;

        /// <summary>记住我</summary>
        [DisplayName("记住我")]
        public bool RememberMe { get; set; } = false;

        /// <summary>客户端IP地址</summary>
        [DisplayName("客户端IP")]
        public string? ClientIp { get; set; }

        /// <summary>用户代理字符串</summary>
        [DisplayName("用户代理")]
        public string? UserAgent { get; set; }

        /// <summary>登录类型（Password, WeChat, OAuth等）</summary>
        [DisplayName("登录类型")]
        public string LoginType { get; set; } = "Password";
    }
}