using System.ComponentModel.DataAnnotations;

namespace LYBT.UI.PrismWpf.Models
{
    /// <summary>
    /// 登录请求模型
    /// </summary>
    public class LoginRequest
    {
        /// <summary>
        /// 用户名
        /// </summary>
        [Required(ErrorMessage = "请输入用户名")]
        public string UserName { get; set; } = string.Empty;

        /// <summary>
        /// 密码
        /// </summary>
        [Required(ErrorMessage = "请输入密码")]
        public string Password { get; set; } = string.Empty;

        /// <summary>
        /// 记住登录状态
        /// </summary>
        public bool RememberMe { get; set; } = false;
    }
}