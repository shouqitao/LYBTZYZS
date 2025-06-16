using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Users.Dtos {
    /// <summary>
    /// 用户登录请求 DTO（带校验注解）
    /// </summary>
    public class UserLoginDto {
        /// <summary>
        /// 登录用户名
        /// </summary>
        [Required(ErrorMessage = "登录用户名不能为空")]
        [StringLength(32, ErrorMessage = "登录用户名长度不能超过32个字符")]
        public string Username { get; set; } = string.Empty;

        /// <summary>
        /// 登录密码
        /// </summary>
        [Required(ErrorMessage = "密码不能为空")]
        [StringLength(32, MinimumLength = 6, ErrorMessage = "密码长度必须在6-32个字符之间")]
        public string Password { get; set; } = string.Empty;
    }
}
